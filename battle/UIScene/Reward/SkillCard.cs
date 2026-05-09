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
    public TextureRect SkillIcon => field ??= GetNode<TextureRect>("SubViewport/ArtFrame/SkillIcon");
    public Panel HoverHint => field ??= GetNode<Panel>("SubViewport/HoverHint");
    public Panel BG2 => field ??= GetNode<Panel>("SubViewport/BG2");
    public Panel ArtFrame => field ??= GetNode<Panel>("SubViewport/ArtFrame");
    public Control RarityBadge => field ??= GetNode<Control>("SubViewport/RarityBadge");
    public Control EnergyBadge => field ??= GetNode<Control>("SubViewport/EnergyBadge");
    public Label RarityLabel => field ??= GetNode<Label>("SubViewport/RarityBadge/RarityLabel");
    public Label CharacterName => field ??= GetNode<Label>("SubViewport/CharacterName");
    public Label TypeLabel => field ??= GetNode<Label>("SubViewport/ArtFrame/TypeLabel");
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
    private Character[] _previewTargets = Array.Empty<Character>();
    private readonly List<Label> _previewDamageLabels = new();
    private static readonly Color TargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Vector2 DamagePreviewLabelOffset = new(-50f, -130f);
    private static readonly Color DamagePreviewColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color DamagePreviewOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);
    private static readonly Dictionary<Skill.SkillTypes, Texture2D> TypeIconCache = new();
    private static readonly Dictionary<SkillID, Texture2D> SkillIconCache = new();
    private static readonly Dictionary<SkillID, Texture2D> SkillPictureCache = new();
    private static Texture2D _defaultSkillIcon;

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
            if (!UseDefaultHoverEffect)
                return;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale * 1.08f, 0.15f);
        };
        Button.MouseExited += () =>
        {
            HoverHint.Visible = false;
            if (!UseDefaultHoverEffect)
                return;

            _hoverTween?.Kill();
            _hoverTween = CreateTween();
            _hoverTween.TweenProperty(this, "scale", _baseScale, 0.15f);
        };
        (Material as ShaderMaterial)?.SetShaderParameter("progress", 1f);
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
        _motionTween?.Kill();

        HoverHint.Visible = false;
        ApplyConfiguredDisplayScale();
        Scale = _baseScale;
        Position = Vector2.Zero;
        ZIndex = 0;

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
            CharacterName.Text = string.Empty;
            RarityLabel.Text = string.Empty;
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
        NameLabel.Text = CurrentSkill.SkillName ?? string.Empty;
        RarityLabel.Text = GetRarityDisplayName(CurrentSkill.Rarity);
        TypeLabel.Text = CurrentSkill.SkillType.GetDescription();
        Description.Text = CurrentSkill.Description ?? string.Empty;
        EnergyCost.Text = $"\u8017\u80fd\uff1a{CurrentSkill.CardEnergyCostText}";
        ApplyRarityStyles(CurrentSkill.Rarity);

        Texture2D skillPicture = GetSkillPictureTexture(CurrentSkill);
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

    public override void _ExitTree()
    {
        HideSkillPreview();
        FreeDamagePreviewLabels();
        base._ExitTree();
    }

    private static Texture2D GetSkillIconTexture(Skill skill)
    {
        if (skill?.SkillId is SkillID skillId)
        {
            if (SkillIconCache.TryGetValue(skillId, out Texture2D cachedTexture) && cachedTexture != null)
                return cachedTexture;

            string path = $"res://asset/svg/SkillIcon/{skillId}.svg";
            Texture2D texture = ResourceLoader.Exists(path)
                ? GD.Load<Texture2D>(path)
                : null;

            if (texture != null)
            {
                SkillIconCache[skillId] = texture;
                return texture;
            }
        }

        return GetSkillTypeIconTexture(skill?.SkillType ?? Skill.SkillTypes.none);
    }

    private static Texture2D GetSkillPictureTexture(Skill skill)
    {
        if (skill?.SkillId is not SkillID skillId)
            return null;

        if (SkillPictureCache.TryGetValue(skillId, out Texture2D cachedTexture) && cachedTexture != null)
            return cachedTexture;

        string[] fileNames = [skillId.ToString(), $"Kasiya{skillId}"];
        string[] extensions = [".png", ".jpg", ".jpeg", ".webp"];
        foreach (string fileName in fileNames)
        {
            foreach (string extension in extensions)
            {
                string path = $"res://asset/CardPicture/{fileName}{extension}";
                if (!ResourceLoader.Exists(path))
                    continue;

                Texture2D texture = GD.Load<Texture2D>(path);
                if (texture == null)
                    continue;

                SkillPictureCache[skillId] = texture;
                return texture;
            }
        }

        return null;
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

        texture = GD.Load<Texture2D>(path) ?? GetDefaultSkillIcon();
        TypeIconCache[skillType] = texture;
        return texture;
    }

    private static Texture2D GetDefaultSkillIcon()
    {
        _defaultSkillIcon ??= GD.Load<Texture2D>("res://asset/svg/SkillIcon/default.svg");
        return _defaultSkillIcon;
    }

    private void ShowTargetPreview()
    {
        HideTargetPreview();
        if (CurrentSkill == null)
            return;

        _previewTargets = CurrentSkill.GetPreviewHostileTargets();
        if (_previewTargets == null || _previewTargets.Length == 0)
        {
            _previewTargets = Array.Empty<Character>();
            return;
        }

        foreach (var target in _previewTargets.Where(GodotObject.IsInstanceValid))
            target.ShowTargetPreview(TargetPreviewColor);
    }

    private void HideTargetPreview()
    {
        if (_previewTargets == null || _previewTargets.Length == 0)
        {
            _previewTargets = Array.Empty<Character>();
            return;
        }

        foreach (var target in _previewTargets.Where(GodotObject.IsInstanceValid))
            target.HideTargetPreview();

        _previewTargets = Array.Empty<Character>();
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
                ? $"{entry.Damage}({entry.HitCount}\u6b21)"
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

        for (
            int fontSize = _baseDescriptionFontSize;
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

    private static string GetRarityDisplayName(Skill.SkillRarity rarity)
    {
        return rarity switch
        {
            Skill.SkillRarity.Uncommon => "\u7f55\u89c1",
            Skill.SkillRarity.Rare => "\u7a00\u6709",
            _ => "\u666e\u901a",
        };
    }

    private void SetCardBeamColor(Color color)
    {
        if (Material is not ShaderMaterial shader)
            return;

        shader.SetShaderParameter("beam_color", new Color(color.R, color.G, color.B, 1f));
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
