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
    public static PackedScene EventScene = GD.Load<PackedScene>("res://Event/EventInterface.tscn");
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
        Button.Pressed += PressButton;
    }

    public List<EnemyRegedit> ProduceEnemies()
    {
        var rng = new Random(RandomNum);
        EnemyRegedit[] enemyRegedits =
        [
            new EvilRegedit(),
            new FearWormRegedit(),
            new ArmonRegedit(),
        ];
        List<EnemyRegedit> list = new()
        {
            enemyRegedits[rng.Next(0, 3)].GetRegedit(),
            enemyRegedits[rng.Next(0, 3)].GetRegedit(),
            enemyRegedits[rng.Next(0, 3)].GetRegedit(),
            enemyRegedits[rng.Next(0, 3)].GetRegedit(),
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
        ExplodeAnimation();
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
        switch (Type)
        {
            case LevelType.Normal:
                GotoBattlePreview();
                break;
            case LevelType.Boss:
                GotoBattlePreview();
                break;
            case LevelType.Elite:
                GotoBattlePreview();
                break;
            case LevelType.Event:
                GotoEvent();
                break;
        }
    }

    public void GotoBattlePreview()
    {
        var tween = ExplodeAnimation();

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
                    preview.StartAnimation();
                })
            );
    }

    public void GotoEvent()
    {
        var gameEventInterface = EventScene.Instantiate() as EventInterface;
        gameEventInterface.WhichNode = this;
        gameEventInterface.ThisEvent = GameEvent.Catalog[
            new Random().Next(0, GameEvent.Catalog.Length)
        ];
        var tween = ExplodeAnimation();
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    GetTree().Root.GetNode("Map/SiteUI").AddChild(gameEventInterface);
                })
            );
    }

    public Tween ExplodeAnimation()
    {
        IsAnimate = true;
        Ghost.Modulate = new Color(1, 1, 1, 1);
        Ghost.Scale = Vector2.One;
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
        tween.TweenCallback(
            Callable.From(() =>
            {
                IsAnimate = false;
                Ghost.Scale = Vector2.One;
                Ghost.Modulate = new Color(1, 1, 1, 0);
            })
        );
        return tween;
    }

    public static void RandomPosition<T>(List<T> list, int RandomNum)
        where T : EnemyRegedit
    {
        Random random = new Random(RandomNum);

        // Chosetarget1 prefers targets within a "lane" in front->mid->back order:
        // (1,4,7), (2,5,8), (3,6,9).
        //
        // Placement rule:
        // - FrontRow enemies: absolutely random among all free slots (1..9).
        // - BackRow enemies: prefer back slots (7..9) if possible, then mid (4..6), then front (1..3).
        var front = new List<int> { 1, 2, 3 };
        var middle = new List<int> { 4, 5, 6 };
        var back = new List<int> { 7, 8, 9 };
        var any = Enumerable.Range(1, 9).ToList();
        var available = new HashSet<int>(any);

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
        Shuffle(any, random);

        int? TakeOne(List<int> pool)
        {
            while (pool.Count > 0)
            {
                int idx = pool.Count - 1;
                int val = pool[idx];
                pool.RemoveAt(idx);
                if (available.Remove(val))
                    return val;
            }
            return null;
        }

        int? TakeAny()
        {
            while (any.Count > 0)
            {
                int idx = any.Count - 1;
                int val = any[idx];
                any.RemoveAt(idx);
                if (available.Remove(val))
                    return val;
            }
            return null;
        }

        var frontEnemies = list.Where(x =>
                x != null && x.PType == EnemyRegedit.EnemyPositionType.FrontRow
            )
            .ToList();
        var backEnemies = list.Where(x =>
                x != null && x.PType == EnemyRegedit.EnemyPositionType.BackRow
            )
            .ToList();
        var otherEnemies = list.Where(x =>
                x != null
                && x.PType != EnemyRegedit.EnemyPositionType.FrontRow
                && x.PType != EnemyRegedit.EnemyPositionType.BackRow
            )
            .ToList();

        foreach (var enemy in frontEnemies)
        {
            int? pos = TakeAny();

            if (pos.HasValue)
                enemy.PositionIndex = pos.Value;
        }

        foreach (var enemy in backEnemies)
        {
            int? pos = TakeOne(back) ?? TakeOne(middle) ?? TakeOne(front) ?? TakeAny();

            if (pos.HasValue)
                enemy.PositionIndex = pos.Value;
        }

        foreach (var enemy in otherEnemies)
        {
            int? pos = TakeAny();

            if (pos.HasValue)
                enemy.PositionIndex = pos.Value;
        }
    }
}
