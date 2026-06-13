using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public sealed class CardTrailMoveOptions
{
    public float CompressDuration { get; init; } = 0.18f;
    public float FlyDuration { get; init; } = 0.34f;
    public float TrailFadeDuration { get; init; } = 0.14f;
    public float CompressedScaleFactor { get; init; } = 0.38f;
    public float TargetScaleFactor { get; init; } = 0.18f;
    public float CenterVanish { get; init; } = 0.9f;
    public float GlowMultiplier { get; init; } = 1.2f;
    public bool HideCardVisualOnArrival { get; init; } = true;
    public bool RotateWithVelocity { get; init; } = true;
}

public partial class SkillCard : Control
{
    private const int DefaultDescriptionFontSize = 17;
    private const int MinDescriptionFontSize = 8;
    private const string EnergyCostNumberColor = "#fff05a";
    private const string EnergyCostInsufficientNumberColor = "#9aa0a8";
    private static readonly Vector2 CardBaseSize = new(240f, 370f);
    private static readonly Color CardBaseColor = new(0.028700002f, 0.04109f, 0.07f, 0.82f);
    private static readonly Color ArtFrameBaseColor = new(0.07350001f, 0.09389999f, 0.15f, 0.56f);
    private static readonly Color DescriptionBaseColor = new(
        0.06666667f,
        0.08627451f,
        0.13333334f,
        0.96f
    );
    private static readonly Color FooterBaseColor = new(
        0.050980393f,
        0.07058824f,
        0.10980392f,
        0.98f
    );
    private static readonly Color EchoPlateColor = new(0.86f, 0.42f, 1.0f, 1f);
    private static readonly Color KasiyaPlateColor = new(0.96f, 0.36f, 0.32f, 1f);
    private static readonly Color MariyaPlateColor = new(0.36f, 0.80f, 0.52f, 1f);
    private static readonly Color NightingalePlateColor = new(0.10f, 0.28f, 0.72f, 1f);

    public Panel BG => field ??= GetNode<Panel>("SubViewport/BG");
    public Panel InnerFrame => field ??= GetNode<Panel>("SubViewport/InnerFrame");
    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("SubViewport/Description");
    public Label NameLabel => field ??= GetNode<Label>("SubViewport/NameLabel");
    public Button Button => field ??= GetNode<Button>("SubViewport/Button");
    public CanvasGroup CardVisualRoot => field ??= GetNodeOrNull<CanvasGroup>("SubViewport");
    public TextureRect SkillPicture =>
        field ??= GetNodeOrNull<TextureRect>("SubViewport/ArtFrame/SkillPicture");
    public TextureRect SkillIcon =>
        field ??= GetNode<TextureRect>("SubViewport/ArtFrame/SkillIcon");
    public Panel HoverHint => field ??= GetNode<Panel>("SubViewport/HoverHint");
    public Panel BG2 => field ??= GetNode<Panel>("SubViewport/BG2");
    public Panel ArtFrame => field ??= GetNode<Panel>("SubViewport/ArtFrame");
    public Control RarityBadge => field ??= GetNode<Control>("SubViewport/RarityBadge");
    public Control EnergyBadge => field ??= GetNode<Control>("SubViewport/EnergyBadge");
    public Label TypeLabel => field ??= GetNode<Label>("SubViewport/TypeBadge/TypeLabel");
    public Label CharacterName => field ??= GetNode<Label>("SubViewport/CharacterName");
    public RichTextLabel EnergyCost =>
        field ??= GetNode<RichTextLabel>("SubViewport/EnergyBadge/EnergyCost");
    public ColorRect ArtFill => field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtFill");
    public ColorRect ArtBandTop =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtBandTop");
    public ColorRect TopAccent => field ??= GetNodeOrNull<ColorRect>("SubViewport/TopAccent");
    public ColorRect ArtDiamondOuter =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtDiamondOuter");
    public ColorRect ArtDiamondInner =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtDiamondInner");
    public Node2D NativeFrame => field ??= GetNodeOrNull<Node2D>("SubViewport/NativeFrame");
    public Polygon2D RarityPlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/RarityPlate");
    public Polygon2D EnergyPlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/EnergyPlate");
    public Polygon2D NamePlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/BG2/NamePlate");
    public Polygon2D CharacterPlate =>
        field ??= GetNodeOrNull<Polygon2D>("SubViewport/BG2/CharacterPlate");
    public Node2D DiscardTrailTarget => field ??= GetNodeOrNull<Node2D>("DiscardCardTrailTarget");
    public Line DiscardTrail => field ??= GetNodeOrNull<Line>("DiscardCardTrail");
    public GpuParticles2D DiscardTrailParticles =>
        field ??= GetNodeOrNull<GpuParticles2D>("DiscardCardTrailTarget/DiscardCardTrailParticles");
    public Node2D DrawTrailTarget => field ??= GetNodeOrNull<Node2D>("DrawCardTrailTarget");
    public Line DrawTrail => field ??= GetNodeOrNull<Line>("DrawCardTrail");
    public GpuParticles2D DrawTrailParticles =>
        field ??= GetNodeOrNull<GpuParticles2D>("DrawCardTrailTarget/DrawCardTrailParticles");
    public Skill CurrentSkill { get; set; }
    public string PreviewCharacterName { get; set; }
    public string PreviewCharacterKey { get; set; }
    public string DisplayNameOverride { get; set; }
    public bool AutoPressEffect { get; set; } = true;
    public bool UseDefaultHoverEffect { get; set; } = true;
    public bool HoverUiEnabled { get; set; } = true;
    public bool AutoAdjustDescriptionTextSize { get; set; } = true;

    private Tween _progressTween;
    private Tween _pressTween;
    private Tween _hoverTween;
    private Tween _motionTween;
    private Tween _drawSettleTween;
    private Tween _playableHighlightTween;
    private int _baseDescriptionFontSize;
    private int _textAdjustVersion;
    private Vector2 _baseScale = Vector2.One;
    private Vector2 _configuredDisplayScale = Vector2.One;
    private StyleBoxFlat _bgStyle;
    private StyleBoxFlat _innerFrameStyle;
    private StyleBoxFlat _descriptionStyle;
    private StyleBoxFlat _bg2Style;
    private StyleBoxFlat _artFrameStyle;
    private ShaderMaterial _defaultCardMaterial;
    private ShaderMaterial _playableHighlightMaterial;
    private ColorRect _playableHighlight;
    private float _playableHighlightEnabledValue = 1f;
    private CanvasItem _cardEffectMaterialTarget;
    private Character[] _previewHostileTargets = Array.Empty<Character>();
    private Character[] _previewFriendlyTargets = Array.Empty<Character>();
    private bool _energyCostAffordable = true;
    private readonly List<VBoxContainer> _previewDamagePanels = new();
    private static readonly Color HostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color FriendlyTargetPreviewColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Vector2 DamagePreviewLabelOffset = new(-50f, -115f);
    private static readonly Dictionary<Skill.SkillTypes, Texture2D> TypeIconCache = new();
    private static readonly Dictionary<SkillID, Texture2D> SkillIconCache = new();
    private static readonly Dictionary<string, Texture2D> SkillPictureCache = new();
    private static readonly HashSet<string> MissingSkillPicturePaths = new(StringComparer.Ordinal);
    private static readonly string[] SkillPictureExtensions = [".png", ".jpg", ".jpeg", ".webp"];
    private static Shader _defaultCardShader;
    private static Shader _defaultCanvasGroupCardShader;
    private static Shader _cardExhaustShader;
    private static Shader _cardCanvasGroupExhaustShader;
    private static Shader _playableHighlightShader;
    private static Shader DefaultCardShader =>
        _defaultCardShader ??= GD.Load<Shader>("res://shader/Effect/RewardCard.gdshader");
    private static Shader DefaultCanvasGroupCardShader =>
        _defaultCanvasGroupCardShader ??= GD.Load<Shader>(
            "res://shader/Effect/RewardCardCanvasGroup.gdshader"
        );
    private static Shader CardExhaustShader =>
        _cardExhaustShader ??= GD.Load<Shader>("res://shader/Effect/CardExhaust.gdshader");
    private static Shader CardCanvasGroupExhaustShader =>
        _cardCanvasGroupExhaustShader ??= GD.Load<Shader>(
            "res://shader/Effect/CardExhaustCanvasGroup.gdshader"
        );
    private static Shader PlayableHighlightShader =>
        _playableHighlightShader ??= GD.Load<Shader>(
            "res://shader/Effect/PlayableCardCrystalBorder.gdshader"
        );
    private static NoiseTexture2D _cardExhaustNoiseTexture;
    private static ShaderMaterial _cardExhaustMaterialTemplate;
    private static Texture2D _defaultSkillIcon;
    private Tip _keywordTooltip;
    private Skill.SkillRarity _lastAppliedRarity = Skill.SkillRarity.Common;
    private bool _lastAppliedStatusCard;
    private string _lastAppliedStyleCharacterKey = string.Empty;
    private Tip KeywordTooltip => _keywordTooltip ??= EnsureGlobalTooltip();

