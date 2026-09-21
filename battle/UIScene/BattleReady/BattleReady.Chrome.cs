using System;
using Godot;

/// <summary>
/// Screen chrome for the pre-battle tactical console: the framing band at the top, the
/// deployment-brief column on the right, the footer readout, and the corner marks.
/// Everything here is decorative or read-only, so it never touches assembly state.
/// </summary>
public partial class BattleReady
{
    private const float ChromeContentLeft = 248f;
    private const float ChromeContentRight = 1448f;
    private const float ChromeContentTop = 292f;
    private const float ChromeContentBottom = 952f;
    private const float ChromeColumnLeft = 1488f;
    private const float ChromeColumnWidth = 344f;
    private const float ChromeColumnTop = 200f;
    private const float ChromeColumnHeight = 752f;
    private static readonly Vector2 ChromeScreenSize = new(1920f, 1080f);

    private Control _chromeRoot;
    private Label _chromeEyebrow;
    private Label _chromeTitle;
    private Label _chromeFooterRight;
    private Control _briefBody;
    private Control _deploymentBrief;
    private Tween _deploymentBriefTween;

    private static readonly string[] ChromeUnitCodes = ["01", "02", "03", "04"];

    private readonly record struct ModeCaption(string Eyebrow, string TitleKey, string TitleFallback);

    private static ModeCaption GetModeCaption(BattleReadyMode mode) =>
        mode switch
        {
            BattleReadyMode.Talent => new("TALENT MATRIX // 天赋矩阵", "ui.battle_ready.mode_talent", "天赋树"),
            _ => new("TACTICAL LOADOUT // 战术编成", "ui.battle_ready.mode_tactics", "技能一览"),
        };

    private void BuildChrome()
    {
        if (_chromeRoot != null && GodotObject.IsInstanceValid(_chromeRoot))
            return;

        // The scene root is a zero-sized Control, so anchored children would collapse into the
        // top-left corner. Everything here is laid out against the design resolution instead.
        _chromeRoot = new Control
        {
            Name = "ConsoleChrome",
            Size = ChromeScreenSize,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_chromeRoot);
        // Sits above the starfield background but below the interactive mode content.
        MoveChild(_chromeRoot, 1);

        _chromeRoot.AddChild(BattleReadyStyle.CornerBrackets(ChromeScreenSize));
        BuildChromeHeader();
        BuildChromeFooter();
        BuildDeploymentBrief();
        BuildTacticsBackdrop();
        BuildTalentBackdrop();
    }

