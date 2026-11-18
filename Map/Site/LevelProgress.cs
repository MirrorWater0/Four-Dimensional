using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class LevelProgress : Control
{
    private const int RegionOneMapLength = 13;
    private const int RegionTwoMapLength = 12;
    private static int MapLength => GameInfo.CurrentLevel > 0 ? RegionTwoMapLength : RegionOneMapLength;
    private const float NodeSpacingX = 250f;
    private const float MapLeftMargin = 200f;
    private const int RoadSamplesPerStage = 24;
    public const float RestHealPercent = 0.3f;
    public const float RestSingleHealPercent = 0.6f;

    [ExportGroup("Node Colors")]
    [Export]
    public Color NormalNodeColor = new("#FFFFFF");

    [Export]
    public Color EventNodeColor = new("#0099FF");

    [Export]
    public Color ShopNodeColor = new("#FFD62E");

    [Export]
    public Color RestNodeColor = new("#1AE675");

    [Export]
    public Color EliteNodeColor = new("#FF1A1A");

    [Export]
    public Color BossNodeColor = new("#9900E6");

    [Export]
    public Color TreasureNodeColor = new("#FFB020");

    public Color GetNodeTypeRingColor(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Boss => BossNodeColor,
            LevelNode.LevelType.Elite => EliteNodeColor,
            LevelNode.LevelType.Event => EventNodeColor,
            LevelNode.LevelType.Shop => ShopNodeColor,
            LevelNode.LevelType.Rest => RestNodeColor,
            LevelNode.LevelType.Treasure => TreasureNodeColor,
            _ => NormalNodeColor,
        };
    }
    private static readonly LevelNode.LevelType[] ProbabilityDisplayTypes =
    [
        LevelNode.LevelType.Normal,
        LevelNode.LevelType.Event,
        LevelNode.LevelType.Shop,
        LevelNode.LevelType.Rest,
        LevelNode.LevelType.Elite,
        LevelNode.LevelType.Treasure,
    ];

    private PackedScene _nodeScene => GD.Load<PackedScene>("res://Map/Site/LevelNode.tscn");
    private readonly List<List<LevelNode>> _mapNodes = new();
    private readonly Dictionary<(LevelNode, LevelNode), Line2D> _paths = new();
    private Control _nodeContainer;
    private Control _connectionContainer;
    private CanvasLayer _siteUiLayer;
    private Map _map;
    private bool _manualLock;
    private bool _manualLockSawBlockingUi;
    private bool _interactionBlocked;

    public int RoadStageCount => MapLength;
    public LevelNode NextRoadNode
    {
        get
        {
            foreach (var layer in _mapNodes)
                foreach (var node in layer)
                    if (node.State == LevelNode.LevelState.Unlocked)
                        return node;
            return null;
        }
    }
    public int NextRoadStage => NextRoadNode?.SelfCoordinate.X ?? MapLength;
    public bool IsNextEncounterCommitted
    {
        get
        {
            LevelNode next = NextRoadNode;
            return next != null && GameInfo.WorldLineNodeTypes.ContainsKey(next.SelfCoordinate);
        }
    }
    public bool CanAdvanceAlongRoad() => !GameInfo.RunFinished && !_manualLock && !IsWorldLineTransitioning
        && !IsInteractionBlockedByUi() && NextRoadNode != null;
    public bool CanSwitchWorldLine => CanAdvanceAlongRoad() && !IsNextEncounterCommitted
        && GameInfo.WorldLineSwitchesRemaining > 0;

    public override void _Ready()
    {
        _map = GetParent() as Map;
        if (_map?.WarmupMode == true)
        {
            SetProcess(false);
            return;
        }

        // Allow mouse events to pass through empty space to the underlying Map DragButton
        MouseFilter = MouseFilterEnum.Ignore;

        _connectionContainer = new Control { Name = "RoadConnections" };
        _connectionContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _connectionContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_connectionContainer);

        _nodeContainer = new Control { Name = "RoadNodes" };
        _nodeContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _nodeContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_nodeContainer);

        _siteUiLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("Map/SiteUI");
        EnsureRegionAdvancedFromCompletedBoss();
        GenerateMap();
        RefreshNodeInteractivity();
    }

    public override void _Process(double delta)
    {
        bool interactionBlocked = IsInteractionBlockedByUi();
        if (_manualLock && interactionBlocked)
            _manualLockSawBlockingUi = true;

        bool releasedManualLock = false;
        if (_manualLock && _manualLockSawBlockingUi && !interactionBlocked)
        {
            _manualLock = false;
            _manualLockSawBlockingUi = false;
            releasedManualLock = true;
        }

        bool needsRefresh = HasStaleNodeInteractivity(interactionBlocked);
        if (interactionBlocked == _interactionBlocked && !releasedManualLock && !needsRefresh)
            return;

        _interactionBlocked = interactionBlocked;
        RefreshNodeInteractivity();
    }

    public void LockAllNodes()
    {
        _manualLock = true;
        _manualLockSawBlockingUi = false;
        RefreshNodeInteractivity();
    }

    public void UnlockAllNodes()
    {
        _manualLock = false;
        _manualLockSawBlockingUi = false;
        RefreshRoadPresentations();
        RefreshNodeInteractivity();
        CallDeferred(nameof(RefreshNodeInteractivity));
    }

    /// <summary>
    /// Passive diagnostic for map-node pointer routing. It injects only synthetic mouse-motion
    /// events through the current viewport and never presses a node button.
    /// </summary>
    public Task<IReadOnlyList<MapNodePointerProbe.Result>> RunPointerInputProbeAsync()
    {
        IEnumerable<LevelNode> nodes = _mapNodes
            .SelectMany(layer => layer)
            .Where(node => node != null)
            .ToArray();
        return MapNodePointerProbe.RunAsync(this, nodes);
    }

    private void RefreshNodeInteractivity()
    {
        bool hasChildren = HasVisibleSiteUi();
        bool interactionBlocked = IsInteractionBlockedByUi();
        if (_manualLock && interactionBlocked)
            _manualLockSawBlockingUi = true;

        if (hasChildren)
        {
            _manualLock = false;
        }
        if (!_manualLock)
            _manualLockSawBlockingUi = false;

        _interactionBlocked = interactionBlocked;

        bool shouldDisable = _manualLock || interactionBlocked || IsWorldLineTransitioning;
        foreach (var layer in _mapNodes)
        {
            foreach (var node in layer)
            {
                if (node == null)
                    continue;
                node.Button.Disabled = shouldDisable || node.State != LevelNode.LevelState.Unlocked;
            }
        }
    }

    private bool HasStaleNodeInteractivity(bool interactionBlocked)
    {
        bool shouldDisable = _manualLock || interactionBlocked || IsWorldLineTransitioning;
        foreach (var layer in _mapNodes)
        {
            foreach (var node in layer)
            {
                if (node == null)
                    continue;

                bool expectedDisabled =
                    shouldDisable || node.State != LevelNode.LevelState.Unlocked;
                if (node.Button.Disabled != expectedDisabled)
                    return true;
            }
        }

        return false;
    }

    private bool IsInteractionBlockedByUi()
    {
        _map ??= GetParent() as Map;
        return _map?.IsMapInteractionBlocked() == true || HasVisibleSiteUi();
    }

    private bool HasVisibleSiteUi()
    {
        if (_siteUiLayer == null)
            return false;

        foreach (Node child in _siteUiLayer.GetChildren())
        {
            if (IsVisibleBlockingUiNode(child))
                return true;
        }

        return false;
    }

    private static bool IsVisibleBlockingUiNode(Node node)
    {
        if (node == null || node.IsQueuedForDeletion())
            return false;

        if (node is CanvasLayer canvasLayer)
            return canvasLayer.Visible;

        if (node is CanvasItem canvasItem)
            return canvasItem.IsVisibleInTree();

        foreach (Node child in node.GetChildren())
        {
            if (IsVisibleBlockingUiNode(child))
                return true;
        }

        return false;
    }

    private void GenerateMap()
    {
        GameInfo.PrepareWorldLineMap(MapLength);
        ResetRoadTransition();
        _mapNodes.Clear();
        _paths.Clear();
        _roadSegments.Clear();
        foreach (Node child in _nodeContainer.GetChildren())
        {
            _nodeContainer.RemoveChild(child);
            child.QueueFree();
        }
        foreach (Node child in _connectionContainer.GetChildren())
        {
            _connectionContainer.RemoveChild(child);
            child.QueueFree();
        }

        for (int stage = 0; stage < MapLength; stage++)
        {
            var coordinate = new Vector2I(stage, 0);
            var node = _nodeScene.Instantiate<LevelNode>();
            node.SelfCoordinate = coordinate;
            node.ConcealEncounterType = true;
            var fixedType = GetFixedRoadNodeType(stage);
            node.IsFixedEncounterType = fixedType.HasValue;
            if (fixedType.HasValue)
                node.Type = fixedType.Value;
            node.RandomNum = GameInfo.CreateWorldLineRng(stage, 0).Next();
            node.State = GameInfo.FirstLevelState.TryGetValue(coordinate, out var state)
                ? state : LevelNode.LevelState.Locked;
            if (GameInfo.WorldLineNodeTypes.TryGetValue(coordinate, out var resolvedType))
            {
                // Apply fixed encounters and opening protection to unfinished encounters from older saves.
                node.Type = node.State == LevelNode.LevelState.Completed
                    ? resolvedType : ResolveRoadNodeType(stage, resolvedType);
                GameInfo.WorldLineNodeTypes[coordinate] = node.Type;
            }
            node.Position = GetRoadPosition(stage) - node.Size * 0.5f;
            _nodeContainer.AddChild(node);
            GameInfo.FirstLevelState[coordinate] = node.State;
            _mapNodes.Add(new List<LevelNode> { node });
            node.ApplyLoadedState();

            if (stage > 0)
            {
                LevelNode previous = _mapNodes[stage - 1][0];
                previous.NextNodes.Add(node);
                node.ParentNodes.Add(previous);
                CreateRoadSegment(previous, node);
            }
        }

        // Only the first unfinished stage can be entered, including after reload.
        bool foundNext = false;
        foreach (LevelNode node in _mapNodes.SelectMany(layer => layer))
        {
            if (node.State == LevelNode.LevelState.Completed)
                continue;
            if (!foundNext)
            {
                node.Unlock();
                foundNext = true;
            }
            else
            {
                node.State = LevelNode.LevelState.Locked;
                GameInfo.FirstLevelState[node.SelfCoordinate] = node.State;
                node.ApplyLockedVisuals();
            }
        }
        AssignContentQueueIndices();
        RefreshRoadColors();
        RefreshRoadPresentations();
    }

    private void CreateRoadSegment(LevelNode from, LevelNode to)
    {
        var points = new Vector2[RoadSamplesPerStage + 1];
        for (int sample = 0; sample <= RoadSamplesPerStage; sample++)
            points[sample] = GetRoadPosition(from.SelfCoordinate.X + sample / (float)RoadSamplesPerStage);

        var road = new Line2D
        {
            Name = $"Road{from.SelfCoordinate.X}",
            Points = points,
            Width = 4f,
            Antialiased = true,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        _connectionContainer.AddChild(road);
        var travelled = new Line2D
        {
            Name = $"Travelled{from.SelfCoordinate.X}",
            Width = 5f,
            Antialiased = true,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        _connectionContainer.AddChild(travelled);
        _paths[(from, to)] = travelled;
        _roadSegments.Add(new RoadSegment(from.SelfCoordinate.X, road, travelled,
            to.State == LevelNode.LevelState.Completed ? 1f : 0f));
        if (to.State == LevelNode.LevelState.Completed)
            travelled.Points = points;
    }

    private void RefreshRoadColors()
    {
        Color color = WorldLineVisuals.GetColor(_roadFromLine).Lerp(
            WorldLineVisuals.GetColor(GameInfo.CurrentWorldLine), _roadMorphProgress);
        foreach (RoadSegment segment in _roadSegments)
        {
            segment.Road.DefaultColor = new Color(color, 0.45f);
            segment.Travelled.DefaultColor = color;
        }
    }

    public bool TryPrepareNodeEntry(LevelNode node)
    {
        if (node != NextRoadNode || !CanAdvanceAlongRoad())
            return false;

        bool hasSavedType = GameInfo.WorldLineNodeTypes.TryGetValue(node.SelfCoordinate, out var savedType);
        node.Type = ResolveRoadNodeType(node.SelfCoordinate.X, hasSavedType ? savedType : null);
        GameInfo.WorldLineNodeTypes[node.SelfCoordinate] = node.Type;
        AssignContentQueueIndices();
        LockAllNodes();
        // Commit the encounter and switch budget together before opening any content.
        SaveSystem.SaveRunCheckpoint(background: false);
        return true;
    }

    public void AdvanceAlongRoad()
    {
        if (CanAdvanceAlongRoad())
            NextRoadNode?.PressButton();
    }

    public bool SwitchWorldLine(int worldLine)
    {
        int previousLine = GameInfo.CurrentWorldLine;
        if (!CanSwitchWorldLine || !GameInfo.TrySwitchWorldLine(worldLine))
            return false;

        BeginRoadTransition(previousLine);
        RefreshRoadPresentations();
        SaveSystem.SaveRunCheckpoint(background: false);
        return true;
    }

    private void RefreshRoadPresentations()
    {
        LevelNode next = NextRoadNode;
        if (next != null)
        {
            bool hasSavedType = GameInfo.WorldLineNodeTypes.TryGetValue(next.SelfCoordinate, out var savedType);
            // Revealing the next encounter must not commit it or disable worldline switching.
            next.Type = ResolveRoadNodeType(next.SelfCoordinate.X, hasSavedType ? savedType : null);
        }
        foreach (LevelNode node in _mapNodes.SelectMany(layer => layer))
            node.RefreshRoadPresentation();
    }

    private static LevelNode.LevelType? GetFixedRoadNodeType(int stage) => stage switch
    {
        0 => LevelNode.LevelType.Normal,
        _ when stage == MapLength - 1 => LevelNode.LevelType.Boss,
        // For even-length roads, use the later of the two central nodes.
        _ when stage == MapLength / 2 => LevelNode.LevelType.Rest,
        _ => null,
    };

    private static LevelNode.LevelType ResolveRoadNodeType(int stage, LevelNode.LevelType? savedType = null)
    {
        var fixedType = GetFixedRoadNodeType(stage);
        if (fixedType.HasValue)
            return fixedType.Value;
        if (savedType.HasValue && (savedType.Value != LevelNode.LevelType.Elite
            || stage >= GameInfo.FirstEliteRoadStage))
            return savedType.Value;
        return GameInfo.RollWorldLineNodeType(GameInfo.CurrentWorldLine,
            GameInfo.CreateWorldLineRng(stage), stage);
    }

    public int GetRoadNodeProbability(int stage, int worldLine, LevelNode.LevelType type)
    {
        if (stage < 0 || stage >= MapLength)
            return 0;
        var fixedType = GetFixedRoadNodeType(stage);
        if (fixedType.HasValue)
            return type == fixedType.Value ? 100 : 0;
        return GameInfo.GetWorldLineProbability(worldLine, type, stage);
    }

    public string GetNextNodeProbabilityText(int worldLine = -1) =>
        GetNodeProbabilityText(NextRoadStage, worldLine);

    public string GetNodeProbabilityText(int stage, int worldLine = -1)
    {
        if (stage >= MapLength)
            return "道路已完成";
        if (stage == MapLength - 1)
            return "终点 · 首领\n所有世界线的终点固定为首领";
        int line = worldLine < 0 ? GameInfo.CurrentWorldLine : worldLine;
        return string.Join("\n", ProbabilityDisplayTypes.Select(type =>
            $"{GetNodeTypeDisplayName(type)}  {GetRoadNodeProbability(stage, line, type)}%"));
    }

    public string GetRoadAdvanceText() => NextRoadNode == null
        ? "道路已完成" : IsNextEncounterCommitted ? "继续当前遭遇" : "向前探索";

    internal static string GetNodeTypeDisplayName(LevelNode.LevelType type) => type switch
    {
        LevelNode.LevelType.Event => "事件",
        LevelNode.LevelType.Shop => "商店",
        LevelNode.LevelType.Rest => "休息",
        LevelNode.LevelType.Elite => "精英",
        LevelNode.LevelType.Treasure => "宝箱",
        LevelNode.LevelType.Boss => "首领",
        _ => "普通战斗",
    };

    public async void AdvanceToNextRegion()
    {
        var transition = SceneTransitionLayer.Ensure(this);
        if (transition != null)
            await transition.FadeToBlackAsync(0.36f);

        bool showBossRelicChoice = CommitRegionAdvance();

        GenerateMap();
        RefreshNodeInteractivity();

        _map ??= GetParent() as Map;
        if (_map?.PlayerResourceState != null)
            _map.PlayerResourceState.RefreshPartyLifeResource();
        _map?.ForceUpdateRegionLabel();
        _map?.ResetCameraToStart();

        if (GetTree() != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // 切换区域时同步存档，确保新地图与区域进度可被继续游戏读取。
        SaveSystem.SaveRunCheckpoint(background: false);

        if (transition != null)
            await transition.FadeFromBlackAsync(0.36f);

        if (showBossRelicChoice)
            BossRelicChoice.Show(this);
    }

    private void EnsureRegionAdvancedFromCompletedBoss()
    {
        if (!GameInfo.NeedsRegionTransitionRecovery())
            return;

        bool showBossRelicChoice = PrepareRegionMapState(
            targetLevel: 1,
            offerBossRelicChoice: BossRelicChoice.ShouldShowPendingChoice()
        );
        SaveSystem.SaveRunCheckpoint(background: false);

        _map ??= GetParent() as Map;
        _map?.PlayerResourceState?.RefreshPartyLifeResource();
        _map?.ForceUpdateRegionLabel();

        if (showBossRelicChoice)
            CallDeferred(nameof(ShowPendingBossRelicChoiceIfNeeded));
    }

    private bool CommitRegionAdvance()
    {
        int nextLevel = Math.Max(0, GameInfo.CurrentLevel) + 1;
        return PrepareRegionMapState(nextLevel, offerBossRelicChoice: nextLevel == 1);
    }

    private bool PrepareRegionMapState(int targetLevel, bool offerBossRelicChoice)
    {
        GameInfo.CurrentLevel = Math.Max(0, targetLevel);
        GameInfo.ApplyDifficultyRegionEntryLife(targetLevel);
        GameInfo.ResetNormalBattleVisitState();
        GameInfo.ResetEliteBattleVisitState();
        if (offerBossRelicChoice)
            GameInfo.PendingBossRelicChoice = true;
        GameInfo.FirstLevelState.Clear();
        GameInfo.NodeRelicQueueStarts.Clear();
        GameInfo.NodeRelicQueueSlots.Clear();
        ResetMapInteractionLocks();
        return offerBossRelicChoice;
    }

    private void ResetMapInteractionLocks()
    {
        _manualLock = false;
        _manualLockSawBlockingUi = false;
        _interactionBlocked = false;
    }

    private void ShowPendingBossRelicChoiceIfNeeded()
    {
        if (!BossRelicChoice.ShouldShowPendingChoice())
            return;

        BossRelicChoice.Show(this);
    }
    public void OnNodeSelected(LevelNode selectedNode)
    {
        foreach (LevelNode parent in selectedNode.ParentNodes)
        {
            if (parent.State != LevelNode.LevelState.Completed
                || !_paths.TryGetValue((parent, selectedNode), out var path))
                continue;

            RoadSegment segment = _roadSegments[parent.SelfCoordinate.X];
            segment.TravelTween?.Kill();
            segment.TravelTween = CreateTween();
            segment.TravelTween.TweenMethod(Callable.From<float>(progress =>
            {
                if (!GodotObject.IsInstanceValid(path))
                    return;
                segment.Progress = progress;
                UpdateTravelledSegment(segment);
            }), segment.Progress, 1f, 0.45f).SetEase(Tween.EaseType.Out);
        }
    }

    public void SetNodeTypeLegendHighlight(LevelNode.LevelType? highlightedType)
    {
        // Legacy callers may only highlight encounters already completed.
        foreach (LevelNode node in _mapNodes.SelectMany(layer => layer))
            node.SetTypeLegendHighlighted(node.State == LevelNode.LevelState.Completed
                && highlightedType.HasValue && node.Type == highlightedType.Value);
    }

    private void AssignContentQueueIndices()
    {
        GameInfo.ResetRunRngState();
        var contentCounts = new Dictionary<LevelNode.LevelType, int>();
        int regionBattleIndex = 0;
        foreach (LevelNode node in _mapNodes.SelectMany(layer => layer))
        {
            // Unknown stages must not reserve rewards or affect encounter queues.
            if (!GameInfo.WorldLineNodeTypes.ContainsKey(node.SelfCoordinate))
                continue;
            contentCounts.TryGetValue(node.Type, out int queueIndex);
            contentCounts[node.Type] = queueIndex + 1;
            node.ContentQueueIndex = queueIndex;
            GameInfo.RegisterNodeContentQueueIndex(node.SelfCoordinate, queueIndex);
            if (GameInfo.NodeNormalBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int normalIndex))
                node.NormalBattleVisitIndex = normalIndex;
            if (GameInfo.NodeEliteBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int eliteIndex))
                node.EliteBattleVisitIndex = eliteIndex;
            if (GameInfo.IsBattleNodeType(node.Type))
            {
                node.RegionBattleQueueIndex = regionBattleIndex;
                GameInfo.RegisterNodeRegionBattleQueueIndex(node.SelfCoordinate, regionBattleIndex++);
            }
        }
        GameInfo.RefreshBattleItemDropChancePreview();
    }
}
