using System;
using System.Linq;

public partial class Skill
{
    protected const string UnfixedPlaceholder = "x";
    protected const int TooltipTotalMax = 999;

    protected enum StatX
    {
        Power,
        Survivability,
        Speed,
        Energy,
        Life,
        MaxLife,
    }

    public static string GetPropertyLabel(PropertyType type) => type.GetDescription();

    public static string GetColoredPropertyLabel(PropertyType type)
    {
        return $"[color={GetPropertyColor(type)}]{GetPropertyLabel(type)}[/color]";
    }

    private static string GetPropertyColor(PropertyType type)
    {
        return type switch
        {
            PropertyType.Power => "#ff0000",
            PropertyType.Survivability => "#89fffd",
            PropertyType.Speed => "#b56bff",
            _ => "white",
        };
    }

    private static string GetStatLabel(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyLabel(PropertyType.Power),
            StatX.Survivability => GetPropertyLabel(PropertyType.Survivability),
            StatX.Speed => "速度",
            StatX.Energy => "能量",
            StatX.Life => "生命",
            StatX.MaxLife => "最大生命",
            _ => string.Empty,
        };
    }

    private static string GetStatColor(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyColor(PropertyType.Power),
            StatX.Survivability => GetPropertyColor(PropertyType.Survivability),
            StatX.Speed => "#b56bff",
            StatX.Energy => "#5353ff",
            StatX.Life => "#6bff6b",
            StatX.MaxLife => "#6bff6b",
            _ => "white",
        };
    }

    protected void SetDescriptionText(string text)
    {
        string output = GlobalFunction.ColorizeNumbers(text ?? string.Empty);
        Description = GlobalFunction.ColorizeKeywords(output);
    }

    protected void SetDescriptionLines(params string[] lines)
    {
        var filtered = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        string text = string.Join("\n", filtered);
        SetDescriptionText(text);
    }

    protected static string X(StatX stat)
    {
        string label = GetStatLabel(stat);
        if (string.IsNullOrWhiteSpace(label))
            return UnfixedPlaceholder;

        string color = GetStatColor(stat);
        return $"[color={color}]{UnfixedPlaceholder}({label})[/color]";
    }

    protected static string FormatBasePlusX(int baseValue, StatX stat, int xMultiplier = 1)
    {
        string x = X(stat);
        string xPart = xMultiplier switch
        {
            1 => x,
            -1 => $"-{x}",
            _ => $"{xMultiplier}{x}",
        };

        if (baseValue == 0)
            return xPart;

        if (xPart.StartsWith("-", StringComparison.Ordinal))
            return $"{baseValue}{xPart}";

        return $"{baseValue}+{xPart}";
    }

    protected string WithBattleTotal(string basisText, int total, int clampMax = TooltipTotalMax)
    {
        if (!IsInBattle)
            return basisText;

        int clamped = Math.Clamp(total, 0, clampMax);
        return $"{basisText}(总计：{clamped})";
    }

    protected string WithBattleTotal(string basisText, string totalText)
    {
        if (!IsInBattle)
            return basisText;

        return $"{basisText}(总计：{totalText})";
    }

    protected string XWithBattleTotal(StatX stat, int total, int clampMax = TooltipTotalMax) =>
        WithBattleTotal(X(stat), total, clampMax);

    protected string BasePlusXWithBattleTotal(
        int baseValue,
        int total,
        StatX stat,
        int xMultiplier = 1,
        int clampMax = TooltipTotalMax
    ) => WithBattleTotal(FormatBasePlusX(baseValue, stat, xMultiplier), total, clampMax);

    public virtual void UpdateDescription()
    {
        var plan = GetPlan();
        if (plan != null)
        {
            SetDescriptionLines(plan.DescribeLines());
        }
    }

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
        int rawDamage = baseDamage + OwnerPower * powerMultiplier;
        string basisText = FormatBasePlusX(baseDamage, StatX.Power, powerMultiplier);

        if (!IsInBattle)
            return basisText;

        int rawClampedDamage = Math.Clamp(rawDamage, 0, clampMax);
        int modifiedDamage = Math.Clamp(
            AttackBuff.ApplyOutgoingDamageModifiers(
                OwnerCharater,
                rawDamage,
                previewState: new AttackBuff.PreviewState()
            ),
            0,
            clampMax
        );

        if (modifiedDamage == rawClampedDamage)
            return WithBattleTotal(basisText, rawClampedDamage, clampMax);

        return WithBattleTotal(basisText, $"{rawClampedDamage}→{modifiedDamage}");
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
