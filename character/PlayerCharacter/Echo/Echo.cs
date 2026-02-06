using System;
using Godot;

public partial class Echo : PlayerCharacter
{
    public override PackedScene CharaterScene { get; set; } = ChoseCharater._Echo;
    Label label => field ??= GetNode<Label>("Label");
    public override string CharaterName { get; set; } = "Echo";

    public override void _Ready()
    {
        base._Ready();
        label.Text = PositionIndex.ToString();
    }

    public override void StartAction()
    {
        base.StartAction();
    }
}
