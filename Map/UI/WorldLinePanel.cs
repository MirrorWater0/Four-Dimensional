using Godot;

public partial class WorldLinePanel : PanelContainer {
    private LevelProgress _levelProgress;
    private int _selectedLine;
    private bool _regularDistribution;
    private bool _bound;
    private readonly Control[] _charges = new Control[GameInfo.WorldLineSwitchLimit];
    private WorldLineProbabilityRow[] _rows;
    private Label _stage;
    private Label _selectedIndex;
    private Label _selectedName;
    private Label _selectedState;
    private Label _switches;
    private WorldLineTrace _trace;
    private Control _probabilityRows;
    private Control _endpoint;
    private Control _identity;
    private Button _previousPreview;
    private Button _nextPreview;
    private Button _next;
    private Button _switch;
    private Tween _detailTween;
    private Tween _chargeTween;
    private Tween _tracePulseTween;
    private StyleBoxFlat _primaryStyle;
    private StyleBoxFlat _primaryHoverStyle;
    private (int Region, int Active, int Preview, int Switches, int Stage,
        bool Committed, bool Finished, bool Transitioning, bool Regular)? _state;

    public override void _Ready() {
        _stage = GetNode<Label>("%Stage");
        _selectedIndex = GetNode<Label>("%SelectedIndex");
        _selectedName = GetNode<Label>("%SelectedName");
        _selectedState = GetNode<Label>("%SelectedState");
        _switches = GetNode<Label>("%Switches");
        _trace = GetNode<WorldLineTrace>("%Trace");
        _probabilityRows = GetNode<Control>("%ProbabilityRows");
        _endpoint = GetNode<Control>("%Endpoint");
        _identity = GetNode<Control>("%Identity");
        _previousPreview = GetNode<Button>("%PreviousPreview");
        _nextPreview = GetNode<Button>("%NextPreview");
        _next = GetNode<Button>("%Next");
        _switch = GetNode<Button>("%Switch");
        _primaryStyle = (StyleBoxFlat)_switch.GetThemeStylebox("normal").Duplicate();
        _primaryHoverStyle = (StyleBoxFlat)_switch.GetThemeStylebox("hover").Duplicate();
        _switch.AddThemeStyleboxOverride("normal", _primaryStyle);
        _switch.AddThemeStyleboxOverride("pressed", _primaryStyle);
        _switch.AddThemeStyleboxOverride("hover", _primaryHoverStyle);
        _rows = new WorldLineProbabilityRow[_probabilityRows.GetChildCount()];
        for (int i = 0; i < _rows.Length; i++)
            _rows[i] = _probabilityRows.GetChild<WorldLineProbabilityRow>(i);
        for (int i = 0; i < _charges.Length; i++)
            _charges[i] = GetNode<Control>($"%Charge{i + 1}");
    }

    public void Bind(LevelProgress levelProgress) {
        if (_bound)
            return;
        _levelProgress = levelProgress;
        _selectedLine = GameInfo.CurrentWorldLine;
        _bound = true;
        _previousPreview.Pressed += () => CyclePreview(-1);
        _nextPreview.Pressed += () => CyclePreview(1);
        _next.Pressed += () => SetDistributionMode(false);
        GetNode<Button>("%Regular").Pressed += () => SetDistributionMode(true);
        _switch.Pressed += () => {
            _levelProgress.SwitchWorldLine(_selectedLine);
            Refresh();
        };
        Refresh();
    }

    private void SetDistributionMode(bool regular) {
        _regularDistribution = regular;
        Refresh();
    }

    private void CyclePreview(int direction) {
        _selectedLine = (_selectedLine + direction + GameInfo.WorldLineCount) % GameInfo.WorldLineCount;
        Refresh();
    }

