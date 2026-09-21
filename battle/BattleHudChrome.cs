using System;
using Godot;

// Battle-only presentation. Existing controls retain their input and gameplay wiring.
public partial class BattleHudChrome : Control {
    public static readonly Color Ink = new(0.045f, 0.058f, 0.078f, 0.94f);
    public static readonly Color Paper = new(0.88f, 0.91f, 0.91f);
    public static readonly Color Accent = new(0.66f, 0.80f, 0.78f);
    private static readonly Color Muted = new(0.48f, 0.56f, 0.61f);
    private static readonly SystemFont UiFont = new() { FontNames = new[] { "Microsoft YaHei UI", "Segoe UI" } };
    private Battle _battle;
    private Label _turn;
    private Label _encounter;
    private float _refresh;
    private Button _endTurn;
    private ShaderMaterial _endTurnGlass;
    private float _glassHover;
    private float _glassPress;

    public override void _Ready() {
        MouseFilter = MouseFilterEnum.Ignore;
        _battle = GetParent()?.GetParent() as Battle;
        if (_battle == null)
            return;
        BuildFraming();
        StyleActions();
        RefreshLabels();
    }

    private void BuildFraming() {
        // Open edges and local plates leave the arena unobstructed.
        AddRule(new Vector2(64, 62), new Vector2(3, 60), Accent);
        AddCaption("FOUR DIMENSIONAL", new Vector2(82, 37), new Vector2(340, 20), 13, Muted);
        _turn = AddCaption("", new Vector2(82, 62), new Vector2(350, 42), 30, Paper);
        _encounter = AddCaption("", new Vector2(1430, 43), new Vector2(250, 30), 17, Muted);
        _encounter.HorizontalAlignment = HorizontalAlignment.Right;
        AddRule(new Vector2(64, 143), new Vector2(355, 1), new Color(0.6f, 0.7f, 0.74f, 0.18f));

        var fade = new GradientTexture2D {
            Width = 8, Height = 256, FillFrom = Vector2.Zero, FillTo = Vector2.Down,
            Gradient = new Gradient {
                Offsets = new[] { 0f, 0.5f, 1f },
                Colors = new[] { new Color(0.02f, 0.03f, 0.045f, 0f), new Color(0.02f, 0.03f, 0.045f, 0.54f), new Color(0.02f, 0.03f, 0.045f, 0.94f) }
            }
        };
        AddChild(new TextureRect {
            Position = new Vector2(0, 725), Size = new Vector2(1920, 355),
            Texture = fade, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore, ZIndex = -1
        });
        AddCaption(I18n.Tr("ui.battle.hud.energy", "可用能量"), new Vector2(94, 786), new Vector2(168, 28), 17, Muted).HorizontalAlignment = HorizontalAlignment.Center;
        AddCaption(I18n.Tr("ui.battle.hud.hand_hint", "选择卡牌 · 制定行动"), new Vector2(680, 756), new Vector2(560, 28), 15, Muted).HorizontalAlignment = HorizontalAlignment.Center;
        AddRule(new Vector2(342, 770), new Vector2(275, 1), new Color(0.6f, 0.7f, 0.74f, 0.16f));
        AddRule(new Vector2(1303, 770), new Vector2(275, 1), new Color(0.6f, 0.7f, 0.74f, 0.16f));
        AddCaption(MobilePlatform.IsMobile ? "" : I18n.Tr("ui.battle.hud.end_shortcut", "E  /  结束回合"), new Vector2(1640, 931), new Vector2(216, 25), 13, Muted).HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void StyleActions() {
        var layer = _battle.GetNode<CanvasLayer>("CharacterControlLayer");
        var energy = layer.GetNode<ColorRect>("EnergyBG");
        energy.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shader/UI/BattleEnergyDial.gdshader") };
        energy.MouseFilter = MouseFilterEnum.Ignore;
        Label number = energy.GetNode<Label>("EnergyLabel");
        number.AddThemeFontOverride("font", UiFont);
        number.AddThemeFontSizeOverride("font_size", 58);
        number.AddThemeColorOverride("font_color", Paper);
        number.AddThemeConstantOverride("outline_size", 0);

        var actions = layer.GetNode<Control>("BattleActionButtons");
        StyleButton(actions.GetNode<Button>("EndTurnButton"), true);
        BuildEndTurnGlass(actions.GetNode<Button>("EndTurnButton"));
        StyleButton(_battle.RecordButton, false);
        AddPileCaption(actions.GetNode<Button>("DrawPileButton"), I18n.Tr("ui.common.draw_pile", "抽牌堆"));
        AddPileCaption(actions.GetNode<Button>("DiscardPileButton"), I18n.Tr("ui.common.discard_pile", "弃牌堆"));
        AddPileCaption(actions.GetNode<Button>("ExhaustedPileButton"), I18n.Tr("ui.common.exhaust_pile", "消耗牌堆"));
    }

    private static void AddPileCaption(Button button, string text) {
        var label = new Label {
            Name = "PileCaption", Text = text,
            Position = new Vector2(0, button.Size.Y + 7), Size = new Vector2(button.Size.X, 22),
            HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontOverride("font", UiFont);
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", Muted);
        button.AddChild(label);
    }

    private void BuildEndTurnGlass(Button button) {
        _endTurn = button;
        _endTurnGlass = new ShaderMaterial { Shader = GD.Load<Shader>("res://shader/UI/BattleCrystalButton.gdshader") };
        foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "hover_pressed" })
            button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        button.AddThemeColorOverride("font_color", Paper);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Accent);
        var glass = new ColorRect {
            Name = "CrystalSurface", Material = _endTurnGlass,
            MouseFilter = MouseFilterEnum.Ignore, ShowBehindParent = true
        };
        button.AddChild(glass);
        glass.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public static StyleBoxFlat Plate(Color fill, Color border, int radius = 3) => new() {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 6
    };

