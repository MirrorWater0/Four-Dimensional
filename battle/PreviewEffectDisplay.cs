using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class PreviewEffectDisplay
{
    private const float IconSize = 28f;
    private const float IconVerticalOffset = 6f;
    private const float ActionIconVerticalOffset = 1f;
    private const float StatIconSourceSize = 40f;
    private const float StatIconVisualScale = 0.92f;
    private const float ActionIconSourceSize = 40f;
    private const float ActionIconVisualScale = 1.0f;
    private const float BuffIconSourceSize = 40f;
    private const float BuffIconVisualScale = 1.0f;
    private const string SwordShaderPath = "res://shader/Icon/sword.gdshader";
    private const string RhomboidShaderPath = "res://shader/Icon/Rhomboid.gdshader";
    private const string WingShaderPath = "res://shader/Icon/wing.gdshader";
    private const string DamagePreviewIconPath = "res://asset/svg/SkillIcon/attack.svg";
    private const string HealPreviewIconPath = "res://asset/svg/SkillIcon/HealPreview.svg";
    private const string BlockPreviewIconPath = "res://asset/svg/SkillIcon/survive.svg";
    private const string MaxLifePreviewIconPath = "res://asset/svg/SkillIcon/MaxLife.svg";
    private const float DamagePreviewIconRotation = Mathf.Pi / 4f;
    private static readonly Color OutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);
    private static readonly Color DamageColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color HealColor = new(0.46f, 1f, 0.68f, 1f);
    private static readonly Color BlockColor = new(0.56f, 0.92f, 1f, 1f);
    private static readonly Color PowerColor = new(1f, 0.23f, 0.2f, 1f);
    private static readonly Color SurvivabilityColor = new(0.52f, 0.95f, 1f, 1f);
    private static readonly Color SpeedColor = Colors.White;
    private static readonly Color MaxLifeColor = new(1f, 0.9f, 0.58f, 1f);
    private static readonly Color BuffColor = new(0.9f, 0.96f, 1f, 1f);

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
        Vector2 offset
    )
    {
        if (panel == null)
            return;

        ClearPanel(panel);
        foreach (Skill.PreviewEffectEntry effect in OrderEffects(effects))
            AddEffectRow(panel, effect);

        if (panel.GetChildCount() == 0)
        {
            panel.Visible = false;
            return;
        }

        panel.Modulate = Colors.White;
        panel.Scale = Vector2.One;
        panel.Visible = true;
        Vector2 size = panel.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);

        panel.Size = size;
        Vector2 anchor = targetScreenPosition + offset;
        panel.Position = new Vector2(anchor.X - size.X / 2f, anchor.Y);
    }

    public static void ClearPanel(VBoxContainer panel)
    {
        if (panel == null)
            return;

        foreach (Node child in panel.GetChildren())
            child.QueueFree();
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
                    CreateHealPreviewIcon(),
                    $"+{Math.Max(0, effect.Value)}",
                    HealColor
                );
                break;
            case Skill.PreviewEffectKind.Block:
                AddRow(
                    panel,
                    CreateBlockPreviewIcon(),
                    $"+{Math.Max(0, effect.Value)}",
                    BlockColor
                );
                break;
            case Skill.PreviewEffectKind.Property:
                if (effect.PropertyType.HasValue)
                {
                    AddRow(
                        panel,
                        CreateStatIcon(effect.Target, effect.PropertyType.Value),
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
                        CreateBuffPreviewIcon(effect.BuffName.Value),
                        $"x{Math.Abs(effect.Value)}",
                        BuffColor
                    );
                }
                break;
        }
    }

    private static IEnumerable<Skill.PreviewEffectEntry> OrderEffects(
        IReadOnlyList<Skill.PreviewEffectEntry> effects
    )
    {
        IEnumerable<Skill.PreviewEffectEntry> source =
            effects ?? Array.Empty<Skill.PreviewEffectEntry>();

        return source
            .Select((effect, index) => (effect, index))
            .OrderBy(entry => GetKindSortOrder(entry.effect.Kind))
            .ThenBy(entry => GetPropertySortOrder(entry.effect.PropertyType))
            .ThenBy(entry => entry.index)
            .Select(entry => entry.effect);
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
            _ => 99,
        };
    }

    private static int GetPropertySortOrder(PropertyType? type)
    {
        return type switch
        {
            PropertyType.Power => 0,
            PropertyType.Survivability => 1,
            PropertyType.Speed => 2,
            PropertyType.MaxLife => 3,
            _ => 99,
        };
    }

    private static void AddRow(VBoxContainer panel, Control icon, string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
        };
        row.AddThemeConstantOverride("separation", 4);

        if (icon != null)
            row.AddChild(icon);

        row.AddChild(CreatePreviewLabel(text, color, 26, 5));
        panel.AddChild(row);
    }

    private static void AddDamageRow(VBoxContainer panel, Skill.PreviewEffectEntry effect)
    {
        string damageText = FormatDamageText(effect);
        if (string.IsNullOrWhiteSpace(damageText))
            return;

        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
        };
        row.AddThemeConstantOverride("separation", 4);
        row.AddChild(CreateDamagePreviewIcon());

        row.AddChild(CreatePreviewLabel(damageText, DamageColor, 26, 5));
        panel.AddChild(row);
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

    private static Control CreatePreviewStatIcon(PropertyType type)
    {
        if (type == PropertyType.MaxLife)
            return CreateSvgStatIcon(MaxLifePreviewIconPath);

        string shaderPath = type switch
        {
            PropertyType.Power => SwordShaderPath,
            PropertyType.Survivability => RhomboidShaderPath,
            PropertyType.Speed => WingShaderPath,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(shaderPath))
            return null;

        var shader = GD.Load<Shader>(shaderPath);
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
            case PropertyType.Speed:
                material.SetShaderParameter("wing_color", new Color(0.92f, 0.97f, 1f, 1f));
                material.SetShaderParameter("flip_vertical", false);
                material.SetShaderParameter("wing_size", 0.82f);
                material.SetShaderParameter("pivot", new Vector2(0.56f, 0.58f));
                material.SetShaderParameter("rotation", 0.72f);
                material.SetShaderParameter("edge_smoothness", 0.024f);
                material.SetShaderParameter("blade_count", 5);
                break;
        }
    }

    private static Control CreateSvgActionIcon(string iconPath, float rotation = 0f)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
            return null;

        var texture = GD.Load<Texture2D>(iconPath);
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
        if (string.IsNullOrWhiteSpace(iconPath))
            return null;

        var texture = GD.Load<Texture2D>(iconPath);
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
            PropertyType.Speed => SpeedColor,
            PropertyType.MaxLife => MaxLifeColor,
            _ => Colors.White,
        };
    }
}
