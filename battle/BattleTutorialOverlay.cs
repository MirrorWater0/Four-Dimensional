using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BattleTutorialOverlay : CanvasLayer
{
    [Export] public float HighlightPadding { get; set; } = 12f;
    [Export] public float CardScreenMargin { get; set; } = 34f;

    private const string TutorialSavePath = "user://tutorial.cfg";
    private const string TutorialSection = "Tutorial";
    private const string BattleTutorialSeenKey = "BattleTutorialSeenV5";
    private const string TurnOrderTutorialBody =
        "同一阵营会按站位顺序轮流出手，行动后排到队尾。\n\n"
        + "我方阵营阶段结束后会进入敌方阵营阶段；敌方也按站位顺序依次行动。";

    private readonly List<TutorialStep> _steps = new();
    private TaskCompletionSource<bool> _completion;
    private int _stepIndex;
    private Battle _battle;
    private Control _root;
    private ColorRect _scrimTop;
    private ColorRect _scrimBottom;
    private ColorRect _scrimLeft;
    private ColorRect _scrimRight;
    private Panel _highlight;
    private PanelContainer _card;
    private Label _titleLabel;
    private RichTextLabel _bodyLabel;
    private Label _progressLabel;
    private Button _nextButton;
    private Button _skipButton;

    public static bool HasSeenTutorial()
    {
        var config = new ConfigFile();
        if (config.Load(TutorialSavePath) != Error.Ok)
            return false;

        return config.GetValue(TutorialSection, BattleTutorialSeenKey, false).AsBool();
    }

    public static void MarkTutorialSeen()
    {
        var config = new ConfigFile();
        config.Load(TutorialSavePath);
        config.SetValue(TutorialSection, BattleTutorialSeenKey, true);
        config.Save(TutorialSavePath);
    }

    public static async Task ShowAsync(Battle battle)
    {
        if (battle == null || !GodotObject.IsInstanceValid(battle))
            return;

        var overlay = GD.Load<PackedScene>("res://battle/UIScene/BattleTutorialOverlay.tscn").Instantiate<BattleTutorialOverlay>();
        battle.AddChild(overlay);
        await overlay.RunAsync(battle);
    }

    private async Task RunAsync(Battle battle)
    {
        _battle = battle;
        _completion = new TaskCompletionSource<bool>();
        BuildSteps();
        BuildUi();
        ShowStep(0);
        await _completion.Task;
    }

    private void BuildSteps()
    {
        _steps.Clear();
        BuildUpdatedSteps();
        if (_steps.Count > 0)
            return;

        _steps.Add(
            new TutorialStep(
                "战斗教程",
                "欢迎来到第一场战斗。\n\n这里是回合制战斗：观察敌人，选择角色技能，把敌方生命降到 0 即可让其进入濒死。"
            )
        );
        _steps.Add(
            new TutorialStep(
                "角色面板",
                "下方是我方角色面板。\n\n轮到角色行动时，对应面板的技能按钮会亮起。每名角色通常有攻击、生存、特殊三个技能。",
                battle => battle.CharacterControl?.ActionCardContainer
            )
        );
        _steps.Add(
            new TutorialStep(
                "技能按钮",
                "鼠标悬停技能可以查看说明和伤害预览。\n\n点击技能后角色会行动，行动结束会自动进入下一个角色或敌人的回合。",
                battle => battle.CharacterControl?.GetCardSlot(0)
            )
        );
        _steps.Add(
            new TutorialStep(
                "目标选择",
                "技能没有写明具体目标时，会按常规目标规则选择敌人。\n\n常规目标优先选择可被选中的存活敌人；嘲讽只会抢占攻击目标，隐身通常不会被选中。同排或距离更近的敌人会更早被考虑。"
            )
        );
        _steps.Add(
            new TutorialStep(
                "相对位",
                "技能说明里的“自身”“前一位”“后一位”说的是同队阵容顺序。\n\n自身是当前行动角色；后一位是阵容列表里的下一名队友，前一位是上一名队友。队伍首尾会相连计算。"
            )
        );
        _steps.Add(
            new TutorialStep(
                "出手顺序",
                TurnOrderTutorialBody
            )
        );
        _steps.Add(
            new TutorialStep(
                "生命与格挡",
                "角色头顶的生命条表示剩余生命，蓝色护盾数值表示格挡。\n\n受到伤害时会先扣格挡，再扣生命。生命归零会进入濒死。"
            )
        );
        _steps.Add(
            new TutorialStep(
                "复生",
                "濒死角色会跳过普通出手，也通常不能被普通治疗救回。\n\n带有“复生”的技能或复生状态会在濒死时让角色恢复生命并回到战斗中；没有复生时，濒死会持续到战斗结束。"
            )
        );
        _steps.Add(
            new TutorialStep(
                "物品",
                "资源栏里的物品是一次性消耗品。\n\n点击物品后选择目标即可生效，常见效果包括治疗、获得格挡、提升属性或造成伤害。物品用完会从资源栏移除。",
                battle => GetItemContainer(battle)
            )
        );
        _steps.Add(
            new TutorialStep(
                "遗物",
                "遗物是长期生效的奖励。\n\n它们通常会在战斗开始、结算或特定条件下自动触发。悬停遗物图标可以查看具体效果和剩余数量。",
                battle => GetRelicContainer(battle)
            )
        );
        _steps.Add(
            new TutorialStep(
                "战斗记录",
                "右侧按钮可以展开战斗记录，方便查看伤害、治疗、Buff 和濒死触发。\n\n如果效果很多，战斗记录会帮你复盘发生了什么。",
                battle => battle.RecordButton
            )
        );
        _steps.Add(
            new TutorialStep(
                "开始战斗",
                "教程结束后战斗会正式开始。\n\n先试着悬停技能看预览，再选择一个技能攻击敌人吧。"
            )
        );
    }

    private void BuildUpdatedSteps()
    {
        _steps.AddRange(
            new[]
            {
                new TutorialStep(
                    "战斗教程",
                    "欢迎来到第一场战斗。\n\n这里是回合制卡牌战斗：观察敌人，轮到我方阵营时从队伍牌堆抽牌，打出卡牌削减敌方生命。生命降到 0 会进入濒死。"
                ),
                new TutorialStep(
                    "手牌区域",
                    "下方是队伍共享手牌。\n\n所有角色的牌会放入同一个队伍抽牌堆；阵营回合开始时抽取队伍手牌，打出的牌和回合结束时剩余的手牌会进入队伍弃牌堆。抽牌堆空了以后，会把弃牌堆洗回抽牌堆继续抽。",
                    battle => battle.CharacterControl?.ActionCardContainer
                ),
                new TutorialStep(
                    "打出卡牌",
                    "悬停卡牌可以查看说明、关键词解释、目标高亮和伤害预览。\n\n点击卡牌会先把它拿起，再在手牌区域外点击即可打出；右键可以取消。卡牌左下角写着耗能，当前能量不足时仍可查看，但不能打出。",
                    battle => battle.CharacterControl?.GetCardSlot(0)
                ),
                new TutorialStep(
                    "结束回合",
                    "点击结束回合，或按 E 快捷结束。\n\n我方角色会按站位依次普通攻击，每人默认1次。基础攻击牌会增加本回合的临时攻击次数；两种次数会显示在角色状态栏。攻击结算后，临时次数清零，弃掉手牌，再进入敌方行动。",
                    battle => battle.CharacterControl?.EndTurnButton
                ),
                new TutorialStep(
                    "目标选择",
                    "大多数攻击会按常规规则自动选择敌人。\n\n嘲讽只会抢占攻击目标，隐身通常不会被选中；同排或距离更近的敌人会更早被考虑。少数支援牌会弹出友方目标选择，点选一名队友后才会结算。"
                ),
                new TutorialStep(
                    "相对位",
                    "技能说明里的“自身”“前一位”“后一位”说的是同队阵容顺序。\n\n自身是当前行动角色；后一位是阵容列表里的下一名队友，前一位是上一名队友。队伍首尾会相连计算。"
                ),
                new TutorialStep(
                    "出手顺序",
                    TurnOrderTutorialBody
                ),
                new TutorialStep(
                    "生命与格挡",
                    "角色头顶的生命条表示剩余生命，蓝色护盾数值表示格挡。\n\n受到伤害时会先扣格挡，再扣生命。生命归零会进入濒死；带有“复生”的技能或状态可以让濒死角色回到战斗。"
                ),
                new TutorialStep(
                    "物品与遗物",
                    "资源栏里的物品是一次性消耗品，角色行动时可以使用；遗物是长期生效的奖励，会在战斗开始、结算或特定条件下自动触发。\n\n悬停物品和遗物图标可以查看具体效果。",
                    battle => (Control)GetItemContainer(battle) ?? GetRelicContainer(battle)
                ),
                new TutorialStep(
                    "队伍生命",
                    "地图资源栏中的队伍生命显示全队当前生命总和，会跨战斗保留。\n\n"
                        + "角色生命归零会进入濒死并保持到后续战斗；带有“复生”的技能或状态可以让濒死角色回到战斗。\n\n"
                        + "如果我方全员濒死，本次行动失败。",
                    battle => GetCoreEnergyControl(battle)
                ),
                new TutorialStep(
                    "战斗记录",
                    "右侧按钮可以展开战斗记录，方便查看伤害、治疗、Buff、濒死和复生触发。\n\n如果一回合里效果很多，战斗记录会帮你复盘发生了什么。",
                    battle => battle.RecordButton
                ),
                new TutorialStep(
                    "开始战斗",
                    "教程结束后战斗会正式开始。\n\n先悬停卡牌看预览，再挑一张能量足够的牌打出去吧。"
                ),
            }
        );
    }

    private static HBoxContainer GetItemContainer(Battle battle) =>
        battle?.MapNode?.PlayerResourceState?.ItemContainer;

    private static VFlowContainer GetRelicContainer(Battle battle) =>
        battle?.MapNode?.PlayerResourceState?.RelicContainer;

    private static Control GetCoreEnergyControl(Battle battle) =>
        battle?.MapNode?.PlayerResourceState?.TransitionEnergyControl;

    private void BuildUi()
    {
        _root = GetNode<Control>("Root");
        _scrimTop = _root.GetNode<ColorRect>("ScrimTop");
        _scrimBottom = _root.GetNode<ColorRect>("ScrimBottom");
        _scrimLeft = _root.GetNode<ColorRect>("ScrimLeft");
        _scrimRight = _root.GetNode<ColorRect>("ScrimRight");
        _highlight = _root.GetNode<Panel>("Highlight");
        _card = _root.GetNode<PanelContainer>("Card");
        _titleLabel = _card.GetNode<Label>("Layout/Title");
        _bodyLabel = _card.GetNode<RichTextLabel>("Layout/Body");
        _progressLabel = _card.GetNode<Label>("Layout/Footer/Progress");
        _skipButton = _card.GetNode<Button>("Layout/Footer/Skip");
        _nextButton = _card.GetNode<Button>("Layout/Footer/Next");
        _skipButton.Pressed += Finish;
        _nextButton.Pressed += NextStep;
    }

    private void ShowStep(int index)
    {
        _stepIndex = Math.Clamp(index, 0, _steps.Count - 1);
        var step = _steps[_stepIndex];
        _titleLabel.Text = step.Title;
        _bodyLabel.Text = step.Body;
        _progressLabel.Text = $"{_stepIndex + 1}/{_steps.Count}";
        _nextButton.Text = _stepIndex >= _steps.Count - 1 ? "开始战斗" : "下一步";

        Rect2? targetRect = step.GetTargetRect(_battle);
        PositionHighlight(targetRect);
        PositionCard(targetRect);
    }

    private void PositionHighlight(Rect2? targetRect)
    {
        UpdateScrim(targetRect);

        if (targetRect == null)
        {
            _highlight.Visible = false;
            return;
        }

        Rect2 rect = targetRect.Value.Grow(HighlightPadding);
        _highlight.Visible = true;
        _highlight.Position = rect.Position;
        _highlight.Size = rect.Size;
    }

    private void UpdateScrim(Rect2? targetRect)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (targetRect == null)
        {
            SetRect(_scrimTop, new Rect2(Vector2.Zero, viewportSize));
            SetRect(_scrimBottom, new Rect2());
            SetRect(_scrimLeft, new Rect2());
            SetRect(_scrimRight, new Rect2());
            return;
        }

        Rect2 rect = targetRect.Value.Grow(HighlightPadding);
        rect.Position = new Vector2(
            Mathf.Clamp(rect.Position.X, 0f, viewportSize.X),
            Mathf.Clamp(rect.Position.Y, 0f, viewportSize.Y)
        );
        rect.Size = new Vector2(
            Mathf.Clamp(rect.Size.X, 0f, viewportSize.X - rect.Position.X),
            Mathf.Clamp(rect.Size.Y, 0f, viewportSize.Y - rect.Position.Y)
        );

        float leftWidth = Mathf.Max(0f, rect.Position.X);
        float topHeight = Mathf.Max(0f, rect.Position.Y);
        float rightWidth = Mathf.Max(0f, viewportSize.X - rect.End.X);
        float bottomHeight = Mathf.Max(0f, viewportSize.Y - rect.End.Y);

        SetRect(_scrimTop, new Rect2(0f, 0f, viewportSize.X, topHeight));
        SetRect(_scrimBottom, new Rect2(0f, rect.End.Y, viewportSize.X, bottomHeight));
        SetRect(_scrimLeft, new Rect2(0f, rect.Position.Y, leftWidth, rect.Size.Y));
        SetRect(_scrimRight, new Rect2(rect.End.X, rect.Position.Y, rightWidth, rect.Size.Y));
    }

    private static void SetRect(Control control, Rect2 rect)
    {
        if (control == null)
            return;

        control.Visible = rect.Size.X > 0f && rect.Size.Y > 0f;
        control.Position = rect.Position;
        control.Size = rect.Size;
    }

    private void PositionCard(Rect2? targetRect)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 cardSize = _card.Size;
        float margin = CardScreenMargin;

        if (targetRect == null)
        {
            _card.Position = new Vector2(
                (viewportSize.X - cardSize.X) * 0.5f,
                viewportSize.Y - cardSize.Y - margin
            );
            return;
        }

        Rect2 rect = targetRect.Value;
        float x = Mathf.Clamp(rect.Position.X, margin, viewportSize.X - cardSize.X - margin);
        float yBelow = rect.End.Y + margin;
        float yAbove = rect.Position.Y - cardSize.Y - margin;
        float y = yBelow + cardSize.Y <= viewportSize.Y - margin
            ? yBelow
            : Mathf.Max(margin, yAbove);

        _card.Position = new Vector2(x, y);
    }

    private void NextStep()
    {
        if (_stepIndex >= _steps.Count - 1)
        {
            Finish();
            return;
        }

        ShowStep(_stepIndex + 1);
    }

    private void Finish()
    {
        _completion?.TrySetResult(true);
        QueueFree();
    }

    public override void _ExitTree()
    {
        _completion?.TrySetResult(true);
        base._ExitTree();
    }

    private readonly record struct TutorialStep(
        string Title,
        string Body,
        Func<Battle, Control> Target = null
    )
    {
        public Rect2? GetTargetRect(Battle battle)
        {
            Control target = Target?.Invoke(battle);
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || !target.IsInsideTree()
                || !target.IsVisibleInTree()
            )
                return null;

            Rect2 rect = target.GetGlobalRect();
            return rect.Size.X <= 0 || rect.Size.Y <= 0 ? null : rect;
        }
    }
}
