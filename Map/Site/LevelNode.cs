using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelNode : ColorRect
{
    private const int WeakEnemyStageCount = 3;
    private const float RegionTwoEliteStatMultiplier = 1.3f;
    internal const float RegionTwoBossStatMultiplier = 1.5f;
    private const int PlayerFormationRandomSalt = unchecked((int)0x51f15e0d);
    private const int EnemyFormationRandomSalt = unchecked((int)0x2a7f3c19);
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
    private bool _isButtonHovered;
    private bool _isTypeLegendHighlighted;
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
            _isButtonHovered = true;
            Ghost.Modulate = new Color(1, 1, 1, 1);
            Ghost.Scale = new Vector2(1f, 1f);
            CreateTween().TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.2f);
        };
        Button.MouseExited += () =>
        {
            _isButtonHovered = false;
            if (IsAnimate == true)
                return;
            if (_isTypeLegendHighlighted)
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

        if (!ShouldShowHoverTip() || HasVisibleBlockingSiteUi())
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

    public void SetTypeLegendHighlighted(bool highlighted)
    {
        _isTypeLegendHighlighted = highlighted;

        if (IsAnimate)
            return;

        if (highlighted)
        {
            Ghost.Modulate = new Color(1f, 1f, 1f, 0.92f);
            Ghost.Scale = new Vector2(1.1f, 1.1f);
            Modulate = new Color(1.28f, 1.28f, 1.28f, 1f);
            CreateTween()
                .TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.16f)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            return;
        }

        Modulate = Colors.White;
        if (_isButtonHovered)
        {
            Ghost.Modulate = new Color(1, 1, 1, 1);
            Ghost.Scale = Vector2.One;
            CreateTween().TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.12f);
            return;
        }

        Ghost.Modulate = new Color(1, 1, 1, 0);
        Ghost.Scale = Vector2.One;
        CreateTween()
            .TweenProperty(this, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
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
        bool isBoss = Type == LevelType.Boss;
        var levelProgress = GetParent().GetParent<LevelProgress>();

        if (!isBoss)
        {
            foreach (var node in NextNodes)
            {
                if (node.State == LevelState.Locked)
                    node.Unlock();
            }
        }
        ExplodeAnimation();
        levelProgress?.OnNodeSelected(this);
        State = LevelState.Completed;
        GameInfo.FirstLevelState[SelfCoordinate] = LevelState.Completed;
        Button.Disabled = true;
        ApplyCompletedVisuals();
        GameInfo.CompleteLevelNodeTracking(this);
        UpdateHoverTipIfVisible();
        levelProgress?.UnlockAllNodes();

        if (isBoss)
        {
            if (GameInfo.CurrentLevel <= 0 && levelProgress != null)
            {
                levelProgress.AdvanceToNextRegion();
                return;
            }

            CompleteRunAfterFinalBoss();
            return;
        }

        SaveSystem.SaveAll();
    }

    private void CompleteRunAfterFinalBoss()
    {
        GameInfo.RecordCurrentRunHistory(victory: true, includeCurrentNode: false);
        SaveSystem.SaveAll();
        GameOverSummary.Show(this);
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
        RandomizePlayerPreviewPositions();
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

    private void RandomizePlayerPreviewPositions()
    {
        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return;

        var positions = Enumerable.Range(1, 9).ToList();
        Random rng = new Random(HashFormationSeed(RandomNum, PlayerFormationRandomSalt));
        var pickedPositions = new List<int>(GameInfo.PlayerCharacters.Length);

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            if (positions.Count == 0)
                break;

            int positionPickIndex = rng.Next(positions.Count);
            pickedPositions.Add(positions[positionPickIndex]);
            positions.RemoveAt(positionPickIndex);
        }

        if (IsSameFormationPattern(pickedPositions, EnemiesRegeditList))
            OffsetFormationPattern(pickedPositions);

        for (int i = 0; i < pickedPositions.Count; i++)
            GameInfo.PlayerCharacters[i].PositionIndex = pickedPositions[i];
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
        Random random = new Random(HashFormationSeed(RandomNum, EnemyFormationRandomSalt));
        var positions = Enumerable.Range(1, 9).ToList();

        foreach (var enemy in list)
        {
            if (enemy == null || enemy.PositionIndex <= 0)
                continue;

            positions.Remove(enemy.PositionIndex);
        }

        foreach (var enemy in list)
        {
            if (enemy == null)
                continue;
            if (enemy.PositionIndex > 0)
                continue;
            if (positions.Count == 0)
                break;

            int posIndex = random.Next(positions.Count);
            enemy.PositionIndex = positions[posIndex];
            positions.RemoveAt(posIndex);
        }
    }

    private static int HashFormationSeed(int baseSeed, int salt)
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash ^ baseSeed) * 16777619;
            hash = (hash ^ salt) * 16777619;
            return hash;
        }
    }

    private static bool IsSameFormationPattern(
        IReadOnlyList<int> playerPositions,
        IReadOnlyList<EnemyRegedit> enemies
    )
    {
        if (playerPositions == null || enemies == null || playerPositions.Count != enemies.Count)
            return false;

        for (int i = 0; i < playerPositions.Count; i++)
        {
            if (enemies[i] == null || enemies[i].PositionIndex != playerPositions[i])
                return false;
        }

        return true;
    }

    private static void OffsetFormationPattern(List<int> positions)
    {
        if (positions == null || positions.Count <= 1)
            return;

        int first = positions[0];
        positions.RemoveAt(0);
        positions.Add(first);
    }

    public List<EnemyRegedit> GetNormalEnemies()
    {
        var rng = new Random(RandomNum);
        EnemyRegedit[] weakEnemyRegedits = BuildWeakEnemyCatalogForCurrentRegion();
        EnemyRegedit[] strongEnemyRegedits = BuildStrongEnemyCatalogForCurrentRegion();

        List<EnemyRegedit> list = new()
        {
            weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit(),
            weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit(),
        };
        if (SelfCoordinate.X >= WeakEnemyStageCount)
        {
            bool useStrongFormation = rng.Next(0, 2) == 0;
            if (useStrongFormation)
            {
                list.Add(weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit());
                list.Add(strongEnemyRegedits[rng.Next(strongEnemyRegedits.Length)].GetRegedit());
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    list.Add(weakEnemyRegedits[rng.Next(weakEnemyRegedits.Length)].GetRegedit());
                }
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
        List<EnemyRegedit> list = new() { new ArroganceRegedit() };
        list[0].PositionIndex = 5;
        if (GameInfo.CurrentLevel > 0)
            ApplyStatMultiplier(list[0], RegionTwoEliteStatMultiplier);
        return list;
    }

    public List<EnemyRegedit> GetBossEnemies()
    {
        EnemyRegedit boss = PickBossForThisNode();
        boss.PositionIndex = 5;
        if (GameInfo.CurrentLevel > 0)
            ApplyStatMultiplier(boss, RegionTwoBossStatMultiplier);
        List<EnemyRegedit> list = new() { boss };
        return list;
    }

    private EnemyRegedit PickBossForThisNode()
    {
        EnemyRegedit[] bossPool = BuildBossCatalog();
        HashSet<string> defeatedBossNames = GetDefeatedBossNames();
        EnemyRegedit[] availableBosses = bossPool
            .Where(boss => boss != null && !defeatedBossNames.Contains(GetBossIdentity(boss)))
            .ToArray();

        EnemyRegedit[] pickPool = availableBosses.Length > 0 ? availableBosses : bossPool;
        if (pickPool.Length == 0)
            return new WarRegedit();

        int index = (int)(Math.Abs((long)RandomNum) % pickPool.Length);
        return pickPool[index].GetRegedit();
    }

    private static EnemyRegedit[] BuildBossCatalog()
    {
        return [new WarRegedit(), new DeathRegedit()];
    }

    private static HashSet<string> GetDefeatedBossNames()
    {
        return GameInfo
                .CompletedLevelNodeRecords?.Values.Where(record =>
                    record != null && record.NodeType == LevelType.Boss
                )
                .SelectMany(record => record.EnemyNames ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(NormalizeBossIdentity)
                .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    private static string GetBossIdentity(EnemyRegedit boss) =>
        NormalizeBossIdentity(boss?.CharacterName);

    private static string NormalizeBossIdentity(string name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();

    private static void ApplyStatMultiplier(EnemyRegedit enemy, float multiplier)
    {
        if (enemy == null)
            return;

        enemy.Power = ScaleStat(enemy.Power, multiplier);
        enemy.Survivability = ScaleStat(enemy.Survivability, multiplier);
        enemy.Speed = ScaleStat(enemy.Speed, multiplier);
        enemy.MaxLife = ScaleStat(enemy.MaxLife, multiplier);
    }

    private static int ScaleStat(int value, float multiplier)
    {
        if (value <= 0)
            return value;

        return Math.Max(1, Mathf.CeilToInt(value * multiplier));
    }

    private static EnemyRegedit[] BuildWeakEnemyCatalogForCurrentRegion()
    {
        return GameInfo.CurrentLevel > 0
            ? BuildRegionTwoWeakEnemyCatalog()
            : BuildRegionOneWeakEnemyCatalog();
    }

    private static EnemyRegedit[] BuildStrongEnemyCatalogForCurrentRegion()
    {
        return GameInfo.CurrentLevel > 0
            ? BuildRegionTwoStrongEnemyCatalog()
            : BuildRegionOneStrongEnemyCatalog();
    }

    private static EnemyRegedit[] BuildRegionOneWeakEnemyCatalog()
    {
        return [new EvilRegedit(), new FearWormRegedit(), new AlienBodyRegedit()];
    }

    private static EnemyRegedit[] BuildRegionOneStrongEnemyCatalog()
    {
        return [new FerociouessRegedit(), new BlackHawkRegedit(), new InexorabilityRegedit()];
    }

    private static EnemyRegedit[] BuildRegionTwoWeakEnemyCatalog()
    {
        return
        [
            new ArmonRegedit(),
            new RedHuskRegedit(),
            new TurbineRegedit(),
            new VoidAcolyteRegedit(),
            new HollowBulwarkRegedit(),
        ];
    }

    private static EnemyRegedit[] BuildRegionTwoStrongEnemyCatalog()
    {
        return [new GraveWraithRegedit(), new MarrowReaverRegedit()];
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
        string bossPreview = BuildBossPreviewText();
        string summary = GameInfo.GetLevelNodeCompletionSummary(SelfCoordinate);
        string dropPreview = GameInfo.BuildBattleRewardDropPreviewText();
        if (string.IsNullOrWhiteSpace(summary))
        {
            if (!string.IsNullOrWhiteSpace(bossPreview))
                return ColorizeHoverTipText(bossPreview);

            string emptyRecordText = "[b]节点记录[/b]\n该节点暂无完成记录。";
            if (!string.IsNullOrWhiteSpace(dropPreview))
                emptyRecordText += $"\n\n{dropPreview}";
            return ColorizeHoverTipText(emptyRecordText);
        }

        string text = string.IsNullOrWhiteSpace(bossPreview)
            ? $"[b]节点记录[/b]\n{summary}"
            : $"{bossPreview}\n\n[b]节点记录[/b]\n{summary}";

        return ColorizeHoverTipText(text);
    }

    private string BuildBossPreviewText()
    {
        if (Type != LevelType.Boss)
            return string.Empty;

        EnemyRegedit boss = GetBossEnemies().FirstOrDefault();
        string bossName = string.IsNullOrWhiteSpace(boss?.CharacterName)
            ? "未知"
            : boss.CharacterName;
        return $"[b]Boss[/b]\n即将遭遇：[color=#ff6b8b]{bossName}[/color]";
    }

    private static string ColorizeHoverTipText(string text)
    {
        text = GlobalFunction.ColorizeNumbers(text);
        return GlobalFunction.ColorizeKeywords(text);
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
        if (!ShouldShowHoverTip())
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

    private bool ShouldShowHoverTip()
    {
        return _isNodeHovered
            && IsVisibleInTree()
            && (State == LevelState.Completed || Type == LevelType.Boss);
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

            if (child is CanvasLayer canvasLayer)
            {
                if (canvasLayer.Visible)
                    return true;
                continue;
            }

            if (child is CanvasItem canvasItem)
            {
                if (canvasItem.IsVisibleInTree())
                    return true;
                continue;
            }
        }

        return false;
    }
}
