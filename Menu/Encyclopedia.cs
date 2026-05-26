using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

public partial class Encyclopedia : Control
{
    private const float SkillCardSwitchExitStagger = 0.012f;
    private const float SkillCardSwitchExitDuration = 0.3f;
    private const float SkillCardSwitchMaxExitTotalDuration = 0.4f;
    private const float SkillCardSwitchEnterStagger = 0.035f;
    private const float SkillCardSwitchEnterDuration = 0.4f;
    private const float SkillCardSwitchMaxTotalDuration = 0.5f;
    private const float InterfaceEnterDuration = 0.34f;
    private const float InterfaceExitDuration = 0.22f;
    private const float InterfaceEnterStagger = 0.035f;
    private const float InterfaceExitStagger = 0.018f;
    private const int SkillCardsBuildPerFrame = 4;
    private static readonly Vector2 EncyclopediaSkillCardDisplaySize = new(250f, 375f);
    private static readonly Vector2 EncyclopediaSkillCardHoverPadding = new(16f, 18f);

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
        public SkillTypeFilter SkillTypeFilter { get; init; }
        public Skill.SkillRarity SkillRarity { get; init; }
        public SkillCostFilter SkillCostFilter { get; init; }
        public int SkillCostSortValue { get; init; }
        public bool IsStatusCard { get; init; }
    }

    private static readonly Dictionary<EncyclopediaModule, string> ModuleNames = new()
    {
        [EncyclopediaModule.Buffs] = "Buff",
        [EncyclopediaModule.Skills] = I18n.Tr("ui.encyclopedia.module.skills", "人物技能图鉴"),
        [EncyclopediaModule.Relics] = I18n.Tr("ui.encyclopedia.module.relics", "遗物"),
        [EncyclopediaModule.Items] = I18n.Tr("ui.encyclopedia.module.items", "道具"),
        [EncyclopediaModule.Enemies] = I18n.Tr("ui.encyclopedia.module.enemies", "敌人"),
    };

    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );

    private readonly Dictionary<EncyclopediaModule, List<EncyclopediaEntry>> _entries = new();
    private readonly Dictionary<Button, EncyclopediaEntry> _buttonEntries = new();
    private readonly Dictionary<PlayerCharacterKey, Button> _characterFilterButtons = new();
    private readonly Dictionary<SkillTypeFilter, Button> _skillTypeFilterButtons = new();
    private readonly Dictionary<Skill.SkillRarity, Button> _skillRarityFilterButtons = new();
    private readonly Dictionary<SkillCostFilter, Button> _skillCostFilterButtons = new();
    private readonly Dictionary<EncyclopediaEntry, PanelContainer> _skillCardFrames = new();
    private readonly Random _skillAnimationRandom = new();

    private ColorRect _backdrop;
    private PanelContainer _centerPanel;
    private HBoxContainer _header;
    private PanelContainer _modulePanel;
    private VBoxContainer _contentRoot;
    private HBoxContainer _searchRow;
    private LineEdit _searchBox;
    private Label _moduleTitle;
    private Label _countLabel;
    private HBoxContainer _characterFilterRow;
    private RichTextLabel _detailLabel;
    private PanelContainer _detailPanel;
    private PanelContainer _skillGridPanel;
    private GridContainer _skillGrid;
    private VBoxContainer _skillFilterPanel;
    private SettingsDropdown _skillSortOption;
    private Button _allSkillRarityFilterButton;
    private CenterContainer _detailCardHost;
    private SkillCard _detailSkillCard;

    private EncyclopediaModule _currentModule = EncyclopediaModule.Skills;
    private PlayerCharacterKey _selectedSkillCharacter = PlayerCharacterKey.Echo;
    private SkillTypeFilter _selectedSkillTypeFilter = SkillTypeFilter.All;
    private Skill.SkillRarity? _selectedSkillRarityFilter;
    private SkillCostFilter _selectedSkillCostFilter = SkillCostFilter.All;
    private SkillSortMode _selectedSkillSortMode = SkillSortMode.Type;
    private EncyclopediaEntry _selectedEntry;
    private int _resultRefreshVersion;
    private Tween _interfaceTween;
    private bool _isClosing;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        BuildCatalog();
        BindUi();
        SelectModule(EncyclopediaModule.Skills);
        PrepareInterfaceEnterState();
        PlayEnterAnimationDeferred();
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
                return CreateEntry(name, nature, detail, buff.ToString());
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
                string description = string.IsNullOrWhiteSpace(skill.Description)
                    ? I18n.Tr("ui.encyclopedia.no_description", "暂无描述。")
                    : skill.Description;
                string detail =
                    $"[b]{EscapeBbcode(skill.SkillName)}[/b]\n"
                    + $"{EscapeBbcode(characterName)} · {EscapeBbcode(type)} · {EscapeBbcode(rarityText)}\n"
                    + I18n.Format(
                        "ui.encyclopedia.detail.cost_line",
                        "费用：{cost}\n",
                        ("cost", EscapeBbcode(costText))
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
                string count = Relic.FormatCountLabel(Relic.GetAcquireAmount(id));
                string subtitle = string.IsNullOrWhiteSpace(count)
                    ? I18n.Tr("ui.encyclopedia.relic.unique", "唯一遗物")
                    : I18n.Format(
                        "ui.encyclopedia.relic.count",
                        "获得数量：{count}",
                        ("count", count)
                    );
                string detail =
                    $"[b]{EscapeBbcode(relic.RelicName)}[/b]\n{EscapeBbcode(subtitle)}\nID: {id}\n\n{GlobalFunction.ColorizeNumbers(relic.RelicDescription)}";
                return CreateEntry(relic.RelicName, subtitle, detail, id.ToString());
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
                    "生命 {life} · 力量 {power} · 生存 {survivability} · 速度 {speed}",
                    ("life", regedit.MaxLife),
                    ("power", regedit.Power),
                    ("survivability", regedit.Survivability),
                    ("speed", regedit.Speed)
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

            skill.SetPreviewStats(regedit.Power, regedit.Survivability, 1);
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
        _currentModule = EncyclopediaModule.Skills;
        _selectedEntry = null;
        _moduleTitle.Text = I18n.Tr("ui.encyclopedia.title", "卡牌图鉴");
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
        _skillGridPanel.Visible = true;
        _skillFilterPanel.Visible = true;
        _detailCardHost.Visible = false;
        _detailPanel.Visible = false;
    }

    private void RefreshFilterStates()
    {
        if (_skillFilterPanel != null)
            _skillFilterPanel.Visible = true;

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

    private void AddFlatResults(IEnumerable<EncyclopediaEntry> entries) { }

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
            card?.CallDeferred(
                nameof(SkillCard.StartAnimation),
                GetSkillCardEnterDelay(i, entries.Count)
            );

            if ((i + 1) % SkillCardsBuildPerFrame == 0 && i + 1 < entries.Count)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private SkillCard AddSkillCard(EncyclopediaEntry entry)
    {
        var frame = new PanelContainer
        {
            CustomMinimumSize =
                EncyclopediaSkillCardDisplaySize + EncyclopediaSkillCardHoverPadding * 2f,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        frame.AddThemeStyleboxOverride("panel", CreateSkillTileFrameStyle(false));

        var cardMargin = new MarginContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        cardMargin.AddThemeConstantOverride(
            "margin_left",
            (int)EncyclopediaSkillCardHoverPadding.X
        );
        cardMargin.AddThemeConstantOverride("margin_top", (int)EncyclopediaSkillCardHoverPadding.Y);
        cardMargin.AddThemeConstantOverride(
            "margin_right",
            (int)EncyclopediaSkillCardHoverPadding.X
        );
        cardMargin.AddThemeConstantOverride(
            "margin_bottom",
            (int)EncyclopediaSkillCardHoverPadding.Y
        );

        Skill skill = entry.SkillId.HasValue ? CreatePreviewSkill(entry.SkillId.Value) : null;
        var card = SkillCardScene.Instantiate<SkillCard>();
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.Set("stretch", true);
        card.Set("stretch_shrink", 1);
        card.PreviewCharacterName = entry.Group;
        card.DisplayNameOverride = entry.Title;
        card.CustomMinimumSize = EncyclopediaSkillCardDisplaySize;
        card.Size = EncyclopediaSkillCardDisplaySize;
        card.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        card.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        card.SetSkill(skill);
        card.Button.Pressed += () => SelectEntry(entry);

        cardMargin.AddChild(card);
        frame.AddChild(cardMargin);
        _skillGrid.AddChild(frame);
        _skillCardFrames[entry] = frame;
        return card;
    }

    private void SelectEntry(EncyclopediaEntry entry)
    {
        _selectedEntry = entry;
        _detailLabel.Text =
            entry?.Detail ?? I18n.Tr("ui.encyclopedia.no_match", "没有找到匹配条目。");

        foreach (var pair in _buttonEntries)
        {
            Color color =
                pair.Value == _selectedEntry
                    ? new Color(0.74f, 0.9f, 1f, 1f)
                    : new Color(0.93f, 0.97f, 1f, 0.92f);
            pair.Key.AddThemeColorOverride("font_color", color);
        }

        foreach (var pair in _skillCardFrames)
            pair.Value.AddThemeStyleboxOverride("panel", CreateSkillTileFrameStyle(false));
    }

    private void RefreshModuleButtonStates() { }

    private void ClearResultNodes()
    {
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

        ShuffleSkillAnimationOrder(holdersToAnimate);
        for (int i = 0; i < holdersToAnimate.Count; i++)
        {
            var holder = holdersToAnimate[i];
            float delay = GetSkillCardExitDelay(i, holdersToAnimate.Count);
            var tween = holder.CreateTween();
            tween.TweenProperty(holder, "modulate:a", 0.0f, 0.10f).SetDelay(delay);

            if (TryGetSkillCardFromHolder(holder, out var card))
            {
                var cardTween = card.CreateTween();
                cardTween.TweenCallback(Callable.From(() => card.Vanish())).SetDelay(delay);
            }
        }

        float waitTime =
            SkillCardSwitchExitDuration
            + GetSkillCardExitDelay(holdersToAnimate.Count - 1, holdersToAnimate.Count);
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
        bool isStatusCard = false
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
            SkillTypeFilter = skillTypeFilter,
            SkillRarity = skillRarity,
            SkillCostFilter = skillCostFilter,
            SkillCostSortValue = skillCostSortValue,
            IsStatusCard = isStatusCard,
        };
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
            SkillTypeFilter.Status => 3,
            _ => 4,
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
            CustomMinimumSize = new Vector2(wide ? 0 : 76, 40),
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
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
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
        };
    }

    private static StyleBoxFlat CreateSkillTileFrameStyle(bool selected)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            BorderColor = new Color(0f, 0f, 0f, 0f),
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
    }
}
