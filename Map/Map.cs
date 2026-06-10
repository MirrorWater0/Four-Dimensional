using System;
using System.Collections.Generic;
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
    public Label RegionLabel =>
        field ??=
            GetNodeOrNull<Label>("MapLabel/RegionLabel") ?? GetNodeOrNull<Label>("UI/RegionLabel");
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
    public bool IsMapPeekModeActive => _mapPeekModeActive;
    private bool _regionLabelInitialized;
    private bool _lastRegionTwoUnlocked;
    private ulong _blockingOverlayFrame = ulong.MaxValue;
    private bool _blockingOverlayResult;
    private bool _mapPeekModeActive;
    private readonly List<VisibilitySnapshot> _mapPeekHiddenNodes = new();

    private sealed class VisibilitySnapshot
    {
        public Node Node;
        public bool Visible;
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

        LocalizeStaticTexts();
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

        CallDeferred(nameof(ShowPendingBossRelicChoiceIfNeeded));
    }

    public override void _ExitTree()
    {
        ExitMapPeekMode();
    }

    private void ShowPendingBossRelicChoiceIfNeeded()
    {
        if (WarmupMode || !BossRelicChoice.ShouldShowPendingChoice())
            return;

        BossRelicChoice.Show(this);
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
        _blockingOverlayFrame = ulong.MaxValue;
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
