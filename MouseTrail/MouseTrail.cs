using Godot;
using System;

public partial class MouseTrail : CanvasLayer
{
    Node2D node2d => field ??= GetNode<Node2D>("Node2D");
    public override void _Process(double delta)
    {
        node2d.GlobalPosition = node2d.GetGlobalMousePosition();
    }
}
