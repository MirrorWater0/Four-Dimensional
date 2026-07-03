using System;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// StS2-inspired card morph: white flash hides the model swap, then border glow + bounce reveal.
/// </summary>
public partial class CardTransformShineVfx : Control
{
    private const string ScenePath = "res://battle/Effect/CardTransformShineVfx.tscn";

    private static readonly Vector2 CardBaseSize = new(240f, 370f);
    private static readonly Color WhiteOpaque = new(1f, 1f, 1f, 1f);
    private static readonly Color WhiteClear = new(1f, 1f, 1f, 0f);

    private static PackedScene _scene;

    private ColorRect _whiteOverlay;
    private Panel _borderGlow;
    private GpuParticles2D _revealParticles;
    private GpuParticles2D _shineParticles;
    private GpuParticles2D _endParticles;
    private Tween _tween;

    public static void Prewarm() => _ = GetScene();

    public static async Task PlayOnCardAsync(
        SkillCard card,
        Action onSwap,
        bool shortVersion = false
    )
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return;

        PackedScene scene = GetScene();
        if (scene == null)
        {
            onSwap?.Invoke();
            return;
        }

        var vfx = scene.Instantiate<CardTransformShineVfx>();
        card.AddChild(vfx);
        vfx.SetAnchorsPreset(LayoutPreset.FullRect);
        vfx.SetOffsetsPreset(LayoutPreset.FullRect);
        vfx.ZIndex = 120;
        vfx.MouseFilter = MouseFilterEnum.Ignore;
        await vfx.PlayAsync(card, onSwap, shortVersion);
        if (GodotObject.IsInstanceValid(vfx))
            vfx.QueueFree();
    }

    private static PackedScene GetScene()
    {
        _scene ??= GD.Load<PackedScene>(ScenePath);
        return _scene;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        _whiteOverlay = GetNodeOrNull<ColorRect>("WhiteOverlay");
        _borderGlow = GetNodeOrNull<Panel>("BorderGlow");
        _revealParticles = GetNodeOrNull<GpuParticles2D>("RevealParticles");
        _shineParticles = GetNodeOrNull<GpuParticles2D>("ShineParticles");
        _endParticles = GetNodeOrNull<GpuParticles2D>("EndParticles");
    }

    public override void _ExitTree() => _tween?.Kill();

    public async Task PlayAsync(SkillCard card, Action onSwap, bool shortVersion)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        float overlayShow = shortVersion ? 0.15f : 0.25f;
        float overlayIdle = shortVersion ? 0.075f : 0.125f;
        float overlayHide = 0.1f;
        float glowFade = 0.25f;
        float glowTopScale = 1.35f;
        float shineDelay = 0.1f;
        float endParticlesDelay = 0.165f;

        Vector2 baseScale = card.Scale;
        ResetVisuals();

        Task anticipationScale = AnimateCardScaleAsync(
            card,
            baseScale,
            SampleAnticipationScale,
            overlayShow + overlayIdle
        );

        _tween = CreateTween();
        _tween.TweenProperty(_whiteOverlay, "modulate", WhiteOpaque, overlayShow)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Quart);
        await anticipationScale;

        onSwap?.Invoke();

        Task revealScale = AnimateCardScaleAsync(
            card,
            baseScale,
            SampleRevealScale,
            glowFade
        );

        if (_borderGlow != null)
        {
            _borderGlow.Modulate = WhiteOpaque;
            _borderGlow.Scale = Vector2.One;
            _borderGlow.PivotOffset = _borderGlow.Size * 0.5f;
        }

        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(_whiteOverlay, "modulate", WhiteClear, overlayHide)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quart);
        if (_borderGlow != null)
        {
            _tween.TweenProperty(_borderGlow, "scale", Vector2.One * glowTopScale, glowFade)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quint);
            _tween.TweenProperty(_borderGlow, "modulate", WhiteClear, glowFade)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quint);
        }

        RestartParticles(_revealParticles);
        await WaitSeconds(shineDelay);
        RestartParticles(_shineParticles);
        await WaitSeconds(endParticlesDelay);
        RestartParticles(_endParticles);

        await revealScale;
        if (GodotObject.IsInstanceValid(card))
            card.Scale = baseScale;
    }

    private void ResetVisuals()
    {
        if (_whiteOverlay != null)
            _whiteOverlay.Modulate = WhiteClear;
        if (_borderGlow != null)
        {
            _borderGlow.Modulate = WhiteClear;
            _borderGlow.Scale = Vector2.One;
        }
    }

    private static void RestartParticles(GpuParticles2D particles)
    {
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Restart();
        particles.Emitting = true;
    }

    private async Task WaitSeconds(float seconds)
    {
        if (seconds <= 0f)
            return;

        SceneTree tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private static async Task AnimateCardScaleAsync(
        SkillCard card,
        Vector2 baseScale,
        Func<float, Vector2> sampleScale,
        float duration
    )
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || duration <= 0f)
            return;

        float elapsed = 0f;
        SceneTree tree = card.GetTree();
        ulong startUsec = Time.GetTicksUsec();
        while (elapsed < duration && GodotObject.IsInstanceValid(card))
        {
            float t = Mathf.Clamp(elapsed / duration, 0f, 1f);
            Vector2 multiplier = sampleScale(t);
            card.Scale = new Vector2(baseScale.X * multiplier.X, baseScale.Y * multiplier.Y);
            if (tree == null)
                break;

            await card.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            elapsed = (Time.GetTicksUsec() - startUsec) / 1_000_000f;
        }

        if (GodotObject.IsInstanceValid(card))
        {
            Vector2 finalMultiplier = sampleScale(1f);
            card.Scale = new Vector2(
                baseScale.X * finalMultiplier.X,
                baseScale.Y * finalMultiplier.Y
            );
        }
    }

    private static Vector2 SampleAnticipationScale(float t)
    {
        float x = Mathf.Lerp(1f, 1.12f, EaseInOut(t));
        float y = Mathf.Lerp(1f, 0.87f, EaseInOut(t));
        return new Vector2(x, y);
    }

    private static Vector2 SampleRevealScale(float t)
    {
        if (t <= 0.33f)
        {
            float local = t / 0.33f;
            return new Vector2(
                Mathf.Lerp(0.98f, 1.1f, EaseOut(local)),
                Mathf.Lerp(0.98f, 0.85f, EaseOut(local))
            );
        }

        if (t <= 0.66f)
        {
            float local = (t - 0.33f) / 0.33f;
            return new Vector2(
                Mathf.Lerp(1.1f, 0.95f, EaseInOut(local)),
                Mathf.Lerp(0.85f, 1.05f, EaseInOut(local))
            );
        }

        float tail = (t - 0.66f) / 0.34f;
        return new Vector2(
            Mathf.Lerp(0.95f, 1f, EaseOut(tail)),
            Mathf.Lerp(1.05f, 1f, EaseOut(tail))
        );
    }

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
}