    public void Refresh() {
        if (!_bound || !GodotObject.IsInstanceValid(_levelProgress))
            return;
        bool initial = !_state.HasValue;
        if (_state.HasValue && _state.Value.Region != GameInfo.CurrentLevel)
            _selectedLine = GameInfo.CurrentWorldLine;
        bool selectedIsCurrent = _selectedLine == GameInfo.CurrentWorldLine;
        bool transitioning = _levelProgress.IsWorldLineTransitioning;
        _switch.Disabled = selectedIsCurrent || !_levelProgress.CanSwitchWorldLine;

        var state = (Region: GameInfo.CurrentLevel, Active: GameInfo.CurrentWorldLine,
            Preview: _selectedLine, Switches: GameInfo.WorldLineSwitchesRemaining,
            Stage: _levelProgress.NextRoadStage, Committed: _levelProgress.IsNextEncounterCommitted,
            Finished: GameInfo.RunFinished, Transitioning: transitioning, Regular: _regularDistribution);
        if (_state == state)
            return;
        bool selectionChanged = initial || _state.Value.Preview != _selectedLine;
        bool chargesChanged = initial || _state.Value.Switches != state.Switches;
        bool startedTransition = transitioning && (initial || !_state.Value.Transitioning);
        _state = state;

        Color color = WorldLineVisuals.GetColor(_selectedLine);
        _primaryStyle.BgColor = Colors.Transparent;
        _primaryStyle.BorderColor = new Color(color, 0.65f);
        _primaryHoverStyle.BgColor = Colors.Transparent;
        _primaryHoverStyle.BorderColor = color;
        int nextStage = Mathf.Clamp(state.Stage + 1, 1, _levelProgress.RoadStageCount);
        _stage.Text = $"{nextStage:00} / {_levelProgress.RoadStageCount:00}";
        _selectedIndex.Text = $"{_selectedLine + 1:00} / {GameInfo.WorldLineCount:00}";
        _selectedIndex.AddThemeColorOverride("font_color", new Color(color, 0.55f));
        _selectedName.Text = GameInfo.GetWorldLineName(_selectedLine);
        _selectedName.AddThemeColorOverride("font_color", color);
        _selectedState.Text = transitioning ? "重构中" : selectedIsCurrent ? "当前" : "预览";
        _selectedState.TooltipText = $"当前：{GameInfo.GetWorldLineName()}\n亮色轨迹为预览，暗色轨迹为当前世界线。";
        _trace.ShowWorldLine(_selectedLine, GameInfo.CurrentWorldLine, !initial);

        bool endOfRoad = state.Stage >= _levelProgress.RoadStageCount - 1;
        bool showEndpoint = !_regularDistribution && endOfRoad;
        _probabilityRows.Visible = !showEndpoint;
        _endpoint.Visible = showEndpoint;
        _endpoint.GetNode<Label>("Title").Text = state.Stage >= _levelProgress.RoadStageCount
            ? "道路已完成" : "终点 · 首领";
        int inspectedStage = _regularDistribution ? GameInfo.FirstEliteRoadStage : state.Stage;
        for (int i = 0; i < _rows.Length; i++) {
            WorldLineProbabilityRow row = _rows[i];
            int probability = _levelProgress.GetRoadNodeProbability(inspectedStage, _selectedLine, row.EncounterType);
            int activeProbability = _levelProgress.GetRoadNodeProbability(inspectedStage, GameInfo.CurrentWorldLine, row.EncounterType);
            row.ShowProbability(probability, activeProbability,
                _levelProgress.GetNodeTypeRingColor(row.EncounterType), !initial && !showEndpoint, i * 0.025f);
        }

        _next.TooltipText = endOfRoad ? "所有世界线的终点均为首领。"
            : state.Stage == _levelProgress.RoadStageCount / 2 ? "中途固定休息 · 所有世界线一致"
            : state.Committed ? "当前遭遇已锁定 · 此处显示该步的抽取概率"
            : state.Stage == 0 ? "起点固定普通战斗 · 前3节点不出现精英"
            : state.Stage < GameInfo.FirstEliteRoadStage ? "开局保护 · 精英概率归零，其余概率已重分配"
            : $"第 {state.Stage + 1} 节点 · 差值与当前世界线比较";
        _switches.Text = $"{state.Switches}/{GameInfo.WorldLineSwitchLimit}";
        _switch.Text = transitioning ? "切换中…"
            : state.Finished || state.Stage >= _levelProgress.RoadStageCount ? "已完成"
            : selectedIsCurrent ? "已启用"
            : state.Switches <= 0 ? "次数已用尽"
            : state.Committed ? "遭遇已锁定"
            : "切换 · 1次";
        _switch.TooltipText = transitioning ? "道路正在变换，请稍候。"
            : state.Finished || state.Stage >= _levelProgress.RoadStageCount ? "当前道路已完成。"
            : selectedIsCurrent ? "正在使用此世界线。使用左右三角按钮免费预览其他世界线。"
            : state.Switches <= 0 ? "切换次数已用尽，仍可免费查看所有世界线的概率。"
            : state.Committed ? "当前遭遇已确定，完成后可再次切换。"
            : $"切换至{GameInfo.GetWorldLineName(_selectedLine)}，消耗1次机会。";
        _previousPreview.TooltipText = $"预览{GameInfo.GetWorldLineName((_selectedLine + GameInfo.WorldLineCount - 1) % GameInfo.WorldLineCount)}，不消耗次数";
        _nextPreview.TooltipText = $"预览{GameInfo.GetWorldLineName((_selectedLine + 1) % GameInfo.WorldLineCount)}，不消耗次数";
        foreach (Button arrow in new[] { _previousPreview, _nextPreview }) {
            arrow.AddThemeColorOverride("font_color", new Color(color, 0.72f));
            arrow.AddThemeColorOverride("font_hover_color", color);
            arrow.AddThemeColorOverride("font_pressed_color", new Color(color, 0.85f));
        }

        if (chargesChanged) {
            _chargeTween?.Kill();
            _chargeTween = CreateTween().SetParallel();
            for (int i = 0; i < _charges.Length; i++)
                _chargeTween.TweenProperty(_charges[i], "modulate:a", i < state.Switches ? 1f : 0.15f, 0.3f);
        }
        if (selectionChanged && !initial) {
            _detailTween?.Kill();
            _identity.Modulate = new Color(1f, 1f, 1f, 0.35f);
            _detailTween = CreateTween();
            _detailTween.TweenProperty(_identity, "modulate:a", 1f, 0.26f);
        }
        if (startedTransition) {
            _tracePulseTween?.Kill();
            _trace.Modulate = new Color(1.55f, 1.55f, 1.55f);
            _tracePulseTween = CreateTween();
            _tracePulseTween.TweenProperty(_trace, "modulate", Colors.White, 0.85f);
        }
    }
}
