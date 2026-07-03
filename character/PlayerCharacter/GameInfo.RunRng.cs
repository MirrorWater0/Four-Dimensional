using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// STS 风格的独立随机数流：每种内容类型维护各自的队列序号，
/// 节点内容由「类型 + 队列序号 + 种子」决定，与路线选择无关。
/// </summary>
public enum RunRngStream
{
    NormalBattle = 1,
    EliteBattle = 2,
    BossBattle = 3,
    Event = 4,
    Shop = 5,
    BattleReward = 6,
    Rest = 7,
    Treasure = 8,
}

public static partial class GameInfo
{
    public const int BattleItemDropRollSalt = unchecked((int)0x49E17A31);
    public const int BattleCoinRewardSalt = unchecked((int)0x7C4A1D93);
    public const int BattleFormationSalt = unchecked((int)0x6D2B79F5);
    public const int EventContentSalt = unchecked((int)0x16F0B39D);

    public static Dictionary<Vector2I, int> NodeContentQueueIndices = new();
    public static Dictionary<Vector2I, int> NodeRegionBattleQueueIndices = new();
    public static Dictionary<Vector2I, int> NodeNormalBattleVisitIndices = new();
    public static Dictionary<Vector2I, int> NodeEliteBattleVisitIndices = new();
    public static int NormalBattlesVisited;
    public static int ElitesVisited;

    public static void ResetRunRngState()
    {
        NodeContentQueueIndices.Clear();
        NodeRegionBattleQueueIndices.Clear();
    }

    public static int ResolveNormalBattleVisitIndex(LevelNode node)
    {
        if (node == null)
            return 0;

        if (node.NormalBattleVisitIndex >= 0)
            return node.NormalBattleVisitIndex;

        if (NodeNormalBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int savedIndex))
        {
            node.NormalBattleVisitIndex = savedIndex;
            return savedIndex;
        }

        int visitIndex = NormalBattlesVisited++;
        node.NormalBattleVisitIndex = visitIndex;
        NodeNormalBattleVisitIndices[node.SelfCoordinate] = visitIndex;
        return visitIndex;
    }

    public static int PeekNormalBattleVisitIndex(LevelNode node)
    {
        if (node == null)
            return NormalBattlesVisited;

        if (node.NormalBattleVisitIndex >= 0)
            return node.NormalBattleVisitIndex;

        if (NodeNormalBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int savedIndex))
            return savedIndex;

        return NormalBattlesVisited;
    }

    public static int GetNodeNormalBattleVisitIndex(LevelNode node) =>
        PeekNormalBattleVisitIndex(node);

    public static int ResolveEliteBattleVisitIndex(LevelNode node)
    {
        if (node == null)
            return 0;

        if (node.EliteBattleVisitIndex >= 0)
            return node.EliteBattleVisitIndex;

        if (NodeEliteBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int savedIndex))
        {
            node.EliteBattleVisitIndex = savedIndex;
            return savedIndex;
        }

        int visitIndex = ElitesVisited++;
        node.EliteBattleVisitIndex = visitIndex;
        NodeEliteBattleVisitIndices[node.SelfCoordinate] = visitIndex;
        return visitIndex;
    }

    public static int PeekEliteBattleVisitIndex(LevelNode node)
    {
        if (node == null)
            return ElitesVisited;

        if (node.EliteBattleVisitIndex >= 0)
            return node.EliteBattleVisitIndex;

        if (NodeEliteBattleVisitIndices.TryGetValue(node.SelfCoordinate, out int savedIndex))
            return savedIndex;

        return ElitesVisited;
    }

    public static int GetNodeEliteBattleVisitIndex(LevelNode node) => PeekEliteBattleVisitIndex(node);

    public static int GetBattleRngQueueIndex(LevelNode node)
    {
        if (node == null)
            return 0;

        return node.Type switch
        {
            LevelNode.LevelType.Normal => GetNodeNormalBattleVisitIndex(node),
            LevelNode.LevelType.Elite => GetNodeEliteBattleVisitIndex(node),
            _ => GetNodeContentQueueIndex(node),
        };
    }

    public static int GetNodeContentQueueIndex(LevelNode node)
    {
        if (node == null)
            return 0;

        if (node.ContentQueueIndex >= 0)
            return node.ContentQueueIndex;

        if (NodeContentQueueIndices.TryGetValue(node.SelfCoordinate, out int index))
            return index;

        return 0;
    }

    public static int GetNodeRegionBattleQueueIndex(LevelNode node)
    {
        if (node == null)
            return 0;

        if (node.RegionBattleQueueIndex >= 0)
            return node.RegionBattleQueueIndex;

        if (NodeRegionBattleQueueIndices.TryGetValue(node.SelfCoordinate, out int index))
            return index;

        return 0;
    }

    public static void RegisterNodeContentQueueIndex(Vector2I coordinate, int queueIndex)
    {
        NodeContentQueueIndices[coordinate] = queueIndex;
    }

    public static void RegisterNodeRegionBattleQueueIndex(Vector2I coordinate, int queueIndex)
    {
        NodeRegionBattleQueueIndices[coordinate] = queueIndex;
    }

    public static RunRngStream GetStreamForNodeType(LevelNode.LevelType type)
    {
        return type switch
        {
            LevelNode.LevelType.Normal => RunRngStream.NormalBattle,
            LevelNode.LevelType.Elite => RunRngStream.EliteBattle,
            LevelNode.LevelType.Boss => RunRngStream.BossBattle,
            LevelNode.LevelType.Event => RunRngStream.Event,
            LevelNode.LevelType.Shop => RunRngStream.Shop,
            LevelNode.LevelType.Rest => RunRngStream.Rest,
            LevelNode.LevelType.Treasure => RunRngStream.Treasure,
            _ => RunRngStream.NormalBattle,
        };
    }

    public static RunRngStream GetBattleRewardStream(LevelNode node)
    {
        if (node == null)
            return RunRngStream.BattleReward;

        return node.Type switch
        {
            LevelNode.LevelType.Elite => RunRngStream.EliteBattle,
            LevelNode.LevelType.Boss => RunRngStream.BossBattle,
            _ => RunRngStream.NormalBattle,
        };
    }

    public static int CreateRunRngSeed(RunRngStream stream, int queueIndex, int salt = 0)
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash ^ Seed) * 16777619;
            hash = (hash ^ CurrentLevel) * 16777619;
            hash = (hash ^ (int)stream) * 16777619;
            hash = (hash ^ queueIndex) * 16777619;
            hash = (hash ^ salt) * 16777619;
            return hash;
        }
    }

    public static Random CreateRunRng(RunRngStream stream, int queueIndex, int salt = 0)
    {
        return new Random(CreateRunRngSeed(stream, queueIndex, salt));
    }

    public static Random CreateRunRng(LevelNode node, int salt = 0)
    {
        if (node == null)
            return new Random(Seed ^ salt);

        return CreateRunRng(
            GetStreamForNodeType(node.Type),
            GetBattleRngQueueIndex(node),
            salt
        );
    }

    public static Random CreateBattleRewardRng(LevelNode node, int salt = 0)
    {
        if (node == null)
            return new Random(Seed ^ salt);

        return CreateRunRng(
            GetBattleRewardStream(node),
            GetNodeContentQueueIndex(node),
            salt
        );
    }

    public static bool IsBattleNodeType(LevelNode.LevelType type)
    {
        return type
            is LevelNode.LevelType.Normal
                or LevelNode.LevelType.Elite
                or LevelNode.LevelType.Boss;
    }
}
