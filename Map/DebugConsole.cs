using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class DebugConsole : CanvasLayer
{
    private sealed class CommandDefinition
    {
        public CommandDefinition(
            string name,
            string usage,
            string description,
            string[] argumentHints,
            params string[] aliases
        )
        {
            Name = name;
            Usage = usage;
            Description = description;
            ArgumentHints = argumentHints ?? Array.Empty<string>();
            Aliases = aliases ?? Array.Empty<string>();
        }

        public string Name { get; }
        public string Usage { get; }
        public string Description { get; }
        public string[] ArgumentHints { get; }
        public string[] Aliases { get; }
    }

    private sealed class CompletionItem
    {
        public CompletionItem(string insertText, string displayText, params string[] searchTerms)
        {
            InsertText = insertText;
            DisplayText = displayText;
            SearchTerms = searchTerms ?? Array.Empty<string>();
        }

        public string InsertText { get; }
        public string DisplayText { get; }
        public string[] SearchTerms { get; }
    }

    private readonly struct InputContext(string raw, string[] tokens, bool endsWithSpace)
    {
        public string Raw { get; } = raw;
        public string[] Tokens { get; } = tokens ?? Array.Empty<string>();
        public bool EndsWithSpace { get; } = endsWithSpace;
        public int TargetTokenIndex => EndsWithSpace ? Tokens.Length : Math.Max(Tokens.Length - 1, 0);
        public string CurrentToken =>
            Tokens.Length == 0 || EndsWithSpace ? string.Empty : Tokens[^1];
    }

    private const float PanelWidth = 1460f;
    private const float PanelHeight = 780f;
    private const string AccentColor = "#3794ff";
    private const string SuccessColor = "#89d185";
    private const string ErrorColor = "#f48771";
    private const string NeutralColor = "#d4d4d4";
    private const string DimColor = "#858585";
    private const string EditorBorderColor = "#3c3c3c";
    private const string EditorSelectionColor = "#264f78";
    private const string EditorSelectionAccent = "#3794ff";
    private const string EditorMutedColor = "#808080";
    private const string EditorKeywordColor = "#4fc1ff";
    private const string EditorSymbolColor = "#c586c0";
    private const int MaxVisibleCompletions = 10;
    private static readonly SkillID[] StarterSkillIds =
    [
        SkillID.BasicAttack,
        SkillID.BasicDefense,
        SkillID.BasicGuard,
        SkillID.BasicSpecial,
    ];

    private static readonly CommandDefinition[] CommandDefinitions =
    [
        new("help", "help", "显示控制台帮助。", Array.Empty<string>(), "帮助"),
        new("clear", "clear", "清空输出日志。", Array.Empty<string>(), "清屏"),
        new("players", "players", "列出当前角色和基础属性。", Array.Empty<string>(), "角色"),
        new(
            "catalog",
            "catalog <skill|equipment|relic|item> [角色]",
            "查看技能池或可添加对象列表。",
            ["类型：skill/equipment/relic/item", "当类型为 skill 时填角色名或序号"],
            "列表"
        ),
        new(
            "addskill",
            "addskill <角色> <技能ID/技能名>",
            "向角色牌库添加技能。",
            ["角色名或 1-4", "技能 ID 或技能名"],
            "加技能"
        ),
        new(
            "addequipment",
            "addequipment <装备ID/装备名> [数量]",
            "向装备库存添加装备。",
            ["装备 ID 或装备名", "可选：数量"],
            "加装备"
        ),
        new(
            "addrelic",
            "addrelic <遗物ID/遗物名> [数量]",
            "向遗物栏添加遗物。",
            ["遗物 ID 或遗物名", "可选：数量"],
            "加遗物"
        ),
        new(
            "additem",
            "additem <道具ID/道具名> [数量]",
            "向道具栏添加道具。",
            ["道具 ID 或道具名", "可选：数量"],
            "加道具",
            "加药水"
        ),
        new(
            "setstat",
            "setstat <角色> <power|survivability|speed|maxlife> <值>",
            "将角色基础属性直接设为指定值。",
            ["角色名或 1-4", "属性类型", "目标值"],
            "设属性"
        ),
        new(
            "addstat",
            "addstat <角色> <power|survivability|speed|maxlife> <增量>",
            "在角色基础属性上增减数值。",
            ["角色名或 1-4", "属性类型", "增量"],
            "加属性"
        ),
        new(
            "setresource",
            "setresource <coin|energy|maxenergy> <值>",
            "修改电币、当前能量或能量上限。",
            ["资源类型", "目标值"],
            "设资源"
        ),
        new(
            "battletest",
            "battletest <on|off|toggle>",
            "开关 Battle.Istest 测试模式。",
            ["on/off/toggle，也支持 true/false/1/0/开/关"],
            "战斗测试",
            "bt"
        ),
        new("save", "save", "手动保存当前 GameInfo。", Array.Empty<string>(), "保存"),
    ];

    private Control _root;
    private ColorRect _backdrop;
    private PanelContainer _panel;
    private PanelContainer _completionPopup;
    private LineEdit _input;
    private RichTextLabel _help;
    private RichTextLabel _assistHint;
    private RichTextLabel _completionList;
    private RichTextLabel _completionDetails;
    private RichTextLabel _log;
    private readonly List<string> _history = new();
    private readonly List<CompletionItem> _visibleCompletions = new();
    private int _historyIndex = -1;
    private int _selectedCompletionIndex = -1;
    private string _lastObservedInput = string.Empty;
    private InputContext _lastInputContext = new(string.Empty, Array.Empty<string>(), false);

    public bool IsOpen => Visible;
    private Map MapNode => field ??= GetParent() as Map ?? GetNodeOrNull<Map>("/root/Map");
    private PlayerResourceState ResourceState =>
        field ??= GetNodeOrNull<PlayerResourceState>("/root/Map/PlayerResourceState");

    public override void _Ready()
    {
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always;
        if (!TryBindSceneUi())
            BuildUi();

        ApplySharedUiState();
        CloseImmediate();
        AppendInfo("调试控制台已就绪。按 ~ 或右上角齿轮打开。");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            return;

        if (keyEvent.Keycode == Key.Quoteleft)
        {
            ToggleOpen();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (Visible && keyEvent.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible || _input == null)
            return;

        UpdateCompletionPopupLayout();

        string current = _input.Text ?? string.Empty;
        if (!string.Equals(current, _lastObservedInput, StringComparison.Ordinal))
        {
            _lastObservedInput = current;
            RefreshAssistState();
        }
    }

    public void ToggleOpen()
    {
        if (Visible)
            Close();
        else
            Open();
    }

    public void Open()
    {
        Visible = true;
        _lastObservedInput = _input?.Text ?? string.Empty;
        RefreshAssistState();
        _input?.GrabFocus();
        _input?.SelectAll();
    }

    public void Close()
    {
        Visible = false;
        ClearCompletionState();
        ReleaseFocusFromInput();
    }

    private void CloseImmediate()
    {
        Visible = false;
        ReleaseFocusFromInput();
    }

    private void ReleaseFocusFromInput()
    {
        if (_input != null && _input.HasFocus())
            _input.ReleaseFocus();
    }

    private bool TryBindSceneUi()
    {
        _root = GetNodeOrNull<Control>("Root");
        _backdrop = GetNodeOrNull<ColorRect>("Root/Backdrop");
        _panel = GetNodeOrNull<PanelContainer>("Root/Center/Panel");
        _completionPopup = GetNodeOrNull<PanelContainer>("Root/SuggestPopup");
        _input = GetNodeOrNull<LineEdit>("Root/Center/Panel/Margin/Layout/InputRow/CommandInput");
        _assistHint = GetNodeOrNull<RichTextLabel>(
            "Root/Center/Panel/Margin/Layout/AssistSection/AssistHint"
        );
        _completionList = GetNodeOrNull<RichTextLabel>(
            "Root/SuggestPopup/PopupLayout/CompletionList"
        );
        _completionDetails = GetNodeOrNull<RichTextLabel>(
            "Root/SuggestPopup/PopupLayout/CompletionDetails"
        );
        _help = GetNodeOrNull<RichTextLabel>("Root/Center/Panel/Margin/Layout/Help");
        _log = GetNodeOrNull<RichTextLabel>("Root/Center/Panel/Margin/Layout/Log");

        Button runButton = GetNodeOrNull<Button>("Root/Center/Panel/Margin/Layout/InputRow/RunButton");
        Button clearButton = GetNodeOrNull<Button>(
            "Root/Center/Panel/Margin/Layout/InputRow/ClearButton"
        );
        Button helpButton = GetNodeOrNull<Button>("Root/Center/Panel/Margin/Layout/InputRow/HelpButton");
        Label title = GetNodeOrNull<Label>("Root/Center/Panel/Margin/Layout/Header/Title");
        Label subtitle = GetNodeOrNull<Label>("Root/Center/Panel/Margin/Layout/Header/Subtitle");

        if (
            _root == null
            || _panel == null
            || _completionPopup == null
            || _input == null
            || _assistHint == null
            || _completionList == null
            || _completionDetails == null
            || _help == null
            || _log == null
            || runButton == null
            || clearButton == null
            || helpButton == null
            || title == null
            || subtitle == null
        )
        {
            return false;
        }

        _panel.CustomMinimumSize = new Vector2(PanelWidth, PanelHeight);
        _panel.AddThemeStyleboxOverride("panel", BuildPanelStyle());

        title.Text = "GM Console";
        title.AddThemeFontSizeOverride("font_size", 34);
        title.Modulate = new Color(0.84f, 0.95f, 1f, 1f);

        subtitle.Text = "支持加技能、装备、遗物、药水，以及改角色属性。输入 help 查看命令，~ / ESC 关闭。";
        subtitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        subtitle.AddThemeFontSizeOverride("font_size", 18);
        subtitle.Modulate = new Color(0.72f, 0.82f, 0.92f, 0.92f);

        _input.PlaceholderText = "例如：addskill Nightingale BreakStrike  或  setstat Echo power 20";
        _input.ClearButtonEnabled = true;
        _input.AddThemeFontSizeOverride("font_size", 20);
        _input.AddThemeColorOverride("font_color", new Color(NeutralColor));
        _input.AddThemeColorOverride("caret_color", new Color(AccentColor));
        _input.AddThemeColorOverride("font_placeholder_color", new Color("#6a9955"));
        _input.AddThemeStyleboxOverride("normal", BuildInputStyle());
        _input.AddThemeStyleboxOverride("focus", BuildInputStyle(focused: true));
        _input.TextSubmitted -= OnInputSubmitted;
        _input.TextSubmitted += OnInputSubmitted;
        _input.GuiInput -= OnInputGuiInput;
        _input.GuiInput += OnInputGuiInput;

        runButton.Text = "执行";
        runButton.Pressed -= ExecuteCurrentInput;
        runButton.Pressed += ExecuteCurrentInput;
        runButton.AddThemeFontSizeOverride("font_size", 19);
        ApplyButtonTheme(runButton);

        clearButton.Text = "清屏";
        clearButton.Pressed -= ClearLog;
        clearButton.Pressed += ClearLog;
        clearButton.AddThemeFontSizeOverride("font_size", 19);
        ApplyButtonTheme(clearButton);

        helpButton.Text = "帮助";
        helpButton.Pressed -= RefreshHelp;
        helpButton.Pressed += RefreshHelp;
        helpButton.AddThemeFontSizeOverride("font_size", 19);
        ApplyButtonTheme(helpButton);

        return true;
    }

    private void ApplySharedUiState()
    {
        if (_backdrop != null)
            _backdrop.Color = new Color(0.02f, 0.05f, 0.08f, 0.46f);

        _help?.AddThemeFontSizeOverride("normal_font_size", 18);
        _help?.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#252526")));
        if (_help != null)
            _help.Text = BuildHelpText();

        _log?.AddThemeFontSizeOverride("normal_font_size", 18);
        _log?.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#1e1e1e")));

        _assistHint?.AddThemeFontSizeOverride("normal_font_size", 17);
        _assistHint?.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#252526")));

        _completionList?.AddThemeFontSizeOverride("normal_font_size", 16);
        _completionList?.AddThemeStyleboxOverride("normal", BuildPopupColumnStyle(withLeftDivider: false));

        _completionDetails?.AddThemeFontSizeOverride("normal_font_size", 15);
        _completionDetails?.AddThemeStyleboxOverride("normal", BuildPopupColumnStyle(withLeftDivider: true));

        if (_completionPopup != null)
        {
            _completionPopup.AddThemeStyleboxOverride("panel", BuildPopupStyle());
            _completionPopup.Visible = false;
        }
    }

    private void OnInputSubmitted(string _)
    {
        ExecuteCurrentInput();
    }

    private void ClearLog()
    {
        if (_log != null)
            _log.Text = string.Empty;
    }

    private void RefreshHelp()
    {
        if (_help != null)
            _help.Text = BuildHelpText();
        AppendInfo("帮助已刷新。");
    }

    private void BuildUi()
    {
        _root = new Control
        {
            Name = "Root",
            MouseFilter = Control.MouseFilterEnum.Stop,
            FocusMode = Control.FocusModeEnum.None,
        };
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _backdrop = new ColorRect
        {
            Name = "Backdrop",
            Color = new Color(0.02f, 0.05f, 0.08f, 0.46f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_backdrop);

        var center = new CenterContainer
        {
            Name = "Center",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);

        _panel = new PanelContainer
        {
            Name = "Panel",
            CustomMinimumSize = new Vector2(PanelWidth, PanelHeight),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _panel.AddThemeStyleboxOverride("panel", BuildPanelStyle());
        center.AddChild(_panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_right", 22);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        _panel.AddChild(margin);

        var layout = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        layout.AddThemeConstantOverride("separation", 12);
        margin.AddChild(layout);

        layout.AddChild(BuildHeader());
        layout.AddChild(BuildInputRow());
        layout.AddChild(BuildAssistSection());

        _help = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = true,
            SelectionEnabled = true,
            CustomMinimumSize = new Vector2(0f, 228f),
            SizeFlagsVertical = Control.SizeFlags.Fill,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _help.AddThemeFontSizeOverride("normal_font_size", 18);
        _help.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#102030")));
        _help.Text = BuildHelpText();
        layout.AddChild(_help);

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = true,
            ScrollFollowing = true,
            SelectionEnabled = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _log.AddThemeFontSizeOverride("normal_font_size", 18);
        _log.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#09131d")));
        layout.AddChild(_log);

        BuildSuggestPopupUi();
    }

    private void BuildSuggestPopupUi()
    {
        if (_root == null)
            return;

        _completionPopup = new PanelContainer
        {
            Name = "SuggestPopup",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var popupLayout = new HBoxContainer
        {
            Name = "PopupLayout",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        popupLayout.AddThemeConstantOverride("separation", 0);
        _completionPopup.AddChild(popupLayout);

        _completionList = new RichTextLabel
        {
            Name = "CompletionList",
            BbcodeEnabled = true,
            ScrollActive = true,
            FitContent = false,
            SelectionEnabled = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(280f, 0f),
        };
        popupLayout.AddChild(_completionList);

        _completionDetails = new RichTextLabel
        {
            Name = "CompletionDetails",
            BbcodeEnabled = true,
            ScrollActive = true,
            FitContent = false,
            SelectionEnabled = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(250f, 0f),
        };
        popupLayout.AddChild(_completionDetails);

        _root.AddChild(_completionPopup);
    }

    private Control BuildAssistSection()
    {
        var section = new VBoxContainer();
        section.AddThemeConstantOverride("separation", 8);

        _assistHint = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollActive = false,
            FitContent = true,
            CustomMinimumSize = new Vector2(0f, 78f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _assistHint.AddThemeFontSizeOverride("normal_font_size", 17);
        _assistHint.AddThemeStyleboxOverride("normal", BuildInnerPanelStyle(new Color("#0d1c2a")));
        section.AddChild(_assistHint);

        return section;
    }

    private Control BuildHeader()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);

        var title = new Label { Text = "GM Console" };
        title.AddThemeFontSizeOverride("font_size", 34);
        title.Modulate = new Color(0.84f, 0.95f, 1f, 1f);
        box.AddChild(title);

        var hint = new Label
        {
            Text = "支持加技能、装备、遗物、药水，以及改角色属性。输入 help 查看命令，~ / ESC 关闭。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        hint.AddThemeFontSizeOverride("font_size", 18);
        hint.Modulate = new Color(0.72f, 0.82f, 0.92f, 0.92f);
        box.AddChild(hint);

        return box;
    }

    private Control BuildInputRow()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        _input = new LineEdit
        {
            PlaceholderText = "例如：addskill Nightingale BreakStrike  或  setstat Echo power 20",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClearButtonEnabled = true,
        };
        _input.AddThemeFontSizeOverride("font_size", 20);
        _input.AddThemeColorOverride("font_color", new Color(NeutralColor));
        _input.AddThemeColorOverride("caret_color", new Color(AccentColor));
        _input.AddThemeColorOverride("font_placeholder_color", new Color("#6a9955"));
        _input.AddThemeStyleboxOverride("normal", BuildInputStyle());
        _input.AddThemeStyleboxOverride("focus", BuildInputStyle(focused: true));
        _input.TextSubmitted += _ => ExecuteCurrentInput();
        _input.GuiInput += OnInputGuiInput;
        row.AddChild(_input);

        var runButton = BuildActionButton("执行");
        runButton.Pressed += ExecuteCurrentInput;
        row.AddChild(runButton);

        var clearButton = BuildActionButton("清屏");
        clearButton.Pressed += () => _log.Text = string.Empty;
        row.AddChild(clearButton);

        var helpButton = BuildActionButton("帮助");
        helpButton.Pressed += () =>
        {
            _help.Text = BuildHelpText();
            AppendInfo("帮助已刷新。");
        };
        row.AddChild(helpButton);

        return row;
    }

    private Button BuildActionButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(108f, 0f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        button.AddThemeFontSizeOverride("font_size", 19);
        ApplyButtonTheme(button);
        return button;
    }

    private static void ApplyButtonTheme(Button button)
    {
        if (button == null)
            return;

        button.AddThemeColorOverride("font_color", new Color(NeutralColor));
        button.AddThemeColorOverride("font_pressed_color", new Color("#ffffff"));
        button.AddThemeColorOverride("font_hover_color", new Color("#ffffff"));
        button.AddThemeStyleboxOverride("normal", BuildButtonStyle(new Color("#2d2d30")));
        button.AddThemeStyleboxOverride("hover", BuildButtonStyle(new Color("#37373d")));
        button.AddThemeStyleboxOverride("pressed", BuildButtonStyle(new Color("#094771")));
        button.AddThemeStyleboxOverride("focus", BuildButtonStyle(new Color("#37373d"), true));
    }

    private static StyleBoxFlat BuildPanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.118f, 0.118f, 0.118f, 0.84f),
            BorderColor = new Color("#3c3c3c"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.45f),
            ShadowSize = 16,
            ShadowOffset = new Vector2(0f, 6f),
        };
    }

    private static StyleBoxFlat BuildInnerPanelStyle(Color bg)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = new Color("#31566f"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4,
            ContentMarginLeft = 10,
            ContentMarginTop = 8,
            ContentMarginRight = 10,
            ContentMarginBottom = 8,
        };
    }

    private static StyleBoxFlat BuildButtonStyle(Color bg, bool focused = false)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = focused ? new Color(AccentColor) : new Color(EditorBorderColor),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4,
            ContentMarginLeft = 12,
            ContentMarginTop = 8,
            ContentMarginRight = 12,
            ContentMarginBottom = 8,
        };
    }

    private static StyleBoxFlat BuildInputStyle(bool focused = false)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color("#181818"),
            BorderColor = focused ? new Color(AccentColor) : new Color(EditorBorderColor),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10,
        };
    }

    private static StyleBoxFlat BuildPopupStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.118f, 0.118f, 0.118f, 0.78f),
            BorderColor = new Color(0.31f, 0.31f, 0.31f, 0.95f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomRight = 5,
            CornerRadiusBottomLeft = 5,
            ContentMarginLeft = 6,
            ContentMarginTop = 6,
            ContentMarginRight = 6,
            ContentMarginBottom = 6,
            ShadowColor = new Color(0f, 0f, 0f, 0.62f),
            ShadowSize = 14,
            ShadowOffset = new Vector2(0f, 6f),
        };
    }

    private static StyleBoxFlat BuildPopupColumnStyle(bool withLeftDivider)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderColor = withLeftDivider ? new Color(0.26f, 0.26f, 0.26f, 0.95f) : new Color(0f, 0f, 0f, 0f),
            BorderWidthLeft = withLeftDivider ? 1 : 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            ContentMarginLeft = withLeftDivider ? 12 : 6,
            ContentMarginTop = 4,
            ContentMarginRight = 8,
            ContentMarginBottom = 4,
        };
    }

    private void OnInputGuiInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            return;

        if (keyEvent.Keycode == Key.Tab)
        {
            ApplySelectedCompletion();
            GetViewport().SetInputAsHandled();
        }
        else if (keyEvent.Keycode == Key.Right && _input.CaretColumn >= (_input.Text?.Length ?? 0))
        {
            ApplySelectedCompletion();
            GetViewport().SetInputAsHandled();
        }
        else if (keyEvent.Keycode == Key.Up)
        {
            if (HasVisibleCompletions())
                MoveCompletionSelection(-1);
            else
                NavigateHistory(-1);
            GetViewport().SetInputAsHandled();
        }
        else if (keyEvent.Keycode == Key.Down)
        {
            if (HasVisibleCompletions())
                MoveCompletionSelection(1);
            else
                NavigateHistory(1);
            GetViewport().SetInputAsHandled();
        }
    }

    private void NavigateHistory(int direction)
    {
        if (_history.Count == 0 || _input == null)
            return;

        if (_historyIndex < 0)
            _historyIndex = _history.Count;

        _historyIndex = Math.Clamp(_historyIndex + direction, 0, _history.Count);
        _input.Text = _historyIndex >= 0 && _historyIndex < _history.Count
            ? _history[_historyIndex]
            : string.Empty;
        _input.CaretColumn = _input.Text.Length;
        _lastObservedInput = _input.Text;
        RefreshAssistState();
    }

    private async void ExecuteCurrentInput()
    {
        if (_input == null)
            return;

        string raw = _input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return;

        _input.Clear();
        _history.Add(raw);
        _historyIndex = _history.Count;
        AppendCommand(raw);
        ClearCompletionState();

        try
        {
            await ExecuteCommandAsync(raw);
        }
        catch (Exception ex)
        {
            AppendError($"执行失败：{ex.Message}");
        }

        _lastObservedInput = _input.Text ?? string.Empty;
        RefreshAssistState();
        _input.GrabFocus();
    }

    private void MoveCompletionSelection(int direction)
    {
        if (!HasVisibleCompletions())
            return;

        if (_selectedCompletionIndex < 0)
            _selectedCompletionIndex = 0;
        else
            _selectedCompletionIndex =
                (_selectedCompletionIndex + direction + _visibleCompletions.Count)
                % _visibleCompletions.Count;

        RenderCompletionPopup();
    }

    private bool HasVisibleCompletions() => _visibleCompletions.Count > 0;

    private void ApplySelectedCompletion()
    {
        if (!HasVisibleCompletions() || _input == null)
            return;

        int index = _selectedCompletionIndex >= 0 ? _selectedCompletionIndex : 0;
        index = Math.Clamp(index, 0, _visibleCompletions.Count - 1);
        ApplyCompletion(_visibleCompletions[index]);
    }

    private void ApplyCompletion(CompletionItem item)
    {
        if (item == null || _input == null)
            return;

        InputContext context = BuildInputContext(_input.Text ?? string.Empty);
        var tokens = context.Tokens.ToList();
        if (context.EndsWithSpace)
        {
            tokens.Add(item.InsertText);
        }
        else if (tokens.Count == 0)
        {
            tokens.Add(item.InsertText);
        }
        else
        {
            tokens[context.TargetTokenIndex] = item.InsertText;
        }

        string newText = string.Join(" ", tokens);
        if (!newText.EndsWith(' '))
            newText += " ";

        _input.Text = newText;
        _input.CaretColumn = newText.Length;
        _lastObservedInput = newText;
        RefreshAssistState();
    }

    private void RefreshAssistState()
    {
        string raw = _input?.Text ?? string.Empty;
        InputContext context = BuildInputContext(raw);
        _lastInputContext = context;

        RenderAssistHint(BuildAssistHint(context));
        UpdateCompletionItems(context);
        RenderCompletionPopup();
    }

    private void ClearCompletionState()
    {
        _visibleCompletions.Clear();
        _selectedCompletionIndex = -1;
        RenderCompletionPopup();
    }

    private void UpdateCompletionPopupLayout()
    {
        if (_completionPopup == null || _input == null || _root == null)
            return;

        Rect2 inputRect = _input.GetGlobalRect();
        Vector2 inputPositionInRoot = inputRect.Position - _root.GlobalPosition;
        Vector2 rootSize = _root.Size;

        const float horizontalMargin = 12f;
        const float verticalSpacing = 2f;
        const float bottomMargin = 12f;
        const float minPopupWidth = 460f;
        const float maxPopupHeight = 320f;
        const float minPopupHeight = 120f;

        float maxWidth = Math.Max(minPopupWidth, rootSize.X - horizontalMargin * 2f);
        float popupWidth = Mathf.Clamp(inputRect.Size.X + 220f, minPopupWidth, maxWidth);

        float popupX = inputPositionInRoot.X;
        float maxX = Math.Max(horizontalMargin, rootSize.X - popupWidth - horizontalMargin);
        popupX = Mathf.Clamp(popupX, horizontalMargin, maxX);

        float popupY = inputPositionInRoot.Y + inputRect.Size.Y + verticalSpacing;
        float availableHeight = rootSize.Y - popupY - bottomMargin;
        int visibleCount = Math.Max(1, _visibleCompletions.Count);
        float desiredHeight = 44f + visibleCount * 28f;
        float popupHeight = Mathf.Clamp(desiredHeight, minPopupHeight, maxPopupHeight);
        popupHeight = Mathf.Min(popupHeight, availableHeight);
        if (availableHeight < minPopupHeight)
            popupHeight = Mathf.Max(52f, availableHeight);

        _completionPopup.Position = new Vector2(popupX, popupY);
        _completionPopup.Size = new Vector2(popupWidth, popupHeight);
    }

    private static InputContext BuildInputContext(string raw)
    {
        raw ??= string.Empty;
        bool endsWithSpace = raw.Length > 0 && char.IsWhiteSpace(raw[^1]);
        string[] tokens = raw.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return new InputContext(raw, tokens, endsWithSpace);
    }

    private void RenderAssistHint(string bbcode)
    {
        if (_assistHint == null)
            return;

        _assistHint.Text = string.IsNullOrWhiteSpace(bbcode)
            ? $"[color={DimColor}]输入命令，Tab 自动补全，↑↓ 切换候选或历史。[/color]"
            : bbcode;
    }

    private void UpdateCompletionItems(InputContext context)
    {
        _visibleCompletions.Clear();
        _visibleCompletions.AddRange(GetCompletionItems(context).Take(MaxVisibleCompletions));
        if (_visibleCompletions.Count == 0)
        {
            _selectedCompletionIndex = -1;
            return;
        }

        _selectedCompletionIndex = Math.Clamp(_selectedCompletionIndex, 0, _visibleCompletions.Count - 1);
        if (_selectedCompletionIndex < 0)
            _selectedCompletionIndex = 0;
    }

    private void RenderCompletionPopup()
    {
        if (_completionList == null || _completionDetails == null || _completionPopup == null)
            return;

        bool hasInput = !string.IsNullOrWhiteSpace(_input?.Text);
        if (!hasInput || _visibleCompletions.Count == 0)
        {
            _completionPopup.Visible = false;
            _completionList.Text = string.Empty;
            _completionDetails.Text = string.Empty;
            return;
        }

        _completionPopup.Visible = Visible;
        UpdateCompletionPopupLayout();

        var builder = new StringBuilder();
        for (int i = 0; i < _visibleCompletions.Count; i++)
        {
            CompletionItem item = _visibleCompletions[i];
            bool selected = i == _selectedCompletionIndex;
            string detail = BuildCompletionInlineDetail(item);
            if (selected)
            {
                builder.Append(
                    $"[bgcolor={EditorSelectionColor}][color=#ffffff]  {EscapeBbcode(item.InsertText)}[/color]"
                        + $"[color=#9cdcfe]    {EscapeBbcode(detail)}[/color][/bgcolor]"
                );
            }
            else
            {
                builder.Append(
                    $"[color=#d4d4d4]  {EscapeBbcode(item.InsertText)}[/color]"
                        + $"[color=#808080]    {EscapeBbcode(detail)}[/color]"
                );
            }

            if (i < _visibleCompletions.Count - 1)
                builder.Append('\n');
        }

        _completionList.Text = builder.ToString();
        int selectedIndex = _selectedCompletionIndex < 0 ? 0 : _selectedCompletionIndex;
        selectedIndex = Math.Clamp(selectedIndex, 0, _visibleCompletions.Count - 1);
        _completionDetails.Text = BuildCompletionDetails(_visibleCompletions[selectedIndex]);
    }

    private static string BuildCompletionInlineDetail(CompletionItem item)
    {
        if (item == null)
            return string.Empty;

        string detail = item.DisplayText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(detail))
            return string.Empty;

        if (
            !string.IsNullOrWhiteSpace(item.InsertText)
            && detail.StartsWith(item.InsertText, StringComparison.OrdinalIgnoreCase)
        )
        {
            detail = detail[item.InsertText.Length..].TrimStart();
            if (detail.StartsWith("·", StringComparison.Ordinal))
                detail = detail[1..].TrimStart();
        }

        return detail;
    }

    private string BuildCompletionDetails(CompletionItem item)
    {
        if (item == null)
            return string.Empty;

        var builder = new StringBuilder();
        builder.Append($"[color={EditorMutedColor}]Completion[/color]\n");
        builder.Append($"[color=#ffffff]{EscapeBbcode(item.InsertText)}[/color]\n");
        builder.Append($"[color={DimColor}]{EscapeBbcode(BuildCompletionInlineDetail(item))}[/color]");

        if (item.SearchTerms.Length > 0)
        {
            builder.Append("\n\n");
            builder.Append($"[color={EditorKeywordColor}]Matches[/color]\n");
            builder.Append(
                $"[color={EditorMutedColor}]{EscapeBbcode(string.Join(", ", item.SearchTerms.Take(6)))}[/color]"
            );
        }

        if (_lastInputContext.Tokens.Length > 0)
        {
            CommandDefinition definition = ResolveCommandDefinition(_lastInputContext.Tokens[0]);
            if (definition != null)
            {
                builder.Append("\n\n");
                builder.Append($"[color={EditorSymbolColor}]Context[/color]\n");
                builder.Append($"[color={NeutralColor}]{EscapeBbcode(definition.Usage)}[/color]");
            }
        }

        return builder.ToString();
    }

    private void RenderCompletions()
    {
        if (_completionList == null || _completionDetails == null || _completionPopup == null)
            return;

        if (_visibleCompletions.Count == 0)
        {
            _completionList.Text = $"[color={DimColor}]当前没有可补全项。[/color]";
            return;
        }

        var builder = new StringBuilder();
        builder.Append($"[color={AccentColor}]补全候选[/color] [color={DimColor}](Tab/→ 应用，↑↓ 切换)[/color]\n");
        for (int i = 0; i < _visibleCompletions.Count; i++)
        {
            var item = _visibleCompletions[i];
            bool selected = i == _selectedCompletionIndex;
            string prefix = selected ? ">" : " ";
            string color = selected ? SuccessColor : NeutralColor;
            builder.Append($"[color={color}]{prefix} {EscapeBbcode(item.DisplayText)}[/color]");
            if (i < _visibleCompletions.Count - 1)
                builder.Append('\n');
        }

        _completionList.Text = builder.ToString();
    }

    private string BuildAssistHint(InputContext context)
    {
        if (context.Tokens.Length == 0)
        {
            return
                $"[color={AccentColor}]开始输入[/color]\n"
                + $"[color={DimColor}]常用：addskill, addequipment, addrelic, additem, setstat, setresource, battletest[/color]";
        }

        CommandDefinition definition = ResolveCommandDefinition(context.Tokens[0]);
        if (definition == null)
        {
            return
                $"[color={ErrorColor}]未知命令[/color] [color={NeutralColor}]{EscapeBbcode(context.Tokens[0])}[/color]\n"
                + $"[color={DimColor}]尝试输入 help，或直接按 Tab 从可用命令里补全。[/color]";
        }

        var builder = new StringBuilder();
        builder.Append($"[color={AccentColor}]用法[/color] {EscapeBbcode(definition.Usage)}\n");
        builder.Append($"[color={NeutralColor}]{EscapeBbcode(definition.Description)}[/color]");

        int targetArgumentIndex = context.TargetTokenIndex - 1;
        if (targetArgumentIndex >= 0 && targetArgumentIndex < definition.ArgumentHints.Length)
        {
            builder.Append('\n');
            builder.Append(
                $"[color={SuccessColor}]当前参数[/color] {EscapeBbcode(definition.ArgumentHints[targetArgumentIndex])}"
            );
        }
        else if (context.TargetTokenIndex > 0 && definition.ArgumentHints.Length > 0)
        {
            builder.Append('\n');
            builder.Append($"[color={DimColor}]参数已齐，可以直接回车执行。[/color]");
        }

        if (definition.Aliases.Length > 0)
        {
            builder.Append('\n');
            builder.Append(
                $"[color={DimColor}]别名：{EscapeBbcode(string.Join(", ", definition.Aliases))}[/color]"
            );
        }

        return builder.ToString();
    }

    private IEnumerable<CompletionItem> GetCompletionItems(InputContext context)
    {
        string prefix = context.CurrentToken;
        if (context.TargetTokenIndex == 0)
            return FilterCompletionItems(BuildCommandCompletions(), prefix);

        CommandDefinition definition =
            context.Tokens.Length > 0 ? ResolveCommandDefinition(context.Tokens[0]) : null;
        if (definition == null)
            return FilterCompletionItems(BuildCommandCompletions(), prefix);

        string commandName = definition.Name;
        return commandName switch
        {
            "catalog" => BuildCatalogCompletions(context),
            "addskill" => BuildAddSkillCompletions(context),
            "addequipment" => FilterCompletionItems(BuildEquipmentCompletions(), prefix),
            "addrelic" => FilterCompletionItems(BuildRelicCompletions(), prefix),
            "additem" => FilterCompletionItems(BuildItemCompletions(), prefix),
            "setstat" => BuildStatCompletions(context),
            "addstat" => BuildStatCompletions(context),
            "setresource" => BuildResourceCompletions(context),
            "battletest" => BuildToggleCompletions(context),
            _ => Enumerable.Empty<CompletionItem>(),
        };
    }

    private static IEnumerable<CompletionItem> BuildCommandCompletions()
    {
        return CommandDefinitions.Select(definition =>
            new CompletionItem(
                definition.Name,
                $"{definition.Name}  ·  {definition.Description}",
                new[] { definition.Name }.Concat(definition.Aliases).ToArray()
            )
        );
    }

    private IEnumerable<CompletionItem> BuildCatalogCompletions(InputContext context)
    {
        string prefix = context.CurrentToken;
        if (context.TargetTokenIndex == 1)
        {
            return FilterCompletionItems(
                [
                    new CompletionItem("skill", "skill  ·  查看角色技能池", "skill", "技能"),
                    new CompletionItem("equipment", "equipment  ·  查看装备列表", "equipment", "装备"),
                    new CompletionItem("relic", "relic  ·  查看遗物列表", "relic", "遗物"),
                    new CompletionItem("item", "item  ·  查看道具列表", "item", "道具", "药水"),
                ],
                prefix
            );
        }

        if (
            context.TargetTokenIndex == 2
            && context.Tokens.Length >= 2
            && Matches(context.Tokens[1], "skill", "skills", "技能")
        )
        {
            return FilterCompletionItems(BuildPlayerCompletions(includeIndices: true), prefix);
        }

        return Enumerable.Empty<CompletionItem>();
    }

    private IEnumerable<CompletionItem> BuildAddSkillCompletions(InputContext context)
    {
        string prefix = context.CurrentToken;
        if (context.TargetTokenIndex == 1)
            return FilterCompletionItems(BuildPlayerCompletions(includeIndices: true), prefix);
        if (context.TargetTokenIndex == 2)
            return FilterCompletionItems(BuildSkillCompletionsForContext(context, playerTokenIndex: 1), prefix);
        return Enumerable.Empty<CompletionItem>();
    }

    private IEnumerable<CompletionItem> BuildStatCompletions(InputContext context)
    {
        string prefix = context.CurrentToken;
        if (context.TargetTokenIndex == 1)
            return FilterCompletionItems(BuildPlayerCompletions(includeIndices: true), prefix);
        if (context.TargetTokenIndex == 2)
            return FilterCompletionItems(BuildPropertyCompletions(), prefix);
        if (context.TargetTokenIndex == 3)
            return FilterCompletionItems(BuildNumberCompletions(), prefix);
        return Enumerable.Empty<CompletionItem>();
    }

    private IEnumerable<CompletionItem> BuildResourceCompletions(InputContext context)
    {
        string prefix = context.CurrentToken;
        if (context.TargetTokenIndex == 1)
        {
            return FilterCompletionItems(
                [
                    new CompletionItem("coin", "coin  ·  电币", "coin", "coins", "电币"),
                    new CompletionItem("energy", "energy  ·  当前能量", "energy", "能量"),
                    new CompletionItem("maxenergy", "maxenergy  ·  最大能量", "maxenergy", "energymax", "最大能量"),
                ],
                prefix
            );
        }

        if (context.TargetTokenIndex == 2)
            return FilterCompletionItems(BuildNumberCompletions(), prefix);

        return Enumerable.Empty<CompletionItem>();
    }

    private static IEnumerable<CompletionItem> BuildToggleCompletions(InputContext context)
    {
        if (context.TargetTokenIndex != 1)
            return Enumerable.Empty<CompletionItem>();

        return FilterCompletionItems(
            [
                new CompletionItem("on", "on  ·  开启", "on", "true", "1", "开"),
                new CompletionItem("off", "off  ·  关闭", "off", "false", "0", "关"),
                new CompletionItem("toggle", "toggle  ·  切换", "toggle", "切换"),
            ],
            context.CurrentToken
        );
    }


    private IEnumerable<CompletionItem> BuildSkillCompletionsForContext(InputContext context, int playerTokenIndex)
    {
        if (context.Tokens.Length <= playerTokenIndex)
            return BuildAllSkillCompletions();

        if (!TryResolvePlayerSilently(context.Tokens[playerTokenIndex], out int playerIndex))
            return BuildAllSkillCompletions();

        var info = GameInfo.PlayerCharacters[playerIndex];
        SkillID[] pool = GetConsoleSkillPool(info);
        return pool.Select(skillId =>
            new CompletionItem(
                skillId.ToString(),
                $"{skillId}  ·  {GetSkillDisplayName(skillId)}",
                skillId.ToString(),
                GetSkillDisplayName(skillId)
            )
        );
    }

    private static IEnumerable<CompletionItem> BuildAllSkillCompletions()
    {
        return Enum.GetValues<SkillID>().Select(skillId =>
            new CompletionItem(
                skillId.ToString(),
                $"{skillId}  ·  {GetSkillDisplayName(skillId)}",
                skillId.ToString(),
                GetSkillDisplayName(skillId)
            )
        );
    }

    private static IEnumerable<CompletionItem> BuildPlayerCompletions(bool includeIndices)
    {
        if (GameInfo.PlayerCharacters == null)
            return Enumerable.Empty<CompletionItem>();

        var results = new List<CompletionItem>();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            results.Add(
                new CompletionItem(
                    info.CharacterName,
                    $"{info.CharacterName}  ·  角色 {i + 1}",
                    info.CharacterName,
                    (i + 1).ToString()
                )
            );
            if (includeIndices)
            {
                results.Add(
                    new CompletionItem(
                        (i + 1).ToString(),
                        $"{i + 1}  ·  {info.CharacterName}",
                        (i + 1).ToString(),
                        info.CharacterName
                    )
                );
            }
        }

        return results;
    }

    private static IEnumerable<CompletionItem> BuildEquipmentCompletions()
    {
        return Enum.GetValues<Equipment.EquipmentName>().Select(name =>
        {
            Equipment equipment = Equipment.Create(name);
            return new CompletionItem(
                name.ToString(),
                $"{name}  ·  {equipment?.DisplayName}",
                name.ToString(),
                equipment?.DisplayName ?? string.Empty
            );
        });
    }

    private static IEnumerable<CompletionItem> BuildRelicCompletions()
    {
        return Enum.GetValues<RelicID>().Select(id =>
        {
            Relic relic = Relic.Create(id);
            return new CompletionItem(
                id.ToString(),
                $"{id}  ·  {relic.RelicName}",
                id.ToString(),
                relic.RelicName
            );
        });
    }

    private static IEnumerable<CompletionItem> BuildItemCompletions()
    {
        return Enum.GetValues<ItemID>()
            .Where(id => id != ItemID.None)
            .Select(id =>
                new CompletionItem(
                    id.ToString(),
                    $"{id}  ·  {ConsumeItem.GetItemName(id)}",
                    id.ToString(),
                    ConsumeItem.GetItemName(id)
                )
            );
    }

    private static IEnumerable<CompletionItem> BuildPropertyCompletions()
    {
        return
        [
            new CompletionItem("power", "power  ·  力量", "power", "力量"),
            new CompletionItem("survivability", "survivability  ·  生存", "survivability", "survive", "生存"),
            new CompletionItem("speed", "speed  ·  速度", "speed", "速度"),
            new CompletionItem("maxlife", "maxlife  ·  生命上限", "maxlife", "maxhp", "生命", "生命上限"),
        ];
    }

    private static IEnumerable<CompletionItem> BuildNumberCompletions()
    {
        return
        [
            new CompletionItem("1", "1", "1"),
            new CompletionItem("2", "2", "2"),
            new CompletionItem("3", "3", "3"),
            new CompletionItem("5", "5", "5"),
            new CompletionItem("10", "10", "10"),
            new CompletionItem("20", "20", "20"),
            new CompletionItem("50", "50", "50"),
            new CompletionItem("100", "100", "100"),
            new CompletionItem("999", "999", "999"),
        ];
    }

    private static IEnumerable<CompletionItem> FilterCompletionItems(
        IEnumerable<CompletionItem> source,
        string prefix
    )
    {
        prefix ??= string.Empty;
        string trimmedPrefix = prefix.Trim();
        return source
            .Where(item => GetCompletionMatchScore(item, trimmedPrefix) >= 0)
            .OrderByDescending(item => GetCompletionMatchScore(item, trimmedPrefix))
            .ThenBy(item => item.InsertText.Length)
            .ThenBy(item => item.InsertText, StringComparer.OrdinalIgnoreCase)
            .DistinctBy(item => item.InsertText);
    }

    private static int GetCompletionMatchScore(CompletionItem item, string prefix)
    {
        if (item == null)
            return -1;
        if (string.IsNullOrWhiteSpace(prefix))
            return 1;

        string normalized = prefix.Trim();
        foreach (string term in item.SearchTerms.Prepend(item.InsertText).Prepend(item.DisplayText))
        {
            if (string.IsNullOrWhiteSpace(term))
                continue;
            if (string.Equals(term, normalized, StringComparison.OrdinalIgnoreCase))
                return 100;
            if (term.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                return 80;
            if (term.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                return 40;
        }

        return -1;
    }

    private static CommandDefinition ResolveCommandDefinition(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return CommandDefinitions.FirstOrDefault(definition =>
            string.Equals(definition.Name, token, StringComparison.OrdinalIgnoreCase)
            || definition.Aliases.Any(alias =>
                string.Equals(alias, token, StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    private async Task ExecuteCommandAsync(string raw)
    {
        string[] args = raw.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (args.Length == 0)
            return;

        string command = args[0];
        if (Matches(command, "help", "帮助"))
        {
            _help.Text = BuildHelpText();
            AppendInfo("帮助已更新。");
            return;
        }

        if (Matches(command, "clear", "清屏"))
        {
            _log.Text = string.Empty;
            return;
        }

        if (Matches(command, "players", "角色"))
        {
            AppendInfo(BuildPlayersText());
            return;
        }

        if (Matches(command, "catalog", "列表"))
        {
            ExecuteCatalogCommand(args);
            return;
        }

        if (Matches(command, "addskill", "加技能"))
        {
            await ExecuteAddSkillAsync(args);
            return;
        }

        if (Matches(command, "addequipment", "加装备"))
        {
            ExecuteAddEquipment(args);
            return;
        }

        if (Matches(command, "addrelic", "加遗物"))
        {
            ExecuteAddRelic(args);
            return;
        }

        if (Matches(command, "additem", "加道具", "加药水"))
        {
            ExecuteAddItem(args);
            return;
        }

        if (Matches(command, "setstat", "设属性"))
        {
            await ExecuteSetStatAsync(args, absolute: true);
            return;
        }

        if (Matches(command, "addstat", "加属性"))
        {
            await ExecuteSetStatAsync(args, absolute: false);
            return;
        }

        if (Matches(command, "setresource", "设资源"))
        {
            ExecuteSetResource(args);
            return;
        }

        if (Matches(command, "battletest", "战斗测试", "bt"))
        {
            ExecuteBattleTest(args);
            return;
        }

        if (Matches(command, "save", "保存"))
        {
            SaveSystem.SaveAll();
            AppendSuccess("已保存当前 GameInfo。");
            return;
        }

        AppendError($"未知命令：{command}");
    }

    private void ExecuteCatalogCommand(string[] args)
    {
        if (args.Length < 2)
        {
            AppendError("用法：catalog skill <角色> | catalog equipment | catalog relic | catalog item");
            return;
        }

        string category = args[1];
        if (Matches(category, "skill", "skills", "技能"))
        {
            if (args.Length < 3)
            {
                AppendError("用法：catalog skill <角色>");
                return;
            }

            if (!TryResolvePlayer(args[2], out int playerIndex))
                return;

            var info = GameInfo.PlayerCharacters[playerIndex];
            SkillID[] pool = GetConsoleSkillPool(info);
            string lines = string.Join(
                "\n",
                pool.Select(skillId => $"{skillId}  ->  {GetSkillDisplayName(skillId)}")
            );
            AppendInfo($"{info.CharacterName} 技能池：\n{lines}");
            return;
        }

        if (Matches(category, "equipment", "装备"))
        {
            string lines = string.Join(
                "\n",
                Enum.GetValues<Equipment.EquipmentName>()
                    .Select(name =>
                    {
                        var equipment = Equipment.Create(name);
                        return $"{name}  ->  {equipment?.DisplayName}";
                    })
            );
            AppendInfo($"装备列表：\n{lines}");
            return;
        }

        if (Matches(category, "relic", "遗物"))
        {
            string lines = string.Join(
                "\n",
                Enum.GetValues<RelicID>().Select(id => $"{id}  ->  {Relic.Create(id).RelicName}")
            );
            AppendInfo($"遗物列表：\n{lines}");
            return;
        }

        if (Matches(category, "item", "items", "道具", "药水"))
        {
            string lines = string.Join(
                "\n",
                Enum.GetValues<ItemID>()
                    .Where(id => id != ItemID.None)
                    .Select(id => $"{id}  ->  {ConsumeItem.GetItemName(id)}")
            );
            AppendInfo($"道具列表：\n{lines}");
            return;
        }

        AppendError($"未知列表类型：{category}");
    }

    private async Task ExecuteAddSkillAsync(string[] args)
    {
        if (args.Length < 3)
        {
            AppendError("用法：addskill <角色> <技能ID/技能名>");
            return;
        }

        if (!TryResolvePlayer(args[1], out int playerIndex))
            return;
        if (!TryResolveSkillId(args[2], out SkillID skillId))
            return;

        var info = GameInfo.PlayerCharacters[playerIndex];
        if (!CanPlayerUseConsoleSkill(info, skillId))
        {
            AppendError($"{info.CharacterName} 的技能池或初始牌不包含 {skillId}。");
            return;
        }

        info.GainedSkills ??= new List<SkillID>();
        if (IsStarterSkillId(skillId) || !info.GainedSkills.Contains(skillId))
            info.GainedSkills.Add(skillId);

        GameInfo.PlayerCharacters[playerIndex] = info;
        GameInfo.NormalizePlayerCharacters();
        RefreshOpenUi();
        SaveSystem.SaveAll();
        await Task.CompletedTask;

        AppendSuccess($"已为 {info.CharacterName} 添加技能 {GetSkillDisplayName(skillId)}。");
    }

    private static SkillID[] GetConsoleSkillPool(PlayerInfoStructure info)
    {
        return (info.AllSkills ?? Array.Empty<SkillID>())
            .Concat(StarterSkillIds)
            .Distinct()
            .ToArray();
    }

    private static bool CanPlayerUseConsoleSkill(PlayerInfoStructure info, SkillID skillId)
    {
        return IsStarterSkillId(skillId) || (info.AllSkills ?? Array.Empty<SkillID>()).Contains(skillId);
    }

    private static bool IsStarterSkillId(SkillID skillId) => StarterSkillIds.Contains(skillId);

    private void ExecuteAddEquipment(string[] args)
    {
        if (args.Length < 2)
        {
            AppendError("用法：addequipment <装备ID/装备名> [数量]");
            return;
        }

        if (!TryResolveEquipmentName(args[1], out var equipmentName))
            return;

        int count = 1;
        if (args.Length >= 3 && !TryParsePositiveInt(args[2], out count))
            return;

        AppendError("Equipment system has been removed.");
        return;
    }

    private void ExecuteAddRelic(string[] args)
    {
        if (args.Length < 2)
        {
            AppendError("用法：addrelic <遗物ID/遗物名> [数量]");
            return;
        }

        if (!TryResolveRelicId(args[1], out var relicId))
            return;

        int count = 1;
        if (args.Length >= 3 && !TryParsePositiveInt(args[2], out count))
            return;

        GameInfo.Relics ??= new Dictionary<RelicID, int>();

        if (relicId == RelicID.Blessing)
        {
            int existing = GameInfo.Relics.GetValueOrDefault(relicId, 0);
            GameInfo.Relics[relicId] = Math.Max(0, existing + count);
        }
        else if (!GameInfo.Relics.ContainsKey(relicId))
        {
            GameInfo.Relics[relicId] = Relic.GetAcquireAmount(relicId);
        }

        RefreshOpenUi();
        SaveSystem.SaveAll();
        AppendSuccess($"已添加遗物 {Relic.Create(relicId).RelicName}。");
    }

    private void ExecuteAddItem(string[] args)
    {
        if (args.Length < 2)
        {
            AppendError("用法：additem <道具ID/道具名> [数量]");
            return;
        }

        if (!TryResolveItemId(args[1], out var itemId))
            return;

        int count = 1;
        if (args.Length >= 3 && !TryParsePositiveInt(args[2], out count))
            return;

        if (ResourceState == null)
        {
            AppendError("当前没有可用的 PlayerResourceState。");
            return;
        }

        int before = GameInfo.Items?.Count ?? 0;
        for (int i = 0; i < count; i++)
            ConsumeItem.AddItem(ResourceState, itemId);
        int after = GameInfo.Items?.Count ?? 0;
        int added = Math.Max(0, after - before);

        RefreshOpenUi();
        SaveSystem.SaveAll();
        AppendSuccess(
            added > 0
                ? $"已添加道具 {ConsumeItem.GetItemName(itemId)} x{added}。"
                : "道具栏已满，未能添加新道具。"
        );
    }

    private async Task ExecuteSetStatAsync(string[] args, bool absolute)
    {
        if (args.Length < 4)
        {
            AppendError(
                absolute
                    ? "用法：setstat <角色> <power|survivability|speed|maxlife> <值>"
                    : "用法：addstat <角色> <power|survivability|speed|maxlife> <增量>"
            );
            return;
        }

        if (!TryResolvePlayer(args[1], out int playerIndex))
            return;
        if (!TryResolvePropertyType(args[2], out var propertyType))
            return;
        if (!int.TryParse(args[3], out int value))
        {
            AppendError($"不是有效数字：{args[3]}");
            return;
        }

        var info = GameInfo.PlayerCharacters[playerIndex];
        switch (propertyType)
        {
            case PropertyType.Power:
                info.Power = absolute ? value : info.Power + value;
                break;
            case PropertyType.Survivability:
                info.Survivability = absolute ? value : info.Survivability + value;
                break;
            case PropertyType.Speed:
                info.Speed = absolute ? value : info.Speed + value;
                break;
            case PropertyType.MaxLife:
                info.LifeMax = absolute ? value : info.LifeMax + value;
                break;
        }

        info.Power = Math.Clamp(info.Power, 0, 999);
        info.Survivability = Math.Clamp(info.Survivability, 0, 999);
        info.Speed = Math.Clamp(info.Speed, 0, 999);
        info.LifeMax = Math.Clamp(info.LifeMax, 1, 9999);
        GameInfo.PlayerCharacters[playerIndex] = info;
        GameInfo.NormalizePlayerCharacters();
        await SyncBattleStatsAsync(playerIndex, propertyType);
        RefreshOpenUi();
        SaveSystem.SaveAll();

        AppendSuccess(
            $"已将 {info.CharacterName} 的 {GetPropertyName(propertyType)}{(absolute ? "设为" : "调整为")} {GetBasePropertyValue(GameInfo.PlayerCharacters[playerIndex], propertyType)}。"
        );
    }

    private void ExecuteSetResource(string[] args)
    {
        if (args.Length < 3)
        {
            AppendError("Usage: setresource <coin|energy|core|maxenergy|maxcore> <value>");
            return;
        }

        if (!int.TryParse(args[2], out int value))
        {
            AppendError($"不是有效数字：{args[2]}");
            return;
        }

        string target = args[1];
        if (Matches(target, "coin", "coins", "电币"))
        {
            GameInfo.ElectricityCoin = Math.Max(0, value);
        }
        else if (Matches(target, "energy", "core", "coreenergy", "能量", "核心能源"))
        {
            GameInfo.TransitionEnergy = Math.Clamp(value, 0, GameInfo.TransitionEnergyMax);
        }
        else if (Matches(target, "maxenergy", "energymax", "maxcore", "coremax", "最大能量", "核心能源上限"))
        {
            GameInfo.TransitionEnergyMax = Math.Max(0, value);
            GameInfo.TransitionEnergy = Math.Clamp(
                GameInfo.TransitionEnergy,
                0,
                GameInfo.TransitionEnergyMax
            );
        }
        else
        {
            AppendError($"未知资源类型：{target}");
            return;
        }

        RefreshOpenUi();
        SaveSystem.SaveAll();
        AppendSuccess($"已更新资源 {target}。");
    }

    private void ExecuteBattleTest(string[] args)
    {
        if (args.Length < 2)
        {
            AppendInfo($"Battle.Istest 当前为 {(Battle.Istest ? "ON" : "OFF")}。用法：battletest <on|off|toggle>");
            return;
        }

        string value = args[1];
        if (Matches(value, "toggle", "切换"))
        {
            Battle.Istest = !Battle.Istest;
        }
        else if (TryParseToggleValue(value, out bool enabled))
        {
            Battle.Istest = enabled;
        }
        else
        {
            AppendError($"未知开关值：{value}。可用：on/off/toggle");
            return;
        }

        AppendSuccess($"Battle.Istest 已{(Battle.Istest ? "开启" : "关闭")}。");
    }


    private async Task SyncBattleStatsAsync(int playerIndex, PropertyType propertyType)
    {
        var battlePlayer = FindBattle()
            ?.PlayersList?.FirstOrDefault(player => player.CharacterIndex == playerIndex);
        if (battlePlayer == null)
            return;

        var info = GameInfo.PlayerCharacters[playerIndex];
        int expected = GetExpectedBattleStat(battlePlayer, info, propertyType);
        int current = GetCurrentBattleStat(battlePlayer, propertyType);
        int delta = expected - current;
        if (delta == 0)
            return;

        if (delta > 0)
            await battlePlayer.IncreaseProperties(propertyType, delta);
        else
            await battlePlayer.DescendingProperties(propertyType, -delta);
    }

    private void RefreshOpenUi()
    {
        ResourceState?.RefreshDebugView();
        GetNodeOrNull<BattlePreview>("/root/Map/SiteUI/BattlePreview")?.SetPortraitPostion();
        GetNodeOrNull<BattleReady>("/root/Map/BattleReadyLayer/BattleReady")
            ?.RefreshFromExternalChange();
    }

    private Battle FindBattle()
    {
        var root = GetTree()?.Root;
        return root == null ? null : FindBattleRecursive(root);
    }

    private static Battle FindBattleRecursive(Node node)
    {
        if (node is Battle battle)
            return battle;

        foreach (Node child in node.GetChildren())
        {
            Battle result = FindBattleRecursive(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private int GetExpectedBattleStat(
        PlayerCharacter player,
        PlayerInfoStructure info,
        PropertyType propertyType
    )
    {
        return propertyType switch
        {
            PropertyType.Power => info.Power,
            PropertyType.Survivability => info.Survivability,
            PropertyType.Speed => info.Speed,
            PropertyType.MaxLife => info.LifeMax,
            _ => 0,
        };
    }

    private static int GetCurrentBattleStat(PlayerCharacter player, PropertyType propertyType)
    {
        return propertyType switch
        {
            PropertyType.Power => player.BattlePower,
            PropertyType.Survivability => player.BattleSurvivability,
            PropertyType.Speed => player.Speed,
            PropertyType.MaxLife => player.BattleMaxLife,
            _ => 0,
        };
    }

    private static int GetBasePropertyValue(PlayerInfoStructure info, PropertyType propertyType)
    {
        return propertyType switch
        {
            PropertyType.Power => info.Power,
            PropertyType.Survivability => info.Survivability,
            PropertyType.Speed => info.Speed,
            PropertyType.MaxLife => info.LifeMax,
            _ => 0,
        };
    }

    private bool TryResolvePlayer(string token, out int playerIndex)
    {
        playerIndex = -1;
        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
        {
            AppendError("当前没有玩家角色数据。");
            return false;
        }

        if (int.TryParse(token, out int numericIndex))
        {
            numericIndex -= 1;
            if (numericIndex >= 0 && numericIndex < players.Length)
            {
                playerIndex = numericIndex;
                return true;
            }
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (string.Equals(players[i].CharacterName, token, StringComparison.OrdinalIgnoreCase))
            {
                playerIndex = i;
                return true;
            }
        }

        AppendError($"找不到角色：{token}");
        return false;
    }

    private static bool TryResolvePlayerSilently(string token, out int playerIndex)
    {
        playerIndex = -1;
        var players = GameInfo.PlayerCharacters;
        if (players == null || players.Length == 0)
            return false;

        if (int.TryParse(token, out int numericIndex))
        {
            numericIndex -= 1;
            if (numericIndex >= 0 && numericIndex < players.Length)
            {
                playerIndex = numericIndex;
                return true;
            }
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (string.Equals(players[i].CharacterName, token, StringComparison.OrdinalIgnoreCase))
            {
                playerIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveSkillId(string token, out SkillID skillId)
    {
        if (Enum.TryParse(token, true, out skillId))
            return true;

        foreach (SkillID candidate in Enum.GetValues<SkillID>())
        {
            if (
                string.Equals(
                    GetSkillDisplayName(candidate),
                    token,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                skillId = candidate;
                return true;
            }
        }

        AppendError($"找不到技能：{token}");
        skillId = default;
        return false;
    }

    private bool TryResolveEquipmentName(string token, out Equipment.EquipmentName equipmentName)
    {
        if (Enum.TryParse(token, true, out equipmentName))
            return true;

        foreach (Equipment.EquipmentName candidate in Enum.GetValues<Equipment.EquipmentName>())
        {
            if (
                string.Equals(
                    Equipment.Create(candidate)?.DisplayName,
                    token,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                equipmentName = candidate;
                return true;
            }
        }

        AppendError($"找不到装备：{token}");
        equipmentName = default;
        return false;
    }

    private bool TryResolveRelicId(string token, out RelicID relicId)
    {
        if (Enum.TryParse(token, true, out relicId))
            return true;

        foreach (RelicID candidate in Enum.GetValues<RelicID>())
        {
            if (
                string.Equals(
                    Relic.Create(candidate).RelicName,
                    token,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                relicId = candidate;
                return true;
            }
        }

        AppendError($"找不到遗物：{token}");
        relicId = default;
        return false;
    }

    private bool TryResolveItemId(string token, out ItemID itemId)
    {
        if (Enum.TryParse(token, true, out itemId) && itemId != ItemID.None)
            return true;

        foreach (ItemID candidate in Enum.GetValues<ItemID>())
        {
            if (candidate == ItemID.None)
                continue;
            if (
                string.Equals(
                    ConsumeItem.GetItemName(candidate),
                    token,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                itemId = candidate;
                return true;
            }
        }

        AppendError($"找不到道具：{token}");
        itemId = default;
        return false;
    }

    private bool TryResolvePropertyType(string token, out PropertyType propertyType)
    {
        if (Matches(token, "power", "力量"))
        {
            propertyType = PropertyType.Power;
            return true;
        }

        if (Matches(token, "survivability", "survive", "生存"))
        {
            propertyType = PropertyType.Survivability;
            return true;
        }

        if (Matches(token, "speed", "速度"))
        {
            propertyType = PropertyType.Speed;
            return true;
        }

        if (Matches(token, "maxlife", "maxhp", "生命", "生命上限"))
        {
            propertyType = PropertyType.MaxLife;
            return true;
        }

        AppendError($"不支持的属性类型：{token}");
        propertyType = default;
        return false;
    }

    private bool TryParsePositiveInt(string token, out int value)
    {
        if (!int.TryParse(token, out value) || value <= 0)
        {
            AppendError($"不是有效正整数：{token}");
            return false;
        }

        return true;
    }

    private static string GetSkillDisplayName(SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        return skill?.SkillName ?? skillId.ToString();
    }

    private static string GetPropertyName(PropertyType propertyType)
    {
        return propertyType switch
        {
            PropertyType.Power => "力量",
            PropertyType.Survivability => "生存",
            PropertyType.Speed => "速度",
            PropertyType.MaxLife => "生命上限",
            _ => propertyType.ToString(),
        };
    }

    private string BuildPlayersText()
    {
        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return "当前没有角色。";

        var builder = new StringBuilder();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            builder.AppendLine(
                $"{i + 1}. {info.CharacterName}  力量 {info.Power}  生存 {info.Survivability}  速度 {info.Speed}  生命上限 {info.LifeMax}"
            );
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildHelpText()
    {
        return
            $"[color={AccentColor}]常用命令[/color]\n"
            + $"[color={DimColor}]Tab/→ 应用补全，↑↓ 切换候选或历史，Enter 执行。[/color]\n"
            + "help / 帮助\n"
            + "players / 角色\n"
            + "catalog skill Nightingale\n"
            + "catalog equipment\n"
            + "catalog relic\n"
            + "catalog item\n\n"
            + $"[color={AccentColor}]修改内容[/color]\n"
            + "addskill <角色> <技能ID/技能名>\n"
            + "addequipment <装备ID/装备名> [数量]\n"
            + "addrelic <遗物ID/遗物名> [数量]\n"
            + "additem <道具ID/道具名> [数量]\n"
            + "setstat <角色> <power|survivability|speed|maxlife> <值>\n"
            + "addstat <角色> <power|survivability|speed|maxlife> <增量>\n"
            + "setresource <coin|energy|maxenergy> <值>\n"
            + "battletest <on|off|toggle>\n"
            + "save\n\n"
            + $"[color={AccentColor}]示例[/color]\n"
            + "addskill Nightingale BreakStrike\n"
            + "addequipment 裂隙短刃 2\n"
            + "addrelic Blessing 5\n"
            + "additem Fury 1\n"
            + "setstat Kasiya power 20\n"
            + "addstat Mariya speed 3\n"
            + "setresource coin 999\n\n"
            + "battletest toggle\n\n"
            + "角色名支持英文名，也支持用 1-4 指代当前队伍顺序。";
    }

    private void AppendCommand(string text)
    {
        AppendLogLine($"[color={AccentColor}]> {EscapeBbcode(text)}[/color]");
    }

    private void AppendInfo(string text)
    {
        AppendLogLine($"[color={NeutralColor}]{EscapeBbcode(text)}[/color]");
    }

    private void AppendSuccess(string text)
    {
        AppendLogLine($"[color={SuccessColor}]{EscapeBbcode(text)}[/color]");
    }

    private void AppendError(string text)
    {
        AppendLogLine($"[color={ErrorColor}]{EscapeBbcode(text)}[/color]");
    }

    private void AppendLogLine(string bbcode)
    {
        if (_log == null)
            return;

        if (!string.IsNullOrEmpty(_log.Text))
            _log.AppendText("\n");
        _log.AppendText(bbcode);
    }

    private static string EscapeBbcode(string text)
    {
        return text?.Replace("[", "[lb]").Replace("]", "[rb]") ?? string.Empty;
    }

    private static bool Matches(string token, params string[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (string.Equals(token, options[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryParseToggleValue(string token, out bool enabled)
    {
        if (Matches(token, "on", "true", "1", "yes", "开", "开启"))
        {
            enabled = true;
            return true;
        }

        if (Matches(token, "off", "false", "0", "no", "关", "关闭"))
        {
            enabled = false;
            return true;
        }

        enabled = false;
        return false;
    }
}
