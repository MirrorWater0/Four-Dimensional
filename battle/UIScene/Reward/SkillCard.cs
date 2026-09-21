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
    public float RotationSpinTurns { get; init; }
    public Action OnArrival { get; init; }
}

public partial class SkillCard : Control
{
    private const int DefaultDescriptionFontSize = 17;
    private const int MinDescriptionFontSize = 8;
    private const string EnergyCostNumberColor = "#e8ebee";
    private const string EnergyCostInsufficientNumberColor = "#858b92";
    private const float OrnateFrameBrightness = 1.56f;
    private static readonly Vector2 CardBaseSize = new(240f, 370f);

    public Control CardVisualScaleHost => field ??= GetNode<Control>("VisualTransform");
    public Panel BG => field ??= GetNode<Panel>("VisualTransform/SubViewport/BG");
    public Panel InnerFrame => field ??= GetNode<Panel>("VisualTransform/SubViewport/InnerFrame");
    public RichTextLabel Description =>
        field ??= GetNode<RichTextLabel>("VisualTransform/SubViewport/Description");
    public Label NameLabel => field ??= GetNode<Label>("VisualTransform/SubViewport/NameLabel");
    public Button Button => field ??= GetNode<Button>("VisualTransform/SubViewport/Button");
    public CanvasGroup CardVisualRoot =>
        field ??= GetNodeOrNull<CanvasGroup>("VisualTransform/SubViewport");
    public TextureRect SkillPicture =>
        field ??= GetNodeOrNull<TextureRect>("VisualTransform/SubViewport/ArtFrame/SkillPicture");
    public TextureRect SkillIcon =>
        field ??= GetNode<TextureRect>("VisualTransform/SubViewport/ArtFrame/SkillIcon");
    public Panel HoverHint => field ??= GetNode<Panel>("VisualTransform/SubViewport/HoverHint");
    public Panel BG2 => field ??= GetNode<Panel>("VisualTransform/SubViewport/BG2");
    public Panel ArtFrame => field ??= GetNode<Panel>("VisualTransform/SubViewport/ArtFrame");
    public Control RarityBadge =>
        field ??= GetNode<Control>("VisualTransform/SubViewport/RarityBadge");
    public Control EnergyBadge =>
        field ??= GetNode<Control>("VisualTransform/SubViewport/EnergyBadge");
    public Label TypeLabel =>
        field ??= GetNode<Label>("VisualTransform/SubViewport/TypeBadge/TypeLabel");
    public Label CharacterName =>
        field ??= GetNode<Label>("VisualTransform/SubViewport/CharacterName");
    public RichTextLabel EnergyCost =>
        field ??= GetNode<RichTextLabel>("VisualTransform/SubViewport/EnergyBadge/EnergyCost");
    public ColorRect ArtFill =>
        field ??= GetNodeOrNull<ColorRect>("VisualTransform/SubViewport/ArtFrame/ArtFill");
    public ColorRect ArtBandTop =>
        field ??= GetNodeOrNull<ColorRect>("VisualTransform/SubViewport/ArtFrame/ArtBandTop");
    public ColorRect TopAccent =>
        field ??= GetNodeOrNull<ColorRect>("VisualTransform/SubViewport/TopAccent");
    public ColorRect CharacterAccent =>
        field ??= GetNodeOrNull<ColorRect>("VisualTransform/SubViewport/CharacterAccent");
    public TextureRect OrnateFrame =>
        field ??= GetNodeOrNull<TextureRect>("VisualTransform/SubViewport/OrnateFrame");
    public ColorRect ArtDiamondOuter =>
        field ??= GetNodeOrNull<ColorRect>(
            "VisualTransform/SubViewport/ArtFrame/ArtDiamondOuter"
        );
    public ColorRect ArtDiamondInner =>
        field ??= GetNodeOrNull<ColorRect>(
            "VisualTransform/SubViewport/ArtFrame/ArtDiamondInner"
        );
    public Node2D NativeFrame =>
        field ??= GetNodeOrNull<Node2D>("VisualTransform/SubViewport/NativeFrame");
    public Polygon2D RarityPlate =>
        field ??= GetNodeOrNull<Polygon2D>("VisualTransform/SubViewport/RarityPlate");
    public Polygon2D EnergyPlate =>
        field ??= GetNodeOrNull<Polygon2D>("VisualTransform/SubViewport/EnergyPlate");
    public Polygon2D NamePlate =>
        field ??= GetNodeOrNull<Polygon2D>("VisualTransform/SubViewport/BG2/NamePlate");
    public Polygon2D CharacterPlate =>
        field ??= GetNodeOrNull<Polygon2D>("VisualTransform/SubViewport/BG2/CharacterPlate");
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
    public bool SuppressRelatedCardPreview { get; private set; }
    public bool AutoAdjustDescriptionTextSize { get; set; } = true;
    public bool IsPlayableHighlightEnabled => _playableHighlightEnabled;
    public Vector2 ConfiguredDisplayScale => _configuredDisplayScale;
    public float PointerHoverScaleMultiplier { get; set; } = 1.08f;

