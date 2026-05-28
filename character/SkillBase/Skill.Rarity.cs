using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Skill
{
    public enum SkillRarity
    {
        Common,
        Uncommon,
        Rare,
    }

    public virtual SkillRarity Rarity => SkillRarity.Common;

    public static SkillRarity GetRarity(SkillID? skillId)
    {
        if (!skillId.HasValue)
            return SkillRarity.Common;

        return GetSkill(skillId.Value)?.Rarity ?? SkillRarity.Common;
    }

    public static Color GetRarityBorderColor(SkillRarity rarity)
    {
        return rarity switch
        {
            SkillRarity.Uncommon => new Color(0.44f, 0.67f, 1.0f, 1.0f),
            SkillRarity.Rare => new Color(0.95f, 0.78f, 0.42f, 1.0f),
            _ => 0.7f * new Color(0.95f, 0.95f, 0.95f, 1.0f),
        };
    }

    public static SkillRarity RollRewardRarity(Random rng, bool allowRare = true)
    {
        int roll = rng?.Next(100) ?? 0;
        if (roll < 60)
            return SkillRarity.Common;
        if (!allowRare || roll < 95)
            return SkillRarity.Uncommon;
        return SkillRarity.Rare;
    }

    public static SkillID[] FilterSkillPoolByRarity(IEnumerable<SkillID> pool, SkillRarity rarity)
    {
        return (pool ?? Array.Empty<SkillID>())
            .Where(skillId => GetRarity(skillId) == rarity)
            .ToArray();
    }

    public static SkillRarity[] GetRewardRarityFallbackOrder(SkillRarity rarity, bool allowRare = true)
    {
        if (!allowRare)
        {
            return rarity switch
            {
                SkillRarity.Uncommon => [SkillRarity.Uncommon, SkillRarity.Common],
                _ => [SkillRarity.Common, SkillRarity.Uncommon],
            };
        }

        return rarity switch
        {
            SkillRarity.Rare => [SkillRarity.Rare, SkillRarity.Uncommon, SkillRarity.Common],
            SkillRarity.Uncommon => [SkillRarity.Uncommon, SkillRarity.Common, SkillRarity.Rare],
            _ => [SkillRarity.Common, SkillRarity.Uncommon, SkillRarity.Rare],
        };
    }
}
