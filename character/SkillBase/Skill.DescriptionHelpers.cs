using System;
using System.Collections.Generic;
using System.Linq;

public partial class Skill
{
    protected const string UnfixedPlaceholder = "x";
    protected const int TooltipTotalMax = 999;
    public const string CarryKeyword = "\u8fde\u643a";
    public const string CarryKeywordEffectText =
        "\u968f\u673a\u4ece\u76ee\u6807\u7684\u62bd\u724c\u5806\u548c\u5f03\u724c\u5806\u4e2d\u6253\u51fa1\u5f20\u724c\uff0c\u53ef\u6307\u5b9a\u7c7b\u578b\uff0c\u4e0d\u6d88\u8017\u80fd\u91cf\u3002";
    public const string ExhaustKeyword = "\u6d88\u8017";
    public const string ExhaustKeywordEffectText =
        "\u6253\u51fa\u540e\uff0c\u672c\u573a\u6218\u6597\u4e2d\u79fb\u51fa\u3002";
    public const string RebirthKeyword = "\u590d\u751f";
    public const string RebirthKeywordEffectText =
        "\u53ef\u5bf9\u6fd2\u6b7b\u76ee\u6807\u751f\u6548\u3002\u9009\u62e9\u76ee\u6807\u65f6\uff0c\u4f18\u5148\u6fd2\u6b7b\u76ee\u6807\uff0c\u5176\u6b21\u975e\u6ee1\u8840\u76ee\u6807\u3002";
    private const string ExhaustKeywordLine = ExhaustKeyword + "\u3002";

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
        if (UseCompactBattleCardDescription)
            return clamped.ToString();

