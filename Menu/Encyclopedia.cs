using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

public partial class Encyclopedia : Control
{
    private const float SkillCardSwitchExitStagger = 0.01f;
    private const float SkillCardSwitchExitDuration = 0.22f;
    private const float SkillCardSwitchMaxExitTotalDuration = 0.32f;
    private const float SkillCardSwitchEnterStagger = 0.024f;
    private const float SkillCardSwitchEnterDuration = 0.32f;
    private const float SkillCardSwitchMaxTotalDuration = 0.5f;
    private const float InterfaceEnterDuration = 0.34f;
    private const float InterfaceExitDuration = 0.22f;
    private const float InterfaceEnterStagger = 0.035f;
    private const float InterfaceExitStagger = 0.018f;
    private const int SkillCardsBuildPerFrame = 3;
    private const int SkillCardsAnimatedOnSwitch = 12;
    private static readonly Vector2 EncyclopediaSkillCardDisplaySize = new(240f, 370f);
    private static readonly Vector2 EncyclopediaSkillCardHoverPadding = new(12f, 14f);
    private static readonly Vector2 EncyclopediaRelicButtonSize = new(230f, 74f);
    private static readonly Vector2 EncyclopediaHomeButtonSize = new(360f, 164f);
    private const float EncyclopediaRelicDetailWidth = 460f;

    private static readonly Dictionary<EncyclopediaModule, string> ModuleIconPaths = new()
    {
        [EncyclopediaModule.Home] =
            "res://asset/third_party/kenney_board_game_icons/Vector/Icons/book_open.svg",
        [EncyclopediaModule.Skills] =
            "res://asset/third_party/kenney_board_game_icons/Vector/Icons/cards_collection.svg",
        [EncyclopediaModule.Relics] =
            "res://asset/third_party/kenney_board_game_icons/Vector/Icons/crown_b.svg",
        [EncyclopediaModule.Buffs] =
            "res://asset/third_party/kenney_board_game_icons/Vector/Icons/shield.svg",
    };

    private readonly struct AssemblyItem
    {
        public AssemblyItem(Control control, Vector2 offset, float delay)
        {
            Control = control;
            Offset = offset;
            Delay = delay;
        }

        public Control Control { get; }
        public Vector2 Offset { get; }
        public float Delay { get; }
    }

    private enum EncyclopediaModule
    {
        Home,
        Buffs,
        Skills,
        Relics,
        Items,
        Enemies,
    }

    private enum SkillTypeFilter
    {
        All,
        Attack,
        Survive,
        Special,
        Ability,
        Status,
    }

    private enum SkillCostFilter
    {
        All,
        Zero,
        One,
        Two,
        ThreePlus,
        X,
    }

    private enum SkillSortMode
    {
        Type,
        Name,
        Cost,
        Rarity,
    }

    private sealed class EncyclopediaEntry
    {
        public string Title { get; init; }
        public string Subtitle { get; init; }
        public string Group { get; init; }
        public string Detail { get; init; }
        public string SearchText { get; init; }
        public PlayerCharacterKey? CharacterKey { get; init; }
        public SkillID? SkillId { get; init; }
        public RelicID? RelicId { get; init; }
        public Buff.BuffName? BuffName { get; init; }
        public SkillTypeFilter SkillTypeFilter { get; init; }
        public Skill.SkillRarity SkillRarity { get; init; }
        public SkillCostFilter SkillCostFilter { get; init; }
        public int SkillCostSortValue { get; init; }
        public bool IsStatusCard { get; init; }
    }

