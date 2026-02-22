using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Godot;

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

    // public List<EnemyRegedit> EnemiesRegeditList;
    public LevelState State { get; set; }
    public LevelType Type { get; set; } = LevelType.Normal;
    public List<EnemyRegedit> EnemiesRegeditList;
    public Button Button => field ??= GetNode("Button") as Button;

    // public ProgressBar ProgressBar => field ??= GetNode("ProgressBar") as ProgressBar;
    public ShaderMaterial mat;
    public Color LockColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);
    public List<LevelNode> NextNodes = new List<LevelNode>();
    public List<LevelNode> ParentNodes = new List<LevelNode>();
    public static PackedScene BattlePreviewScene = GD.Load<PackedScene>(
        "res://battle/BattlePreview/BattlePreview.tscn"
    );
    public Vector2I SelfCoordinate;
    public ColorRect Ghost => field ??= GetNode("ghost") as ColorRect;
    public AnimationPlayer AnimationPlayer =>
        field ??= GetNode("AnimationPlayer") as AnimationPlayer;
    public int RandomNum;

    public override void _Ready()
    {
        mat = Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Material = mat;
        mat.SetShaderParameter("show_inner", false);

        Color = LockColor;
        Button.Disabled = true;
        Button.Disabled = State == LevelState.Locked;
        StartAnimation();
        PivotOffset = Size / 2;
        Ghost.PivotOffset = Ghost.Size / 2;
        Button.MouseEntered += () =>
        {
            Ghost.Modulate = new Color(1, 1, 1, 1);
            Ghost.Scale = new Vector2(1f, 1f);
            CreateTween().TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.2f);
        };
        Button.MouseExited += () =>
        {
            if (IsAnimate == true)
                return;
            Ghost.Modulate = new Color(1, 1, 1, 0);
            CreateTween().TweenProperty(this, "scale", new Vector2(1f, 1f), 0.2f);
        };
        Button.Pressed += Completed;
        Button.Pressed += PressButton;
    }

    // public override void _Process(double delta)
    // {
    // }

    public List<EnemyRegedit> ProduceEnemies()
    {
        List<EnemyRegedit> list = new()
        {
            new EvilRegedit(),
            new FearWormRegedit(),
            new EvilRegedit(),
        };
        RandomPosition(list, RandomNum);
        return list;
    }

    public void Unlock()
    {
        if (State == LevelState.Completed)
            return;
        Color = 2 * new Color(1, 1, 1, 1);
        GameInfo.FirstLevelState[SelfCoordinate] = LevelState.Unlocked;
        State = LevelState.Unlocked;
        Button.Disabled = false;
    }

    public void ApplyLoadedState()
    {
        // Apply visual state based on the loaded State value
        switch (State)
        {
            case LevelState.Locked:
                Color = LockColor;
                Button.Disabled = true;
                break;

            case LevelState.Unlocked:
                Color = 2 * new Color(1, 1, 1, 1);
                Button.Disabled = false;

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
                break;

            case LevelState.Completed:
                Color = LockColor;
                Button.Disabled = true;
                ApplyCompletedVisuals();
                break;
        }
    }

    public void ApplyCompletedVisuals()
    {
        // Only the visual effects, no state changes
        Color = 2 * new Color(1, 1, 1, 1);
        mat.SetShaderParameter("show_inner", true);
        mat.SetShaderParameter("show_inner_color", new Color(1, 0.8f, 0, 1));
    }

    public void Completed()
    {
        if (Type != LevelType.Boss)
        {
            foreach (var node in NextNodes)
            {
                if (node.State == LevelState.Locked)
                    node.Unlock();
            }
        }
        GetParent().GetParent<LevelProgress>().OnNodeSelected(this);
        State = LevelState.Completed;
        GameInfo.FirstLevelState[SelfCoordinate] = LevelState.Completed;
        Button.Disabled = true;
        ApplyCompletedVisuals();
        SaveSystem.SaveAll();
    }

    public void StartAnimation()
    {
        // ProgressBar.Scale = new Vector2(0, 1);
        // Tween tween = CreateTween();
        // tween.TweenProperty(ProgressBar, "scale", new Vector2(1, 1), 0.5f);
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

    private bool IsAnimate = false;

    public void PressButton()
    {
        IsAnimate = true;
        Tween tween = CreateTween();
        tween.TweenProperty(Ghost, "scale", new Vector2(2.2f, 2.2f), 0.3f);
        tween
            .Parallel()
            .TweenProperty(this, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        tween
            .Parallel()
            .TweenProperty(Ghost, "modulate", new Color(1, 1, 1, 0f), 0.3f)
            .SetEase(Tween.EaseType.Out);

        EnemiesRegeditList = ProduceEnemies();
        var preview = BattlePreviewScene.Instantiate() as BattlePreview;
        preview.WhichNode = this;
        preview.RandomNum = RandomNum;
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    GetTree().Root.GetNode("Map/SiteUI").AddChild(preview);
                    IsAnimate = false;
                })
            );
    }

    public static void RandomPosition<T>(List<T> list, int RandomNum)
        where T : EnemyRegedit
    {
        Random random = new Random(RandomNum);

        // Chosetarget1 prefers positions within the same row in front->mid->back order:
        // (1,4,7), (2,5,8), (3,6,9). So we map "front row" to col0 (1-3) and "back row"
        // to col2 (7-9) to match that priority.
        var front = new List<int> { 1, 2, 3 };
        var middle = new List<int> { 4, 5, 6 };
        var back = new List<int> { 7, 8, 9 };

        static void Shuffle(List<int> arr, Random rng)
        {
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (arr[i], arr[k]) = (arr[k], arr[i]);
            }
        }

        Shuffle(front, random);
        Shuffle(middle, random);
        Shuffle(back, random);

        static int? TakeOne(List<int> pool)
        {
            if (pool.Count == 0)
                return null;
            int idx = pool.Count - 1;
            int val = pool[idx];
            pool.RemoveAt(idx);
            return val;
        }

        foreach (var enemy in list)
        {
            if (enemy == null)
                continue;

            int? pos = enemy.PType switch
            {
                EnemyRegedit.EnemyPositionType.FrontRow =>
                    TakeOne(front) ?? TakeOne(middle) ?? TakeOne(back),
                EnemyRegedit.EnemyPositionType.BackRow =>
                    TakeOne(back) ?? TakeOne(middle) ?? TakeOne(front),
                _ => TakeOne(front) ?? TakeOne(middle) ?? TakeOne(back),
            };

            if (pos.HasValue)
                enemy.PositionIndex = pos.Value;
        }
    }
}
