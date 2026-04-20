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
    ColorRect BlackMask => field ??= GetNode<ColorRect>("/root/Map/MaskLayer/Mask");
    private CanvasLayer SiteUiLayer => field ??= GetNodeOrNull<CanvasLayer>("SiteUI");
    private CanvasLayer FrontUiLayer => field ??= GetNodeOrNull<CanvasLayer>("BattleReadyLayer");
    private ReadyButton ReadyButtonNode => field ??= GetNodeOrNull<ReadyButton>("UI/ReadyButton");
    private EquipmentButton EquipmentButtonNode =>
        field ??= GetNodeOrNull<EquipmentButton>("UI/EquipmentButton");
    private DebugConsole DebugConsoleNode => field ??= GetNodeOrNull<DebugConsole>("DebugConsole");
    public PlayerResourceState PlayerResourceState =>
        field ??= GetNode<PlayerResourceState>("PlayerResourceState");

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        ulong frame = Engine.GetProcessFrames();

        if (HasBlockingOverlay())
        {
            _isDrag = false;
            _isDragActive = false;
            _isWheelPanning = false;
            _dragVelocity = Vector2.Zero;
            _velocity = Vector2.Zero;
            _targetPos = Camera.ClampToBoundary(Camera.GlobalPosition);
            _wheelHandledFrame = frame;
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
        Camera.LimitEnabled = false;
        Camera.PositionSmoothingEnabled = false;
        Camera.Zoom = Vector2.One;
        Camera.HalfViewportWidth = 960.0f;
        Camera.FixedCenterY = 540.0f;
        DragButton.Disabled = false;
        _targetPos = Camera.ClampToBoundary(Camera.GlobalPosition);
        SetCameraPosition(_targetPos);
        BlackMask.Modulate = new Color(1, 1, 1, 0);
        EnsureDebugConsole();
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

    public void CloseWindow()
    {
        GetTree().Quit();
    }

    public Tween BlackMaskAnimation(float duration)
    {
        BlackMask.Visible = true;
        var tween = CreateTween();
        tween.TweenProperty(BlackMask, "modulate:a", 1, duration);
        tween.Chain().TweenProperty(BlackMask, "modulate:a", 0, duration);
        tween.TweenCallback(Callable.From(() => BlackMask.Visible = false));
        return tween;
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

            if (child is CanvasItem canvasItem)
            {
                if (canvasItem.Visible)
                    return true;
                continue;
            }

            return true;
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
}
