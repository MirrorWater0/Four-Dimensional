using System;
using System.Collections.Generic;

/// <summary>
/// 普通战斗固定遭遇表。每组遭遇是预先设计好的编队；整局队列由 GameInfo.InitializeNormalEncounterQueue 预生成。
/// </summary>
public static class NormalBattleEncounter
{
    public const int PoolSize = 8;
    private const int NormalEncounterOrderSalt = unchecked((int)0x31C41A7B);

    private enum EncounterTier
    {
        Weak,
        Strong,
    }

    private const int FrontLeft = 1;
    private const int FrontRight = 2;
    private const int BackLeft = 3;
    private const int BackRight = 4;

    public static List<int> BuildRunQueue(int regionIndex, int passIndex = 0)
    {
        EncounterDefinition[] pool = GetRegionPool(regionIndex);
        var weakIndices = new List<int>();
        var strongIndices = new List<int>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i].Tier == EncounterTier.Weak)
                weakIndices.Add(i);
            else
                strongIndices.Add(i);
        }

        var rng = GameInfo.CreateRunRng(
            RunRngStream.NormalBattle,
            regionIndex,
            NormalEncounterOrderSalt ^ passIndex
        );
        ShuffleInPlace(weakIndices, rng);
        ShuffleInPlace(strongIndices, rng);
        weakIndices.AddRange(strongIndices);
        return weakIndices;
    }

    public static List<EnemyRegedit> BuildFormation(int regionIndex, int formationIndex)
    {
        EncounterDefinition[] pool = GetRegionPool(regionIndex);
        if (pool.Length == 0)
            return new List<EnemyRegedit>();

        int index = Math.Clamp(formationIndex, 0, pool.Length - 1);
        return pool[index].Build?.Invoke() ?? new List<EnemyRegedit>();
    }

    private static EncounterDefinition[] GetRegionPool(int regionIndex) =>
        regionIndex > 0 ? RegionTwoEncounters : RegionOneEncounters;

    private static void ShuffleInPlace<T>(IList<T> values, Random rng)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }

    private readonly record struct EncounterDefinition(
        EncounterTier Tier,
        Func<List<EnemyRegedit>> Build
    );

    private static EncounterDefinition Weak(Func<List<EnemyRegedit>> build) =>
        new(EncounterTier.Weak, build);

    private static EncounterDefinition Strong(Func<List<EnemyRegedit>> build) =>
        new(EncounterTier.Strong, build);

    private static readonly EncounterDefinition[] RegionOneEncounters =
    [
        Weak(() => Formation([Slot<EvilRegedit>(2)])),
        Weak(() => Formation([Slot<FearWormRegedit>(2)])),
        Weak(() =>
            Formation([Slot<AlienBodyRegedit>(FrontLeft), Slot<AlienBodyRegedit>(FrontRight)])
        ),
        Strong(() => Formation([Slot<EvilRegedit>(FrontLeft), Slot<FearWormRegedit>(BackRight)])),
        Strong(() => Formation([Slot<EvilRegedit>(FrontRight), Slot<EvilRegedit>(BackLeft)])),
        Strong(() => Formation([Slot<FerociouessRegedit>(FrontRight)])),
        Strong(() =>
            Formation([
                Slot<AlienBodyRegedit>(FrontLeft),
                Slot<AlienBodyRegedit>(FrontRight),
                Slot<AlienBodyRegedit>(BackLeft),
            ])
        ),
        Strong(() => Formation([Slot<BlackHawkRegedit>(2), Slot<AlienBodyRegedit>(3)])),
    ];

    private static readonly EncounterDefinition[] RegionTwoEncounters =
    [
        Weak(() => Formation([Slot<RedHuskRegedit>(2)])),
        Weak(() => Formation([Slot<HollowBulwarkRegedit>(2)])),
        Weak(() => Formation([Slot<VoidRotorRegedit>(2)])),
        Strong(() => Formation([Slot<GraveWraithRegedit>(2)])),
        Strong(() => Formation([Slot<MarrowReaverRegedit>(FrontRight)])),
        Strong(() =>
            Formation([Slot<MarrowReaverRegedit>(FrontRight), Slot<RedHuskRegedit>(BackRight)])
        ),
    ];

    private readonly record struct EncounterSlot(Func<EnemyRegedit> CreateEnemy, int PositionIndex);

    private static EncounterSlot Slot<T>(int positionIndex)
        where T : EnemyRegedit, new() => new(() => new T().GetRegedit(), positionIndex);

    private static List<EnemyRegedit> Formation(EncounterSlot[] slots)
    {
        var list = new List<EnemyRegedit>(slots.Length);
        foreach (var slot in slots)
        {
            var enemy = slot.CreateEnemy();
            if (enemy == null)
                continue;

            enemy.PositionIndex = slot.PositionIndex;
            list.Add(enemy);
        }

        return list;
    }
}
