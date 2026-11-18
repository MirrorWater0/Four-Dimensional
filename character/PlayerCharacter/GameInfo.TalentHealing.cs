using System;
using System.Linq;

public static partial class GameInfo {
    public static int ApplyTalentMapHealingBonus(int amount) {
        if (amount <= 0 || PlayerCharacters == null)
            return Math.Max(0, amount);
        int flatBonus = PlayerCharacters.Count(info => TalentTree.HasUnlocked(info, "Mariya.HealingPlus"));
        int percentBonuses = PlayerCharacters.Count(info => TalentTree.HasUnlocked(info, "Mariya.MapHealing"));
        long scaledHealing = ((long)amount * (100L + percentBonuses * 10L) + 99L) / 100L;
        return (int)Math.Min(int.MaxValue, scaledHealing + flatBonus);
    }
}
