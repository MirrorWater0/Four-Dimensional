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
        if (start < 0)
            return Array.Empty<RelicID>();

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
        RelicSelection selection = SelectRelicsAtSlots(RelicQueueNextIndex, 1);
        RelicQueueNextIndex = Math.Max(RelicQueueNextIndex, selection.NextIndex);
        return selection.Relics.Length > 0 ? selection.Relics[0] : null;
    }

    public static RelicID[] GetRelicsAtSlots(int start, int count)
    {
        return SelectRelicsAtSlots(start, count).Relics;
    }

    private static RelicSelection SelectRelicsAtSlots(int start, int count)
    {
        EnsureRelicQueueCapacity();

        var result = new List<RelicID>();
        if (count <= 0 || RelicQueue == null || RelicQueue.Count == 0)
            return new RelicSelection(result.ToArray(), Math.Max(0, start));

        var offered = new HashSet<RelicID>();
        int index = Math.Max(0, start);
        for (; index < RelicQueue.Count && result.Count < count; index++)
        {
            RelicID relic = RelicQueue[index];
            if (!HasRelic(relic) && offered.Add(relic))
                result.Add(relic);
        }

        return new RelicSelection(result.ToArray(), index);
    }

    public static RelicID? GetRelicAtSlot(int slot)
    {
        RelicID[] relics = GetRelicsAtSlots(slot, 1);
        return relics.Length > 0 ? relics[0] : null;
    }

    private static int ResolveShopRelicQueueStart(LevelNode shopNode)
    {
        if (shopNode == null)
            return -1;

        if (NodeRelicQueueStarts.TryGetValue(shopNode.SelfCoordinate, out int start))
        {
            shopNode.RelicQueueStart = start;
            return start;
        }

        start = RelicQueueNextIndex;
        RelicSelection selection = SelectRelicsAtSlots(start, ShopRelicQueueSlots);
        RelicQueueNextIndex = Math.Max(RelicQueueNextIndex, selection.NextIndex);
        NodeRelicQueueStarts[shopNode.SelfCoordinate] = start;
        shopNode.RelicQueueStart = start;
        return start;
    }

    private static int ResolveNodeRelicQueueSlot(LevelNode node)
    {
        if (node == null)
            return -1;

        if (NodeRelicQueueSlots.TryGetValue(node.SelfCoordinate, out int slot))
        {
            node.RelicQueueSlot = slot;
            return slot;
        }

        slot = RelicQueueNextIndex;
        RelicSelection selection = SelectRelicsAtSlots(slot, 1);
        RelicQueueNextIndex = Math.Max(RelicQueueNextIndex, selection.NextIndex);
        NodeRelicQueueSlots[node.SelfCoordinate] = slot;
        node.RelicQueueSlot = slot;
        return slot;
    }

    private readonly struct RelicSelection(RelicID[] relics, int nextIndex)
    {
        public RelicID[] Relics { get; } = relics;
        public int NextIndex { get; } = nextIndex;
    }
}
