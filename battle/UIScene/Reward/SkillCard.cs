using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class SkillCard : SubViewportContainer
{
    private const int DefaultDescriptionFontSize = 17;
    private const int MinDescriptionFontSize = 8;
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

    public Panel BG => field ??= GetNode<Panel>("SubViewport/BG");
    public Panel InnerFrame => field ??= GetNode<Panel>("SubViewport/InnerFrame");
    public RichTextLabel Description => field ??= GetNode<RichTextLabel>("SubViewport/Description");
    public Label NameLabel => field ??= GetNode<Label>("SubViewport/NameLabel");
    public Button Button => field ??= GetNode<Button>("SubViewport/Button");
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
    public Label EnergyCost => field ??= GetNode<Label>("SubViewport/EnergyBadge/EnergyCost");
    public ColorRect ArtFill => field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtFill");
    public ColorRect ArtBandTop =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtBandTop");
    public ColorRect TopAccent => field ??= GetNodeOrNull<ColorRect>("SubViewport/TopAccent");
    public ColorRect ArtDiamondOuter =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtDiamondOuter");
    public ColorRect ArtDiamondInner =>
        field ??= GetNodeOrNull<ColorRect>("SubViewport/ArtFrame/ArtDiamondInner");
    public Polygon2D RarityPlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/RarityPlate");
    public Polygon2D EnergyPlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/EnergyPlate");
    public Polygon2D NamePlate => field ??= GetNodeOrNull<Polygon2D>("SubViewport/BG2/NamePlate");
    public Polygon2D CharacterPlate =>
        field ??= GetNodeOrNull<Polygon2D>("SubViewport/BG2/CharacterPlate");
    public Skill CurrentSkill { get; set; }
    public string PreviewCharacterName { get; set; }
    public string PreviewCharacterKey { get; set; }
    public string DisplayNameOverride { get; set; }
    public bool AutoPressEffect { get; set; } = true;
    public bool UseDefaultHoverEffect { get; set; } = true;

    private Tween _progressTween;
    private Tween _pressTween;
    private Tween _hoverTween;
    private Tween _motionTween;
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
    private Character[] _previewHostileTargets = Array.Empty<Character>();
    private Character[] _previewFriendlyTargets = Array.Empty<Character>();
    private readonly List<Label> _previewDamageLabels = new();
    private static readonly Color HostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color FriendlyTargetPreviewColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Vector2 DamagePreviewLabelOffset = new(-50f, -130f);
    private static readonly Color DamagePreviewColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color DamagePreviewOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);
    private static readonly Dictionary<Skill.SkillTypes, Texture2D> TypeIconCache = new();
    private static readonly Dictionary<SkillID, Texture2D> SkillIconCache = new();
    private static readonly Dictionary<string, Texture2D> SkillPictureCache = new();
    private static readonly string[] SkillPictureExtensions = [".png", ".jpg", ".jpeg", ".webp"];
    private static Shader _defaultCardShader;
    private static Shader _cardExhaustShader;
    private static Shader DefaultCardShader =>
        _defaultCardShader ??= GD.Load<Shader>("res://shader/Effect/RewardCard.gdshader");
    private static Shader CardExhaustShader =>
        _cardExhaustShader ??= GD.Load<Shader>("res://shader/Effect/CardExhaust.gdshader");
    private static NoiseTexture2D _cardExhaustNoiseTexture;
    private static ShaderMaterial _cardExhaustMaterialTemplate;
    private static Texture2D _defaultSkillIcon;
    private Tip _keywordTooltip;
    private Tip KeywordTooltip => _keywordTooltip ??= EnsureGlobalTooltip();

    public override void _Ready()
    {
        CacheDefaultCardMaterial();
        ApplySkillToUi();
        HoverHint.Visible = false;
        CacheBaseFontSizes();
        ApplyConfiguredDisplayScale();
        PivotOffsetRatio = new Vector2(0.5f, 0.5f);
        Button.MouseEntered += () =>
        {
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
            HoverHint.Visible = false;
            _keywordTooltip?.HideTooltip();
            if (!UseDefaultHoverEffect)
                return;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale, 0.15f);
        };
        RestoreDefaultCardMaterial()?.SetShaderParameter("progress", 1f);
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

        HoverHint.Visible = false;
        ApplyConfiguredDisplayScale();
        Scale = _baseScale;
        Position = Vector2.Zero;
        ZIndex = 0;
        Modulate = new Color(1, 1, 1, 1);

        if (RestoreDefaultCardMaterial() is ShaderMaterial shader)
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
        _motionTween?.Kill();

        HoverHint.Visible = false;
        ApplyConfiguredDisplayScale();
        Scale = _baseScale;
        Position = Vector2.Zero;
        ZIndex = 0;

        if (RestoreDefaultCardMaterial() is ShaderMaterial shader)
        {
            shader.SetShaderParameter("progress", 0f);
            shader.SetShaderParameter("center_vanish", 0f);
        }
    }

    public void StartAnimation(float delay = 0f)
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
            CharacterName.Text = string.Empty;
            TypeLabel.Text = string.Empty;
            Description.Text = string.Empty;
            EnergyCost.Text = string.Empty;
            if (SkillPicture != null)
            {
                SkillPicture.Texture = null;
                SkillPicture.Visible = false;
            }
            SkillIcon.Texture = null;
            SkillIcon.Visible = false;
            SetArtPlaceholderVisible(true);
            ApplyRarityStyles(Skill.SkillRarity.Common);
            return;
        }

        CurrentSkill.UpdateDescription();
        NameLabel.Text = DisplayNameOverride ?? CurrentSkill.SkillName ?? string.Empty;
        CharacterName.Text =
            PreviewCharacterName
            ?? CurrentSkill.OwnerCharater?.CharacterName
            ?? string.Empty;
        bool isStatusCard = IsStatusCard(CurrentSkill);
        string skillTypeText = isStatusCard
            ? I18n.Tr("ui.encyclopedia.skill_type.status", "状态")
            : CurrentSkill.SkillType.GetDescription();
        TypeLabel.Text = skillTypeText;
        Description.Text = CurrentSkill.Description ?? string.Empty;
        EnergyCost.Text = !CurrentSkill.CanBePlayed
            ? I18n.Tr("ui.encyclopedia.skill_cost.unplayable", "不可打出")
            : I18n.Format(
                "ui.reward.energy_cost",
                "耗能:{cost}",
                ("cost", CurrentSkill.CardEnergyCostText)
            );
        ApplyRarityStyles(CurrentSkill.Rarity);
        if (isStatusCard)
            ApplyStatusCardStyle();

        Texture2D skillPicture = GetSkillPictureTexture(CurrentSkill, PreviewCharacterName, PreviewCharacterKey);
        bool hasSkillPicture = skillPicture != null;
        if (SkillPicture != null)
        {
            SkillPicture.Texture = skillPicture;
            SkillPicture.Visible = hasSkillPicture;
            SkillPicture.Modulate = Colors.White;
        }

        SetArtPlaceholderVisible(!hasSkillPicture);
        SkillIcon.Visible = !hasSkillPicture;
        if (!hasSkillPicture)
        {
            SkillIcon.Texture = GetSkillIconTexture(CurrentSkill);
            SkillIcon.Modulate = Colors.White;
        }

        QueueAdjustTextSizes();
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
        QueueAdjustTextSizes();
        _keywordTooltip?.RefreshTextSizeFromSettings();
    }

    public void Vanish()
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
            return;

        _progressTween?.Kill();
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
        _motionTween?.Kill();
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

    public void PressEffect()
    {
        if (RestoreDefaultCardMaterial() is not ShaderMaterial shader)
            return;

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

    public void PlayExhaustEffect(float duration = 0.8f)
    {
        if (CardExhaustShader == null)
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
        Material = shader;

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

        if (
            Material is ShaderMaterial sceneMaterial
            && IsDefaultCardShader(sceneMaterial.Shader)
            && sceneMaterial.Duplicate() is ShaderMaterial duplicatedSceneMaterial
        )
        {
            _defaultCardMaterial = duplicatedSceneMaterial;
        }
        else
        {
            _defaultCardMaterial = new ShaderMaterial { Shader = DefaultCardShader };
            InitializeDefaultCardShaderParameters(_defaultCardMaterial);
        }

        if (_defaultCardMaterial == null)
            return;

        _defaultCardMaterial.ResourceLocalToScene = true;
        Material = _defaultCardMaterial;
    }

    private ShaderMaterial RestoreDefaultCardMaterial()
    {
        CacheDefaultCardMaterial();
        if (_defaultCardMaterial != null && Material != _defaultCardMaterial)
            Material = _defaultCardMaterial;

        return Material as ShaderMaterial;
    }

    private static bool IsDefaultCardShader(Shader shader)
    {
        if (shader == null || DefaultCardShader == null)
            return false;

        return shader == DefaultCardShader || shader.ResourcePath == DefaultCardShader.ResourcePath;
    }

    private static void InitializeDefaultCardShaderParameters(ShaderMaterial shader)
    {
        if (shader == null)
            return;

        shader.SetShaderParameter("progress", 0f);
        shader.SetShaderParameter("center_vanish", 0f);
        shader.SetShaderParameter("line_density", 30f);
        shader.SetShaderParameter("line_speed", 8f);
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
        _defaultSkillIcon = null;
        _cardExhaustNoiseTexture = null;
        _cardExhaustMaterialTemplate = null;
        _defaultCardShader = null;
        _cardExhaustShader = null;
    }

    private static ShaderMaterial CreateCardExhaustMaterial()
    {
        PrewarmExhaustEffect();
        ShaderMaterial shader =
            _cardExhaustMaterialTemplate?.Duplicate() as ShaderMaterial
            ?? CreateCardExhaustMaterialTemplate();
        shader.ResourceLocalToScene = true;
        shader.SetShaderParameter("exhaust_progress", 0f);
        return shader;
    }

    private static ShaderMaterial CreateCardExhaustMaterialTemplate()
    {
        var shader = new ShaderMaterial { Shader = CardExhaustShader, ResourceLocalToScene = true };
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

    private static Texture2D GetSkillPictureTexture(Skill skill, string previewCharacterName = null, string previewCharacterKey = null)
    {
        if (skill?.SkillId is not SkillID skillId)
            return null;

        foreach (string path in EnumerateSkillPicturePaths(skill, skillId, previewCharacterName, previewCharacterKey))
        {
            if (
                SkillPictureCache.TryGetValue(path, out Texture2D cachedTexture)
                && cachedTexture != null
            )
                return cachedTexture;

            if (!ResourceLoader.Exists(path))
                continue;

            Texture2D texture = PreloadeScene.GetTexture(path);
            if (texture == null)
                continue;

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

        foreach (string folder in GetSkillPictureFolders(skill, skillId, previewCharacterName, previewCharacterKey))
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

        if (skill?.OwnerCharater is PlayerCharacter player && !string.IsNullOrWhiteSpace(player.CharacterKey))
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

        var entries = CurrentSkill.GetPreviewHostileDamageEntries();
        if (entries == null || entries.Length == 0)
            return;

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.Target == null || !GodotObject.IsInstanceValid(entry.Target))
                continue;

            var label = GetOrCreateDamageLabel(layer, i);
            ShowDamageLabel(label, entry, GetTargetScreenPosition(entry.Target));
        }
    }

    private void HideDamagePreview()
    {
        for (int i = 0; i < _previewDamageLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamageLabels[i]))
                _previewDamageLabels[i].Visible = false;
        }
    }

    private void FreeDamagePreviewLabels()
    {
        for (int i = 0; i < _previewDamageLabels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_previewDamageLabels[i]))
                _previewDamageLabels[i].QueueFree();
        }
        _previewDamageLabels.Clear();
    }

    private Label GetOrCreateDamageLabel(CanvasLayer layer, int index)
    {
        while (_previewDamageLabels.Count <= index)
        {
            var label = CreateDamageLabel();
            layer.AddChild(label);
            _previewDamageLabels.Add(label);
        }

        var pooledLabel = _previewDamageLabels[index];
        if (!GodotObject.IsInstanceValid(pooledLabel))
        {
            pooledLabel = CreateDamageLabel();
            layer.AddChild(pooledLabel);
            _previewDamageLabels[index] = pooledLabel;
        }
        else if (pooledLabel.GetParent() == null)
        {
            layer.AddChild(pooledLabel);
        }

        return pooledLabel;
    }

    private static Label CreateDamageLabel()
    {
        var label = new Label
        {
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = false,
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_color", DamagePreviewColor);
        label.AddThemeColorOverride("font_outline_color", DamagePreviewOutlineColor);
        return label;
    }

    private static void ShowDamageLabel(
        Label label,
        Skill.PreviewDamageEntry entry,
        Vector2 targetScreenPosition
    )
    {
        label.Text =
            entry.HitCount > 1
                ? $"{entry.Damage}({entry.HitCount}次)"
                : entry.Damage.ToString();
        label.AddThemeColorOverride("font_color", DamagePreviewColor);
        label.AddThemeColorOverride("font_outline_color", DamagePreviewOutlineColor);
        label.Modulate = Colors.White;
        label.Scale = Vector2.One;
        label.Visible = true;

        Vector2 size = label.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);
        label.Size = size;

        Vector2 anchor = targetScreenPosition + DamagePreviewLabelOffset;
        label.Position = anchor - size / 2f;
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

    private void AdjustDescriptionFont()
    {
        float availableHeight = Description.Size.Y;
        if (availableHeight <= 0.0f)
            return;

        int preferredFontSize = UserSettings.ScaleTextFontSize(_baseDescriptionFontSize);
        for (
            int fontSize = preferredFontSize;
            fontSize >= MinDescriptionFontSize;
            fontSize--
        )
        {
            Description.AddThemeFontSizeOverride("normal_font_size", fontSize);
            if (Description.GetContentHeight() <= availableHeight)
                return;
        }

        Description.AddThemeFontSizeOverride("normal_font_size", MinDescriptionFontSize);
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
            _descriptionStyle.BorderColor = borderColor;
            _descriptionStyle.BgColor = DescriptionBaseColor;
        }
        if (_bg2Style != null)
        {
            _bg2Style.BorderColor = borderColor;
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
        if (ArtDiamondOuter != null)
            ArtDiamondOuter.Color = accentColor;
        if (ArtDiamondInner != null)
            ArtDiamondInner.Color = diamondCore;
        SetCardBeamColor(borderColor);

        if (TopAccent != null)
            TopAccent.Color = WithAlpha(borderColor, 0.58f);
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
            _descriptionStyle.BorderColor = borderColor;
            _descriptionStyle.BgColor = surfaceColor;
        }
        if (_bg2Style != null)
        {
            _bg2Style.BorderColor = borderColor;
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
        if (Material is not ShaderMaterial shader)
            return;

        shader.SetShaderParameter("beam_color", new Color(color.R, color.G, color.B, 1f));
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
