using System;
using System.Threading.Tasks;
using Godot;

public partial class Map : Control
{
    private static readonly PackedScene DebugConsoleScene = GD.Load<PackedScene>(
        "res://Map/DebugConsole.tscn"
    );

    [Export]
    public bool WarmupMode { get; set; }

    public Button DragButton => field ??= GetNode("DragButton") as Button;
    public TextureRect GameMap => field ??= GetNode("GameMap") as TextureRect;
    public DynamicCamera Camera => field ??= GetNode("Camera") as DynamicCamera;
    public Label SeedLabel => field ??= GetNode("UI/SeedLabel") as Label;
    public Label RegionLabel => field ??= GetNodeOrNull<Label>("UI/RegionLabel");
    public Label DifficultyLabel =>
        field ??= GetNodeOrNull<Label>("PlayerResourceState/TransitionEnergyControl/Difficulty");
    private Control MiniMapRoot => field ??= GetNodeOrNull<Control>("UI/MiniMap");
    private TextureRect MiniMapPreview => field ??= GetNodeOrNull<TextureRect>("UI/MiniMap/MapPreview");
    private Control MiniMapPlayerIndicator =>
        field ??= GetNodeOrNull<Control>("UI/MiniMap/MapPreview/PlayerIndicator");
    private LevelProgress LevelProgressNode => field ??= GetNodeOrNull<LevelProgress>("LevelProgress");
    private Control NodeTypeLegend => field ??= GetNodeOrNull<Control>("MapLabel/NodeTypeLegend");

    [Export(PropertyHint.Range, "0,40,1")]
    public float DragStartThreshold = 16.0f;

    [Export]
    public bool SnapCameraToPixel = false;

    [Export(PropertyHint.Range, "1,60,1")]
    public float DragFollowSharpness = 24.0f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float InertiaStrength = 0.18f;

    [Export(PropertyHint.Range, "1,600,1")]
    public float WheelStep = 100.0f;

    [Export(PropertyHint.Range, "1,60,1")]
    public float WheelFollowSharpness = 18.0f;

    private Vector2 _targetPos;
    private bool _isDrag;
    private bool _isDragActive;
    private bool _isWheelPanning;
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
    private bool _regionLabelInitialized;
    private bool _lastRegionTwoUnlocked;
    private ulong _blockingOverlayFrame = ulong.MaxValue;
    private bool _blockingOverlayResult;

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
            Vector2 wheelDesiredTarget = Camera.ClampToBoundary(_targetPos);
            _targetPos = wheelDesiredTarget;
            float alpha = 1.0f - Mathf.Exp(-WheelFollowSharpness * dt);
            SetCameraPosition(Camera.GlobalPosition.Lerp(wheelDesiredTarget, alpha));
            _velocity = Vector2.Zero;

            if (Camera.GlobalPosition.DistanceSquaredTo(wheelDesiredTarget) < 0.25f)
            {
                SetCameraPosition(wheelDesiredTarget);
                _isWheelPanning = false;
            }

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
                ApplyWheelMove(-WheelStep);
                _wheelHandledFrame = Engine.GetProcessFrames();
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ApplyWheelMove(WheelStep);
                _wheelHandledFrame = Engine.GetProcessFrames();
                return;
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

        SeedLabel.Text = $"Seed: {GameInfo.Seed}";
        if (DifficultyLabel != null)
        {
            int difficulty = Math.Clamp(
                GameInfo.Difficulty,
                GameInfo.MinDifficulty,
                GameInfo.MaxDifficulty
            );
            DifficultyLabel.Text = $"难度 {difficulty}";
        }
        UpdateRegionLabel();
        Camera.LimitEnabled = false;
        Camera.PositionSmoothingEnabled = false;
        Camera.Zoom = Vector2.One;
        Camera.HalfViewportWidth = 960.0f;
        Camera.FixedCenterY = 540.0f;
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
        _dragVelocity = Vector2.Zero;
        _velocity = Vector2.Zero;
        _targetPos = Camera.ClampToBoundary(new Vector2(Camera.WorldLeftBoundary, Camera.FixedCenterY));
        SetCameraPosition(_targetPos);
        UpdateMiniMapIndicator();
    }

    private void ApplyWheelMove(float deltaX)
    {
        _targetPos = Camera.ClampToBoundary(_targetPos + new Vector2(deltaX, 0));
        _isWheelPanning = true;
        _velocity = Vector2.Zero;
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
        return HasBlockingOverlay();
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
        if (MiniMapRoot == null || MiniMapPreview == null || MiniMapPlayerIndicator == null || Camera == null)
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

        localPosition.X = Mathf.Clamp(localPosition.X, 0.0f, Mathf.Max(0.0f, previewSize.X - indicatorSize.X));
        localPosition.Y = Mathf.Clamp(localPosition.Y, 0.0f, Mathf.Max(0.0f, previewSize.Y - indicatorSize.Y));

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
        RegionLabel.Text = regionTwoUnlocked ? "区域 2" : "区域 1";
        RegionLabel.Modulate = regionTwoUnlocked
            ? new Color(1f, 0.88f, 0.38f, 0.96f)
            : new Color(0.7f, 0.92f, 1f, 0.92f);
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
