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

    private static readonly Dictionary<SkillID, SkillRarity> SkillRarityCatalog =
        BuildSkillRarityCatalog();

    public SkillRarity Rarity => GetRarity(SkillId);

    public static SkillRarity GetRarity(SkillID? skillId)
    {
        if (!skillId.HasValue)
            return SkillRarity.Common;

        return SkillRarityCatalog.TryGetValue(skillId.Value, out SkillRarity rarity)
            ? rarity
            : SkillRarity.Common;
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

    public static SkillRarity RollRewardRarity(Random rng)
    {
        int roll = rng?.Next(100) ?? 0;
        if (roll < 60)
            return SkillRarity.Common;
        if (roll < 90)
            return SkillRarity.Uncommon;
        return SkillRarity.Rare;
    }

    public static SkillID[] FilterSkillPoolByRarity(IEnumerable<SkillID> pool, SkillRarity rarity)
    {
        return (pool ?? Array.Empty<SkillID>())
            .Where(skillId => GetRarity(skillId) == rarity)
            .ToArray();
    }

    public static SkillRarity[] GetRewardRarityFallbackOrder(SkillRarity rarity)
    {
        return rarity switch
        {
            SkillRarity.Rare => [SkillRarity.Rare, SkillRarity.Uncommon, SkillRarity.Common],
            SkillRarity.Uncommon => [SkillRarity.Uncommon, SkillRarity.Common, SkillRarity.Rare],
            _ => [SkillRarity.Common, SkillRarity.Uncommon, SkillRarity.Rare],
        };
    }

    private static Dictionary<SkillID, SkillRarity> BuildSkillRarityCatalog()
    {
        Dictionary<SkillID, SkillRarity> catalog = new();

        AddRarity(
            catalog,
            SkillRarity.Uncommon,
            SkillID.Extract,
            SkillID.BladeOfSlaughter,
            SkillID.BreakStrike,
            SkillID.SonicBoom,
            SkillID.DissonantField,
            SkillID.ReverbChain,
            SkillID.RelayShift,
            SkillID.Charge,
            SkillID.VulnerablePurge,
            SkillID.VulnerabilityStrike,
            SkillID.WeakpointBulwark,
            SkillID.HolySeal,
            SkillID.Vower,
            SkillID.VulnerabilityConversion,
            SkillID.ChargedBlade,
            SkillID.CrescentWind,
            SkillID.QuietVeil,
            SkillID.EnergyTransfer,
            SkillID.EnergyRelay,
            SkillID.GroupHealing,
            SkillID.TouchOfGod,
            SkillID.StasisBlade,
            SkillID.ContinuousPierce,
            SkillID.RuinBlade,
            SkillID.LongNight,
            SkillID.FlashOfLight,
            SkillID.AfterimageWard,
            SkillID.StarWard
        );

        AddRarity(
            catalog,
            SkillRarity.Rare,
            SkillID.DisasterImpact,
            SkillID.VoidForm,
            SkillID.EchoForm,
            SkillID.BarrierDuplication,
            SkillID.AegisPledge,
            SkillID.DemonForm,
            SkillID.RearlineRevival,
            SkillID.Ragnarok,
            SkillID.SanctuaryForm,
            SkillID.RequiemBloom,
            SkillID.CurtainCallMoment,
            SkillID.TwilightParadox,
            SkillID.ShadowForm
        );

        return catalog;
    }

    private static void AddRarity(
        Dictionary<SkillID, SkillRarity> catalog,
        SkillRarity rarity,
        params SkillID[] skillIds
    )
    {
        if (catalog == null || skillIds == null)
            return;

        for (int i = 0; i < skillIds.Length; i++)
            catalog[skillIds[i]] = rarity;
    }
}
