using System;
using Godot;

public partial class SkillCard : SubViewportContainer
{
    private const int DefaultDescriptionFontSize = 19;
    private const int MinDescriptionFontSize = 8;

    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("SubViewport/Description");
    public Label NameLabel => field ??= GetNode<Label>("SubViewport/NameLabel");
    public Button Button => field ??= GetNode<Button>("SubViewport/Button");
    public SkillButton SkillTypeIcon => field ??= GetNode<SkillButton>("SubViewport/SkillIcon");
    public Panel HoverHint => field ??= GetNode<Panel>("SubViewport/HoverHint");
    public Label CharacterName => field ??= GetNode<Label>("SubViewport/CharacterName");
    public Skill CurrentSkill { get; set; }

    private Tween _progressTween;
    private Tween _pressTween;
    private Tween _hoverTween;
    private int _baseDescriptionFontSize;
    private int _textAdjustVersion;
    private Vector2 _baseScale = Vector2.One;
    private Vector2 _configuredDisplayScale = Vector2.One;

    public override void _Ready()
    {
        ApplySkillToUi();
        HoverHint.Visible = false;
        CacheBaseFontSizes();
        ApplyConfiguredDisplayScale();
        PivotOffsetRatio = new Vector2(0.5f, 0.5f);
        Button.MouseEntered += () =>
        {
            HoverHint.Visible = true;
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale * 1.08f, 0.2f);
        };
        Button.MouseExited += () =>
        {
            HoverHint.Visible = false;
            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale, 0.2f);
        };
        (Material as ShaderMaterial)?.SetShaderParameter("progress", 1f);
        Button.Pressed += PressEffect;
        SkillTypeIcon.Disabled = true;
    }

    public void ConfigureDisplayScale(Vector2 scale)
    {
        _configuredDisplayScale = scale;
        if (IsInsideTree())
            ApplyConfiguredDisplayScale();
    }

    public void ResetState()
    {
        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();

        HoverHint.Visible = false;
        Scale = _baseScale;
        Modulate = new Color(1, 1, 1, 1);

        if (Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("progress", 1f);
            shader.SetShaderParameter("center_vanish", 0f);
        }
    }

    public void RestoreDisplayState()
    {
        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();

        HoverHint.Visible = false;
        Scale = _baseScale;

        if (Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("progress", 0f);
            shader.SetShaderParameter("center_vanish", 0f);
        }
    }

    public void StartAnimation(float delay = 0f)
    {
        if (Material is not ShaderMaterial shader)
            return;

        _progressTween?.Kill();

        shader.SetShaderParameter("progress", 1f);

        _progressTween = CreateTween();
        if (delay > 0)
            _progressTween.TweenInterval(delay);

        _progressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("progress", value)),
                1f,
                0f,
                0.4f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void SetSkill(Skill skill)
    {
        CurrentSkill = skill;
        ApplySkillToUi();
    }

    private void ApplySkillToUi()
    {
        if (!IsInsideTree())
            return;

        if (CurrentSkill == null)
        {
            NameLabel.Text = string.Empty;
            Description.Text = string.Empty;
            return;
        }

        CurrentSkill.UpdateDescription();
        NameLabel.Text = CurrentSkill.SkillName ?? string.Empty;
        Description.Text = CurrentSkill.Description ?? string.Empty;

        SkillTypeIcon.SelfSkill = CurrentSkill;
        SkillTypeIcon.SetSkillType(CurrentSkill.SkillType);

        QueueAdjustTextSizes();
    }

    public void Vanish()
    {
        if (Material is not ShaderMaterial shader)
            return;

        _progressTween?.Kill();
        _progressTween = CreateTween();
        _progressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("progress", value)),
                (float)shader.GetShaderParameter("progress"),
                1f,
                0.3f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void PressEffect()
    {
        if (Material is not ShaderMaterial shader)
            return;

        _pressTween?.Kill();
        _pressTween = CreateTween();
        _pressTween.SetParallel(true);

        _pressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("center_vanish", value)),
                (float)shader.GetShaderParameter("center_vanish"),
                1.0f,
                0.4f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        _pressTween
            .TweenProperty(this, "modulate", 3 * new Color(1, 1, 1, 1f), 0.3f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void CacheBaseFontSizes()
    {
        _baseDescriptionFontSize = Description.GetThemeFontSize("normal_font_size");
        if (_baseDescriptionFontSize <= 0)
            _baseDescriptionFontSize = DefaultDescriptionFontSize;
    }

    private void ApplyConfiguredDisplayScale()
    {
        _baseScale = _configuredDisplayScale;
        Scale = _baseScale;
    }

    private async void QueueAdjustTextSizes()
    {
        int version = ++_textAdjustVersion;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (!IsInsideTree() || version != _textAdjustVersion)
            return;

        Description.Size = new Vector2(Description.Size.X, CharacterName.Position.Y - Description.Position.Y - 10f);
        if (!IsInsideTree())
            return;

        AdjustDescriptionFont();
    }

    private void AdjustDescriptionFont()
    {
        float availableHeight = Description.Size.Y;
        if (availableHeight <= 0.0f)
            return;

        for (int fontSize = _baseDescriptionFontSize; fontSize >= MinDescriptionFontSize; fontSize--)
        {
            Description.AddThemeFontSizeOverride("normal_font_size", fontSize);
            if (Description.GetContentHeight() <= availableHeight)
                return;
        }

        Description.AddThemeFontSizeOverride("normal_font_size", MinDescriptionFontSize);
    }

}
