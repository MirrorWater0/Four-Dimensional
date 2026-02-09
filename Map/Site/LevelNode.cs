using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Godot;

[Tool]
public partial class LevelNode : ColorRect
{
    [Export]
    bool BarVisible = true;

    public enum LevelState
    {
        Locked,
        Unlocked,
        Completed,
    }

    public enum LevelType
    {
        Normal,
        Event,
        Elite,
        Boss,
    }

    public static List<EnemyRegedit> EnemiesRegeditList;
    public LevelState State { get; set; }
    public LevelType Type { get; set; } = LevelType.Normal;

    public Button Button => field ??= GetNode("Button") as Button;
    public ProgressBar ProgressBar => field ??= GetNode("ProgressBar") as ProgressBar;
    public ShaderMaterial mat;
    public Color LockColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
    public LevelNode NextNode;
    public PackedScene BattlePreviewScene = GD.Load<PackedScene>("res://battle/BattlePreview.tscn");
    public Vector2I SelfCoordinate;

    public override void _Ready()
    {
        mat = Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Material = mat;
        mat.SetShaderParameter("show_inner", false);

        Color = LockColor;
        Button.Disabled = true;
        ProgressBar.Value = 0;
        Button.Disabled = State == LevelState.Locked;
        StartAnimation();

        Button.Pressed += CompletedAnimation;
        Button.Pressed += PressButton;
    }

    public override void _Process(double delta)
    {
        if (BarVisible == false)
        {
            ProgressBar.Visible = false;
        }
    }

    public List<EnemyRegedit> ProduceEnemies()
    {
        List<EnemyRegedit> list = new() { new EvilRegedit(), new EvilRegedit(), new EvilRegedit() };
        RandomPosition(list);
        return list;
    }

    public void Unlock()
    {
        Color = 2 * new Color(1, 1, 1, 1);
        Color ringColor = new Color(1, 1, 1, 1);

        switch (Type)
        {
            case LevelType.Boss:
                ringColor = new Color(0.6f, 0, 0.9f, 1);
                break;
            case LevelType.Elite:
                ringColor = new Color(1, 0.1f, 0.1f, 1);
                break;
            case LevelType.Event:
                ringColor = new Color(0, 0.6f, 1, 1);
                break;
        }
        GameInfo.FirstLevelState[SelfCoordinate] = LevelState.Unlocked;
        mat.SetShaderParameter("ring_color", ringColor);
        State = LevelState.Unlocked;
        Button.Disabled = false;
    }

    public void CompletedAnimation()
    {
        if (NextNode.State == LevelState.Locked)
            NextNode?.Unlock();
        State = LevelState.Completed;
        GameInfo.FirstLevelState[SelfCoordinate] = LevelState.Completed;
        Button.Disabled = true;
        mat.SetShaderParameter("show_inner", true);
        mat.SetShaderParameter("show_inner_color", new Color(1, 0.8f, 0, 1));
        Tween tween = CreateTween();
        tween.TweenProperty(ProgressBar, "value", 100, 0.5f);
    }

    public void StartAnimation()
    {
        ProgressBar.Scale = new Vector2(0, 1);
        Tween tween = CreateTween();
        tween.TweenProperty(ProgressBar, "scale", new Vector2(1, 1), 0.5f);
    }

    public void ApplyEntranceMove(Vector2 offset, float duration, float delay)
    {
        Vector2 targetPos = Position;
        Position += offset;

        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        if (delay > 0)
            tween.TweenInterval(delay);

        tween.TweenProperty(this, "position", targetPos, duration);
    }

    public void ColorChose()
    {
        Color ringColor = new Color(1, 1, 1, 1);

        switch (Type)
        {
            case LevelType.Boss:
                ringColor = new Color(0.6f, 0, 0.9f, 1);
                break;
            case LevelType.Elite:
                ringColor = new Color(1, 0.1f, 0.1f, 1);
                break;
            case LevelType.Event:
                ringColor = new Color(0, 0.6f, 1, 1);
                break;
        }

        mat.SetShaderParameter("ring_color", ringColor);
    }

    public void PressButton()
    {
        EnemiesRegeditList = ProduceEnemies();
        var preview = BattlePreviewScene.Instantiate();
        GetTree().Root.GetNode("Map/SiteUI").AddChild(preview);
    }

    public static void RandomPosition<T>(List<T> list)
        where T : EnemyRegedit
    {
        Random random = new Random(GameInfo.Seed);

        // 1. 准备所有可能的位置 (1 到 9)
        List<int> possiblePositions = Enumerable.Range(1, 9).ToList();

        // 2. 洗牌算法：打乱这个列表
        for (int i = possiblePositions.Count - 1; i > 0; i--)
        {
            int k = random.Next(i + 1); // 随机选一个索引
            // 交换位置
            int value = possiblePositions[k];
            possiblePositions[k] = possiblePositions[i];
            possiblePositions[i] = value;
        }

        // 3. 按顺序分配给敌人
        for (int i = 0; i < list.Count; i++)
        {
            // 注意：如果敌人数量超过9个，这里会溢出，需要加判断
            if (i < possiblePositions.Count)
            {
                list[i].PositionIndex = possiblePositions[i];
            }
        }
    }
}
