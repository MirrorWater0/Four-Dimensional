using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelProgress : Control
{
    private const int MapLength = 12; // Number of stages
    private const int MaxMapHeight = 4; // Maximum vertical slots per stage
    private const int BossSlot = 1;
    private const int PathCount = 6;
    private const float NodeSpacingX = 250f; // Distance between stages
    private const float NodeSpacingY = 290f; // Vertical distance between slots
    private const float MapLeftMargin = 200f;

    [Export]
    public float JitterAmount = 90f;

    // Legacy tuning fields kept for scene compatibility.
    // The current path-first generator does not use them directly.
    [Export]
    public int TwoBranchPercentage = 30; // % of nodes with 2 branches

    [Export]
    public int ThreeBranchPercentage = 10; // % of nodes with 3 branches

    // Remaining nodes will have 1 branch
    private PackedScene _nodeScene => GD.Load<PackedScene>("res://Map/Site/LevelNode.tscn");
    private PackedScene _accessWayScene => GD.Load<PackedScene>("res://Map/Site/AccessWay.tscn");
    private List<List<LevelNode>> _mapNodes = new List<List<LevelNode>>();
    private Dictionary<(LevelNode, LevelNode), ProgressBar> _paths =
        new Dictionary<(LevelNode, LevelNode), ProgressBar>();

    // Using a separate container for nodes to separate them from other potential children
    private Control _nodeContainer;
    private Control _connectionContainer;
    private readonly Random rng = new Random(GameInfo.Seed);
    private CanvasLayer _siteUiLayer;
    private bool _manualLock;
    private bool _siteUiHasChildren;

    private Vector2[] GetStageNodePositions(int stageIndex, int nodeCount, bool isBoss = false)
    {
        float totalMapHeight = (MaxMapHeight - 1) * NodeSpacingY;
        float startY = (1080 - totalMapHeight) / 2f;
        float baseX = MapLeftMargin + stageIndex * NodeSpacingX;
        Vector2[] positions = new Vector2[nodeCount];

        if (nodeCount <= 1)
        {
            positions[0] = new Vector2(baseX, startY + totalMapHeight / 2f);
            return positions;
        }

        float edgeInset = MathF.Min(NodeSpacingY * 0.18f, JitterAmount * 0.45f);
        float usableHeight = MathF.Max(totalMapHeight - edgeInset * 2f, totalMapHeight * 0.6f);
        float maxStageYOffset = isBoss ? 0f : MathF.Min(JitterAmount * 0.22f, (totalMapHeight - usableHeight) / 2f);
        float stageYOffset = isBoss ? 0f : RandomRange(-maxStageYOffset, maxStageYOffset);
        float stageStartY = startY + (totalMapHeight - usableHeight) / 2f + stageYOffset;

        float[] segmentWeights = new float[nodeCount - 1];
        float weightSum = 0f;
        float spacingVariance = Mathf.Clamp(JitterAmount / NodeSpacingY * 0.75f, 0.12f, 0.4f);
        for (int i = 0; i < segmentWeights.Length; i++)
        {
            float randomOffset = ((float)rng.NextDouble() * 2f - 1f) * spacingVariance;
            segmentWeights[i] = 1f + randomOffset;
            weightSum += segmentWeights[i];
        }

        float segmentScale = usableHeight / weightSum;
        float stageXOffset = isBoss ? 0f : RandomRange(-JitterAmount * 0.08f, JitterAmount * 0.08f);
        float maxXJitter = isBoss ? 0f : MathF.Min(32f, JitterAmount * 0.35f);
        float currentY = stageStartY;
        positions[0] = new Vector2(baseX + stageXOffset + RandomRange(-maxXJitter, maxXJitter), currentY);

        for (int i = 1; i < nodeCount; i++)
        {
            currentY += segmentWeights[i - 1] * segmentScale;
            positions[i] = new Vector2(
                baseX + stageXOffset + RandomRange(-maxXJitter, maxXJitter),
                currentY
            );
        }

        return positions;
    }

    private float RandomRange(float minValue, float maxValue)
    {
        return minValue + (float)rng.NextDouble() * (maxValue - minValue);
    }

    private bool[,] GeneratePathDrivenNodes(out bool[,,] edges)
    {
        bool[,] activeNodes = new bool[MapLength, MaxMapHeight];
        edges = new bool[MapLength - 1, MaxMapHeight, MaxMapHeight];

        List<int> startSlots = new List<int>(PathCount);
        for (int y = 0; y < MaxMapHeight; y++)
            startSlots.Add(y);

        while (startSlots.Count < PathCount)
            startSlots.Add(rng.Next(MaxMapHeight));

        Shuffle(startSlots);

        foreach (int startY in startSlots)
            GenerateSinglePath(startY, activeNodes, edges);

        activeNodes[MapLength - 1, BossSlot] = true;
        return activeNodes;
    }

    private void GenerateSinglePath(int startY, bool[,] activeNodes, bool[,,] edges)
    {
        int currentY = startY;
        activeNodes[0, currentY] = true;

        for (int x = 0; x < MapLength - 1; x++)
        {
            int nextY = x == MapLength - 2 ? BossSlot : ChooseNextPathSlot(x, currentY, activeNodes, edges);
            edges[x, currentY, nextY] = true;
            activeNodes[x + 1, nextY] = true;
            currentY = nextY;
        }
    }

    private int ChooseNextPathSlot(int stageIndex, int currentY, bool[,] activeNodes, bool[,,] edges)
    {
        var preferred = new List<(int nextY, int weight)>(3);
        var fallback = new List<(int nextY, int weight)>(3);

        for (
            int nextY = Math.Max(0, currentY - 1);
            nextY <= Math.Min(MaxMapHeight - 1, currentY + 1);
            nextY++
        )
        {
            int weight = CalculatePathCandidateWeight(stageIndex, currentY, nextY, activeNodes);
            if (!WouldCrossExistingEdge(stageIndex, currentY, nextY, edges))
                preferred.Add((nextY, weight));
            else
                fallback.Add((nextY, Math.Max(1, weight / 3)));
        }

        return ChooseWeightedNextSlot(preferred.Count > 0 ? preferred : fallback);
    }

    private int CalculatePathCandidateWeight(
        int stageIndex,
        int currentY,
        int nextY,
        bool[,] activeNodes
    )
    {
        int weight = 1;

        // Prefer unused nodes in the next stage so full layers appear more often.
        weight += activeNodes[stageIndex + 1, nextY] ? 2 : 9;

        // Favor straighter paths, but still allow adjacent drift.
        weight += nextY == currentY ? 4 : 2;

        // Gently pull paths toward the centered boss lane near the end.
        int distanceToBoss = Math.Abs(BossSlot - nextY);
        weight += Math.Max(0, 3 - distanceToBoss);
        if (stageIndex >= MapLength - 5)
            weight += Math.Max(0, 4 - distanceToBoss * 2);

        return weight;
    }

    private int ChooseWeightedNextSlot(List<(int nextY, int weight)> candidates)
    {
        int totalWeight = 0;
        foreach (var candidate in candidates)
            totalWeight += candidate.weight;

        int roll = rng.Next(totalWeight);
        foreach (var candidate in candidates)
        {
            roll -= candidate.weight;
            if (roll < 0)
                return candidate.nextY;
        }

        return candidates[candidates.Count - 1].nextY;
    }

    private bool WouldCrossExistingEdge(int stageIndex, int fromY, int toY, bool[,,] edges)
    {
        for (int otherFrom = 0; otherFrom < MaxMapHeight; otherFrom++)
        {
            for (int otherTo = 0; otherTo < MaxMapHeight; otherTo++)
            {
                if (!edges[stageIndex, otherFrom, otherTo])
                    continue;

                if ((fromY - otherFrom) * (toY - otherTo) < 0)
                    return true;
            }
        }

        return false;
    }

    private void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private void ApplyConnectionAwareLayout()
    {
        var basePositions = new Dictionary<LevelNode, Vector2>();
        float totalMapHeight = (MaxMapHeight - 1) * NodeSpacingY;
        float startY = (1080 - totalMapHeight) / 2f;
        float minY = startY;
        float maxY = startY + totalMapHeight;

        for (int x = 0; x < MapLength; x++)
        {
            var orderedNodes = _mapNodes[x].Where(node => node != null).OrderBy(node => node.SelfCoordinate.Y).ToList();
            if (orderedNodes.Count == 0)
                continue;

            Vector2[] stagePositions = GetStageNodePositions(x, orderedNodes.Count, x == MapLength - 1);
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                orderedNodes[i].Position = stagePositions[i];
                basePositions[orderedNodes[i]] = stagePositions[i];
            }
        }

        for (int pass = 0; pass < 3; pass++)
        {
            for (int x = 0; x < MapLength; x++)
            {
                var orderedNodes = _mapNodes[x].Where(node => node != null).OrderBy(node => node.SelfCoordinate.Y).ToList();
                if (orderedNodes.Count == 0)
                    continue;

                float[] desiredY = new float[orderedNodes.Count];
                for (int i = 0; i < orderedNodes.Count; i++)
                {
                    LevelNode node = orderedNodes[i];
                    float baseY = basePositions[node].Y;
                    List<float> neighborY = new List<float>(node.ParentNodes.Count + node.NextNodes.Count);

                    foreach (var parent in node.ParentNodes)
                        neighborY.Add(parent.Position.Y);

                    foreach (var next in node.NextNodes)
                        neighborY.Add(next.Position.Y);

                    if (neighborY.Count == 0)
                    {
                        desiredY[i] = baseY;
                        continue;
                    }

                    float connectionCenter = neighborY.Average();
                    float pull = GetConnectionPull(x, node);
                    desiredY[i] = Mathf.Lerp(baseY, connectionCenter, pull);
                }

                RelaxStageNodePositions(orderedNodes, desiredY, minY, maxY);
            }
        }
    }

    private float GetConnectionPull(int stageIndex, LevelNode node)
    {
        if (stageIndex == MapLength - 1)
            return 0f;

        if (stageIndex == 0)
            return 0.28f;

        int connectionCount = node.ParentNodes.Count + node.NextNodes.Count;
        if (connectionCount >= 3)
            return 0.55f;

        if (node.ParentNodes.Count > 0 && node.NextNodes.Count > 0)
            return 0.48f;

        return 0.38f;
    }

    private void RelaxStageNodePositions(List<LevelNode> orderedNodes, float[] desiredY, float minY, float maxY)
    {
        if (orderedNodes.Count == 1)
        {
            orderedNodes[0].Position = new Vector2(
                orderedNodes[0].Position.X,
                Mathf.Clamp(desiredY[0], minY, maxY)
            );
            return;
        }

        float availableHeight = maxY - minY;
        float minSpacing = MathF.Min(
            NodeSpacingY * 0.64f,
            availableHeight / (orderedNodes.Count - 1) * 0.92f
        );
        float[] adjusted = (float[])desiredY.Clone();

        adjusted[0] = Mathf.Clamp(
            adjusted[0],
            minY,
            maxY - minSpacing * (orderedNodes.Count - 1)
        );

        for (int i = 1; i < adjusted.Length; i++)
            adjusted[i] = MathF.Max(adjusted[i], adjusted[i - 1] + minSpacing);

        float overflow = adjusted[adjusted.Length - 1] - maxY;
        if (overflow > 0f)
        {
            for (int i = 0; i < adjusted.Length; i++)
                adjusted[i] -= overflow;
        }

        if (adjusted[0] < minY)
        {
            float underflow = minY - adjusted[0];
            for (int i = 0; i < adjusted.Length; i++)
                adjusted[i] += underflow;
        }

        for (int i = 1; i < adjusted.Length; i++)
            adjusted[i] = MathF.Max(adjusted[i], adjusted[i - 1] + minSpacing);

        overflow = adjusted[adjusted.Length - 1] - maxY;
        if (overflow > 0f)
        {
            for (int i = 0; i < adjusted.Length; i++)
                adjusted[i] -= overflow;
        }

        for (int i = 0; i < orderedNodes.Count; i++)
            orderedNodes[i].Position = new Vector2(orderedNodes[i].Position.X, adjusted[i]);
    }

    private void BuildAccessWays()
    {
        _paths.Clear();
        foreach (Node child in _connectionContainer.GetChildren())
            child.QueueFree();

        foreach (var layer in _mapNodes)
        {
            foreach (var node in layer)
            {
                if (node == null)
                    continue;

                foreach (var next in node.NextNodes)
                    CreateAccessWay(node, next);
            }
        }
    }

    public override void _Ready()
    {
        // Allow mouse events to pass through empty space to the underlying Map DragButton
        MouseFilter = MouseFilterEnum.Ignore;

        _connectionContainer = new Control();
        _connectionContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _connectionContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_connectionContainer);

        _nodeContainer = new Control();
        _nodeContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _nodeContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_nodeContainer);

        _siteUiLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("Map/SiteUI");
        GenerateMap();
        RefreshNodeInteractivity();
        // CallDeferred("StartAnimation");
    }

    public override void _Process(double delta)
    {
        if (_siteUiLayer == null)
            return;

        bool hasChildren = HasVisibleSiteUi();
        if (hasChildren == _siteUiHasChildren)
            return;

        _siteUiHasChildren = hasChildren;
        RefreshNodeInteractivity();
    }

    public void LockAllNodes()
    {
        _manualLock = true;
        RefreshNodeInteractivity();
    }

    private void RefreshNodeInteractivity()
    {
        bool hasChildren = HasVisibleSiteUi();
        if (hasChildren)
        {
            _manualLock = false;
            _siteUiHasChildren = true;
        }
        else
        {
            _siteUiHasChildren = false;
        }

        bool shouldDisable = _manualLock || hasChildren;
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

    private bool HasVisibleSiteUi()
    {
        if (_siteUiLayer == null)
            return false;

        foreach (Node child in _siteUiLayer.GetChildren())
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

    private void GenerateMap()
    {
        _mapNodes.Clear();
        _paths.Clear();

        foreach (Node child in _nodeContainer.GetChildren())
            child.QueueFree();

        foreach (Node child in _connectionContainer.GetChildren())
            child.QueueFree();

        bool[,,] topologyEdges;
        bool[,] activeNodes = GeneratePathDrivenNodes(out topologyEdges);

        // 1. Generate Nodes (Left to Right)
        for (int x = 0; x < MapLength; x++)
        {
            List<LevelNode> currentLayer = new List<LevelNode>();
            LevelNode[] layerNodes = new LevelNode[MaxMapHeight];

            for (int y = 0; y < MaxMapHeight; y++)
            {
                bool shouldCreate = x == MapLength - 1 ? y == BossSlot : activeNodes[x, y];
                if (shouldCreate)
                    layerNodes[y] = CreateNode(x, y, 0);
            }

            currentLayer.AddRange(layerNodes);
            _mapNodes.Add(currentLayer);
        }

        // 2. Build topology from full paths first, then position nodes from those connections.
        for (int x = 0; x < MapLength - 1; x++)
        {
            for (int fromY = 0; fromY < MaxMapHeight; fromY++)
            {
                for (int toY = 0; toY < MaxMapHeight; toY++)
                {
                    if (!topologyEdges[x, fromY, toY])
                        continue;

                    LevelNode from = _mapNodes[x][fromY];
                    LevelNode to = _mapNodes[x + 1][toY];
                    if (from != null && to != null)
                        ConnectNodes(from, to);
                }
            }
        }

        ApplyConnectionAwareLayout();
        BuildAccessWays();

        // 3. Assign Types
        AssignNodeTypes(rng);

        // 4. Unlock Start (only if this is a new game, not loading from save)
        bool isNewGame =
            GameInfo.FirstLevelState.Count == 0
            || !GameInfo.FirstLevelState.Values.Any(state => state != LevelNode.LevelState.Locked);

        if (isNewGame)
        {
            foreach (var node in _mapNodes[0])
            {
                if (node != null)
                    node.Unlock();
            }
        }
        else
        {
            foreach (var kvp in _paths)
            {
                LevelNode from = kvp.Key.Item1;
                LevelNode to = kvp.Key.Item2;
                ProgressBar accessWay = kvp.Value;

                // Only show paths originating from a Completed node (the path we took)
                if (from.State == LevelNode.LevelState.Completed)
                {
                    // Case 1: Past history - Both nodes are completed
                    // Case 2: Current options - From Completed to Unlocked (available next steps)
                    if (to.State == LevelNode.LevelState.Completed)
                    {
                        accessWay.Value = 100;
                    }
                }
            }
        }
    }

    private void ConnectNodes(LevelNode from, LevelNode to)
    {
        if (!from.NextNodes.Contains(to))
        {
            from.NextNodes.Add(to);
            to.ParentNodes.Add(from);
        }
    }

    private void CreateAccessWay(LevelNode from, LevelNode to)
    {
        var accessWay = _accessWayScene.Instantiate<ProgressBar>();
        _connectionContainer.AddChild(accessWay);

        Vector2 nodeCenterOffset = new Vector2(40, 40);
        Vector2 fromCenter = from.Position + nodeCenterOffset;
        Vector2 toCenter = to.Position + nodeCenterOffset;
        Vector2 direction = (toCenter - fromCenter).Normalized();

        float halfSize = 40f;
        float maxComponent = Mathf.Max(Mathf.Abs(direction.X), Mathf.Abs(direction.Y));
        float distToEdge = (maxComponent > 0.001f) ? (halfSize / maxComponent) : halfSize;
        float margin = 5f;

        Vector2 startPos = fromCenter + direction * (distToEdge + margin);
        Vector2 endPos = toCenter - direction * (distToEdge + margin);

        float distance = startPos.DistanceTo(endPos);
        float angle = startPos.AngleToPoint(endPos);

        accessWay.Size = new Vector2(distance, accessWay.Size.Y);
        accessWay.Position = startPos;
        accessWay.Rotation = angle;
        accessWay.PivotOffset = new Vector2(0, accessWay.Size.Y / 2);
        accessWay.Value = 0;

        _paths[(from, to)] = accessWay;
    }

    public void OnNodeSelected(LevelNode selectedNode)
    {
        LockSiblings(selectedNode);
        AnimatePathTo(selectedNode);
    }

    private void AnimatePathTo(LevelNode node)
    {
        // Find the parent node that is Completed (the one we came from)
        foreach (var parent in node.ParentNodes)
        {
            if (parent.State == LevelNode.LevelState.Completed)
            {
                if (_paths.TryGetValue((parent, node), out ProgressBar path))
                {
                    Tween tween = CreateTween();
                    tween
                        .TweenProperty(path, "value", 100, 0.5f)
                        .SetTrans(Tween.TransitionType.Cubic)
                        .SetEase(Tween.EaseType.Out);
                }
                // Assuming only one valid path from the previous layer in a standard run
                break;
            }
        }
    }

    private void LockSiblings(LevelNode selectedNode)
    {
        int x = selectedNode.SelfCoordinate.X;
        if (x >= _mapNodes.Count)
            return;

        var layer = _mapNodes[x];
        foreach (var node in layer)
        {
            if (node != null && node != selectedNode && node.State == LevelNode.LevelState.Unlocked)
            {
                node.State = LevelNode.LevelState.Locked;
                node.Button.Disabled = true;
                node.Color = node.LockColor;
                GameInfo.FirstLevelState[node.SelfCoordinate] = LevelNode.LevelState.Locked;
            }
        }
    }

    private LevelNode CreateNode(int x, int y, float jitterY = 0)
    {
        var node = _nodeScene.Instantiate<LevelNode>();
        node.RandomNum = rng.Next();
        _nodeContainer.AddChild(node);

        float totalMapHeight = (MaxMapHeight - 1) * NodeSpacingY;
        float startY = (1080 - totalMapHeight) / 2;

        node.Position = new Vector2(
            MapLeftMargin + x * NodeSpacingX,
            startY + y * NodeSpacingY + jitterY
        );
        node.SelfCoordinate = new Vector2I(x, y);

        if (GameInfo.FirstLevelState.ContainsKey(node.SelfCoordinate))
        {
            node.State = GameInfo.FirstLevelState[node.SelfCoordinate];

            // Apply the loaded state to the node's visual appearance
            // This needs to be called deferred because the node's _Ready hasn't been called yet
            node.CallDeferred("ApplyLoadedState");
        }
        else
        {
            node.State = LevelNode.LevelState.Locked;
            GameInfo.FirstLevelState[node.SelfCoordinate] = LevelNode.LevelState.Locked;
        }

        // node.Button.Pressed += () => OnNodeSelected(node);
        return node;
    }

    private void AssignNodeTypes(Random rng)
    {
        for (int x = 0; x < MapLength; x++)
        {
            var layer = _mapNodes[x];
            foreach (var node in layer)
            {
                if (node == null)
                    continue;

                if (x == 0)
                    node.Type = LevelNode.LevelType.Normal;
                else if (x == MapLength - 1)
                    node.Type = LevelNode.LevelType.Boss;
                else if (x == MapLength / 2 && x > 2) // Elite only after first 3 stages
                    node.Type = LevelNode.LevelType.Elite;
                else
                {
                    int roll = rng.Next(100);
                    if (roll < 45)
                        node.Type = LevelNode.LevelType.Normal;
                    else if (roll < 60)
                        node.Type = LevelNode.LevelType.Event;
                    else if (roll < 75)
                        node.Type = LevelNode.LevelType.Shop;
                    else if (roll < 90 && x > 2) // Elite can only appear after first 3 stages
                        node.Type = LevelNode.LevelType.Elite;
                    else
                        node.Type = LevelNode.LevelType.Normal;
                }
                node.ColorChose();
            }
        }
    }
}
