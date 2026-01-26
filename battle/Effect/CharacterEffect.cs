using Godot;
using System;

public partial class CharacterEffect : Node2D
{
    public AnimationPlayer Animation => field ??= GetNode<AnimationPlayer>("AnimationPlayer");
    public override async void _Ready()
    {
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        await ToSignal(Animation, "animation_finished");
        QueueFree();
    }
}