    private static void StyleButton(Button button, bool primary) {
        button.AddThemeFontOverride("font", UiFont);
        button.AddThemeFontSizeOverride("font_size", primary ? 22 : 17);
        button.AddThemeConstantOverride("outline_size", 0);
        button.AddThemeColorOverride("font_color", primary ? Ink : Paper);
        button.AddThemeColorOverride("font_hover_color", primary ? Ink : Colors.White);
        button.AddThemeColorOverride("font_pressed_color", primary ? Ink : Accent);
        button.AddThemeColorOverride("font_disabled_color", Muted);
        button.AddThemeStyleboxOverride("normal", Plate(primary ? Accent : Ink, primary ? Accent : new Color(0.38f, 0.47f, 0.51f, 0.55f)));
        button.AddThemeStyleboxOverride("hover", Plate(primary ? Paper : new Color(0.10f, 0.15f, 0.18f), Accent));
        button.AddThemeStyleboxOverride("pressed", Plate(primary ? new Color(0.48f, 0.66f, 0.65f) : Ink, Accent));
        button.AddThemeStyleboxOverride("disabled", Plate(new Color(0.065f, 0.085f, 0.10f, 0.9f), new Color(0.3f, 0.38f, 0.42f, 0.3f)));
        button.MouseDefaultCursorShape = CursorShape.PointingHand;
    }

    private Label AddCaption(string text, Vector2 position, Vector2 size, int fontSize, Color color) {
        var label = new Label { Text = text, Position = position, Size = size, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", UiFont);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        AddChild(label);
        return label;
    }

    private void AddRule(Vector2 position, Vector2 size, Color color) =>
        AddChild(new ColorRect { Position = position, Size = size, Color = color, MouseFilter = MouseFilterEnum.Ignore });

    public override void _Process(double delta) {
        if (GodotObject.IsInstanceValid(_endTurn) && _endTurnGlass != null) {
            float blend = 1f - Mathf.Exp(-12f * (float)delta);
            _glassHover = Mathf.Lerp(_glassHover, _endTurn.IsHovered() && !_endTurn.Disabled ? 1f : 0f, blend);
            _glassPress = Mathf.Lerp(_glassPress, _endTurn.ButtonPressed ? 1f : 0f, blend);
            _endTurnGlass.SetShaderParameter("panel_size", _endTurn.Size);
            _endTurnGlass.SetShaderParameter("hover_amount", _glassHover);
            _endTurnGlass.SetShaderParameter("press_amount", _glassPress);
            _endTurnGlass.SetShaderParameter("disabled_amount", _endTurn.Disabled ? 1f : 0f);
        }
        _refresh -= (float)delta;
        if (_refresh > 0f)
            return;
        _refresh = 0.15f;
        RefreshLabels();
    }

    private void RefreshLabels() {
        if (!GodotObject.IsInstanceValid(_battle) || _turn == null)
            return;
        _turn.Text = I18n.Format("ui.battle.hud.turn", "第 {number} 回合", ("number", Math.Max(1, _battle.ElapsedTurnCount).ToString("00")));
        _encounter.Text = _battle.CurrentLevelNode?.Type switch {
            LevelNode.LevelType.Elite => I18n.Tr("ui.battle.hud.elite", "精英遭遇"),
            LevelNode.LevelType.Boss => I18n.Tr("ui.battle.hud.boss", "首领战"),
            _ => I18n.Tr("ui.battle.hud.normal", "战斗遭遇")
        };
    }

    public static void StyleCharacter(Character character) {
        var bar = character.LifeBar;
        if (bar.HasMeta("battle_hud_styled"))
            return;
        bar.SetMeta("battle_hud_styled", true);
        Label life = character.LifeLabel;
        life.SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        life.Position = new Vector2(0f, -31f);
        life.Size = new Vector2(175f, 28f);
        life.Scale = Vector2.One;
        life.HorizontalAlignment = HorizontalAlignment.Right;
        life.VerticalAlignment = VerticalAlignment.Center;
        life.AddThemeFontOverride("font", UiFont);
        life.AddThemeFontSizeOverride("font_size", 24);
        life.AddThemeConstantOverride("outline_size", 2);
        life.AddThemeColorOverride("font_color", Paper);
        // Reset after changing the font: the old 50px face imposed a taller minimum.
        life.Size = new Vector2(175f, 28f);
        var name = new Label {
            Name = "BattleUnitName", Text = character.CharacterName,
            Position = new Vector2(0f, -31f), Size = new Vector2(88f, 28f),
            MouseFilter = MouseFilterEnum.Ignore, ClipText = true
        };
        name.AddThemeFontOverride("font", UiFont);
        name.AddThemeFontSizeOverride("font_size", 19);
        name.AddThemeColorOverride("font_color", character.IsPlayer ? Accent : new Color(0.80f, 0.65f, 0.63f));
        bar.AddChild(name);
        var plate = new Panel {
            Position = new Vector2(-9, -37), Size = new Vector2(193, 55),
            MouseFilter = MouseFilterEnum.Ignore, ShowBehindParent = true, ZIndex = -1
        };
        plate.AddThemeStyleboxOverride("panel", Plate(new Color(0.03f, 0.045f, 0.06f, 0.66f), new Color(0.42f, 0.51f, 0.56f, 0.22f)));
        bar.AddChild(plate);
    }
}
