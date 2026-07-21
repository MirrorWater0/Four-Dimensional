using System;
using Godot;

public partial class Line : Line2D
{
    [Export]
    public Node2D Target;

    [Export]
    public bool ManualPreviewMode;

    [Export]
    public int TrailLength = 30;

    [Export(PropertyHint.Range, "0.1,8.0,0.1")]
    public float MinPointDistance = 0.75f;

    [Export]
    public Vector2 Offset = new Vector2(0, 0);

    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (ManualPreviewMode)
            return;

        if (
            !IsVisibleInTree()
            || Target == null
            || !GodotObject.IsInstanceValid(Target)
        )
        {
            return;
        }

        GlobalPosition = Vector2.Zero;
        Vector2 targetPoint = ToLocal(Target.GlobalPosition) + Offset;
        int pointCount = GetPointCount();
        if (
            pointCount > 0
            && GetPointPosition(pointCount - 1).DistanceSquaredTo(targetPoint)
                < MinPointDistance * MinPointDistance
        )
        {
            // Let an existing trail collapse after movement, then stop rebuilding its mesh
            // while the target is stationary.
            if (pointCount > 1)
                RemovePoint(0);
            return;
        }

        AddPoint(targetPoint);
        if (GetPointCount() > TrailLength)
            RemovePoint(0);
    }
}
