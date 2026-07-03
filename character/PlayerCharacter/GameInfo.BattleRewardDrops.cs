using System;
using System.Collections.Generic;
using System.Linq;

public static partial class GameInfo
{
    public const int BaseBattleItemDropChance = 30;
    private const int BattleDropChanceStep = 10;

    public static void ResetBattleRewardDropState()
    {
        BattleItemDropChance = BaseBattleItemDropChance;
        RefreshBattleItemDropChancePreview();
    }

    public static string BuildBattleRewardDropPreviewText()
    {
        RefreshBattleItemDropChancePreview();
        int itemChance = NormalizeDropChance(BattleItemDropChance, BaseBattleItemDropChance);
        return $"下一场战斗掉率：\n道具 {itemChance}%";
    }

    public static int GetBattleItemDropChanceForNode(LevelNode node)
    {
        if (node == null || !IsBattleNodeType(node.Type))
            return BaseBattleItemDropChance;

        return GetBattleItemDropChanceAtRegionBattleIndex(GetNodeRegionBattleQueueIndex(node));
    }

    public static int GetBattleItemDropChanceForNextBattleAfter(LevelNode completedNode = null)
    {
        int nextIndex = GetLowestUncompletedRegionBattleIndex(completedNode);
        return GetBattleItemDropChanceAtRegionBattleIndex(nextIndex);
    }

    public static void RefreshBattleItemDropChancePreview()
    {
        BattleItemDropChance = GetBattleItemDropChanceForNextBattleAfter();
    }

    public static bool RollBattleItemDrop(LevelNode node)
    {
        if (node == null || !IsBattleNodeType(node.Type))
            return false;

        int regionIndex = GetNodeRegionBattleQueueIndex(node);
        int chance = GetBattleItemDropChanceAtRegionBattleIndex(regionIndex);
        return RollBattleItemDropAtRegionBattleIndex(regionIndex, chance);
    }

    public static bool PreviewBattleItemDrop(LevelNode node)
    {
        return RollBattleItemDrop(node);
    }

    private static int GetBattleItemDropChanceAtRegionBattleIndex(int regionBattleIndex)
    {
        if (regionBattleIndex <= 0)
            return BaseBattleItemDropChance;

        int chance = BaseBattleItemDropChance;
        for (int i = 0; i < regionBattleIndex; i++)
        {
            if (RollBattleItemDropAtRegionBattleIndex(i, chance))
                chance = BaseBattleItemDropChance;
            else
                chance = IncreaseDropChance(chance);
        }

        return NormalizeDropChance(chance, BaseBattleItemDropChance);
    }

    private static bool RollBattleItemDropAtRegionBattleIndex(int regionBattleIndex, int chance)
    {
        var rng = CreateRunRng(RunRngStream.BattleReward, regionBattleIndex, BattleItemDropRollSalt);
        return rng.Next(100) < NormalizeDropChance(chance, BaseBattleItemDropChance);
    }

    private static int GetLowestUncompletedRegionBattleIndex(LevelNode treatAsCompleted = null)
    {
        HashSet<int> completed = GetCompletedRegionBattleIndices();
        if (treatAsCompleted != null && IsBattleNodeType(treatAsCompleted.Type))
            completed.Add(GetNodeRegionBattleQueueIndex(treatAsCompleted));

        if (NodeRegionBattleQueueIndices.Count == 0)
            return 0;

        int maxIndex = NodeRegionBattleQueueIndices.Values.Max();
        for (int i = 0; i <= maxIndex; i++)
        {
            if (!completed.Contains(i))
                return i;
        }

        return maxIndex + 1;
    }

    private static HashSet<int> GetCompletedRegionBattleIndices()
    {
        var completed = new HashSet<int>();
        if (CompletedLevelNodeRecords == null || CompletedLevelNodeRecords.Count == 0)
            return completed;

        foreach (LevelNodeCompletionRecord record in CompletedLevelNodeRecords.Values)
        {
            if (record == null || record.MapLevel != CurrentLevel)
                continue;
            if (!IsBattleNodeType(record.NodeType))
                continue;

            if (NodeRegionBattleQueueIndices.TryGetValue(record.Coordinate, out int index))
                completed.Add(index);
        }

        return completed;
    }

    private static int NormalizeDropChance(int chance, int fallback)
    {
        if (chance <= 0)
            chance = fallback;

        return Math.Clamp(chance, 0, 100);
    }

    private static int IncreaseDropChance(int currentChance)
    {
        return Math.Min(100, currentChance + BattleDropChanceStep);
    }
}
