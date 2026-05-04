using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

public partial class Encyclopedia : Control
{
    private enum EncyclopediaModule
    {
        Buffs,
        Skills,
        Relics,
        Items,
        Enemies,
    }

    private sealed class EncyclopediaEntry
    {
        public string Title { get; init; }
        public string Subtitle { get; init; }
        public string Group { get; init; }
        public string Detail { get; init; }
        public string SearchText { get; init; }
        public PlayerCharacterKey? CharacterKey { get; init; }
    }

    private static readonly Dictionary<EncyclopediaModule, string> ModuleNames = new()
    {
        [EncyclopediaModule.Buffs] = "Buff",
        [EncyclopediaModule.Skills] = "人物技能",
        [EncyclopediaModule.Relics] = "遗物",
        [EncyclopediaModule.Items] = "道具",
        [EncyclopediaModule.Enemies] = "敌人",
    };

    private readonly Dictionary<EncyclopediaModule, List<EncyclopediaEntry>> _entries = new();
    private readonly Dictionary<Button, EncyclopediaEntry> _buttonEntries = new();
    private readonly Dictionary<EncyclopediaModule, Button> _moduleButtonsByModule = new();
    private readonly Dictionary<PlayerCharacterKey, Button> _characterFilterButtons = new();
    private LineEdit _searchBox;
    private Label _moduleTitle;
    private Label _countLabel;
    private HBoxContainer _characterFilterRow;
    private VBoxContainer _resultList;
    private RichTextLabel _detailLabel;
    private EncyclopediaModule _currentModule = EncyclopediaModule.Buffs;
    private PlayerCharacterKey _selectedSkillCharacter = PlayerCharacterKey.Echo;
    private EncyclopediaEntry _selectedEntry;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        BuildCatalog();
        BindUi();
        SelectModule(EncyclopediaModule.Buffs);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            QueueFree();
        }
    }

    private void BindUi()
    {
        _moduleTitle = GetNode<Label>("CenterPanel/Margin/Root/Body/Content/SearchRow/ModuleTitle");
        _searchBox = GetNode<LineEdit>("CenterPanel/Margin/Root/Body/Content/SearchRow/SearchBox");
        _countLabel = GetNode<Label>("CenterPanel/Margin/Root/Body/Content/SearchRow/CountLabel");
        _characterFilterRow = GetNode<HBoxContainer>(
            "CenterPanel/Margin/Root/Body/Content/CharacterFilterRow"
        );
        _resultList = GetNode<VBoxContainer>(
            "CenterPanel/Margin/Root/Body/Content/Split/ListPanel/Margin/ListScroll/ResultList"
        );
        _detailLabel = GetNode<RichTextLabel>(
            "CenterPanel/Margin/Root/Body/Content/Split/DetailPanel/Margin/DetailLabel"
        );

        _searchBox.TextChanged += _ => RefreshResults();

        var closeButton = GetNode<Button>("CenterPanel/Margin/Root/Header/CloseButton");
        ApplyButtonTheme(closeButton);
        closeButton.Pressed += QueueFree;

        _moduleButtonsByModule.Clear();
        RegisterModuleButton(
            EncyclopediaModule.Buffs,
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/ModuleButtons/BuffButton"
        );
        RegisterModuleButton(
            EncyclopediaModule.Skills,
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/ModuleButtons/SkillsButton"
        );
        RegisterModuleButton(
            EncyclopediaModule.Relics,
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/ModuleButtons/RelicsButton"
        );
        RegisterModuleButton(
            EncyclopediaModule.Items,
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/ModuleButtons/ItemsButton"
        );
        RegisterModuleButton(
            EncyclopediaModule.Enemies,
            "CenterPanel/Margin/Root/Body/ModulePanel/Margin/ModuleButtons/EnemiesButton"
        );

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
    }

    private void RegisterModuleButton(EncyclopediaModule module, string nodePath)
    {
        var button = GetNode<Button>(nodePath);
        ApplyButtonTheme(button);
        button.Pressed += () => SelectModule(module);
        _moduleButtonsByModule[module] = button;
    }

    private void RegisterCharacterButton(PlayerCharacterKey character, string nodePath)
    {
        var button = GetNode<Button>(nodePath);
        ApplyButtonTheme(button);
        button.Pressed += () => SelectSkillCharacter(character);
        _characterFilterButtons[character] = button;
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
                string nature = Buff.IsDebuff(buff) ? "负面状态" : "正面状态";
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
            var skillEntries = new List<EncyclopediaEntry>();
            foreach (SkillID skillId in Skill.GetPlayerSkillPool(character))
            {
                Skill skill = Skill.GetSkill(skillId);
                if (skill == null)
                    continue;

                skill.SetPreviewStats(0, 0, 1);
                skill.UpdateDescription();

                string type = skill.SkillType.GetDescription();
                string description = string.IsNullOrWhiteSpace(skill.Description)
                    ? "暂无描述。"
                    : skill.Description;
                string detail =
                    $"[b]{EscapeBbcode(skill.SkillName)}[/b]\n{EscapeBbcode(characterName)} · {EscapeBbcode(type)}\nID: {skillId}\n\n{description}";
                skillEntries.Add(
                    CreateEntry(
                        skill.SkillName,
                        type,
                        detail,
                        skillId.ToString(),
                        characterName,
                        character
                    )
                );
            }

            entries.AddRange(skillEntries.OrderBy(entry => entry.Subtitle).ThenBy(entry => entry.Title));
        }

        return entries;
    }

    private List<EncyclopediaEntry> BuildRelicEntries()
    {
        return Enum.GetValues<RelicID>()
            .Where(id => id != RelicID.curse)
            .Select(id =>
            {
                Relic relic = Relic.Create(id);
                string count = Relic.FormatCountLabel(Relic.GetAcquireAmount(id));
                string subtitle = string.IsNullOrWhiteSpace(count) ? "唯一遗物" : $"获得数量：{count}";
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
                string subtitle = $"数值：{ConsumeItem.GetItemValue(id)}";
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
                string title = string.IsNullOrWhiteSpace(regedit.CharacterName)
                    ? regedit.GetType().Name.Replace("Regedit", string.Empty)
                    : regedit.CharacterName;
                string subtitle =
                    $"生命 {regedit.MaxLife} · 力量 {regedit.Power} · 生存 {regedit.Survivability} · 速度 {regedit.Speed}";
                string detail =
                    $"[b]{EscapeBbcode(title)}[/b]\n{EscapeBbcode(subtitle)}\n类型：{regedit.PType}\n\n"
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
        ];
    }

    private static string BuildPassiveDetail(EnemyRegedit regedit)
    {
        if (string.IsNullOrWhiteSpace(regedit.PassiveName) && string.IsNullOrWhiteSpace(regedit.PassiveDescription))
            return string.Empty;

        return $"[b]被动 · {EscapeBbcode(regedit.PassiveName)}[/b]\n{regedit.PassiveDescription}\n\n";
    }

    private static string BuildEnemySkillDetail(EnemyRegedit regedit)
    {
        var ids = regedit.SkillIDs ?? Array.Empty<SkillID>();
        if (ids.Length == 0)
            return string.Empty;

        var lines = new List<string> { "[b]技能[/b]" };
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
        _currentModule = module;
        _selectedEntry = null;
        _moduleTitle.Text = ModuleNames[module];
        _searchBox.Text = string.Empty;
        RefreshModuleButtonStates();
        RefreshCharacterFilter();
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

    private void RefreshResults()
    {
        foreach (Node child in _resultList.GetChildren())
            child.QueueFree();
        _buttonEntries.Clear();

        string query = NormalizeSearch(_searchBox.Text);
        var source = _entries.TryGetValue(_currentModule, out var entries)
            ? entries
            : new List<EncyclopediaEntry>();

        if (_currentModule == EncyclopediaModule.Skills)
        {
            source = source
                .Where(entry => entry.CharacterKey == _selectedSkillCharacter)
                .ToList();
        }

        var filtered = source
            .Where(entry => string.IsNullOrWhiteSpace(query) || entry.SearchText.Contains(query))
            .ToList();

        _countLabel.Text = $"{filtered.Count}/{source.Count}";

        if (_currentModule == EncyclopediaModule.Skills)
            AddGroupedSkillResults(filtered);
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
            pair.Value.Disabled = selected;
            pair.Value.Modulate = selected
                ? new Color(0.74f, 0.9f, 1f, 1f)
                : Colors.White;
        }
    }

    private void AddFlatResults(IEnumerable<EncyclopediaEntry> entries)
    {
        foreach (var entry in entries)
            AddEntryButton(entry);
    }

    private void AddGroupedSkillResults(IEnumerable<EncyclopediaEntry> entries)
    {
        foreach (var group in entries.GroupBy(entry => entry.Group ?? string.Empty))
        {
            if (!string.IsNullOrWhiteSpace(group.Key))
                _resultList.AddChild(CreateGroupHeader(group.Key));

            foreach (var entry in group)
                AddEntryButton(entry);
        }
    }

    private void AddEntryButton(EncyclopediaEntry entry)
    {
        var button = CreateEntryButton(entry);
        _buttonEntries[button] = entry;
        _resultList.AddChild(button);
    }

    private Button CreateEntryButton(EncyclopediaEntry entry)
    {
        var button = CreateButton($"{entry.Title}\n{entry.Subtitle}", new Vector2(0, 72));
        button.Alignment = HorizontalAlignment.Left;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.Pressed += () => SelectEntry(entry);
        return button;
    }

    private void SelectEntry(EncyclopediaEntry entry)
    {
        _selectedEntry = entry;
        _detailLabel.Text = entry?.Detail ?? "没有找到匹配条目。";

        foreach (var pair in _buttonEntries)
        {
            Color color = pair.Value == _selectedEntry
                ? new Color(0.74f, 0.9f, 1f, 1f)
                : new Color(0.93f, 0.97f, 1f, 0.92f);
            pair.Key.AddThemeColorOverride("font_color", color);
        }
    }

    private void RefreshModuleButtonStates()
    {
        foreach (var pair in _moduleButtonsByModule)
            pair.Value.Disabled = pair.Key == _currentModule;
    }

    private static EncyclopediaEntry CreateEntry(
        string title,
        string subtitle,
        string detail,
        string extraSearch = null,
        string group = null,
        PlayerCharacterKey? characterKey = null
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
        };
    }

    private static string GetPlayerCharacterDisplayName(PlayerCharacterKey character)
    {
        return character switch
        {
            PlayerCharacterKey.Echo => "Echo",
            PlayerCharacterKey.Kasiya => "Kasiya",
            PlayerCharacterKey.Mariya => "Mariya",
            PlayerCharacterKey.Nightingale => "Nightingale",
            _ => character.ToString(),
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
        return (text ?? string.Empty)
            .Replace("[", "[lb]")
            .Replace("]", "[rb]");
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

    private static Control CreateGroupHeader(string text)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 4);

        var label = new Label
        {
            Text = text,
            Modulate = new Color(0.76f, 0.88f, 0.98f, 0.92f),
        };
        label.AddThemeFontSizeOverride("font_size", 22);
        container.AddChild(label);

        container.AddChild(
            new ColorRect
            {
                CustomMinimumSize = new Vector2(0, 2),
                Color = new Color(0.28f, 0.45f, 0.62f, 0.72f),
            }
        );
        return container;
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
}
