using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public partial class GameStatistics : CanvasLayer
{
    private const int NodeRouteRows = 4;
    private const int NodeRouteColumns = 15;
    private const float NodeDiamondSize = 23f;
    private const float NodeDiamondWrapperSize = 36f;
    private const float NodeGridCellMinWidth = 44f;
    private const float NodeGridCellHeight = 36f;
    private const int NodeRegionRowSeparation = 14;
    private const float NodeHoverScale = 1.25f;
    private const float MinimumSkillColumnHeight = 170f;
    private const float SkillColumnFixedHeight = 62f;
    private const float SkillPillHeight = 36f;
    private const float SkillPillSeparation = 8f;
    private static readonly Vector2 SkillCardPreviewScale = new(1.08f, 1.08f);
    private static readonly Vector2 SkillCardBaseDisplaySize = new(240f, 370f);

    private static readonly PackedScene StatisticsScene = GD.Load<PackedScene>(
        "res://BeginGame/Statistics/GameStatistics.tscn"
    );
    private static readonly PackedScene RelicIconScene = GD.Load<PackedScene>(
        "res://Relic/RelicIcon.tscn"
    );
    private static readonly PackedScene SkillButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/SkillButton.tscn"
    );
    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );
    private static readonly PackedScene TipScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );

    private readonly Dictionary<Control, Tween> _hoverTweens = new();

    private ColorRect BG => field ??= GetNodeOrNull<ColorRect>("BG");
    private Control CenterPanel => field ??= GetNodeOrNull<Control>("CenterPanel");
    private Control FrameDecor => field ??= GetNodeOrNull<Control>("FrameDecor");
    private Control VisualContent =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Detail/Scroll/Content"
        );
    private Label DescriptionLabel =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Detail/Description");
    private Label EyebrowLabel =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Eyebrow");
    private Label TitleLabel =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Sidebar/Title");
    private Label RelicTitleLabel =>
        field ??=
            GetNodeOrNull<Label>(
                "CenterPanel/Detail/Scroll/Content/RelicSection/RelicMargin/RelicVBox/RelicTitle"
            );
    private Label EquipmentTitleLabel =>
        field ??=
            GetNodeOrNull<Label>(
                "CenterPanel/Detail/Scroll/Content/EquipmentSection/EquipmentMargin/EquipmentVBox/EquipmentTitle"
            );
    private Label CharacterSelectHeaderLabel =>
        field ??=
            GetNodeOrNull<Label>(
                "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectHeader"
            );
    private GridContainer SummaryRow =>
        field ??= GetNodeOrNull<GridContainer>(
            "CenterPanel/Detail/Scroll/Content/SummaryRow"
        );
    private VBoxContainer NodeRows =>
        field ??= GetNodeOrNull<VBoxContainer>(
            "CenterPanel/Detail/Scroll/Content/NodeSection/NodeMargin/NodeVBox/RouteScroll/NodeRows"
        );
    private HFlowContainer RelicGrid =>
        field ??= GetNodeOrNull<HFlowContainer>(
            "CenterPanel/Detail/Scroll/Content/RelicSection/RelicMargin/RelicVBox/RelicGrid"
        );
    private HFlowContainer EquipmentGrid =>
        field ??= GetNodeOrNull<HFlowContainer>(
            "CenterPanel/Detail/Scroll/Content/EquipmentSection/EquipmentMargin/EquipmentVBox/EquipmentGrid"
        );
    private Control EquipmentSection =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Detail/Scroll/Content/EquipmentSection"
        );
    private Control CharacterSelectorRoot =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/CharacterSwitch"
        );
    private Control CharacterSelectorThumb =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectThumb"
        );
    private Control CharacterSelectorFrame =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectFrame"
        );
    private HBoxContainer CharacterButtonList =>
        field ??= GetNodeOrNull<HBoxContainer>(
            "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectPanel/CharacterButtonList"
        );
    private HBoxContainer SkillColumns =>
        field ??= GetNodeOrNull<HBoxContainer>(
            "CenterPanel/Detail/Scroll/Content/SkillSection/SkillMargin/SkillVBox/SkillColumns"
        );
    private Button PreviousHistoryButton =>
        field ??= GetNodeOrNull<Button>("PreviousHistoryButton");
    private Button NextHistoryButton => field ??= GetNodeOrNull<Button>("NextHistoryButton");
    private Button SeedCopyButton => field ??= GetNodeOrNull<Button>("SeedCopyButton");
    private ExitButton ExitButton => field ??= GetNodeOrNull<ExitButton>("ExitButton");

    private readonly Skill.SkillTypes[] _skillTypes =
    [
        Skill.SkillTypes.Attack,
        Skill.SkillTypes.Survive,
        Skill.SkillTypes.Special,
        Skill.SkillTypes.Ability,
    ];

    private RunHistoryRecord _currentRecord;
    private int _selectedHistoryIndex = -1;
    private int _selectedCharacterIndex;
    private Tween _transitionTween;
    private Tween _characterSelectorTween;
    private Tween _historyPageTween;
    private bool _characterSelectorPositioned;
    private bool _isSwitchingHistoryPage;
    private int _characterSelectorLayoutRequestId;
    private Tip _tooltip;
    private Control _skillCardPreviewRoot;
    private ColorRect _skillCardPreviewDim;
    private SkillCard _skillCardPreview;
    private readonly List<Button> _characterButtons = new();

    public static GameStatistics Show(Node caller)
    {
        var root = caller?.GetTree()?.Root;
        if (root == null || StatisticsScene == null)
            return null;

        var existing = root.GetNodeOrNull<GameStatistics>("GameStatistics");
        if (existing != null)
        {
            existing.CallDeferred(nameof(Open));
            return existing;
        }

        var statistics = StatisticsScene.Instantiate<GameStatistics>();
        statistics.Name = "GameStatistics";
        statistics.Layer = 60;
        root.AddChild(statistics);
        statistics.CallDeferred(nameof(Open));
        return statistics;
    }

    public override void _Ready()
    {
        Visible = false;
        LocalizeStaticTexts();

        EnsureExitButtonAction();
        LocalizeArchiveChrome();

        if (CharacterSelectorRoot != null)
            CharacterSelectorRoot.Resized += SnapCharacterSelector;

        if (PreviousHistoryButton != null)
            PreviousHistoryButton.Pressed += ShowPreviousHistoryRecord;
        if (NextHistoryButton != null)
            NextHistoryButton.Pressed += ShowNextHistoryRecord;
        if (SeedCopyButton != null)
            SeedCopyButton.Pressed += CopyCurrentSeedToClipboard;
    }

    public override void _ExitTree()
    {
        _tooltip?.HideTooltip();
        HideSkillCardPreview();
        base._ExitTree();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible) return;
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (_skillCardPreviewRoot?.Visible == true) HideSkillCardPreview();
            else Close();
            GetViewport()?.SetInputAsHandled();
        }
    }

    public void Open()
    {
        EnsureExitButtonAction();
        ResetHistoryPageSwitchVisuals();
        SelectLatestHistoryRecord();
        BuildArchiveDirectory();
        RefreshVisualStatistics();
        Visible = true;
        PlayIntro();
        QueueCharacterSelectorSnapAfterLayout();
    }

    private void EnsureExitButtonAction()
    {
        if (ExitButton != null && !ExitButton.PressedActions.Contains(Close))
            ExitButton.PressedActions.Add(Close);
    }

    private void RefreshVisualStatistics()
    {
        var records = GetHistoryRecords();
        if (records.Count == 0)
        {
            _selectedHistoryIndex = -1;
            _currentRecord = null;
        }
        else
        {
            if (_selectedHistoryIndex < 0 || _selectedHistoryIndex >= records.Count)
                _selectedHistoryIndex = records.Count - 1;

            _currentRecord = records[_selectedHistoryIndex];
        }
        _selectedCharacterIndex = 0;

        RefreshSummary();
        RefreshNodeRoute();
        RefreshRelics();
        RefreshEquipments();
        ConfigureCharacterSelector();
        RefreshCharacterSkills();
        RefreshHistoryNavigationButtons();
        RefreshSeedCopyButton();
        RefreshArchiveChrome();
    }

    private void SelectLatestHistoryRecord()
    {
        var records = GetHistoryRecords();
        _selectedHistoryIndex = records.Count - 1;
    }

    private void ShowPreviousHistoryRecord()
    {
        SwitchHistoryRecord(-1);
    }

    private void ShowNextHistoryRecord()
    {
        SwitchHistoryRecord(1);
    }

    private void SwitchHistoryRecord(int direction)
    {
        if (_isSwitchingHistoryPage)
            return;

        var records = GetHistoryRecords();
        if (records.Count == 0)
            return;

        int targetIndex = Mathf.Clamp(_selectedHistoryIndex + direction, 0, records.Count - 1);
        if (targetIndex == _selectedHistoryIndex)
            return;

        HideTooltip();
        PlayHistoryPageSwitch(targetIndex, direction);
    }

    private void PlayHistoryPageSwitch(int targetIndex, int direction)
    {
        var content = VisualContent;
        if (content == null || !IsInsideTree())
        {
            _selectedHistoryIndex = targetIndex;
            RefreshVisualStatistics();
            return;
        }

        _historyPageTween?.Kill();
        _isSwitchingHistoryPage = true;
        SetHistoryNavigationButtonsEnabled(false);

        content.Modulate = content.Modulate with { A = 1f };

        _historyPageTween = CreateTween();
        _historyPageTween.SetEase(Tween.EaseType.In);
        _historyPageTween.SetTrans(Tween.TransitionType.Cubic);
        _historyPageTween.TweenProperty(content, "modulate:a", 0f, 0.12f);
        _historyPageTween.TweenCallback(
            Callable.From(() =>
            {
                _selectedHistoryIndex = targetIndex;
                RefreshVisualStatistics();
                SetHistoryNavigationButtonsEnabled(false);
                content.Modulate = content.Modulate with { A = 0f };
            })
        );
        _historyPageTween.SetEase(Tween.EaseType.Out);
        _historyPageTween.SetTrans(Tween.TransitionType.Cubic);
        _historyPageTween.TweenProperty(content, "modulate:a", 1f, 0.18f);
        _historyPageTween.Finished += () =>
        {
            if (content != null && GodotObject.IsInstanceValid(content))
            {
                content.Modulate = content.Modulate with { A = 1f };
            }

            _isSwitchingHistoryPage = false;
            RefreshHistoryNavigationButtons();
            SetHistoryNavigationButtonsEnabled(true);
        };
    }

    private void RefreshHistoryNavigationButtons()
    {
        var records = GetHistoryRecords();
        bool hasHistory = records.Count > 0 && _selectedHistoryIndex >= 0;

        if (PreviousHistoryButton != null)
            PreviousHistoryButton.Disabled = !hasHistory || _selectedHistoryIndex <= 0 || _isSwitchingHistoryPage;
        if (NextHistoryButton != null)
            NextHistoryButton.Disabled = !hasHistory || _selectedHistoryIndex >= records.Count - 1 || _isSwitchingHistoryPage;
    }

    private void RefreshSeedCopyButton()
    {
        if (SeedCopyButton == null)
            return;

        bool hasRecord = _currentRecord != null;
        SeedCopyButton.Visible = hasRecord;
        SeedCopyButton.Disabled = !hasRecord;
        SeedCopyButton.Text = hasRecord
            ? I18n.Format("ui.map.seed", "Seed: {value}", ("value", _currentRecord.Seed))
            : I18n.Tr("ui.statistics.seed_placeholder", "Seed: -");
    }

    private async void CopyCurrentSeedToClipboard()
    {
        if (_currentRecord == null || SeedCopyButton == null)
            return;

        string seedText = _currentRecord.Seed.ToString();
        DisplayServer.ClipboardSet(seedText);

        SeedCopyButton.Text = I18n.Format(
            "ui.statistics.seed_copied",
            "Copied: {value}",
            ("value", seedText)
        );
        await ToSignal(GetTree().CreateTimer(0.9f), SceneTreeTimer.SignalName.Timeout);

        if (GodotObject.IsInstanceValid(this))
            RefreshSeedCopyButton();
    }

    private void SetHistoryNavigationButtonsEnabled(bool enabled)
    {
        RefreshHistoryNavigationButtons();
        if (!enabled) {
            if (PreviousHistoryButton != null) PreviousHistoryButton.Disabled = true;
            if (NextHistoryButton != null) NextHistoryButton.Disabled = true;
        }
    }

    private void ResetHistoryPageSwitchVisuals()
    {
        _historyPageTween?.Kill();
        _isSwitchingHistoryPage = false;

        if (VisualContent != null)
        {
            VisualContent.Modulate = VisualContent.Modulate with { A = 1f };
        }

        SetHistoryNavigationButtonsEnabled(true);
    }

    private static List<RunHistoryRecord> GetHistoryRecords()
    {
        return GameInfo.RunHistoryRecords?.Where(record => record != null).ToList()
            ?? new List<RunHistoryRecord>();
    }

    private void LocalizeStaticTexts()
    {
        if (EyebrowLabel != null)
            EyebrowLabel.Text = I18n.Tr("ui.statistics.eyebrow", "RUN ARCHIVE");
        if (TitleLabel != null)
            TitleLabel.Text = I18n.Tr("ui.statistics.title", "历史记录");
        if (DescriptionLabel != null)
            DescriptionLabel.Text = I18n.Tr("ui.statistics.description", "节点记录");
        if (RelicTitleLabel != null)
            RelicTitleLabel.Text = I18n.Tr("ui.common.relics", "遗物");
        if (EquipmentTitleLabel != null)
            EquipmentTitleLabel.Text = I18n.Tr("ui.statistics.equipment", "装备");
        if (CharacterSelectHeaderLabel != null)
            CharacterSelectHeaderLabel.Text = I18n.Tr("ui.statistics.character_select", "人物选择");
        if (SeedCopyButton != null)
            SeedCopyButton.Text = I18n.Tr("ui.statistics.seed_placeholder", "Seed: -");
    }

    private void RefreshSummary() {
        ClearChildren(SummaryRow);
        if (_currentRecord == null) {
            DescriptionLabel.Text = I18n.Tr("ui.statistics.no_history_description", "暂无历史游戏记录。本局结束后会在这里留下路线、遗物和技能快照。");
            SummaryRow.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.no_records", "暂无记录")));
            return;
        }
        DescriptionLabel.Text = I18n.Tr("ui.archive.detail_hint", "每一次选择，都留下一条不同的轨迹。");
        AddArchiveMetric(I18n.Tr("ui.archive.result", "结果"),
            I18n.Tr(_currentRecord.Victory ? "ui.common.victory" : "ui.common.defeat", _currentRecord.Victory ? "胜利" : "战败"),
            _currentRecord.Victory ? new Color(0.68f, 0.83f, 0.78f) : new Color(0.86f, 0.73f, 0.72f));
        AddArchiveMetric(I18n.Tr("ui.archive.difficulty", "难度"), _currentRecord.Difficulty.ToString("00"));
        AddArchiveMetric(I18n.Tr("ui.archive.duration", "航行时长"), FormatArchiveDuration(_currentRecord.SessionPlaySeconds));
        AddArchiveMetric(I18n.Tr("ui.archive.nodes", "经过节点"), _currentRecord.NodesVisited.ToString());
        AddArchiveMetric(I18n.Tr("ui.archive.enemies", "击败敌人"), _currentRecord.EnemiesDefeated.ToString());
        AddArchiveMetric(I18n.Tr("ui.archive.elites", "击败精英"), _currentRecord.EliteDefeated.ToString());
        int bossNodes = _currentRecord.NodeRecords?.Count(record => record?.NodeType == LevelNode.LevelType.Boss) ?? 0;
        AddArchiveMetric("BOSS", bossNodes > _currentRecord.BossDefeated ? $"{_currentRecord.BossDefeated} / {bossNodes}" : _currentRecord.BossDefeated.ToString());
        AddArchiveMetric(I18n.Tr("ui.archive.coins", "获得电力币"), _currentRecord.ElectricityCoinGained.ToString());
        AddArchiveMetric(I18n.Tr("ui.common.relics", "遗物"), _currentRecord.RelicGained.ToString());
        AddArchiveMetric(I18n.Tr("ui.archive.talents", "天赋"), CountRunTalents(_currentRecord).ToString());
    }

    private void AddArchiveMetric(string caption, string value, Color? color = null) {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(150f, 66f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        column.AddChild(CreateLabel(caption, 14, new Color(0.64f, 0.70f, 0.75f)));
        var number = CreateLabel(value, 27, color ?? new Color(0.88f, 0.93f, 0.96f));
        column.AddChild(number);
        SummaryRow.AddChild(column);
    }

    private void AppendHistoryProgressToDescription()
    {
        int historyCount = GetHistoryRecords().Count;
        if (DescriptionLabel == null || historyCount <= 1 || _selectedHistoryIndex < 0)
            return;

        DescriptionLabel.Text += I18n.Format("ui.statistics.progress_suffix", " ({current}/{total})", ("current", _selectedHistoryIndex + 1), ("total", historyCount));
    }

    private void RefreshNodeRoute()
    {
        ClearChildren(NodeRows);
        if (NodeRows == null)
            return;

        NodeRows.AddThemeConstantOverride("separation", NodeRegionRowSeparation);

        var records =
            _currentRecord?.NodeRecords?.Where(record => record != null).ToList()
            ?? new List<LevelNodeCompletionRecord>();
        if (records.Count == 0)
        {
            NodeRows.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.no_node_records", "没有节点记录")));
            return;
        }

        var groups = records
            .GroupBy(record => record.MapLevel)
            .OrderBy(group => group.Key)
            .ToList();

        foreach (var group in groups)
        {
            var row = new HBoxContainer
            {
                CustomMinimumSize = new Vector2(0f, NodeGridCellHeight),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Alignment = BoxContainer.AlignmentMode.Begin,
            };
            row.AddThemeConstantOverride("separation", 14);

            var regionLabel = CreateLabel(I18n.Format("ui.statistics.region", "区域 {index}", ("index", group.Key + 1)), 17, new Color(0.78f, 0.8f, 0.86f, 0.92f));
            regionLabel.CustomMinimumSize = new Vector2(72f, NodeGridCellHeight);
            regionLabel.VerticalAlignment = VerticalAlignment.Center;
            row.AddChild(regionLabel);

            row.AddChild(CreateNodeRouteLine(group.OrderBy(record => record.CompletionOrder)));
            NodeRows.AddChild(row);
        }
    }

    private Control CreateNodeRouteLine(IEnumerable<LevelNodeCompletionRecord> records)
    {
        var line = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0f, NodeGridCellHeight),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Begin,
        };
        line.AddThemeConstantOverride("separation", 8);

        foreach (var record in records.Where(record => record != null))
        {
            if (line.GetChildCount() > 0) {
                line.AddChild(new ColorRect {
                    CustomMinimumSize = new Vector2(12f, 1f),
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    Color = new Color(0.73f, 0.80f, 0.84f, 0.25f),
                });
            }
            var cell = new CenterContainer
            {
                CustomMinimumSize = new Vector2(NodeGridCellMinWidth, NodeGridCellHeight),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            cell.AddChild(CreateNodeDiamond(record));
            line.AddChild(cell);
        }

        return line;
    }

    private Control CreateNodeDiamond(LevelNodeCompletionRecord record)
    {
        var wrapper = new Control
        {
            CustomMinimumSize = new Vector2(NodeDiamondWrapperSize, NodeDiamondWrapperSize),
            MouseFilter = Control.MouseFilterEnum.Pass,
        };

        var panel = new Panel
        {
            Size = new Vector2(NodeDiamondSize, NodeDiamondSize),
            Position = new Vector2(
                (NodeDiamondWrapperSize - NodeDiamondSize) * 0.5f,
                (NodeDiamondWrapperSize - NodeDiamondSize) * 0.5f
            ),
            PivotOffset = new Vector2(NodeDiamondSize, NodeDiamondSize) * 0.5f,
            Rotation = Mathf.Pi / 4f,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        Color color = GetNodeTypeColor(record.NodeType).Lerp(new Color(0.79f, 0.84f, 0.87f), 0.58f);
        if (IsIncompleteNodeRecord(record))
            color = color.Lerp(Colors.White, 0.28f);

        panel.AddThemeStyleboxOverride(
            "panel",
            CreateStyleBox(
                new Color(0.15f, 0.17f, 0.19f, 0.4f),
                IsIncompleteNodeRecord(record)
                    ? new Color(0.9f, 0.92f, 0.96f, 0.95f)
                    : color with { A = 0.90f },
                0,
                IsIncompleteNodeRecord(record) ? 2 : 1
            )
        );

        var innerDot = new ColorRect
        {
            Color = color with { A = 0.95f },
            Size = new Vector2(5f, 5f),
            Position = new Vector2((NodeDiamondSize - 5f) * 0.5f, (NodeDiamondSize - 5f) * 0.5f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddChild(innerDot);

        panel.MouseEntered += () =>
        {
            TweenHover(panel, NodeHoverScale);
            ShowTooltip(BuildNodeTooltip(record));
        };
        panel.MouseExited += () =>
        {
            TweenHover(panel, 1f);
            HideTooltip();
        };

        wrapper.AddChild(panel);
        return wrapper;
    }

    private void RefreshRelics()
    {
        ClearChildren(RelicGrid);
        if (RelicGrid == null)
            return;

        var relics =
            _currentRecord?.RelicRecords?.Where(record => record != null).ToList()
            ?? new List<RunHistoryRelicRecord>();
        if (relics.Count == 0)
        {
            RelicGrid.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.no_relics", "没有遗物")));
            return;
        }

        foreach (var record in relics)
            RelicGrid.AddChild(CreateRelicIcon(record));
    }

    private Control CreateRelicIcon(RunHistoryRelicRecord record)
    {
        var wrapper = new Control
        {
            CustomMinimumSize = new Vector2(58f, 58f),
            MouseFilter = Control.MouseFilterEnum.Pass,
        };

        Control icon = RelicIconScene?.Instantiate<Control>();
        if (icon == null)
        {
            icon = new ColorRect
            {
                Color = new Color(0.92f, 0.68f, 0.18f, 1f),
                CustomMinimumSize = new Vector2(50f, 50f),
            };
        }

        icon.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        icon.Position = new Vector2(4f, 4f);
        icon.Size = new Vector2(50f, 50f);
        icon.CustomMinimumSize = new Vector2(50f, 50f);
        icon.MouseFilter = Control.MouseFilterEnum.Stop;

        if (icon is ColorRect colorRect)
        {
            Relic.ApplyIconVisual(colorRect, record.RelicID);
        }

        var countLabel = icon.GetNodeOrNull<Label>("Label");
        if (countLabel != null)
            countLabel.Text = Relic.FormatCountLabel(record.Count);

        if (icon.GetNodeOrNull<Panel>("Panel") is { } border) {
            border.AddThemeStyleboxOverride("panel", CreateStyleBox(new Color(0, 0, 0, 0), new Color(0.76f, 0.82f, 0.87f, 0.5f), 0, 1));
        }
        string tooltip = BuildRelicTooltip(record);
        icon.MouseEntered += () =>
        {
            TweenHover(icon, 1.12f);
            ShowTooltip(tooltip);
        };
        icon.MouseExited += () =>
        {
            TweenHover(icon, 1f);
            HideTooltip();
        };

        wrapper.AddChild(icon);
        return wrapper;
    }    private void RefreshEquipments()
    {
        if (EquipmentSection != null)
            EquipmentSection.Visible = false;
        ClearChildren(EquipmentGrid);
    }
    private Control CreateEquipmentRecordLabel(RunHistoryEquipmentRecord record)
    {
        string count = record.Count > 1 ? $" x{record.Count}" : string.Empty;
        var label = CreateLabel(
            $"{record.DisplayName}{count}",
            18,
            new Color(0.90f, 0.94f, 1f, 0.92f)
        );
        label.CustomMinimumSize = new Vector2(150f, 30f);
        label.MouseFilter = Control.MouseFilterEnum.Stop;

        string tooltip = BuildEquipmentTooltip(record);
        label.MouseEntered += () =>
        {
            TweenHover(label, 1.06f);
            ShowTooltip(tooltip);
        };
        label.MouseExited += () =>
        {
            TweenHover(label, 1f);
            HideTooltip();
        };

        return label;
    }

    private void ConfigureCharacterSelector()
    {
        ClearChildren(CharacterButtonList);
        _characterButtons.Clear();
        _characterSelectorPositioned = false;

        var characters = GetCharacterRecords();
        int count = characters.Count;

        if (CharacterSelectorThumb != null)
            CharacterSelectorThumb.Visible = count > 0;

        if (CharacterButtonList == null)
            return;

        if (count == 0)
        {
            var empty = CreateLabel(I18n.Tr("ui.statistics.no_characters", "暂无角色"), 18, new Color(0.74f, 0.82f, 0.90f, 0.62f), HorizontalAlignment.Center);
            empty.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            CharacterButtonList.AddChild(empty);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var character = characters[i];
            var button = CreateCharacterButton(character, i);
            _characterButtons.Add(button);
            CharacterButtonList.AddChild(button);
        }

        _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, characters.Count - 1);
        UpdateCharacterButtonState(false);
        QueueCharacterSelectorSnapAfterLayout();
    }

    private Button CreateCharacterButton(RunHistoryCharacterSkillRecord character, int index)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0f, 46f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ToggleMode = true,
            Flat = true,
            FocusMode = Control.FocusModeEnum.All,
            Text = string.IsNullOrWhiteSpace(character?.CharacterName)
                ? I18n.Format("ui.common.character_n", "角色 {index}", ("index", index + 1))
                : character.CharacterName,
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.AddThemeColorOverride("font_color", new Color(0.58f, 0.66f, 0.78f, 0.85f));
        button.AddThemeColorOverride("font_hover_color", new Color(0.95f, 0.97f, 1f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.88f, 0.96f, 0.96f));
        button.AddThemeColorOverride("font_focus_color", new Color(0.95f, 0.97f, 1f));

        int capturedIndex = index;
        button.Pressed += () => SelectCharacter(capturedIndex);
        return button;
    }

    private void SelectCharacter(int characterIndex)
    {
        var characters = GetCharacterRecords();
        if (characterIndex < 0 || characterIndex >= characters.Count)
            return;

        if (_selectedCharacterIndex == characterIndex)
        {
            UpdateCharacterButtonState(true);
            return;
        }

        _selectedCharacterIndex = characterIndex;
        UpdateCharacterButtonState(true);
        RefreshCharacterSkills();
    }

    private void UpdateCharacterButtonState(bool animateSelector)
    {
        for (int i = 0; i < _characterButtons.Count; i++)
        {
            var button = _characterButtons[i];
            bool active = i == _selectedCharacterIndex;
            button.SetPressedNoSignal(active);
            button.Modulate = active ? Colors.White : new Color(0.85f, 0.89f, 0.93f, 0.85f);
        }

        UpdateCharacterSelectorPosition(animateSelector);
    }

    public void SnapCharacterSelector()
    {
        UpdateCharacterSelectorPosition(false);
    }

    private void QueueCharacterSelectorSnapAfterLayout()
    {
        if (!IsInsideTree())
            return;

        _characterSelectorLayoutRequestId++;
        SnapCharacterSelectorAfterLayout(_characterSelectorLayoutRequestId);
    }

    private async void SnapCharacterSelectorAfterLayout(int requestId)
    {
        SceneTree tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || requestId != _characterSelectorLayoutRequestId)
            return;

        tree = GetTree();
        if (tree == null)
            return;

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || requestId != _characterSelectorLayoutRequestId)
            return;

        SnapCharacterSelector();
    }

    private void UpdateCharacterSelectorPosition(bool animate)
    {
        var button = GetSelectedCharacterButton();
        if (button == null || CharacterSelectorThumb == null || CharacterSelectorRoot == null)
        {
            if (CharacterSelectorThumb != null)
                CharacterSelectorThumb.Visible = false;
            return;
        }

        Rect2 selectorRect = CharacterSelectorRoot.GetGlobalRect();
        Rect2 buttonRect = button.GetGlobalRect();
        Rect2 frameRect = CharacterSelectorFrame?.GetGlobalRect() ?? buttonRect;
        if (selectorRect.Size.X <= 0f || buttonRect.Size.X <= 0f || frameRect.Size.Y <= 0f)
            return;

        CharacterSelectorThumb.Visible = true;
        Vector2 targetPosition = new(
            buttonRect.Position.X - selectorRect.Position.X,
            frameRect.Position.Y - selectorRect.Position.Y
        );
        Vector2 targetSize = new(buttonRect.Size.X, frameRect.Size.Y);

        _characterSelectorTween?.Kill();
        if (animate && _characterSelectorPositioned)
        {
            _characterSelectorTween = CreateTween();
            _characterSelectorTween.SetParallel(true);
            _characterSelectorTween.SetEase(Tween.EaseType.Out);
            _characterSelectorTween.SetTrans(Tween.TransitionType.Cubic);
            _characterSelectorTween.TweenProperty(CharacterSelectorThumb, "position", targetPosition, 0.20f);
            _characterSelectorTween.TweenProperty(CharacterSelectorThumb, "size", targetSize, 0.20f);
        }
        else
        {
            CharacterSelectorThumb.Position = targetPosition;
            CharacterSelectorThumb.Size = targetSize;
        }

        _characterSelectorPositioned = true;
    }

    private Button GetSelectedCharacterButton()
    {
        if (_selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characterButtons.Count)
            return _characterButtons[_selectedCharacterIndex];

        return _characterButtons.Count > 0 ? _characterButtons[0] : null;
    }

    private void RefreshCharacterSkills()
    {
        HideSkillCardPreview();
        ClearChildren(SkillColumns);
        if (SkillColumns == null)
            return;

        float columnMinHeight = GetSkillColumnMinimumHeight();
        SkillColumns.CustomMinimumSize = new Vector2(0f, columnMinHeight);

        var characters = GetCharacterRecords();
        if (characters.Count == 0)
        {
            SkillColumns.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.no_skill_records", "没有技能记录")));
            return;
        }

        _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, characters.Count - 1);
        var character = characters[_selectedCharacterIndex];

        SkillColumns.AddChild(CreateCharacterSnapshotColumn(character, columnMinHeight));
        foreach (var skillType in _skillTypes)
            SkillColumns.AddChild(CreateSkillTypeColumn(character, skillType, columnMinHeight));
    }

    private Control CreateCharacterSnapshotColumn(
        RunHistoryCharacterSkillRecord character,
        float columnMinHeight
    )
    {
        var panel = CreateColumnCard(new Vector2(200f, columnMinHeight), out var column);
        column.AddThemeConstantOverride("separation", 6);

        var title = CreateLabel(
            I18n.Tr("ui.statistics.final_state", "最终状态"),
            21,
            new Color(0.85f, 0.90f, 0.93f),
            HorizontalAlignment.Center
        );
        title.CustomMinimumSize = new Vector2(0f, 26f);
        title.VerticalAlignment = VerticalAlignment.Center;
        column.AddChild(title);
        column.AddChild(CreateTitleUnderline(new Color(0.79f, 0.85f, 0.89f, 0.4f)));

        column.AddChild(CreateCharacterStatLine(I18n.Format("ui.statistics.life_line", "生命 {value}", ("value", character.MaxLife)), new Color(0.96f, 0.92f, 0.82f, 0.96f)));
        column.AddChild(CreateCharacterStatLine(I18n.Format("ui.statistics.power_line", "力量 {value}", ("value", character.Power)), new Color(1.00f, 0.56f, 0.42f, 0.96f)));
        column.AddChild(CreateCharacterStatLine(I18n.Format("ui.statistics.survivability_line", "生存 {value}", ("value", character.Survivability)), new Color(0.50f, 0.82f, 1.00f, 0.96f)));

        int talentCount = character.UnlockedTalentIds?.Count
            ?? character.UnlockedTalentNames?.Count
            ?? 0;
        column.AddChild(
            CreateCharacterStatLine(
                I18n.Format("ui.statistics.talent_line", "天赋 {count} / 剩余 {remaining}", ("count", talentCount), ("remaining", character.TalentPoints)),
                new Color(0.88f, 0.74f, 1.00f, 0.96f)
            )
        );

        string tooltip = BuildCharacterSnapshotTooltip(character);
        panel.MouseEntered += () =>
        {
            TweenHover(panel, 1.02f);
            ShowTooltip(tooltip);
        };
        panel.MouseExited += () =>
        {
            TweenHover(panel, 1f);
            HideTooltip();
        };

        return panel;
    }

    private PanelContainer CreateColumnCard(Vector2 minSize, out VBoxContainer column)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = minSize,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        var columnStyle = CreateStyleBox(new Color(0, 0, 0, 0), new Color(0.72f, 0.78f, 0.82f, 0.20f), 0, 0);
        columnStyle.BorderWidthLeft = 1;
        panel.AddThemeStyleboxOverride("panel", columnStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        column = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        margin.AddChild(column);
        return panel;
    }

    private static Control CreateTitleUnderline(Color color, float width = 40f)
    {
        var row = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var bar = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(width, 2f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddChild(bar);
        return row;
    }

    private static Label CreateCharacterStatLine(string text, Color color)
    {
        var label = CreateLabel(text, 17, color.Lerp(new Color(0.82f, 0.87f, 0.91f), 0.70f), HorizontalAlignment.Left);
        label.CustomMinimumSize = new Vector2(0f, 24f);
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    private Control CreateSkillTypeColumn(
        RunHistoryCharacterSkillRecord character,
        Skill.SkillTypes skillType,
        float columnMinHeight
    )
    {
        var panel = CreateColumnCard(new Vector2(0f, columnMinHeight), out var column);
        panel.MouseFilter = Control.MouseFilterEnum.Ignore;
        column.AddThemeConstantOverride("separation", 6);

        var typeRecord = character
            .SkillTypeRecords?.FirstOrDefault(record => record != null && record.SkillType == skillType);
        var skillNames = typeRecord?.SkillNames ?? new List<string>();
        var skillIds = typeRecord?.SkillIds ?? new List<SkillID>();
        int skillCount = CountSkillTypeEntries(skillNames, skillIds);

        Color typeColor = GetSkillTypeColor(skillType).Lerp(new Color(0.83f, 0.87f, 0.90f), 0.65f);
        var title = CreateLabel($"{GetSkillTypeLabel(skillType)} {skillCount}", 21, typeColor, HorizontalAlignment.Center);
        title.CustomMinimumSize = new Vector2(0f, 26f);
        title.VerticalAlignment = VerticalAlignment.Center;
        column.AddChild(title);
        column.AddChild(CreateTitleUnderline(typeColor with { A = 0.55f }));

        var flow = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        flow.AddThemeConstantOverride("h_separation", 10);
        flow.AddThemeConstantOverride("separation", 8);

        if (skillNames.Count == 0 && skillIds.Count == 0)
        {
            flow.AddChild(CreateEmptyLabel(I18n.Tr("ui.statistics.not_acquired", "未获得")));
        }
        else
        {
            foreach (var entry in BuildSkillDisplayEntries(skillNames, skillIds))
            {
                flow.AddChild(CreateSkillPill(character, skillType, entry.Name, entry.SkillId));
            }
        }

        column.AddChild(flow);
        return panel;
    }

    private float GetSkillColumnMinimumHeight()
    {
        int maximumSkillCount = 1;
        foreach (var character in GetCharacterRecords())
        {
            foreach (var skillType in _skillTypes)
            {
                var typeRecord = character
                    .SkillTypeRecords?
                    .FirstOrDefault(record => record != null && record.SkillType == skillType);
                int displayCount = BuildSkillDisplayEntries(
                    typeRecord?.SkillNames ?? new List<string>(),
                    typeRecord?.SkillIds ?? new List<SkillID>()
                ).Count;
                maximumSkillCount = Math.Max(maximumSkillCount, displayCount);
            }
        }

        float skillListHeight =
            maximumSkillCount * SkillPillHeight
            + (maximumSkillCount - 1) * SkillPillSeparation;
        return Math.Max(MinimumSkillColumnHeight, SkillColumnFixedHeight + skillListHeight);
    }

    private static int CountSkillTypeEntries(List<string> skillNames, List<SkillID> skillIds)
    {
        if (skillIds != null && skillIds.Count > 0)
            return skillIds.Count;

        return skillNames?.Count(name => !string.IsNullOrWhiteSpace(name)) ?? 0;
    }

    private static List<(string Name, SkillID? SkillId)> BuildSkillDisplayEntries(
        List<string> skillNames,
        List<SkillID> skillIds
    )
    {
        if (skillIds != null && skillIds.Count > 0)
        {
            return skillIds
                .Select(
                    (skillId, index) =>
                        new
                        {
                            SkillId = skillId,
                            Name = index < (skillNames?.Count ?? 0)
                                ? skillNames[index]
                                : GetSkillDisplayName(skillId),
                        }
                )
                .GroupBy(entry => entry.SkillId)
                .Select(group =>
                {
                    var first = group.First();
                    int count = group.Count();
                    string name = string.IsNullOrWhiteSpace(first.Name)
                        ? GetSkillDisplayName(first.SkillId)
                        : first.Name;
                    return (FormatCountedName(name, count), (SkillID?)first.SkillId);
                })
                .ToList();
        }

        return (skillNames ?? new List<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name)
            .Select(group => (FormatCountedName(group.Key, group.Count()), (SkillID?)null))
            .ToList();
    }

    private static string FormatCountedName(string name, int count) =>
        count > 1 ? $"{name} x{count}" : name;

    private Control CreateSkillPill(
        RunHistoryCharacterSkillRecord character,
        Skill.SkillTypes type,
        string skillName,
        SkillID? skillId
    )
    {
        Color typeColor = GetSkillTypeColor(type).Lerp(new Color(0.83f, 0.87f, 0.90f), 0.65f);

        var style = CreateStyleBox(
            new Color(0.78f, 0.84f, 0.88f, 0.025f),
            new Color(0.58f, 0.6f, 0.66f, 0.22f),
            0,
            1
        );
        style.BorderWidthLeft = 0;
        style.BorderWidthRight = 0;
        style.BorderWidthTop = 0;
        style.ContentMarginTop = 3f;
        style.ContentMarginBottom = 3f;
        style.ContentMarginRight = 10f;

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(150f, 36f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        var accent = new ColorRect
        {
            Color = typeColor with { A = 0.9f },
            CustomMinimumSize = new Vector2(2f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddChild(accent);

        var label = CreateLabel(
            string.IsNullOrWhiteSpace(skillName) ? I18n.Tr("ui.common.unknown_skill", "未知技能") : skillName,
            17,
            new Color(0.90f, 0.91f, 0.94f, 0.95f)
        );
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        row.AddChild(label);

        string tooltip = BuildSkillTooltip(type, skillName, skillId);
        panel.GuiInput += @event => OnSkillPillGuiInput(@event, character, skillName, skillId);
        panel.MouseEntered += () =>
        {
            TweenHover(panel, 1.04f);
            ShowTooltip(tooltip);
        };
        panel.MouseExited += () =>
        {
            TweenHover(panel, 1f);
            HideTooltip();
        };

        return panel;
    }

    private void OnSkillPillGuiInput(
        InputEvent @event,
        RunHistoryCharacterSkillRecord character,
        string displayName,
        SkillID? skillId
    )
    {
        if (
            @event is not InputEventMouseButton mouseButton
            || !mouseButton.Pressed
            || mouseButton.ButtonIndex != MouseButton.Right
            || !skillId.HasValue
        )
        {
            return;
        }

        GetViewport()?.SetInputAsHandled();
        ShowSkillCardPreview(skillId.Value, displayName, character);
    }

    private void ShowSkillCardPreview(
        SkillID skillId,
        string displayName,
        RunHistoryCharacterSkillRecord character
    )
    {
        var skill = Skill.GetSkill(skillId);
        if (skill == null)
            return;

        HideTooltip();
        EnsureSkillCardPreviewRoot();
        if (_skillCardPreviewRoot == null || SkillCardScene == null)
            return;

        if (_skillCardPreview != null && GodotObject.IsInstanceValid(_skillCardPreview))
        {
            _skillCardPreview.QueueFree();
            _skillCardPreview = null;
        }

        skill.SetPreviewStats(
            Math.Max(0, character?.Power ?? 0),
            Math.Max(0, character?.Survivability ?? 0),
            1
        );

        _skillCardPreview = SkillCardScene.Instantiate<SkillCard>();
        _skillCardPreview.Name = $"StatisticsSkillCard_{skillId}";
        _skillCardPreview.ConfigureDisplayScale(SkillCardPreviewScale);
        _skillCardPreview.AutoPressEffect = false;
        _skillCardPreview.UseDefaultHoverEffect = false;
        _skillCardPreview.MouseFilter = Control.MouseFilterEnum.Stop;
        _skillCardPreview.PreviewCharacterName = character?.CharacterName ?? string.Empty;
        _skillCardPreview.DisplayNameOverride = displayName;
        _skillCardPreviewRoot.AddChild(_skillCardPreview);
        _skillCardPreview.SetSkill(skill);
        _skillCardPreview.RestoreDisplayState();
        _skillCardPreview.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true })
                GetViewport()?.SetInputAsHandled();
        };

        _skillCardPreviewRoot.Visible = true;
        RefreshSkillCardPreviewRootBounds();
        _skillCardPreview.Position = GetCenteredSkillCardPreviewPosition();
        _skillCardPreviewRoot.MoveToFront();
        _skillCardPreviewDim.Modulate = new Color(0f, 0f, 0f, 0f);
        _skillCardPreview.Modulate = new Color(1f, 1f, 1f, 0f);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_skillCardPreviewDim, "modulate:a", 0.62f, 0.12f);
        tween.TweenProperty(_skillCardPreview, "modulate:a", 1f, 0.12f);
    }

    private void EnsureSkillCardPreviewRoot()
    {
        if (_skillCardPreviewRoot != null && GodotObject.IsInstanceValid(_skillCardPreviewRoot))
            return;

        _skillCardPreviewRoot = new Control
        {
            Name = "SkillCardPreviewRoot",
            TopLevel = true,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 500,
        };
        AddChild(_skillCardPreviewRoot);

        _skillCardPreviewDim = new ColorRect
        {
            Name = "Dim",
            Color = new Color(0f, 0f, 0f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _skillCardPreviewDim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _skillCardPreviewRoot.AddChild(_skillCardPreviewDim);
        _skillCardPreviewRoot.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true })
                HideSkillCardPreview();
        };
    }

    private void RefreshSkillCardPreviewRootBounds()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        _skillCardPreviewRoot.Position = Vector2.Zero;
        _skillCardPreviewRoot.Size = viewportSize;
    }

    private Vector2 GetCenteredSkillCardPreviewPosition()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 cardSize = SkillCardBaseDisplaySize * SkillCardPreviewScale;
        return (viewportSize - cardSize) * 0.5f;
    }

    private void HideSkillCardPreview()
    {
        if (_skillCardPreview != null && GodotObject.IsInstanceValid(_skillCardPreview))
        {
            _skillCardPreview.QueueFree();
            _skillCardPreview = null;
        }

        if (_skillCardPreviewRoot != null && GodotObject.IsInstanceValid(_skillCardPreviewRoot))
            _skillCardPreviewRoot.Visible = false;
    }

    private List<RunHistoryCharacterSkillRecord> GetCharacterRecords()
    {
        return _currentRecord
                ?.CharacterSkillRecords
                ?.Where(record => record != null)
                .ToList()
            ?? new List<RunHistoryCharacterSkillRecord>();
    }

    private static int CountRunTalents(RunHistoryRecord record)
    {
        return record
                ?.CharacterSkillRecords
                ?.Where(character => character != null)
                .Sum(character =>
                    character.UnlockedTalentIds?.Count
                    ?? character.UnlockedTalentNames?.Count
                    ?? 0
                )
            ?? 0;
    }

    private List<RunHistoryEquipmentRecord> GetEquipmentRecords()
    {
        var records =
            _currentRecord?.EquipmentRecords?.Where(record => record != null).ToList()
            ?? new List<RunHistoryEquipmentRecord>();
        if (records.Count > 0)
            return records;

        return BuildEquipmentRecordsFromNodeChanges();
    }

    private List<RunHistoryEquipmentRecord> BuildEquipmentRecordsFromNodeChanges()
    {
        var result = new List<RunHistoryEquipmentRecord>();
        var names = _currentRecord
                ?.NodeRecords
                ?.Where(record => record?.EquipmentChanges != null)
                .SelectMany(record => record.EquipmentChanges)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
            ?? Enumerable.Empty<string>();

        foreach (string name in names)
        {
            result.Add(
                new RunHistoryEquipmentRecord
                {
                    DisplayName = name,
                    Count = 1,
                }
            );
        }

        return result;
    }

    private string BuildNodeTooltip(LevelNodeCompletionRecord record)
    {
        if (record == null)
            return I18n.Tr("ui.common.unknown_node", "未知节点");

        var sb = new StringBuilder(256);
        string order = record.CompletionOrder > 0 ? record.CompletionOrder.ToString() : "?";
        string typeLabel = GetNodeTypeLabel(record.NodeType);
        if (IsIncompleteNodeRecord(record))
            typeLabel += I18n.Tr("ui.statistics.node_incomplete_suffix", "（未完成）");
        sb.Append($"[b]#{order} {typeLabel}[/b]");
        sb.Append(
            I18n.Format(
                "ui.statistics.node_location",
                "\n区域：{region}  坐标：{x},{y}",
                ("region", record.MapLevel + 1),
                ("x", record.Coordinate.X),
                ("y", record.Coordinate.Y)
            )
        );

        string summary = RemoveEquipmentLines(record.Summary);
        if (string.IsNullOrWhiteSpace(summary))
            summary = BuildFallbackNodeSummary(record);

        if (!string.IsNullOrWhiteSpace(summary))
            sb.Append($"\n{summary}");

        return ColorizeTooltip(sb.ToString());
    }

    private string BuildFallbackNodeSummary(LevelNodeCompletionRecord record)
    {
        var parts = new List<string>();
        if (record.EnemyNames != null && record.EnemyNames.Count > 0)
            parts.Add(I18n.Format("ui.statistics.node_enemies", "敌人：{value}", ("value", string.Join("，", record.EnemyNames))));
        if (record.ElectricityCoinChange != 0)
            parts.Add(I18n.Format("ui.statistics.node_coins", "电力币：{value}", ("value", FormatSigned(record.ElectricityCoinChange))));
        if (record.NodeType == LevelNode.LevelType.Rest)
        {
            var recoveryLines = record.PlayerLifeChanges
                ?.Where(change => change != null && change.Amount > 0)
                .Select(change => $"{change.CharacterName} {FormatSigned(change.Amount)}")
                .ToList();
            AppendJoined(parts, "角色恢复", recoveryLines);
        }
        else if (record.TransitionEnergyChange != 0)
            parts.Add(I18n.Format("ui.statistics.node_core_energy", "队伍生命：{value}", ("value", FormatSigned(record.TransitionEnergyChange))));
        AppendJoined(parts, I18n.Tr("ui.statistics.skills", "技能"), record.SkillChanges);
        AppendJoined(parts, I18n.Tr("ui.statistics.items", "道具"), record.GainedItems);
        AppendJoined(parts, I18n.Tr("ui.statistics.relics", "遗物"), record.RelicChanges);
        AppendJoined(parts, I18n.Tr("ui.statistics.notes", "备注"), record.Notes);
        return parts.Count == 0 ? I18n.Tr("ui.statistics.no_extra_record", "无额外记录") : string.Join("\n", parts);
    }

    private static string RemoveEquipmentLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return string.Join(
            "\n",
            text.Split('\n').Where(line => !line.Contains("装备", StringComparison.Ordinal))
        );
    }

    private string BuildRelicTooltip(RunHistoryRelicRecord record)
    {
        if (record == null)
            return I18n.Tr("ui.common.unknown_relic", "未知遗物");

        var relic = Relic.Create(record.RelicID);
        string name = string.IsNullOrWhiteSpace(record.RelicName)
            ? relic?.RelicName ?? record.RelicID.ToString()
            : record.RelicName;
        string count = record.Count > 1 ? $" x{record.Count}" : string.Empty;
        string description = relic?.RelicDescription ?? string.Empty;
        return ColorizeTooltip($"[b]{name}{count}[/b]\n{description}");
    }

    private string BuildSkillTooltip(Skill.SkillTypes type, string skillName, SkillID? skillId)
    {
        Skill skill = skillId.HasValue ? Skill.GetSkill(skillId.Value) : null;
        if (skill != null)
        {
            skill.UpdateDescription();
            string name = string.IsNullOrWhiteSpace(skill.SkillName) ? skillId.Value.ToString() : skill.SkillName;
            string description = string.IsNullOrWhiteSpace(skill.Description) ? "-" : skill.Description;
            return ColorizeTooltip($"[b]{name}[/b]  [color=#cccccc]({GetSkillTypeLabel(skill.SkillType)})[/color]\n{description}");
        }

        string fallbackName = string.IsNullOrWhiteSpace(skillName) ? I18n.Tr("ui.common.unknown_skill", "未知技能") : skillName;
        return ColorizeTooltip($"[b]{fallbackName}[/b]  [color=#cccccc]({GetSkillTypeLabel(type)})[/color]");
    }

    private string BuildCharacterSnapshotTooltip(RunHistoryCharacterSkillRecord character)
    {
        if (character == null)
            return I18n.Tr("ui.common.unknown_character", "未知角色");

        var sb = new StringBuilder(256);
        string name = string.IsNullOrWhiteSpace(character.CharacterName)
            ? I18n.Tr("ui.common.character", "角色")
            : character.CharacterName;
        int talentCount = character.UnlockedTalentIds?.Count
            ?? character.UnlockedTalentNames?.Count
            ?? 0;

        sb.Append(I18n.Format("ui.statistics.character_snapshot_title", "[b]{name}[/b]  [color=#cccccc](最终状态)[/color]", ("name", name)));
        sb.Append(
            I18n.Format(
                "ui.statistics.character_snapshot_stats",
                "\n生命 {life}  力量 {power}  生存 {survivability}",
                ("life", character.MaxLife),
                ("power", character.Power),
                ("survivability", character.Survivability)
            )
        );
        sb.Append(
            I18n.Format(
                "ui.statistics.character_snapshot_talent",
                "\n天赋：{count}  剩余点：{remaining}",
                ("count", talentCount),
                ("remaining", character.TalentPoints)
            )
        );

        var talentEffects = character.UnlockedTalentEffects ?? new List<string>();
        var talentNames = character.UnlockedTalentNames ?? new List<string>();

        if (talentEffects.Count > 0)
        {
            sb.Append(I18n.Tr("ui.statistics.unlocked_talents_header", "\n[hr]\n[b]已点亮天赋[/b]"));
            foreach (string talent in talentEffects.Where(value => !string.IsNullOrWhiteSpace(value)))
                sb.Append($"\n{talent}");
        }
        else if (talentNames.Count > 0)
        {
            sb.Append(I18n.Tr("ui.statistics.unlocked_talents_header", "\n[hr]\n[b]已点亮天赋[/b]"));
            foreach (string talent in talentNames.Where(value => !string.IsNullOrWhiteSpace(value)))
                sb.Append($"\n{talent}");
        }
        else
        {
            sb.Append(I18n.Tr("ui.statistics.no_unlocked_talents", "\n[hr]\n未点亮天赋"));
        }

        return ColorizeTooltip(sb.ToString());
    }

    private string BuildEquipmentTooltip(RunHistoryEquipmentRecord record)
    {
        if (record == null)
            return I18n.Tr("ui.common.unknown_equipment", "未知装备");

        var sb = new StringBuilder(160);
        string name = string.IsNullOrWhiteSpace(record.DisplayName)
            ? record.EquipmentName.ToString()
            : record.DisplayName;
        sb.Append($"[b]{name}[/b]");
        if (record.Count > 1)
            sb.Append($" x{record.Count}");
        if (!string.IsNullOrWhiteSpace(record.TypeLabel))
            sb.Append($"  [color=#cccccc]({record.TypeLabel})[/color]");

        var stats = BuildEquipmentStatsText(record);
        if (!string.IsNullOrWhiteSpace(stats))
            sb.Append($"\n{stats}");

        if (!string.IsNullOrWhiteSpace(record.Description))
            sb.Append($"\n{record.Description}");

        return ColorizeTooltip(sb.ToString());
    }

    private static string BuildEquipmentStatsText(RunHistoryEquipmentRecord record)
    {
        if (record == null)
            return string.Empty;

        var parts = new List<string>();
        AddEquipmentStat(parts, I18n.Tr("property.power", "力量"), record.Power);
        AddEquipmentStat(parts, I18n.Tr("property.survivability", "生存"), record.Survivability);
        AddEquipmentStat(parts, I18n.Tr("property.max_life", "生命上限"), record.MaxLife);
        return string.Join("  ", parts);
    }

    private static void AddEquipmentStat(List<string> parts, string label, int value)
    {
        if (parts == null || value == 0)
            return;

        parts.Add($"{label}{FormatSigned(value)}");
    }

    private void ShowTooltip(string text)
    {
        var tip = EnsureTooltip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(22f, 20f);
        tip.MinContentWidth = 320f;
        tip.SetText(text);
    }

    private void HideTooltip()
    {
        _tooltip?.HideTooltip();
    }

    private Tip EnsureTooltip()
    {
        if (_tooltip != null && GodotObject.IsInstanceValid(_tooltip))
            return _tooltip;

        if (TipScene == null)
            return null;

        _tooltip = TipScene.Instantiate<Tip>();
        _tooltip.Name = "StatisticsTip";
        _tooltip.FollowMouse = true;
        _tooltip.AnchorOffset = new Vector2(22f, 20f);
        AddChild(_tooltip);
        return _tooltip;
    }

    private static string ColorizeTooltip(string text)
    {
        text = GlobalFunction.ColorizeNumbers(text ?? string.Empty);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static void AppendJoined(List<string> parts, string label, List<string> values)
    {
        if (parts == null || values == null || values.Count == 0)
            return;

        var text = string.Join("，", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(text))
            parts.Add($"{label}：{text}");
    }

    private void TweenHover(Control target, float scale)
    {
        if (target == null || !target.IsInsideTree())
            return;

        if (_hoverTweens.TryGetValue(target, out Tween running) && GodotObject.IsInstanceValid(running))
            running.Kill();

        Tween tween = target.CreateTween();
        _hoverTweens[target] = tween;
        tween
            .TweenProperty(target, "scale", new Vector2(scale, scale), 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.Finished += () =>
        {
            if (_hoverTweens.TryGetValue(target, out Tween activeTween) && activeTween == tween)
                _hoverTweens.Remove(target);
        };
    }

    private static Label CreateEmptyLabel(string text)
    {
        var label = CreateLabel(text, 17, new Color(0.67f, 0.73f, 0.79f, 0.86f), HorizontalAlignment.Center);
        label.CustomMinimumSize = new Vector2(160f, 42f);
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    private static Label CreateLabel(
        string text,
        int fontSize,
        Color color,
        HorizontalAlignment alignment = HorizontalAlignment.Left
    )
    {
        var label = new Label
        {
            Text = text ?? string.Empty,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0.01f, 0.02f, 0.04f, 0.75f));
        label.AddThemeConstantOverride("outline_size", 0);
        return label;
    }

    private static StyleBoxFlat CreateStyleBox(
        Color bgColor,
        Color borderColor,
        int radius,
        int borderWidth = 1
    )
    {
        return new StyleBoxFlat
        {
            BgColor = bgColor,
            BorderColor = borderColor,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
        };
    }

    private static void ClearChildren(Node parent)
    {
        if (parent == null)
            return;

        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static Color GetNodeTypeColor(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => Colors.White,
            LevelNode.LevelType.Elite => new Color(1f, 0.1f, 0.1f, 1f),
            LevelNode.LevelType.Boss => new Color(0.6f, 0f, 0.9f, 1f),
            LevelNode.LevelType.Event => new Color(0f, 0.6f, 1f, 1f),
            LevelNode.LevelType.Shop => new Color(1f, 0.84f, 0.18f, 1f),
            LevelNode.LevelType.Rest => new Color(0.1f, 0.9f, 0.46f, 1f),
            LevelNode.LevelType.Treasure => new Color(1f, 0.69f, 0.13f, 1f),
            _ => new Color(0.70f, 0.75f, 0.82f, 1f),
        };
    }

    private static string GetNodeTypeLabel(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => I18n.Tr("ui.statistics.node_type.normal", "普通战斗"),
            LevelNode.LevelType.Elite => I18n.Tr("ui.statistics.node_type.elite", "精英战斗"),
            LevelNode.LevelType.Boss => I18n.Tr("ui.statistics.node_type.boss", "首领战斗"),
            LevelNode.LevelType.Event => I18n.Tr("ui.statistics.node_type.event", "事件"),
            LevelNode.LevelType.Shop => I18n.Tr("ui.statistics.node_type.shop", "商店"),
            LevelNode.LevelType.Rest => I18n.Tr("ui.statistics.node_type.rest", "休息"),
            LevelNode.LevelType.Treasure => I18n.Tr("ui.statistics.node_type.treasure", "宝箱"),
            _ => I18n.Tr("ui.common.unknown_node", "未知节点"),
        };
    }

    private static bool IsIncompleteNodeRecord(LevelNodeCompletionRecord record) =>
        record?.Notes?.Any(note =>
            !string.IsNullOrWhiteSpace(note) && note.Contains("本节点未完成", StringComparison.Ordinal)
        ) == true;

    private static Color GetSkillTypeColor(Skill.SkillTypes type)
    {
        return type switch
        {
            Skill.SkillTypes.Attack => new Color(1.00f, 0.48f, 0.36f, 1f),
            Skill.SkillTypes.Survive => new Color(0.42f, 0.78f, 1.00f, 1f),
            Skill.SkillTypes.Special => new Color(0.86f, 0.72f, 1.00f, 1f),
            Skill.SkillTypes.Ability => new Color(1.00f, 0.82f, 0.38f, 1f),
            _ => new Color(0.82f, 0.86f, 0.92f, 1f),
        };
    }

    private static string GetSkillTypeLabel(Skill.SkillTypes type)
    {
        return type switch
        {
            Skill.SkillTypes.Attack => I18n.Tr("skill_type.attack", "攻击"),
            Skill.SkillTypes.Survive => I18n.Tr("skill_type.survive", "生存"),
            Skill.SkillTypes.Special => I18n.Tr("skill_type.special", "特殊"),
            Skill.SkillTypes.Ability => I18n.Tr("skill_type.ability", "能力"),
            Skill.SkillTypes.Status => I18n.Tr("ui.encyclopedia.skill_type.status", "状态"),
            _ => I18n.Tr("ui.statistics.skill_type.other", "其它"),
        };
    }

    private static string GetSkillDisplayName(SkillID skillId)
    {
        var skill = Skill.GetSkill(skillId);
        return skill == null || string.IsNullOrWhiteSpace(skill.SkillName)
            ? skillId.ToString()
            : skill.SkillName;
    }

    private static string FormatSigned(int value) => value >= 0 ? $"+{value}" : value.ToString();

    private static string FormatRunDuration(long totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        if (I18n.IsEnglishLocale())
        {
            if (hours > 0)
                return $"{hours}h {minutes:00}m {seconds:00}s";
            if (minutes > 0)
                return $"{minutes}m {seconds:00}s";
            return $"{seconds}s";
        }

        if (hours > 0)
            return $"{hours}小时{minutes}分{seconds}秒";
        if (minutes > 0)
            return $"{minutes}分{seconds}秒";
        return $"{seconds}秒";
    }

    private void PlayIntro()
    {
        _transitionTween?.Kill();
        AnimateArchiveChrome(true);
        if (BG != null)
            BG.Modulate = BG.Modulate with { A = 0f };
        if (CenterPanel != null)
        {
            CenterPanel.Scale = Vector2.One;
            CenterPanel.Modulate = CenterPanel.Modulate with { A = 0f };
            CenterPanel.PivotOffset = CenterPanel.Size * 0.5f;
        }
        if (FrameDecor != null)
        {
            FrameDecor.Scale = new Vector2(0.96f, 0.96f);
            FrameDecor.Modulate = FrameDecor.Modulate with { A = 0f };
            FrameDecor.PivotOffset = FrameDecor.Size * 0.5f;
        }

        _transitionTween = CreateTween();
        _transitionTween.SetParallel(true);
        _transitionTween.SetEase(Tween.EaseType.Out);
        _transitionTween.SetTrans(Tween.TransitionType.Cubic);
        if (BG != null)
            _transitionTween.TweenProperty(BG, "modulate:a", 1f, 0.18f);
        if (CenterPanel != null)
        {
            _transitionTween.TweenProperty(CenterPanel, "scale", Vector2.One, 0.22f);
            _transitionTween.TweenProperty(CenterPanel, "modulate:a", 1f, 0.18f);
        }
        if (FrameDecor != null)
        {
            _transitionTween.TweenProperty(FrameDecor, "scale", Vector2.One, 0.22f);
            _transitionTween.TweenProperty(FrameDecor, "modulate:a", 1f, 0.18f);
        }
    }

    private void Close()
    {
        HideTooltip();
        HideSkillCardPreview();
        AnimateArchiveChrome(false);
        _transitionTween?.Kill();
        _characterSelectorTween?.Kill();
        _historyPageTween?.Kill();
        _isSwitchingHistoryPage = false;
        if (!IsInsideTree())
        {
            QueueFree();
            return;
        }

        _transitionTween = CreateTween();
        _transitionTween.SetParallel(true);
        _transitionTween.SetEase(Tween.EaseType.In);
        _transitionTween.SetTrans(Tween.TransitionType.Quad);
        if (BG != null)
            _transitionTween.TweenProperty(BG, "modulate:a", 0f, 0.14f);
        if (CenterPanel != null)
        {
            _transitionTween.TweenProperty(CenterPanel, "scale", Vector2.One, 0.14f);
            _transitionTween.TweenProperty(CenterPanel, "modulate:a", 0f, 0.14f);
        }
        if (FrameDecor != null)
        {
            _transitionTween.TweenProperty(FrameDecor, "scale", new Vector2(0.98f, 0.98f), 0.14f);
            _transitionTween.TweenProperty(FrameDecor, "modulate:a", 0f, 0.14f);
        }
        _transitionTween.Finished += QueueFree;
    }
}
