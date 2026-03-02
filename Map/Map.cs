using System;
using Godot;

public partial class Map : Control
{
    public Button DragButton => field ??= GetNode("DragButton") as Button;
    public TextureRect GameMap => field ??= GetNode("GameMap") as TextureRect;
    public DynamicCamera Camera => field ??= GetNode("Camera") as DynamicCamera;
    public Label SeedLabel => field ??= GetNode("UI/SeedLabel") as Label;

    [Export(PropertyHint.Range, "0,40,1")]
    public float DragStartThreshold = 16.0f;

    private Vector2 _targetPos;
    private bool _isDrag;
    private bool _isDragActive;
    private Vector2 _dragStartMousePos = Vector2.Zero;
    Vector2 _velocity = Vector2.Zero;
    private Vector2 _dragVelocity = Vector2.Zero;
    ColorRect BlackMask => field ??= GetNode<ColorRect>("/root/Map/MaskLayer/Mask");
    public PlayerResourceState PlayerResourceState =>
        field ??= GetNode<PlayerResourceState>("PlayerResourceState");

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_isDragActive)
        {
            Camera.GlobalPosition = _targetPos;
            _velocity = Vector2.Zero;
            return;
        }

        bool isTargetOutOfBoundary = !Camera.IsInsideBoundary(_targetPos);
        Vector2 desiredTarget = isTargetOutOfBoundary ? Camera.ClampToBoundary(_targetPos) : _targetPos;

        if (isTargetOutOfBoundary)
        {
            _targetPos = desiredTarget;
        }

        _velocity *= Camera.VelocityDamping;

        if (isTargetOutOfBoundary)
        {
            _velocity += Camera.FollowStrength * (desiredTarget - Camera.GlobalPosition) * dt;
        }

        Camera.GlobalPosition += _velocity * dt;

        if (!_isDrag
            && isTargetOutOfBoundary
            && _velocity.LengthSquared() < 0.25f
            && Camera.GlobalPosition.DistanceSquaredTo(desiredTarget) < 0.25f)
        {
            Camera.GlobalPosition = desiredTarget;
            _velocity = Vector2.Zero;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && _isDrag)
        {
            var eventmove = (InputEventMouseMotion)@event;

            if (!_isDragActive)
            {
                if (eventmove.Position.DistanceTo(_dragStartMousePos) < DragStartThreshold)
                {
                    return;
                }

                _isDragActive = true;
                _dragVelocity = Vector2.Zero;
                return;
            }

            Vector2 nextTarget = _targetPos - eventmove.Relative / Camera.Zoom;
            _targetPos = Camera.ApplyBoundaryResistance(nextTarget);

            Vector2 worldDragVelocity = -eventmove.Velocity / Camera.Zoom;
            _dragVelocity = _dragVelocity.Lerp(worldDragVelocity, 0.35f);
        }

        if (Input.IsActionPressed("Wheelup"))
        {
            _targetPos = Camera.ApplyBoundaryResistance(_targetPos - new Vector2(100, 0));
        }

        if (Input.IsActionPressed("Wheeldown"))
        {
            _targetPos = Camera.ApplyBoundaryResistance(_targetPos + new Vector2(100, 0));
        }
    }

    public override void _Ready()
    {
        SeedLabel.Text = $"Seed: {GameInfo.Seed}";
        Camera.PositionSmoothingEnabled = false;
        DragButton.Disabled = false;
        _targetPos = Camera.GlobalPosition;
        BlackMask.Modulate = new Color(1, 1, 1, 0);
        DragButton.ButtonDown += () =>
        {
            _isDrag = true;
            _isDragActive = false;
            _dragStartMousePos = GetViewport().GetMousePosition();
            _velocity = Vector2.Zero;
            _dragVelocity = Vector2.Zero;
            _targetPos = Camera.GlobalPosition;
        };
        DragButton.ButtonUp += () =>
        {
            _isDrag = false;
            _isDragActive = false;
            Vector2 releaseVelocity = _dragVelocity * Camera.ReleaseInertiaMultiplier;
            _velocity = releaseVelocity.LimitLength(Camera.MaxReleaseSpeed);

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
}
