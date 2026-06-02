using System;
using System.Collections.Generic;
using System.Linq;

public partial class Skill
{
    protected const string UnfixedPlaceholder = "x";
    protected const int TooltipTotalMax = 999;
    public const string CarryKeyword = "连携";
    public const string CarryKeywordEffectText =
        "随机从目标的抽牌堆和弃牌堆中打出1张牌，可指定类型，不消耗能量。";
    public const string ExhaustKeyword = "消耗";
    public const string ExhaustKeywordEffectText =
        "打出后，本场战斗中移出。";
    public const string VoidnessKeyword = "虚无";
    public const string VoidnessKeywordEffectText =
        "回合结束时若在手牌中则消耗。";
    public const string RebirthKeyword = "复生";
    public const string RebirthKeywordEffectText =
        "可对濒死目标生效。";

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
            StatX.Speed => I18n.Tr("property.speed", "速度"),
            StatX.Energy => I18n.Tr("keyword.energy", "能量"),
            StatX.Life => I18n.Tr("ui.common.life", "生命"),
            StatX.MaxLife => I18n.Tr("property.max_life", "最大生命"),
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
        string color = GetStatColor(stat);
        return $"[color={color}]{UnfixedPlaceholder}[/color]";
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
            lines = new[] { GetExhaustKeywordLine() }.Concat(lines);

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
        int multiplier = 1,
        int clampMax = 9999,
        int times = 1
    )
    {
        times = Math.Max(1, times);
        int rawDamage = baseDamage + OwnerPower * multiplier;
        string basisText = FormatBasePlusX(baseDamage, StatX.Power, multiplier);
        if (times > 1)
            basisText = $"({basisText})*{times}";

        if (!IsInBattle)
            return basisText;

        int rawClampedDamage = Math.Clamp(rawDamage, 0, clampMax) * times;
        var previewState = new AttackBuff.PreviewState();
        int modifiedDamage = Math.Clamp(
            AttackBuff.ApplyOutgoingDamageModifiers(
                OwnerCharater,
                rawDamage,
                previewState: previewState
            ),
            0,
            clampMax
        ) * times;

        if (UseCompactBattleCardDescription)
        {
            int totalDamage = modifiedDamage == rawClampedDamage ? rawClampedDamage : modifiedDamage;
            return BuildCompactBattleValueText(
                totalDamage,
                PropertyType.Power,
                multiplier,
                times
            );
        }

        if (modifiedDamage == rawClampedDamage)
            return WithBattleTotal(basisText, rawClampedDamage.ToString());

        return WithBattleTotal(basisText, $"{rawClampedDamage}→{modifiedDamage}");
    }

    protected string BlockFromSurvivabilityText(
        int baseBlock = 0,
        int multiplier = 1,
        int clampMax = 999
    )
    {
        int totalBlock = baseBlock + OwnerSurvivability * multiplier;
        if (UseCompactBattleCardDescription)
        {
            return BuildCompactBattleValueText(
                Math.Clamp(totalBlock, 0, clampMax),
                PropertyType.Survivability,
                multiplier
            );
        }

        return BasePlusXWithBattleTotal(
            baseBlock,
            totalBlock,
            StatX.Survivability,
            xMultiplier: multiplier,
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
            hints.Add($"{Math.Abs(propertyMultiplier)}倍{GetPropertyLabel(scalingProperty)}加成");
        if (times > 1)
            hints.Add($"{times}段");

        return hints.Count == 0
            ? total.ToString()
            : $"{total}（{string.Join("，", hints)}）";
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
        int multiplier = 1,
        string prefix = "造成",
        string suffix = "点伤害。",
        int clampMax = 9999,
        int times = 1
    ) => $"{prefix}{DamageFromPowerText(baseDamage, multiplier, clampMax, times)}{suffix}";

    protected string BlockLine(
        int baseBlock = 0,
        int multiplier = 1,
        string prefix = "获得",
        string suffix = "点格挡。",
        int clampMax = 999
    ) => $"{prefix}{BlockFromSurvivabilityText(baseBlock, multiplier, clampMax)}{suffix}";

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
        {
            entries.Add(
                (-1, BuildKeywordTooltipEntry(GetExhaustKeyword(), GetExhaustKeywordEffectText()))
            );
        }

        string description = skill.Description ?? string.Empty;
        string plainDescription = StripBbCodeTags(description);

        int carryIndex = FindKeywordIndex(plainDescription, GetCarryKeyword(), CarryKeyword);
        if (carryIndex >= 0)
        {
            entries.Add(
                (carryIndex, BuildKeywordTooltipEntry(GetCarryKeyword(), GetCarryKeywordEffectText()))
            );
        }

        int voidnessIndex = FindKeywordIndex(plainDescription, GetVoidnessKeyword(), VoidnessKeyword);
        if (voidnessIndex >= 0)
            entries.Add(
                (
                    voidnessIndex,
                    BuildKeywordTooltipEntry(
                        GetVoidnessKeyword(),
                        GetVoidnessKeywordEffectText()
                    )
                )
            );

        int rebirthIndex = FindValidRebirthKeywordIndex(
            plainDescription,
            GetRebirthKeyword(),
            RebirthKeyword
        );
        if (rebirthIndex >= 0)
            entries.Add(
                (
                    rebirthIndex,
                    BuildKeywordTooltipEntry(GetRebirthKeyword(), GetRebirthKeywordEffectText())
                )
            );

        AddStatXTooltipEntries(entries, description, plainDescription);

        foreach (Buff.BuffName buffName in Enum.GetValues(typeof(Buff.BuffName)))
        {
            string displayName = Buff.GetBuffDisplayName(buffName);
            string fallbackName = buffName.GetDescription();
            if (string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(fallbackName))
                continue;

            int matchIndex = FindKeywordIndex(plainDescription, displayName, fallbackName);
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
                .DistinctBy(entry => entry.Text)
                .Select(entry => entry.Text)
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
        );
    }

    private static void AddStatXTooltipEntries(
        List<(int Index, string Text)> entries,
        string description,
        string plainDescription
    )
    {
        if (string.IsNullOrWhiteSpace(plainDescription))
            return;

        int xIndex = FindUnfixedPlaceholderIndex(plainDescription);
        if (xIndex < 0)
            return;

        string effectText = BuildStatXTooltipEffectText(description);
        if (string.IsNullOrWhiteSpace(effectText))
            return;

        entries.Add((xIndex, BuildKeywordTooltipEntry(UnfixedPlaceholder, effectText)));
    }

    private static string BuildStatXTooltipEffectText(string description)
    {
        var lines = new List<string>();
        AddStatXTooltipLine(lines, description, StatX.Power, PropertyType.Power);
        AddStatXTooltipLine(lines, description, StatX.Survivability, PropertyType.Survivability);
        AddStatXTooltipLine(
            lines,
            description,
            StatX.Energy,
            I18n.Tr("keyword.energy", "能量")
        );

        return string.Join("\n", lines);
    }

    private static void AddStatXTooltipLine(
        List<string> lines,
        string description,
        StatX stat,
        PropertyType propertyType
    ) => AddStatXTooltipLine(lines, description, stat, GetPropertyLabel(propertyType));

    private static void AddStatXTooltipLine(
        List<string> lines,
        string description,
        StatX stat,
        string label
    )
    {
        if (string.IsNullOrWhiteSpace(description) || !description.Contains(X(stat), StringComparison.Ordinal))
            return;

        lines.Add(I18n.Format(
            "keyword.x.stat_line",
            "{x}为{stat}。",
            ("x", X(stat)),
            ("stat", label)
        ));
    }

    private static int FindValidRebirthKeywordIndex(
        string plainDescription,
        string localizedKeyword,
        string fallbackKeyword
    )
    {
        if (string.IsNullOrEmpty(plainDescription))
            return -1;

        int localizedIndex = FindKeywordIndex(plainDescription, localizedKeyword, fallbackKeyword);
        if (localizedIndex < 0)
            return -1;

        string matchedKeyword = MatchKeyword(plainDescription, localizedIndex, localizedKeyword)
            ? localizedKeyword
            : fallbackKeyword;
        if (!string.Equals(matchedKeyword, RebirthKeyword, StringComparison.Ordinal))
            return localizedIndex;

        int searchStart = 0;
        while (searchStart < plainDescription.Length)
        {
            int index = plainDescription.IndexOf(RebirthKeyword, searchStart, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            if (GlobalFunction.IsValidRebirthKeywordMatch(plainDescription, index))
                return index;

            searchStart = index + 1;
        }

        return -1;
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

    private static string GetCarryKeyword() => I18n.Tr("keyword.carry", CarryKeyword);

    private static string GetCarryKeywordEffectText() =>
        I18n.Tr("keyword.carry.effect", CarryKeywordEffectText);

    private static string GetExhaustKeyword() => I18n.Tr("keyword.exhaust", ExhaustKeyword);

    private static string GetExhaustKeywordEffectText() =>
        I18n.Tr("keyword.exhaust.effect", ExhaustKeywordEffectText);

    private static string GetVoidnessKeyword() => I18n.Tr("keyword.voidness", VoidnessKeyword);

    private static string GetVoidnessKeywordEffectText() =>
        I18n.Tr("keyword.voidness.effect", VoidnessKeywordEffectText);

    private static string GetRebirthKeyword() => I18n.Tr("keyword.rebirth", RebirthKeyword);

    private static string GetRebirthKeywordEffectText() =>
        I18n.Tr("keyword.rebirth.effect", RebirthKeywordEffectText);

    private static string GetExhaustKeywordLine() => $"{GetExhaustKeyword()}。";

    private static int FindKeywordIndex(
        string text,
        string localizedKeyword,
        string fallbackKeyword
    )
    {
        int localizedIndex = FindTokenIndex(text, localizedKeyword);
        int fallbackIndex = FindTokenIndex(text, fallbackKeyword);

        if (localizedIndex < 0)
            return fallbackIndex;
        if (fallbackIndex < 0)
            return localizedIndex;
        return Math.Min(localizedIndex, fallbackIndex);
    }

    private static int FindTokenIndex(string text, string token)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(token))
            return -1;

        int searchStart = 0;
        while (searchStart <= text.Length - token.Length)
        {
            int index = text.IndexOf(token, searchStart, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return -1;

            if (!ContainsLatinOrDigit(token) || HasWordBoundaryAround(text, index, token.Length))
                return index;

            searchStart = index + 1;
        }

        return -1;
    }

    private static int FindUnfixedPlaceholderIndex(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return -1;

        int searchStart = 0;
        while (searchStart < text.Length)
        {
            int index = text.IndexOf(UnfixedPlaceholder, searchStart, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return -1;

            int endIndex = index + UnfixedPlaceholder.Length;
            bool rightOk = endIndex >= text.Length || !IsWordChar(text[endIndex]);
            if (rightOk)
                return index;

            searchStart = index + 1;
        }

        return -1;
    }

    private static bool MatchKeyword(string text, int index, string token)
    {
        return !string.IsNullOrWhiteSpace(token)
            && index >= 0
            && index + token.Length <= text.Length
            && string.Equals(
                text.Substring(index, token.Length),
                token,
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static bool HasWordBoundaryAround(string input, int index, int length)
    {
        bool leftOk = index == 0 || !IsWordChar(input[index - 1]);
        int endIndex = index + length;
        bool rightOk = endIndex >= input.Length || !IsWordChar(input[endIndex]);
        return leftOk && rightOk;
    }

    private static bool ContainsLatinOrDigit(string text) => text.Any(IsWordChar);

    private static bool IsWordChar(char ch) =>
        (ch >= 'a' && ch <= 'z')
        || (ch >= 'A' && ch <= 'Z')
        || char.IsDigit(ch)
        || ch == '_';
}