    private Tween _progressTween;
    private Tween _pressTween;
    private Tween _hoverTween;
    private Tween _rejectShakeTween;
    private Tween _visualLayoutTween;
    private Tween _motionTween;
    private Tween _drawSettleTween;
    private Tween _playableHighlightTween;
    private bool _transientPointerInputDisabled;
    private bool _transientPointerInputPreviousButtonDisabled;
    private bool _transientPointerInputPreviousHoverUiEnabled = true;
    private Vector2? _battleMotionTargetPosition;
    private Vector2? _battleMotionTargetScale;
    private int _baseDescriptionFontSize;
    private int _textAdjustVersion;
    private Vector2 _baseScale = Vector2.One;
    // Selection can enlarge VisualTransform while the SkillCard root stays in its hand slot.
    // Pointer hover multiplies this baseline instead of replacing it.
    private Vector2 _visualTransformBaseScale = Vector2.One;
    private Vector2 _visualTransformHoverScale = Vector2.One;
    private Vector2 _configuredDisplayScale = Vector2.One;
    private bool _pilePreviewVisualsActive;
    private const int PilePreviewHoverZIndex = 12;
    private const float DefaultCanvasFitMargin = 32f;
    private ShaderMaterial _defaultCardMaterial;
    private ShaderMaterial _playableHighlightMaterial;
    private ColorRect _playableHighlight;
    private float _playableHighlightWidth = 0.2f;
    private bool _playableHighlightEnabled;
    private CanvasItem _cardEffectMaterialTarget;
    private Character[] _previewHostileTargets = Array.Empty<Character>();
    private Character[] _previewFriendlyTargets = Array.Empty<Character>();
    private bool _energyCostAffordable = true;
    private Label _handIndexLabel;
    private readonly List<VBoxContainer> _previewDamagePanels = new();
    private readonly List<Character> _previewDamageTargetsBuffer = new();
    private readonly Dictionary<Character, List<Skill.PreviewEffectEntry>> _previewDamageEntriesByTarget =
        new();
    private static readonly Color HostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color FriendlyTargetPreviewColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Color ExhaustFadeModulate = new(0.1f, 0.1f, 0.1f, 0f);
    private const int KeywordTooltipHoverDelayMs = 70;
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
    private SkillRelatedCardPreview _relatedCardPreview;
    private Skill _cachedHoverSkill;
    private Skill _activeSkillPreviewSkill;
    private Skill _cachedSkillPreviewSkill;
    private int _cachedSkillPreviewRevision = int.MinValue;
    private int _cachedHoverPreviewRevision = int.MinValue;
    private bool _skillPreviewActive;
    private bool _preserveSkillPreviewOnNextExitTree;
    private int _keywordTooltipHoverVersion;
    private bool _pointerHoverActive;
    private string _cachedKeywordTooltipText = string.Empty;
    private IReadOnlyList<SkillID> _cachedRelatedSkillIds = Array.Empty<SkillID>();
    private Character[] _cachedPreviewHostileTargets = Array.Empty<Character>();
    private Character[] _cachedPreviewFriendlyTargets = Array.Empty<Character>();
    private Skill.PreviewEffectEntry[] _cachedPreviewEffectEntries =
        Array.Empty<Skill.PreviewEffectEntry>();
    private CardPreviewTargetEffectGroup[] _cachedPreviewEffectGroups =
        Array.Empty<CardPreviewTargetEffectGroup>();
    private int _debugSkillPreviewShowCalls;
    private int _debugSkillPreviewShowSkipped;
    private int _debugSkillPreviewCacheBuilds;
    private Tip KeywordTooltip => _keywordTooltip ??= EnsureGlobalTooltip();
    private SkillRelatedCardPreview RelatedCardPreview =>
        _relatedCardPreview ??= EnsureRelatedCardPreview();

