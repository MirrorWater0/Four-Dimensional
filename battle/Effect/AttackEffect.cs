using Godot;
using System;
using System.Threading.Tasks;

public partial class AttackEffect : Node2D
{
    public AnimatedSprite2D Sprite1 => field??=GetNode("Effect1") as AnimatedSprite2D;
    public AnimationPlayer AnimationPlayer0 => field??=GetNode("AnimationPlayer") as AnimationPlayer;
    public async override void _Ready()
    {
        await Task.Delay(3000);
        if (IsInstanceValid(this))
        {
            QueueFree();
        }
    }
}