        return $"{basisText}(总计：{clamped})";
    }

    protected string WithBattleTotal(string basisText, string totalText)
    {
        if (!IsInBattle)
            return basisText;

        if (UseCompactBattleCardDescription)
            return totalText;

        return $"{basisText}(总计：{totalText})";
    }

    private bool UseCompactBattleCardDescription
    {
        get
        {
            UserSettings.EnsureLoaded();
            return IsInBattle && UserSettings.UseCompactBattleCardDescriptions;
        }
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
        IEnumerable<string> lines = plan?.DescribeLines() ?? Array.Empty<string>();
        if (ExhaustsAfterUse)
            lines = new[] { ExhaustKeywordLine }.Concat(lines);

        SetDescriptionLines(lines.ToArray());
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
        int clampMax = 9999,
        int times = 1
    )
    {
        times = Math.Max(1, times);
        int rawDamage = baseDamage + OwnerPower * powerMultiplier;
        string basisText = FormatBasePlusX(baseDamage, StatX.Power, powerMultiplier);
        if (times > 1)
            basisText = $"({basisText})*{times}";

        if (!IsInBattle)
            return basisText;

        int rawClampedDamage = Math.Clamp(rawDamage, 0, clampMax) * times;
        int modifiedDamage = 0;
        var previewState = new AttackBuff.PreviewState();
        for (int i = 0; i < times; i++)
        {
            modifiedDamage += Math.Clamp(
                AttackBuff.ApplyOutgoingDamageModifiers(
                    OwnerCharater,
                    rawDamage,
                    previewState: previewState
                ),
                0,
                clampMax
            );
        }

        if (UseCompactBattleCardDescription)
        {
            int totalDamage = modifiedDamage == rawClampedDamage ? rawClampedDamage : modifiedDamage;
            return BuildCompactBattleValueText(
                totalDamage,
                PropertyType.Power,
                powerMultiplier,
                times
            );
        }

        if (modifiedDamage == rawClampedDamage)
            return WithBattleTotal(basisText, rawClampedDamage.ToString());

        return WithBattleTotal(basisText, $"{rawClampedDamage}→{modifiedDamage}");
    }

    protected string BlockFromSurvivabilityText(
        int baseBlock = 0,
        int survivabilityMultiplier = 1,
        int clampMax = 999
    )
    {
        int totalBlock = baseBlock + OwnerSurvivability * survivabilityMultiplier;
        if (UseCompactBattleCardDescription)
        {
            return BuildCompactBattleValueText(
                Math.Clamp(totalBlock, 0, clampMax),
                PropertyType.Survivability,
                survivabilityMultiplier
            );
        }

        return BasePlusXWithBattleTotal(
            baseBlock,
            totalBlock,
            StatX.Survivability,
            xMultiplier: survivabilityMultiplier,
            clampMax: clampMax
        );
    }

    private static string BuildCompactBattleValueText(
        int total,
        PropertyType scalingProperty,
        int propertyMultiplier,
        int times = 1
    )
    {
        var hints = new List<string>();
        if (Math.Abs(propertyMultiplier) > 1)
            hints.Add($"{Math.Abs(propertyMultiplier)}\u500d{GetPropertyLabel(scalingProperty)}\u52a0\u6210");
        if (times > 1)
            hints.Add($"{times}\u6bb5");

        return hints.Count == 0
            ? total.ToString()
            : $"{total}\uff08{string.Join("\uff0c", hints)}\uff09";
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
        int clampMax = 9999,
        int times = 1
    ) => $"{prefix}{DamageFromPowerText(baseDamage, powerMultiplier, clampMax, times)}{suffix}";

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

    public static string BuildKeywordTooltipText(Skill skill)
    {
        if (skill == null)
            return string.Empty;

        var entries = new List<(int Index, string Text)>();
        if (skill.ExhaustsAfterUse)
            entries.Add((-1, BuildKeywordTooltipEntry(ExhaustKeyword, ExhaustKeywordEffectText)));

        string description = skill.Description ?? string.Empty;
        string plainDescription = StripBbCodeTags(description);

        int carryIndex = plainDescription.IndexOf(CarryKeyword, StringComparison.Ordinal);
        if (carryIndex >= 0)
            entries.Add((carryIndex, BuildKeywordTooltipEntry(CarryKeyword, CarryKeywordEffectText)));

        int rebirthIndex = plainDescription.IndexOf(RebirthKeyword, StringComparison.Ordinal);
        if (rebirthIndex >= 0)
            entries.Add(
                (rebirthIndex, BuildKeywordTooltipEntry(RebirthKeyword, RebirthKeywordEffectText))
            );

        foreach (Buff.BuffName buffName in Enum.GetValues(typeof(Buff.BuffName)))
        {
            string displayName = Buff.GetBuffDisplayName(buffName);
            if (string.IsNullOrWhiteSpace(displayName))
                continue;

            int matchIndex = plainDescription.IndexOf(displayName, StringComparison.Ordinal);
            if (matchIndex < 0)
                continue;

            string effectText = Buff.GetBuffEffectText(buffName);
            if (string.IsNullOrWhiteSpace(effectText))
                continue;

            entries.Add((matchIndex, BuildKeywordTooltipEntry(displayName, effectText)));
        }

        if (entries.Count == 0)
            return string.Empty;

        return string.Join(
            "\n\n",
            entries
                .OrderBy(entry => entry.Index)
                .Select(entry => entry.Text)
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
        );
    }

    private static string BuildKeywordTooltipEntry(string title, string effectText)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(effectText))
            return string.Empty;

        string formattedEffect = GlobalFunction.ColorizeKeywords(
            GlobalFunction.ColorizeNumbers(effectText)
        );
        return $"[outline_size=0][color=#a8f0ad]{title}[/color][/outline_size]\n{formattedEffect}";
    }

    private static string StripBbCodeTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        char[] buffer = new char[text.Length];
        int count = 0;
        bool inTag = false;

        foreach (char ch in text)
        {
            if (ch == '[')
            {
                inTag = true;
                continue;
            }

            if (ch == ']')
            {
                inTag = false;
                continue;
            }

            if (!inTag)
                buffer[count++] = ch;
        }

        return new string(buffer, 0, count);
    }
}