    public override void _Ready()
    {
        CacheDefaultCardMaterial();
        CacheBaseFontSizes();
        EnsurePlayableHighlight();
        SetPlayableHighlight(false, instant: true);
        ApplySkillToUi();
        HoverHint.Visible = false;
        ApplyConfiguredDisplayScale();
        PivotOffsetRatio = new Vector2(0.5f, 0.5f);
        ResetCardVisualHoverTransform();
        Button.MouseEntered += () => ApplyPointerHoverState(true);
        Button.MouseExited += () => ApplyPointerHoverState(false);
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

    public void ConfigurePilePreviewVisuals()
    {
        _pilePreviewVisualsActive = true;
        ClipContents = false;
        if (CardVisualRoot != null)
        {
            CardVisualRoot.FitMargin = 0f;
            CardVisualRoot.ClearMargin = 0f;
        }
    }

    private void RestoreDefaultPilePreviewVisuals()
    {
        if (!_pilePreviewVisualsActive)
            return;

        _pilePreviewVisualsActive = false;
        if (CardVisualRoot != null)
        {
            CardVisualRoot.FitMargin = DefaultCanvasFitMargin;
            CardVisualRoot.ClearMargin = DefaultCanvasFitMargin;
        }
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

        RestoreDefaultPilePreviewVisuals();
        SetCardVisualVisible(true);
        ResetCardVisualHoverTransform();
        PointerHoverScaleMultiplier = 1.08f;
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
        RestoreDisplayState(true);
    }

    public void RestoreDisplayState(bool resetPlayableHighlight)
    {
        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();
        _motionTween?.Kill();
        _drawSettleTween?.Kill();
        if (resetPlayableHighlight)
        {
            _playableHighlightTween?.Kill();
            SetPlayableHighlight(false, instant: true);
        }

        RestoreDefaultPilePreviewVisuals();
        SetCardVisualVisible(true);
        ResetCardVisualHoverTransform();
        PointerHoverScaleMultiplier = 1.08f;
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

    public void SetHandIndexBadge(int index, bool visible)
    {
        Label label = EnsureHandIndexLabel();
        if (label == null)
            return;

        label.Text = index.ToString();
        label.Visible = visible && index > 0;
    }

    private Label EnsureHandIndexLabel()
    {
        if (_handIndexLabel != null && GodotObject.IsInstanceValid(_handIndexLabel))
            return _handIndexLabel;

        _handIndexLabel = GetNodeOrNull<Label>("VisualTransform/SubViewport/HandIndexLabel");
        return _handIndexLabel;
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
        Scale = _baseScale * 1.018f;
        _drawSettleTween = CreateTween();
        if (delay > 0f)
            _drawSettleTween.TweenInterval(delay);

        _drawSettleTween
            .TweenProperty(this, "scale", _baseScale * 0.998f, 0.055f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _drawSettleTween
            .TweenProperty(this, "scale", _baseScale, 0.07f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void SetSkill(Skill skill)
    {
        if (skill == null && CurrentSkill == null)
        {
            ApplySkillToUi();
            return;
        }

        if (ReferenceEquals(CurrentSkill, skill))
        {
            RefreshCurrentSkillDynamicText();
            RefreshSkillPreviewIfStale();
            return;
        }

        CurrentSkill = skill;
        InvalidateHoverPreviewCache();
        ApplySkillToUi();
    }

    public void InvalidateHoverPreviewCache()
    {
        _cachedHoverSkill = null;
        InvalidateSkillPreviewState();
        _cachedKeywordTooltipText = string.Empty;
        _cachedRelatedSkillIds = Array.Empty<SkillID>();
        _keywordTooltip?.HideTooltip();
    }

    private void InvalidateSkillPreviewState()
    {
        _activeSkillPreviewSkill = null;
        _cachedSkillPreviewSkill = null;
        _cachedSkillPreviewRevision = int.MinValue;
        _cachedPreviewHostileTargets = Array.Empty<Character>();
        _cachedPreviewFriendlyTargets = Array.Empty<Character>();
        _cachedPreviewEffectEntries = Array.Empty<Skill.PreviewEffectEntry>();
        _cachedPreviewEffectGroups = Array.Empty<CardPreviewTargetEffectGroup>();
    }

    private int GetCurrentPreviewCacheRevision() =>
        CurrentSkill?.ComputePreviewCacheRevision() ?? 0;

    public void RefreshSkillPreviewIfStale()
    {
        if (CurrentSkill == null)
            return;

        int revision = GetCurrentPreviewCacheRevision();
        bool cacheFresh =
            ReferenceEquals(_cachedSkillPreviewSkill, CurrentSkill)
            && _cachedSkillPreviewRevision == revision;
        bool previewWasActive = _skillPreviewActive;

        if (!cacheFresh)
            InvalidateSkillPreviewState();

        if (
            !ReferenceEquals(_cachedHoverSkill, CurrentSkill)
            || _cachedHoverPreviewRevision != revision
        )
        {
            _cachedHoverSkill = null;
            _cachedHoverPreviewRevision = int.MinValue;
            _cachedKeywordTooltipText = string.Empty;
            _cachedRelatedSkillIds = Array.Empty<SkillID>();
        }

        if (previewWasActive)
            ShowSkillPreview();
    }

    private void RefreshCurrentSkillDynamicText()
    {
        if (!IsInsideTree() || CurrentSkill == null)
            return;

        CurrentSkill.UpdateDescription();
        string displayName = DisplayNameOverride ?? CurrentSkill.SkillName ?? string.Empty;
        string descriptionText = CurrentSkill.Description ?? string.Empty;
        string centeredEnergyText = BuildEnergyCostText(CurrentSkill, _energyCostAffordable);
        bool textChanged = false;
        if (NameLabel.Text != displayName)
        {
            NameLabel.Text = displayName;
            textChanged = true;
        }
        if (Description.Text != descriptionText)
        {
            Description.Text = descriptionText;
            textChanged = true;
        }
        if (EnergyCost.Text != centeredEnergyText)
            EnergyCost.Text = centeredEnergyText;

        if (textChanged)
            ApplyDescriptionFontSizing();
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
            ApplyCardAccentColors(null);
            ApplyPreferredDescriptionFontSize();
            return;
        }

        CurrentSkill.UpdateDescription();
        _energyCostAffordable = true;
        bool isStatusCard = IsStatusCard(CurrentSkill);
        bool isColorlessCard = CurrentSkill.IsColorless;
        string displayName = DisplayNameOverride ?? CurrentSkill.SkillName ?? string.Empty;
        string characterName = isStatusCard
            ? string.Empty
            : isColorlessCard
                ? I18n.Tr("keyword.colorless", "无色")
            : PreviewCharacterName ?? CurrentSkill.OwnerCharater?.CharacterName ?? string.Empty;
        string skillTypeText = isStatusCard
            ? I18n.Tr("ui.encyclopedia.skill_type.status", "状态")
            : CurrentSkill.SkillType.GetDescription();
        string descriptionText = CurrentSkill.Description ?? string.Empty;
        string centeredEnergyText = BuildEnergyCostText(CurrentSkill, _energyCostAffordable);
        ApplyCardAccentColors(CurrentSkill);
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

    private void ApplyCardAccentColors(Skill skill)
    {
        Color characterColor = ResolveCharacterAccentColor(skill);
        Color rarityColor = skill == null
            ? new Color(0.58f, 0.6f, 0.64f, 1f)
            : Skill.GetRarityBorderColor(skill.Rarity);

        ApplyAccentModulate(CharacterAccent, characterColor);
        ApplyAccentFontColor(CharacterName, characterColor.Lerp(Colors.White, 0.38f));
        ApplyAccentModulate(OrnateFrame, GetOrnateFrameColor(rarityColor));
        ApplyAccentModulate(RarityPlate, rarityColor);
        ApplyAccentModulate(EnergyPlate, GetMutedAccentColor(characterColor, 0.52f));
        ApplyAccentModulate(NamePlate, GetMutedAccentColor(characterColor, 0.72f));
        ApplyAccentModulate(CharacterPlate, GetMutedAccentColor(characterColor, 0.52f));
        ApplyAccentModulate(ArtBandTop, GetMutedAccentColor(characterColor, 0.84f));
        ApplyAccentModulate(TopAccent, GetMutedAccentColor(rarityColor, 0.72f));
    }

    private static Color GetMutedAccentColor(Color color, float alpha)
    {
        return new Color(
            Mathf.Lerp(0.42f, color.R, 0.7f),
            Mathf.Lerp(0.48f, color.G, 0.7f),
            Mathf.Lerp(0.56f, color.B, 0.7f),
            alpha
        );
    }

    private static Color GetOrnateFrameColor(Color rarityColor)
    {
        return new Color(
            rarityColor.R * OrnateFrameBrightness,
            rarityColor.G * OrnateFrameBrightness,
            rarityColor.B * OrnateFrameBrightness,
            rarityColor.A
        );
    }

    private Color ResolveCharacterAccentColor(Skill skill)
    {
        if (skill?.IsColorless == true)
            return CharacterPlateColors.ColorlessColor;

        string ownerKey = skill?.OwnerCharater is PlayerCharacter player
            ? player.CharacterKey
            : string.Empty;
        string ownerName = skill?.OwnerCharater?.CharacterName;
        string[] candidates = [PreviewCharacterKey, ownerKey, PreviewCharacterName, ownerName];
        foreach (string candidate in candidates)
        {
            if (CharacterPlateColors.TryGetColor(candidate, out Color color))
                return color;
        }

        return new Color(0.58f, 0.6f, 0.64f, 1f);
    }

    private static void ApplyAccentModulate(CanvasItem item, Color color)
    {
        if (item != null && GodotObject.IsInstanceValid(item) && item.Modulate != color)
            item.Modulate = color;
    }

    private static void ApplyAccentFontColor(Label label, Color color)
    {
        if (
            label != null
            && GodotObject.IsInstanceValid(label)
            && label.GetThemeColor("font_color") != color
        )
            label.AddThemeColorOverride("font_color", color);
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
        _debugSkillPreviewShowCalls++;
        int revision = GetCurrentPreviewCacheRevision();
        if (
            _skillPreviewActive
            && ReferenceEquals(_activeSkillPreviewSkill, CurrentSkill)
            && _cachedSkillPreviewRevision == revision
        )
        {
            _debugSkillPreviewShowSkipped++;
            return;
        }

        HideSkillPreview();
        if (CurrentSkill == null)
            return;

        _skillPreviewActive = true;
        _activeSkillPreviewSkill = CurrentSkill;
        ShowTargetPreview();
        ShowDamagePreview();
    }

    public void HideSkillPreview()
    {
        HideDamagePreview();
        HideTargetPreview();
        _skillPreviewActive = false;
        _activeSkillPreviewSkill = null;
    }

    public void PreserveSkillPreviewOnNextReparent()
    {
        _preserveSkillPreviewOnNextExitTree = true;
    }

    public Dictionary<string, object> GetDebugSkillPreviewState()
    {
        return new Dictionary<string, object>
        {
            ["hasSkill"] = CurrentSkill != null,
            ["skillId"] = CurrentSkill?.SkillId?.ToString() ?? string.Empty,
            ["skillName"] = CurrentSkill?.SkillName ?? string.Empty,
            ["previewActive"] = _skillPreviewActive,
            ["activeSkillMatchesCurrent"] = ReferenceEquals(_activeSkillPreviewSkill, CurrentSkill),
            ["cachedSkillMatchesCurrent"] = ReferenceEquals(_cachedSkillPreviewSkill, CurrentSkill),
            ["cachedPreviewRevision"] = _cachedSkillPreviewRevision,
            ["currentPreviewRevision"] = GetCurrentPreviewCacheRevision(),
            ["cachedHostileTargets"] = _cachedPreviewHostileTargets?.Length ?? 0,
            ["cachedFriendlyTargets"] = _cachedPreviewFriendlyTargets?.Length ?? 0,
            ["cachedEffectEntries"] = _cachedPreviewEffectEntries?.Length ?? 0,
            ["cachedEffectGroups"] = _cachedPreviewEffectGroups?.Length ?? 0,
            ["showCalls"] = _debugSkillPreviewShowCalls,
            ["showSkipped"] = _debugSkillPreviewShowSkipped,
            ["cacheBuilds"] = _debugSkillPreviewCacheBuilds,
        };
    }

    public void SetPlayableHighlight(bool enabled, bool instant = false)
    {
        ColorRect highlight = EnsurePlayableHighlight();
        if (highlight == null || _playableHighlightMaterial == null)
            return;

        float target = enabled ? _playableHighlightWidth : 0f;
        if (_playableHighlightEnabled == enabled && !instant)
        {
            if (highlight.Visible != enabled)
                highlight.Visible = enabled;
            return;
        }

        _playableHighlightEnabled = enabled;
        _playableHighlightTween?.Kill();
        if (!enabled && !highlight.Visible)
        {
            _playableHighlightMaterial.SetShaderParameter("width", 0f);
            return;
        }

        if (enabled && !highlight.Visible && !instant && IsInsideTree())
            _playableHighlightMaterial.SetShaderParameter("width", 0f);

        if (instant || !IsInsideTree())
        {
            _playableHighlightMaterial.SetShaderParameter("width", target);
            highlight.Visible = enabled;
            return;
        }

        highlight.Visible = true;
        float current = GetShaderParameterFloat(_playableHighlightMaterial, "width");
        _playableHighlightTween = CreateTween();
        _playableHighlightTween
            .TweenMethod(
                Callable.From<float>(
                    value => _playableHighlightMaterial.SetShaderParameter("width", value)
                ),
                current,
                target,
                enabled ? 0.5f : 0.5f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        if (!enabled)
            _playableHighlightTween.TweenCallback(Callable.From(() => highlight.Visible = false));
    }

    public void CopyPlayableHighlightFrom(SkillCard source)
    {
        if (source == null || !GodotObject.IsInstanceValid(source))
            return;

        ColorRect sourceHighlight = source.EnsurePlayableHighlight();
        ColorRect highlight = EnsurePlayableHighlight();
        if (
            sourceHighlight == null
            || highlight == null
            || source._playableHighlightMaterial == null
            || _playableHighlightMaterial == null
        )
        {
            return;
        }

        _playableHighlightTween?.Kill();
        _playableHighlightEnabled = source._playableHighlightEnabled;
        highlight.Visible = sourceHighlight.Visible;
        _playableHighlightMaterial.SetShaderParameter(
            "width",
            GetShaderParameterFloat(source._playableHighlightMaterial, "width")
        );
    }

    public void SetHoverUiEnabled(bool enabled)
    {
        HoverUiEnabled = enabled;
        if (!enabled)
            ApplyPointerHoverState(false, instant: true);
    }

    /// <summary>
    /// Updates the visual-only hover layer without moving the SkillCard layout root.
    /// This explicit path is also used by battle cards, whose automatic card hover is disabled.
    /// </summary>
    public void SetCenteredPointerHoverState(bool hovered, bool instant = false)
    {
        if (hovered)
        {
            if (
                !HoverUiEnabled
                || Button.Disabled
                || Input.IsMouseButtonPressed(MouseButton.Left)
            )
            {
                return;
            }

            ApplyPointerHoverState(true, instant);
            TweenPointerHoverScale(
                Vector2.One * PointerHoverScaleMultiplier,
                instant,
                force: true
            );
            return;
        }

        ApplyPointerHoverState(false, instant);
        TweenPointerHoverScale(Vector2.One, instant, force: true);
    }

    /// <summary>Restores a selected card's explicit hover after layout/input refreshes.</summary>
    public void EnsureCenteredPointerHoverState(bool instant = false)
    {
        Vector2 targetHoverScale = Vector2.One * PointerHoverScaleMultiplier;
        if (
            _pointerHoverActive
            && HoverHint.Visible
            && _visualTransformHoverScale.DistanceSquaredTo(targetHoverScale) < 0.000001f
        )
        {
            return;
        }

        SetCenteredPointerHoverState(true, instant);
    }

    public void SetTransientPointerInputDisabled(
        bool disabled,
        bool refreshHoverWhenEnabled = false
    )
    {
        if (disabled)
        {
            if (!_transientPointerInputDisabled)
            {
                _transientPointerInputPreviousButtonDisabled = Button.Disabled;
                _transientPointerInputPreviousHoverUiEnabled = HoverUiEnabled;
            }

            _transientPointerInputDisabled = true;
            Button.Disabled = true;
            HoverUiEnabled = false;
            ApplyPointerHoverState(false, instant: true);
            return;
        }

        if (_transientPointerInputDisabled)
        {
            Button.Disabled = _transientPointerInputPreviousButtonDisabled;
            HoverUiEnabled = _transientPointerInputPreviousHoverUiEnabled;
            _transientPointerInputDisabled = false;
        }

        if (refreshHoverWhenEnabled)
            RefreshPointerHoverStateFromMouse();
    }

    public void RefreshPointerHoverStateFromMouse()
    {
        if (
            !HoverUiEnabled
            || Button.Disabled
            || Input.IsMouseButtonPressed(MouseButton.Left)
            || !IsInsideTree()
        )
        {
            ApplyPointerHoverState(false, instant: true);
            return;
        }

        ApplyPointerHoverState(Button.IsHovered());
    }

    public void SetRelatedCardPreviewSuppressed(bool suppressed)
    {
        SuppressRelatedCardPreview = suppressed;
        if (suppressed)
            _relatedCardPreview?.HidePreviews();
    }

    public void HideHoverUi()
    {
        _keywordTooltipHoverVersion++;
        _pointerHoverActive = false;
        HoverHint.Visible = false;
        _keywordTooltip?.HideTooltip();
        _relatedCardPreview?.HidePreviews();
    }

    private void ApplyPointerHoverState(bool hovered, bool instant = false)
    {
        if (hovered)
        {
            if (
                !HoverUiEnabled
                || Button.Disabled
                || Input.IsMouseButtonPressed(MouseButton.Left)
            )
            {
                return;
            }

            if (_pointerHoverActive && !instant)
                return;

            _pointerHoverActive = true;
            ClearStaleSiblingPilePreviewHoverStates();
            HoverHint.Visible = true;
            ScheduleKeywordTooltip();
            TweenPointerHoverScale(Vector2.One * PointerHoverScaleMultiplier, instant);
            if (_pilePreviewVisualsActive)
                ZIndex = PilePreviewHoverZIndex;
            return;
        }

        _keywordTooltipHoverVersion++;
        _pointerHoverActive = false;
        HideHoverUi();
        TweenPointerHoverScale(Vector2.One, instant);
        if (_pilePreviewVisualsActive)
            ZIndex = 0;
    }

    private void ScheduleKeywordTooltip()
    {
        int version = ++_keywordTooltipHoverVersion;
        _ = ShowKeywordTooltipDelayed(version);
    }

    private async Task ShowKeywordTooltipDelayed(int version)
    {
        if (KeywordTooltipHoverDelayMs > 0)
        {
            SceneTree tree = GetTree();
            if (tree == null)
                return;

            await ToSignal(
                tree.CreateTimer(KeywordTooltipHoverDelayMs / 1000.0f),
                SceneTreeTimer.SignalName.Timeout
            );
        }

        if (
            version != _keywordTooltipHoverVersion
            || !_pointerHoverActive
            || !HoverUiEnabled
            || Button.Disabled
            || Input.IsMouseButtonPressed(MouseButton.Left)
            || !IsInsideTree()
        )
        {
            return;
        }

        ShowKeywordTooltip();
    }

    private void ClearStaleSiblingPilePreviewHoverStates()
    {
        if (!_pilePreviewVisualsActive || Button == null || !IsInsideTree())
            return;

        Node holder = GetParent();
        Node grid = holder?.GetParent();
        if (grid == null)
            return;

        Vector2 mousePosition = GetGlobalMousePosition();
        foreach (Node siblingHolder in grid.GetChildren())
        {
            if (siblingHolder == holder)
                continue;

            SkillCard card = FindSkillCardChild(siblingHolder);
            if (
                card == null
                || !GodotObject.IsInstanceValid(card)
                || ReferenceEquals(card, this)
                || !card._pilePreviewVisualsActive
            )
            {
                continue;
            }

            if (card.Button != null && card.Button.GetGlobalRect().HasPoint(mousePosition))
                continue;

            card.ApplyPointerHoverState(false, instant: true);
        }
    }

    private static SkillCard FindSkillCardChild(Node node)
    {
        if (node == null)
            return null;

        for (int i = 0; i < node.GetChildCount(); i++)
        {
            if (node.GetChild(i) is SkillCard card)
                return card;
        }

        return null;
    }

    private void TweenPointerHoverScale(Vector2 targetScale, bool instant, bool force = false)
    {
        if (!UseDefaultHoverEffect && !force)
            return;

        _visualTransformHoverScale = targetScale;
        TweenCenteredScale(
            _visualTransformBaseScale * targetScale,
            GetPointerHoverScaleDuration(targetScale),
            Tween.TransitionType.Cubic,
            GetPointerHoverScaleEase(targetScale),
            instant
        );
    }

    /// <summary>
    /// The single entry point for interactive card scaling. SkillCard stays as the layout frame;
    /// the VisualTransform Control is the sole card-wide transform node.
    /// </summary>
    public Tween TweenCenteredScale(
        Vector2 targetScale,
        float duration,
        Tween.TransitionType transition = Tween.TransitionType.Cubic,
        Tween.EaseType ease = Tween.EaseType.Out,
        bool instant = false,
        bool useGlobalScale = false
    )
    {
        _hoverTween?.Kill();

        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return null;

        EnsureVisualTransformPivot(visualScaleHost);
        Vector2 resolvedScale = useGlobalScale
            ? GetVisualTransformLocalScaleFromGlobalScale(visualScaleHost, targetScale)
            : targetScale;
        if (useGlobalScale)
            _visualTransformBaseScale = resolvedScale;

        if (instant || duration <= 0f || !IsInsideTree())
        {
            visualScaleHost.Scale = resolvedScale;
            return null;
        }

        _hoverTween = CreateTween();
        _hoverTween.SetParallel(true);
        _hoverTween
            .TweenProperty(
                visualScaleHost,
                "scale",
                resolvedScale,
                duration
            )
            .SetTrans(transition)
            .SetEase(ease);
        return _hoverTween;
    }

    /// <summary>
    /// Moves a card's visual layer to a selection layout without moving the SkillCard root out
    /// of its hand slot. The layout scale becomes the new baseline for pointer hover.
    /// </summary>
    public Tween TweenCenteredVisualTransformLayout(
        Vector2 targetLocalPosition,
        Vector2 targetGlobalScale,
        float duration,
        Tween.TransitionType transition = Tween.TransitionType.Cubic,
        Tween.EaseType ease = Tween.EaseType.Out,
        bool instant = false
    )
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return null;

        _visualLayoutTween?.Kill();
        EnsureVisualTransformPivot(visualScaleHost);
        _visualTransformBaseScale = GetVisualTransformLocalScaleFromGlobalScale(
            visualScaleHost,
            targetGlobalScale
        );
        Vector2 targetScale = _visualTransformBaseScale * _visualTransformHoverScale;

        if (instant || duration <= 0f || !IsInsideTree())
        {
            visualScaleHost.Position = targetLocalPosition;
            TweenCenteredScale(targetScale, 0f, transition, ease, instant: true);
            return null;
        }

        // Position/layout and pointer scaling must not share a Tween. A selected card can move
        // beneath a stationary pointer; starting its hover scale must not cancel that movement.
        _visualLayoutTween = CreateTween();
        _visualLayoutTween
            .TweenProperty(visualScaleHost, "position", targetLocalPosition, duration)
            .SetTrans(transition)
            .SetEase(ease);
        TweenCenteredScale(targetScale, duration, transition, ease);
        return _visualLayoutTween;
    }

    /// <summary>
    /// Resolves the VisualTransform's local position from an on-screen centre. Its pivot stays
    /// at the card centre, so the root can remain at its original hand-slot coordinates.
    /// </summary>
    public Vector2 GetCenteredVisualTransformLocalPosition(Vector2 visualGlobalCenter)
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return Vector2.Zero;

        EnsureVisualTransformPivot(visualScaleHost);
        Transform2D rootTransform = GetGlobalTransformWithCanvas();
        if (Mathf.IsZeroApprox(rootTransform.Determinant()))
            return visualScaleHost.Position;

        return rootTransform.AffineInverse() * visualGlobalCenter - visualScaleHost.PivotOffset;
    }

    public Vector2 GetCenteredVisualTransformGlobalScale()
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return Vector2.One;

        Transform2D transform = visualScaleHost.GetGlobalTransformWithCanvas();
        return new Vector2(transform.X.Length(), transform.Y.Length());
    }

    public float GetCenteredVisualTransformGlobalRotation()
    {
        Control visualScaleHost = CardVisualScaleHost;
        return visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost)
            ? 0f
            : visualScaleHost.GetGlobalTransformWithCanvas().X.Angle();
    }

    public void SetCenteredVisualTransformGlobalRotation(float globalRotation)
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return;

        float parentRotation = visualScaleHost.GetParent() is CanvasItem parent
            ? parent.GetGlobalTransformWithCanvas().X.Angle()
            : 0f;
        visualScaleHost.Rotation = globalRotation - parentRotation;
    }

    private static Vector2 GetVisualTransformLocalScaleFromGlobalScale(
        Control visualScaleHost,
        Vector2 targetGlobalScale
    )
    {
        if (visualScaleHost.GetParent() is not CanvasItem parent)
            return targetGlobalScale;

        Transform2D parentTransform = parent.GetGlobalTransformWithCanvas();
        float parentScaleX = Math.Max(0.0001f, parentTransform.X.Length());
        float parentScaleY = Math.Max(0.0001f, parentTransform.Y.Length());
        return new Vector2(targetGlobalScale.X / parentScaleX, targetGlobalScale.Y / parentScaleY);
    }

    public void ResetCenteredVisualTransformLayout()
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return;

        _visualLayoutTween?.Kill();
        _hoverTween?.Kill();
        visualScaleHost.Position = Vector2.Zero;
        visualScaleHost.Scale = Vector2.One;
        visualScaleHost.Rotation = 0f;
        EnsureVisualTransformPivot(visualScaleHost);
        _visualTransformBaseScale = Vector2.One;
        _visualTransformHoverScale = Vector2.One;
    }

