using System;
using System.Collections.Generic;

/// <summary>
/// 精英战斗固定遭遇表。整局队列由 GameInfo.InitializeEliteEncounterQueue 预生成。
/// </summary>
public static class EliteBattleEncounter
{
    public const int PoolSize = 2;
    private const int EliteEncounterOrderSalt = unchecked((int)0x52B7E4C9);

    public static List<int> BuildRunQueue(int regionIndex, int passIndex = 0)
    {
        var indices = new List<int> { 0, 1 };
        var rng = GameInfo.CreateRunRng(
            RunRngStream.EliteBattle,
            regionIndex,
            EliteEncounterOrderSalt ^ passIndex
        );
        ShuffleInPlace(indices, rng);
        return indices;
    }

    public static EnemyRegedit BuildElite(int regionIndex, int catalogIndex)
    {
        EnemyRegedit[] catalog = GetCatalog(regionIndex);
        if (catalog.Length == 0)
            return new ArroganceRegedit().GetRegedit();

        int index = Math.Clamp(catalogIndex, 0, catalog.Length - 1);
        return catalog[index].GetRegedit();
    }

    private static EnemyRegedit[] GetCatalog(int regionIndex) =>
        regionIndex > 0
            ? [new FearEliteRegedit(), new EnvyEliteRegedit()]
            : [new ArroganceRegedit(), new AngerEliteRegedit()];

    private static void ShuffleInPlace<T>(IList<T> values, Random rng)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }
}
