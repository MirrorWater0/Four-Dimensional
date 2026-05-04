using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BattleTutorialOverlay : CanvasLayer
{
    private const string TutorialSavePath = "user://tutorial.cfg";
    private const string TutorialSection = "Tutorial";
    private const string BattleTutorialSeenKey = "BattleTutorialSeen";

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

        var overlay = new BattleTutorialOverlay();
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
                battle => battle.CharacterControl?.CharaterFrame1
            )
        );
        _steps.Add(
            new TutorialStep(
                "技能按钮",
                "鼠标悬停技能可以查看说明和伤害预览。\n\n点击技能后角色会行动，行动结束会自动进入下一个角色或敌人的回合。",
                battle => battle.CharacterControl?.CharaterFrame1?.SkillButtonContainer
            )
        );
        _steps.Add(
            new TutorialStep(
                "目标选择",
                "技能没有写明具体目标时，会按常规目标规则选择敌人。\n\n常规目标优先选择可被选中的存活敌人；嘲讽会抢占目标，隐身通常不会被选中。同排或距离更近的敌人会更早被考虑。"
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
                "上方是双方行动点数条。\n\n队伍内按阵位轮流出手，行动后排到队尾。左边数字是当前行动点数，括号里是阵容总速度；行动点数到达阈值时，对应队伍会获得一次额外出手机会。",
                battle => battle.GetNodeOrNull<Control>("ActionPoinBox")
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

    private static HBoxContainer GetItemContainer(Battle battle) =>
        battle?.MapNode?.PlayerResourceState?.ItemContainer;

    private static VBoxContainer GetRelicContainer(Battle battle) =>
        battle?.MapNode?.PlayerResourceState?.RelicContainer;

    private void BuildUi()
    {
        Layer = 100;
        Name = "BattleTutorialOverlay";

        _root = new Control
        {
            Name = "Root",
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);

        _scrimTop = CreateScrimRect();
        _scrimBottom = CreateScrimRect();
        _scrimLeft = CreateScrimRect();
        _scrimRight = CreateScrimRect();
        _root.AddChild(_scrimTop);
        _root.AddChild(_scrimBottom);
        _root.AddChild(_scrimLeft);
        _root.AddChild(_scrimRight);

        _highlight = new Panel
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var highlightStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.5f, 0.78f, 1f, 0.06f),
            BorderColor = new Color(0.65f, 0.95f, 1f, 0.95f),
            BorderWidthLeft = 3,
            BorderWidthTop = 3,
            BorderWidthRight = 3,
            BorderWidthBottom = 3,
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14,
        };
        _highlight.AddThemeStyleboxOverride("panel", highlightStyle);
        _root.AddChild(_highlight);

        _card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(620, 0),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.035f, 0.055f, 0.1f, 0.96f),
            BorderColor = new Color(0.55f, 0.8f, 1f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 18,
        };
        _card.AddThemeStyleboxOverride("panel", cardStyle);
        _root.AddChild(_card);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 12);
        _card.AddChild(layout);

        _titleLabel = new Label();
        _titleLabel.AddThemeFontSizeOverride("font_size", 34);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.94f, 1f, 1f));
        layout.AddChild(_titleLabel);

        _bodyLabel = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(560, 165),
            FitContent = true,
            BbcodeEnabled = true,
            ScrollActive = false,
        };
        _bodyLabel.AddThemeFontSizeOverride("normal_font_size", 22);
        _bodyLabel.AddThemeColorOverride("default_color", new Color(0.9f, 0.96f, 1f, 0.96f));
        layout.AddChild(_bodyLabel);

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 12);
        layout.AddChild(footer);

        _progressLabel = new Label
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _progressLabel.AddThemeFontSizeOverride("font_size", 18);
        _progressLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.78f, 0.88f, 1f));
        footer.AddChild(_progressLabel);

        _skipButton = new Button { Text = "跳过" };
        _skipButton.Pressed += Finish;
        footer.AddChild(_skipButton);

        _nextButton = new Button { Text = "下一步" };
        _nextButton.Pressed += NextStep;
        footer.AddChild(_nextButton);
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

    private static ColorRect CreateScrimRect() =>
        new()
        {
            Color = new Color(0.02f, 0.03f, 0.07f, 0.72f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };

    private void PositionHighlight(Rect2? targetRect)
    {
        UpdateScrim(targetRect);

        if (targetRect == null)
        {
            _highlight.Visible = false;
            return;
        }

        Rect2 rect = targetRect.Value.Grow(12);
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

        Rect2 rect = targetRect.Value.Grow(12);
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
        Vector2 cardSize = new(620, 270);
        const float margin = 34f;

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
            if (target == null || !GodotObject.IsInstanceValid(target) || !target.IsInsideTree())
                return null;

            Rect2 rect = target.GetGlobalRect();
            return rect.Size.X <= 0 || rect.Size.Y <= 0 ? null : rect;
        }
    }
}