    private void ResetCardVisualHoverTransform() => ResetCenteredVisualTransformLayout();

    private static void EnsureVisualTransformPivot(Control visualScaleHost)
    {
        if (visualScaleHost == null || !GodotObject.IsInstanceValid(visualScaleHost))
            return;

        Vector2 size = visualScaleHost.Size;
        if (size.X <= 1f || size.Y <= 1f)
            size = CardBaseSize;
        visualScaleHost.PivotOffset = size * 0.5f;
    }

    private float GetPointerHoverScaleDuration(Vector2 targetScale) =>
        targetScale.DistanceSquaredTo(Vector2.One) > 0.0001f ? 0.18f : 0.14f;

    private Tween.EaseType GetPointerHoverScaleEase(Vector2 targetScale) =>
        targetScale.DistanceSquaredTo(Vector2.One) > 0.0001f
            ? Tween.EaseType.Out
            : Tween.EaseType.InOut;

    private void ShowKeywordTooltip()
    {
        if (CurrentSkill == null || !HoverUiEnabled || Input.IsMouseButtonPressed(MouseButton.Left))
            return;

        EnsureHoverPreviewCache();
        string tooltipText = _cachedKeywordTooltipText;
        IReadOnlyList<SkillID> relatedSkillIds = _cachedRelatedSkillIds;
        bool hasText = !string.IsNullOrWhiteSpace(tooltipText);
        bool hasCardPreviews = relatedSkillIds.Count > 0;

        if (!hasText && !hasCardPreviews)
        {
            _keywordTooltip?.HideTooltip();
            _relatedCardPreview?.HidePreviews();
            return;
        }

        Tip tip = KeywordTooltip;
        if (tip != null)
        {
            if (hasText)
            {
                tip.FollowMouse = true;
                tip.SetText(tooltipText);
            }
            else
            {
                tip.HideTooltip();
            }
        }

        if (hasCardPreviews && !SuppressRelatedCardPreview)
            RelatedCardPreview?.ShowPreviews(relatedSkillIds, CurrentSkill, this);
        else
            _relatedCardPreview?.HidePreviews();
    }

