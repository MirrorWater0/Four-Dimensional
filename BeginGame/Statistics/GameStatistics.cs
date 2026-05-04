using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

public partial class GameStatistics : CanvasLayer
{
    private const int NodeRouteRows = 4;
    private const int NodeRouteColumns = 15;
    private const float NodeDiamondSize = 30f;
    private const float NodeDiamondWrapperSize = 36f;
    private const float NodeGridCellMinWidth = 44f;
    private const float NodeGridCellHeight = 36f;
    private const float NodeHoverScale = 1.25f;
    private const float HistoryPageSwitchOffset = 64f;

    private static readonly PackedScene StatisticsScene = GD.Load<PackedScene>(
        "res://BeginGame/Statistics/GameStatistics.tscn"
    );
    private static readonly PackedScene RelicIconScene = GD.Load<PackedScene>(
        "res://Relic/RelicIcon.tscn"
    );
    private static readonly PackedScene SkillButtonScene = GD.Load<PackedScene>(
        "res://battle/UIScene/SkillButton.tscn"
    );
    private static readonly PackedScene TipScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );

    private ColorRect BG => field ??= GetNodeOrNull<ColorRect>("BG");
    private Control CenterPanel => field ??= GetNodeOrNull<Control>("CenterPanel");
    private Control VisualContent =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent"
        );
    private Label DescriptionLabel =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/Description");
    private HBoxContainer SummaryRow =>
        field ??= GetNodeOrNull<HBoxContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SummaryRow"
        );
    private VBoxContainer NodeRows =>
        field ??= GetNodeOrNull<VBoxContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/NodeSection/NodeMargin/NodeRows"
        );
    private HFlowContainer RelicGrid =>
        field ??= GetNodeOrNull<HFlowContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/RelicSection/RelicMargin/RelicVBox/RelicGrid"
        );
    private HFlowContainer EquipmentGrid =>
        field ??= GetNodeOrNull<HFlowContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/EquipmentSection/EquipmentMargin/EquipmentVBox/EquipmentGrid"
        );
    private Control CharacterSelectorRoot =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SkillSection/SkillMargin/SkillVBox/CharacterSwitch"
        );
    private Control CharacterSelectorThumb =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectThumb"
        );
    private Control CharacterSelectorFrame =>
        field ??= GetNodeOrNull<Control>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectFrame"
        );
    private HBoxContainer CharacterButtonList =>
        field ??= GetNodeOrNull<HBoxContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectPanel/CharacterButtonList"
        );
    private HBoxContainer SkillColumns =>
        field ??= GetNodeOrNull<HBoxContainer>(
            "CenterPanel/Margin/VBox/MainVisualBox/StatisticsMargin/VisualContent/SkillSection/SkillMargin/SkillVBox/SkillColumns"
        );
    private Button PreviousHistoryButton =>
        field ??= GetNodeOrNull<Button>("PreviousHistoryButton");
    private Button NextHistoryButton => field ??= GetNodeOrNull<Button>("NextHistoryButton");
    private ExitButton ExitButton => field ??= GetNodeOrNull<ExitButton>("ExitButton");

    private readonly Skill.SkillTypes[] _skillTypes =
    [
        Skill.SkillTypes.Attack,
        Skill.SkillTypes.Survive,
        Skill.SkillTypes.Special,
    ];

    private RunHistoryRecord _currentRecord;
    private int _selectedHistoryIndex = -1;
    private int _selectedCharacterIndex;
    private Tween _transitionTween;
    private Tween _characterSelectorTween;
    private Tween _historyPageTween;
    private bool _characterSelectorPositioned;
    private bool _isSwitchingHistoryPage;
    private Tip _tooltip;
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

        EnsureExitButtonAction();

        if (CharacterSelectorRoot != null)
            CharacterSelectorRoot.Resized += SnapCharacterSelector;

        if (PreviousHistoryButton != null)
            PreviousHistoryButton.Pressed += ShowPreviousHistoryRecord;
        if (NextHistoryButton != null)
            NextHistoryButton.Pressed += ShowNextHistoryRecord;
    }

    public override void _ExitTree()
    {
        _tooltip?.HideTooltip();
        base._ExitTree();
    }

    public void Open()
    {
        EnsureExitButtonAction();
        ResetHistoryPageSwitchVisuals();
        SelectLatestHistoryRecord();
        RefreshVisualStatistics();
        Visible = true;
        PlayIntro();
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

        Vector2 basePosition = content.Position;
        Vector2 exitPosition = basePosition + new Vector2(-direction * HistoryPageSwitchOffset, 0f);
        Vector2 enterPosition = basePosition + new Vector2(direction * HistoryPageSwitchOffset, 0f);
        content.Modulate = content.Modulate with { A = 1f };
        content.Position = basePosition;

        _historyPageTween = CreateTween();
        _historyPageTween.SetEase(Tween.EaseType.In);
        _historyPageTween.SetTrans(Tween.TransitionType.Cubic);
        _historyPageTween.TweenProperty(content, "position", exitPosition, 0.12f);
        _historyPageTween.Parallel().TweenProperty(content, "modulate:a", 0f, 0.10f);
        _historyPageTween.TweenCallback(
            Callable.From(() =>
            {
                _selectedHistoryIndex = targetIndex;
                RefreshVisualStatistics();
                SetHistoryNavigationButtonsEnabled(false);
                content.Position = enterPosition;
                content.Modulate = content.Modulate with { A = 0f };
            })
        );
        _historyPageTween.SetEase(Tween.EaseType.Out);
        _historyPageTween.SetTrans(Tween.TransitionType.Cubic);
        _historyPageTween.TweenProperty(content, "position", basePosition, 0.18f);
        _historyPageTween.Parallel().TweenProperty(content, "modulate:a", 1f, 0.16f);
        _historyPageTween.Finished += () =>
        {
            if (content != null && GodotObject.IsInstanceValid(content))
            {
                content.Position = basePosition;
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
            PreviousHistoryButton.Visible = hasHistory && _selectedHistoryIndex > 0;
        if (NextHistoryButton != null)
            NextHistoryButton.Visible = hasHistory && _selectedHistoryIndex < records.Count - 1;
    }

    private void SetHistoryNavigationButtonsEnabled(bool enabled)
    {
        if (PreviousHistoryButton != null)
            PreviousHistoryButton.Disabled = !enabled;
        if (NextHistoryButton != null)
            NextHistoryButton.Disabled = !enabled;
    }

    private void ResetHistoryPageSwitchVisuals()
    {
        _historyPageTween?.Kill();
        _isSwitchingHistoryPage = false;

        if (VisualContent != null)
        {
            VisualContent.Position = Vector2.Zero;
            VisualContent.Modulate = VisualContent.Modulate with { A = 1f };
        }

        SetHistoryNavigationButtonsEnabled(true);
    }

    private static List<RunHistoryRecord> GetHistoryRecords()
    {
        return GameInfo.RunHistoryRecords?.Where(record => record != null).ToList()
            ?? new List<RunHistoryRecord>();
    }

    private void RefreshSummary()
    {
        ClearChildren(SummaryRow);

        if (_currentRecord == null)
        {
            if (DescriptionLabel != null)
                DescriptionLabel.Text = "暂无历史游戏记录。本局结束后会在这里留下路线、遗物和技能快照。";

            AddSummaryChip("暂无记录", Colors.White, new Color(0.20f, 0.25f, 0.32f, 0.88f));
            return;
        }

        if (DescriptionLabel != null)
        {
            DescriptionLabel.Text =
                $"第 {_currentRecord.RunIndex} 局 · {(_currentRecord.Victory ? "胜利" : "战败")} · 鼠标移到节点、遗物或技能上查看细节";
        }

        AppendHistoryProgressToDescription();

        AddSummaryChip(
            _currentRecord.Victory ? "胜利" : "战败",
            Colors.White,
            _currentRecord.Victory
                ? new Color(0.12f, 0.42f, 0.30f, 0.92f)
                : new Color(0.48f, 0.18f, 0.16f, 0.92f)
        );
        AddSummaryChip($"节点 {_currentRecord.NodesVisited}", Colors.White, new Color(0.12f, 0.19f, 0.28f, 0.92f));
        AddSummaryChip($"敌人 {_currentRecord.EnemiesDefeated}", Colors.White, new Color(0.28f, 0.16f, 0.14f, 0.92f));
        AddSummaryChip($"精英 {_currentRecord.EliteDefeated}", Colors.White, new Color(0.30f, 0.22f, 0.12f, 0.92f));
        AddSummaryChip($"Boss {_currentRecord.BossDefeated}", Colors.White, new Color(0.34f, 0.14f, 0.26f, 0.92f));
        AddSummaryChip($"电力币 {_currentRecord.ElectricityCoinGained}", Colors.White, new Color(0.18f, 0.28f, 0.40f, 0.92f));
        AddSummaryChip($"装备 {_currentRecord.EquipmentGained}", Colors.White, new Color(0.23f, 0.24f, 0.34f, 0.92f));
        AddSummaryChip($"遗物 {_currentRecord.RelicGained}", Colors.White, new Color(0.32f, 0.24f, 0.12f, 0.92f));
    }

    private void AddSummaryChip(string text, Color fontColor, Color bgColor)
    {
        if (SummaryRow == null)
            return;

        var label = CreateLabel(text, 19, fontColor, HorizontalAlignment.Center);
        label.CustomMinimumSize = new Vector2(104f, 34f);
        label.VerticalAlignment = VerticalAlignment.Center;
        SummaryRow.AddChild(label);
    }

    private void AppendHistoryProgressToDescription()
    {
        int historyCount = GetHistoryRecords().Count;
        if (DescriptionLabel == null || historyCount <= 1 || _selectedHistoryIndex < 0)
            return;

        DescriptionLabel.Text += $" ({_selectedHistoryIndex + 1}/{historyCount})";
    }

    private void RefreshNodeRoute()
    {
        ClearChildren(NodeRows);
        if (NodeRows == null)
            return;

        var records =
            _currentRecord?.NodeRecords?.Where(record => record != null).ToList()
            ?? new List<LevelNodeCompletionRecord>();
        if (records.Count == 0)
        {
            NodeRows.AddChild(CreateEmptyLabel("没有节点记录"));
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

            var regionLabel = CreateLabel($"区域 {group.Key + 1}", 18, new Color(0.72f, 0.84f, 0.94f, 0.84f));
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

        Color color = GetNodeTypeColor(record.NodeType);
        panel.AddThemeStyleboxOverride(
            "panel",
            CreateStyleBox(color, new Color(1f, 1f, 1f, 0.66f), 4, 2)
        );

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
            RelicGrid.AddChild(CreateEmptyLabel("没有遗物"));
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
            string shaderPath = Relic.GetIconShaderPath(record.RelicID);
            var shader = string.IsNullOrWhiteSpace(shaderPath) ? null : GD.Load<Shader>(shaderPath);
            if (shader != null)
                colorRect.Material = new ShaderMaterial { Shader = shader };
        }

        var countLabel = icon.GetNodeOrNull<Label>("Label");
        if (countLabel != null)
            countLabel.Text = Relic.FormatCountLabel(record.Count);

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
    }

    private void RefreshEquipments()
    {
        ClearChildren(EquipmentGrid);
        if (EquipmentGrid == null)
            return;

        var records = GetEquipmentRecords();
        if (records.Count == 0)
        {
            EquipmentGrid.AddChild(CreateEmptyLabel("没有装备记录"));
            return;
        }

        foreach (var record in records)
            EquipmentGrid.AddChild(CreateEquipmentRecordLabel(record));
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
            var empty = CreateLabel("暂无角色", 18, new Color(0.74f, 0.82f, 0.90f, 0.62f), HorizontalAlignment.Center);
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
        CallDeferred(nameof(SnapCharacterSelector));
    }

    private Button CreateCharacterButton(RunHistoryCharacterSkillRecord character, int index)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(0f, 35f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ToggleMode = true,
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            Text = string.IsNullOrWhiteSpace(character?.CharacterName)
                ? $"角色 {index + 1}"
                : character.CharacterName,
        };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.AddThemeColorOverride("font_color", new Color(0.82f, 0.86f, 0.94f, 0.72f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", Colors.White);

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
            button.Modulate = active ? Colors.White : new Color(0.82f, 0.86f, 0.94f, 0.68f);
        }

        UpdateCharacterSelectorPosition(animateSelector);
    }

    public void SnapCharacterSelector()
    {
        UpdateCharacterSelectorPosition(false);
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
        ClearChildren(SkillColumns);
        if (SkillColumns == null)
            return;

        var characters = GetCharacterRecords();
        if (characters.Count == 0)
        {
            SkillColumns.AddChild(CreateEmptyLabel("没有技能记录"));
            return;
        }

        _selectedCharacterIndex = Mathf.Clamp(_selectedCharacterIndex, 0, characters.Count - 1);
        var character = characters[_selectedCharacterIndex];

        foreach (var skillType in _skillTypes)
            SkillColumns.AddChild(CreateSkillTypeColumn(character, skillType));
    }

    private Control CreateSkillTypeColumn(
        RunHistoryCharacterSkillRecord character,
        Skill.SkillTypes skillType
    )
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(0f, 170f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 8);

        var title = CreateLabel(GetSkillTypeLabel(skillType), 22, GetSkillTypeColor(skillType), HorizontalAlignment.Center);
        title.CustomMinimumSize = new Vector2(0f, 28f);
        title.VerticalAlignment = VerticalAlignment.Center;
        column.AddChild(title);

        var flow = new HFlowContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        flow.AddThemeConstantOverride("h_separation", 10);
        flow.AddThemeConstantOverride("v_separation", 8);

        var typeRecord = character
            .SkillTypeRecords?.FirstOrDefault(record => record != null && record.SkillType == skillType);
        var skillNames = typeRecord?.SkillNames ?? new List<string>();
        var skillIds = typeRecord?.SkillIds ?? new List<SkillID>();

        if (skillNames.Count == 0 && skillIds.Count == 0)
        {
            flow.AddChild(CreateEmptyLabel("未获得"));
        }
        else
        {
            int count = Math.Max(skillNames.Count, skillIds.Count);
            for (int i = 0; i < count; i++)
            {
                string skillName = i < skillNames.Count ? skillNames[i] : GetSkillDisplayName(skillIds[i]);
                SkillID? skillId = i < skillIds.Count ? skillIds[i] : null;
                flow.AddChild(CreateSkillPill(skillType, skillName, skillId));
            }
        }

        column.AddChild(flow);
        return column;
    }

    private Control CreateSkillPill(Skill.SkillTypes type, string skillName, SkillID? skillId)
    {
        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(150f, 36f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

        var label = CreateLabel(
            string.IsNullOrWhiteSpace(skillName) ? "未知技能" : skillName,
            18,
            new Color(0.92f, 0.96f, 1f, 0.94f)
        );
        label.CustomMinimumSize = new Vector2(142f, 34f);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);

        string tooltip = BuildSkillTooltip(type, skillName, skillId);
        row.MouseEntered += () =>
        {
            TweenHover(row, 1.04f);
            ShowTooltip(tooltip);
        };
        row.MouseExited += () =>
        {
            TweenHover(row, 1f);
            HideTooltip();
        };

        return row;
    }

    private List<RunHistoryCharacterSkillRecord> GetCharacterRecords()
    {
        return _currentRecord
                ?.CharacterSkillRecords
                ?.Where(record => record != null)
                .ToList()
            ?? new List<RunHistoryCharacterSkillRecord>();
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
            return "未知节点";

        var sb = new StringBuilder(256);
        string order = record.CompletionOrder > 0 ? record.CompletionOrder.ToString() : "?";
        sb.Append($"[b]#{order} {GetNodeTypeLabel(record.NodeType)}[/b]");
        sb.Append($"\n区域：{record.MapLevel + 1}  坐标：{record.Coordinate.X},{record.Coordinate.Y}");

        string summary = record.Summary;
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
            parts.Add($"敌人：{string.Join("，", record.EnemyNames)}");
        if (record.ElectricityCoinChange != 0)
            parts.Add($"电力币：{FormatSigned(record.ElectricityCoinChange)}");
        if (record.TransitionEnergyChange != 0)
            parts.Add($"跃迁能量：{FormatSigned(record.TransitionEnergyChange)}");
        AppendJoined(parts, "技能", record.SkillChanges);
        AppendJoined(parts, "道具", record.GainedItems);
        AppendJoined(parts, "装备", record.EquipmentChanges);
        AppendJoined(parts, "遗物", record.RelicChanges);
        AppendJoined(parts, "备注", record.Notes);
        return parts.Count == 0 ? "无额外记录" : string.Join("\n", parts);
    }

    private string BuildRelicTooltip(RunHistoryRelicRecord record)
    {
        if (record == null)
            return "未知遗物";

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

        string fallbackName = string.IsNullOrWhiteSpace(skillName) ? "未知技能" : skillName;
        return ColorizeTooltip($"[b]{fallbackName}[/b]  [color=#cccccc]({GetSkillTypeLabel(type)})[/color]");
    }

    private string BuildEquipmentTooltip(RunHistoryEquipmentRecord record)
    {
        if (record == null)
            return "未知装备";

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
        AddEquipmentStat(parts, "力量", record.Power);
        AddEquipmentStat(parts, "生存", record.Survivability);
        AddEquipmentStat(parts, "速度", record.Speed);
        AddEquipmentStat(parts, "生命上限", record.MaxLife);
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

    private static void TweenHover(Control target, float scale)
    {
        if (target == null || !target.IsInsideTree())
            return;

        target.CreateTween()
            .TweenProperty(target, "scale", new Vector2(scale, scale), 0.10f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private static Label CreateEmptyLabel(string text)
    {
        var label = CreateLabel(text, 18, new Color(0.74f, 0.82f, 0.90f, 0.62f), HorizontalAlignment.Center);
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
        label.AddThemeConstantOverride("outline_size", 2);
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
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomRight = radius,
            CornerRadiusBottomLeft = radius,
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
            _ => new Color(0.70f, 0.75f, 0.82f, 1f),
        };
    }

    private static string GetNodeTypeLabel(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => "普通战斗",
            LevelNode.LevelType.Elite => "精英战斗",
            LevelNode.LevelType.Boss => "首领战斗",
            LevelNode.LevelType.Event => "事件",
            LevelNode.LevelType.Shop => "商店",
            _ => "未知节点",
        };
    }

    private static Color GetSkillTypeColor(Skill.SkillTypes type)
    {
        return type switch
        {
            Skill.SkillTypes.Attack => new Color(1.00f, 0.48f, 0.36f, 1f),
            Skill.SkillTypes.Survive => new Color(0.42f, 0.78f, 1.00f, 1f),
            Skill.SkillTypes.Special => new Color(0.86f, 0.72f, 1.00f, 1f),
            _ => new Color(0.82f, 0.86f, 0.92f, 1f),
        };
    }

    private static string GetSkillTypeLabel(Skill.SkillTypes type)
    {
        return type switch
        {
            Skill.SkillTypes.Attack => "攻击",
            Skill.SkillTypes.Survive => "生存",
            Skill.SkillTypes.Special => "特殊",
            _ => "其它",
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

    private void PlayIntro()
    {
        _transitionTween?.Kill();
        if (BG != null)
            BG.Modulate = BG.Modulate with { A = 0f };
        if (CenterPanel != null)
        {
            CenterPanel.Scale = new Vector2(0.96f, 0.96f);
            CenterPanel.Modulate = CenterPanel.Modulate with { A = 0f };
            CenterPanel.PivotOffset = CenterPanel.Size * 0.5f;
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
    }

    private void Close()
    {
        HideTooltip();
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
            _transitionTween.TweenProperty(CenterPanel, "scale", new Vector2(0.98f, 0.98f), 0.14f);
            _transitionTween.TweenProperty(CenterPanel, "modulate:a", 0f, 0.14f);
        }
        _transitionTween.Finished += QueueFree;
    }
}
