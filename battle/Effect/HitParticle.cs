using Godot;
using System;
using System.Threading.Tasks;

public partial class HitParticle : GpuParticles2D
{
    public override async void _Ready()
    {
        OneShot = true;
        Emitting = true;
        await ToSignal(GetTree().CreateTimer(1.3f), "timeout");
        QueueFree();
    }
}
