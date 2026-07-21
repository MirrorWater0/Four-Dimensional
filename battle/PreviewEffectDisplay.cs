using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public static class PreviewEffectDisplay
{
    private const float IconSize = 60f;
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
    private static readonly Color DamageColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color HealColor = new(0.46f, 1f, 0.68f, 1f);
    private static readonly Color BlockColor = new(0.56f, 0.92f, 1f, 1f);
    private static readonly Color PowerColor = new(1f, 0.23f, 0.2f, 1f);
    private static readonly Color SurvivabilityColor = new(0.52f, 0.95f, 1f, 1f);
    private static readonly Color MaxLifeColor = new(1f, 0.9f, 0.58f, 1f);
    private static readonly Color BuffColor = new(0.9f, 0.96f, 1f, 1f);
    private static readonly Color MessageColor = new(1f, 0.86f, 0.48f, 1f);
    private static readonly ConditionalWeakTable<VBoxContainer, PanelPoolState> PanelStates =
        new();

    public static VBoxContainer CreatePanel()
    {
        var panel = new VBoxContainer
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            ZIndex = 80,
            ZAsRelative = false,
        };
        panel.AddThemeConstantOverride("separation", 2);
        return panel;
    }

    public static void ShowPanel(
        VBoxContainer panel,
        IReadOnlyList<Skill.PreviewEffectEntry> effects,
        Vector2 targetScreenPosition,
        Vector2 offset,
        bool preservePosition = false
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
            Vector2 anchor = targetScreenPosition + offset;
            panel.Position = new Vector2(anchor.X - size.X / 2f, anchor.Y);
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
            var row = new PreviewEffectRow();
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
        int fontSize = 40,
        int outlineSize = 6
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

        AddRow(panel, PreviewIconKey.Damage, damageText, DamageColor, 30, 5);
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
        public PanelPoolState(VBoxContainer panel)
        {
            Panel = panel;
        }

        public VBoxContainer Panel { get; }
        public readonly List<PreviewEffectRow> Rows = new();
        public int UsedRows;
    }

    private sealed class PreviewEffectRow
    {
        public readonly HBoxContainer Row;
        private readonly Label _label;
        private Control _icon;
        private PreviewIconKey _iconKey;
        private int _fontSize = 40;
        private int _outlineSize = 6;

        public PreviewEffectRow()
        {
            Row = new HBoxContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = false,
            };
            Row.AddThemeConstantOverride("separation", 4);
            _label = CreatePreviewLabel(string.Empty, Colors.White, _fontSize, _outlineSize);
            Row.AddChild(_label);
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
                    Row.RemoveChild(_icon);
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

            Row.AddChild(_icon);
            Row.MoveChild(_icon, 0);
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