    /// <summary>
    /// Labels the loadout grid without placing it on a solid rectangular plate. The cards remain
    /// the visual units; this just provides an alignment rule for scanning the section.
    /// </summary>
    private void BuildTacticsBackdrop()
    {
        var size = new Vector2(ChromeContentRight - ChromeContentLeft, ChromeContentBottom - ChromeContentTop);
        var backdrop = new Control
        {
            Name = "LoadoutBackdrop",
            Position = new Vector2(ChromeContentLeft, ChromeContentTop),
            Size = size,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var caption = BattleReadyStyle.MicroLabel("LOADOUT // EQUIPPED SKILLS", 14, 0.42f);
        caption.Position = new Vector2(22f, 12f);
        backdrop.AddChild(caption);
        AddOpenSectionRule(backdrop, new Vector2(22f, 36f), size.X - 44f);

        TacticsModeRoot.AddChild(backdrop);
        TacticsModeRoot.MoveChild(backdrop, 0);
    }

    /// <summary>Labels the talent constellation without a boxed backdrop.</summary>
    private void BuildTalentBackdrop()
    {
        var backdrop = new Control
        {
            Name = "TalentBackdrop",
            Position = new Vector2(528f, 402f),
            Size = new Vector2(640f, 440f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var caption = BattleReadyStyle.MicroLabel("TALENT MATRIX // STAGE 01-03", 14, 0.42f);
        caption.Position = new Vector2(20f, 14f);
        backdrop.AddChild(caption);
        AddOpenSectionRule(backdrop, new Vector2(20f, 38f), 600f);

        TalentModeRoot.AddChild(backdrop);
        TalentModeRoot.MoveChild(backdrop, 0);
    }

    private void BuildChromeHeader()
    {
        _chromeEyebrow = BattleReadyStyle.MicroLabel(string.Empty, 18, 0.8f);
        _chromeEyebrow.Position = new Vector2(96f, 44f);
        _chromeRoot.AddChild(_chromeEyebrow);

        _chromeTitle = BattleReadyStyle.DisplayLabel(string.Empty, 52, BattleReadyStyle.GoldBright);
        _chromeTitle.Position = new Vector2(92f, 70f);
        _chromeRoot.AddChild(_chromeTitle);

        var rule = BattleReadyStyle.TitleRule(180f);
        rule.Position = new Vector2(96f, 152f);
        rule.Size = new Vector2(1736f, 12f);
        _chromeRoot.AddChild(rule);
    }

    private void BuildChromeFooter()
    {
        var footer = new HBoxContainer
        {
            Position = new Vector2(96f, 986f),
            Size = new Vector2(1736f, 24f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        footer.AddThemeConstantOverride("separation", 16);
        _chromeRoot.AddChild(footer);

        footer.AddChild(BattleReadyStyle.MicroLabel("SECTOR // COMBAT PREP", 18, 0.85f));

        var hatch = BattleReadyStyle.HatchBar(280f, 10f, 1f, BattleReadyStyle.Steel);
        hatch.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        footer.AddChild(hatch);

        footer.AddChild(
            new ColorRect
            {
                CustomMinimumSize = new Vector2(0f, 1f),
                Color = BattleReadyStyle.Steel with { A = 0.14f },
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            }
        );

        _chromeFooterRight = BattleReadyStyle.MicroLabel(string.Empty, 18, 0.85f);
        footer.AddChild(_chromeFooterRight);
    }

    private void BuildDeploymentBrief()
    {
        var column = new Control
        {
            Name = "DeploymentBrief",
            Position = new Vector2(ChromeColumnLeft, ChromeColumnTop),
            Size = new Vector2(ChromeColumnWidth, ChromeColumnHeight),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _deploymentBrief = column;
        _chromeRoot.AddChild(column);

        AddOpenPanelMarkers(column, inset: 0f);

        var margin = new MarginContainer
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        column.AddChild(margin);

        _briefBody = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        ((VBoxContainer)_briefBody).AddThemeConstantOverride("separation", 14);
        margin.AddChild(_briefBody);
    }

    private static void AddOpenSectionRule(Control host, Vector2 position, float width)
    {
        host.AddChild(
            new ColorRect
            {
                Position = position,
                Size = new Vector2(Mathf.Max(width, 0f), 1f),
                Color = BattleReadyStyle.Steel with { A = 0.24f },
                MouseFilter = MouseFilterEnum.Ignore,
            }
        );
    }

    private static void AddOpenPanelMarkers(Control host, float inset = 14f)
    {
        var topRule = new ColorRect
        {
            AnchorLeft = 0f,
            AnchorRight = 1f,
            OffsetLeft = inset,
            OffsetTop = inset,
            OffsetRight = -inset,
            OffsetBottom = inset + 1f,
            Color = BattleReadyStyle.Steel with { A = 0.22f },
            MouseFilter = MouseFilterEnum.Ignore,
        };
        host.AddChild(topRule);
        host.MoveChild(topRule, 0);

        var sideMarker = new ColorRect
        {
            AnchorTop = 0f,
            AnchorBottom = 1f,
            OffsetLeft = inset,
            OffsetTop = inset,
            OffsetRight = inset + 2f,
            OffsetBottom = -inset,
            Color = BattleReadyStyle.GoldBright with { A = 0.56f },
            MouseFilter = MouseFilterEnum.Ignore,
        };
        host.AddChild(sideMarker);
        host.MoveChild(sideMarker, 0);
    }

    /// <summary>Repaints the header caption and the deployment brief for the current selection.</summary>
    private void RefreshChrome(bool animateBrief = true)
    {
        if (_chromeRoot == null || !GodotObject.IsInstanceValid(_chromeRoot))
            return;

        ModeCaption caption = GetModeCaption(_currentMode);
        _chromeEyebrow.Text = caption.Eyebrow;
        _chromeTitle.Text = I18n.Tr(caption.TitleKey, caption.TitleFallback);

        PlayerInfoStructure[] players = GameInfo.PlayerCharacters ?? [];
        bool hasUnit = _selectedCharacterIndex >= 0 && _selectedCharacterIndex < players.Length;
        string unitCode = hasUnit && _selectedCharacterIndex < ChromeUnitCodes.Length
            ? ChromeUnitCodes[_selectedCharacterIndex]
            : "--";

        _chromeFooterRight.Text = $"UNIT {unitCode} // {players.Length} DEPLOYED";

        RebuildDeploymentBrief(players, hasUnit ? players[_selectedCharacterIndex] : default, hasUnit, unitCode);
        if (animateBrief)
            AnimateDeploymentBrief();
    }

    private void RebuildDeploymentBrief(
        PlayerInfoStructure[] players,
        PlayerInfoStructure unit,
        bool hasUnit,
        string unitCode
    )
    {
        if (_briefBody == null || !GodotObject.IsInstanceValid(_briefBody))
            return;

        foreach (Node child in _briefBody.GetChildren())
        {
            _briefBody.RemoveChild(child);
            child.QueueFree();
        }

        _briefBody.AddChild(BattleReadyStyle.MicroLabel("DEPLOYMENT BRIEF", 15, 0.5f));

        string unitName = hasUnit && !string.IsNullOrWhiteSpace(unit.CharacterName)
            ? unit.CharacterName
            : I18n.Tr("ui.common.no_selection", "未选择");
        _briefBody.AddChild(
            BattleReadyStyle.DisplayLabel(unitName, 32, BattleReadyStyle.GoldBright)
        );
        _briefBody.AddChild(BattleReadyStyle.MicroLabel($"UNIT {unitCode}", 14, 0.42f));
        _briefBody.AddChild(BattleReadyStyle.Hairline1());

        if (!hasUnit)
            return;

        AddBriefAttributeBlock(unit);
        _briefBody.AddChild(BattleReadyStyle.Hairline1());
        AddBriefTalentBlock(unit);
        _briefBody.AddChild(BattleReadyStyle.Hairline1());
        AddBriefPassiveBlock(unit);
    }

    private void AnimateDeploymentBrief()
    {
        if (_deploymentBrief == null || !GodotObject.IsInstanceValid(_deploymentBrief))
            return;

        _deploymentBriefTween?.Kill();
        _deploymentBrief.Position = new Vector2(ChromeColumnLeft + 18f, ChromeColumnTop);
        _deploymentBrief.Modulate = _deploymentBrief.Modulate with { A = 0.0f };

        _deploymentBriefTween = CreateTween();
        _deploymentBriefTween.SetParallel(true);
        _deploymentBriefTween.SetEase(Tween.EaseType.Out);
        _deploymentBriefTween.SetTrans(Tween.TransitionType.Cubic);
        _deploymentBriefTween.TweenProperty(
            _deploymentBrief,
            "position",
            new Vector2(ChromeColumnLeft, ChromeColumnTop),
            0.22f
        );
        _deploymentBriefTween.TweenProperty(_deploymentBrief, "modulate:a", 1.0f, 0.18f);
    }

    private void AddBriefAttributeBlock(PlayerInfoStructure unit)
    {
        _briefBody.AddChild(BattleReadyStyle.MicroLabel("UNIT ATTRIBUTES // 角色属性", 15, 0.5f));
        AddBriefStatRow(
            I18n.Tr("ui.common.life", "生命"),
            unit.LifeMax.ToString(),
            new Color(1f, 0.48f, 0.52f)
        );
        AddBriefStatRow(
            I18n.Tr("property.power", "力量"),
            TalentTree.GetEffectivePower(unit).ToString(),
            new Color(1f, 0.78f, 0.38f)
        );
        AddBriefStatRow(
            I18n.Tr("property.survivability", "生存"),
            TalentTree.GetEffectiveSurvivability(unit).ToString(),
            new Color(0.54f, 1f, 0.99f)
        );
    }

    private void AddBriefStatRow(string name, string value, Color color)
    {
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        _briefBody.AddChild(row);

        var label = BattleReadyStyle.TextLabel(name, 18, BattleReadyStyle.Steel with { A = 0.88f });
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(label);

        var readout = BattleReadyStyle.DisplayLabel(value, 26, color);
        readout.HorizontalAlignment = HorizontalAlignment.Right;
        readout.CustomMinimumSize = new Vector2(72f, 32f);
        row.AddChild(readout);
    }

    private void AddBriefTalentBlock(PlayerInfoStructure unit)
    {
        _briefBody.AddChild(BattleReadyStyle.MicroLabel("TALENT POINTS", 15, 0.5f));

        int unlocked = unit.UnlockedTalents?.Count ?? 0;
        int total = TalentTree.GetNodes(unit)?.Count ?? 0;

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        _briefBody.AddChild(row);

        Color pointColor = unit.TalentPoints > 0
            ? BattleReadyStyle.GoldBright
            : BattleReadyStyle.Steel with { A = 0.55f };
        var value = BattleReadyStyle.DisplayLabel(unit.TalentPoints.ToString(), 44, pointColor);
        value.CustomMinimumSize = new Vector2(46f, 52f);
        value.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(value);

        var caption = BattleReadyStyle.TextLabel(
            I18n.Format(
                "ui.battle_ready.talent_progress",
                "已点亮 {value}/{total}",
                ("value", unlocked),
                ("total", total)
            ),
            17,
            BattleReadyStyle.Steel with { A = 0.9f },
            HorizontalAlignment.Right
        );
        caption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        caption.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        caption.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(caption);

        _briefBody.AddChild(
            BattleReadyStyle.HatchBar(
                ChromeColumnWidth - 48f,
                8f,
                total <= 0 ? 0f : unlocked / (float)total,
                BattleReadyStyle.Gold
            )
        );
    }

    private void AddBriefPassiveBlock(PlayerInfoStructure unit)
    {
        _briefBody.AddChild(BattleReadyStyle.MicroLabel("PASSIVE // 被动", 15, 0.5f));

        string passiveName = string.IsNullOrWhiteSpace(unit.PassiveName)
            ? I18n.Tr("ui.common.passive", "被动")
            : unit.PassiveName;
        _briefBody.AddChild(BattleReadyStyle.TextLabel(passiveName, 20, BattleReadyStyle.GoldBright));

        string description = TalentTree.GetPassiveDescription(unit);
        description = GlobalFunction.ColorizeKeywords(GlobalFunction.ColorizeNumbers(description));

        var detail = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(0f, 116f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            BbcodeEnabled = true,
            ScrollActive = true,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        detail.AddThemeFontSizeOverride("normal_font_size", 16);
        detail.Text = description;
        _briefBody.AddChild(detail);
    }
}
