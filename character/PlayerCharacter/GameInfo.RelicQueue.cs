using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static partial class GameInfo
{
    public const int ShopRelicQueueSlots = 3;
    private const int RelicQueueShuffleSalt = unchecked((int)0x8E11C0DE);

    public static List<RelicID> RelicQueue = new();
    /// <summary>下一段遗物内容将从队列的该下标开始分配。</summary>
    public static int RelicQueueNextIndex;
    public static Dictionary<Vector2I, int> NodeRelicQueueStarts = new();
    public static Dictionary<Vector2I, int> NodeRelicQueueSlots = new();

    private const int MaxShopNodesPerRegion = 5;
    private const int MaxEliteNodesPerRegion = 6;
    public const int MaxEventNodesPerRegion = 8;
    private const int MaxRegionalBonusRelicSlotsPerRegion = 1;
    private const int RunRegionCount = 2;
    private const int StarterRelicHeadroom = 4;

    public static int GetRelicQueueTargetLength()
    {
        int perRegion =
            MaxShopNodesPerRegion * ShopRelicQueueSlots
            + MaxEliteNodesPerRegion
            + MaxEventNodesPerRegion
            + MaxRegionalBonusRelicSlotsPerRegion;

        return StarterRelicHeadroom + RunRegionCount * perRegion;
    }

    public static void ResetRelicQueueState()
    {
        RelicQueue.Clear();
        RelicQueueNextIndex = 0;
        NodeRelicQueueStarts.Clear();
        NodeRelicQueueSlots.Clear();
    }

    public static void InitializeRelicQueue()
    {
        RelicID[] pool = Relic.GetStandardOfferPool();
        RelicQueue = new List<RelicID>();
        if (pool.Length == 0)
            return;

        int targetLength = GetRelicQueueTargetLength();
        int passIndex = 0;
        while (RelicQueue.Count < targetLength)
        {
            var rng = new Random(
                CreateRunRngSeed(RunRngStream.BattleReward, passIndex, RelicQueueShuffleSalt)
            );
            foreach (RelicID relic in pool.OrderBy(_ => rng.Next()))
            {
                RelicQueue.Add(relic);
                if (RelicQueue.Count >= targetLength)
                    break;
            }

            passIndex++;
        }
    }

    public static void EnsureRelicQueueCapacity()
    {
        if (RelicQueue != null && RelicQueue.Count >= GetRelicQueueTargetLength())
            return;

        InitializeRelicQueue();
    }

    public static RelicID[] GetShopRelicOffers(LevelNode shopNode)
    {
        int start = ResolveShopRelicQueueStart(shopNode);
        return GetRelicsAtSlots(start, ShopRelicQueueSlots);
    }

    public static RelicID? GetEventRelicOffer(LevelNode eventNode)
    {
        int slot = ResolveNodeRelicQueueSlot(eventNode);
        if (slot < 0)
            return null;

        return GetRelicAtSlot(slot);
    }

    public static RelicID? GetBattleRelicOffer(LevelNode battleNode, int rewardOffset = 0)
    {
        if (battleNode == null)
            return null;

        int slot = ResolveNodeRelicQueueSlot(battleNode);
        if (slot < 0)
            return null;

        return GetRelicAtSlot(slot + rewardOffset);
    }

    public static RelicID? DrawStarterRelicFromQueue()
    {
        RelicID? relic = GetRelicAtSlot(RelicQueueNextIndex);
        RelicQueueNextIndex++;
        return relic;
    }

    public static RelicID[] GetRelicsAtSlots(int start, int count)
    {
        EnsureRelicQueueCapacity();

        var result = new List<RelicID>();
        if (count <= 0 || RelicQueue == null || RelicQueue.Count == 0)
            return result.ToArray();

        var offered = new HashSet<RelicID>();
        for (int index = Math.Max(0, start); index < RelicQueue.Count && result.Count < count; index++)
        {
            RelicID relic = RelicQueue[index];
            if (!HasRelic(relic) && offered.Add(relic))
                result.Add(relic);
        }

        return result.ToArray();
    }

    public static RelicID? GetRelicAtSlot(int slot)
    {
        RelicID[] relics = GetRelicsAtSlots(slot, 1);
        return relics.Length > 0 ? relics[0] : null;
    }

    private static int ResolveShopRelicQueueStart(LevelNode shopNode)
    {
        if (shopNode == null)
            return RelicQueueNextIndex;

        if (NodeRelicQueueStarts.TryGetValue(shopNode.SelfCoordinate, out int start))
            return start;

        start = RelicQueueNextIndex;
        RelicQueueNextIndex += ShopRelicQueueSlots;
        NodeRelicQueueStarts[shopNode.SelfCoordinate] = start;
        return start;
    }

    private static int ResolveNodeRelicQueueSlot(LevelNode node)
    {
        if (node == null)
            return -1;

        if (NodeRelicQueueSlots.TryGetValue(node.SelfCoordinate, out int slot))
            return slot;

        slot = RelicQueueNextIndex;
        RelicQueueNextIndex++;
        NodeRelicQueueSlots[node.SelfCoordinate] = slot;
        return slot;
    }
}
