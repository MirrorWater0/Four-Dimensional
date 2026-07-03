using Godot;

public partial class CharacterFootMarker : ColorRect
{
    private const float HoverTweenDuration = 0.16f;
    private static readonly Color FrameColor = new(0.88f, 0.84f, 0.76f, 1f);
    private static readonly Vector2 HoverScale = new(1.04f, 1.08f);

    private ShaderMaterial _material;
    private Tween _hoverTween;
    private float _hoverAmount;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        EnsureMaterial();
    }

    public void ApplyCharacterColor(Color color)
    {
        EnsureMaterial();
        _material?.SetShaderParameter("fill_color", color);
        _material?.SetShaderParameter("frame_color", FrameColor);
        _material?.SetShaderParameter("hover_amount", _hoverAmount);
    }

    public void SetCardHoverHighlight(bool active, bool instant = false)
    {
        EnsureMaterial();
        if (_material == null)
            return;

        float target = active ? 1f : 0f;
        _hoverTween?.Kill();
        if (instant || !IsInsideTree())
        {
            _hoverAmount = target;
            _material.SetShaderParameter("hover_amount", target);
            Scale = active ? HoverScale : Vector2.One;
            return;
        }

        Vector2 startScale = Scale;
        Vector2 targetScale = active ? HoverScale : Vector2.One;
        _hoverTween = CreateTween();
        _hoverTween.SetParallel(true);
        _hoverTween
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    _hoverAmount = value;
                    _material.SetShaderParameter("hover_amount", value);
                }),
                _hoverAmount,
                target,
                HoverTweenDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _hoverTween
            .TweenProperty(this, "scale", targetScale, HoverTweenDuration)
            .From(startScale)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void EnsureMaterial()
    {
        if (_material != null && GodotObject.IsInstanceValid(_material))
            return;

        var shader = GD.Load<Shader>("res://shader/UI/CharacterFootMarker.gdshader");
        if (shader == null)
            return;

        _material = new ShaderMaterial
        {
            Shader = shader,
            ResourceLocalToScene = true,
        };
        Material = _material;
    }
}
