using System;
using System.IO;
using System.Text;
using Godot;

public partial class MouseTrail : CanvasLayer
{
    private const int CursorLayerOrder = 1000;
    private const int MouseTraceFrameCount = 1200;
    private const string MouseTracePath = "user://mouse_frame_trace.csv";

    private struct MouseFrameSample
    {
        public ulong Frame;
        public ulong TickUsec;
        public float DeltaMs;
        public Vector2 ViewportPosition;
        public Vector2I ScreenPosition;
        public Vector2 EventPosition;
        public int MotionEventCount;
        public bool Focused;
        public bool UseAccumulatedInput;
    }

    [Export]
    private Vector2 _cursorHotspot = Vector2.Zero;

    [Export]
    private bool _enableStutterMonitor = true;

    [Export]
    private bool _showStutterOverlay = false;

    [Export(PropertyHint.Range, "8,120,1")]
    private float _stutterThresholdMs = 24.0f;

    [Export(PropertyHint.Range, "0.1,3,0.1")]
    private float _stutterLogCooldownSeconds = 0.4f;

    [Export(PropertyHint.Range, "0.05,1.0,0.01")]
    private float _overlayRefreshSeconds = 0.15f;

    [Export]
    private bool _logStutterAsError = false;

    private const float PressLerpSpeed = 14.0f;
    private const float CursorRotationSpeed = 9.0f;
    private const float LeftPressCursorRotation = -0.24f;
    private const float BurstDecaySpeed = 2.8f;
    private const float MotionResponse = 0.0008f;

    private MouseTrailVisual _trailVisual;
    private bool _useSystemCursor;
    private Control _cursor;
    private ShaderMaterial _cursorMaterial;
    private Label _stutterLabel;
    private Vector2 _previousMousePosition;
    private float _pressAmount;
    private float _burstAmount;
    private float _motionAmount;
    private float _cursorRotation;
    private float _avgFrameMs = 16.7f;
    private float _peakFrameMs;
    private float _stutterLogCooldownLeft;
    private float _overlayRefreshLeft;
    private int _stutterCount;
    private int _gc0Count;
    private int _gc1Count;
    private int _gc2Count;
    private bool _ignoreNextTimingSample;
    private readonly Vector2 _stutterOverlayMargin = new(14f, 12f);
    private bool _traceMouseFrames;
    private MouseFrameSample[] _mouseTraceSamples;
    private int _mouseTraceSampleCount;
    private int _mouseMotionEventsSinceLastFrame;
    private Vector2 _lastMouseMotionEventPosition;

