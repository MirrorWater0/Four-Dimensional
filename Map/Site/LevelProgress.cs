using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class LevelProgress : Control
{
    private const int MapLength = 12; // Number of stages
    private const int MapHeight = 4; // Nodes per stage (Fixed to 4 vertical slots)
    private const float NodeSpacingX = 250f; // Distance between stages
    private const float NodeSpacingY = 250f; // Increased vertical spacing for 4 nodes
    private const float MapLeftMargin = 200f;

    [Export]
    public float JitterAmount = 100f;

    // Branch control parameters (percentage of nodes per stage)
    [Export]
    public int TwoBranchPercentage = 30; // % of nodes with 2 branches

    [Export]
    public int ThreeBranchPercentage = 10; // % of nodes with 3 branches

    // Remaining nodes will have 1 branch
    private PackedScene _nodeScene;
    private PackedScene _accessWayScene;
    private List<List<LevelNode>> _mapNodes = new List<List<LevelNode>>();
    private Dictionary<(LevelNode, LevelNode), ProgressBar> _paths =
        new Dictionary<(LevelNode, LevelNode), ProgressBar>();

    // Using a separate container for nodes to separate them from other potential children
    private Control _nodeContainer;
    private Control _connectionContainer;

    public override void _Ready()
    {
        // Allow mouse events to pass through empty space to the underlying Map DragButton
        MouseFilter = MouseFilterEnum.Ignore;

        _nodeScene = GD.Load<PackedScene>("res://Map/Site/LevelNode.tscn");
        _accessWayScene = GD.Load<PackedScene>("res://Map/Site/AccessWay.tscn");

        // Remove existing static layout if any, or just ignore VBoxContainer
        var existingVBox = GetNodeOrNull<VBoxContainer>("VBoxContainer");
        if (existingVBox != null)
        {
            existingVBox.QueueFree();
        }

        _connectionContainer = new Control();
        _connectionContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _connectionContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_connectionContainer);

        _nodeContainer = new Control();
        _nodeContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _nodeContainer.MouseFilter = MouseFilterEnum.Ignore; // Pass events through
        AddChild(_nodeContainer);

        GenerateMap();
        // CallDeferred("StartAnimation");
    }

    private void GenerateMap()
    {
        Random rng = new Random(GameInfo.Seed);
        _mapNodes.Clear();

        // 1. Generate Nodes (Left to Right)
        for (int x = 0; x < MapLength; x++)
        {
            List<LevelNode> currentLayer = new List<LevelNode>();

            // Fixed 4 nodes per layer, except boss
            int nodeCount = (x == MapLength - 1) ? 1 : 4;

            LevelNode[] layerNodes = new LevelNode[MapHeight];

            if (nodeCount == 1) // Boss
            {
                // Boss in the middle slot-ish (between 1 and 2 visual center)
                // For simplicity, let's put it at index 1 and offset visually later, or just index 1.
                // Or better: Logic coordinate 1 or 2.
                int bossY = 1;
                layerNodes[bossY] = CreateNode(x, bossY, 0);

                // Adjust boss position to be vertically centered
                float totalHeight = (MapHeight - 1) * NodeSpacingY;
                float startY = (1080 - totalHeight) / 2;
                layerNodes[bossY].Position = new Vector2(
                    MapLeftMargin + x * NodeSpacingX,
                    startY + 1.5f * NodeSpacingY // Center between 1 and 2
                );
            }
            else
            {
                // Create exactly 4 nodes at y=0,1,2,3
                for (int y = 0; y < MapHeight; y++)
                {
                    float jitter = (float)(rng.NextDouble() * 2 - 1) * JitterAmount;
                    layerNodes[y] = CreateNode(x, y, jitter);
                }
            }

            currentLayer.AddRange(layerNodes);
            _mapNodes.Add(currentLayer);
        }

        // 2. Connect Nodes using Gap Logic to prevent crossing
        for (int x = 0; x < MapLength - 1; x++)
        {
            var currentLayer = _mapNodes[x];
            var nextLayer = _mapNodes[x + 1];

            // Gap constraints: 0=None, 1=Down(y->y+1), -1=Up(y+1->y)
            // Gaps are between 0-1, 1-2, 2-3. Indices 0, 1, 2.
            int[] gaps = new int[MapHeight - 1];

            // Randomly decide gap directions
            for (int i = 0; i < gaps.Length; i++)
            {
                int roll = rng.Next(100);
                if (roll < 40)
                    gaps[i] = 0; // No cross
                else if (roll < 70)
                    gaps[i] = 1; // Down
                else
                    gaps[i] = -1; // Up
            }

            // Determine max branches for each node based on percentages
            int[] maxBranches = new int[MapHeight];
            for (int y = 0; y < MapHeight; y++)
            {
                if (currentLayer[y] == null)
                {
                    maxBranches[y] = 0;
                    continue;
                }

                int roll = rng.Next(100);
                if (roll < ThreeBranchPercentage)
                    maxBranches[y] = 3; // 3 branches
                else if (roll < ThreeBranchPercentage + TwoBranchPercentage)
                    maxBranches[y] = 2; // 2 branches
                else
                    maxBranches[y] = 1; // 1 branch
            }

            // Step A & B: Create connections with variable max branches
            for (int y = 0; y < MapHeight; y++)
            {
                var node = currentLayer[y];
                if (node == null)
                    continue;

                List<LevelNode> candidates = new List<LevelNode>();

                // 1. Straight
                if (nextLayer[y] != null)
                    candidates.Add(nextLayer[y]);

                // 2. Cross Down (y -> y+1) if gap says so
                if (y < MapHeight - 1 && gaps[y] == 1 && nextLayer[y + 1] != null)
                    candidates.Add(nextLayer[y + 1]);

                // 3. Cross Up (y -> y-1) if gap says so (gaps[y-1] == -1 means y -> y-1)
                if (y > 0 && gaps[y - 1] == -1 && nextLayer[y - 1] != null)
                    candidates.Add(nextLayer[y - 1]);

                // Enforce max branches for this specific node
                while (candidates.Count > maxBranches[y])
                {
                    // If we have 3 (Straight, Down, Up) and need to reduce, remove Straight to favor crossing
                    if (
                        candidates.Count == 3
                        && maxBranches[y] < 3
                        && candidates.Contains(nextLayer[y])
                    )
                        candidates.Remove(nextLayer[y]);
                    else
                        candidates.RemoveAt(candidates.Count - 1);
                }

                foreach (var next in candidates)
                {
                    ConnectNodes(node, next);
                }
            }

            // Step C: Handle Boss layer (special case, everything converges)
            if (x == MapLength - 2)
            {
                LevelNode boss = nextLayer[1]; // We put boss at index 1
                if (boss == null)
                {
                    // Find non-null boss
                    foreach (var n in nextLayer)
                        if (n != null)
                            boss = n;
                }

                foreach (var node in currentLayer)
                {
                    if (node != null)
                        ConnectNodes(node, boss);
                }
            }
            else
            {
                // Step D: Connectivity Check & Repair
                // 1. Ensure every Current node has a child
                for (int y = 0; y < MapHeight; y++)
                {
                    var node = currentLayer[y];
                    if (node != null && node.NextNodes.Count == 0)
                    {
                        // Needs a child. Try Straight, then Valid Cross.
                        if (nextLayer[y] != null)
                        {
                            ConnectNodes(node, nextLayer[y]);
                        }
                        else
                        {
                            // If straight not avail (unlikely with fixed grid), look neighbors
                            // Check valid gap directions
                            if (y < MapHeight - 1 && gaps[y] != -1 && nextLayer[y + 1] != null) // Can go down?
                            {
                                ConnectNodes(node, nextLayer[y + 1]);
                                gaps[y] = 1; // Enforce
                            }
                            else if (y > 0 && gaps[y - 1] != 1 && nextLayer[y - 1] != null) // Can go up?
                            {
                                ConnectNodes(node, nextLayer[y - 1]);
                                gaps[y - 1] = -1; // Enforce
                            }
                        }
                    }
                }

                // 2. Ensure every Next node has a parent
                for (int y = 0; y < MapHeight; y++)
                {
                    var node = nextLayer[y];
                    if (node != null && node.ParentNodes.Count == 0)
                    {
                        // Attempt to find a parent with available slots (< 2)
                        bool connected = false;

                        // Candidates: Straight, Above (Down), Below (Up)
                        var candidates = new List<(LevelNode parent, int gapIdx, int gapVal)>();

                        if (currentLayer[y] != null)
                            candidates.Add((currentLayer[y], -1, 0)); // Straight

                        if (y > 0 && gaps[y - 1] != -1 && currentLayer[y - 1] != null)
                            candidates.Add((currentLayer[y - 1], y - 1, 1)); // Down

                        if (y < MapHeight - 1 && gaps[y] != 1 && currentLayer[y + 1] != null)
                            candidates.Add((currentLayer[y + 1], y, -1)); // Up

                        // Try to find one with space
                        foreach (var cand in candidates)
                        {
                            if (cand.parent.NextNodes.Count < 2)
                            {
                                ConnectNodes(cand.parent, node);
                                if (cand.gapIdx != -1)
                                    gaps[cand.gapIdx] = cand.gapVal;
                                connected = true;
                                break;
                            }
                        }

                        // If still not connected (all full), force connect to first candidate
                        if (!connected && candidates.Count > 0)
                        {
                            var cand = candidates[0];
                            ConnectNodes(cand.parent, node);
                            if (cand.gapIdx != -1)
                                gaps[cand.gapIdx] = cand.gapVal;
                        }
                    }
                }

                // Step E: Random Pruning (Optional)
                // To make it look more like STS, we can remove some straight paths if redundancy exists
                // But simple connectivity is better for now to guarantee no dead ends.
                // We'll leave all valid connections.
            }
        }

        // 3. Assign Types
        AssignNodeTypes(rng);

        // 4. Unlock Start (only if this is a new game, not loading from save)
        bool isNewGame = GameInfo.FirstLevelState.Count == 0 ||
                         !GameInfo.FirstLevelState.Values.Any(state => state != LevelNode.LevelState.Locked);

        if (isNewGame)
        {
            foreach (var node in _mapNodes[0])
            {
                if (node != null)
                    node.Unlock();
            }
        }
    }

    private void ConnectNodes(LevelNode from, LevelNode to)
    {
        if (!from.NextNodes.Contains(to))
        {
            from.NextNodes.Add(to);
            to.ParentNodes.Add(from);
            CreateAccessWay(from, to);
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

    private void OnNodeSelected(LevelNode selectedNode)
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
        _nodeContainer.AddChild(node);

        float totalMapHeight = (MapHeight - 1) * NodeSpacingY;
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

        node.Button.Pressed += () => OnNodeSelected(node);
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
                    if (roll < 50)
                        node.Type = LevelNode.LevelType.Normal;
                    else if (roll < 70)
                        node.Type = LevelNode.LevelType.Event;
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
