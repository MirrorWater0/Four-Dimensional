using Godot;
using System;
using System.Collections.Generic;

public partial class CharacterEffect : Node2D
{
    private const string ScenePath = "res://battle/Effect/CharacterEffect.tscn";
    private const int MaxPoolSize = 18;
    private static PackedScene _scene;
    private static readonly Queue<CharacterEffect> Pool = new();

    public AnimationPlayer Animation => field ??= GetNode<AnimationPlayer>("AnimationPlayer");

    private bool _isInPool;
    private bool _recycling;

    public override void _Ready()
    {
        Animation.AnimationFinished += OnAnimationFinished;
    }

    public static CharacterEffect Spawn(
        Node parent,
        string animationName,
        Vector2? position = null
    )
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        CharacterEffect effect = Rent();
        if (effect == null)
            return null;

        parent.AddChild(effect);
        effect.Position = position ?? Vector2.Zero;
        effect.Play(animationName);
        return effect;
    }

    public static void Prewarm(Node owner, int count = 3)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        count = Math.Clamp(count, 0, MaxPoolSize);
        for (int i = Pool.Count; i < count; i++)
        {
            CharacterEffect effect = CreateInstance();
            if (effect == null)
                return;

            effect.Visible = false;
            effect._isInPool = true;
            Pool.Enqueue(effect);
        }
    }

    private static CharacterEffect Rent()
    {
        while (Pool.Count > 0)
        {
            CharacterEffect pooled = Pool.Dequeue();
            if (pooled != null && GodotObject.IsInstanceValid(pooled))
            {
                pooled._isInPool = false;
                return pooled;
            }
        }

        return CreateInstance();
    }

    private static CharacterEffect CreateInstance()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene?.Instantiate<CharacterEffect>();
    }

    public void Play(string animationName)
    {
        _recycling = false;
        Visible = true;
        Modulate = Colors.White;
        Scale = Vector2.One;
        Rotation = 0f;
        if (Animation == null)
            return;

        Animation.Stop();
        Animation.Play(animationName);
    }

    private void OnAnimationFinished(StringName animationName)
    {
        RecycleOrFree();
    }

    private void RecycleOrFree()
    {
        if (_recycling || _isInPool)
            return;

        _recycling = true;
        if (Pool.Count >= MaxPoolSize)
        {
            QueueFree();
            return;
        }

        Animation?.Stop();
        Visible = false;
        GetParent()?.RemoveChild(this);
        _isInPool = true;
        _recycling = false;
        Pool.Enqueue(this);
    }
}