    public override void _Ready()
    {
        CacheDefaultCardMaterial();
        CacheBaseFontSizes();
        EnsurePlayableHighlight();
        ApplySkillToUi();
        HoverHint.Visible = false;
        ApplyConfiguredDisplayScale();
        PivotOffsetRatio = new Vector2(0.5f, 0.5f);
        Button.MouseEntered += () =>
        {
            if (!HoverUiEnabled)
                return;

            HoverHint.Visible = true;
            ShowKeywordTooltip();
            if (!UseDefaultHoverEffect)
                return;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale * 1.08f, 0.15f);
        };
        Button.MouseExited += () =>
        {
            HideHoverUi();
            if (!UseDefaultHoverEffect)
                return;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale, 0.15f);
        };
        RestoreDefaultCardMaterial()?.SetShaderParameter("progress", 0f);
        Button.Pressed += () =>
        {
            if (AutoPressEffect)
                PressEffect();
        };
    }

    public void ConfigureDisplayScale(Vector2 scale)
    {
        _configuredDisplayScale = scale;
        ApplyConfiguredDisplayScale();
    }

    public void ResetState()
    {
        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();
        _motionTween?.Kill();
        _drawSettleTween?.Kill();
        _playableHighlightTween?.Kill();
        SetPlayableHighlight(false, instant: true);

        SetCardVisualVisible(true);
        HoverHint.Visible = false;
        PivotOffset = CardBaseSize * 0.5f;
        ApplyConfiguredDisplayScale();
        Scale = _baseScale;
        Position = Vector2.Zero;
        Rotation = 0f;
        ZIndex = 0;
        Modulate = new Color(1, 1, 1, 1);

        if (RestoreDefaultCardMaterial() is ShaderMaterial shader)
        {
            shader.SetShaderParameter("progress", 0f);
            shader.SetShaderParameter("center_vanish", 0f);
            shader.SetShaderParameter("line_strength", 1f);
        }

        ResetDiscardTrailEffects();
    }

    public void RestoreDisplayState()
    {
        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();
        _motionTween?.Kill();
        _drawSettleTween?.Kill();
        _playableHighlightTween?.Kill();
        SetPlayableHighlight(false, instant: true);

        SetCardVisualVisible(true);
        HoverHint.Visible = false;
        PivotOffset = CardBaseSize * 0.5f;
        ApplyConfiguredDisplayScale();
        Scale = _baseScale;
        Position = Vector2.Zero;
        Rotation = 0f;
        ZIndex = 0;

        if (RestoreDefaultCardMaterial() is ShaderMaterial shader)
        {
            shader.SetShaderParameter("progress", 0f);
            shader.SetShaderParameter("center_vanish", 0f);
            shader.SetShaderParameter("line_strength", 1f);
        }

        ResetDiscardTrailEffects();
    }

    private void ResetDiscardTrailEffects()
    {
        if (DiscardTrail != null && GodotObject.IsInstanceValid(DiscardTrail))
        {
            DiscardTrail.Visible = false;
            DiscardTrail.ClearPoints();
            DiscardTrail.Modulate = Colors.White;
            DiscardTrail.ManualPreviewMode = false;
        }

        if (DiscardTrailParticles != null && GodotObject.IsInstanceValid(DiscardTrailParticles))
        {
            DiscardTrailParticles.Emitting = false;
            DiscardTrailParticles.Visible = false;
            DiscardTrailParticles.Modulate = Colors.White;
        }

        if (DiscardTrailTarget != null && GodotObject.IsInstanceValid(DiscardTrailTarget))
            DiscardTrailTarget.Visible = false;

        if (DrawTrail != null && GodotObject.IsInstanceValid(DrawTrail))
        {
            DrawTrail.Visible = false;
            DrawTrail.ClearPoints();
            DrawTrail.Modulate = Colors.White;
            DrawTrail.ManualPreviewMode = false;
        }

        if (DrawTrailParticles != null && GodotObject.IsInstanceValid(DrawTrailParticles))
        {
            DrawTrailParticles.Emitting = false;
            DrawTrailParticles.Visible = false;
            DrawTrailParticles.Modulate = Colors.White;
        }

        if (DrawTrailTarget != null && GodotObject.IsInstanceValid(DrawTrailTarget))
            DrawTrailTarget.Visible = false;
    }

    public void SetCardVisualVisible(bool visible)
    {
        if (CardVisualRoot != null && GodotObject.IsInstanceValid(CardVisualRoot))
            CardVisualRoot.Visible = visible;
    }

    public void StartAnimation(float delay = 0f)
    {
        StartAnimationWithDuration(delay, 0.4f);
    }

    public void StartAnimationWithDuration(float delay, float duration)
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
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
                duration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void PlayDrawSettleEffect(float delay = 0f)
    {
        if (!IsInsideTree())
            return;

        _drawSettleTween?.Kill();
        Scale = _baseScale * 1.045f;
        _drawSettleTween = CreateTween();
        if (delay > 0f)
            _drawSettleTween.TweenInterval(delay);

        _drawSettleTween
            .TweenProperty(this, "scale", _baseScale * 0.992f, 0.07f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _drawSettleTween
            .TweenProperty(this, "scale", _baseScale, 0.09f)
            .SetTrans(Tween.TransitionType.Back)
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
            _energyCostAffordable = true;
            if (NameLabel.Text.Length > 0)
                NameLabel.Text = string.Empty;
            if (CharacterName.Text.Length > 0)
                CharacterName.Text = string.Empty;
            if (TypeLabel.Text.Length > 0)
                TypeLabel.Text = string.Empty;
            if (Description.Text.Length > 0)
                Description.Text = string.Empty;
            if (EnergyCost.Text.Length > 0)
                SetEnergyCostText(string.Empty);
            if (SkillPicture != null)
            {
                if (SkillPicture.Texture != null)
                    SkillPicture.Texture = null;
                if (SkillPicture.Visible)
                    SkillPicture.Visible = false;
            }
            if (SkillIcon.Texture != null)
                SkillIcon.Texture = null;
            if (SkillIcon.Visible)
                SkillIcon.Visible = false;
            SetArtPlaceholderVisible(true);
            ApplyRarityStyles(Skill.SkillRarity.Common);
            _lastAppliedRarity = Skill.SkillRarity.Common;
            _lastAppliedStatusCard = false;
            _lastAppliedStyleCharacterKey = string.Empty;
            ApplyPreferredDescriptionFontSize();
            return;
        }

        CurrentSkill.UpdateDescription();
        _energyCostAffordable = true;
        bool isStatusCard = IsStatusCard(CurrentSkill);
        string displayName = DisplayNameOverride ?? CurrentSkill.SkillName ?? string.Empty;
        string characterName = isStatusCard
            ? string.Empty
            : PreviewCharacterName ?? CurrentSkill.OwnerCharater?.CharacterName ?? string.Empty;
        string skillTypeText = isStatusCard
            ? I18n.Tr("ui.encyclopedia.skill_type.status", "状态")
            : CurrentSkill.SkillType.GetDescription();
        string descriptionText = CurrentSkill.Description ?? string.Empty;
        string centeredEnergyText = BuildEnergyCostText(CurrentSkill, _energyCostAffordable);
        bool textChanged = false;
        if (NameLabel.Text != displayName)
        {
            NameLabel.Text = displayName;
            textChanged = true;
        }
        if (CharacterName.Text != characterName)
            CharacterName.Text = characterName;
        if (TypeLabel.Text != skillTypeText)
        {
            TypeLabel.Text = skillTypeText;
            textChanged = true;
        }
        if (Description.Text != descriptionText)
        {
            Description.Text = descriptionText;
            textChanged = true;
        }
        if (EnergyCost.Text != centeredEnergyText)
            EnergyCost.Text = centeredEnergyText;

        string styleCharacterKey = isStatusCard
            ? string.Empty
            : PreviewCharacterKey
                ?? (CurrentSkill.OwnerCharater as PlayerCharacter)?.CharacterKey
                ?? CurrentSkill.OwnerCharater?.CharacterName
                ?? string.Empty;
        bool shouldRefreshStyle =
            _lastAppliedRarity != CurrentSkill.Rarity
            || _lastAppliedStatusCard != isStatusCard
            || !string.Equals(
                _lastAppliedStyleCharacterKey,
                styleCharacterKey,
                StringComparison.Ordinal
            );
        if (shouldRefreshStyle)
        {
            ApplyRarityStyles(CurrentSkill.Rarity);
            if (isStatusCard)
                ApplyStatusCardStyle();
            else
                ApplyCharacterPlateStyle(CurrentSkill);
            _lastAppliedRarity = CurrentSkill.Rarity;
            _lastAppliedStatusCard = isStatusCard;
            _lastAppliedStyleCharacterKey = styleCharacterKey;
        }

        Texture2D skillPicture = GetSkillPictureTexture(
            CurrentSkill,
            PreviewCharacterName,
            PreviewCharacterKey
        );
        bool hasSkillPicture = skillPicture != null;
        if (SkillPicture != null)
        {
            if (SkillPicture.Texture != skillPicture)
                SkillPicture.Texture = skillPicture;
            if (SkillPicture.Visible != hasSkillPicture)
                SkillPicture.Visible = hasSkillPicture;
            if (SkillPicture.Modulate != Colors.White)
                SkillPicture.Modulate = Colors.White;
        }

        SetArtPlaceholderVisible(!hasSkillPicture);
        if (SkillIcon.Visible != !hasSkillPicture)
            SkillIcon.Visible = !hasSkillPicture;
        if (!hasSkillPicture)
        {
            Texture2D iconTexture = GetSkillIconTexture(CurrentSkill);
            if (SkillIcon.Texture != iconTexture)
                SkillIcon.Texture = iconTexture;
            if (SkillIcon.Modulate != Colors.White)
                SkillIcon.Modulate = Colors.White;
        }
        else if (SkillIcon.Texture != null)
        {
            SkillIcon.Texture = null;
        }

        if (textChanged)
            ApplyDescriptionFontSizing();
    }

    public void SetEnergyCostText(string text)
    {
        EnergyCost.Text = string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : $"[center]{text}[/center]";
    }

    public void SetEnergyCostCostText(string costText)
    {
        string coloredCost =
            $"[font_size=28][b][color={EnergyCostNumberColor}]{costText}[/color][/b][/font_size]";
        SetEnergyCostText(
            I18n.Format("ui.reward.energy_cost", "耗能:{cost}", ("cost", coloredCost))
        );
    }

    public void SetEnergyCostAffordable(bool affordable)
    {
        _energyCostAffordable = affordable;
        if (CurrentSkill == null)
            return;

        string energyText = BuildEnergyCostText(CurrentSkill, affordable);
        if (EnergyCost.Text != energyText)
            EnergyCost.Text = energyText;
    }

    private static string BuildEnergyCostText(Skill skill, bool affordable)
    {
        if (skill == null)
            return string.Empty;

        string energyText;
        if (skill.CanBePlayed)
        {
            string color = affordable ? EnergyCostNumberColor : EnergyCostInsufficientNumberColor;
            string coloredCost =
                $"[font_size=28][b][color={color}]{skill.CardEnergyCostText}[/color][/b][/font_size]";
            energyText = I18n.Format("ui.reward.energy_cost", "耗能:{cost}", ("cost", coloredCost));
        }
        else
        {
            energyText = I18n.Tr("ui.encyclopedia.skill_cost.unplayable", "不可打出");
        }

        return string.IsNullOrWhiteSpace(energyText) ? string.Empty : $"[center]{energyText}[/center]";
    }

    public void ShowSkillPreview()
    {
        ShowTargetPreview();
        ShowDamagePreview();
    }

    public void HideSkillPreview()
    {
        HideDamagePreview();
        HideTargetPreview();
    }

    public void SetPlayableHighlight(bool enabled, bool instant = false)
    {
        ColorRect highlight = EnsurePlayableHighlight();
        if (highlight == null || _playableHighlightMaterial == null)
            return;

        float target = enabled ? _playableHighlightEnabledValue : 0f;
        _playableHighlightTween?.Kill();
        if (!enabled && !highlight.Visible)
        {
            _playableHighlightMaterial.SetShaderParameter("highlight_enabled", 0f);
            return;
        }

        if (enabled && !highlight.Visible && !instant && IsInsideTree())
            _playableHighlightMaterial.SetShaderParameter("highlight_enabled", 0f);

        if (instant || !IsInsideTree())
        {
            _playableHighlightMaterial.SetShaderParameter("highlight_enabled", target);
            highlight.Visible = enabled;
            return;
        }

        highlight.Visible = true;
        float current = GetShaderParameterFloat(_playableHighlightMaterial, "highlight_enabled");
        _playableHighlightTween = CreateTween();
        _playableHighlightTween
            .TweenMethod(
                Callable.From<float>(
                    value => _playableHighlightMaterial.SetShaderParameter(
                        "highlight_enabled",
                        value
                    )
                ),
                current,
                target,
                enabled ? 0.18f : 0.14f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        if (!enabled)
            _playableHighlightTween.TweenCallback(Callable.From(() => highlight.Visible = false));
    }

    public void SetHoverUiEnabled(bool enabled)
    {
        HoverUiEnabled = enabled;
        if (!enabled)
            HideHoverUi();
    }

    public void HideHoverUi()
    {
        HoverHint.Visible = false;
        _keywordTooltip?.HideTooltip();
    }

    private void ShowKeywordTooltip()
    {
        if (CurrentSkill == null || KeywordTooltip == null)
            return;

        CurrentSkill.UpdateDescription();
        string tooltipText = Skill.BuildKeywordTooltipText(CurrentSkill);
        if (string.IsNullOrWhiteSpace(tooltipText))
        {
            KeywordTooltip.HideTooltip();
            return;
        }

        KeywordTooltip.FollowMouse = true;
        KeywordTooltip.SetText(tooltipText);
    }

    public void RefreshTextSizeFromSettings()
    {
        ApplyDescriptionFontSizing(force: true);
        _keywordTooltip?.RefreshTextSizeFromSettings();
    }

    public void Vanish()
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
            return;

        _progressTween?.Kill();
        shader.SetShaderParameter("line_strength", 0f);
        _progressTween = CreateTween();
        _progressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("progress", value)),
                GetShaderParameterFloat(shader, "progress"),
                1f,
                0.3f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void StopBattleMotion()
    {
        _hoverTween?.Kill();
        _hoverTween = null;
        _motionTween?.Kill();
        _motionTween = null;
        _drawSettleTween?.Kill();
        _drawSettleTween = null;
    }

    public void TweenBattleMotion(
        Vector2 targetPosition,
        Vector2 targetScale,
        float duration = 0.16f,
        bool instant = false
    )
    {
        StopBattleMotion();

        if (instant || duration <= 0f || !IsInsideTree())
        {
            Position = targetPosition;
            Scale = targetScale;
            return;
        }

        _motionTween = CreateTween();
        _motionTween.SetParallel(true);
        _motionTween
            .TweenProperty(this, "position", targetPosition, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _motionTween
            .TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public async Task<bool> FlyWithTrailToControlAsync(
        Control target,
        CardTrailMoveOptions options = null
    )
    {
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || !target.IsInsideTree()
        )
        {
            return false;
        }

        return await FlyWithTrailToPointAsync(target.GetGlobalRect().GetCenter(), options);
    }

    public async Task<bool> FlyWithTrailToPointAsync(
        Vector2 endCenter,
        CardTrailMoveOptions options = null
    )
    {
        options ??= new CardTrailMoveOptions();
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
            return false;

        Rect2 startRect = GetGlobalRect();
        Vector2 startCenter = startRect.Position + startRect.Size * 0.5f;
        if (startCenter.DistanceSquaredTo(endCenter) < 16f)
            return false;

        PivotOffset = CardBaseSize * 0.5f;
        if (options.CompressDuration > 0f)
        {
            Tween compressShaderTween = PressEffectPartial(
                centerVanish: options.CenterVanish,
                glowMultiplier: options.GlowMultiplier,
                duration: options.CompressDuration
            );

            Tween compressTween = CreateTween();
            compressTween.SetParallel(true);
            compressTween
                .TweenProperty(
                    this,
                    "scale",
                    Vector2.One * options.CompressedScaleFactor,
                    options.CompressDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            compressTween
                .TweenMethod(
                    Callable.From<float>(_ => SetPivotCenterAt(startCenter)),
                    0f,
                    1f,
                    options.CompressDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            compressTween.SetParallel(false);
            await Task.WhenAll(
                WaitForTweenFinishedAsync(compressTween),
                WaitForTweenFinishedAsync(compressShaderTween)
            );
        }

        if (!GodotObject.IsInstanceValid(this))
            return false;

        Vector2 flyStartCenter = startCenter;
        PrepareMoveTrail(out Line trail, out GpuParticles2D particles);
        Vector2 control = GetCardFlyControlPoint(flyStartCenter, endCenter);
        Vector2 initialVelocity = GetQuadraticBezierVelocity(
            flyStartCenter,
            control,
            endCenter,
            0.01f
        );

        if (options.RotateWithVelocity)
            Rotation = GetRotationWithTopFacingVelocity(initialVelocity);
        UpdateTrailParticlesRotation(particles, initialVelocity);

        Tween flyShaderTween = PressEffectPartial(
            centerVanish: options.CenterVanish,
            glowMultiplier: options.GlowMultiplier,
            duration: options.FlyDuration
        );
        Tween flyTween = CreateTween();
        flyTween.SetParallel(true);
        flyTween
            .TweenProperty(
                this,
                "scale",
                Vector2.One * options.TargetScaleFactor,
                options.FlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        flyTween
            .TweenMethod(
                Callable.From<float>(t =>
                {
                    if (!GodotObject.IsInstanceValid(this))
                        return;

                    Vector2 center = QuadraticBezier(flyStartCenter, control, endCenter, t);
                    Vector2 velocity = GetQuadraticBezierVelocity(
                        flyStartCenter,
                        control,
                        endCenter,
                        t
                    );
                    SetPivotCenterAt(center);
                    if (options.RotateWithVelocity)
                        Rotation = GetRotationWithTopFacingVelocity(velocity);
                    UpdateTrailParticlesRotation(particles, velocity);
                }),
                0f,
                1f,
                options.FlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        flyTween.SetParallel(false);

        await Task.WhenAll(
            WaitForTweenFinishedAsync(flyTween),
            WaitForTweenFinishedAsync(flyShaderTween)
        );

        if (GodotObject.IsInstanceValid(this) && options.HideCardVisualOnArrival)
        {
            SetCardVisualVisible(false);
            Button.Disabled = true;
            HoverHint.Visible = false;
        }

        await FadeAndHideMoveTrailAsync(trail, particles, options.TrailFadeDuration);
        return true;
    }

    public void HideMoveTrail()
    {
        HideTrail(DiscardTrail, DiscardTrailParticles);
        if (DiscardTrailTarget != null && GodotObject.IsInstanceValid(DiscardTrailTarget))
            DiscardTrailTarget.Visible = false;
    }

    private static async Task WaitForTweenFinishedAsync(Tween tween)
    {
        if (tween == null || !GodotObject.IsInstanceValid(tween))
            return;

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        tween.Finished += () => completion.TrySetResult(true);
        await completion.Task;
    }

    private void SetPivotCenterAt(Vector2 center)
    {
        if (!GodotObject.IsInstanceValid(this))
            return;

        Vector2 currentPivotCenter = GetGlobalTransformWithCanvas() * PivotOffset;
        GlobalPosition += center - currentPivotCenter;
    }

    private void PrepareMoveTrail(out Line trail, out GpuParticles2D particles)
    {
        trail = null;
        particles = null;
        Node2D target = DiscardTrailTarget;
        trail = DiscardTrail;
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || trail == null
            || !GodotObject.IsInstanceValid(trail)
        )
        {
            return;
        }

        target.Visible = true;
        target.Position = PivotOffset;
        trail.Target = target;
        trail.ManualPreviewMode = false;
        trail.Visible = true;
        trail.GlobalPosition = Vector2.Zero;
        trail.Modulate = Colors.White;
        trail.ClearPoints();

        particles = DiscardTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        particles.Emitting = true;
    }

    private static async Task FadeAndHideMoveTrailAsync(
        Line trail,
        GpuParticles2D particles,
        float duration
    )
    {
        if (particles != null && GodotObject.IsInstanceValid(particles))
            particles.Emitting = false;

        if (trail == null || !GodotObject.IsInstanceValid(trail))
        {
            HideTrailParticles(particles);
            return;
        }

        trail.ManualPreviewMode = true;
        float startWidth = trail.Width;
        Tween tween = trail.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(fade =>
            {
                if (trail == null || !GodotObject.IsInstanceValid(trail))
                    return;

                trail.Modulate = new Color(1f, 1f, 1f, 1f - fade);
                trail.Width = Mathf.Lerp(startWidth, 0.5f, fade);
            }),
            0f,
            1f,
            Math.Max(0.01f, duration)
        );
        tween.TweenCallback(
            Callable.From(() =>
            {
                HideTrail(trail, particles);
                if (trail?.Target != null && GodotObject.IsInstanceValid(trail.Target))
                    trail.Target.Visible = false;
                if (trail != null && GodotObject.IsInstanceValid(trail))
                    trail.Width = startWidth;
            })
        );

        await WaitForTweenFinishedAsync(tween);
    }

    private static void HideTrail(Line trail, GpuParticles2D particles)
    {
        if (trail != null && GodotObject.IsInstanceValid(trail))
        {
            trail.Visible = false;
            trail.ClearPoints();
            trail.Modulate = Colors.White;
            trail.ManualPreviewMode = false;
        }

        HideTrailParticles(particles);
    }

    private static void HideTrailParticles(GpuParticles2D particles)
    {
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Emitting = false;
        particles.Visible = false;
        particles.Modulate = Colors.White;
    }

    private static void UpdateTrailParticlesRotation(GpuParticles2D particles, Vector2 velocity)
    {
        if (
            particles == null
            || !GodotObject.IsInstanceValid(particles)
            || velocity.LengthSquared() < 0.001f
        )
        {
            return;
        }

        particles.GlobalRotation = velocity.Angle() + Mathf.Pi;
    }

    private static Vector2 GetCardFlyControlPoint(Vector2 start, Vector2 end)
    {
        Vector2 mid = (start + end) * 0.5f;
        float distance = start.DistanceTo(end);
        float lift =
            Math.Min(330f, Math.Max(120f, distance * 0.28f)) + (float)GD.RandRange(-28f, 52f);
        float side = end.X >= start.X ? 1f : -1f;
        float sideOffset = side * Math.Min(190f, distance * 0.16f) + (float)GD.RandRange(-90f, 90f);
        return mid + new Vector2(sideOffset, -lift);
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        Vector2 a = start.Lerp(control, t);
        Vector2 b = control.Lerp(end, t);
        return a.Lerp(b, t);
    }

    private static Vector2 GetQuadraticBezierVelocity(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return 2f * ((1f - t) * (control - start) + t * (end - control));
    }

    private static float GetRotationWithTopFacingVelocity(Vector2 velocity)
    {
        if (velocity.LengthSquared() < 0.001f)
            return 0f;

        return velocity.Angle() + Mathf.Pi * 0.5f;
    }

    public void PressEffect()
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
            return;

        StopProgressEffect(shader);
        _pressTween?.Kill();
        _pressTween = CreateTween();
        _pressTween.SetParallel(true);

        _pressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("center_vanish", value)),
                GetShaderParameterFloat(shader, "center_vanish"),
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

    public Tween PressEffectPartial(
        float centerVanish = 0.72f,
        float glowMultiplier = 1.55f,
        float duration = 0.32f
    )
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
            return null;

        centerVanish = Mathf.Clamp(centerVanish, 0f, 1f);
        glowMultiplier = Mathf.Max(1f, glowMultiplier);
        duration = Math.Max(0.01f, duration);

        StopProgressEffect(shader);
        _pressTween?.Kill();
        _pressTween = CreateTween();
        _pressTween.SetParallel(true);

        _pressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("center_vanish", value)),
                GetShaderParameterFloat(shader, "center_vanish"),
                centerVanish,
                duration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        Color startModulate = Modulate;
        Color targetModulate = new(
            startModulate.R * glowMultiplier,
            startModulate.G * glowMultiplier,
            startModulate.B * glowMultiplier,
            startModulate.A
        );
        _pressTween
            .TweenProperty(this, "modulate", targetModulate, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        return _pressTween;
    }

    private void StopProgressEffect(ShaderMaterial shader)
    {
        _progressTween?.Kill();
        _progressTween = null;
        shader?.SetShaderParameter("progress", 0f);
    }

    public void PlayExhaustEffect(float duration = 0.8f)
    {
        if (GetCardExhaustShader() == null)
        {
            PressEffect();
            return;
        }

        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();
        _motionTween?.Kill();

        var shader = CreateCardExhaustMaterial();
        shader.SetShaderParameter("noise_offset", CreateCardExhaustNoiseOffset());
        CardEffectMaterialTarget.Material = shader;

        if (Modulate.A <= 0f)
            Modulate = Colors.White;
        _pressTween = CreateTween();
        _pressTween.SetParallel(true);
        _pressTween
            .TweenMethod(
                Callable.From<float>(value => shader.SetShaderParameter("exhaust_progress", value)),
                0f,
                1f,
                duration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _pressTween
            .TweenProperty(this, "scale", Scale * 1.035f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void CacheDefaultCardMaterial()
    {
        if (_defaultCardMaterial != null)
            return;

        CanvasItem materialTarget = CardEffectMaterialTarget;
        if (
            materialTarget.Material is ShaderMaterial sceneMaterial
            && IsDefaultCardShader(sceneMaterial.Shader)
            && sceneMaterial.Duplicate() is ShaderMaterial duplicatedSceneMaterial
        )
        {
            _defaultCardMaterial = duplicatedSceneMaterial;
        }
        else
        {
            _defaultCardMaterial = new ShaderMaterial { Shader = GetDefaultCardShader() };
            InitializeDefaultCardShaderParameters(_defaultCardMaterial);
        }

        if (_defaultCardMaterial == null)
            return;

        _defaultCardMaterial.ResourceLocalToScene = true;
        materialTarget.Material = _defaultCardMaterial;
    }

    private ShaderMaterial RestoreDefaultCardMaterial()
    {
        CacheDefaultCardMaterial();
        CanvasItem materialTarget = CardEffectMaterialTarget;
        if (_defaultCardMaterial != null && materialTarget.Material != _defaultCardMaterial)
            materialTarget.Material = _defaultCardMaterial;

        return materialTarget.Material as ShaderMaterial;
    }

    private ColorRect EnsurePlayableHighlight()
    {
        if (_playableHighlight != null && GodotObject.IsInstanceValid(_playableHighlight))
            return _playableHighlight;

        CanvasGroup root = CardVisualRoot;
        Shader shader = PlayableHighlightShader;
        if (root == null || !GodotObject.IsInstanceValid(root) || shader == null)
            return null;

        _playableHighlight = root.GetNodeOrNull<ColorRect>("PlayableHighlight");
        if (_playableHighlight != null && GodotObject.IsInstanceValid(_playableHighlight))
        {
            _playableHighlightMaterial =
                _playableHighlight.Material as ShaderMaterial
                ?? CreatePlayableHighlightMaterial(shader);
            _playableHighlight.Material = _playableHighlightMaterial;
            _playableHighlight.MouseFilter = MouseFilterEnum.Ignore;
            _playableHighlightEnabledValue = Math.Max(
                0.001f,
                GetShaderParameterFloat(_playableHighlightMaterial, "highlight_enabled")
            );
            if (_playableHighlight.GetIndex() != 0)
                root.MoveChild(_playableHighlight, 0);
            return _playableHighlight;
        }

        _playableHighlightMaterial = CreatePlayableHighlightMaterial(shader);
        _playableHighlight = new ColorRect
        {
            Name = "PlayableHighlight",
            Position = Vector2.Zero,
            Size = CardBaseSize,
            CustomMinimumSize = CardBaseSize,
            MouseFilter = MouseFilterEnum.Ignore,
            Color = Colors.White,
            Material = _playableHighlightMaterial,
            Visible = false,
        };
        root.AddChild(_playableHighlight);
        root.MoveChild(_playableHighlight, 0);
        return _playableHighlight;
    }

    private static ShaderMaterial CreatePlayableHighlightMaterial(Shader shader)
    {
        var material = new ShaderMaterial
        {
            Shader = shader,
            ResourceLocalToScene = true,
        };
        material.SetShaderParameter("highlight_enabled", 0f);
        return material;
    }

    private CanvasItem CardEffectMaterialTarget
    {
        get
        {
            if (_cardEffectMaterialTarget != null && GodotObject.IsInstanceValid(_cardEffectMaterialTarget))
                return _cardEffectMaterialTarget;

            _cardEffectMaterialTarget = GetNodeOrNull<CanvasItem>("CardGroup");
            if (_cardEffectMaterialTarget == null)
                _cardEffectMaterialTarget = GetNodeOrNull<CanvasGroup>("SubViewport");
            _cardEffectMaterialTarget ??= this;
            return _cardEffectMaterialTarget;
        }
    }

    private bool UsesCanvasGroupCardMaterial => CardEffectMaterialTarget is CanvasGroup;

    private Shader GetDefaultCardShader() =>
        UsesCanvasGroupCardMaterial ? DefaultCanvasGroupCardShader : DefaultCardShader;

    private Shader GetCardExhaustShader() =>
        UsesCanvasGroupCardMaterial ? CardCanvasGroupExhaustShader : CardExhaustShader;

    private bool IsDefaultCardShader(Shader shader)
    {
        Shader defaultShader = GetDefaultCardShader();
        if (shader == null || defaultShader == null)
            return false;

        return shader == defaultShader || shader.ResourcePath == defaultShader.ResourcePath;
    }

    private static void InitializeDefaultCardShaderParameters(ShaderMaterial shader)
    {
        if (shader == null)
            return;

        shader.SetShaderParameter("progress", 0f);
        shader.SetShaderParameter("center_vanish", 0f);
        shader.SetShaderParameter("line_density", 30f);
        shader.SetShaderParameter("line_speed", 8f);
        shader.SetShaderParameter("line_strength", 1f);
        shader.SetShaderParameter("aberration_amount", 0.03f);
        shader.SetShaderParameter("beam_color", new Color(0f, 1f, 1f, 1f));
    }

    private static NoiseTexture2D GetCardExhaustNoiseTexture()
    {
        if (_cardExhaustNoiseTexture != null)
            return _cardExhaustNoiseTexture;

        var noise = new FastNoiseLite
        {
            Seed = 47391,
            Frequency = 0.018f,
            FractalOctaves = 6,
            FractalGain = 0.54f,
            FractalLacunarity = 2.35f,
        };

        _cardExhaustNoiseTexture = new NoiseTexture2D
        {
            Width = 256,
            Height = 256,
            Seamless = true,
            Noise = noise,
        };
        return _cardExhaustNoiseTexture;
    }

    private static Vector2 CreateCardExhaustNoiseOffset()
    {
        double tick = Time.GetTicksUsec() * 0.000001;
        return new Vector2(
            Mathf.PosMod((float)(tick * 0.173), 1f),
            Mathf.PosMod((float)(tick * 0.317 + 0.41), 1f)
        );
    }

    public static void PrewarmExhaustEffect()
    {
        if (CardExhaustShader == null)
            return;

        _cardExhaustMaterialTemplate ??= CreateCardExhaustMaterialTemplate();
        GetCardExhaustNoiseTexture();
    }

    public static void ClearSharedCaches()
    {
        TypeIconCache.Clear();
        SkillIconCache.Clear();
        SkillPictureCache.Clear();
        MissingSkillPicturePaths.Clear();
        _defaultSkillIcon = null;
        _cardExhaustNoiseTexture = null;
        _cardExhaustMaterialTemplate = null;
        _defaultCardShader = null;
        _defaultCanvasGroupCardShader = null;
        _cardExhaustShader = null;
        _cardCanvasGroupExhaustShader = null;
        _playableHighlightShader = null;
    }

    private ShaderMaterial CreateCardExhaustMaterial()
    {
        PrewarmExhaustEffect();
        ShaderMaterial shader;
        if (UsesCanvasGroupCardMaterial)
        {
            shader = CreateCardExhaustMaterialTemplate(GetCardExhaustShader());
        }
        else
        {
            shader =
                _cardExhaustMaterialTemplate?.Duplicate() as ShaderMaterial
                ?? CreateCardExhaustMaterialTemplate(CardExhaustShader);
        }
        shader.ResourceLocalToScene = true;
        shader.SetShaderParameter("exhaust_progress", 0f);
        return shader;
    }

    private static ShaderMaterial CreateCardExhaustMaterialTemplate() =>
        CreateCardExhaustMaterialTemplate(CardExhaustShader);

    private static ShaderMaterial CreateCardExhaustMaterialTemplate(Shader shaderResource)
    {
        var shader = new ShaderMaterial { Shader = shaderResource, ResourceLocalToScene = true };
        shader.SetShaderParameter("exhaust_progress", 0f);
        shader.SetShaderParameter("ember_color", new Color(0.50f, 0.62f, 1f, 1f));
        shader.SetShaderParameter("ash_color", new Color(0.09f, 0.075f, 0.105f, 1f));
        shader.SetShaderParameter("edge_width", 0.07f);
        shader.SetShaderParameter("noise_scale", 1.35f);
        shader.SetShaderParameter("ember_amount", 0.48f);
        shader.SetShaderParameter("aberration_amount", 0.006f);
        shader.SetShaderParameter("noise_tex", GetCardExhaustNoiseTexture());
        shader.SetShaderParameter("noise_offset", Vector2.Zero);
        return shader;
    }

    public override void _ExitTree()
    {
        HideSkillPreview();
        _keywordTooltip?.HideTooltip();
        FreeDamagePreviewLabels();
        base._ExitTree();
    }

    private static Texture2D GetSkillIconTexture(Skill skill)
    {
        if (skill?.SkillId is SkillID skillId)
        {
            if (
                SkillIconCache.TryGetValue(skillId, out Texture2D cachedTexture)
                && cachedTexture != null
            )
                return cachedTexture;

            string path = $"res://asset/svg/SkillIcon/{skillId}.svg";
            Texture2D texture = PreloadeScene.GetTexture(path);

            if (texture != null)
            {
                SkillIconCache[skillId] = texture;
                return texture;
            }
        }

        return GetSkillTypeIconTexture(skill?.SkillType ?? Skill.SkillTypes.none);
    }

    public static void PrewarmSkillResources(
        Skill skill,
        string previewCharacterName = null,
        string previewCharacterKey = null
    )
    {
        if (skill == null)
            return;

        GetSkillPictureTexture(skill, previewCharacterName, previewCharacterKey);
        GetSkillIconTexture(skill);
    }

    private static Texture2D GetSkillPictureTexture(
        Skill skill,
        string previewCharacterName = null,
        string previewCharacterKey = null
    )
    {
        if (skill?.SkillId is not SkillID skillId)
            return null;

        foreach (
            string path in EnumerateSkillPicturePaths(
                skill,
                skillId,
                previewCharacterName,
                previewCharacterKey
            )
        )
        {
            if (MissingSkillPicturePaths.Contains(path))
                continue;

            if (
                SkillPictureCache.TryGetValue(path, out Texture2D cachedTexture)
                && cachedTexture != null
            )
                return cachedTexture;

            if (!ResourceLoader.Exists(path))
            {
                MissingSkillPicturePaths.Add(path);
                continue;
            }

            Texture2D texture = PreloadeScene.GetTexture(path);
            if (texture == null)
            {
                MissingSkillPicturePaths.Add(path);
                continue;
            }

            SkillPictureCache[path] = texture;
            return texture;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSkillPicturePaths(
        Skill skill,
        SkillID skillId,
        string previewCharacterName = null,
        string previewCharacterKey = null
    )
    {
        string skillFileName = skillId.ToString();
        var searchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (
            string folder in GetSkillPictureFolders(
                skill,
                skillId,
                previewCharacterName,
                previewCharacterKey
            )
        )
        {
            if (string.IsNullOrWhiteSpace(folder) || !searchedFolders.Add(folder))
                continue;

            foreach (string extension in SkillPictureExtensions)
                yield return $"res://asset/CardPicture/{folder}/{skillFileName}{extension}";
        }

        string[] legacyFileNames = [skillFileName, $"Kasiya{skillFileName}"];
        foreach (string legacyFileName in legacyFileNames)
        {
            foreach (string extension in SkillPictureExtensions)
                yield return $"res://asset/CardPicture/{legacyFileName}{extension}";
        }
    }

    private static IEnumerable<string> GetSkillPictureFolders(
        Skill skill,
        SkillID skillId,
        string previewCharacterName = null,
        string previewCharacterKey = null
    )
    {
        if (!string.IsNullOrWhiteSpace(previewCharacterKey))
            yield return previewCharacterKey;

        if (
            skill?.OwnerCharater is PlayerCharacter player
            && !string.IsNullOrWhiteSpace(player.CharacterKey)
        )
            yield return player.CharacterKey;

        if (!string.IsNullOrWhiteSpace(skill?.OwnerCharater?.CharacterName))
            yield return skill.OwnerCharater.CharacterName;

        if (!string.IsNullOrWhiteSpace(previewCharacterName))
            yield return previewCharacterName;

        if (Skill.TryGetPlayerCharacterKey(skillId, out PlayerCharacterKey characterKey))
            yield return characterKey.ToString();
    }

    private void SetArtPlaceholderVisible(bool visible)
    {
        if (ArtFill != null)
            ArtFill.Visible = visible;
        if (ArtBandTop != null)
            ArtBandTop.Visible = visible;
        if (ArtDiamondOuter != null)
            ArtDiamondOuter.Visible = visible;
        if (ArtDiamondInner != null)
            ArtDiamondInner.Visible = visible;
    }

    private static Texture2D GetSkillTypeIconTexture(Skill.SkillTypes skillType)
    {
        if (skillType == Skill.SkillTypes.none)
            return GetDefaultSkillIcon();

        if (TypeIconCache.TryGetValue(skillType, out Texture2D texture) && texture != null)
            return texture;

        string path = skillType switch
        {
            Skill.SkillTypes.Attack => "res://asset/svg/SkillIcon/attack.svg",
            Skill.SkillTypes.Survive => "res://asset/svg/SkillIcon/survive.svg",
            Skill.SkillTypes.Special => "res://asset/svg/SkillIcon/special.svg",
            _ => "res://asset/svg/SkillIcon/default.svg",
        };

        texture = PreloadeScene.GetTexture(path) ?? GetDefaultSkillIcon();
        TypeIconCache[skillType] = texture;
        return texture;
    }

    private static Texture2D GetDefaultSkillIcon()
    {
        _defaultSkillIcon ??= PreloadeScene.GetTexture("res://asset/svg/SkillIcon/default.svg");
        return _defaultSkillIcon;
    }

    private void ShowTargetPreview()
    {
        HideTargetPreview();
        if (CurrentSkill == null)
            return;

        _previewHostileTargets = CurrentSkill.GetPreviewHostileTargets();
        _previewFriendlyTargets = CurrentSkill.GetPreviewFriendlyTargets();

        foreach (
            var target in (_previewHostileTargets ?? Array.Empty<Character>()).Where(
                GodotObject.IsInstanceValid
            )
        )
            target.ShowTargetPreview(HostileTargetPreviewColor);

        foreach (
            var target in (_previewFriendlyTargets ?? Array.Empty<Character>()).Where(
                GodotObject.IsInstanceValid
            )
        )
            target.ShowTargetPreview(FriendlyTargetPreviewColor);
    }

    private void HideTargetPreview()
    {
        if (
            (_previewHostileTargets == null || _previewHostileTargets.Length == 0)
            && (_previewFriendlyTargets == null || _previewFriendlyTargets.Length == 0)
        )
        {
            _previewHostileTargets = Array.Empty<Character>();
            _previewFriendlyTargets = Array.Empty<Character>();
            return;
        }

        foreach (
            var target in (_previewHostileTargets ?? Array.Empty<Character>()).Where(
                GodotObject.IsInstanceValid
            )
        )
            target.HideTargetPreview();

        foreach (
            var target in (_previewFriendlyTargets ?? Array.Empty<Character>()).Where(
                GodotObject.IsInstanceValid
            )
        )
            target.HideTargetPreview();

        _previewHostileTargets = Array.Empty<Character>();
        _previewFriendlyTargets = Array.Empty<Character>();
    }

    private void ShowDamagePreview()
    {
        HideDamagePreview();
        if (CurrentSkill == null)
            return;

        var entries = CurrentSkill.GetPreviewEffectEntries();
        if (entries == null || entries.Length == 0)
            return;

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        int panelIndex = 0;
        foreach (
            var group in entries
                .Where(entry => entry.Target != null && GodotObject.IsInstanceValid(entry.Target))
                .GroupBy(entry => entry.Target)
        )
        {
            var panel = GetOrCreateDamagePanel(layer, panelIndex++);
            PreviewEffectDisplay.ShowPanel(
                panel,
                group.ToArray(),
                GetTargetScreenPosition(group.Key),
                DamagePreviewLabelOffset
            );
        }

        for (int i = panelIndex; i < _previewDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamagePanels[i]))
                _previewDamagePanels[i].Visible = false;
        }
    }

    private void HideDamagePreview()
    {
        for (int i = 0; i < _previewDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamagePanels[i]))
                _previewDamagePanels[i].Visible = false;
        }
    }

    private void FreeDamagePreviewLabels()
    {
        for (int i = 0; i < _previewDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamagePanels[i]))
                _previewDamagePanels[i].QueueFree();
        }
        _previewDamagePanels.Clear();
    }

    private VBoxContainer GetOrCreateDamagePanel(CanvasLayer layer, int index)
    {
        while (_previewDamagePanels.Count <= index)
        {
            var panel = PreviewEffectDisplay.CreatePanel();
            layer.AddChild(panel);
            _previewDamagePanels.Add(panel);
        }

        var pooledPanel = _previewDamagePanels[index];
        if (!GodotObject.IsInstanceValid(pooledPanel))
        {
            pooledPanel = PreviewEffectDisplay.CreatePanel();
            layer.AddChild(pooledPanel);
            _previewDamagePanels[index] = pooledPanel;
        }
        else if (pooledPanel.GetParent() == null)
        {
            layer.AddChild(pooledPanel);
        }

        return pooledPanel;
    }

    private CanvasLayer EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
            return existingLayer;

        existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        root.AddChild(existingLayer);
        return existingLayer;
    }

    private Tip EnsureGlobalTooltip()
    {
        var layer = EnsureTipLayer();
        if (layer == null)
            return null;

        var tip = layer.GetNodeOrNull<Tip>("Tip");
        if (tip != null)
            return tip;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        tip = tipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        layer.AddChild(tip);
        return tip;
    }

    private static Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        return target.GetGlobalTransformWithCanvas().Origin;
    }

    private void ApplyConfiguredDisplayScale()
    {
        _baseScale = _configuredDisplayScale;
        Scale = _baseScale;
    }

    private void CacheBaseFontSizes()
    {
        _baseDescriptionFontSize = Description.GetThemeFontSize("normal_font_size");
        if (_baseDescriptionFontSize <= 0)
            _baseDescriptionFontSize = DefaultDescriptionFontSize;
    }

    private async void QueueAdjustTextSizes()
    {
        int version = ++_textAdjustVersion;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (!IsInsideTree() || version != _textAdjustVersion)
            return;

        AdjustDescriptionFont();
    }

    private void ApplyDescriptionFontSizing(bool force = false)
    {
        if (!AutoAdjustDescriptionTextSize)
        {
            _textAdjustVersion++;
            ApplyPreferredDescriptionFontSize();
            return;
        }

        ApplyPreferredDescriptionFontSize();
        if (force)
            _textAdjustVersion++;

        QueueAdjustTextSizes();
    }

    private void ApplyPreferredDescriptionFontSize()
    {
        int preferredFontSize = UserSettings.ScaleTextFontSize(_baseDescriptionFontSize);
        if (preferredFontSize <= 0)
            preferredFontSize = DefaultDescriptionFontSize;

        Description.AddThemeFontSizeOverride("normal_font_size", preferredFontSize);
    }

    private void AdjustDescriptionFont()
    {
        ApplyPreferredDescriptionFontSize();
        float availableHeight = Description.Size.Y;
        float availableWidth = Description.Size.X;
        if (availableHeight <= 0.0f || availableWidth <= 0.0f)
            return;

        int preferredFontSize = UserSettings.ScaleTextFontSize(_baseDescriptionFontSize);
        if (preferredFontSize <= 0)
            preferredFontSize = DefaultDescriptionFontSize;

        if (DoesDescriptionFitAtFontSize(preferredFontSize, availableWidth, availableHeight))
            return;

        for (int fontSize = preferredFontSize - 1; fontSize >= MinDescriptionFontSize; fontSize--)
        {
            Description.AddThemeFontSizeOverride("normal_font_size", fontSize);
            if (DoesDescriptionFitAtFontSize(fontSize, availableWidth, availableHeight))
                return;
        }

        Description.AddThemeFontSizeOverride("normal_font_size", MinDescriptionFontSize);
    }

    private bool DoesDescriptionFitAtFontSize(int fontSize, float availableWidth, float availableHeight)
    {
        string plainText = StripBbCodeTags(Description.Text);
        float estimatedHeight = EstimateDescriptionContentHeight(
            plainText,
            fontSize,
            availableWidth
        );
        return estimatedHeight <= availableHeight;
    }

    private static float EstimateDescriptionContentHeight(
        string text,
        int fontSize,
        float availableWidth
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0f;

        float unitsPerLine = Math.Max(1f, availableWidth / Math.Max(1f, fontSize * 0.92f));
        int lineCount = 0;
        foreach (string paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            float units = EstimateDescriptionTextUnits(paragraph);
            lineCount += Math.Max(1, Mathf.CeilToInt(units / unitsPerLine));
        }

        return lineCount * fontSize * 1.22f;
    }

    private static float EstimateDescriptionTextUnits(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        float units = 0f;
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch))
                units += 0.35f;
            else if (ch <= 0x007f)
                units += char.IsLetterOrDigit(ch) ? 0.55f : 0.35f;
            else
                units += 1f;
        }

        return units;
    }

    private static string StripBbCodeTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        char[] buffer = new char[text.Length];
        int count = 0;
        bool inTag = false;

        foreach (char ch in text)
        {
            if (ch == '[')
            {
                inTag = true;
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                buffer[count++] = ch;
        }

        return new string(buffer, 0, count);
    }

    private void ApplyRarityStyles(Skill.SkillRarity rarity)
    {
        EnsureStyleOverridesReady();

        Color borderColor = Skill.GetRarityBorderColor(rarity);
        Color badgeFill = WithAlpha(borderColor, 0.24f);
        Color footerFill = WithAlpha(borderColor, 0.16f);
        Color accentColor = WithAlpha(borderColor, 0.66f);
        Color accentGlow = WithAlpha(borderColor, 0.24f);
        Color diamondCore = WithAlpha(borderColor, 0.22f);
        Color outerBorder = WithAlpha(borderColor, 0.9f);
        Color innerBorder = WithAlpha(borderColor, 0.28f);

        if (_bgStyle != null)
        {
            _bgStyle.BorderColor = outerBorder;
            _bgStyle.BgColor = CardBaseColor;
        }
        if (_innerFrameStyle != null)
            _innerFrameStyle.BorderColor = innerBorder;
        if (_descriptionStyle != null)
        {
            _descriptionStyle.BorderColor = Colors.Transparent;
            _descriptionStyle.BgColor = DescriptionBaseColor;
        }
        if (_bg2Style != null)
        {
            _bg2Style.BorderColor = Colors.Transparent;
            _bg2Style.BgColor = FooterBaseColor;
        }
        if (_artFrameStyle != null)
        {
            _artFrameStyle.BorderColor = accentColor;
            _artFrameStyle.BgColor = ArtFrameBaseColor;
        }
        if (RarityPlate != null)
            RarityPlate.Color = badgeFill;
        if (EnergyPlate != null)
            EnergyPlate.Color = badgeFill;
        if (NamePlate != null)
            NamePlate.Color = accentGlow;
        if (CharacterPlate != null)
            CharacterPlate.Color = footerFill;
        if (CharacterName != null)
        {
            CharacterName.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.92f));
            CharacterName.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 1f));
        }
        if (ArtDiamondOuter != null)
            ArtDiamondOuter.Color = accentColor;
        if (ArtDiamondInner != null)
            ArtDiamondInner.Color = diamondCore;
        SetCardBeamColor(borderColor);

        if (TopAccent != null)
            TopAccent.Color = WithAlpha(borderColor, 0.58f);

        SetNativeFramePalette(borderColor, accentColor);
    }

    private void ApplyCharacterPlateStyle(Skill skill)
    {
        if (skill == null)
            return;

        if (!TryGetCharacterPlateColor(skill, out Color color))
            return;

        if (CharacterPlate != null)
            CharacterPlate.Color = WithAlpha(color, 0.42f);
        if (NamePlate != null)
            NamePlate.Color = WithAlpha(color, 0.18f);
        if (CharacterName != null)
        {
            CharacterName.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 0.96f));
            CharacterName.AddThemeColorOverride(
                "font_outline_color",
                WithAlpha(new Color(color.R * 0.18f, color.G * 0.18f, color.B * 0.18f, 1f), 0.95f)
            );
        }

        SetNativeFramePalette(Skill.GetRarityBorderColor(skill.Rarity), color);
    }

    private bool TryGetCharacterPlateColor(Skill skill, out Color color)
    {
        string key = ResolveCharacterColorKey(skill);
        color = key switch
        {
            "Echo" => EchoPlateColor,
            "Kasiya" => KasiyaPlateColor,
            "Mariya" => MariyaPlateColor,
            "Nightingale" => NightingalePlateColor,
            _ => default,
        };

        return !string.IsNullOrWhiteSpace(key)
            && key is "Echo" or "Kasiya" or "Mariya" or "Nightingale";
    }

    private string ResolveCharacterColorKey(Skill skill)
    {
        if (!string.IsNullOrWhiteSpace(PreviewCharacterKey))
            return NormalizeCharacterColorKey(PreviewCharacterKey);

        if (skill?.OwnerCharater is PlayerCharacter player && !string.IsNullOrWhiteSpace(player.CharacterKey))
            return NormalizeCharacterColorKey(player.CharacterKey);

        if (!string.IsNullOrWhiteSpace(skill?.OwnerCharater?.CharacterName))
            return NormalizeCharacterColorKey(skill.OwnerCharater.CharacterName);

        if (skill?.SkillId.HasValue == true && Skill.TryGetPlayerCharacterKey(skill.SkillId.Value, out var characterKey))
            return characterKey.ToString();

        if (!string.IsNullOrWhiteSpace(PreviewCharacterName))
            return NormalizeCharacterColorKey(PreviewCharacterName);

        return string.Empty;
    }

    private static string NormalizeCharacterColorKey(string value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized switch
        {
            "Echo" or "回声" => "Echo",
            "Kasiya" or "卡西亚" => "Kasiya",
            "Mariya" or "玛瑞娅" => "Mariya",
            "Nightingale" or "夜莺" => "Nightingale",
            _ => normalized,
        };
    }

    private void ApplyStatusCardStyle()
    {
        EnsureStyleOverridesReady();

        Color borderColor = new(0.76f, 0.58f, 1f, 1f);
        Color accentColor = new(0.52f, 0.34f, 0.95f, 0.7f);
        Color surfaceColor = new(0.055f, 0.045f, 0.09f, 0.96f);

        if (_bgStyle != null)
        {
            _bgStyle.BorderColor = borderColor;
            _bgStyle.BgColor = new Color(0.035f, 0.026f, 0.06f, 0.86f);
        }
        if (_innerFrameStyle != null)
            _innerFrameStyle.BorderColor = new Color(0.82f, 0.7f, 1f, 0.34f);
        if (_descriptionStyle != null)
        {
            _descriptionStyle.BorderColor = Colors.Transparent;
            _descriptionStyle.BgColor = surfaceColor;
        }
        if (_bg2Style != null)
        {
            _bg2Style.BorderColor = Colors.Transparent;
            _bg2Style.BgColor = new Color(0.04f, 0.032f, 0.075f, 0.98f);
        }
        if (_artFrameStyle != null)
        {
            _artFrameStyle.BorderColor = accentColor;
            _artFrameStyle.BgColor = new Color(0.045f, 0.036f, 0.08f, 0.68f);
        }
        if (RarityPlate != null)
            RarityPlate.Color = new Color(0.62f, 0.42f, 1f, 0.3f);
        if (EnergyPlate != null)
            EnergyPlate.Color = new Color(0.62f, 0.42f, 1f, 0.3f);
        if (NamePlate != null)
            NamePlate.Color = new Color(0.62f, 0.42f, 1f, 0.26f);
        if (CharacterPlate != null)
            CharacterPlate.Color = new Color(0.48f, 0.31f, 0.86f, 0.22f);
        if (ArtDiamondOuter != null)
            ArtDiamondOuter.Color = new Color(0.72f, 0.54f, 1f, 0.5f);
        if (ArtDiamondInner != null)
            ArtDiamondInner.Color = new Color(0.88f, 0.76f, 1f, 0.22f);
        if (TopAccent != null)
            TopAccent.Color = new Color(0.86f, 0.72f, 1f, 0.72f);

        SetCardBeamColor(borderColor);
        SetNativeFramePalette(borderColor, accentColor);
    }

    private static bool IsStatusCard(Skill skill) => skill?.IsStatusCard == true;

    private void EnsureStyleOverridesReady()
    {
        if (_bgStyle == null)
        {
            _bgStyle = BG.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
            if (_bgStyle != null)
                BG.AddThemeStyleboxOverride("panel", _bgStyle);
        }

        if (_innerFrameStyle == null)
        {
            _innerFrameStyle = InnerFrame.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
            if (_innerFrameStyle != null)
                InnerFrame.AddThemeStyleboxOverride("panel", _innerFrameStyle);
        }

        if (_descriptionStyle == null)
        {
            _descriptionStyle = Description.GetThemeStylebox("normal")?.Duplicate() as StyleBoxFlat;
            if (_descriptionStyle != null)
                Description.AddThemeStyleboxOverride("normal", _descriptionStyle);
        }

        if (_bg2Style == null)
        {
            _bg2Style = BG2.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
            if (_bg2Style != null)
                BG2.AddThemeStyleboxOverride("panel", _bg2Style);
        }

        if (_artFrameStyle == null)
        {
            _artFrameStyle = ArtFrame.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
            if (_artFrameStyle != null)
                ArtFrame.AddThemeStyleboxOverride("panel", _artFrameStyle);
        }
    }

    private void SetCardBeamColor(Color color)
    {
        if (CardEffectMaterialTarget.Material is not ShaderMaterial shader)
            return;

        shader.SetShaderParameter("beam_color", new Color(color.R, color.G, color.B, 1f));
    }

    private void SetNativeFramePalette(Color lineColor, Color accentColor)
    {
        if (NativeFrame == null || !GodotObject.IsInstanceValid(NativeFrame))
            return;

        Color strongLine = WithAlpha(lineColor, 0.82f);
        Color softLine = WithAlpha(lineColor, 0.24f);
        Color accent = WithAlpha(accentColor, 0.74f);
        Color accentSoft = WithAlpha(accentColor, 0.48f);
        Color transparentAccent = WithAlpha(accentColor, 0f);
        Color recessShadow = new(0.004f, 0.012f, 0.024f, 0.78f);

        foreach (Node child in NativeFrame.GetChildren())
        {
            string nodeName = child.Name.ToString();
            if (child is Line2D line)
            {
                if (line.Gradient != null)
                {
                    if (nodeName.Contains("Left"))
                    {
                        line.Gradient.SetColor(0, transparentAccent);
                        line.Gradient.SetColor(1, accentSoft);
                    }
                    else if (nodeName.Contains("Right"))
                    {
                        line.Gradient.SetColor(0, accentSoft);
                        line.Gradient.SetColor(1, transparentAccent);
                    }
                }

                line.DefaultColor = nodeName.Contains("Shadow")
                    ? recessShadow
                    : nodeName.Contains("Recess")
                        ? accent
                        : nodeName.Contains("Soft")
                    ? softLine
                    : nodeName.Contains("Accent")
                        ? accentSoft
                        : strongLine;
            }
            else if (child is Polygon2D polygon)
            {
                polygon.Color = nodeName.Contains("Inner")
                    ? WithAlpha(lineColor, 0.72f)
                    : accent;
            }
        }
    }

    private static float GetShaderParameterFloat(ShaderMaterial shader, string parameterName)
    {
        if (shader == null)
            return 0f;

        Variant value = shader.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }

    private static Color BlendSurface(Color baseColor, Color accentColor, float accentAmount)
    {
        return new Color(
            Mathf.Lerp(baseColor.R, accentColor.R, accentAmount),
            Mathf.Lerp(baseColor.G, accentColor.G, accentAmount),
            Mathf.Lerp(baseColor.B, accentColor.B, accentAmount),
            baseColor.A
        );
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.R, color.G, color.B, alpha);
    }
}
