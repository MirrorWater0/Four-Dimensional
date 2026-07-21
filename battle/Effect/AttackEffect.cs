using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class AttackEffect : Node2D
{
    private const string ScenePath = "res://battle/Effect/AttackEffect.tscn";
    private const int MaxPoolSize = 10;
    private static PackedScene _scene;
    private static readonly Queue<AttackEffect> Pool = new();

    public AnimatedSprite2D Sprite1 => field ??= GetNode("Effect1") as AnimatedSprite2D;
    public AnimationPlayer AnimationPlayer0 =>
        field ??= GetNode("AnimationPlayer") as AnimationPlayer;

    private bool _isInPool;
    private bool _recycling;

    public override void _Ready()
    {
        AnimationPlayer0.AnimationFinished += OnAnimationFinished;
    }

    public static AttackEffect Spawn(Node parent)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        AttackEffect effect = Rent();
        if (effect == null)
            return null;

        parent.AddChild(effect);
        effect.ResetForPlay();
        return effect;
    }

    public static void Prewarm(int count = 2)
    {
        count = Math.Clamp(count, 0, MaxPoolSize);
        for (int i = Pool.Count; i < count; i++)
        {
            AttackEffect effect = CreateInstance();
            if (effect == null)
                return;

            effect.Visible = false;
            effect._isInPool = true;
            Pool.Enqueue(effect);
        }
    }

    private static AttackEffect Rent()
    {
        while (Pool.Count > 0)
        {
            AttackEffect pooled = Pool.Dequeue();
            if (pooled != null && GodotObject.IsInstanceValid(pooled))
            {
                pooled._isInPool = false;
                return pooled;
            }
        }

        return CreateInstance();
    }

    private static AttackEffect CreateInstance()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene?.Instantiate<AttackEffect>();
    }

    public void PlayAttack()
    {
        Visible = true;
        AnimationPlayer0?.Stop();
        AnimationPlayer0?.Play("Attack1");
    }

    private void ResetForPlay()
    {
        _recycling = false;
        Visible = true;
        Position = Vector2.Zero;
        GlobalPosition = Vector2.Zero;
        Scale = Vector2.One;
        Rotation = 0f;
        Modulate = Colors.White;
        if (Sprite1 != null)
        {
            Sprite1.Frame = 0;
            Sprite1.SelfModulate = new Color(1f, 1f, 1f, 0f);
        }
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

        AnimationPlayer0?.Stop();
        Visible = false;
        GetParent()?.RemoveChild(this);
        _isInPool = true;
        _recycling = false;
        Pool.Enqueue(this);
    }
}
