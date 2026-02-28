using System;

public partial class Skill
{
    protected static string LineIf(bool condition, string line) => condition ? line : null;

    protected string Total(string basisText, int total, int clampMax = TooltipTotalMax) =>
        WithBattleTotal(basisText, total, clampMax);

    protected int BonusCastsFromEnergy(int costPerCast)
    {
        int energy = OwnerEnergy;
        return Math.Max(0, (int)Math.Ceiling((double)energy / costPerCast));
    }

    protected int CastTimesFromEnergy(int costPerCast, int baseCasts = 1) =>
        baseCasts + BonusCastsFromEnergy(costPerCast);

    protected string DamageFromPowerText(
        int baseDamage = 0,
        int powerMultiplier = 1,
        int clampMax = 9999
    )
    {
        int totalDamage = baseDamage + OwnerPower * powerMultiplier;
        return BasePlusXWithBattleTotal(
            baseDamage,
            totalDamage,
            StatX.Power,
            xMultiplier: powerMultiplier,
            clampMax: clampMax
        );
    }

    protected string BlockFromSurvivabilityText(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    )
    {
        int totalBlock = baseBlock + OwnerSurvivability * survivabilityMultiplier;
        return BasePlusXWithBattleTotal(
            baseBlock,
            totalBlock,
            StatX.Survivability,
            xMultiplier: survivabilityMultiplier,
            clampMax: clampMax
        );
    }

    protected string HealFromSurvivabilityText(
        int baseHeal = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    )
    {
        int totalHeal = baseHeal + OwnerSurvivability * survivabilityMultiplier;
        return BasePlusXWithBattleTotal(
            baseHeal,
            totalHeal,
            StatX.Survivability,
            xMultiplier: survivabilityMultiplier,
            clampMax: clampMax
        );
    }

    protected static string GainPropertyText(PropertyType type, int value) =>
        $"+{value}{GetColoredPropertyLabel(type)}";

    protected static string LosePropertyText(PropertyType type, int value) =>
        $"-{value}{GetColoredPropertyLabel(type)}";

    protected static string DeltaPropertyText(PropertyType type, int delta) =>
        delta >= 0 ? GainPropertyText(type, delta) : LosePropertyText(type, -delta);

    protected static string BuffStacksText(Buff.BuffName buff, int stacks) =>
        $"{stacks}层{buff.GetDescription()}";

    protected string DamageLine(
        int baseDamage = 0,
        int powerMultiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int clampMax = 9999
    ) => $"{prefix}{DamageFromPowerText(baseDamage, powerMultiplier, clampMax)}{suffix}";

    protected string BlockLine(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        string prefix = "获得",
        string suffix = "点格挡。",
        int clampMax = 999
    ) => $"{prefix}{BlockFromSurvivabilityText(baseBlock, survivabilityMultiplier, clampMax)}{suffix}";

    protected static string GainLine(PropertyType type, int value, string prefix = "获得") =>
        $"{prefix}{GainPropertyText(type, value)}。";

    protected static string BuffLine(Buff.BuffName buff, int stacks, string prefix = "获得") =>
        $"{prefix}{BuffStacksText(buff, stacks)}。";

    protected string EnergyXText() => X(StatX.Energy);

    protected string CastTimesFromEnergyText(int costPerCast, int baseCasts = 1)
    {
        int castTimes = CastTimesFromEnergy(costPerCast, baseCasts);

        string energyX = EnergyXText();
        string castTimesBasis =
            costPerCast == 1 ? $"{baseCasts}+{energyX}" : $"{baseCasts}+ceil({energyX}/{costPerCast})";
        return WithBattleTotal(castTimesBasis, castTimes);
    }
}
