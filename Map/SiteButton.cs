using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class SiteButton : Button
{
    [Export]
    public PackedScene Where;
    public ColorRect Mask => field ??= GetNodeOrNull<ColorRect>("/root/Map/UI/ColorRect");
    public ColorRect Appearance => field ??= GetNodeOrNull<ColorRect>("Appearance");
    public CanvasLayer SiteUILayer => field ??= GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI");
    private ShaderMaterial mat;
    private readonly Dictionary<string, Tween> _shaderTweens = new();
    private Tween _motionTween;
    private Tween _maskTween;
    private bool _isOpening;

    public override void _Ready()
    {
        PivotOffset = Size * 0.5f;
        if (Appearance?.Material is ShaderMaterial shader)
        {
            mat = shader.Duplicate() as ShaderMaterial;
            mat.ResourceLocalToScene = true;
            Appearance.Material = mat;
        }

        ButtonDown += GotoSite;
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        mat?.SetShaderParameter("inner_ring_radius", 0.028f);
        mat?.SetShaderParameter("broken_ring_radius", 0.14f);
        base._Ready();
    }

    public async void GotoSite()
    {
        if (_isOpening || Where == null || SiteUILayer == null)
            return;

        _isOpening = true;
        Disabled = true;
        await PlayOpenPulseAsync();
        if (!GodotObject.IsInstanceValid(this) || SiteUILayer == null)
            return;

        var node = Where.Instantiate() as Node;
        if (node != null)
            SiteUILayer.AddChild(node);
    }

    public void mouse_entered()
    {
        if (_isOpening)
            return;

        TweenShader("inner_ring_radius", 0.08f);
        TweenShader("broken_ring_radius", 0.18f);
        TweenMotion(new Vector2(1.08f, 1.08f), new Color(1.22f, 1.22f, 1.22f, 1f), 0.12f);
    }

    public void mouse_exited()
    {
        if (_isOpening)
            return;

        TweenShader("inner_ring_radius", 0.028f);
        TweenShader("broken_ring_radius", 0.14f);
        TweenMotion(Vector2.One, Colors.White, 0.13f);
    }

    public void TweenShader(string var, float val)
    {
        if (mat == null)
            return;

        if (_shaderTweens.TryGetValue(var, out Tween running) && GodotObject.IsInstanceValid(running))
            running.Kill();

        Tween tween = CreateTween();
        _shaderTweens[var] = tween;
        tween.TweenMethod(
            Callable.From<float>(value => mat.SetShaderParameter(var, value)),
            GetShaderParameterFloat(var),
            val,
            0.18f
        )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.Finished += () => _shaderTweens.Remove(var);
    }

    private void TweenMotion(Vector2 targetScale, Color targetModulate, float duration)
    {
        _motionTween?.Kill();
        _motionTween = CreateTween();
        _motionTween.SetParallel(true);
        _motionTween
            .TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _motionTween
            .TweenProperty(this, "modulate", targetModulate, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private async Task PlayOpenPulseAsync()
    {
        _motionTween?.Kill();
        TweenShader("inner_ring_radius", 0.11f);
        TweenShader("broken_ring_radius", 0.23f);

        if (Mask != null && GodotObject.IsInstanceValid(Mask))
        {
            _maskTween?.Kill();
            _maskTween = CreateTween();
            _maskTween.TweenProperty(Mask, "color", new Color(0.72f, 0.96f, 1f, 0.18f), 0.055f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _maskTween.TweenProperty(Mask, "color", new Color(0, 0, 0, 0), 0.14f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        _motionTween = CreateTween();
        _motionTween.SetParallel(false);
        _motionTween
            .TweenProperty(this, "scale", new Vector2(0.9f, 0.9f), 0.05f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _motionTween
            .TweenProperty(this, "scale", new Vector2(1.08f, 1.08f), 0.08f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _motionTween
            .TweenProperty(this, "scale", Vector2.One, 0.06f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(_motionTween, Tween.SignalName.Finished);
    }

    private float GetShaderParameterFloat(string parameterName)
    {
        if (mat == null)
            return 0f;

        Variant value = mat.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }
}
