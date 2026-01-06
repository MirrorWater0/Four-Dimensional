using Godot;
using System;

public partial class Tip : Control
{
    public ColorRect bg => field??=GetNode<ColorRect>("bg");
    public Label Description => field??=GetNode<Label>("Description");
    public override void _Process(double delta)
    {
        base._Process(delta);
        bg.Size = Description.Size;
    }

}
