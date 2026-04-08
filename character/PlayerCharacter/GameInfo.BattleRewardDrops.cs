using System;

public static partial class GameInfo
{
    public const int BaseBattleItemDropChance = 30;
    public const int BaseBattleEquipmentDropChance = 10;
    private const int BattleDropChanceStep = 10;

    public static void ResetBattleRewardDropState()
    {
        BattleItemDropChance = BaseBattleItemDropChance;
        BattleEquipmentDropChance = BaseBattleEquipmentDropChance;
    }

    public static string BuildBattleRewardDropPreviewText()
    {
        int itemChance = NormalizeDropChance(BattleItemDropChance, BaseBattleItemDropChance);
        int equipmentChance = NormalizeDropChance(
            BattleEquipmentDropChance,
            BaseBattleEquipmentDropChance
        );

        return $"下一场战斗掉率：\n道具 {itemChance}%\n普通装备 {equipmentChance}%";
    }

    public static bool RollBattleItemDrop(Random rng)
    {
        int chance = NormalizeDropChance(BattleItemDropChance, BaseBattleItemDropChance);
        bool dropped = RollChance(rng, chance);
        BattleItemDropChance = dropped ? BaseBattleItemDropChance : IncreaseDropChance(chance);
        return dropped;
    }

    public static int RollBattleEquipmentDropCount(LevelNode.LevelType levelType, Random rng)
    {
        return levelType switch
        {
            LevelNode.LevelType.Boss => OnBattleEquipmentDropped(2),
            LevelNode.LevelType.Elite => OnBattleEquipmentDropped(1),
            _ => RollNormalBattleEquipmentDropCount(rng),
        };
    }

    private static int RollNormalBattleEquipmentDropCount(Random rng)
    {
        int chance = NormalizeDropChance(BattleEquipmentDropChance, BaseBattleEquipmentDropChance);
        bool dropped = RollChance(rng, chance);
        if (dropped)
            return OnBattleEquipmentDropped(1);

        BattleEquipmentDropChance = IncreaseDropChance(chance);
        return 0;
    }

    private static int OnBattleEquipmentDropped(int equipCount)
    {
        BattleEquipmentDropChance = BaseBattleEquipmentDropChance;
        return equipCount;
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
        if (rng == null)
            rng = new Random();

        return rng.Next(100) < Math.Clamp(chance, 0, 100);
    }
}
