using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelNode : ColorRect
{
    private const int WeakEnemyStageCount = 5;
    private static readonly PackedScene TipScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );
    private static readonly Vector2 HoverTipOffset = new Vector2(36f, 28f);
    private Tip _hoverTip;

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
        Shop,
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
    private bool _isNodeHovered;
    private string _lastHoverTipText;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
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

        MouseEntered += OnNodeMouseEntered;
        MouseExited += OnNodeMouseExited;
    }

    public override void _Process(double delta)
    {
        if (!_isNodeHovered)
            return;

        if (State != LevelState.Completed || !IsVisibleInTree() || HasVisibleBlockingSiteUi())
        {
            HideHoverTip();
            return;
        }

        if (_hoverTip == null || !GodotObject.IsInstanceValid(_hoverTip) || !_hoverTip.Visible)
            TryShowHoverTip();
    }

    public override void _ExitTree()
    {
        _isNodeHovered = false;
        HideHoverTip();
        DisposeHoverTip();
    }

    public List<EnemyRegedit> ProduceEnemies()
    {
        List<EnemyRegedit> list = Type switch
        {
            LevelType.Normal => GetNormalEnemies(),
            LevelType.Elite => GetEliteEnemies(),
            LevelType.Boss => GetBossEnemies(),
            _ => GetEliteEnemies(),
        };
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
                    case LevelType.Shop:
                        ringColor = new Color(1f, 0.84f, 0.18f, 1f);
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
        GameInfo.CompleteLevelNodeTracking(this);
        UpdateHoverTipIfVisible();

        SaveSystem.SaveAll();
    }

    private void OnNodeMouseEntered()
    {
        _isNodeHovered = true;
        TryShowHoverTip();
    }

    private void OnNodeMouseExited()
    {
        _isNodeHovered = false;
        HideHoverTip();
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
            case LevelType.Shop:
                ringColor = new Color(1f, 0.84f, 0.18f, 1f);
                break;
        }

        mat.SetShaderParameter("ring_color", ringColor);
    }

    private bool IsAnimate = false;

    public void PressButton()
    {
        if (State != LevelState.Unlocked)
            return;

        GetParent()?.GetParent<LevelProgress>()?.LockAllNodes();
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
            case LevelType.Shop:
                GotoShop();
                break;
        }
    }

    public void GotoBattlePreview()
    {
        GameInfo.BeginLevelNodeTracking(this);
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
        GameInfo.BeginLevelNodeTracking(this);
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

    public void GotoShop()
    {
        GameInfo.BeginLevelNodeTracking(this);
        GetParent()?.GetParent<LevelProgress>()?.OnNodeSelected(this);

        var tween = ExplodeAnimation();
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    var shop = SpaceStationShop.Show(this);
                    shop.WhichNode = this;
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
        var positions = Enumerable.Range(1, 9).ToList();

        for (int i = positions.Count - 1; i > 0; i--)
        {
            int k = random.Next(i + 1);
            (positions[i], positions[k]) = (positions[k], positions[i]);
        }

        int posIndex = 0;
        foreach (var enemy in list)
        {
            if (enemy == null)
                continue;
            if (posIndex >= positions.Count)
                break;
            enemy.PositionIndex = positions[posIndex++];
        }
    }

    public List<EnemyRegedit> GetNormalEnemies()
    {
        var rng = new Random(RandomNum);
        EnemyRegedit[] weakEnemyRegedits =
        [
            new EvilRegedit(),
            new FearWormRegedit(),
            new ArmonRegedit(),
            new EvilRegedit(),
            new AlienBodyRegedit(),
            new RedHuskRegedit(),
            new TurbineRegedit(),
        ];
        EnemyRegedit[] strongEnemyRegedits = [new FerociouessRegedit()];

        List<EnemyRegedit> list = new()
        {
            weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit(),
            weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit(),
        };
        if (SelfCoordinate.X >= WeakEnemyStageCount)
        {
            int strongEnemyCount = rng.Next(0, 3);
            for (int i = 0; i < strongEnemyCount; i++)
            {
                list.Add(strongEnemyRegedits[rng.Next(strongEnemyRegedits.Length)].GetRegedit());
            }

            for (int i = 0; i < (2 - strongEnemyCount) * 2; i++)
            {
                list.Add(weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit());
            }
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                list.Add(weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit());
            }
        }
        RandomPosition(list, RandomNum);
        // var list = new List<EnemyRegedit>
        // {
        //     new WarRegedit(){ PositionIndex = 5 },
        // };
        return list;
    }

    public List<EnemyRegedit> GetEliteEnemies()
    {
        var rng = new Random(RandomNum);
        EnemyRegedit[] eliteRegedits =
            SelfCoordinate.X < WeakEnemyStageCount
                ? [new ArroganceRegedit()]
                : [new ArroganceRegedit(), new FerociouessRegedit()];
        List<EnemyRegedit> list = new()
        {
            eliteRegedits[rng.Next(eliteRegedits.Length)].GetRegedit(),
        };
        list[0].PositionIndex = 5;
        return list;
    }

    public List<EnemyRegedit> GetBossEnemies()
    {
        var rng = new Random(RandomNum);
        List<EnemyRegedit> list = new() { new WarRegedit() { PositionIndex = 5 } };
        return list;
    }

    private void UpdateHoverTipIfVisible()
    {
        if (_hoverTip == null || !GodotObject.IsInstanceValid(_hoverTip) || !_hoverTip.Visible)
            return;

        string text = BuildHoverTipText();
        if (string.IsNullOrWhiteSpace(text))
        {
            HideHoverTip();
            return;
        }

        _hoverTip.SetText(text);
        _lastHoverTipText = text;
    }

    private string BuildHoverTipText()
    {
        string summary = GameInfo.GetLevelNodeCompletionSummary(SelfCoordinate);
        string dropPreview = GameInfo.BuildBattleRewardDropPreviewText();
        if (string.IsNullOrWhiteSpace(summary))
        {
            string emptyRecordText = "[b]节点记录[/b]\n该节点暂无完成记录。";
            if (!string.IsNullOrWhiteSpace(dropPreview))
                emptyRecordText += $"\n\n{dropPreview}";
            emptyRecordText = GlobalFunction.ColorizeNumbers(emptyRecordText);
            return GlobalFunction.ColorizeKeywords(emptyRecordText);
        }

        string text = $"[b]节点记录[/b]\n{summary}";
        if (!string.IsNullOrWhiteSpace(dropPreview))
            text += $"\n\n{dropPreview}";

        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private Tip EnsureHoverTip()
    {
        if (_hoverTip != null && GodotObject.IsInstanceValid(_hoverTip))
            return _hoverTip;

        var root = GetTree()?.Root;
        if (root == null || TipScene == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        string tipName = $"LevelNodeTip_{GetInstanceId()}";
        _hoverTip = layer.GetNodeOrNull<Tip>(tipName);
        if (_hoverTip == null)
        {
            _hoverTip = TipScene.Instantiate<Tip>();
            _hoverTip.Name = tipName;
            layer.AddChild(_hoverTip);
        }

        _hoverTip.FollowMouse = true;
        _hoverTip.AnchorOffset = HoverTipOffset;
        return _hoverTip;
    }

    private void TryShowHoverTip()
    {
        if (!_isNodeHovered || State != LevelState.Completed || !IsVisibleInTree())
        {
            HideHoverTip();
            return;
        }

        if (HasVisibleBlockingSiteUi())
        {
            HideHoverTip();
            return;
        }

        var tip = EnsureHoverTip();
        if (tip == null)
            return;

        if (tip.Visible)
            return;

        string text = BuildHoverTipText();
        if (string.IsNullOrWhiteSpace(text))
        {
            HideHoverTip();
            return;
        }

        if (!string.Equals(_lastHoverTipText, text, StringComparison.Ordinal))
        {
            tip.SetText(text);
            _lastHoverTipText = text;
        }
    }

    private void HideHoverTip()
    {
        if (_hoverTip != null && GodotObject.IsInstanceValid(_hoverTip))
            _hoverTip.HideTooltip();

        _lastHoverTipText = null;
    }

    private void DisposeHoverTip()
    {
        if (_hoverTip != null && GodotObject.IsInstanceValid(_hoverTip))
            _hoverTip.QueueFree();

        _hoverTip = null;
        _lastHoverTipText = null;
    }

    private bool HasVisibleBlockingSiteUi()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return false;

        var map = root.GetNodeOrNull<Map>("Map") ?? root.GetNodeOrNull<Map>("/root/Map");
        if (map?.IsMapInteractionBlocked() == true)
            return true;

        var siteUiLayer =
            root.GetNodeOrNull<CanvasLayer>("Map/SiteUI")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/SiteUI");
        var frontUiLayer =
            root.GetNodeOrNull<CanvasLayer>("Map/BattleReadyLayer")
            ?? root.GetNodeOrNull<CanvasLayer>("/root/Map/BattleReadyLayer");

        return LayerHasVisibleChildren(siteUiLayer) || LayerHasVisibleChildren(frontUiLayer);
    }

    private static bool LayerHasVisibleChildren(CanvasLayer layer)
    {
        if (layer == null)
            return false;

        foreach (Node child in layer.GetChildren())
        {
            if (child == null || child.IsQueuedForDeletion())
                continue;

            if (child is CanvasItem canvasItem)
            {
                if (canvasItem.Visible)
                    return true;
                continue;
            }

            return true;
        }

        return false;
    }
}
