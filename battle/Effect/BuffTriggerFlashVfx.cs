using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;

public partial class BuffTriggerFlashVfx : Node2D
{
    private const float IconSize = 112f;
    private const float VerticalOffset = -36f;
    private const string ScenePath = "res://battle/Effect/BuffTriggerFlashVfx.tscn";
    private const int MaxPoolSize = 14;
    private static PackedScene _scene;
    private static readonly Queue<BuffTriggerFlashVfx> Pool = new();

    private Buff _buff;
    private Control _icon;
    private Tween _tween;
    private bool _isInPool;
    private bool _recycling;

    public void Initialize(Buff buff) => _buff = buff;

    public static BuffTriggerFlashVfx Spawn(Buff buff, Node parent)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        BuffTriggerFlashVfx vfx = Rent();
        if (vfx == null)
            return null;

        vfx.Initialize(buff);
        parent.AddChild(vfx);
        vfx.Play();
        return vfx;
    }

    public static void Prewarm(int count = 3)
    {
        count = System.Math.Clamp(count, 0, MaxPoolSize);
        for (int i = Pool.Count; i < count; i++)
        {
            BuffTriggerFlashVfx vfx = CreateInstance();
            if (vfx == null)
                return;

            vfx.Visible = false;
            vfx._isInPool = true;
            Pool.Enqueue(vfx);
        }
    }

    private static BuffTriggerFlashVfx Rent()
    {
        while (Pool.Count > 0)
        {
            BuffTriggerFlashVfx pooled = Pool.Dequeue();
            if (pooled != null && GodotObject.IsInstanceValid(pooled))
            {
                pooled._isInPool = false;
                return pooled;
            }
        }

        return CreateInstance();
    }

    private static BuffTriggerFlashVfx CreateInstance()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene?.Instantiate<BuffTriggerFlashVfx>();
    }

    private void Play()
    {
        _recycling = false;
        Visible = true;
        Position = Vector2.Zero;
        Scale = Vector2.One;
        Rotation = 0f;
        ClearIcon();

        if (
            _buff == null
            || _buff.Owner == null
            || !GodotObject.IsInstanceValid(_buff.Owner)
            || _buff.Owner.State == Character.CharacterState.Dying
        )
        {
            RecycleOrFree();
            return;
        }

        _icon = Buff.CreateBuffTooltipIcon(_buff.ThisBuffName);
        if (_icon == null)
        {
            RecycleOrFree();
            return;
        }

        _icon.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _icon.Size = new Vector2(IconSize, IconSize);
        _icon.PivotOffset = new Vector2(IconSize / 2f, IconSize / 2f);
        _icon.Position = new Vector2(-IconSize / 2f, -IconSize / 2f + VerticalOffset);
        _icon.Modulate = new Color(1f, 1f, 1f, 0.7f);
        Buff.RefreshTextureIconOverrideLayout(_icon);
        AddChild(_icon);
        _ = PlayAsync();
    }

    public override void _ExitTree() => _tween?.Kill();

    private async Task PlayAsync()
    {
        _tween = CreateTween();
        _tween.SetParallel();
        _tween
            .TweenProperty(_icon, "scale", Vector2.One * 1.62f, 0.34)
            .From(Vector2.One * 0.70f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        _tween
            .TweenProperty(_icon, "modulate:a", 1f, 0.22)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _tween.SetParallel(false);
        _tween
            .TweenProperty(_icon, "scale", Vector2.One * 1.85f, 0.24)
            .SetEase(Tween.EaseType.Out);
        _tween
            .TweenProperty(_icon, "modulate:a", 0f, 0.32)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(_tween, Tween.SignalName.Finished);
        RecycleOrFree();
    }

    private void RecycleOrFree()
    {
        if (_recycling || _isInPool)
            return;

        _recycling = true;
        _tween?.Kill();
        _tween = null;
        ClearIcon();
        _buff = null;

        if (Pool.Count >= MaxPoolSize)
        {
            QueueFree();
            return;
        }

        Visible = false;
        GetParent()?.RemoveChild(this);
        _isInPool = true;
        _recycling = false;
        Pool.Enqueue(this);
    }

    private void ClearIcon()
    {
        if (_icon != null && GodotObject.IsInstanceValid(_icon))
        {
            RemoveChild(_icon);
            _icon.QueueFree();
        }

        _icon = null;
    }
}
