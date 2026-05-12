using System;

public static partial class GameInfo
{
    public const int BaseBattleItemDropChance = 30;
    private const int BattleDropChanceStep = 10;

    public static void ResetBattleRewardDropState()
    {
        BattleItemDropChance = BaseBattleItemDropChance;
    }

    public static string BuildBattleRewardDropPreviewText()
    {
        int itemChance = NormalizeDropChance(BattleItemDropChance, BaseBattleItemDropChance);
        return $"下一场战斗掉率：\n道具 {itemChance}%";
    }

    public static bool RollBattleItemDrop(Random rng)
    {
        int chance = NormalizeDropChance(BattleItemDropChance, BaseBattleItemDropChance);
        bool dropped = RollChance(rng, chance);
        BattleItemDropChance = dropped ? BaseBattleItemDropChance : IncreaseDropChance(chance);
        return dropped;
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

    private static bool RollChance(Random rng, int chance)
    {
        rng ??= new Random();
        return rng.Next(100) < Math.Clamp(chance, 0, 100);
    }
}
