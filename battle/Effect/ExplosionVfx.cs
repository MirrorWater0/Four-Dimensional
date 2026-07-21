using Godot;

public partial class ExplosionVfx : Node2D
{
    [Export(PropertyHint.Range, "0.2,3,0.05")]
    public float LifetimeSeconds { get; set; } = 1.05f;

    public override async void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is AnimatedSprite2D sprite)
            {
                sprite.Play();
                continue;
            }

            if (child is not GpuParticles2D particles)
                continue;

            particles.OneShot = true;
            particles.Restart();
            particles.Emitting = true;
        }

        var tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(tree.CreateTimer(LifetimeSeconds), SceneTreeTimer.SignalName.Timeout);

        if (GodotObject.IsInstanceValid(this))
            QueueFree();
    }
}
