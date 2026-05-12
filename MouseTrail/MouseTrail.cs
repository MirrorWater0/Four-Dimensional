using System;
using Godot;

public partial class MouseTrail : CanvasLayer
{
    [Export]
    private Vector2 _cursorHotspot = Vector2.Zero;

    [Export]
    private bool _enableStutterMonitor = true;

    [Export]
    private bool _showStutterOverlay = true;

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
    private const float MotionResponse = 0.02f;

    private Node2D _targetNode;
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
    private readonly Vector2 _stutterOverlayMargin = new(14f, 12f);

    public override void _Ready()
    {
        _targetNode = GetNodeOrNull<Node2D>("Node2D");
        _cursor = GetNodeOrNull<Control>("Cursor");
        _cursorMaterial = _cursor?.Material as ShaderMaterial;
        if (_cursor != null)
        {
            UpdateCursorPivot();
            _cursor.Resized += UpdateCursorPivot;
        }

        Vector2 mousePosition = GetViewport().GetMousePosition();
        _previousMousePosition = mousePosition;

        UpdateCursorPosition(mousePosition);
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        _gc0Count = GC.CollectionCount(0);
        _gc1Count = GC.CollectionCount(1);
        _gc2Count = GC.CollectionCount(2);
        CreateStutterOverlayIfNeeded();
    }

    public override void _ExitTree()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Hidden)
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        _burstAmount = 1.0f;
    }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();
        UpdateCursorPosition(mousePosition);

        float deltaF = (float)delta;
        float speed = deltaF > 0f ? mousePosition.DistanceTo(_previousMousePosition) / deltaF : 0f;
        bool pressed =
            Input.IsMouseButtonPressed(MouseButton.Left)
            || Input.IsMouseButtonPressed(MouseButton.Right)
            || Input.IsMouseButtonPressed(MouseButton.Middle);

        _pressAmount = Mathf.MoveToward(_pressAmount, pressed ? 1.0f : 0.0f, deltaF * PressLerpSpeed);
        _burstAmount = Mathf.MoveToward(_burstAmount, 0.0f, deltaF * BurstDecaySpeed);
        _motionAmount = Mathf.Lerp(_motionAmount, Mathf.Clamp(speed * MotionResponse, 0.0f, 1.0f), 0.18f);
        UpdateCursorRotation(deltaF);

        if (_cursorMaterial != null)
        {
            _cursorMaterial.SetShaderParameter("press_amount", _pressAmount);
            _cursorMaterial.SetShaderParameter("burst_amount", _burstAmount);
            _cursorMaterial.SetShaderParameter("motion_amount", _motionAmount);
        }

        UpdateStutterMonitor(deltaF);
        _previousMousePosition = mousePosition;
    }

    private void CreateStutterOverlayIfNeeded()
    {
        if (!_enableStutterMonitor || !_showStutterOverlay)
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
        if (!_enableStutterMonitor || deltaSeconds <= 0f)
            return;

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

    private void UpdateCursorPosition(Vector2 mousePosition)
    {
        if (_targetNode != null)
            _targetNode.GlobalPosition = mousePosition;

        if (_cursor != null)
            _cursor.GlobalPosition = mousePosition - _cursorHotspot;
    }

    private void UpdateCursorPivot()
    {
        if (_cursor == null)
            return;

        _cursor.PivotOffset = _cursor.Size * 0.5f;
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
