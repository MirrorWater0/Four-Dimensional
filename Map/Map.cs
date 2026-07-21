using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class Map : Control
{
    private const float WheelScrollBounceStep = 18f;
    private const float WheelScrollBounceMaxOffset = 46f;
    private const float WheelScrollBounceOutDuration = 0.06f;
    private const float WheelScrollBounceBackDuration = 0.28f;

    private static readonly PackedScene DebugConsoleScene = GD.Load<PackedScene>(
        "res://Map/DebugConsole.tscn"
    );
    private static readonly string[] BattleWorldBranchPaths =
    [
        "DragButton",
        "LevelProgress",
        "MapLabel",
        "UI",
        "BattleReadyLayer",
        "SiteUI",
        "MaskLayer",
        "Camera",
    ];

    [Export]
    public bool WarmupMode { get; set; }

    public Button DragButton => field ??= GetNode("DragButton") as Button;
    public TextureRect GameMap => field ??= GetNode("GameMap") as TextureRect;
    public DynamicCamera Camera => field ??= GetNode("Camera") as DynamicCamera;
    public Label SeedLabel => field ??= GetNode("UI/SeedLabel") as Label;
    public Label RegionLabel =>
        field ??=
            GetNodeOrNull<Label>("PlayerResourceState/RegionLabel")
            ?? GetNodeOrNull<Label>("MapLabel/RegionLabel")
            ?? GetNodeOrNull<Label>("UI/RegionLabel");
    public Label DifficultyLabel =>
        field ??= GetNodeOrNull<Label>("PlayerResourceState/TransitionEnergyControl/Difficulty");
    private Label TransitionEnergyLabel =>
        field ??= GetNodeOrNull<Label>("PlayerResourceState/TransitionEnergyControl/Label");
    private Label RelicLabel =>
        field ??= GetNodeOrNull<Label>("PlayerResourceState/RelicContainer/Label");
    private Label ReadyButtonLabel => field ??= GetNodeOrNull<Label>("UI/ReadyButton/Label");
    private Label NodeLegendTitleLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Title");
    private Label NodeLegendNormalLabel =>
        field ??= GetNodeOrNull<Label>(
            "MapLabel/NodeTypeLegend/Margin/LegendList/Normal/Row/Label"
        );
    private Label NodeLegendEventLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Event/Row/Label");
    private Label NodeLegendShopLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Shop/Row/Label");
    private Label NodeLegendTreasureLabel =>
        field ??= GetNodeOrNull<Label>(
            "MapLabel/NodeTypeLegend/Margin/LegendList/Treasure/Row/Label"
        );
    private Label NodeLegendRestLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Rest/Row/Label");
    private Label NodeLegendEliteLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Elite/Row/Label");
    private Label NodeLegendBossLabel =>
        field ??= GetNodeOrNull<Label>("MapLabel/NodeTypeLegend/Margin/LegendList/Boss/Row/Label");
    private Control MiniMapRoot => field ??= GetNodeOrNull<Control>("UI/MiniMap");
    private TextureRect MiniMapPreview =>
        field ??= GetNodeOrNull<TextureRect>("UI/MiniMap/MapPreview");
    private Control MiniMapPlayerIndicator =>
        field ??= GetNodeOrNull<Control>("UI/MiniMap/MapPreview/PlayerIndicator");
    private LevelProgress LevelProgressNode =>
        field ??= GetNodeOrNull<LevelProgress>("LevelProgress");
    private Control NodeTypeLegend => field ??= GetNodeOrNull<Control>("MapLabel/NodeTypeLegend");

    [Export(PropertyHint.Range, "0,40,1")]
    public float DragStartThreshold = 16.0f;

    [Export]
    public bool SnapCameraToPixel = false;

    [Export(PropertyHint.Range, "1,60,1")]
    public float DragFollowSharpness = 24.0f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float InertiaStrength = 0.18f;

    [Export(PropertyHint.Range, "1,800,1")]
    public float WheelStep = 360.0f;

    [Export(PropertyHint.Range, "1,600,1")]
    public float WheelScrollSpring = 210.0f;

    [Export(PropertyHint.Range, "1,80,1")]
    public float WheelScrollDamping = 24.0f;

    [Export(PropertyHint.Range, "100,8000,10")]
    public float WheelScrollMaxVelocity = 5200.0f;

    [Export(PropertyHint.Range, "0.1,4,0.1")]
    public float WheelScrollSnapDistance = 0.6f;

    [Export(PropertyHint.Range, "1,100,1")]
    public float WheelScrollStopSpeed = 12.0f;

    [Export(PropertyHint.Range, "1,40,1")]
    public float PanGestureMultiplier = 18.0f;

    private Vector2 _targetPos;
    private bool _isDrag;
    private bool _isDragActive;
    private bool _isWheelPanning;
    private double _wheelScrollPosition;
    private double _wheelScrollVelocity;
    private Vector2 _cameraBaseOffset;
    private Tween _wheelScrollBounceTween;
    private Vector2 _dragStartMousePos = Vector2.Zero;
    private Vector2 _dragStartCameraPos = Vector2.Zero;
    Vector2 _velocity = Vector2.Zero;
    private Vector2 _dragVelocity = Vector2.Zero;
    private ulong _wheelHandledFrame = ulong.MaxValue;
    private CanvasLayer SiteUiLayer => field ??= GetNodeOrNull<CanvasLayer>("SiteUI");
    private CanvasLayer FrontUiLayer => field ??= GetNodeOrNull<CanvasLayer>("BattleReadyLayer");
    private CanvasLayer MenuLayer => field ??= GetNodeOrNull<CanvasLayer>("MenuLayer");
    private ReadyButton ReadyButtonNode => field ??= GetNodeOrNull<ReadyButton>("UI/ReadyButton");
    private DebugConsole DebugConsoleNode => field ??= GetNodeOrNull<DebugConsole>("DebugConsole");
    public PlayerResourceState PlayerResourceState =>
        field ??= GetNode<PlayerResourceState>("PlayerResourceState");
    public bool IsMapPeekModeActive => _mapPeekModeActive;
    public bool IsBattleWorldSuspended => _battleWorldSuspended;
    private bool _regionLabelInitialized;
    private bool _lastRegionTwoUnlocked;
    private ulong _blockingOverlayFrame = ulong.MaxValue;
    private bool _blockingOverlayResult;
    private bool _mapPeekModeActive;
    private readonly List<VisibilitySnapshot> _mapPeekHiddenNodes = new();
    private readonly List<BattleWorldNodeSnapshot> _battleWorldNodeSnapshots = new();
    private bool _battleWorldSuspensionRequested;
    private bool _battleWorldSuspended;
    private bool _battleWorldEnvironmentCaptured;
    private Godot.Environment _battleWorldEnvironment;
    private ProcessModeEnum _battleWorldEnvironmentProcessMode;

    private sealed class VisibilitySnapshot
    {
        public Node Node;
        public bool Visible;
    }

    private sealed class BattleWorldNodeSnapshot
    {
        public Node Node;
        public ProcessModeEnum ProcessMode;
        public bool Processing;
        public bool PhysicsProcessing;
        public bool ProcessingInput;
        public bool ProcessingUnhandledInput;
        public bool? Visible;
        public bool? CameraEnabled;
        public Control.MouseFilterEnum? MouseFilter;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        ulong frame = Engine.GetProcessFrames();
        UpdateRegionLabel();

        if (HasBlockingOverlay())
        {
            _isDrag = false;
            _isDragActive = false;
            _isWheelPanning = false;
            _wheelScrollVelocity = 0d;
            CancelWheelScrollBounce();
            _dragVelocity = Vector2.Zero;
            _velocity = Vector2.Zero;
            _targetPos = Camera.ClampToBoundary(Camera.GlobalPosition);
            _wheelHandledFrame = frame;
            UpdateMiniMapIndicator();
            return;
        }

        if (_wheelHandledFrame != frame)
        {
            if (Input.IsActionJustPressed("Wheelup"))
            {
                ApplyWheelMove(-WheelStep);
                _wheelHandledFrame = frame;
            }
            else if (Input.IsActionJustPressed("Wheeldown"))
            {
                ApplyWheelMove(WheelStep);
                _wheelHandledFrame = frame;
            }
        }

        if (_isDrag)
        {
            Vector2 mousePos = GetViewport().GetMousePosition();

            if (!_isDragActive)
            {
                if (mousePos.DistanceTo(_dragStartMousePos) < DragStartThreshold)
                {
                    return;
                }

                _isDragActive = true;
                _dragStartMousePos = mousePos;
                _dragStartCameraPos = Camera.GlobalPosition;
                _dragVelocity = Vector2.Zero;
                return;
            }

            float dragFromStartX = (mousePos.X - _dragStartMousePos.X) / Camera.Zoom.X;
            Vector2 rawTarget = new(_dragStartCameraPos.X - dragFromStartX, Camera.FixedCenterY);
            Vector2 nextTarget = Camera.ClampToBoundary(rawTarget);

            if (dt > 0.0001f)
            {
                Vector2 worldDragVelocity = (nextTarget - _targetPos) / dt;
                _dragVelocity = _dragVelocity.Lerp(worldDragVelocity, 0.35f);
            }

            _targetPos = nextTarget;
            float dragAlpha = 1.0f - Mathf.Exp(-DragFollowSharpness * dt);
            SetCameraPosition(Camera.GlobalPosition.Lerp(_targetPos, dragAlpha));

            _velocity = Vector2.Zero;
            return;
        }

        if (_isWheelPanning)
        {
            float frameDelta = Mathf.Clamp(dt, 0f, 0.05f);
            Vector2 wheelDesiredTarget = Camera.ClampToBoundary(_targetPos);
            _targetPos = wheelDesiredTarget;
            GetCameraHorizontalCenterBoundary(out float minX, out float maxX);
            _wheelScrollPosition = Math.Clamp(_wheelScrollPosition, minX, maxX);

            double distance = wheelDesiredTarget.X - _wheelScrollPosition;
            if (
                Math.Abs(distance) <= WheelScrollSnapDistance
                && Math.Abs(_wheelScrollVelocity) <= WheelScrollStopSpeed
            )
            {
                _wheelScrollPosition = wheelDesiredTarget.X;
                _wheelScrollVelocity = 0d;
                SetCameraPosition(wheelDesiredTarget);
                _isWheelPanning = false;
                UpdateMiniMapIndicator();
                return;
            }

            _wheelScrollVelocity += distance * WheelScrollSpring * frameDelta;
            _wheelScrollVelocity *= Math.Exp(-WheelScrollDamping * frameDelta);
            _wheelScrollVelocity = Math.Clamp(
                _wheelScrollVelocity,
                -WheelScrollMaxVelocity,
                WheelScrollMaxVelocity
            );

            double nextPosition =
                _wheelScrollPosition + _wheelScrollVelocity * frameDelta;
            if (nextPosition <= minX || nextPosition >= maxX)
            {
                nextPosition = Math.Clamp(nextPosition, minX, maxX);
                _wheelScrollVelocity = 0d;
            }

            _wheelScrollPosition = nextPosition;
            SetCameraPosition(new Vector2((float)_wheelScrollPosition, Camera.FixedCenterY));
            _velocity = Vector2.Zero;
            UpdateMiniMapIndicator();
            return;
        }

        bool isTargetOutOfBoundary = !Camera.IsInsideBoundary(_targetPos);
        Vector2 desiredTarget = isTargetOutOfBoundary
            ? Camera.ClampToBoundary(_targetPos)
            : _targetPos;

        if (isTargetOutOfBoundary)
        {
            _targetPos = desiredTarget;
        }

        _velocity *= Camera.VelocityDamping;

        if (isTargetOutOfBoundary)
        {
            _velocity += Camera.FollowStrength * (desiredTarget - Camera.GlobalPosition) * dt;
        }

        Vector2 wantedPosition = Camera.GlobalPosition + _velocity * dt;
        Vector2 finalPosition = SetCameraPosition(wantedPosition);
        if (!Mathf.IsEqualApprox(finalPosition.X, wantedPosition.X))
        {
            _velocity.X = 0;
        }

        if (
            !_isDrag
            && isTargetOutOfBoundary
            && _velocity.LengthSquared() < 0.25f
            && Camera.GlobalPosition.DistanceSquaredTo(desiredTarget) < 0.25f
        )
        {
            SetCameraPosition(desiredTarget);
            _velocity = Vector2.Zero;
        }

        UpdateMiniMapIndicator();
    }

    public override void _Input(InputEvent @event)
    {
        if (HasBlockingOverlay())
            return;

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ApplyWheelMove(-WheelStep * Math.Max(0.35f, Math.Abs(mouseButton.Factor)));
                _wheelHandledFrame = Engine.GetProcessFrames();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ApplyWheelMove(WheelStep * Math.Max(0.35f, Math.Abs(mouseButton.Factor)));
                _wheelHandledFrame = Engine.GetProcessFrames();
                return;
            }
        }

        if (@event is InputEventPanGesture panGesture)
        {
            float scrollDelta = panGesture.Delta.Y * PanGestureMultiplier;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                ApplyWheelMove(scrollDelta);
                _wheelHandledFrame = Engine.GetProcessFrames();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Ready()
    {
        if (WarmupMode)
        {
            SetProcess(false);
            SetProcessInput(false);
            SetPhysicsProcess(false);
            return;
        }

        LocalizeStaticTexts();
        GetNodeOrNull<MouseTrail>("/root/MouseTrail")?.ResetPointerTrackingDeferred();
        SeedLabel.Text = I18n.Format("ui.map.seed", "Seed: {value}", ("value", GameInfo.Seed));
        if (DifficultyLabel != null)
        {
            int difficulty = Math.Clamp(
                GameInfo.Difficulty,
                GameInfo.MinDifficulty,
                GameInfo.MaxDifficulty
            );
            DifficultyLabel.Text = I18n.Format(
                "ui.common.difficulty_value",
                "难度 {value}",
                ("value", difficulty)
            );
        }
        UpdateRegionLabel();
        Camera.LimitEnabled = false;
        Camera.PositionSmoothingEnabled = false;
        Camera.Zoom = Vector2.One;
        Camera.HalfViewportWidth = 960.0f;
        Camera.FixedCenterY = 540.0f;
        _cameraBaseOffset = Camera.Offset;
        DragButton.Disabled = false;
        _targetPos = Camera.ClampToBoundary(Camera.GlobalPosition);
        SetCameraPosition(_targetPos);
        UpdateMiniMapIndicator();
        SceneTransitionLayer.Ensure(this);
        EnsureDebugConsole();
        ConnectNodeTypeLegend(NodeTypeLegend);
        DragButton.ButtonDown += () =>
        {
            _isDrag = true;
            _isDragActive = false;
            _isWheelPanning = false;
            _wheelScrollVelocity = 0d;
            CancelWheelScrollBounce();
            _dragStartMousePos = GetViewport().GetMousePosition();
            _dragStartCameraPos = _targetPos;
            _velocity = Vector2.Zero;
            _dragVelocity = Vector2.Zero;
            _targetPos = Camera.GlobalPosition;
        };
        DragButton.ButtonUp += () =>
        {
            _isDrag = false;
            _isDragActive = false;
            Vector2 releaseVelocity = _dragVelocity * Mathf.Max(0.0f, InertiaStrength);
            _velocity = releaseVelocity.LimitLength(Camera.MaxReleaseSpeed);
            _targetPos = Camera.ClampToBoundary(_targetPos);

            if (Camera.IsInsideBoundary(_targetPos))
            {
                _targetPos = Camera.GlobalPosition;
            }
        };

        CallDeferred(nameof(ShowPendingStarterBonusChoiceIfNeeded));
        CallDeferred(nameof(ShowPendingBossRelicChoiceIfNeeded));
        CallDeferred(nameof(StartBattleResourcePrewarm));
    }

    public override void _ExitTree()
    {
        _battleWorldSuspensionRequested = false;
        ExitMapPeekMode();
        RestoreBattleWorldSuspension();
    }

    private void ShowPendingStarterBonusChoiceIfNeeded()
    {
        if (WarmupMode || !StarterBonusChoice.ShouldShowPendingChoice())
            return;

        StarterBonusChoice.Show(this);
    }

    private void ShowPendingBossRelicChoiceIfNeeded()
    {
        if (WarmupMode || !BossRelicChoice.ShouldShowPendingChoice())
            return;

        BossRelicChoice.Show(this);
    }

    private async void StartBattleResourcePrewarm()
    {
        if (WarmupMode)
            return;

        await BattleStartResourcePreloader.PrewarmForMapAsync(this, LevelProgressNode);
    }

    public void ToggleMapPeekMode()
    {
        if (_mapPeekModeActive)
            ExitMapPeekMode();
        else
            EnterMapPeekMode();
    }

    public void EnterMapPeekMode()
    {
        if (_mapPeekModeActive)
            return;

        _mapPeekModeActive = true;
        PlayerResourceState?.SetMapPeekInputActive(true);
        if (_battleWorldSuspensionRequested)
        {
            RestoreBattleWorldSuspension();
            Camera?.MakeCurrent();
        }
        _mapPeekHiddenNodes.Clear();

        HideForMapPeek(SiteUiLayer);
        HideForMapPeek(FrontUiLayer);
        HideForMapPeek(MenuLayer);
        HideForMapPeek(GetNodeOrNull<CanvasItem>("UI/ReadyButton"));
        HideRootCanvasLayersForMapPeek();
        HideBattleOverlaysForMapPeek();

        _isDrag = false;
        _isDragActive = false;
        _isWheelPanning = false;
        _wheelScrollVelocity = 0d;
        CancelWheelScrollBounce();
        _dragVelocity = Vector2.Zero;
        _velocity = Vector2.Zero;
        _blockingOverlayFrame = ulong.MaxValue;
    }

    public void ExitMapPeekMode()
    {
        if (!_mapPeekModeActive)
            return;

        for (int i = _mapPeekHiddenNodes.Count - 1; i >= 0; i--)
        {
            var snapshot = _mapPeekHiddenNodes[i];
            if (
                snapshot?.Node == null
                || !GodotObject.IsInstanceValid(snapshot.Node)
                || snapshot.Node.IsQueuedForDeletion()
            )
            {
                continue;
            }

            snapshot.Node.Set("visible", snapshot.Visible);
        }

        _mapPeekHiddenNodes.Clear();
        _mapPeekModeActive = false;
        PlayerResourceState?.SetMapPeekInputActive(false);
        _blockingOverlayFrame = ulong.MaxValue;
        if (_battleWorldSuspensionRequested)
            ApplyBattleWorldSuspension();
    }

    public void SetBattleWorldSuspended(bool suspended)
    {
        _battleWorldSuspensionRequested = suspended;
        if (!suspended)
        {
            RestoreBattleWorldSuspension();
            return;
        }

        if (!_mapPeekModeActive)
            ApplyBattleWorldSuspension();
    }

    private void ApplyBattleWorldSuspension()
    {
        if (_battleWorldSuspended || !IsInsideTree())
            return;

        _battleWorldNodeSnapshots.Clear();
        SuspendBattleWorldNode(this, hide: false, disableProcessMode: false);
        foreach (string path in BattleWorldBranchPaths)
        {
            SuspendBattleWorldNode(
                GetNodeOrNull(path),
                hide: true,
                disableProcessMode: true
            );
        }

        WorldEnvironment environment = GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (environment != null)
        {
            _battleWorldEnvironment = environment.Environment;
            _battleWorldEnvironmentProcessMode = environment.ProcessMode;
            _battleWorldEnvironmentCaptured = true;
            environment.Environment = null;
            environment.ProcessMode = ProcessModeEnum.Disabled;
        }

        _isDrag = false;
        _isDragActive = false;
        _isWheelPanning = false;
        _wheelScrollVelocity = 0d;
        CancelWheelScrollBounce();
        _dragVelocity = Vector2.Zero;
        _velocity = Vector2.Zero;
        _blockingOverlayFrame = ulong.MaxValue;
        _battleWorldSuspended = true;
    }

    private void RestoreBattleWorldSuspension()
    {
        if (!_battleWorldSuspended)
            return;

        foreach (BattleWorldNodeSnapshot snapshot in _battleWorldNodeSnapshots)
        {
            Node node = snapshot?.Node;
            if (
                node == null
                || !GodotObject.IsInstanceValid(node)
                || node.IsQueuedForDeletion()
            )
            {
                continue;
            }

            node.ProcessMode = snapshot.ProcessMode;
            node.SetProcess(snapshot.Processing);
            node.SetPhysicsProcess(snapshot.PhysicsProcessing);
            node.SetProcessInput(snapshot.ProcessingInput);
            node.SetProcessUnhandledInput(snapshot.ProcessingUnhandledInput);
            if (snapshot.Visible.HasValue && node is CanvasItem canvasItem)
                canvasItem.Visible = snapshot.Visible.Value;
            if (snapshot.Visible.HasValue && node is CanvasLayer canvasLayer)
                canvasLayer.Visible = snapshot.Visible.Value;
            if (snapshot.CameraEnabled.HasValue && node is Camera2D camera)
                camera.Enabled = snapshot.CameraEnabled.Value;
            if (snapshot.MouseFilter.HasValue && node is Control control)
                control.MouseFilter = snapshot.MouseFilter.Value;
        }

        if (_battleWorldEnvironmentCaptured)
        {
            WorldEnvironment environment = GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
            if (environment != null)
            {
                environment.Environment = _battleWorldEnvironment;
                environment.ProcessMode = _battleWorldEnvironmentProcessMode;
            }
        }

        _battleWorldNodeSnapshots.Clear();
        _battleWorldEnvironment = null;
        _battleWorldEnvironmentCaptured = false;
        _blockingOverlayFrame = ulong.MaxValue;
        _battleWorldSuspended = false;
    }

    private void SuspendBattleWorldNode(Node node, bool hide, bool disableProcessMode)
    {
        if (node == null || !GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
            return;

        _battleWorldNodeSnapshots.Add(
            new BattleWorldNodeSnapshot
            {
                Node = node,
                ProcessMode = node.ProcessMode,
                Processing = node.IsProcessing(),
                PhysicsProcessing = node.IsPhysicsProcessing(),
                ProcessingInput = node.IsProcessingInput(),
                ProcessingUnhandledInput = node.IsProcessingUnhandledInput(),
                Visible = node is CanvasItem item
                    ? item.Visible
                    : node is CanvasLayer layer ? layer.Visible : null,
                CameraEnabled = node is Camera2D camera ? camera.Enabled : null,
                MouseFilter = node is Control control ? control.MouseFilter : null,
            }
        );

        if (disableProcessMode)
            node.ProcessMode = ProcessModeEnum.Disabled;
        node.SetProcess(false);
        node.SetPhysicsProcess(false);
        node.SetProcessInput(false);
        node.SetProcessUnhandledInput(false);
        if (node is Control inputControl)
            inputControl.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (hide && node is CanvasItem canvasItem)
            canvasItem.Visible = false;
        if (hide && node is CanvasLayer canvasLayer)
            canvasLayer.Visible = false;
        if (node is Camera2D cameraNode)
            cameraNode.Enabled = false;
    }

    private void HideRootCanvasLayersForMapPeek()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return;

        foreach (Node child in root.GetChildren())
        {
            if (child is CanvasLayer layer && !ShouldPreserveRootLayerForMapPeek(layer))
                HideForMapPeek(layer);
        }
    }

    private void HideBattleOverlaysForMapPeek()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return;

        HideBattleOverlaysForMapPeek(root);
    }

    private void HideBattleOverlaysForMapPeek(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
            return;

        if (node is Reward)
            HideForMapPeek(node);

        if (node is Battle battle)
            HideBattleCanvasLayersForMapPeek(battle);

        foreach (Node child in node.GetChildren())
            HideBattleOverlaysForMapPeek(child);
    }

    private void HideBattleCanvasLayersForMapPeek(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
            return;

        foreach (Node child in node.GetChildren())
        {
            if (child == null || !GodotObject.IsInstanceValid(child) || child.IsQueuedForDeletion())
                continue;

            if (child is CanvasLayer layer)
                HideForMapPeek(layer);

            HideBattleCanvasLayersForMapPeek(child);
        }
    }

    private static bool ShouldPreserveRootLayerForMapPeek(CanvasLayer layer)
    {
        return layer != null
            && (
                string.Equals(layer.Name, "MouseTrail", StringComparison.Ordinal)
                || string.Equals(layer.Name, "TipLayer", StringComparison.Ordinal)
            );
    }

    private void HideForMapPeek(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
            return;

        bool visible = node.Get("visible").AsBool();
        if (!visible)
            return;

        _mapPeekHiddenNodes.Add(new VisibilitySnapshot { Node = node, Visible = visible });
        node.Set("visible", false);
    }

    private void ConnectNodeTypeLegend(Control legend)
    {
        if (legend == null)
            return;

        foreach (Node child in legend.GetChildren())
            ConnectLegendButtonsRecursive(child);
    }

    private void ConnectLegendButtonsRecursive(Node node)
    {
        if (node is Button button && button.HasMeta("level_type"))
        {
            var variant = button.GetMeta("level_type");
            var type = (LevelNode.LevelType)(int)variant;
            button.MouseEntered += () => SetNodeTypeLegendHighlight(type);
            button.MouseExited += ClearNodeTypeLegendHighlight;
        }

        foreach (Node child in node.GetChildren())
            ConnectLegendButtonsRecursive(child);
    }

    private void SetNodeTypeLegendHighlight(LevelNode.LevelType type)
    {
        LevelProgressNode?.SetNodeTypeLegendHighlight(type);
    }

    private void ClearNodeTypeLegendHighlight()
    {
        LevelProgressNode?.SetNodeTypeLegendHighlight(null);
    }

    public void CloseWindow()
    {
        PreloadeScene.ReleaseCachedResources();
        GetTree().Quit();
    }

    public Tween BlackMaskAnimation(float duration, bool hideAfter = true)
    {
        return SceneTransitionLayer.Ensure(this)?.PulseBlack(duration, hideAfter);
    }

    public bool HasFrontUiChildren()
    {
        return FrontUiLayer != null && FrontUiLayer.GetChildCount() > 0;
    }

    public async Task CloseFrontUiLayerAsync()
    {
        if (!HasFrontUiChildren())
            return;

        var closingNodes = new Godot.Collections.Array<Node>();
        if (FrontUiLayer != null)
        {
            foreach (Node child in FrontUiLayer.GetChildren())
            {
                if (child != null && GodotObject.IsInstanceValid(child))
                    closingNodes.Add(child);
            }
        }

        var tasks = new System.Collections.Generic.List<Task>(2);
        if (ReadyButtonNode != null)
            tasks.Add(ReadyButtonNode.CloseBattleReadyAsync(confirmTactics: true));

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);

        if (FrontUiLayer == null)
            return;

        for (int i = 0; i < closingNodes.Count; i++)
        {
            var child = closingNodes[i];
            if (
                child != null
                && GodotObject.IsInstanceValid(child)
                && child.IsInsideTree()
                && child.GetParent() == FrontUiLayer
            )
                child.QueueFree();
        }
    }

    private Vector2 SetCameraPosition(Vector2 position)
    {
        Vector2 clamped = Camera.ClampToBoundary(position);
        Camera.GlobalPosition = SnapCameraToPixel ? clamped.Round() : clamped;
        return clamped;
    }

    public void ResetCameraToStart()
    {
        _isDrag = false;
        _isDragActive = false;
        _isWheelPanning = false;
        _wheelScrollVelocity = 0d;
        CancelWheelScrollBounce();
        _dragVelocity = Vector2.Zero;
        _velocity = Vector2.Zero;
        _targetPos = Camera.ClampToBoundary(
            new Vector2(Camera.WorldLeftBoundary, Camera.FixedCenterY)
        );
        SetCameraPosition(_targetPos);
        UpdateMiniMapIndicator();
    }

    public void ForceUpdateRegionLabel()
    {
        _regionLabelInitialized = false;
        UpdateRegionLabel();
    }

    private void ApplyWheelMove(float deltaX)
    {
        if (!_isWheelPanning)
        {
            Vector2 currentPosition = Camera.ClampToBoundary(Camera.GlobalPosition);
            _wheelScrollPosition = currentPosition.X;
            _wheelScrollVelocity = 0d;
            _targetPos = currentPosition;
        }

        Vector2 requestedTarget = _targetPos + new Vector2(deltaX, 0f);
        Vector2 clampedTarget = Camera.ClampToBoundary(requestedTarget);
        if (!Mathf.IsEqualApprox(requestedTarget.X, clampedTarget.X))
        {
            GetCameraHorizontalCenterBoundary(out float minX, out float maxX);
            bool isAtRequestedEdge = deltaX < 0f
                ? _wheelScrollPosition <= minX + 0.5d
                : _wheelScrollPosition >= maxX - 0.5d;
            if (isAtRequestedEdge)
            {
                float bounceStrength = Mathf.Clamp(
                    Mathf.Abs(deltaX) / Mathf.Max(1f, WheelStep),
                    0.5f,
                    2f
                );
                PlayWheelScrollBounce(Mathf.Sign(deltaX), bounceStrength);
            }
        }

        _targetPos = clampedTarget;
        _isWheelPanning = true;
        _velocity = Vector2.Zero;
    }

    private void PlayWheelScrollBounce(float scrollDirection, float strength)
    {
        if (Camera == null || !GodotObject.IsInstanceValid(Camera))
            return;

        GetCameraHorizontalCenterBoundary(out float minX, out float maxX);
        if (Mathf.IsEqualApprox(minX, maxX))
            return;

        float direction = scrollDirection >= 0f ? 1f : -1f;
        float currentOffset = Camera.Offset.X - _cameraBaseOffset.X;
        if (Math.Sign(currentOffset) != Math.Sign(direction))
            currentOffset = 0f;

        float targetOffset = Mathf.Clamp(
            currentOffset
                + direction * WheelScrollBounceStep * Mathf.Clamp(strength, 0.5f, 2f),
            -WheelScrollBounceMaxOffset,
            WheelScrollBounceMaxOffset
        );

        _wheelScrollBounceTween?.Kill();
        _wheelScrollBounceTween = Camera.CreateTween();
        _wheelScrollBounceTween.SetParallel(false);
        _wheelScrollBounceTween
            .TweenProperty(
                Camera,
                "offset",
                _cameraBaseOffset + new Vector2(targetOffset, 0f),
                WheelScrollBounceOutDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _wheelScrollBounceTween
            .TweenProperty(
                Camera,
                "offset",
                _cameraBaseOffset,
                WheelScrollBounceBackDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _wheelScrollBounceTween.TweenCallback(
            Callable.From(() => _wheelScrollBounceTween = null)
        );
    }

    private void CancelWheelScrollBounce()
    {
        _wheelScrollBounceTween?.Kill();
        _wheelScrollBounceTween = null;
        if (Camera != null && GodotObject.IsInstanceValid(Camera))
            Camera.Offset = _cameraBaseOffset;
    }

    private bool HasBlockingOverlay()
    {
        ulong frame = Engine.GetProcessFrames();
        if (_blockingOverlayFrame == frame)
            return _blockingOverlayResult;

        _blockingOverlayFrame = frame;
        _blockingOverlayResult = ComputeBlockingOverlay();
        return _blockingOverlayResult;
    }

    private bool ComputeBlockingOverlay()
    {
        return LayerHasVisibleChildren(SiteUiLayer)
            || LayerHasVisibleChildren(FrontUiLayer)
            || LayerHasVisibleChildren(MenuLayer)
            || HasVisibleRootOverlay("GameOverSummary")
            || HasVisibleRootOverlay("GameStatistics")
            || HasVisibleBattleOverlay()
            || DebugConsoleNode?.IsOpen == true;
    }

    public bool IsMapInteractionBlocked()
    {
        return _mapPeekModeActive || HasBlockingOverlay();
    }

    private bool HasVisibleBattleOverlay()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return false;

        foreach (Node child in root.GetChildren())
        {
            if (child == null || child.IsQueuedForDeletion() || child is not CanvasLayer layer)
                continue;

            foreach (Node layerChild in layer.GetChildren())
            {
                if (layerChild == null || layerChild.IsQueuedForDeletion())
                    continue;

                if (layerChild is Battle battle && battle.IsVisibleInTree())
                    return true;
            }
        }

        return false;
    }

    private static bool LayerHasVisibleChildren(CanvasLayer layer)
    {
        if (layer == null)
            return false;

        foreach (Node child in layer.GetChildren())
        {
            if (child == null || child.IsQueuedForDeletion())
                continue;

            if (child is CanvasLayer canvasLayer)
            {
                if (canvasLayer.Visible)
                    return true;
                continue;
            }

            if (child is CanvasItem canvasItem)
            {
                if (canvasItem.IsVisibleInTree())
                    return true;
                continue;
            }
        }

        return false;
    }

    private void EnsureDebugConsole()
    {
        if (GetNodeOrNull<DebugConsole>("DebugConsole") != null)
            return;

        var console = DebugConsoleScene?.Instantiate<DebugConsole>() ?? new DebugConsole();
        console.Name = "DebugConsole";
        AddChild(console);
    }

    private void UpdateMiniMapIndicator()
    {
        if (
            MiniMapRoot == null
            || MiniMapPreview == null
            || MiniMapPlayerIndicator == null
            || Camera == null
        )
            return;

        Vector2 previewSize = MiniMapPreview.Size;
        if (previewSize.X <= 0.001f || previewSize.Y <= 0.001f)
            return;

        GetCameraHorizontalCenterBoundary(out float minX, out float maxX);

        float progress = 0.5f;
        if (!Mathf.IsEqualApprox(maxX, minX))
            progress = Mathf.Clamp((Camera.GlobalPosition.X - minX) / (maxX - minX), 0.0f, 1.0f);

        Vector2 indicatorSize = MiniMapPlayerIndicator.Size;
        if (indicatorSize.X <= 0.001f || indicatorSize.Y <= 0.001f)
            indicatorSize = new Vector2(8.0f, 8.0f);

        float targetCenterX = progress * previewSize.X;
        float targetCenterY = previewSize.Y * 0.5f;

        Vector2 localPosition = new(
            targetCenterX - indicatorSize.X * 0.5f,
            targetCenterY - indicatorSize.Y * 0.5f
        );

        localPosition.X = Mathf.Clamp(
            localPosition.X,
            0.0f,
            Mathf.Max(0.0f, previewSize.X - indicatorSize.X)
        );
        localPosition.Y = Mathf.Clamp(
            localPosition.Y,
            0.0f,
            Mathf.Max(0.0f, previewSize.Y - indicatorSize.Y)
        );

        MiniMapPlayerIndicator.Position = localPosition;
    }

    private void UpdateRegionLabel()
    {
        if (RegionLabel == null)
            return;

        bool regionTwoUnlocked = GameInfo.IsRegionTwoUnlocked();
        if (_regionLabelInitialized && _lastRegionTwoUnlocked == regionTwoUnlocked)
            return;

        _regionLabelInitialized = true;
        _lastRegionTwoUnlocked = regionTwoUnlocked;
        RegionLabel.Text = I18n.Tr(
            regionTwoUnlocked ? "ui.map.region_2" : "ui.map.region_1",
            regionTwoUnlocked ? "区域 2" : "区域 1"
        );
        RegionLabel.Modulate = regionTwoUnlocked
            ? new Color(1f, 0.88f, 0.38f, 0.96f)
            : new Color(0.7f, 0.92f, 1f, 0.92f);
    }

    private void LocalizeStaticTexts()
    {
        if (TransitionEnergyLabel != null)
            TransitionEnergyLabel.Text = I18n.Tr("ui.common.party_life", "队伍生命");

        if (RelicLabel != null)
            RelicLabel.Text = I18n.Tr("ui.common.relics", "遗物");

        if (ReadyButtonLabel != null)
            ReadyButtonLabel.Text = I18n.Tr("ui.map.tactic", "战术");

        if (NodeLegendTitleLabel != null)
            NodeLegendTitleLabel.Text = I18n.Tr("ui.map.legend_title", "节点图式");

        if (NodeLegendNormalLabel != null)
            NodeLegendNormalLabel.Text = I18n.Tr("ui.map.node_type.normal", "普通战斗");

        if (NodeLegendEventLabel != null)
            NodeLegendEventLabel.Text = I18n.Tr("ui.map.node_type.event", "事件");

        if (NodeLegendShopLabel != null)
            NodeLegendShopLabel.Text = I18n.Tr("ui.map.node_type.shop", "商店");

        if (NodeLegendTreasureLabel != null)
            NodeLegendTreasureLabel.Text = I18n.Tr("ui.map.node_type.treasure", "宝箱");

        if (NodeLegendRestLabel != null)
            NodeLegendRestLabel.Text = I18n.Tr("ui.map.node_type.rest", "休息");

        if (NodeLegendEliteLabel != null)
            NodeLegendEliteLabel.Text = I18n.Tr("ui.map.node_type.elite", "精英");

        if (NodeLegendBossLabel != null)
            NodeLegendBossLabel.Text = I18n.Tr("ui.map.node_type.boss", "首领");
    }

    private void GetCameraHorizontalCenterBoundary(out float minX, out float maxX)
    {
        float left = Mathf.Min(Camera.WorldLeftBoundary, Camera.WorldRightBoundary);
        float right = Mathf.Max(Camera.WorldLeftBoundary, Camera.WorldRightBoundary);
        float halfViewWidth = Mathf.Max(0.0f, Camera.HalfViewportWidth);

        minX = left + halfViewWidth;
        maxX = right - halfViewWidth;

        if (minX > maxX)
        {
            float centerX = (left + right) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }
    }

    private bool HasVisibleRootOverlay(string nodeName)
    {
        var root = GetTree()?.Root;
        if (root == null || string.IsNullOrWhiteSpace(nodeName))
            return false;

        if (root.GetNodeOrNull(nodeName) is not CanvasLayer overlay)
            return false;

        return overlay.Visible && overlay.IsInsideTree();
    }
}