    private void EnsureHoverPreviewCache()
    {
        if (CurrentSkill == null)
        {
            _cachedHoverSkill = null;
            _cachedHoverPreviewRevision = int.MinValue;
            _cachedKeywordTooltipText = string.Empty;
            _cachedRelatedSkillIds = Array.Empty<SkillID>();
            return;
        }

        int revision = GetCurrentPreviewCacheRevision();
        if (ReferenceEquals(_cachedHoverSkill, CurrentSkill) && _cachedHoverPreviewRevision == revision)
            return;

        _cachedHoverSkill = CurrentSkill;
        _cachedHoverPreviewRevision = revision;
        CurrentSkill.UpdateDescription();
        Skill.SkillTooltipHints hints = CurrentSkill.CollectTooltipHints();
        _cachedRelatedSkillIds = hints.RelatedSkillIds;
        _cachedKeywordTooltipText = Skill.BuildKeywordTooltipText(CurrentSkill, hints);
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
        _battleMotionTargetPosition = null;
        _battleMotionTargetScale = null;
    }

    public void TweenBattleMotion(
        Vector2 targetPosition,
        Vector2 targetScale,
        float duration = 0.16f,
        bool instant = false
    )
    {
        if (
            !instant
            && _battleMotionTargetPosition.HasValue
            && _battleMotionTargetScale.HasValue
            && _battleMotionTargetPosition.Value.DistanceSquaredTo(targetPosition) < 0.25f
            && _battleMotionTargetScale.Value.DistanceSquaredTo(targetScale) < 0.0001f
            && _motionTween != null
            && _motionTween.IsValid()
        )
        {
            return;
        }

        StopBattleMotion();
        _battleMotionTargetPosition = targetPosition;
        _battleMotionTargetScale = targetScale;

        if (instant || duration <= 0f || !IsInsideTree())
        {
            Position = targetPosition;
            Scale = targetScale;
            _battleMotionTargetPosition = null;
            _battleMotionTargetScale = null;
            return;
        }

        Vector2 startPosition = Position;
        Vector2 startScale = Scale;
        _motionTween = CreateTween();
        _motionTween
            .TweenMethod(
                Callable.From<float>(progress =>
                    ApplyBattleMotionProgress(
                        startPosition,
                        targetPosition,
                        startScale,
                        targetScale,
                        progress
                    )
                ),
                0f,
                1f,
                duration
            )
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.Out);
        _motionTween.Finished += () =>
        {
            Position = targetPosition;
            Scale = targetScale;
            _battleMotionTargetPosition = null;
            _battleMotionTargetScale = null;
        };
    }

    private void ApplyBattleMotionProgress(
        Vector2 startPosition,
        Vector2 targetPosition,
        Vector2 startScale,
        Vector2 targetScale,
        float progress
    )
    {
        float easedProgress = EaseBattleMotionProgress(progress);
        Position = startPosition.Lerp(targetPosition, easedProgress);
        Scale = startScale.Lerp(targetScale, easedProgress);
    }

    private static float EaseBattleMotionProgress(float progress)
    {
        progress = Mathf.Clamp(progress, 0f, 1f);
        float inv = 1f - progress;
        return 1f - inv * inv * inv;
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
            PressEffectPartial(
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
            await ToSignal(compressTween, Tween.SignalName.Finished);
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
        {
            Rotation = GetRotationWithTopFacingVelocity(initialVelocity)
                + GetCardFlySpinAngle(options, 0.01f);
        }
        UpdateTrailParticlesRotation(particles, initialVelocity);

        PressEffectPartial(
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
                    {
                        Rotation = GetRotationWithTopFacingVelocity(velocity)
                            + GetCardFlySpinAngle(options, t);
                    }
                    UpdateTrailParticlesRotation(particles, velocity);
                }),
                0f,
                1f,
                options.FlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        flyTween.SetParallel(false);

        await ToSignal(flyTween, Tween.SignalName.Finished);

        if (GodotObject.IsInstanceValid(this) && options.HideCardVisualOnArrival)
        {
            SetCardVisualVisible(false);
            Button.Disabled = true;
            HoverHint.Visible = false;
        }

        options.OnArrival?.Invoke();

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

    private static float GetCardFlySpinAngle(CardTrailMoveOptions options, float progress)
    {
        if (options == null || Mathf.IsZeroApprox(options.RotationSpinTurns))
            return 0f;

        float acceleratedProgress = Mathf.Clamp(progress, 0f, 1f);
        acceleratedProgress *= acceleratedProgress;
        return options.RotationSpinTurns * Mathf.Pi * 2f * acceleratedProgress;
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

    /// <summary>
    /// Brief horizontal shake for rejected actions such as an unaffordable shop card.
    /// It only moves the visual child, so the card stays in its layout slot.
    /// </summary>
    public void PlayRejectShake()
    {
        Control visualScaleHost = CardVisualScaleHost;
        if (
            visualScaleHost == null
            || !GodotObject.IsInstanceValid(visualScaleHost)
            || !visualScaleHost.IsInsideTree()
        )
        {
            return;
        }

        _rejectShakeTween?.Kill();
        Vector2 basePosition = visualScaleHost.Position;
        _rejectShakeTween = CreateTween();
        _rejectShakeTween
            .TweenProperty(visualScaleHost, "position", basePosition + new Vector2(-10f, 0f), 0.045f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _rejectShakeTween
            .TweenProperty(visualScaleHost, "position", basePosition + new Vector2(9f, 0f), 0.075f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _rejectShakeTween
            .TweenProperty(visualScaleHost, "position", basePosition, 0.065f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _rejectShakeTween.Finished += () =>
        {
            if (GodotObject.IsInstanceValid(visualScaleHost))
                visualScaleHost.Position = basePosition;
        };
    }

    private void StopProgressEffect(ShaderMaterial shader)
    {
        _progressTween?.Kill();
        _progressTween = null;
        shader?.SetShaderParameter("progress", 0f);
    }

    public void PlayExhaustEffect(float duration = 0.8f)
    {
        PlayExhaustEffect(duration, this);
    }

    /// <summary>
    /// Plays the exhaust effect on the rendered card layer while the card root stays in its
    /// original layout slot. This preserves the card's on-screen position during the effect.
    /// </summary>
    public void PlayExhaustEffectAtCurrentVisualTransform(float duration = 0.8f)
    {
        PlayExhaustEffect(duration, CardVisualScaleHost);
    }

    private void PlayExhaustEffect(float duration, Control visualTarget)
    {
        visualTarget ??= this;
        AudioManager.PlayCardExhaust(this);
        ExhaustVfx.SpawnAt(visualTarget, GetExhaustVfxScale(visualTarget), duration);

        _progressTween?.Kill();
        _pressTween?.Kill();
        _hoverTween?.Kill();
        _motionTween?.Kill();

        if (GetCardExhaustShader() == null)
        {
            if (Modulate.A <= 0f)
                Modulate = Colors.White;
            _pressTween = CreateTween();
            _pressTween.SetParallel(true);
            _pressTween
                .TweenProperty(this, "modulate", ExhaustFadeModulate, duration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            _pressTween
                .TweenProperty(visualTarget, "scale", visualTarget.Scale * 1.035f, duration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
            return;
        }

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
            .TweenProperty(this, "modulate", ExhaustFadeModulate, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _pressTween
            .TweenProperty(visualTarget, "scale", visualTarget.Scale * 1.035f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private float GetExhaustVfxScale(Control visualTarget)
    {
        Vector2 size = visualTarget?.GetGlobalRect().Size ?? GetGlobalRect().Size;
        float areaRatio = size.X * size.Y / (CardBaseSize.X * CardBaseSize.Y);
        return Mathf.Clamp(Mathf.Sqrt(areaRatio) * 0.78f, 0.55f, 1.15f);
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
            if (_playableHighlight.Material == null)
                _playableHighlight.Material = _playableHighlightMaterial;
            float configuredWidth = GetShaderParameterFloat(_playableHighlightMaterial, "width");
            if (configuredWidth > 0f)
                _playableHighlightWidth = configuredWidth;
            return _playableHighlight;
        }
        return null;
    }

    private static ShaderMaterial CreatePlayableHighlightMaterial(Shader shader)
    {
        return new ShaderMaterial
        {
            Shader = shader,
            ResourceLocalToScene = true,
        };
    }

    private CanvasItem CardEffectMaterialTarget
    {
        get
        {
            if (_cardEffectMaterialTarget != null && GodotObject.IsInstanceValid(_cardEffectMaterialTarget))
                return _cardEffectMaterialTarget;

            _cardEffectMaterialTarget = GetNodeOrNull<CanvasItem>("CardGroup");
            if (_cardEffectMaterialTarget == null)
                _cardEffectMaterialTarget = GetNodeOrNull<CanvasGroup>(
                    "VisualTransform/SubViewport"
                );
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
        ExhaustVfx.Prewarm();
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
        if (_preserveSkillPreviewOnNextExitTree)
        {
            _preserveSkillPreviewOnNextExitTree = false;
        }
        else
        {
            HideSkillPreview();
            _keywordTooltip?.HideTooltip();
            FreeDamagePreviewLabels();
        }
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
        if (skill?.IsColorless == true)
        {
            yield return "Colorless";
            yield break;
        }

        if (skill?.IsStatusCard == true)
        {
            yield return "Status";
            yield break;
        }

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
            Skill.SkillTypes.Ability => "res://asset/svg/SkillIcon/ability.svg",
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

        EnsureSkillPreviewCache();
        _previewHostileTargets = _cachedPreviewHostileTargets;
        _previewFriendlyTargets = _cachedPreviewFriendlyTargets;

        Character[] hostileTargets = _previewHostileTargets;
        for (int i = 0; i < hostileTargets.Length; i++)
        {
            Character target = hostileTargets[i];
            if (GodotObject.IsInstanceValid(target))
                target.ShowTargetPreview(HostileTargetPreviewColor);
        }

        Character[] friendlyTargets = _previewFriendlyTargets;
        for (int i = 0; i < friendlyTargets.Length; i++)
        {
            Character target = friendlyTargets[i];
            if (GodotObject.IsInstanceValid(target))
                target.ShowTargetPreview(FriendlyTargetPreviewColor);
        }
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

        Character[] hostileTargets = _previewHostileTargets;
        for (int i = 0; i < hostileTargets.Length; i++)
        {
            Character target = hostileTargets[i];
            if (GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        Character[] friendlyTargets = _previewFriendlyTargets;
        for (int i = 0; i < friendlyTargets.Length; i++)
        {
            Character target = friendlyTargets[i];
            if (GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        _previewHostileTargets = Array.Empty<Character>();
        _previewFriendlyTargets = Array.Empty<Character>();
    }

    private void ShowDamagePreview()
    {
        HideDamagePreview();
        if (CurrentSkill == null)
            return;

        EnsureSkillPreviewCache();
        var entries = _cachedPreviewEffectEntries;
        if (entries == null || entries.Length == 0)
            return;

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        int panelIndex = 0;
        CardPreviewTargetEffectGroup[] groups =
            _cachedPreviewEffectGroups ?? Array.Empty<CardPreviewTargetEffectGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            CardPreviewTargetEffectGroup group = groups[i];
            Character target = group.Target;
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || group.Entries == null
                || group.Entries.Length == 0
            )
            {
                continue;
            }

            var panel = GetOrCreateDamagePanel(layer, panelIndex++);
            PreviewEffectDisplay.ShowTargetEffectPanel(
                panel,
                group.Entries,
                GetTargetScreenPosition(target)
            );
        }

        for (int i = panelIndex; i < _previewDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamagePanels[i]))
                _previewDamagePanels[i].Visible = false;
        }
    }

    private void BuildPreviewDamageEntryGroups(Skill.PreviewEffectEntry[] entries)
    {
        _previewDamageTargetsBuffer.Clear();
        foreach (List<Skill.PreviewEffectEntry> targetEntries in _previewDamageEntriesByTarget.Values)
            targetEntries.Clear();

        for (int i = 0; i < entries.Length; i++)
        {
            Skill.PreviewEffectEntry entry = entries[i];
            Character target = entry.Target;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            if (!_previewDamageEntriesByTarget.TryGetValue(target, out var targetEntries))
            {
                targetEntries = new List<Skill.PreviewEffectEntry>(4);
                _previewDamageEntriesByTarget[target] = targetEntries;
            }
            if (targetEntries.Count == 0)
                _previewDamageTargetsBuffer.Add(target);

            targetEntries.Add(entry);
        }
    }

    private void EnsureSkillPreviewCache()
    {
        if (CurrentSkill == null)
        {
            _cachedSkillPreviewSkill = null;
            _cachedSkillPreviewRevision = int.MinValue;
            _cachedPreviewHostileTargets = Array.Empty<Character>();
            _cachedPreviewFriendlyTargets = Array.Empty<Character>();
            _cachedPreviewEffectEntries = Array.Empty<Skill.PreviewEffectEntry>();
            _cachedPreviewEffectGroups = Array.Empty<CardPreviewTargetEffectGroup>();
            return;
        }

        int revision = GetCurrentPreviewCacheRevision();
        if (ReferenceEquals(_cachedSkillPreviewSkill, CurrentSkill) && _cachedSkillPreviewRevision == revision)
            return;

        _cachedSkillPreviewSkill = CurrentSkill;
        _cachedSkillPreviewRevision = revision;
        _debugSkillPreviewCacheBuilds++;
        CardEffectPreviewSnapshot snapshot = CardEffectPreviewEcs.GetSnapshot(CurrentSkill);
        _cachedPreviewHostileTargets = snapshot.HostileTargets;
        _cachedPreviewFriendlyTargets = snapshot.FriendlyTargets;
        _cachedPreviewEffectEntries = snapshot.EffectEntries;
        _cachedPreviewEffectGroups = snapshot.EffectGroupsByTarget;
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

    private SkillRelatedCardPreview EnsureRelatedCardPreview()
    {
        CanvasLayer layer = EnsureTipLayer();
        if (layer == null)
            return null;

        SkillRelatedCardPreview preview = layer.GetNodeOrNull<SkillRelatedCardPreview>(
            "RelatedCardPreview"
        );
        if (preview != null)
            return preview;

        preview = new SkillRelatedCardPreview { Name = "RelatedCardPreview" };
        layer.AddChild(preview);
        return preview;
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

        SetDescriptionFontSize(preferredFontSize);
    }

    private void AdjustDescriptionFont()
    {
        ApplyPreferredDescriptionFontSize();
        StyleBox style = Description.GetThemeStylebox("normal");
        float availableHeight = Description.Size.Y - style.GetMinimumSize().Y;
        if (availableHeight <= 0.0f || Description.Size.X <= 0.0f)
            return;

        int preferredFontSize = UserSettings.ScaleTextFontSize(_baseDescriptionFontSize);
        if (preferredFontSize <= 0)
            preferredFontSize = DefaultDescriptionFontSize;

        if (Description.GetContentHeight() <= availableHeight)
            return;

        for (int fontSize = preferredFontSize - 1; fontSize >= MinDescriptionFontSize; fontSize--)
        {
            SetDescriptionFontSize(fontSize);
            if (Description.GetContentHeight() <= availableHeight)
                return;
        }

        SetDescriptionFontSize(MinDescriptionFontSize);
    }

    private void SetDescriptionFontSize(int fontSize) {
        // BBCode emphasis must shrink with the body; measure the resulting Godot layout.
        Description.AddThemeFontSizeOverride("normal_font_size", fontSize);
        Description.AddThemeFontSizeOverride("bold_font_size", fontSize);
        Description.AddThemeFontSizeOverride("italics_font_size", fontSize);
        Description.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
        Description.AddThemeFontSizeOverride("mono_font_size", fontSize);
    }

    private static bool IsStatusCard(Skill skill) => skill?.IsStatusCard == true;

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

}