    private static readonly Dictionary<EncyclopediaModule, string> ModuleNames = new()
    {
        [EncyclopediaModule.Home] = I18n.Tr("ui.encyclopedia.title", "百科"),
        [EncyclopediaModule.Buffs] = I18n.Tr("ui.encyclopedia.module.buffs", "Buff图鉴"),
        [EncyclopediaModule.Skills] = I18n.Tr("ui.encyclopedia.module.skills", "卡牌图鉴"),
        [EncyclopediaModule.Relics] = I18n.Tr("ui.encyclopedia.module.relics", "遗物图鉴"),
        [EncyclopediaModule.Items] = I18n.Tr("ui.encyclopedia.module.items", "道具"),
        [EncyclopediaModule.Enemies] = I18n.Tr("ui.encyclopedia.module.enemies", "敌人"),
    };

    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );

    private readonly Dictionary<EncyclopediaModule, List<EncyclopediaEntry>> _entries = new();
    private readonly Dictionary<EncyclopediaModule, Button> _moduleButtons = new();
    private readonly Dictionary<Button, EncyclopediaEntry> _buttonEntries = new();
    private readonly Dictionary<PlayerCharacterKey, Button> _characterFilterButtons = new();
    private readonly Dictionary<SkillTypeFilter, Button> _skillTypeFilterButtons = new();
    private readonly Dictionary<Skill.SkillRarity, Button> _skillRarityFilterButtons = new();
    private readonly Dictionary<SkillCostFilter, Button> _skillCostFilterButtons = new();
    private readonly Dictionary<EncyclopediaEntry, Control> _skillCardFrames = new();
    private readonly Dictionary<SkillCard, Tween> _skillCardHoverTweens = new();
    private readonly Dictionary<SkillID, Skill> _previewSkillCache = new();
    private readonly Random _skillAnimationRandom = new();

    private ColorRect _backdrop;
    private PanelContainer _centerPanel;
    private HBoxContainer _header;
    private PanelContainer _modulePanel;
    private VBoxContainer _contentRoot;
    private HBoxContainer _searchRow;
    private LineEdit _searchBox;
    private Label _titleLabel;
    private Label _moduleTitle;
    private Label _countLabel;
    private HBoxContainer _characterFilterRow;
    private RichTextLabel _detailLabel;
    private PanelContainer _detailPanel;
    private PanelContainer _skillGridPanel;
    private GridContainer _skillGrid;
    private VBoxContainer _skillFilterPanel;
    private VBoxContainer _moduleNavigationPanel;
    private SettingsDropdown _skillSortOption;
    private Button _allSkillRarityFilterButton;
    private CenterContainer _detailCardHost;
    private SkillCard _detailSkillCard;

    private EncyclopediaModule _currentModule = EncyclopediaModule.Home;
    private PlayerCharacterKey _selectedSkillCharacter = PlayerCharacterKey.Echo;
    private SkillTypeFilter _selectedSkillTypeFilter = SkillTypeFilter.All;
    private Skill.SkillRarity? _selectedSkillRarityFilter;
    private SkillCostFilter _selectedSkillCostFilter = SkillCostFilter.All;
    private SkillSortMode _selectedSkillSortMode = SkillSortMode.Type;
    private EncyclopediaEntry _selectedEntry;
    private int _resultRefreshVersion;
    private int _observedSkillTuningRevision;
    private Tween _interfaceTween;
    private bool _isClosing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        BuildCatalog();
        BindUi();
        SelectModule(EncyclopediaModule.Home);
        _observedSkillTuningRevision = SkillTuning.Revision;
        PrepareInterfaceEnterState();
        PlayEnterAnimationDeferred();
    }

    public override void _Process(double delta)
    {
        if (!OS.IsDebugBuild() || SkillTuning.Revision == _observedSkillTuningRevision)
            return;

        _observedSkillTuningRevision = SkillTuning.Revision;
        RefreshSkillTuningCatalog();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            CloseWithExitAnimation();
        }
    }

    public override void _ExitTree()
    {
        _interfaceTween?.Kill();
        _interfaceTween = null;
        _skillSortOption?.ClosePopup();
    }

    private void BindUi()
    {
        _backdrop = GetNode<ColorRect>("Backdrop");
        _centerPanel = GetNode<PanelContainer>("CenterPanel");
        _header = GetNode<HBoxContainer>("CenterPanel/Margin/Root/Header");
        _modulePanel = GetNode<PanelContainer>("CenterPanel/Margin/Root/Body/ModulePanel");
        _contentRoot = GetNode<VBoxContainer>("CenterPanel/Margin/Root/Body/Content");
        _searchRow = GetNode<HBoxContainer>("CenterPanel/Margin/Root/Body/Content/SearchRow");
        _titleLabel = GetNode<Label>("CenterPanel/Margin/Root/Header/TitleBox/Title");
        _moduleTitle = GetNode<Label>("CenterPanel/Margin/Root/Body/Content/SearchRow/ModuleTitle");
        _searchBox = GetNode<LineEdit>("CenterPanel/Margin/Root/Body/Content/SearchRow/SearchBox");
        _countLabel = GetNode<Label>("CenterPanel/Margin/Root/Body/Content/SearchRow/CountLabel");
        _characterFilterRow = GetNode<HBoxContainer>(
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow"
        );
        _skillFilterPanel = GetNode<VBoxContainer>(
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/SkillFilterPanel"
        );
        _skillSortOption = GetNode<SettingsDropdown>(
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/SkillFilterPanel/SortSection/Margin/Root/SkillSortOptionHost/SkillSortOptionHostInner/SkillSortOption"
        );
        _skillGridPanel = GetNode<PanelContainer>(
            "CenterPanel/Margin/Root/Body/Content/Split/SkillGridPanel"
        );
        _skillGrid = GetNode<GridContainer>(
            "CenterPanel/Margin/Root/Body/Content/Split/SkillGridPanel/Margin/GridScroll/SkillGrid"
        );
        _detailPanel = GetNode<PanelContainer>(
            "CenterPanel/Margin/Root/Body/Content/Split/DetailPanel"
        );
        _detailCardHost = GetNode<CenterContainer>(
            "CenterPanel/Margin/Root/Body/Content/Split/DetailPanel/Margin/DetailRoot/DetailCardHost"
        );
        _detailLabel = GetNode<RichTextLabel>(
            "CenterPanel/Margin/Root/Body/Content/Split/DetailPanel/Margin/DetailRoot/DetailLabel"
        );

        _detailSkillCard = SkillCardScene.Instantiate<SkillCard>();
        _detailSkillCard.AutoPressEffect = false;
        _detailSkillCard.UseDefaultHoverEffect = false;
        _detailSkillCard.Button.Disabled = true;
        _detailSkillCard.ConfigureDisplayScale(Vector2.One);
        _detailCardHost.AddChild(_detailSkillCard);
        _detailSkillCard.CallDeferred(nameof(SkillCard.RestoreDisplayState));

        _searchBox.TextChanged += _ => RefreshResults();
        ApplySearchTheme(_searchBox);
        _detailLabel.AddThemeColorOverride(
            "default_color",
            new Color(0.83f, 0.9f, 0.96f, 0.96f)
        );
        _detailLabel.AddThemeColorOverride(
            "font_color",
            new Color(0.83f, 0.9f, 0.96f, 0.96f)
        );

        var closeButton = GetNode<Button>("CenterPanel/Margin/Root/Header/CloseButton");
        ApplyButtonTheme(closeButton);
        closeButton.Pressed += CloseWithExitAnimation;

        ApplyOptionTheme(_skillSortOption);
        _skillSortOption.AddItem(
            I18n.Tr("ui.encyclopedia.sort.type", "按类型"),
            (int)SkillSortMode.Type,
            (int)SkillSortMode.Type
        );
        _skillSortOption.AddItem(
            I18n.Tr("ui.encyclopedia.sort.name", "按名称"),
            (int)SkillSortMode.Name,
            (int)SkillSortMode.Name
        );
        _skillSortOption.AddItem(
            I18n.Tr("ui.encyclopedia.sort.cost", "按费用"),
            (int)SkillSortMode.Cost,
            (int)SkillSortMode.Cost
        );
        _skillSortOption.AddItem(
            I18n.Tr("ui.encyclopedia.sort.rarity", "按稀有度"),
            (int)SkillSortMode.Rarity,
            (int)SkillSortMode.Rarity
        );
        _skillSortOption.ItemSelected += OnSkillSortSelected;

        BuildModuleNavigationButtons();

        _characterFilterButtons.Clear();
        RegisterCharacterButton(
            PlayerCharacterKey.Echo,
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow/EchoButton"
        );
        RegisterCharacterButton(
            PlayerCharacterKey.Kasiya,
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow/KasiyaButton"
        );
        RegisterCharacterButton(
            PlayerCharacterKey.Mariya,
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow/MariyaButton"
        );
        RegisterCharacterButton(
            PlayerCharacterKey.Nightingale,
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow/NightingaleButton"
        );

        BuildSkillFilterButtons();
        RefreshFilterStates();
    }

    private void PrepareInterfaceEnterState()
    {
        SetControlAlpha(_backdrop, 0f);
        foreach (var item in CreateInterfaceAssemblyItems(entering: true))
            SetControlAlpha(item.Control, 0f);
    }

    private async void PlayEnterAnimationDeferred()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInsideTree() || _isClosing)
            return;

        PlayInterfaceAssemblyAnimation();
    }

    private void PlayInterfaceAssemblyAnimation()
    {
        _interfaceTween?.Kill();
        _interfaceTween = CreateTween();
        _interfaceTween.SetParallel(true);

        if (_backdrop != null)
        {
            SetControlAlpha(_backdrop, 0f);
            _interfaceTween
                .TweenProperty(_backdrop, "modulate:a", 1.0f, InterfaceEnterDuration * 0.72f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }

        foreach (var item in CreateInterfaceAssemblyItems(entering: true))
        {
            var control = item.Control;
            if (control == null || !GodotObject.IsInstanceValid(control) || !control.Visible)
                continue;

            Vector2 basePosition = control.Position;
            control.Position = basePosition + item.Offset;
            SetControlAlpha(control, 0f);
            _interfaceTween
                .TweenProperty(control, "position", basePosition, InterfaceEnterDuration)
                .SetDelay(item.Delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _interfaceTween
                .TweenProperty(control, "modulate:a", 1.0f, InterfaceEnterDuration * 0.75f)
                .SetDelay(item.Delay)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
        }
    }

    private async void CloseWithExitAnimation()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        _interfaceTween?.Kill();
        _interfaceTween = CreateTween();
        _interfaceTween.SetParallel(true);

        if (_backdrop != null)
        {
            _interfaceTween
                .TweenProperty(_backdrop, "modulate:a", 0.0f, InterfaceExitDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
        }

        float maxDelay = 0f;
        foreach (var item in CreateInterfaceAssemblyItems(entering: false))
        {
            var control = item.Control;
            if (control == null || !GodotObject.IsInstanceValid(control) || !control.Visible)
                continue;

            maxDelay = Math.Max(maxDelay, item.Delay);
            Vector2 targetPosition = control.Position + item.Offset;
            _interfaceTween
                .TweenProperty(control, "position", targetPosition, InterfaceExitDuration)
                .SetDelay(item.Delay)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            _interfaceTween
                .TweenProperty(control, "modulate:a", 0.0f, InterfaceExitDuration * 0.88f)
                .SetDelay(item.Delay)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);
        }

        await ToSignal(
            GetTree().CreateTimer(InterfaceExitDuration + maxDelay),
            SceneTreeTimer.SignalName.Timeout
        );

        if (IsInsideTree())
            QueueFree();
    }

    private List<AssemblyItem> CreateInterfaceAssemblyItems(bool entering)
    {
        float stagger = entering ? InterfaceEnterStagger : InterfaceExitStagger;
        return
        [
            new AssemblyItem(_header, new Vector2(0f, -34f), stagger * 0f),
            new AssemblyItem(_modulePanel, new Vector2(-54f, 26f), stagger * 1f),
            new AssemblyItem(_searchRow, new Vector2(42f, -14f), stagger * 2f),
            new AssemblyItem(_characterFilterRow, new Vector2(54f, 0f), stagger * 3f),
            new AssemblyItem(_skillGridPanel, new Vector2(0f, 42f), stagger * 4f),
            new AssemblyItem(_detailPanel, new Vector2(46f, 30f), stagger * 5f),
        ];
    }

    private static void SetControlAlpha(CanvasItem control, float alpha)
    {
        if (control == null || !GodotObject.IsInstanceValid(control))
            return;

        Color modulate = control.Modulate;
        modulate.A = alpha;
        control.Modulate = modulate;
    }

    private void RegisterCharacterButton(PlayerCharacterKey character, string nodePath)
    {
        var button = GetNode<Button>(nodePath);
        ApplyButtonTheme(button);
        button.Text = GetPlayerCharacterDisplayName(character);
        button.Pressed += () => SelectSkillCharacter(character);
        _characterFilterButtons[character] = button;
    }

    private void BuildSkillFilterButtons()
    {
        if (_skillFilterPanel == null)
            return;

        var typeFlow = FindSkillFilterNode<GridContainer>("TypeFilterGrid");
        if (typeFlow == null)
            return;

        AddSkillTypeFilterButton(typeFlow, I18n.Tr("ui.common.all", "全部"), SkillTypeFilter.All);
        AddSkillTypeFilterButton(
            typeFlow,
            I18n.Tr("skill_type.attack", "攻击"),
            SkillTypeFilter.Attack
        );
        AddSkillTypeFilterButton(
            typeFlow,
            I18n.Tr("skill_type.survive", "生存"),
            SkillTypeFilter.Survive
        );
        AddSkillTypeFilterButton(
            typeFlow,
            I18n.Tr("skill_type.special", "特殊"),
            SkillTypeFilter.Special
        );
        AddSkillTypeFilterButton(
            typeFlow,
            I18n.Tr("skill_type.ability", "能力"),
            SkillTypeFilter.Ability
        );
        AddSkillTypeFilterButton(
            typeFlow,
            I18n.Tr("ui.encyclopedia.skill_type.status", "状态"),
            SkillTypeFilter.Status
        );

        var rarityFlow = FindSkillFilterNode<VBoxContainer>("RarityFilterList");
        if (rarityFlow == null)
            return;

        AddSkillRarityFilterButton(rarityFlow, I18n.Tr("ui.common.all", "全部"), null);
        AddSkillRarityFilterButton(
            rarityFlow,
            I18n.Tr("ui.encyclopedia.rarity.common", "普通"),
            Skill.SkillRarity.Common
        );
        AddSkillRarityFilterButton(
            rarityFlow,
            I18n.Tr("ui.encyclopedia.rarity.uncommon", "罕见"),
            Skill.SkillRarity.Uncommon
        );
        AddSkillRarityFilterButton(
            rarityFlow,
            I18n.Tr("ui.encyclopedia.rarity.rare", "稀有"),
            Skill.SkillRarity.Rare
        );

        var costFlow = FindSkillFilterNode<GridContainer>("CostFilterGrid");
        if (costFlow == null)
            return;

        AddSkillCostFilterButton(costFlow, I18n.Tr("ui.common.all", "全部"), SkillCostFilter.All);
        AddSkillCostFilterButton(costFlow, "0", SkillCostFilter.Zero);
        AddSkillCostFilterButton(costFlow, "1", SkillCostFilter.One);
        AddSkillCostFilterButton(costFlow, "2", SkillCostFilter.Two);
        AddSkillCostFilterButton(costFlow, "3+", SkillCostFilter.ThreePlus);
        AddSkillCostFilterButton(costFlow, "X", SkillCostFilter.X);
    }

    private void BuildModuleNavigationButtons()
    {
        if (_skillFilterPanel == null)
            return;

        var section = new PanelContainer
        {
            Name = "ModuleNavigationSection",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        section.AddThemeStyleboxOverride(
            "panel",
            CreateFilterButtonStyle(new Color(0.07f, 0.11f, 0.16f, 0.9f))
        );

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        _moduleNavigationPanel = new VBoxContainer
        {
            Name = "ModuleNavigationPanel",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _moduleNavigationPanel.AddThemeConstantOverride("separation", 6);

        var title = new Label
        {
            Text = I18n.Tr("ui.encyclopedia.entry.title", "图鉴入口"),
            CustomMinimumSize = new Vector2(0, 28),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 19);
        title.Modulate = new Color(0.9f, 0.98f, 1f, 1f);
        _moduleNavigationPanel.AddChild(title);

        AddModuleNavigationButton(
            EncyclopediaModule.Skills,
            I18n.Tr("ui.encyclopedia.entry.skills", "卡牌图鉴")
        );
        AddModuleNavigationButton(
            EncyclopediaModule.Relics,
            I18n.Tr("ui.encyclopedia.entry.relics", "遗物图鉴")
        );
        AddModuleNavigationButton(
            EncyclopediaModule.Buffs,
            I18n.Tr("ui.encyclopedia.entry.buffs", "Buff图鉴")
        );

        margin.AddChild(_moduleNavigationPanel);
        section.AddChild(margin);
        _skillFilterPanel.AddChild(section);
        _skillFilterPanel.MoveChild(section, 0);
    }

    private void AddModuleNavigationButton(EncyclopediaModule module, string text)
    {
        if (_moduleNavigationPanel == null)
            return;

        Button button = CreateFilterButton(text);
        Texture2D icon = GetModuleIcon(module);
        if (icon != null)
        {
            button.Icon = icon;
            button.ExpandIcon = true;
            button.IconAlignment = HorizontalAlignment.Left;
            button.AddThemeConstantOverride("icon_max_width", 22);
        }
        button.CustomMinimumSize = new Vector2(0f, 44f);
        button.Alignment = HorizontalAlignment.Left;
        button.Pressed += () => SelectModule(module);
        _moduleNavigationPanel.AddChild(button);
        _moduleButtons[module] = button;
    }

    private T FindSkillFilterNode<T>(string name)
        where T : Node
    {
        return _skillFilterPanel?.FindChild(name, recursive: true, owned: false) as T;
    }

    private void BuildCatalog()
    {
        _entries[EncyclopediaModule.Buffs] = BuildBuffEntries();
        _entries[EncyclopediaModule.Skills] = BuildSkillEntries();
        _entries[EncyclopediaModule.Relics] = BuildRelicEntries();
        _entries[EncyclopediaModule.Items] = BuildItemEntries();
        _entries[EncyclopediaModule.Enemies] = BuildEnemyEntries();
    }

    private void RefreshSkillTuningCatalog()
    {
        _previewSkillCache.Clear();
        _entries[EncyclopediaModule.Skills] = BuildSkillEntries();

        if (_currentModule == EncyclopediaModule.Skills)
            RefreshResults();

        GD.Print($"[SkillTuning] refreshed encyclopedia at revision {_observedSkillTuningRevision}");
    }

    private List<EncyclopediaEntry> BuildBuffEntries()
    {
        return Enum.GetValues<Buff.BuffName>()
            .Select(buff =>
            {
                string name = Buff.GetBuffDisplayName(buff);
                string nature = Buff.IsDebuff(buff)
                    ? I18n.Tr("ui.encyclopedia.buff.debuff", "负面状态")
                    : I18n.Tr("ui.encyclopedia.buff.buff", "正面状态");
                string effect = Buff.GetBuffEffectText(buff);
                string detail = $"[b]{EscapeBbcode(name)}[/b]\n{nature}\n\n{effect}";
                return CreateEntry(name, nature, detail, buff.ToString(), buffName: buff);
            })
            .OrderBy(entry => entry.Title)
            .ToList();
    }

    private List<EncyclopediaEntry> BuildSkillEntries()
    {
        var entries = new List<EncyclopediaEntry>();
        foreach (PlayerCharacterKey character in Enum.GetValues<PlayerCharacterKey>())
        {
            string characterName = GetPlayerCharacterDisplayName(character);
            foreach (SkillID skillId in Skill.GetPlayerSkillPool(character))
            {
                Skill skill = Skill.GetSkill(skillId);
                if (skill == null)
                    continue;

                skill.SetPreviewStats(0, 0, 1);
                skill.UpdateDescription();

                bool isStatusCard = skill.IsStatusCard;
                SkillTypeFilter typeFilter = ToSkillTypeFilter(skill.SkillType, isStatusCard);
                Skill.SkillRarity rarity = skill.Rarity;
                SkillCostFilter costFilter = ToSkillCostFilter(skill);
                int sortCost = GetSkillCostSortValue(skill);
                string type = isStatusCard
                    ? I18n.Tr("ui.encyclopedia.skill_type.status", "状态")
                    : skill.SkillType.GetDescription();
                string rarityText = GetRarityLabel(rarity);
                string costText = !skill.CanBePlayed
                    ? I18n.Tr("ui.encyclopedia.skill_cost.unplayable", "不可打出")
                    : skill.CardEnergyCostText;
                string costDisplayText = !skill.CanBePlayed
                    ? EscapeBbcode(costText)
                    : $"[color=#ffd36b]{EscapeBbcode(costText)}[/color]";
                string description = string.IsNullOrWhiteSpace(skill.Description)
                    ? I18n.Tr("ui.encyclopedia.no_description", "暂无描述。")
                    : skill.Description;
                string detail =
                    $"[b]{EscapeBbcode(skill.SkillName)}[/b]\n"
                    + $"{EscapeBbcode(characterName)} · {EscapeBbcode(type)} · {EscapeBbcode(rarityText)}\n"
                    + I18n.Format(
                        "ui.encyclopedia.detail.cost_line",
                        "费用：{cost}\n",
                        ("cost", costDisplayText)
                    )
                    + $"ID: {skillId}\n\n"
                    + description;

                entries.Add(
                    CreateEntry(
                        skill.SkillName,
                        I18n.Format(
                            "ui.encyclopedia.skill.subtitle",
                            "{type} · {rarity} · 费用 {cost}",
                            ("type", type),
                            ("rarity", rarityText),
                            ("cost", costText)
                        ),
                        detail,
                        $"{skillId} {rarityText} {costText}",
                        characterName,
                        character,
                        skillId,
                        typeFilter,
                        rarity,
                        costFilter,
                        sortCost,
                        isStatusCard
                    )
                );
            }
        }

        return entries
            .OrderBy(entry => entry.CharacterKey)
            .ThenBy(entry => GetSkillTypeSortOrder(entry.SkillTypeFilter))
            .ThenBy(entry => entry.SkillCostSortValue)
            .ThenBy(entry => entry.Title)
            .ToList();
    }

    private List<EncyclopediaEntry> BuildRelicEntries()
    {
        return Enum.GetValues<RelicID>()
            .Where(id => id != RelicID.curse)
            .Select(id =>
            {
                Relic relic = Relic.Create(id);
                string subtitle = I18n.Tr("ui.encyclopedia.relic.subtitle", "遗物");
                string detail =
                    $"[b]{EscapeBbcode(relic.RelicName)}[/b]\n{EscapeBbcode(subtitle)}\nID: {id}\n\n{GlobalFunction.ColorizeNumbers(relic.RelicDescription)}";
                return CreateEntry(
                    relic.RelicName,
                    subtitle,
                    detail,
                    id.ToString(),
                    relicId: id
                );
            })
            .OrderBy(entry => entry.Title)
            .ToList();
    }

    private List<EncyclopediaEntry> BuildItemEntries()
    {
        return Enum.GetValues<ItemID>()
            .Where(id => id != ItemID.None)
            .Select(id =>
            {
                string name = ConsumeItem.GetItemName(id);
                string description = ConsumeItem.GetItemDescription(id);
                string subtitle = I18n.Format(
                    "ui.encyclopedia.item.value",
                    "数值：{value}",
                    ("value", ConsumeItem.GetItemValue(id))
                );
                string detail =
                    $"[b]{EscapeBbcode(name)}[/b]\n{EscapeBbcode(subtitle)}\nID: {id}\n\n{GlobalFunction.ColorizeNumbers(description)}";
                return CreateEntry(name, subtitle, detail, id.ToString());
            })
            .OrderBy(entry => entry.Title)
            .ToList();
    }

    private List<EncyclopediaEntry> BuildEnemyEntries()
    {
        return CreateEnemyCatalog()
            .Select(regedit =>
            {
                string title = LocalizationHelper.GetEnemyDisplayName(regedit);
                string subtitle = I18n.Format(
                    "ui.encyclopedia.enemy.subtitle",
                    "生命 {life} · 力量 {power} · 生存 {survivability}",
                    ("life", regedit.MaxLife),
                    ("power", regedit.Power),
                    ("survivability", regedit.Survivability)
                );
                string detail =
                    $"[b]{EscapeBbcode(title)}[/b]\n{EscapeBbcode(subtitle)}\n"
                    + I18n.Format(
                        "ui.encyclopedia.enemy.type_line",
                        "类型：{type}\n\n",
                        ("type", regedit.PType)
                    )
                    + BuildPassiveDetail(regedit)
                    + BuildEnemySkillDetail(regedit);
                return CreateEntry(title, subtitle, detail, regedit.GetType().Name);
            })
            .OrderBy(entry => entry.Title)
            .ToList();
    }

    private static EnemyRegedit[] CreateEnemyCatalog()
    {
        return
        [
            new EvilRegedit(),
            new FearWormRegedit(),
            new ArmonRegedit(),
            new ArroganceRegedit(),
            new AlienBodyRegedit(),
            new RedHuskRegedit(),
            new WarRegedit(),
            new FerociouessRegedit(),
            new TurbineRegedit(),
            new BlackHawkRegedit(),
            new InexorabilityRegedit(),
            new GraveWraithRegedit(),
            new VoidAcolyteRegedit(),
            new HollowBulwarkRegedit(),
            new MarrowReaverRegedit(),
            new DeathRegedit(),
        ];
    }

    private static string BuildPassiveDetail(EnemyRegedit regedit)
    {
        if (
            string.IsNullOrWhiteSpace(regedit.PassiveName)
            && string.IsNullOrWhiteSpace(regedit.PassiveDescription)
        )
        {
            return string.Empty;
        }

        return I18n.Format(
            "ui.encyclopedia.enemy.passive_block",
            "[b]被动 · {name}[/b]\n{description}\n\n",
            ("name", EscapeBbcode(LocalizationHelper.GetEnemyPassiveName(regedit))),
            ("description", LocalizationHelper.GetEnemyPassiveDescription(regedit))
        );
    }

    private static string BuildEnemySkillDetail(EnemyRegedit regedit)
    {
        UserSettings.EnsureLoaded();
        if (UserSettings.HideEnemySkills)
            return string.Empty;

        var ids = regedit.SkillIDs ?? Array.Empty<SkillID>();
        if (ids.Length == 0)
            return string.Empty;

        var lines = new List<string>
        {
            I18n.Tr("ui.encyclopedia.enemy.skills_header", "[b]技能[/b]"),
        };
        foreach (SkillID id in ids)
        {
            Skill skill = Skill.GetSkill(id);
            if (skill == null)
                continue;

            skill.SetPreviewStats(
                regedit.Power,
                regedit.Survivability,
                1,
                basePowerContribution: regedit.BasePowerContribution,
                baseSurvivabilityContribution: regedit.BaseSurvivabilityContribution
            );
            skill.UpdateDescription();
            string type = skill.SkillType.GetDescription();
            lines.Add($"[b]{EscapeBbcode(skill.SkillName)}[/b] · {EscapeBbcode(type)}");
            if (!string.IsNullOrWhiteSpace(skill.Description))
                lines.Add(skill.Description);
        }

        return string.Join("\n", lines);
    }

    private void SelectModule(EncyclopediaModule module)
    {
        _currentModule = module;
        _selectedEntry = null;
        string moduleName = ModuleNames.GetValueOrDefault(module, module.ToString());
        if (_titleLabel != null)
            _titleLabel.Text = moduleName;
        if (_moduleTitle != null)
            _moduleTitle.Text = moduleName;
        _searchBox.Text = string.Empty;
        RefreshModuleButtonStates();
        RefreshCharacterFilter();
        RefreshFilterStates();
        RefreshContentMode();
        RefreshResults();
    }

    private void SelectSkillCharacter(PlayerCharacterKey character)
    {
        if (_selectedSkillCharacter == character)
            return;

        _selectedSkillCharacter = character;
        RefreshCharacterFilter();
        RefreshResults();
    }

    private async void RefreshResults()
    {
        int refreshVersion = ++_resultRefreshVersion;
        if (ShouldAnimateSkillCardSwitch())
            await ClearSkillCardResultsAnimatedAsync(refreshVersion);
        else
            ClearResultNodes();

        if (refreshVersion != _resultRefreshVersion || !IsInsideTree())
            return;

        if (_currentModule == EncyclopediaModule.Home)
        {
            AddHomeEntries();
            SelectEntry(null);
            return;
        }

        string query = NormalizeSearch(_searchBox.Text);
        var source = _entries.TryGetValue(_currentModule, out var entries)
            ? entries
            : new List<EncyclopediaEntry>();

        if (_currentModule == EncyclopediaModule.Skills)
        {
            source = source.Where(entry => entry.CharacterKey == _selectedSkillCharacter).ToList();
        }

        var filtered = source
            .Where(entry => string.IsNullOrWhiteSpace(query) || entry.SearchText.Contains(query))
            .ToList();

        if (_currentModule == EncyclopediaModule.Skills)
        {
            filtered = filtered.Where(MatchesSkillFilters).ToList();
            filtered = SortSkillEntries(filtered).ToList();
        }

        _countLabel.Text = $"{filtered.Count}/{source.Count}";

        if (_currentModule == EncyclopediaModule.Skills)
        {
            await AddSkillCardResultsAsync(filtered, refreshVersion);
            if (refreshVersion != _resultRefreshVersion || !IsInsideTree())
                return;
        }
        else
            AddFlatResults(filtered);

        SelectEntry(filtered.FirstOrDefault());
    }

    private void RefreshCharacterFilter()
    {
        bool show = _currentModule == EncyclopediaModule.Skills;
        _characterFilterRow.Visible = show;
        if (!show)
            return;

        SetControlAlpha(_characterFilterRow, 1f);
        foreach (var pair in _characterFilterButtons)
        {
            bool selected = pair.Key == _selectedSkillCharacter;
            pair.Value.Text = GetPlayerCharacterDisplayName(pair.Key);
            pair.Value.Disabled = selected;
            pair.Value.Modulate = selected ? new Color(0.74f, 0.9f, 1f, 1f) : Colors.White;
        }
    }

    private void RefreshContentMode()
    {
        bool showFlatDetail =
            _currentModule == EncyclopediaModule.Relics
            || _currentModule == EncyclopediaModule.Buffs;
        _skillGridPanel.Visible = true;
        _searchRow.Visible = _currentModule != EncyclopediaModule.Home;
        _modulePanel.Visible = true;
        _skillFilterPanel.Visible = true;
        SetSkillFilterControlsVisible(_currentModule == EncyclopediaModule.Skills);
        _detailCardHost.Visible = false;
        _detailPanel.Visible = showFlatDetail;
        _detailPanel.CustomMinimumSize = showFlatDetail
            ? new Vector2(EncyclopediaRelicDetailWidth, 0f)
            : Vector2.Zero;
        _detailPanel.SizeFlagsHorizontal = showFlatDetail ? SizeFlags.Fill : SizeFlags.ShrinkEnd;
        _skillGridPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SetControlAlpha(_skillGridPanel, 1f);

        if (_searchRow.Visible)
            SetControlAlpha(_searchRow, 1f);

        if (showFlatDetail)
        {
            SetControlAlpha(_detailPanel, 1f);
            SetControlAlpha(_detailLabel, 1f);
        }

        if (_skillGrid != null)
        {
            _skillGrid.Columns =
                _currentModule == EncyclopediaModule.Home ? 3
                : _currentModule == EncyclopediaModule.Skills ? 5
                : showFlatDetail ? 2
                : 2;
            _skillGrid.AddThemeConstantOverride(
                "h_separation",
                showFlatDetail ? 14 : 30
            );
            _skillGrid.AddThemeConstantOverride(
                "v_separation",
                showFlatDetail ? 12 : 20
            );
        }

        if (_searchBox != null)
        {
            _searchBox.PlaceholderText =
                _currentModule == EncyclopediaModule.Relics
                    ? I18n.Tr("ui.encyclopedia.search.relics", "搜索遗物名称或效果")
                : _currentModule == EncyclopediaModule.Buffs
                    ? I18n.Tr("ui.encyclopedia.search.buffs", "搜索Buff名称或效果")
                    : I18n.Tr("ui.encyclopedia.search.skills", "搜索卡牌名称、类型或描述");
        }
    }

    private void RefreshFilterStates()
    {
        if (_skillFilterPanel != null)
            _skillFilterPanel.Visible = true;

        SetSkillFilterControlsVisible(_currentModule == EncyclopediaModule.Skills);

        foreach (var pair in _skillTypeFilterButtons)
        {
            bool selected = pair.Key == _selectedSkillTypeFilter;
            pair.Value.Disabled = selected;
            pair.Value.Modulate = selected ? new Color(0.74f, 0.9f, 1f, 1f) : Colors.White;
        }

        foreach (var pair in _skillRarityFilterButtons)
        {
            bool selected =
                _selectedSkillRarityFilter.HasValue && pair.Key == _selectedSkillRarityFilter.Value;
            pair.Value.Disabled = selected;
            pair.Value.Modulate = selected ? new Color(0.74f, 0.9f, 1f, 1f) : Colors.White;
        }

        if (_allSkillRarityFilterButton != null)
        {
            bool selected = !_selectedSkillRarityFilter.HasValue;
            _allSkillRarityFilterButton.Disabled = selected;
            _allSkillRarityFilterButton.Modulate = selected
                ? new Color(0.74f, 0.9f, 1f, 1f)
                : Colors.White;
        }

        foreach (var pair in _skillCostFilterButtons)
        {
            bool selected = pair.Key == _selectedSkillCostFilter;
            pair.Value.Disabled = selected;
            pair.Value.Modulate = selected ? new Color(0.74f, 0.9f, 1f, 1f) : Colors.White;
        }

        if (_skillSortOption != null)
        {
            int index = FindSkillSortOptionIndex((int)_selectedSkillSortMode);
            if (index >= 0 && _skillSortOption.Selected != index)
                _skillSortOption.Select(index);
        }
    }

    private void SetSkillFilterControlsVisible(bool visible)
    {
        if (_skillFilterPanel == null)
            return;

        foreach (Node child in _skillFilterPanel.GetChildren())
        {
            if (child is not Control control)
                continue;

            control.Visible = control.Name == "ModuleNavigationSection" || visible;
        }
    }

    private void AddHomeEntries()
    {
        AddHomeEntryButton(
            EncyclopediaModule.Skills,
            I18n.Tr("ui.encyclopedia.entry.skills", "卡牌图鉴"),
            I18n.Tr("ui.encyclopedia.entry.skills_desc", "查看角色技能、费用、类型和稀有度。")
        );
        AddHomeEntryButton(
            EncyclopediaModule.Relics,
            I18n.Tr("ui.encyclopedia.entry.relics", "遗物图鉴"),
            I18n.Tr("ui.encyclopedia.entry.relics_desc", "查看遗物名称和战斗效果。")
        );
        AddHomeEntryButton(
            EncyclopediaModule.Buffs,
            I18n.Tr("ui.encyclopedia.entry.buffs", "Buff图鉴"),
            I18n.Tr("ui.encyclopedia.entry.buffs_desc", "查看正面与负面状态的触发规则和效果。")
        );
    }

    private void AddHomeEntryButton(EncyclopediaModule module, string title, string description)
    {
        Color accent = GetModuleAccent(module);
        Button button = CreateButton(string.Empty, EncyclopediaHomeButtonSize);
        button.AddThemeStyleboxOverride("normal", CreateHomeCardStyle(accent, 0.11f));
        button.AddThemeStyleboxOverride("hover", CreateHomeCardStyle(accent, 0.21f));
        button.AddThemeStyleboxOverride("pressed", CreateHomeCardStyle(accent, 0.08f));

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", 18);

        var iconBadge = new PanelContainer
        {
            CustomMinimumSize = new Vector2(72f, 72f),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        iconBadge.AddThemeStyleboxOverride("panel", CreateModuleBadgeStyle(accent));

        var icon = new TextureRect
        {
            Texture = GetModuleIcon(module),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        iconBadge.AddChild(icon);

        int entryCount = _entries.TryGetValue(module, out var entries) ? entries.Count : 0;
        var textColumn = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        textColumn.AddThemeConstantOverride("separation", 3);

        var overline = new Label
        {
            Text = I18n.Format(
                "ui.encyclopedia.entry.count",
                "ARCHIVE  ·  {count} 条记录",
                ("count", entryCount.ToString())
            ),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        overline.AddThemeFontSizeOverride("font_size", 13);
        overline.AddThemeColorOverride("font_color", accent with { A = 0.9f });

        var titleLabel = new Label
        {
            Text = title,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 25);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.94f, 0.98f, 1f, 1f));

        var descriptionLabel = new Label
        {
            Text = description,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        descriptionLabel.AddThemeFontSizeOverride("font_size", 15);
        descriptionLabel.AddThemeColorOverride(
            "font_color",
            new Color(0.66f, 0.76f, 0.84f, 0.92f)
        );

        var arrow = new Label
        {
            Text = "›",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        arrow.AddThemeFontSizeOverride("font_size", 34);
        arrow.AddThemeColorOverride("font_color", accent);

        textColumn.AddChild(overline);
        textColumn.AddChild(titleLabel);
        textColumn.AddChild(descriptionLabel);
        row.AddChild(iconBadge);
        row.AddChild(textColumn);
        row.AddChild(arrow);
        margin.AddChild(row);
        button.AddChild(margin);
        button.Pressed += () => SelectModule(module);
        _skillGrid.AddChild(button);
    }

    private void AddFlatResults(IEnumerable<EncyclopediaEntry> entries)
    {
        foreach (var entry in entries)
        {
            Button button = CreateButton(
                entry.BuffName.HasValue ? string.Empty : $"{entry.Title}\n{entry.Subtitle}",
                EncyclopediaRelicButtonSize
            );
            button.Alignment = HorizontalAlignment.Left;
            button.AddThemeFontSizeOverride("font_size", 18);
            button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;

            if (entry.RelicId.HasValue)
            {
                string texturePath = Relic.GetIconTexturePath(entry.RelicId.Value);
                Texture2D texture = string.IsNullOrWhiteSpace(texturePath)
                    ? null
                    : GD.Load<Texture2D>(texturePath);
                if (texture != null)
                {
                    button.Icon = texture;
                    button.ExpandIcon = true;
                    button.AddThemeConstantOverride("icon_max_width", 46);
                    button.IconAlignment = HorizontalAlignment.Left;
                }
            }
            else if (entry.BuffName.HasValue)
            {
                ConfigureBuffEntryButton(button, entry);
            }

            button.Pressed += () => SelectEntry(entry);
            _skillGrid.AddChild(button);
            _buttonEntries[button] = entry;
        }
    }

    private static void ConfigureBuffEntryButton(Button button, EncyclopediaEntry entry)
    {
        if (button == null)
            return;

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
        };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 8);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        row.AddThemeConstantOverride("separation", 14);

        var iconHost = new CenterContainer
        {
            CustomMinimumSize = new Vector2(48f, 0f),
            MouseFilter = MouseFilterEnum.Ignore,
        };

        Control icon = entry.BuffName.HasValue
            ? Buff.CreateBuffTooltipIcon(entry.BuffName.Value)
            : null;
        if (icon != null)
        {
            const float iconSize = 42f;
            icon.Name = "BuffIcon";
            icon.MouseFilter = MouseFilterEnum.Ignore;
            icon.Modulate = Colors.White;
            if (icon is ColorRect colorRect && HasTextureIconChild(icon))
                colorRect.Color = Colors.Transparent;
            icon.CustomMinimumSize = new Vector2(iconSize, iconSize);
            icon.Size = new Vector2(iconSize, iconSize);
            icon.SetAnchorsPreset(LayoutPreset.TopLeft);
            Buff.RefreshTextureIconOverrideLayout(icon);
            iconHost.AddChild(icon);
        }

        var textColumn = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        textColumn.AddThemeConstantOverride("separation", 0);

        var title = new Label
        {
            Text = entry.Title,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(0.93f, 0.97f, 1f, 0.96f));

        var subtitle = new Label
        {
            Text = entry.Subtitle,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        subtitle.AddThemeFontSizeOverride("font_size", 17);
        subtitle.AddThemeColorOverride("font_color", new Color(0.82f, 0.92f, 1f, 0.88f));

        textColumn.AddChild(title);
        textColumn.AddChild(subtitle);
        row.AddChild(iconHost);
        row.AddChild(textColumn);
        margin.AddChild(row);
        button.AddChild(margin);
    }

    private static bool HasTextureIconChild(Node root)
    {
        if (root == null || !GodotObject.IsInstanceValid(root))
            return false;

        foreach (Node child in root.GetChildren())
        {
            if (child is TextureRect)
                return true;

            if (HasTextureIconChild(child))
                return true;
        }

        return false;
    }

    private async Task AddSkillCardResultsAsync(
        IReadOnlyList<EncyclopediaEntry> entries,
        int refreshVersion
    )
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (refreshVersion != _resultRefreshVersion || !IsInsideTree())
                return;

            var card = AddSkillCard(entries[i]);
            if (card != null)
            {
                if (ShouldAnimateSkillCardAtIndex(i))
                {
                    card.CallDeferred(
                        nameof(SkillCard.StartAnimationWithDuration),
                        GetSkillCardEnterDelay(i, GetSkillCardAnimatedCount(entries.Count)),
                        SkillCardSwitchEnterDuration
                    );
                }
                else
                    card.CallDeferred(nameof(SkillCard.RestoreDisplayState));
            }

            if ((i + 1) % SkillCardsBuildPerFrame == 0 && i + 1 < entries.Count)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private SkillCard AddSkillCard(EncyclopediaEntry entry)
    {
        var frame = new Control
        {
            CustomMinimumSize =
                EncyclopediaSkillCardDisplaySize + EncyclopediaSkillCardHoverPadding * 2f,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        Skill skill = entry.SkillId.HasValue ? GetPreviewSkill(entry.SkillId.Value) : null;
        var card = SkillCardScene.Instantiate<SkillCard>();
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.Set("stretch", true);
        card.Set("stretch_shrink", 1);
        card.PreviewCharacterName = entry.Group;
        card.DisplayNameOverride = entry.Title;
        card.CustomMinimumSize = EncyclopediaSkillCardDisplaySize;
        card.Size = EncyclopediaSkillCardDisplaySize;
        card.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        card.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        card.PivotOffset = Vector2.Zero;
        card.Resized += () => PositionSkillCardInFrame(frame, card);
        frame.Resized += () => PositionSkillCardInFrame(frame, card);
        card.SetSkill(skill);
        card.Button.Pressed += () => SelectEntry(entry);
        card.Button.MouseEntered += () => TweenSkillCardHover(frame, card, true);
        card.Button.MouseExited += () => TweenSkillCardHover(card, false);

        frame.AddChild(card);
        _skillGrid.AddChild(frame);
        PositionSkillCardInFrame(frame, card);
        _skillCardFrames[entry] = frame;
        return card;
    }

    private static void PositionSkillCardInFrame(Control frame, SkillCard card)
    {
        if (
            frame == null
            || card == null
            || !GodotObject.IsInstanceValid(frame)
            || !GodotObject.IsInstanceValid(card)
        )
        {
            return;
        }

        card.Position = (frame.Size - card.Size * card.Scale) * 0.5f;
        card.PivotOffset = Vector2.Zero;
    }

    private void TweenSkillCardHover(Control frame, SkillCard card, bool hovered)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (
            _skillCardHoverTweens.TryGetValue(card, out Tween previousTween)
            && previousTween != null
            && GodotObject.IsInstanceValid(previousTween)
        )
        {
            previousTween.Kill();
        }

        PositionSkillCardInFrame(frame, card);
        if (hovered)
            card.ZIndex = 2;

        float scaleFactor = hovered ? 1.055f : 1f;
        Vector2 targetScale = Vector2.One * scaleFactor;
        Vector2 targetPosition = frame != null
            ? (frame.Size - card.Size * scaleFactor) * 0.5f
            : card.Position;
        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "scale", targetScale, hovered ? 0.14f : 0.11f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        tween
            .TweenProperty(card, "position", targetPosition, hovered ? 0.14f : 0.11f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        if (!hovered)
            tween.Finished += () =>
            {
                if (GodotObject.IsInstanceValid(card))
                    card.ZIndex = 0;
            };
        _skillCardHoverTweens[card] = tween;
    }

    private void TweenSkillCardHover(SkillCard card, bool hovered)
    {
        Control frame = card?.GetParent() as Control;
        TweenSkillCardHover(frame, card, hovered);
    }

    private void SelectEntry(EncyclopediaEntry entry)
    {
        _selectedEntry = entry;
        _detailLabel.Text =
            entry?.Detail ?? I18n.Tr("ui.encyclopedia.no_match", "没有找到匹配条目。");

        foreach (var pair in _buttonEntries)
        {
            bool selected = pair.Value == _selectedEntry;
            Color color =
                selected
                    ? new Color(0.74f, 0.9f, 1f, 1f)
                    : new Color(0.93f, 0.97f, 1f, 0.92f);
            pair.Key.AddThemeColorOverride("font_color", color);
            pair.Key.AddThemeStyleboxOverride(
                "normal",
                CreateButtonStyle(
                    selected
                        ? new Color(0.18f, 0.32f, 0.46f, 1f)
                        : new Color(0.10f, 0.15f, 0.22f, 0.95f)
                )
            );
            pair.Key.AddThemeStyleboxOverride(
                "hover",
                CreateButtonStyle(
                    selected
                        ? new Color(0.22f, 0.39f, 0.56f, 1f)
                        : new Color(0.16f, 0.25f, 0.34f, 1f)
                )
            );
        }

    }

    private void RefreshModuleButtonStates()
    {
        foreach (var pair in _moduleButtons)
        {
            bool selected = pair.Key == _currentModule;
            Color accent = GetModuleAccent(pair.Key);
            pair.Value.Disabled = selected;
            pair.Value.Modulate = Colors.White;
            pair.Value.AddThemeColorOverride(
                selected ? "font_disabled_color" : "font_color",
                selected
                    ? new Color(0.96f, 0.99f, 1f, 1f)
                    : new Color(0.8f, 0.87f, 0.93f, 0.94f)
            );
            pair.Value.AddThemeStyleboxOverride(
                selected ? "disabled" : "normal",
                CreateNavigationButtonStyle(accent, selected)
            );
        }
    }

    private void ClearResultNodes()
    {
        foreach (Tween tween in _skillCardHoverTweens.Values)
        {
            if (tween != null && GodotObject.IsInstanceValid(tween))
                tween.Kill();
        }
        _skillCardHoverTweens.Clear();

        foreach (Node child in _skillGrid.GetChildren())
            child.QueueFree();

        _buttonEntries.Clear();
        _skillCardFrames.Clear();
    }

    private bool ShouldAnimateSkillCardSwitch()
    {
        return _currentModule == EncyclopediaModule.Skills
            && _skillGrid != null
            && _skillGrid.GetChildCount() > 0
            && IsInsideTree();
    }

    private async Task ClearSkillCardResultsAnimatedAsync(int refreshVersion)
    {
        _buttonEntries.Clear();
        _skillCardFrames.Clear();

        var holdersToAnimate = new List<Control>();
        foreach (Node child in _skillGrid.GetChildren())
        {
            if (child is Control holder)
                holdersToAnimate.Add(holder);
        }

        if (holdersToAnimate.Count <= 0)
        {
            ClearResultNodes();
            return;
        }

        int animatedCount = GetSkillCardAnimatedCount(holdersToAnimate.Count);
        for (int i = 0; i < holdersToAnimate.Count; i++)
        {
            var holder = holdersToAnimate[i];
            bool animateCard = ShouldAnimateSkillCardAtIndex(i);
            float delay = animateCard ? GetSkillCardExitDelay(i, animatedCount) : 0f;
            var tween = holder.CreateTween();
            tween.TweenProperty(holder, "modulate:a", 0.0f, 0.10f).SetDelay(delay);

            if (animateCard && TryGetSkillCardFromHolder(holder, out var card))
            {
                var cardTween = card.CreateTween();
                cardTween.TweenCallback(Callable.From(() => card.Vanish())).SetDelay(delay);
            }
        }

        float waitTime =
            SkillCardSwitchExitDuration
            + GetSkillCardExitDelay(animatedCount - 1, animatedCount);
        await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);

        if (refreshVersion != _resultRefreshVersion || !IsInsideTree())
            return;

        ClearResultNodes();
    }

    private void ShuffleSkillAnimationOrder<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = _skillAnimationRandom.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private static bool TryGetSkillCardFromHolder(Control holder, out SkillCard card)
    {
        card = null;
        if (holder == null)
            return false;

        foreach (Node child in holder.GetChildren())
        {
            if (child is SkillCard directCard)
            {
                card = directCard;
                return true;
            }

            if (child is Control nestedControl)
            {
                foreach (Node nestedChild in nestedControl.GetChildren())
                {
                    if (nestedChild is SkillCard nestedCard)
                    {
                        card = nestedCard;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static float GetSkillCardEnterDelay(int index, int count)
    {
        if (index <= 0 || count <= 1)
            return 0f;

        float maxDelay = Mathf.Max(
            0f,
            SkillCardSwitchMaxTotalDuration - SkillCardSwitchEnterDuration
        );
        float stagger = Mathf.Min(SkillCardSwitchEnterStagger, maxDelay / (count - 1));
        return stagger * index;
    }

    private static float GetSkillCardExitDelay(int index, int count)
    {
        if (index <= 0 || count <= 1)
            return 0f;

        float maxDelay = Mathf.Max(
            0f,
            SkillCardSwitchMaxExitTotalDuration - SkillCardSwitchExitDuration
        );
        float stagger = Mathf.Min(SkillCardSwitchExitStagger, maxDelay / (count - 1));
        return stagger * index;
    }

    private static bool ShouldAnimateSkillCardAtIndex(int index)
    {
        return index >= 0 && index < SkillCardsAnimatedOnSwitch;
    }

    private static int GetSkillCardAnimatedCount(int count)
    {
        return Math.Min(Math.Max(count, 0), SkillCardsAnimatedOnSwitch);
    }

    private bool MatchesSkillFilters(EncyclopediaEntry entry)
    {
        if (
            _selectedSkillTypeFilter != SkillTypeFilter.All
            && entry.SkillTypeFilter != _selectedSkillTypeFilter
        )
            return false;

        if (
            _selectedSkillRarityFilter.HasValue
            && entry.SkillRarity != _selectedSkillRarityFilter.Value
        )
            return false;

        if (
            _selectedSkillCostFilter != SkillCostFilter.All
            && entry.SkillCostFilter != _selectedSkillCostFilter
        )
            return false;

        return true;
    }

    private IEnumerable<EncyclopediaEntry> SortSkillEntries(IEnumerable<EncyclopediaEntry> entries)
    {
        return _selectedSkillSortMode switch
        {
            SkillSortMode.Name => entries
                .OrderBy(entry => entry.Title)
                .ThenBy(entry => entry.SkillCostSortValue)
                .ThenBy(entry => entry.SkillRarity),
            SkillSortMode.Cost => entries
                .OrderBy(entry => entry.SkillCostSortValue)
                .ThenBy(entry => GetSkillTypeSortOrder(entry.SkillTypeFilter))
                .ThenBy(entry => entry.Title),
            SkillSortMode.Rarity => entries
                .OrderByDescending(entry => GetSkillRaritySortOrder(entry.SkillRarity))
                .ThenBy(entry => entry.SkillCostSortValue)
                .ThenBy(entry => entry.Title),
            _ => entries
                .OrderBy(entry => GetSkillTypeSortOrder(entry.SkillTypeFilter))
                .ThenBy(entry => entry.SkillCostSortValue)
                .ThenByDescending(entry => GetSkillRaritySortOrder(entry.SkillRarity))
                .ThenBy(entry => entry.Title),
        };
    }

    private void AddSkillTypeFilterButton(
        GridContainer container,
        string text,
        SkillTypeFilter filter
    )
    {
        Button button = CreateFilterButton(text, wide: false);
        button.Pressed += () =>
        {
            _selectedSkillTypeFilter = filter;
            RefreshFilterStates();
            RefreshResults();
        };
        container.AddChild(button);
        _skillTypeFilterButtons[filter] = button;
    }

    private void AddSkillRarityFilterButton(
        VBoxContainer container,
        string text,
        Skill.SkillRarity? rarity
    )
    {
        Button button = CreateFilterButton(text);
        button.Alignment = HorizontalAlignment.Left;
        button.Pressed += () =>
        {
            _selectedSkillRarityFilter = rarity;
            RefreshFilterStates();
            RefreshResults();
        };
        container.AddChild(button);
        if (rarity.HasValue)
            _skillRarityFilterButtons[rarity.Value] = button;
        else
            _allSkillRarityFilterButton = button;
    }

    private void AddSkillCostFilterButton(
        GridContainer container,
        string text,
        SkillCostFilter filter
    )
    {
        Button button = CreateFilterButton(text, wide: false);
        button.Pressed += () =>
        {
            _selectedSkillCostFilter = filter;
            RefreshFilterStates();
            RefreshResults();
        };
        container.AddChild(button);
        _skillCostFilterButtons[filter] = button;
    }

    private void OnSkillSortSelected(long index)
    {
        if (_skillSortOption == null)
            return;

        int selectedIndex = (int)index;
        if (selectedIndex < 0 || selectedIndex >= _skillSortOption.ItemCount)
            return;

        _selectedSkillSortMode = (SkillSortMode)_skillSortOption.GetItemId(selectedIndex);
        RefreshResults();
    }

    private static EncyclopediaEntry CreateEntry(
        string title,
        string subtitle,
        string detail,
        string extraSearch = null,
        string group = null,
        PlayerCharacterKey? characterKey = null,
        SkillID? skillId = null,
        SkillTypeFilter skillTypeFilter = SkillTypeFilter.All,
        Skill.SkillRarity skillRarity = Skill.SkillRarity.Common,
        SkillCostFilter skillCostFilter = SkillCostFilter.All,
        int skillCostSortValue = 0,
        bool isStatusCard = false,
        RelicID? relicId = null,
        Buff.BuffName? buffName = null
    )
    {
        string search = NormalizeSearch(
            $"{title} {subtitle} {group} {StripBbcode(detail)} {extraSearch}"
        );
        return new EncyclopediaEntry
        {
            Title = title ?? string.Empty,
            Subtitle = subtitle ?? string.Empty,
            Group = group ?? string.Empty,
            Detail = detail ?? string.Empty,
            SearchText = search,
            CharacterKey = characterKey,
            SkillId = skillId,
            RelicId = relicId,
            BuffName = buffName,
            SkillTypeFilter = skillTypeFilter,
            SkillRarity = skillRarity,
            SkillCostFilter = skillCostFilter,
            SkillCostSortValue = skillCostSortValue,
            IsStatusCard = isStatusCard,
        };
    }

    private Skill GetPreviewSkill(SkillID skillId)
    {
        if (_previewSkillCache.TryGetValue(skillId, out Skill cachedSkill) && cachedSkill != null)
            return cachedSkill;

        Skill skill = CreatePreviewSkill(skillId);
        if (skill != null)
            _previewSkillCache[skillId] = skill;
        return skill;
    }

    private static Skill CreatePreviewSkill(SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        skill?.SetPreviewStats(0, 0, 1);
        skill?.UpdateDescription();
        return skill;
    }

    private static string GetPlayerCharacterDisplayName(PlayerCharacterKey character)
    {
        return character switch
        {
            PlayerCharacterKey.Echo => I18n.Tr("character.echo.name", "Echo"),
            PlayerCharacterKey.Kasiya => I18n.Tr("character.kasiya.name", "Kasiya"),
            PlayerCharacterKey.Mariya => I18n.Tr("character.mariya.name", "Mariya"),
            PlayerCharacterKey.Nightingale => I18n.Tr("character.nightingale.name", "Nightingale"),
            _ => character.ToString(),
        };
    }

    private static SkillTypeFilter ToSkillTypeFilter(Skill.SkillTypes skillType, bool isStatusCard)
    {
        if (isStatusCard)
            return SkillTypeFilter.Status;

        return skillType switch
        {
            Skill.SkillTypes.Attack => SkillTypeFilter.Attack,
            Skill.SkillTypes.Survive => SkillTypeFilter.Survive,
            Skill.SkillTypes.Special => SkillTypeFilter.Special,
            Skill.SkillTypes.Ability => SkillTypeFilter.Ability,
            _ => SkillTypeFilter.Status,
        };
    }

    private static SkillCostFilter ToSkillCostFilter(Skill skill)
    {
        if (skill == null)
            return SkillCostFilter.All;

        if (!skill.CanBePlayed)
            return SkillCostFilter.X;

        if (skill.UsesXEnergyCost)
            return SkillCostFilter.X;

        return skill.CardEnergyCost switch
        {
            <= 0 => SkillCostFilter.Zero,
            1 => SkillCostFilter.One,
            2 => SkillCostFilter.Two,
            _ => SkillCostFilter.ThreePlus,
        };
    }

    private static int GetSkillCostSortValue(Skill skill)
    {
        if (skill == null)
            return int.MaxValue;

        if (!skill.CanBePlayed)
            return 100;

        if (skill.UsesXEnergyCost)
            return 99;

        return Math.Max(skill.CardEnergyCost, 0);
    }

    private static int GetSkillTypeSortOrder(SkillTypeFilter filter)
    {
        return filter switch
        {
            SkillTypeFilter.Attack => 0,
            SkillTypeFilter.Survive => 1,
            SkillTypeFilter.Special => 2,
            SkillTypeFilter.Ability => 3,
            SkillTypeFilter.Status => 4,
            _ => 5,
        };
    }

    private static int GetSkillRaritySortOrder(Skill.SkillRarity rarity)
    {
        return rarity switch
        {
            Skill.SkillRarity.Rare => 3,
            Skill.SkillRarity.Uncommon => 2,
            _ => 1,
        };
    }

    private static string GetRarityLabel(Skill.SkillRarity rarity)
    {
        return rarity switch
        {
            Skill.SkillRarity.Uncommon => I18n.Tr("ui.encyclopedia.rarity.uncommon", "罕见"),
            Skill.SkillRarity.Rare => I18n.Tr("ui.encyclopedia.rarity.rare", "稀有"),
            _ => I18n.Tr("ui.encyclopedia.rarity.common", "普通"),
        };
    }

    private static string NormalizeSearch(string text)
    {
        return (text ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string StripBbcode(string text)
    {
        return Regex.Replace(text ?? string.Empty, "\\[.*?\\]", string.Empty);
    }

    private static string EscapeBbcode(string text)
    {
        return (text ?? string.Empty).Replace("[", "[lb]").Replace("]", "[rb]");
    }

    private static Button CreateFilterButton(string text, bool wide = true)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(wide ? 0 : 76, 34),
            SizeFlagsHorizontal = wide ? SizeFlags.ExpandFill : SizeFlags.Fill,
            ClipText = true,
        };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateFilterButtonStyle(new Color(0.12f, 0.18f, 0.26f, 0.96f))
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateFilterButtonStyle(new Color(0.18f, 0.29f, 0.39f, 1f))
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateFilterButtonStyle(new Color(0.27f, 0.43f, 0.58f, 1f))
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateFilterButtonStyle(new Color(0.27f, 0.43f, 0.58f, 1f))
        );
        button.AddThemeColorOverride("font_color", new Color(0.93f, 0.97f, 1f, 0.92f));
        button.AddThemeColorOverride("font_disabled_color", new Color(1f, 1f, 1f, 1f));
        return button;
    }

    private static Button CreateButton(string text, Vector2 minSize)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = minSize,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
        };
        ApplyButtonTheme(button);
        return button;
    }

    private static void ApplyButtonTheme(Button button)
    {
        button.AddThemeFontSizeOverride("font_size", 20);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateButtonStyle(new Color(0.10f, 0.15f, 0.22f, 0.95f))
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateButtonStyle(new Color(0.16f, 0.25f, 0.34f, 1f))
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateButtonStyle(new Color(0.08f, 0.12f, 0.18f, 1f))
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateButtonStyle(new Color(0.22f, 0.34f, 0.43f, 1f))
        );
        button.AddThemeColorOverride("font_color", new Color(0.93f, 0.97f, 1f, 0.92f));
        button.AddThemeColorOverride("font_disabled_color", new Color(1f, 1f, 1f, 1f));
    }

    private static void ApplyOptionTheme(SettingsDropdown optionButton)
    {
        optionButton.AddThemeFontSizeOverride("font_size", 16);
        optionButton.AddThemeStyleboxOverride(
            "normal",
            CreateFilterButtonStyle(new Color(0.12f, 0.18f, 0.26f, 0.96f))
        );
        optionButton.AddThemeStyleboxOverride(
            "hover",
            CreateFilterButtonStyle(new Color(0.18f, 0.29f, 0.39f, 1f))
        );
        optionButton.AddThemeStyleboxOverride(
            "pressed",
            CreateFilterButtonStyle(new Color(0.27f, 0.43f, 0.58f, 1f))
        );
        optionButton.AddThemeColorOverride("font_color", new Color(0.93f, 0.97f, 1f, 0.92f));
    }

    private static void ApplySearchTheme(LineEdit searchBox)
    {
        if (searchBox == null)
            return;

        searchBox.ClearButtonEnabled = true;
        searchBox.AddThemeFontSizeOverride("font_size", 17);
        searchBox.AddThemeColorOverride(
            "font_color",
            new Color(0.9f, 0.96f, 1f, 0.96f)
        );
        searchBox.AddThemeColorOverride(
            "font_placeholder_color",
            new Color(0.54f, 0.66f, 0.75f, 0.8f)
        );
        searchBox.AddThemeColorOverride(
            "caret_color",
            new Color(0.38f, 0.9f, 0.96f, 1f)
        );
        searchBox.AddThemeStyleboxOverride("normal", CreateSearchBoxStyle(false));
        searchBox.AddThemeStyleboxOverride("focus", CreateSearchBoxStyle(true));
    }

    private int FindSkillSortOptionIndex(int itemId)
    {
        if (_skillSortOption == null)
            return -1;

        for (int i = 0; i < _skillSortOption.ItemCount; i++)
        {
            if (_skillSortOption.GetItemId(i) == itemId)
                return i;
        }

        return -1;
    }

    private static StyleBoxFlat CreateFilterButtonStyle(Color color)
    {
        return new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = new Color(0.68f, 0.84f, 0.98f, 0.22f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
    }

    private static StyleBoxFlat CreateSearchBoxStyle(bool focused)
    {
        Color accent = focused
            ? new Color(0.35f, 0.9f, 0.96f, 0.72f)
            : new Color(0.38f, 0.7f, 0.86f, 0.22f);
        return new StyleBoxFlat
        {
            BgColor = focused
                ? new Color(0.035f, 0.085f, 0.12f, 0.96f)
                : new Color(0.02f, 0.05f, 0.078f, 0.9f),
            BorderColor = accent,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
            ContentMarginLeft = 16,
            ContentMarginRight = 42,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = focused
                ? new Color(0.12f, 0.72f, 0.88f, 0.16f)
                : Colors.Transparent,
            ShadowSize = focused ? 7 : 0,
        };
    }

    private static Texture2D GetModuleIcon(EncyclopediaModule module)
    {
        return ModuleIconPaths.TryGetValue(module, out string path)
            ? GD.Load<Texture2D>(path)
            : null;
    }

    private static Color GetModuleAccent(EncyclopediaModule module)
    {
        return module switch
        {
            EncyclopediaModule.Skills => new Color(0.26f, 0.86f, 0.95f, 1f),
            EncyclopediaModule.Relics => new Color(0.96f, 0.7f, 0.3f, 1f),
            EncyclopediaModule.Buffs => new Color(0.52f, 0.86f, 0.68f, 1f),
            EncyclopediaModule.Enemies => new Color(0.94f, 0.4f, 0.48f, 1f),
            _ => new Color(0.62f, 0.67f, 0.96f, 1f),
        };
    }

    private static StyleBoxFlat CreateModuleBadgeStyle(Color accent)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(accent.R * 0.13f, accent.G * 0.13f, accent.B * 0.13f, 0.96f),
            BorderColor = accent with { A = 0.48f },
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 16,
            CornerRadiusTopRight = 16,
            CornerRadiusBottomLeft = 16,
            CornerRadiusBottomRight = 16,
            ContentMarginLeft = 17,
            ContentMarginRight = 17,
            ContentMarginTop = 17,
            ContentMarginBottom = 17,
            ShadowColor = accent with { A = 0.16f },
            ShadowSize = 9,
        };
    }

    private static StyleBoxFlat CreateHomeCardStyle(Color accent, float accentStrength)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(
                0.025f + accent.R * accentStrength,
                0.045f + accent.G * accentStrength,
                0.07f + accent.B * accentStrength,
                0.96f
            ),
            BorderColor = accent with { A = 0.38f + accentStrength },
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
            ShadowColor = accent with { A = 0.12f + accentStrength * 0.25f },
            ShadowSize = 10,
            ShadowOffset = new Vector2(0f, 4f),
        };
    }

    private static StyleBoxFlat CreateNavigationButtonStyle(Color accent, bool selected)
    {
        return new StyleBoxFlat
        {
            BgColor = selected
                ? new Color(
                    0.04f + accent.R * 0.16f,
                    0.07f + accent.G * 0.16f,
                    0.1f + accent.B * 0.16f,
                    1f
                )
                : new Color(0.035f, 0.065f, 0.095f, 0.82f),
            BorderColor = selected
                ? accent with { A = 0.66f }
                : new Color(0.4f, 0.68f, 0.82f, 0.14f),
            BorderWidthLeft = selected ? 3 : 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 13,
            CornerRadiusTopRight = 13,
            CornerRadiusBottomLeft = 13,
            CornerRadiusBottomRight = 13,
            ContentMarginLeft = 14,
            ContentMarginRight = 12,
            ContentMarginTop = 9,
            ContentMarginBottom = 9,
        };
    }

    private static StyleBoxFlat CreateButtonStyle(Color color)
    {
        return new StyleBoxFlat
        {
            BgColor = color,
            BorderColor = new Color(0.68f, 0.84f, 0.98f, 0.26f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 16,
            ContentMarginRight = 16,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.24f),
            ShadowSize = 4,
            ShadowOffset = new Vector2(0f, 2f),
        };
    }

}
