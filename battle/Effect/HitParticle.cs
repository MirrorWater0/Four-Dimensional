using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class HitParticle : GpuParticles2D
{
    private const string ScenePath = "res://battle/Effect/HitParticle.tscn";
    private const int MaxPoolSize = 16;
    private const float RecycleDelaySeconds = 1.3f;
    private static PackedScene _scene;
    private static readonly Queue<HitParticle> Pool = new();

    private bool _isInPool;
    private int _playVersion;

    public static HitParticle Spawn(Node parent)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        HitParticle particle = Rent();
        if (particle == null)
            return null;

        parent.AddChild(particle);
        particle.Start();
        return particle;
    }

    public static void Prewarm(int count = 4)
    {
        count = Math.Clamp(count, 0, MaxPoolSize);
        for (int i = Pool.Count; i < count; i++)
        {
            HitParticle particle = CreateInstance();
            if (particle == null)
                return;

            particle.Visible = false;
            particle._isInPool = true;
            Pool.Enqueue(particle);
        }
    }

    private static HitParticle Rent()
    {
        while (Pool.Count > 0)
        {
            HitParticle pooled = Pool.Dequeue();
            if (pooled != null && GodotObject.IsInstanceValid(pooled))
            {
                pooled._isInPool = false;
                return pooled;
            }
        }

        return CreateInstance();
    }

    private static HitParticle CreateInstance()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene?.Instantiate<HitParticle>();
    }

    private async void Start()
    {
        int version = ++_playVersion;
        Visible = true;
        Position = Vector2.Zero;
        Scale = Vector2.One;
        Rotation = 0f;
        OneShot = true;
        Restart();
        Emitting = true;

        SceneTree tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(
            tree.CreateTimer(RecycleDelaySeconds),
            SceneTreeTimer.SignalName.Timeout
        );

        if (version == _playVersion && GodotObject.IsInstanceValid(this))
            RecycleOrFree();
    }

    private void RecycleOrFree()
    {
        if (_isInPool)
            return;

        if (Pool.Count >= MaxPoolSize)
        {
            QueueFree();
            return;
        }

        Emitting = false;
        Visible = false;
        GetParent()?.RemoveChild(this);
        _isInPool = true;
        Pool.Enqueue(this);
    }
}
