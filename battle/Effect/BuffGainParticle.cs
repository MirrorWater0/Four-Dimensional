using Godot;
using System;
using System.Collections.Generic;

public partial class BuffGainParticle : Node2D
{
    private const string ScenePath = "res://battle/Effect/BuffGainParticle.tscn";
    private const int MaxPoolSize = 16;
    private static PackedScene _scene;
    private static readonly Queue<BuffGainParticle> Pool = new();

    [Export(PropertyHint.Range, "0.1,3,0.05")]
    public float LifetimeSeconds { get; set; } = 1.45f;

    private bool _isInPool;
    private int _playVersion;

    public static BuffGainParticle Spawn(Node parent)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        BuffGainParticle particle = Rent();
        if (particle == null)
            return null;

        parent.AddChild(particle);
        particle.Play();
        return particle;
    }

    public static void Prewarm(int count = 4)
    {
        count = Math.Clamp(count, 0, MaxPoolSize);
        for (int i = Pool.Count; i < count; i++)
        {
            BuffGainParticle particle = CreateInstance();
            if (particle == null)
                return;

            particle.Visible = false;
            particle._isInPool = true;
            Pool.Enqueue(particle);
        }
    }

    private static BuffGainParticle Rent()
    {
        while (Pool.Count > 0)
        {
            BuffGainParticle pooled = Pool.Dequeue();
            if (pooled != null && GodotObject.IsInstanceValid(pooled))
            {
                pooled._isInPool = false;
                return pooled;
            }
        }

        return CreateInstance();
    }

    private static BuffGainParticle CreateInstance()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene?.Instantiate<BuffGainParticle>();
    }

    private async void Play()
    {
        int version = ++_playVersion;
        Visible = true;
        Position = Vector2.Zero;
        Scale = Vector2.One;
        Rotation = 0f;
        StartParticles(this);

        var tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(tree.CreateTimer(LifetimeSeconds), SceneTreeTimer.SignalName.Timeout);

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

        StopParticles(this);
        Visible = false;
        GetParent()?.RemoveChild(this);
        _isInPool = true;
        Pool.Enqueue(this);
    }

    private static void StartParticles(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is GpuParticles2D particles)
            {
                particles.OneShot = true;
                particles.Restart();
                particles.Emitting = true;
            }

            StartParticles(child);
        }
    }

    private static void StopParticles(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is GpuParticles2D particles)
                particles.Emitting = false;

            StopParticles(child);
        }
    }
}
