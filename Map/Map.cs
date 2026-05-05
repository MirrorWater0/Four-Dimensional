using System;
using Godot;
using Microsoft.Win32.SafeHandles;

public partial class Map : Control
{
    public Button DragButton => field ??= GetNode("DragButton") as Button;
    public TextureRect GameMap => field ??= GetNode("GameMap") as TextureRect;
<<<<<<< Updated upstream
    public Camera2D Camera => field ??= GetNode("Camera") as Camera2D;
=======
    public DynamicCamera Camera => field ??= GetNode("Camera") as DynamicCamera;
    public Label SeedLabel => field ??= GetNode("UI/SeedLabel") as Label;
    public Label RegionLabel => field ??= GetNodeOrNull<Label>("UI/RegionLabel");
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
>>>>>>> Stashed changes

    private Vector2 _targetPos;
    private bool _isDrag;
    Vector2 _velocity = Vector2.Zero;
    private double _time;

    [Export]
    public Vector2 MinBoundary = Vector2.Zero;

    [Export]
    public Vector2 MaxBoundary = new Vector2(3235, 1970);

    public override void _Process(double delta)
    {
        if (_isDrag)
        {
            _velocity *= 0.9f;
            _velocity += 3000 * (_targetPos - Camera.Position) * (float)delta;
            Camera.GlobalPosition += _velocity * (float)delta;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion & _isDrag)
        {
            var eventmove = @event as InputEventMouseMotion;
            _targetPos -= eventmove.Relative / Camera.Zoom;
            _targetPos.X = Math.Min(2271, _targetPos.X);
            _targetPos.X = Math.Max(966, _targetPos.X);
            _targetPos.Y = Math.Min(1423, _targetPos.Y);
            _targetPos.Y = Math.Max(543, _targetPos.Y);
        }

        if (Input.IsActionPressed("Wheelup"))
        {
            if (1.1 * Camera.Zoom.X < 2)
                Camera.Zoom = 1.1f * Camera.Zoom;
        }

        if (Input.IsActionPressed("Wheeldown"))
        {
            if (Camera.Zoom.X > 0.8)
                Camera.Zoom = 0.9f * Camera.Zoom;
        }
    }

    public override void _Ready()
    {
        _targetPos = Camera.Position;
        DragButton.ButtonDown += () =>
        {
            _isDrag = true;
            _velocity = Vector2.Zero;
            _targetPos = Camera.GlobalPosition;
        };
        DragButton.ButtonUp += () =>
        {
            _isDrag = false;
        };
<<<<<<< Updated upstream
=======
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
>>>>>>> Stashed changes
    }

    public void CloseWindow()
    {
        GetTree().Quit();
    }
<<<<<<< Updated upstream
=======

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
        if (EquipmentButtonNode != null)
            tasks.Add(EquipmentButtonNode.CloseCurrentUiAsync());

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

    private void ApplyWheelMove(float deltaX)
    {
        _targetPos = Camera.ClampToBoundary(_targetPos + new Vector2(deltaX, 0));
        _isWheelPanning = true;
        _velocity = Vector2.Zero;
    }

    private bool HasBlockingOverlay()
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
>>>>>>> Stashed changes
}
