using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public static class PreviewEffectDisplay
{
    private const float PreviewAppearFadeDuration = 0.12f;
    private const float PreviewAppearScaleFactor = 1.8f;
    private static readonly Theme PreviewPalette = GD.Load<Theme>("res://battle/UIScene/EffectPreviewPalette.tres");
    private static Color DamageColor => PreviewPalette.GetColor("font_color", "Damage");
    private static Color HealColor => PreviewPalette.GetColor("font_color", "Heal");
    private static Color BlockColor => PreviewPalette.GetColor("font_color", "Block");
    private static Color PowerColor => PreviewPalette.GetColor("font_color", "Power");
    private static Color SurvivabilityColor => PreviewPalette.GetColor("font_color", "Survivability");
    private static Color MaxLifeColor => PreviewPalette.GetColor("font_color", "MaxLife");
    private static Color BuffColor => PreviewPalette.GetColor("font_color", "Buff");
    private static Color MessageColor => PreviewPalette.GetColor("font_color", "Message");
    private static readonly ConditionalWeakTable<VBoxContainer, PanelPoolState> PanelStates =
        new();

    /// <param name="showRowBackground">
    /// Whether each effect row receives its chip background. Incoming-damage readouts sit above
    /// the health bar, where only the icon and value should remain visible.
    /// </param>
    public static VBoxContainer CreatePanel(bool showRowBackground = true)
    {
        var panel = GD.Load<PackedScene>("res://battle/UIScene/EffectPreviewPanel.tscn").Instantiate<VBoxContainer>();
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
            ((EffectPreviewLayout)panel).TargetOffset,
            anchor: PreviewAnchor.Left
        );
    }

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
        float viewportMargin = ((EffectPreviewLayout)panel).ViewportMargin;
        float minX = viewport.Position.X + viewportMargin;
        float maxX = viewport.End.X - viewportMargin - size.X;
        float minY = viewport.Position.Y + viewportMargin;
        float maxY = viewport.End.Y - viewportMargin - size.Y;

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
        int fontSize = -1,
        int outlineSize = -1
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

    private static Control CreateStatIcon(Character character, PropertyType type)
    {
        Control icon = CreatePreviewStatIcon(type) ?? CreateFallbackStatIcon(type);
        return CreateIconHolder(icon, "StatLayout");
    }

    private static Control CreateDamagePreviewIcon()
    {
        return CreateActionIconHolder(LoadPreviewIcon("Damage"));
    }

    private static Control CreateBlockPreviewIcon()
    {
        return CreateActionIconHolder(LoadPreviewIcon("Block"));
    }

    private static Control CreateHealPreviewIcon()
    {
        return CreateActionIconHolder(LoadPreviewIcon("Heal"));
    }

    private static Control CreateMessagePreviewIcon()
    {
        return CreateActionIconHolder(LoadPreviewIcon("Message"));
    }

    private static Control CreatePreviewStatIcon(PropertyType type)
    {
        return type switch {
            PropertyType.Power => LoadPreviewIcon("Power"),
            PropertyType.Survivability => LoadPreviewIcon("Survivability"),
            PropertyType.MaxLife => LoadPreviewIcon("MaxLife"),
            _ => null,
        };
    }

    private static Control CreateActionIconHolder(Control icon)
    {
        return CreateIconHolder(icon, "ActionLayout");
    }

    private static Control CreateBuffPreviewIcon(Buff.BuffName buffName)
    {
        ColorRect icon = Buff.CreateBuffTooltipIcon(buffName);
        if (icon?.GetChildOrNull<Label>(0) is Label stackLabel)
            stackLabel.Visible = false;
        return CreateIconHolder(icon, "BuffLayout");
    }

    private static Control CreateIconHolder(Control icon, string layout = "ActionLayout")
    {
        var holder = GD.Load<PackedScene>("res://battle/UIScene/EffectPreviewIconHolder.tscn").Instantiate<Control>();
        if (icon != null) {
            Control sceneLayout = holder.GetNode<Control>(layout);
            icon.Position = sceneLayout.Position;
            icon.Size = sceneLayout.Size;
            icon.Scale = sceneLayout.Scale;
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;
            holder.AddChild(icon);
        }
        return holder;
    }

    private static Control LoadPreviewIcon(string kind) =>
        GD.Load<PackedScene>($"res://battle/UIScene/EffectPreview{kind}Icon.tscn").Instantiate<Control>();

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
        private int _fontSize;
        private int _outlineSize;
        private Color _plateAccent = Colors.Transparent;
        private Color _plateFill;

        public PreviewEffectRow(bool showRowBackground)
        {
            _showRowBackground = showRowBackground;
            string path = showRowBackground ? "EffectPreviewRow" : "EffectPreviewPlainRow";
            Row = GD.Load<PackedScene>($"res://battle/UIScene/{path}.tscn").Instantiate<PanelContainer>();
            _content = Row.GetNode<HBoxContainer>("Content");
            _label = _content.GetNode<Label>("Value");
            _fontSize = _label.GetThemeFontSize("font_size");
            _outlineSize = _label.GetThemeConstant("outline_size");
            if (showRowBackground)
            {
                _plate = (StyleBoxFlat)Row.GetThemeStylebox("panel");
                _plateFill = _plate.BgColor;
            }
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
            if (fontSize > 0 && _fontSize != fontSize)
            {
                _fontSize = fontSize;
                _label.AddThemeFontSizeOverride("font_size", fontSize);
            }
            if (outlineSize >= 0 && _outlineSize != outlineSize)
            {
                _outlineSize = outlineSize;
                _label.AddThemeConstantOverride("outline_size", outlineSize);
            }
            _label.AddThemeColorOverride("font_color", color);

            if (_showRowBackground && _plateAccent != color)
            {
                _plateAccent = color;
                _plate.BorderColor = color with { A = 0.85f };
                _plate.BgColor = _plateFill.Lerp(color, 0.07f) with { A = _plateFill.A };
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
        var icon = (ColorRect)LoadPreviewIcon("Fallback");
        icon.Color = GetPropertyColor(type);
        return icon;
    }

    private static ColorRect CreateFallbackActionIcon(Color color)
    {
        var icon = (ColorRect)LoadPreviewIcon("Fallback");
        icon.Color = color;
        return icon;
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
