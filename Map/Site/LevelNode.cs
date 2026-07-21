using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class LevelNode : ColorRect
{
    private static readonly Color CompletedInnerColor = Colors.White;
    private const float RegionTwoEliteStatMultiplier = 1f;
    private const float RegionTwoEliteMaxLifeMultiplier = 1f;
    internal const float RegionTwoBossStatMultiplier = 1f;
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
        Rest,
        Treasure,
    }

    // public List<EnemyRegedit> EnemiesRegeditList;
    public LevelState State { get; set; }
    public LevelType Type { get; set; } = LevelType.Normal;
    public List<EnemyRegedit> EnemiesRegeditList;
    public List<string> PlayerDamageSummaryLines = new();
    public int PlayerTotalTurnCount;
    public int EnemyTotalTurnCount;
    public Button Button => field ??= GetNode("Button") as Button;

    // public ProgressBar ProgressBar => field ??= GetNode("ProgressBar") as ProgressBar;
    public ShaderMaterial mat;
    private ShaderMaterial _ghostMat;
    private const float LockedTintStrength = 0.98f;
    private const float LockedAmbient = 0.9f;
    private const float LockedAlpha = 0.95f;
    public List<LevelNode> NextNodes = new List<LevelNode>();
    public List<LevelNode> ParentNodes = new List<LevelNode>();
    public static PackedScene BattleScene = GD.Load<PackedScene>("res://battle/Battle.tscn");
    public static PackedScene EventScene = GD.Load<PackedScene>("res://Event/EventInterface.tscn");
    public Vector2I SelfCoordinate;
    public ColorRect Ghost => field ??= GetNode<ColorRect>("ghost");
    public AnimationPlayer AnimationPlayer =>
        field ??= GetNode("AnimationPlayer") as AnimationPlayer;
    public int RandomNum;
    public int ContentQueueIndex = -1;
    public int RegionBattleQueueIndex = -1;
    public int NormalBattleVisitIndex = -1;
    public int EliteBattleVisitIndex = -1;
    public int RelicQueueStart = -1;
    public int RelicQueueSlot = -1;
    public int BattleEntryCount;
    private bool _isNodeHovered;
    private bool _isButtonHovered;
    private bool _isTypeLegendHighlighted;
    private string _lastHoverTipText;
    private Tween _hoverScaleTween;
    private Tween _mainVisualScaleTween;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        mat = Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Material = mat;
        mat.SetShaderParameter("show_inner", false);
        if (Ghost.Material is ShaderMaterial ghostMaterial)
        {
            _ghostMat = ghostMaterial.Duplicate() as ShaderMaterial;
            _ghostMat.ResourceLocalToScene = true;
            Ghost.Material = _ghostMat;
            _ghostMat.SetShaderParameter("show_inner", false);
        }

        ApplyTypeVisualStyle();
        if (State == LevelState.Locked)
            ApplyLockedVisuals();
        else if (State == LevelState.Unlocked)
            Color = 2 * Colors.White;

        Button.Disabled = true;
        Button.Disabled = State == LevelState.Locked;
        StartAnimation();
        Button.MouseEntered += () =>
        {
            _isButtonHovered = true;
            Ghost.Modulate = new Color(1, 1, 1, 0.76f);
            Ghost.Scale = new Vector2(1f, 1f);
            TweenHoverScale(new Vector2(1.2f, 1.2f), 0.2f);
        };
        Button.MouseExited += () =>
        {
            _isButtonHovered = false;
            if (IsAnimate == true)
                return;
            if (_isTypeLegendHighlighted)
                return;
            Ghost.Modulate = new Color(1, 1, 1, 0);
            TweenHoverScale(Vector2.One, 0.2f);
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
            Ghost.Modulate = new Color(1f, 1f, 1f, 0.78f);
            Ghost.Scale = new Vector2(1.1f, 1.1f);
            Modulate = new Color(1.28f, 1.28f, 1.28f, 1f);
            TweenHoverScale(new Vector2(1.1f, 1.1f), 0.16f);
            return;
        }

        Modulate = Colors.White;
        if (_isButtonHovered)
        {
            Ghost.Modulate = new Color(1, 1, 1, 0.76f);
            Ghost.Scale = Vector2.One;
            TweenHoverScale(new Vector2(1.2f, 1.2f), 0.12f);
            return;
        }

        Ghost.Modulate = new Color(1, 1, 1, 0);
        Ghost.Scale = Vector2.One;
        TweenHoverScale(Vector2.One, 0.16f);
    }

    public List<EnemyRegedit> ProduceEnemies(bool allocateVisitIndex = true)
    {
        List<EnemyRegedit> list = Type switch
        {
            LevelType.Normal => GetNormalEnemies(allocateVisitIndex),
            LevelType.Elite => GetEliteEnemies(allocateVisitIndex),
            LevelType.Boss => GetBossEnemies(),
            _ => GetEliteEnemies(allocateVisitIndex),
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
        switch (State)
        {
            case LevelState.Locked:
                ApplyLockedVisuals();
                break;

            case LevelState.Unlocked:
                Color = 2 * new Color(1, 1, 1, 1);
                Button.Disabled = false;
                ApplyTypeVisualStyle();
                break;

            case LevelState.Completed:
                Button.Disabled = true;
                ApplyCompletedVisuals();
                break;
        }
    }

    public void ApplyLockedVisuals()
    {
        ApplyTypeVisualStyle();
        Color = BuildLockedModulate(GetTypeRingColor());
        Button.Disabled = true;
    }

    private static Color BuildLockedModulate(Color typeColor)
    {
        return new Color(
            Mathf.Clamp(typeColor.R * LockedTintStrength + LockedAmbient, 0f, 1f),
            Mathf.Clamp(typeColor.G * LockedTintStrength + LockedAmbient, 0f, 1f),
            Mathf.Clamp(typeColor.B * LockedTintStrength + LockedAmbient, 0f, 1f),
            LockedAlpha
        );
    }

    public void ApplyCompletedVisuals()
    {
        // Only the visual effects, no state changes
        Color = 2 * new Color(1, 1, 1, 1);
        SetNodeShaderStyle(GetTypeRingColor(), CompletedInnerColor, true, GetTypePolygonSides());
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
        var completionTween = ExplodeAnimation();
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

            CompleteRunAfterFinalBoss(completionTween);
            return;
        }

        SaveSystem.SaveRunCheckpoint();
    }

    private void CompleteRunAfterFinalBoss(Tween completionTween)
    {
        GameInfo.RecordCurrentRunHistory(victory: true, includeCurrentNode: false);
        SaveSystem.SaveRunCheckpoint(background: false);
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
        ApplyTypeVisualStyle();
        if (State == LevelState.Locked)
            Color = BuildLockedModulate(GetTypeRingColor());
    }

    private void ApplyTypeVisualStyle()
    {
        Color ringColor = GetTypeRingColor();
        SetNodeShaderStyle(ringColor, ringColor, false, GetTypePolygonSides());
    }

    private Color GetTypeRingColor()
    {
        var levelProgress = GetParent()?.GetParent<LevelProgress>();
        if (levelProgress != null)
            return levelProgress.GetNodeTypeRingColor(Type);

        return Type switch
        {
            LevelType.Boss => new Color("#9900E6"),
            LevelType.Elite => new Color("#FF1A1A"),
            LevelType.Event => new Color("#0099FF"),
            LevelType.Shop => new Color("#FFD62E"),
            LevelType.Rest => new Color("#1AE675"),
            LevelType.Treasure => new Color("#FFB020"),
            _ => new Color("#FFFFFF"),
        };
    }

    private float GetTypePolygonSides()
    {
        return Type switch
        {
            LevelType.Event => 5f,
            LevelType.Shop => 4f,
            LevelType.Rest => 3f,
            LevelType.Treasure => 4f,
            _ => 6f,
        };
    }

    private void SetNodeShaderStyle(
        Color ringColor,
        Color innerColor,
        bool showInner,
        float polygonSides
    )
    {
        bool showChest = Type == LevelType.Treasure;
        ApplyNodeShaderStyle(mat, ringColor, innerColor, showInner, polygonSides, showChest);
        ApplyNodeShaderStyle(_ghostMat, ringColor, innerColor, showInner, polygonSides, showChest);
    }

    private static void ApplyNodeShaderStyle(
        ShaderMaterial shader,
        Color ringColor,
        Color innerColor,
        bool showInner,
        float polygonSides,
        bool showChestIcon = false
    )
    {
        if (shader == null)
            return;

        shader.SetShaderParameter("ring_color", ringColor);
        shader.SetShaderParameter("inner_color", innerColor);
        shader.SetShaderParameter("show_inner", showInner);
        shader.SetShaderParameter("polygon_sides", polygonSides);
        shader.SetShaderParameter("show_chest_icon", showChestIcon);
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
                GotoBattle();
                break;
            case LevelType.Boss:
                GotoBattle();
                break;
            case LevelType.Elite:
                GotoBattle();
                break;
            case LevelType.Event:
                GotoEvent();
                break;
            case LevelType.Shop:
                GotoShop();
                break;
            case LevelType.Rest:
                GotoRest();
                break;
            case LevelType.Treasure:
                GotoTreasure();
                break;
        }
    }

    public async void GotoBattle()
    {
        GameInfo.BeginLevelNodeTracking(this);
        var tween = ExplodeAnimation();
        EnsureBattleEncounter();
        int battleRandomNum = NextBattleRandomNum();
        RandomizePlayerPreviewPositions(battleRandomNum);
        var prewarmTask = BattleStartResourcePreloader.PrewarmBattleNodeAsync(
            this,
            this,
            allocateEncounter: false
        );
        SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);
        if (transitionLayer != null)
            await transitionLayer.FadeToBlackAsync(0.4f);
        if (tween != null && GodotObject.IsInstanceValid(tween) && tween.IsRunning())
            await ToSignal(tween, Tween.SignalName.Finished);
        await prewarmTask;
        if (!GodotObject.IsInstanceValid(this))
        {
            if (transitionLayer != null && GodotObject.IsInstanceValid(transitionLayer))
                await transitionLayer.FadeFromBlackAsync(0.24f);
            return;
        }

        var layer = new CanvasLayer { Layer = 4 };
        GetTree().Root.AddChild(layer);

        var battle = BattleScene?.Instantiate<Battle>();
        if (battle == null)
        {
            layer.QueueFree();
            GetParent()?.GetParent<LevelProgress>()?.UnlockAllNodes();
            if (transitionLayer != null && GodotObject.IsInstanceValid(transitionLayer))
                await transitionLayer.FadeFromBlackAsync(0.24f);
            return;
        }

        battle.CurrentLevelNode = this;
        battle.BattleRandomNum = battleRandomNum;
        layer.AddChild(battle);
        await battle.WhenPresentationReadyAsync();
        if (!GodotObject.IsInstanceValid(battle))
        {
            layer.QueueFree();
            GetParent()?.GetParent<LevelProgress>()?.UnlockAllNodes();
            if (transitionLayer != null && GodotObject.IsInstanceValid(transitionLayer))
                await transitionLayer.FadeFromBlackAsync(0.24f);
            return;
        }

        if (transitionLayer != null && GodotObject.IsInstanceValid(transitionLayer))
            await transitionLayer.FadeFromBlackAsync(0.24f);
    }

    private void EnsureBattleEncounter()
    {
        if (EnemiesRegeditList == null || EnemiesRegeditList.Count == 0)
            EnemiesRegeditList = ProduceEnemies();

        foreach (var enemy in EnemiesRegeditList)
        {
            if (enemy != null && enemy.CurrentLife < 0)
                enemy.CurrentLife = Math.Max(1, enemy.MaxLife);
        }
    }

    private int NextBattleRandomNum()
    {
        BattleEntryCount++;
        return GameInfo.CreateRunRngSeed(
            GameInfo.GetStreamForNodeType(Type),
            GameInfo.GetBattleRngQueueIndex(this),
            BattleEntryCount ^ GameInfo.BattleFormationSalt
        );
    }

    private void RandomizePlayerPreviewPositions(int battleRandomNum)
    {
        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return;

        var pickedPositions = Enumerable.Range(1, GameInfo.PlayerCharacters.Length).ToList();
        ShuffleInPlace(pickedPositions, new Random(battleRandomNum));

        if (IsSameFormationPattern(pickedPositions, EnemiesRegeditList))
            OffsetFormationPattern(pickedPositions);

        for (int i = 0; i < pickedPositions.Count; i++)
            GameInfo.PlayerCharacters[i].PositionIndex = pickedPositions[i];
    }

    public void GotoEvent()
    {
        GameInfo.BeginLevelNodeTracking(this);
        var rng = GameInfo.CreateRunRng(this, GameInfo.EventContentSalt);
        OpenEventInterface(GameEvent.Catalog[rng.Next(0, GameEvent.Catalog.Length)]);
    }

    private void OpenEventInterface(GameEvent gameEvent)
    {
        var gameEventInterface = EventScene.Instantiate() as EventInterface;
        if (gameEventInterface == null)
        {
            Completed();
            return;
        }

        gameEventInterface.WhichNode = this;
        gameEventInterface.ThisEvent = gameEvent;
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

    public void GotoRest()
    {
        GameInfo.BeginLevelNodeTracking(this);
        GetParent()?.GetParent<LevelProgress>()?.OnNodeSelected(this);
        OpenEventInterface(GameEvent.BuildRestSite(this));
    }

    public void GotoTreasure()
    {
        GameInfo.BeginLevelNodeTracking(this);
        GetParent()?.GetParent<LevelProgress>()?.OnNodeSelected(this);
        OpenEventInterface(GameEvent.BuildTreasureChest(this));
    }

    public Tween ExplodeAnimation()
    {
        IsAnimate = true;
        _hoverScaleTween?.Kill();
        Scale = Vector2.One;
        Ghost.Modulate = new Color(1, 1, 1, 1);
        Ghost.Scale = Vector2.One;
        Tween tween = CreateTween();
        tween.TweenProperty(Ghost, "scale", new Vector2(2.2f, 2.2f), 0.3f);
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

    private void TweenHoverScale(Vector2 targetScale, float duration)
    {
        _hoverScaleTween?.Kill();
        _hoverScaleTween = CreateTween();
        _hoverScaleTween
            .TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    public static void RandomPosition<T>(List<T> list, int RandomNum)
        where T : EnemyRegedit
    {
        var positions = Enumerable.Range(1, Battle.MaxEnemyFormationSlots).ToList();

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

            int posIndex = 0;
            enemy.PositionIndex = positions[posIndex];
            positions.RemoveAt(posIndex);
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

    private static void ShuffleInPlace<T>(IList<T> list, Random rng)
    {
        if (list == null || rng == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    public List<EnemyRegedit> GetNormalEnemies(bool allocateVisitIndex = true)
    {
        int visitIndex = allocateVisitIndex
            ? GameInfo.ResolveNormalBattleVisitIndex(this)
            : GameInfo.PeekNormalBattleVisitIndex(this);
        int formationIndex = GameInfo.GetNormalEncounterFormationIndex(visitIndex);
        return NormalBattleEncounter.BuildFormation(GameInfo.CurrentLevel, formationIndex);
    }

    public List<EnemyRegedit> GetEliteEnemies(bool allocateVisitIndex = true)
    {
        int visitIndex = allocateVisitIndex
            ? GameInfo.ResolveEliteBattleVisitIndex(this)
            : GameInfo.PeekEliteBattleVisitIndex(this);
        int catalogIndex = GameInfo.GetEliteEncounterCatalogIndex(visitIndex);
        EnemyRegedit elite = EliteBattleEncounter.BuildElite(GameInfo.CurrentLevel, catalogIndex);
        List<EnemyRegedit> list = new() { elite };
        list[0].PositionIndex = Battle.EnemyCenterFormationSlot;
        if (GameInfo.CurrentLevel > 0)
            ApplyEliteRegionTwoMultiplier(list[0]);
        return list;
    }

    public List<EnemyRegedit> GetBossEnemies()
    {
        EnemyRegedit boss = PickBossForThisNode();
        boss.PositionIndex =
            boss is WarRegedit ? Battle.MaxEnemyFormationSlots : Battle.EnemyCenterFormationSlot;
        if (GameInfo.CurrentLevel > 0)
            ApplyStatMultiplier(boss, RegionTwoBossStatMultiplier);
        List<EnemyRegedit> list = new() { boss };
        return list;
    }

    private EnemyRegedit PickBossForThisNode()
    {
        EnemyRegedit[] bossCatalog =
            GameInfo.CurrentLevel > 0
                ? [new DeathRegedit()]
                : [new WarRegedit(), new HavocRegedit()];
        var rng = GameInfo.CreateRunRng(this);
        EnemyRegedit boss = bossCatalog[rng.Next(bossCatalog.Length)];
        return boss.GetRegedit();
    }

    private static EnemyRegedit[] BuildBossCatalog()
    {
        return [new WarRegedit(), new HavocRegedit(), new DeathRegedit()];
    }

    private static HashSet<string> GetDefeatedBossNames()
    {
        return GameInfo
                .CompletedLevelNodeRecords?.Values.Where(record =>
                    record != null && record.NodeType == LevelType.Boss
                )
                .SelectMany(record => record.EnemyNames ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(GetEnemyIdentity)
                .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    private static string GetBossIdentity(EnemyRegedit boss) => GetEnemyIdentity(boss);

    private static string GetEnemyIdentity(EnemyRegedit enemy) =>
        GetEnemyIdentity(enemy?.CharacterName);

    private static string GetEnemyIdentity(string name) =>
        string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();

    private static void ApplyStatMultiplier(EnemyRegedit enemy, float multiplier)
    {
        if (enemy == null)
            return;

        enemy.Power = ScaleStat(enemy.Power, multiplier);
        enemy.Survivability = ScaleStat(enemy.Survivability, multiplier);
        enemy.BasePowerContribution = ScaleStat(enemy.BasePowerContribution, multiplier);
        enemy.BaseSurvivabilityContribution = ScaleStat(
            enemy.BaseSurvivabilityContribution,
            multiplier
        );
        enemy.MaxLife = ScaleStat(enemy.MaxLife, multiplier);
    }

    private static void ApplyEliteRegionTwoMultiplier(EnemyRegedit enemy)
    {
        if (enemy == null)
            return;

        enemy.Power = ScaleStat(enemy.Power, RegionTwoEliteStatMultiplier);
        enemy.Survivability = ScaleStat(enemy.Survivability, RegionTwoEliteStatMultiplier);
        enemy.BasePowerContribution = ScaleStat(
            enemy.BasePowerContribution,
            RegionTwoEliteStatMultiplier
        );
        enemy.BaseSurvivabilityContribution = ScaleStat(
            enemy.BaseSurvivabilityContribution,
            RegionTwoEliteStatMultiplier
        );
        enemy.MaxLife = ScaleStat(enemy.MaxLife, RegionTwoEliteMaxLifeMultiplier);
    }

    private static int ScaleStat(int value, float multiplier)
    {
        if (value <= 0)
            return value;

        return Math.Max(1, Mathf.CeilToInt(value * multiplier));
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

            string emptyRecordText = I18n.Tr(
                "ui.map.node_record_empty",
                "[b]节点记录[/b]\n该节点暂无完成记录。"
            );
            if (!string.IsNullOrWhiteSpace(dropPreview))
                emptyRecordText += $"\n\n{dropPreview}";
            return ColorizeHoverTipText(emptyRecordText);
        }

        string text = string.IsNullOrWhiteSpace(bossPreview)
            ? I18n.Format(
                "ui.map.node_record_summary",
                "[b]节点记录[/b]\n{summary}",
                ("summary", summary)
            )
            : I18n.Format(
                "ui.map.node_record_with_boss",
                "{boss}\n\n[b]节点记录[/b]\n{summary}",
                ("boss", bossPreview),
                ("summary", summary)
            );

        return ColorizeHoverTipText(text);
    }

    private string BuildBossPreviewText()
    {
        if (Type != LevelType.Boss)
            return string.Empty;

        EnemyRegedit boss = GetBossEnemies().FirstOrDefault();
        string bossName = string.IsNullOrWhiteSpace(boss?.CharacterName)
            ? I18n.Tr("ui.common.unknown", "未知")
            : boss.CharacterName;
        return I18n.Format(
            "ui.map.boss_preview",
            "[b]Boss[/b]\n即将遭遇：[color=#ff6b8b]{name}[/color]",
            ("name", bossName)
        );
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
        if (map?.IsMapInteractionBlocked() == true && !map.IsMapPeekModeActive)
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
