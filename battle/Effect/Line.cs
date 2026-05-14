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

    [Export]
    public Vector2 Offset = new Vector2(0, 0);

    public override void _Ready() { }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (ManualPreviewMode)
            return;

        if (Target == null || !GodotObject.IsInstanceValid(Target))
            return;

        GlobalPosition = Vector2.Zero;
        AddPoint(ToLocal(Target.GlobalPosition) + Offset);
        if (GetPointCount() > TrailLength)
        {
            RemovePoint(0);
        }
    }
}
