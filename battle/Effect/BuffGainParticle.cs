using Godot;

public partial class BuffGainParticle : Node2D
{
    [Export(PropertyHint.Range, "0.1,3,0.05")]
    public float LifetimeSeconds { get; set; } = 1.0f;

    public override async void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is not GpuParticles2D particles)
                continue;

            particles.OneShot = true;
            particles.Restart();
            particles.Emitting = true;
        }

        await ToSignal(GetTree().CreateTimer(LifetimeSeconds), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
