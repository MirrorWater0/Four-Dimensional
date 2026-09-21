using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public static class PreviewEffectDisplay
{
    // Tightened alongside the row plate: a 60px slot left a visible gap between the glyph and
    // the number once both sat inside a chip.
    private const float IconSize = 50f;
    private const float IconVerticalOffset = 6f;
    private const float ActionIconVerticalOffset = 1f;
    private const float StatIconSourceSize = 40f;
    private const float StatIconVisualScale = 0.92f;
    private const float ActionIconSourceSize = 40f;
    private const float ActionIconVisualScale = 1.0f;
    private const float BuffIconSourceSize = 40f;
    private const float BuffIconVisualScale = 1.0f;
    private const float PreviewAppearFadeDuration = 0.12f;
    private const float PreviewAppearScaleFactor = 1.8f;
    private const int PreviewFontSize = 33;
    private const int PreviewOutlineSize = 4;
    private const int PlateAccentWidth = 4;
    private const string SwordShaderPath = "res://shader/Icon/sword.gdshader";
    private const string RhomboidShaderPath = "res://shader/Icon/Rhomboid.gdshader";
    private const string DamagePreviewIconPath = "res://asset/svg/SkillIcon/attack.svg";
    private const string HealPreviewIconPath = "res://asset/svg/SkillIcon/HealPreview.svg";
    private const string BlockPreviewIconPath = "res://asset/svg/SkillIcon/survive.svg";
    private const string MaxLifePreviewIconPath = "res://asset/svg/SkillIcon/MaxLife.svg";
    private const float DamagePreviewIconRotation = Mathf.Pi / 4f;
    private static Texture2D _damagePreviewIconTexture;
    private static Texture2D _healPreviewIconTexture;
    private static Texture2D _blockPreviewIconTexture;
    private static Texture2D _maxLifePreviewIconTexture;
    private static Shader _swordShader;
    private static Shader _rhomboidShader;
    private static readonly Color OutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);
    private static readonly Color PlateFillColor = new(0.035f, 0.05f, 0.092f, 0.92f);
    private static readonly Color PlateBorderColor = new(0.624f, 0.702f, 0.851f, 0.35f);
    private static readonly Color PlateShadowColor = new(0f, 0f, 0f, 0.72f);
    private static readonly Color DamageColor = new(1f, 0.6f, 0.45f, 1f);
    private static readonly Color HealColor = new(0.46f, 1f, 0.68f, 1f);
    private static readonly Color BlockColor = new(0.56f, 0.92f, 1f, 1f);
    private static readonly Color PowerColor = new(1f, 0.23f, 0.2f, 1f);
    private static readonly Color SurvivabilityColor = new(0.52f, 0.95f, 1f, 1f);
    private static readonly Color MaxLifeColor = new(1f, 0.9f, 0.58f, 1f);
    private static readonly Color BuffColor = new(0.9f, 0.96f, 1f, 1f);
    private static readonly Color MessageColor = new(1f, 0.86f, 0.48f, 1f);
    private static readonly ConditionalWeakTable<VBoxContainer, PanelPoolState> PanelStates =
        new();

    // All target-effect readouts use this shared placement.  The right side is reserved for
    // tooltips, so labels always grow out from the target's left edge instead.
    private static readonly Vector2 TargetEffectPreviewOffset = new(8f, 0f);

    /// <param name="showRowBackground">
    /// Whether each effect row receives its chip background. Incoming-damage readouts sit above
    /// the health bar, where only the icon and value should remain visible.
    /// </param>
    public static VBoxContainer CreatePanel(bool showRowBackground = true)
    {
        var panel = new VBoxContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            ZIndex = 80,
            ZAsRelative = false,
        };
        panel.AddThemeConstantOverride("separation", 5);
        PanelStates.Add(panel, new PanelPoolState(panel, showRowBackground));
        return panel;
    }

    /// <summary>How <see cref="ShowPanel"/> resolves the anchor into a panel rect.</summary>
    public enum PreviewAnchor
    {
        /// <summary>Anchor is the horizontal centre of the panel.</summary>
        Center,

        /// <summary>
        /// Anchor is the panel's right edge, keeping the readout on the target's left side.
        /// </summary>
        Left,

        /// <summary>
        /// Anchor is the panel's inner edge, so the readout hangs off the side of the target and
        /// never covers it. Flips to the opposite side when it would leave the viewport.
        /// </summary>
        Side,
    }

    public static void ShowPanel(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects,
        Vector2 targetScreenPosition,
        Vector2 offset,
        bool preservePosition = false,
        PreviewAnchor anchor = PreviewAnchor.Center
    )
    {
        if (panel == null)
            return;

        bool wasVisible = panel.Visible;
        Vector2 previousPosition = panel.Position;
        PanelPoolState state = BeginPanelRefresh(panel);
        AddOrderedEffectRows(panel, effects);
        EndPanelRefresh(state);

        if (state.UsedRows == 0)
        {
            panel.Visible = false;
            return;
        }

        panel.Visible = true;
        Vector2 size = panel.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);

        panel.Size = size;
        panel.PivotOffset = size * 0.5f;
        if (preservePosition && wasVisible)
        {
            panel.Position = previousPosition;
        }
        else
        {
            panel.Position = ResolvePanelPosition(
                panel,
                targetScreenPosition,
                offset,
                size,
                anchor
            );
        }

        if (wasVisible)
        {
            panel.Modulate = Colors.White;
            panel.Scale = Vector2.One;
        }
        else
        {
            PlayAppearTween(panel);
        }
    }

    /// <summary>
    /// Displays a card or intention effect preview next to its target using the shared target
    /// preview layout. Keep callers on this path so manual targeting and regular hover cannot
    /// drift to different sides of the character.
    /// </summary>
    public static void ShowTargetEffectPanel(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects,
        Vector2 targetScreenPosition
    )
    {
        ShowPanel(
            panel,
            effects,
            targetScreenPosition,
            TargetEffectPreviewOffset,
            anchor: PreviewAnchor.Left
        );
    }

    private const float PanelViewportMargin = 12f;

    private static Vector2 ResolvePanelPosition(
        Control panel,
        Vector2 targetScreenPosition,
        Vector2 offset,
        Vector2 size,
        PreviewAnchor anchor
    )
    {
        if (anchor == PreviewAnchor.Center)
        {
            Vector2 centred = targetScreenPosition + offset;
            return new Vector2(centred.X - size.X * 0.5f, centred.Y);
        }

        // Hang the plate off the target's side, vertically centred on it, so the silhouette
        // (and especially the face) stays unobstructed.
        float y = targetScreenPosition.Y + offset.Y - size.Y * 0.5f;
        float rightSide = targetScreenPosition.X + offset.X;
        float leftSide = targetScreenPosition.X - offset.X - size.X;

        Rect2 viewport = panel.GetViewportRect();
        float minX = viewport.Position.X + PanelViewportMargin;
        float maxX = viewport.End.X - PanelViewportMargin - size.X;
        float minY = viewport.Position.Y + PanelViewportMargin;
        float maxY = viewport.End.Y - PanelViewportMargin - size.Y;

        float x = anchor switch
        {
            PreviewAnchor.Left => leftSide,
            // Prefer the outward side; mirror when it would run off the edge (bosses sit near it).
            _ => rightSide > maxX && leftSide >= minX ? leftSide : rightSide,
        };

        return new Vector2(
            Mathf.Clamp(x, minX, Mathf.Max(minX, maxX)),
            Mathf.Clamp(y, minY, Mathf.Max(minY, maxY))
        );
    }

    public static void ClearPanel(VBoxContainer panel)
    {
        if (panel == null)
            return;

        PanelPoolState state = GetPanelState(panel);
        state.UsedRows = 0;
        EndPanelRefresh(state);
    }

    private static PanelPoolState BeginPanelRefresh(VBoxContainer panel)
    {
        PanelPoolState state = GetPanelState(panel);
        state.UsedRows = 0;
        return state;
    }

    private static void EndPanelRefresh(PanelPoolState state)
    {
        if (state == null)
            return;

        for (int i = state.UsedRows; i < state.Rows.Count; i++)
            state.Rows[i].Row.Visible = false;
    }

    private static PanelPoolState GetPanelState(VBoxContainer panel)
    {
        return PanelStates.GetValue(panel, CreatePanelState);
    }

    private static PanelPoolState CreatePanelState(VBoxContainer panel)
    {
        return new PanelPoolState(panel);
    }

    private static PreviewEffectRow GetNextRow(VBoxContainer panel)
    {
        PanelPoolState state = GetPanelState(panel);
        if (state.UsedRows >= state.Rows.Count)
        {
            var row = new PreviewEffectRow(state.ShowRowBackground);
            panel.AddChild(row.Row);
            state.Rows.Add(row);
        }

        PreviewEffectRow next = state.Rows[state.UsedRows++];
        next.Row.Visible = true;
        return next;
    }

    private static void AddEffectRow(VBoxContainer panel, Skill.PreviewEffectEntry effect)
    {
        switch (effect.Kind)
        {
            case Skill.PreviewEffectKind.Damage:
                AddDamageRow(panel, effect);
                break;
            case Skill.PreviewEffectKind.Heal:
                AddRow(
                    panel,
                    PreviewIconKey.Heal,
                    $"+{Math.Max(0, effect.Value)}",
                    HealColor
                );
                break;
            case Skill.PreviewEffectKind.Block:
                AddRow(
                    panel,
                    PreviewIconKey.Block,
                    $"+{Math.Max(0, effect.Value)}",
                    BlockColor
                );
                break;
            case Skill.PreviewEffectKind.Property:
                if (effect.PropertyType.HasValue)
                {
                    AddRow(
                        panel,
                        PreviewIconKey.Property(effect.PropertyType.Value),
                        FormatSigned(effect.Value),
                        GetPropertyColor(effect.PropertyType.Value)
                    );
                }
                break;
            case Skill.PreviewEffectKind.Buff:
                if (effect.BuffName.HasValue)
                {
                    AddRow(
                        panel,
                        PreviewIconKey.Buff(effect.BuffName.Value),
                        $"x{Math.Abs(effect.Value)}",
                        BuffColor
                    );
                }
                break;
            case Skill.PreviewEffectKind.Message:
                AddRow(panel, PreviewIconKey.Message, effect.Text, MessageColor);
                break;
        }
    }

    private static void AddOrderedEffectRows(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects
    )
    {
        if (panel == null || effects == null || effects.Count == 0)
            return;

        AddEffectsForKind(panel, effects, Skill.PreviewEffectKind.Damage);
        AddEffectsForKind(panel, effects, Skill.PreviewEffectKind.Heal);
        AddEffectsForKind(panel, effects, Skill.PreviewEffectKind.Block);
        AddPropertyEffects(panel, effects);
        AddEffectsForKind(panel, effects, Skill.PreviewEffectKind.Buff);
        AddEffectsForKind(panel, effects, Skill.PreviewEffectKind.Message);
        AddUnknownKindEffects(panel, effects);
    }

    private static void AddEffectsForKind(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects,
        Skill.PreviewEffectKind kind
    )
    {
        for (int i = 0; i < effects.Count; i++)
        {
            Skill.PreviewEffectEntry effect = effects[i];
            if (effect.Kind == kind)
                AddEffectRow(panel, effect);
        }
    }

    private static void AddPropertyEffects(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects
    )
    {
        AddPropertyEffectsForOrder(panel, effects, PropertyType.Power);
        AddPropertyEffectsForOrder(panel, effects, PropertyType.Survivability);
        AddPropertyEffectsForOrder(panel, effects, PropertyType.MaxLife);

        for (int i = 0; i < effects.Count; i++)
        {
            Skill.PreviewEffectEntry effect = effects[i];
            if (
                effect.Kind == Skill.PreviewEffectKind.Property
                && (
                    !effect.PropertyType.HasValue
                    || GetPropertySortOrder(effect.PropertyType) == 99
                )
            )
            {
                AddEffectRow(panel, effect);
            }
        }
    }

    private static void AddPropertyEffectsForOrder(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects,
        PropertyType propertyType
    )
    {
        for (int i = 0; i < effects.Count; i++)
        {
            Skill.PreviewEffectEntry effect = effects[i];
            if (
                effect.Kind == Skill.PreviewEffectKind.Property
                && effect.PropertyType == propertyType
            )
            {
                AddEffectRow(panel, effect);
            }
        }
    }

    private static void AddUnknownKindEffects(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects
    )
    {
        for (int i = 0; i < effects.Count; i++)
        {
            Skill.PreviewEffectEntry effect = effects[i];
            if (GetKindSortOrder(effect.Kind) == 99)
                AddEffectRow(panel, effect);
        }
    }

    private static int GetKindSortOrder(Skill.PreviewEffectKind kind)
    {
        return kind switch
        {
            Skill.PreviewEffectKind.Damage => 0,
            Skill.PreviewEffectKind.Heal => 1,
            Skill.PreviewEffectKind.Block => 2,
            Skill.PreviewEffectKind.Property => 3,
            Skill.PreviewEffectKind.Buff => 4,
            Skill.PreviewEffectKind.Message => 5,
            _ => 99,
        };
    }

    private static int GetPropertySortOrder(PropertyType? type)
    {
        return type switch
        {
            PropertyType.Power => 0,
            PropertyType.Survivability => 1,
            PropertyType.MaxLife => 2,
            _ => 99,
        };
    }

    private static void AddRow(
        VBoxContainer panel,
        PreviewIconKey iconKey,
        string text,
        Color color,
        int fontSize = PreviewFontSize,
        int outlineSize = PreviewOutlineSize
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        PreviewEffectRow row = GetNextRow(panel);
        row.Configure(iconKey, text, color, fontSize, outlineSize);
    }

    private static void PlayAppearTween(Control panel)
    {
        if (panel == null || !GodotObject.IsInstanceValid(panel))
            return;

        panel.Scale = Vector2.One * PreviewAppearScaleFactor;
        panel.Modulate = new Color(1f, 1f, 1f, 0f);

        Tween tween = panel.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(panel, "modulate", Colors.White, PreviewAppearFadeDuration);
        tween
            .TweenProperty(panel, "scale", Vector2.One, PreviewAppearFadeDuration)
            .SetEase(Tween.EaseType.Out);
    }

    private static void AddDamageRow(VBoxContainer panel, Skill.PreviewEffectEntry effect)
    {
        string damageText = FormatDamageText(effect);
        if (string.IsNullOrWhiteSpace(damageText))
            return;

        AddRow(panel, PreviewIconKey.Damage, damageText, DamageColor);
    }

    private static Label CreatePreviewLabel(string text, Color color, int fontSize, int outlineSize)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = false,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeConstantOverride("outline_size", outlineSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);
        return label;
    }

    private static Control CreateStatIcon(Character character, PropertyType type)
    {
        Control icon = CreatePreviewStatIcon(type) ?? CreateFallbackStatIcon(type);
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        ApplyPreviewIconLayout(icon, StatIconSourceSize, StatIconVisualScale);
        if (icon.GetChildOrNull<Label>(0) is Label label)
            label.Visible = false;
        return CreateIconHolder(icon);
    }

    private static Control CreateDamagePreviewIcon()
    {
        Control icon = CreateSvgActionIcon(DamagePreviewIconPath, DamagePreviewIconRotation)
            ?? CreateFallbackActionIcon(DamageColor);
        return CreateActionIconHolder(icon);
    }

    private static Control CreateBlockPreviewIcon()
    {
        Control icon = CreateSvgActionIcon(BlockPreviewIconPath)
            ?? CreateFallbackActionIcon(BlockColor);
        return CreateActionIconHolder(icon);
    }

    private static Control CreateHealPreviewIcon()
    {
        Control icon = CreateSvgActionIcon(HealPreviewIconPath)
            ?? CreateFallbackActionIcon(HealColor);
        return CreateActionIconHolder(icon);
    }

    private static Control CreateMessagePreviewIcon()
    {
        var label = new Label
        {
            Text = "!",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
            Size = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
        };
        label.AddThemeFontSizeOverride("font_size", 34);
        label.AddThemeConstantOverride("outline_size", 6);
        label.AddThemeColorOverride("font_color", MessageColor);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);
        return CreateActionIconHolder(label);
    }

    private static Control CreatePreviewStatIcon(PropertyType type)
    {
        if (type == PropertyType.MaxLife)
            return CreateSvgStatIcon(MaxLifePreviewIconPath);

        Shader shader = GetStatShader(type);
        if (shader == null)
            return null;

        var material = new ShaderMaterial { Shader = shader };
        ApplyStatIconShaderParameters(material, type);
        return new ColorRect
        {
            Color = Colors.White,
            Material = material,
            CustomMinimumSize = new Vector2(StatIconSourceSize, StatIconSourceSize),
            Size = new Vector2(StatIconSourceSize, StatIconSourceSize),
        };
    }

    private static void ApplyStatIconShaderParameters(ShaderMaterial material, PropertyType type)
    {
        if (material == null)
            return;

        switch (type)
        {
            case PropertyType.Power:
                material.SetShaderParameter("sword_color", PowerColor);
                material.SetShaderParameter("blade_color", new Color(1f, 0.2f, 0.2f, 1f));
                material.SetShaderParameter("handle_color", new Color(0.38f, 0.14f, 0.14f, 1f));
                material.SetShaderParameter("glow_intensity", 0.73f);
                material.SetShaderParameter("angle", 0.0f);
                break;
            case PropertyType.Survivability:
                material.SetShaderParameter("color", SurvivabilityColor);
                break;
        }
    }

    private static Control CreateSvgActionIcon(string iconPath, float rotation = 0f)
    {
        Texture2D texture = GetActionIconTexture(iconPath);
        if (texture == null)
            return null;

        return new TextureRect
        {
            CustomMinimumSize = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
            Size = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            PivotOffset = new Vector2(ActionIconSourceSize, ActionIconSourceSize) * 0.5f,
            Rotation = rotation,
        };
    }

    private static Control CreateSvgStatIcon(string iconPath)
    {
        Texture2D texture = GetStatIconTexture(iconPath);
        if (texture == null)
            return null;

        return new TextureRect
        {
            CustomMinimumSize = new Vector2(StatIconSourceSize, StatIconSourceSize),
            Size = new Vector2(StatIconSourceSize, StatIconSourceSize),
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
    }

    private static Texture2D GetActionIconTexture(string iconPath)
    {
        return iconPath switch
        {
            DamagePreviewIconPath => _damagePreviewIconTexture ??=
                GD.Load<Texture2D>(DamagePreviewIconPath),
            HealPreviewIconPath => _healPreviewIconTexture ??=
                GD.Load<Texture2D>(HealPreviewIconPath),
            BlockPreviewIconPath => _blockPreviewIconTexture ??=
                GD.Load<Texture2D>(BlockPreviewIconPath),
            _ => string.IsNullOrWhiteSpace(iconPath) ? null : GD.Load<Texture2D>(iconPath),
        };
    }

    private static Texture2D GetStatIconTexture(string iconPath)
    {
        return iconPath switch
        {
            MaxLifePreviewIconPath => _maxLifePreviewIconTexture ??=
                GD.Load<Texture2D>(MaxLifePreviewIconPath),
            _ => string.IsNullOrWhiteSpace(iconPath) ? null : GD.Load<Texture2D>(iconPath),
        };
    }

    private static Shader GetStatShader(PropertyType type)
    {
        return type switch
        {
            PropertyType.Power => _swordShader ??= GD.Load<Shader>(SwordShaderPath),
            PropertyType.Survivability => _rhomboidShader ??= GD.Load<Shader>(RhomboidShaderPath),
            _ => null,
        };
    }

    private static Control CreateActionIconHolder(Control icon)
    {
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        ApplyPreviewIconLayout(
            icon,
            ActionIconSourceSize,
            ActionIconVisualScale,
            ActionIconVerticalOffset
        );
        return CreateIconHolder(icon);
    }

    private static Control CreateBuffPreviewIcon(Buff.BuffName buffName)
    {
        ColorRect icon = Buff.CreateBuffTooltipIcon(buffName);
        if (icon == null)
            return CreateIconHolder(null);

        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        ApplyPreviewIconLayout(icon, BuffIconSourceSize, BuffIconVisualScale);
        icon.Visible = true;
        if (icon.GetChildOrNull<Label>(0) is Label stackLabel)
            stackLabel.Visible = false;

        return CreateIconHolder(icon);
    }

    private static void ApplyPreviewIconLayout(
        Control icon,
        float sourceSize,
        float visualScale,
        float verticalOffset = IconVerticalOffset
    )
    {
        if (icon == null)
            return;

        float visualSize = IconSize * visualScale;
        icon.Position = new Vector2(
            (IconSize - visualSize) / 2f,
            verticalOffset + (IconSize - visualSize) / 2f
        );
        icon.CustomMinimumSize = new Vector2(sourceSize, sourceSize);
        icon.Size = new Vector2(sourceSize, sourceSize);
        icon.Scale = Vector2.One * (visualSize / sourceSize);
    }

    private static Control CreateIconHolder(Control icon)
    {
        var holder = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            Size = new Vector2(IconSize, IconSize),
            ClipContents = false,
        };

        if (icon != null)
            holder.AddChild(icon);

        return holder;
    }

    private static Control CreateIconForKey(PreviewIconKey key)
    {
        return key.Kind switch
        {
            PreviewIconKind.Damage => CreateDamagePreviewIcon(),
            PreviewIconKind.Heal => CreateHealPreviewIcon(),
            PreviewIconKind.Block => CreateBlockPreviewIcon(),
            PreviewIconKind.Property => CreateStatIcon(null, (PropertyType)key.Detail),
            PreviewIconKind.Buff => CreateBuffPreviewIcon((Buff.BuffName)key.Detail),
            PreviewIconKind.Message => CreateMessagePreviewIcon(),
            _ => null,
        };
    }

    private sealed class PanelPoolState
    {
        public PanelPoolState(VBoxContainer panel, bool showRowBackground = true)
        {
            Panel = panel;
            ShowRowBackground = showRowBackground;
        }

        public VBoxContainer Panel { get; }
        public bool ShowRowBackground { get; }
        public readonly List<PreviewEffectRow> Rows = new();
        public int UsedRows;
    }

    private sealed class PreviewEffectRow
    {
        public readonly PanelContainer Row;
        private readonly HBoxContainer _content;
        private readonly StyleBoxFlat _plate;
        private readonly Label _label;
        private readonly bool _showRowBackground;
        private Control _icon;
        private PreviewIconKey _iconKey;
        private int _fontSize = PreviewFontSize;
        private int _outlineSize = PreviewOutlineSize;
        private Color _plateAccent = Colors.Transparent;

        public PreviewEffectRow(bool showRowBackground)
        {
            _showRowBackground = showRowBackground;
            if (_showRowBackground)
            {
                // Effect previews on a card or target need a chip to stay readable over art.
                _plate = new StyleBoxFlat
                {
                    BgColor = PlateFillColor,
                    BorderColor = PlateBorderColor,
                    BorderWidthLeft = PlateAccentWidth,
                    BorderWidthTop = 1,
                    BorderWidthRight = 1,
                    BorderWidthBottom = 1,
                    CornerRadiusTopLeft = 0,
                    CornerRadiusTopRight = 0,
                    CornerRadiusBottomRight = 0,
                    CornerRadiusBottomLeft = 0,
                    ContentMarginLeft = 8f,
                    ContentMarginRight = 14f,
                    ContentMarginTop = 2f,
                    ContentMarginBottom = 2f,
                    ShadowColor = PlateShadowColor,
                    ShadowSize = 10,
                    ShadowOffset = new Vector2(0f, 3f),
                };
            }

            Row = new PanelContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                ClipContents = false,
            };
            Row.AddThemeStyleboxOverride(
                "panel",
                _showRowBackground ? _plate : new StyleBoxEmpty()
            );

            _content = new HBoxContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = false,
            };
            _content.AddThemeConstantOverride("separation", 2);
            Row.AddChild(_content);

            _label = CreatePreviewLabel(string.Empty, Colors.White, _fontSize, _outlineSize);
            _content.AddChild(_label);
        }

        public void Configure(
            PreviewIconKey iconKey,
            string text,
            Color color,
            int fontSize,
            int outlineSize
        )
        {
            ConfigureIcon(iconKey);
            _label.Text = text ?? string.Empty;
            if (_fontSize != fontSize)
            {
                _fontSize = fontSize;
                _label.AddThemeFontSizeOverride("font_size", fontSize);
            }
            if (_outlineSize != outlineSize)
            {
                _outlineSize = outlineSize;
                _label.AddThemeConstantOverride("outline_size", outlineSize);
            }
            _label.AddThemeColorOverride("font_color", color);
            _label.AddThemeColorOverride("font_outline_color", OutlineColor);

            if (_showRowBackground && _plateAccent != color)
            {
                _plateAccent = color;
                _plate.BorderColor = color with { A = 0.85f };
                _plate.BgColor = PlateFillColor.Lerp(color, 0.07f) with { A = PlateFillColor.A };
            }
        }

        private void ConfigureIcon(PreviewIconKey iconKey)
        {
            bool hasValidIcon = _icon != null && GodotObject.IsInstanceValid(_icon);
            if (_iconKey.Equals(iconKey) && hasValidIcon == iconKey.HasIcon)
                return;

            if (_icon != null)
            {
                if (GodotObject.IsInstanceValid(_icon))
                {
                    _content.RemoveChild(_icon);
                    _icon.QueueFree();
                }
                _icon = null;
            }

            _iconKey = iconKey;
            if (!iconKey.HasIcon)
                return;

            _icon = CreateIconForKey(iconKey);
            if (_icon == null)
                return;

            _content.AddChild(_icon);
            _content.MoveChild(_icon, 0);
        }
    }

    private readonly struct PreviewIconKey : IEquatable<PreviewIconKey>
    {
        private PreviewIconKey(PreviewIconKind kind, int detail = 0)
        {
            Kind = kind;
            Detail = detail;
        }

        public PreviewIconKind Kind { get; }
        public int Detail { get; }
        public bool HasIcon => Kind != PreviewIconKind.None;

        public static PreviewIconKey Damage => new(PreviewIconKind.Damage);
        public static PreviewIconKey Heal => new(PreviewIconKind.Heal);
        public static PreviewIconKey Block => new(PreviewIconKind.Block);
        public static PreviewIconKey Message => new(PreviewIconKind.Message);
        public static PreviewIconKey Property(PropertyType type) =>
            new(PreviewIconKind.Property, (int)type);
        public static PreviewIconKey Buff(Buff.BuffName buffName) =>
            new(PreviewIconKind.Buff, (int)buffName);

        public bool Equals(PreviewIconKey other) =>
            Kind == other.Kind && Detail == other.Detail;

        public override bool Equals(object obj) =>
            obj is PreviewIconKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Detail);
    }

    private enum PreviewIconKind
    {
        None,
        Damage,
        Heal,
        Block,
        Property,
        Buff,
        Message,
    }

    private static ColorRect CreateFallbackStatIcon(PropertyType type)
    {
        return new ColorRect
        {
            Color = GetPropertyColor(type),
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            Size = new Vector2(IconSize, IconSize),
        };
    }

    private static ColorRect CreateFallbackActionIcon(Color color)
    {
        return new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
            Size = new Vector2(ActionIconSourceSize, ActionIconSourceSize),
        };
    }

    private static string FormatDamageText(Skill.PreviewEffectEntry effect)
    {
        string powerMultiplierText =
            effect.PowerMultiplier >= 2 ? $"（{effect.PowerMultiplier}倍）" : string.Empty;
        return effect.HitCount > 1
            ? $"{Math.Max(0, effect.Value)}({effect.HitCount}次){powerMultiplierText}"
            : $"{Math.Max(0, effect.Value)}{powerMultiplierText}";
    }

    private static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private static Color GetPropertyColor(PropertyType type)
    {
        return type switch
        {
            PropertyType.Power => PowerColor,
            PropertyType.Survivability => SurvivabilityColor,
            PropertyType.MaxLife => MaxLifeColor,
            _ => Colors.White,
        };
    }
}