    public override void _Ready()
    {
        Layer = Math.Max(Layer, CursorLayerOrder);

        if (MobilePlatform.IsMobile)
        {
            Visible = false;
            SetProcess(false);
            SetProcessInput(false);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        _trailVisual = GetNodeOrNull<MouseTrailVisual>("TrailVisual");
        _cursor = GetNodeOrNull<Control>("Cursor");
        _cursorMaterial = _cursor?.Material as ShaderMaterial;
        if (_cursor != null)
        {
            UpdateCursorPivot();
            _cursor.Resized += UpdateCursorPivot;
        }

        Vector2 mousePosition = GetResponsiveMousePosition();
        _previousMousePosition = mousePosition;

        UpdateCursorPosition(mousePosition);
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        _gc0Count = GC.CollectionCount(0);
        _gc1Count = GC.CollectionCount(1);
        _gc2Count = GC.CollectionCount(2);
        CreateStutterOverlayIfNeeded();
        _traceMouseFrames = HasUserArgument("--trace-mouse-frames");
        if (_traceMouseFrames)
        {
            _mouseTraceSamples = new MouseFrameSample[MouseTraceFrameCount];
            GD.Print(
                $"[MouseFrameTrace] begin frames={MouseTraceFrameCount} path={ProjectSettings.GlobalizePath(MouseTracePath)}"
            );
        }
    }

    public override void _ExitTree()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Hidden)
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationApplicationFocusIn)
        {
            CallDeferred(MethodName.ResetPointerTracking);
            _avgFrameMs = 16.7f;
            _peakFrameMs = 0f;
            _stutterLogCooldownLeft = 0f;
        }
    }

    public void ResetPointerTracking()
    {
        Input.UseAccumulatedInput = true;
        Input.FlushBufferedEvents();
        Vector2 mousePosition = GetResponsiveMousePosition();
        _previousMousePosition = mousePosition;
        _motionAmount = 0f;
        _trailVisual?.ResetTrail();
        _ignoreNextTimingSample = true;
        UpdateCursorPosition(mousePosition);
        if (_cursorMaterial != null)
            _cursorMaterial.SetShaderParameter("motion_amount", 0f);
    }

    public void ResetPointerTrackingDeferred()
    {
        CallDeferred(MethodName.ResetPointerTracking);
    }

    public void SetUseSystemCursor(bool useSystemCursor)
    {
        _useSystemCursor = useSystemCursor || MobilePlatform.IsMobile;
        _trailVisual?.ResetTrail();
        if (_trailVisual != null)
            _trailVisual.Visible = !_useSystemCursor;
        if (_cursor != null)
            _cursor.Visible = !_useSystemCursor;

        Input.MouseMode = _useSystemCursor
            ? Input.MouseModeEnum.Visible
            : Input.MouseModeEnum.Hidden;
    }

    public override void _Input(InputEvent @event)
    {
        if (_traceMouseFrames && @event is InputEventMouseMotion mouseMotion)
        {
            _mouseMotionEventsSinceLastFrame++;
            _lastMouseMotionEventPosition = mouseMotion.Position;
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        if (_useSystemCursor || !Visible || GetWindow()?.HasFocus() != true
            || mouseButton.ButtonIndex is not (MouseButton.Left or MouseButton.Right or MouseButton.Middle))
            return;

        _burstAmount = 1.0f;
        _trailVisual?.EmitClick(mouseButton.Position);
    }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetResponsiveMousePosition();
        UpdateCursorPosition(mousePosition);

        float deltaF = (float)delta;
        bool pointerActive = !_useSystemCursor && Visible && GetWindow()?.HasFocus() == true
            && GetViewport().GetVisibleRect().HasPoint(mousePosition);
        if (_cursor != null)
            _cursor.Visible = pointerActive;
        if (pointerActive)
            _trailVisual?.Advance(deltaF, mousePosition);
        else
            _trailVisual?.ResetTrail();
        float speed = deltaF > 0f ? mousePosition.DistanceTo(_previousMousePosition) / deltaF : 0f;
        bool pressed =
            Input.IsMouseButtonPressed(MouseButton.Left)
            || Input.IsMouseButtonPressed(MouseButton.Right)
            || Input.IsMouseButtonPressed(MouseButton.Middle);

        _pressAmount = Mathf.MoveToward(_pressAmount, pressed ? 1.0f : 0.0f, deltaF * PressLerpSpeed);
        _burstAmount = Mathf.MoveToward(_burstAmount, 0.0f, deltaF * BurstDecaySpeed);
        _motionAmount = Mathf.Lerp(_motionAmount, Mathf.Clamp(speed * MotionResponse, 0.0f, 1.0f),
            1.0f - Mathf.Exp(-12.0f * deltaF));
        UpdateCursorRotation(deltaF);

        if (_cursorMaterial != null)
        {
            _cursorMaterial.SetShaderParameter("press_amount", _pressAmount);
            _cursorMaterial.SetShaderParameter("burst_amount", _burstAmount);
            _cursorMaterial.SetShaderParameter("motion_amount", _motionAmount);
        }

        UpdateStutterMonitor(deltaF);
        CaptureMouseFrame(deltaF, GetViewport().GetMousePosition());
        _previousMousePosition = mousePosition;
    }

    private void CaptureMouseFrame(float deltaSeconds, Vector2 viewportPosition)
    {
        if (
            !_traceMouseFrames
            || _mouseTraceSamples == null
            || _mouseTraceSampleCount >= _mouseTraceSamples.Length
        )
        {
            return;
        }

        _mouseTraceSamples[_mouseTraceSampleCount++] = new MouseFrameSample
        {
            Frame = Engine.GetProcessFrames(),
            TickUsec = Time.GetTicksUsec(),
            DeltaMs = deltaSeconds * 1000f,
            ViewportPosition = viewportPosition,
            ScreenPosition = DisplayServer.MouseGetPosition(),
            EventPosition = _lastMouseMotionEventPosition,
            MotionEventCount = _mouseMotionEventsSinceLastFrame,
            Focused = GetWindow()?.HasFocus() == true,
            UseAccumulatedInput = Input.UseAccumulatedInput,
        };
        _mouseMotionEventsSinceLastFrame = 0;

        if (_mouseTraceSampleCount < _mouseTraceSamples.Length)
            return;

        WriteMouseFrameTrace();
        _traceMouseFrames = false;
    }

    private void WriteMouseFrameTrace()
    {
        var csv = new StringBuilder(_mouseTraceSampleCount * 96);
        csv.AppendLine(
            "frame,tick_usec,delta_ms,viewport_x,viewport_y,screen_x,screen_y,event_x,event_y,motion_events,focused,use_accumulated_input"
        );

        int screenMovedViewportStatic = 0;
        int screenMovedWithoutEvent = 0;
        float maxViewportStep = 0f;
        float maxScreenStep = 0f;
        float maxDeltaMs = 0f;
        for (int i = 0; i < _mouseTraceSampleCount; i++)
        {
            MouseFrameSample sample = _mouseTraceSamples[i];
            csv.Append(sample.Frame).Append(',')
                .Append(sample.TickUsec).Append(',')
                .Append(sample.DeltaMs.ToString("F3")).Append(',')
                .Append(sample.ViewportPosition.X.ToString("F3")).Append(',')
                .Append(sample.ViewportPosition.Y.ToString("F3")).Append(',')
                .Append(sample.ScreenPosition.X).Append(',')
                .Append(sample.ScreenPosition.Y).Append(',')
                .Append(sample.EventPosition.X.ToString("F3")).Append(',')
                .Append(sample.EventPosition.Y.ToString("F3")).Append(',')
                .Append(sample.MotionEventCount).Append(',')
                .Append(sample.Focused ? "1" : "0").Append(',')
                .AppendLine(sample.UseAccumulatedInput ? "1" : "0");

            maxDeltaMs = Math.Max(maxDeltaMs, sample.DeltaMs);
            if (i == 0)
                continue;

            MouseFrameSample previous = _mouseTraceSamples[i - 1];
            float viewportStep = sample.ViewportPosition.DistanceTo(previous.ViewportPosition);
            float screenStep = new Vector2(sample.ScreenPosition.X, sample.ScreenPosition.Y).DistanceTo(
                new Vector2(previous.ScreenPosition.X, previous.ScreenPosition.Y)
            );
            maxViewportStep = Math.Max(maxViewportStep, viewportStep);
            maxScreenStep = Math.Max(maxScreenStep, screenStep);
            if (screenStep > 0.5f && viewportStep <= 0.01f)
                screenMovedViewportStatic++;
            if (screenStep > 0.5f && sample.MotionEventCount == 0)
                screenMovedWithoutEvent++;
        }

        string globalPath = ProjectSettings.GlobalizePath(MouseTracePath);
        File.WriteAllText(globalPath, csv.ToString());
        GD.Print(
            "[MouseFrameTrace] result "
                + $"samples={_mouseTraceSampleCount} max_delta_ms={maxDeltaMs:F3} "
                + $"max_viewport_step={maxViewportStep:F1} max_screen_step={maxScreenStep:F1} "
                + $"screen_moved_viewport_static={screenMovedViewportStatic} "
                + $"screen_moved_without_event={screenMovedWithoutEvent} path={globalPath}"
        );
    }

    private static bool HasUserArgument(string expected)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void CreateStutterOverlayIfNeeded()
    {
        if (!OS.IsDebugBuild() || !_enableStutterMonitor || !_showStutterOverlay)
            return;

        _stutterLabel = new Label
        {
            Name = "StutterOverlay",
            TopLevel = true,
            Position = _stutterOverlayMargin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 500,
        };
        _stutterLabel.AddThemeFontSizeOverride("font_size", 14);
        _stutterLabel.AddThemeConstantOverride("outline_size", 2);
        _stutterLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.84f));
        _stutterLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.94f, 1f, 0.95f));
        AddChild(_stutterLabel);
        UpdateStutterOverlayPosition();
    }

    private void UpdateStutterMonitor(float deltaSeconds)
    {
        if (!OS.IsDebugBuild() || !_enableStutterMonitor || deltaSeconds <= 0f)
            return;

        Window window = GetWindow();
        if (window != null && !window.HasFocus())
            return;

        if (_ignoreNextTimingSample)
        {
            _ignoreNextTimingSample = false;
            return;
        }

        float frameMs = deltaSeconds * 1000f;
        _avgFrameMs = Mathf.Lerp(_avgFrameMs, frameMs, 0.08f);
        _peakFrameMs = Mathf.Max(_peakFrameMs * 0.97f, frameMs);
        _stutterLogCooldownLeft = Mathf.Max(0f, _stutterLogCooldownLeft - deltaSeconds);
        _overlayRefreshLeft = Mathf.Max(0f, _overlayRefreshLeft - deltaSeconds);

        int currentGc0 = GC.CollectionCount(0);
        int currentGc1 = GC.CollectionCount(1);
        int currentGc2 = GC.CollectionCount(2);
        int gcDelta0 = currentGc0 - _gc0Count;
        int gcDelta1 = currentGc1 - _gc1Count;
        int gcDelta2 = currentGc2 - _gc2Count;
        _gc0Count = currentGc0;
        _gc1Count = currentGc1;
        _gc2Count = currentGc2;

        if (frameMs >= _stutterThresholdMs)
        {
            _stutterCount++;
            if (_stutterLogCooldownLeft <= 0f)
            {
                string sceneName = GetTree()?.CurrentScene?.Name ?? "UnknownScene";
                string message =
                    $"[Stutter] frame={Engine.GetProcessFrames()} {frameMs:0.0}ms scene={sceneName} gc=({gcDelta0},{gcDelta1},{gcDelta2})";
                if (_logStutterAsError)
                    GD.PrintErr(message);
                else
                    GD.Print(message);
                _stutterLogCooldownLeft = _stutterLogCooldownSeconds;
            }
        }

        if (_stutterLabel == null || !GodotObject.IsInstanceValid(_stutterLabel))
            return;

        if (_overlayRefreshLeft > 0f)
            return;

        _overlayRefreshLeft = Mathf.Max(0.05f, _overlayRefreshSeconds);
        float avgFps = _avgFrameMs > 0.001f ? 1000f / _avgFrameMs : 0f;
        _stutterLabel.Text =
            $"Frame {frameMs:0.0}ms  Avg {_avgFrameMs:0.0}ms ({avgFps:0} FPS)\n"
            + $"Peak {_peakFrameMs:0.0}ms  Spikes {_stutterCount}  GC {gcDelta0}/{gcDelta1}/{gcDelta2}";
        UpdateStutterOverlayPosition();
    }

    public static Vector2 GetResponsiveViewportMousePosition(
        Viewport viewport,
        Window window = null
    )
    {
        // GUI hit testing and InputEvent positions use this coordinate system. Converting
        // the OS cursor manually loses the Windows DPI scale on high-resolution displays.
        return viewport?.GetMousePosition() ?? Vector2.Zero;
    }

    private Vector2 GetResponsiveMousePosition() =>
        GetResponsiveViewportMousePosition(GetViewport(), GetWindow());

    private void UpdateCursorPosition(Vector2 mousePosition)
    {
        if (_cursor != null)
            _cursor.GlobalPosition = mousePosition - _cursorHotspot;
    }

    private void UpdateCursorPivot()
    {
        if (_cursor == null)
            return;

        // Rotation stays anchored to the visible tip, which is also the GUI hit position.
        _cursor.PivotOffset = _cursorHotspot;
    }

    private void UpdateCursorRotation(float deltaSeconds)
    {
        if (_cursor == null)
            return;

        float targetRotation = Input.IsMouseButtonPressed(MouseButton.Left)
            ? LeftPressCursorRotation
            : 0.0f;
        _cursorRotation = Mathf.Lerp(
            _cursorRotation,
            targetRotation,
            1.0f - Mathf.Exp(-CursorRotationSpeed * deltaSeconds)
        );
        _cursor.Rotation = _cursorRotation;
    }

    private void UpdateStutterOverlayPosition()
    {
        if (_stutterLabel == null || !GodotObject.IsInstanceValid(_stutterLabel))
            return;

        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 labelSize = _stutterLabel.GetCombinedMinimumSize();
        _stutterLabel.Position = new Vector2(
            _stutterOverlayMargin.X,
            Mathf.Max(_stutterOverlayMargin.Y, viewportSize.Y - labelSize.Y - _stutterOverlayMargin.Y)
        );
    }
}
