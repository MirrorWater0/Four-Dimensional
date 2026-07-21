using System;
using System.Collections.Generic;
using System.Linq;

public partial class Skill
{
    protected const string UnfixedPlaceholder = "x";
    protected const int TooltipTotalMax = 999;
    public const string CarryKeyword = "连携";
    public const string CarryKeywordEffectText =
        "随机从抽牌堆中打出1张目标所属的牌，可指定类型，不消耗能量。";
    public const string ExhaustKeyword = "消耗";
    public const string ExhaustKeywordEffectText =
        "打出后，本场战斗中移出。";
    public const string RetainKeyword = "保留";
    public const string RetainKeywordEffectText =
        "回合结束时若在手牌中，不会被丢弃。";
    public const string VoidnessKeyword = "虚无";
    public const string VoidnessKeywordEffectText =
        "回合结束时若在手牌中则消耗。";
    public const string ColorlessKeyword = "无色";
    public const string ColorlessKeywordEffectText =
        "不属于任何角色的牌。";
    public const string RebirthKeyword = "复生";
    public const string RebirthKeywordEffectText =
        "可对濒死目标生效。";

    public enum StatX
    {
        Power,
        Survivability,
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
            _ => "white",
        };
    }

    private static string GetStatLabel(StatX stat)
    {
        return stat switch
        {
            StatX.Power => GetPropertyLabel(PropertyType.Power),
            StatX.Survivability => GetPropertyLabel(PropertyType.Survivability),
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
        int clamped = Math.Clamp(total, 0, clampMax);
        if (!IsInBattle)
        {
            if (UseFormulaCardDescription)
                return basisText;

            return clamped.ToString();
        }

        if (!UseFormulaCardDescription)
            return clamped.ToString();

        return $"{basisText}(总计：{clamped})";
    }

    protected string WithBattleTotal(string basisText, string totalText)
    {
        if (!IsInBattle)
        {
            if (UseFormulaCardDescription)
                return basisText;

            return totalText;
        }

        if (!UseFormulaCardDescription)
            return totalText;

        return $"{basisText}(总计：{totalText})";
    }

    private bool UseFormulaCardDescription
    {
        get
        {
            UserSettings.EnsureLoaded();
            return UserSettings.UseFormulaCardDescriptions;
        }
    }

    private bool UseZeroScalingStatsForDescription =>
        !IsInBattle && !UseFormulaCardDescription;

    protected int DescriptionPower =>
        UseZeroScalingStatsForDescription ? 0 : OwnerPower;

    protected int DescriptionSurvivability =>
        UseZeroScalingStatsForDescription ? 0 : OwnerSurvivability;

    protected int DescriptionEnergy =>
        UseZeroScalingStatsForDescription ? 0 : OwnerEnergy;

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
        if (ShowsRetainKeyword)
            lines = new[] { GetRetainKeywordLine() }.Concat(lines);
        if (IsColorless)
            lines = new[] { GetColorlessKeywordLine() }.Concat(lines);
        if (ShowsExhaustKeyword)
            lines = new[] { GetExhaustKeywordLine() }.Concat(lines);
        if (ShowsVoidnessKeyword)
            lines = new[] { GetVoidnessKeywordLine() }.Concat(lines);

        SetDescriptionLines(lines.ToArray());
    }

    protected static string LineIf(bool condition, string line) => condition ? line : null;

    protected string Total(string basisText, int total, int clampMax = TooltipTotalMax) =>
        WithBattleTotal(basisText, total, clampMax);

    protected int BonusCastsFromEnergy(int costPerCast)
    {
        int energy = DescriptionEnergy;
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
        int rawDamage = baseDamage + DescriptionPower * multiplier;
        string basisText = FormatBasePlusX(baseDamage, StatX.Power, multiplier);
        if (times > 1)
            basisText = $"({basisText})*{times}";

        if (!IsInBattle)
        {
            if (UseFormulaCardDescription)
                return basisText;

            int previewDamage =
                (
                    ApplyOwnerSkillDamageScaling(Math.Clamp(rawDamage, 0, clampMax))
                    + GetOwnerSkillAttackDamageBonus()
                ) * times;
            return BuildCompactBattleValueText(
                previewDamage,
                PropertyType.Power,
                multiplier,
                times
            );
        }

        rawDamage = baseDamage + OwnerPower * multiplier;

        int scaledDamage =
            ApplyOwnerSkillDamageScaling(Math.Clamp(rawDamage, 0, clampMax))
            + GetOwnerSkillAttackDamageBonus();
        int rawClampedDamage = scaledDamage * times;
        var previewState = new AttackBuff.PreviewState();
        int modifiedDamage = Math.Clamp(
            AttackBuff.ApplyOutgoingDamageModifiers(
                OwnerCharater,
                scaledDamage,
                previewState: previewState
            ),
            0,
            clampMax
        ) * times;

        if (!UseFormulaCardDescription)
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
        int totalBlock = baseBlock + DescriptionSurvivability * multiplier;
        if (!IsInBattle)
        {
            return BasePlusXWithBattleTotal(
                baseBlock,
                totalBlock,
                StatX.Survivability,
                xMultiplier: multiplier,
                clampMax: clampMax
            );
        }

        totalBlock = baseBlock + OwnerSurvivability * multiplier;
        if (!UseFormulaCardDescription)
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

        skill.UpdateDescription();
        SkillTooltipHints hints = skill.CollectTooltipHints();
        return BuildKeywordTooltipText(skill, hints);
    }

    public static string BuildKeywordTooltipText(Skill skill, SkillTooltipHints hints)
    {
        if (skill == null)
            return string.Empty;

        hints ??= skill.CollectTooltipHints();
        var entries = new List<string>();

        if (skill.ShowsExhaustKeyword)
        {
            entries.Add(
                BuildKeywordTooltipEntry(GetExhaustKeyword(), GetExhaustKeywordEffectText())
            );
        }
        if (skill.ShowsRetainKeyword)
        {
            entries.Add(
                BuildKeywordTooltipEntry(GetRetainKeyword(), GetRetainKeywordEffectText())
            );
        }
        if (skill.IsColorless)
        {
            entries.Add(
                BuildKeywordTooltipEntry(GetColorlessKeyword(), GetColorlessKeywordEffectText())
            );
        }

        if (hints.Keywords.Contains(SkillTooltipKeyword.Carry))
        {
            entries.Add(
                BuildKeywordTooltipEntry(GetCarryKeyword(), GetCarryKeywordEffectText())
            );
        }
        if (hints.Keywords.Contains(SkillTooltipKeyword.Voidness))
        {
            entries.Add(
                BuildKeywordTooltipEntry(
                    GetVoidnessKeyword(),
                    GetVoidnessKeywordEffectText()
                )
            );
        }
        if (hints.Keywords.Contains(SkillTooltipKeyword.Rebirth))
        {
            entries.Add(
                BuildKeywordTooltipEntry(GetRebirthKeyword(), GetRebirthKeywordEffectText())
            );
        }

        UserSettings.EnsureLoaded();
        if (!UserSettings.HideStatXKeywordTooltips)
        {
            foreach (StatX stat in OrderStatVariables(hints.StatVariables))
            {
                string effectText = BuildStatXTooltipEffectText(stat);
                if (!string.IsNullOrWhiteSpace(effectText))
                {
                    entries.Add(BuildKeywordTooltipEntry(UnfixedPlaceholder, effectText));
                }
            }
        }

        foreach (Buff.BuffName buffName in hints.Buffs)
        {
            string displayName = Buff.GetBuffDisplayName(buffName);
            string effectText = Buff.GetBuffEffectText(buffName);
            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(effectText))
                continue;

            entries.Add(BuildKeywordTooltipEntry(displayName, effectText));
        }

        if (entries.Count == 0)
            return string.Empty;

        return string.Join(
            "\n\n",
            entries.Where(entry => !string.IsNullOrWhiteSpace(entry)).Distinct()
        );
    }

    private static IEnumerable<StatX> OrderStatVariables(IEnumerable<StatX> stats)
    {
        StatX[] order =
        {
            StatX.Power,
            StatX.Survivability,
            StatX.Energy,
            StatX.Life,
            StatX.MaxLife,
        };

        foreach (StatX stat in order)
        {
            if (stats.Contains(stat))
                yield return stat;
        }
    }

    private static string BuildStatXTooltipEffectText(StatX stat)
    {
        return stat switch
        {
            StatX.Power => I18n.Format(
                "keyword.x.stat_line",
                "{x}为{stat}。",
                ("x", X(stat)),
                ("stat", GetPropertyLabel(PropertyType.Power))
            ),
            StatX.Survivability => I18n.Format(
                "keyword.x.stat_line",
                "{x}为{stat}。",
                ("x", X(stat)),
                ("stat", GetPropertyLabel(PropertyType.Survivability))
            ),
            StatX.Energy => I18n.Format(
                "keyword.x.stat_line",
                "{x}为{stat}。",
                ("x", X(stat)),
                ("stat", I18n.Tr("keyword.energy", "能量"))
            ),
            StatX.Life => I18n.Format(
                "keyword.x.stat_line",
                "{x}为{stat}。",
                ("x", X(stat)),
                ("stat", I18n.Tr("ui.common.life", "生命"))
            ),
            StatX.MaxLife => I18n.Format(
                "keyword.x.stat_line",
                "{x}为{stat}。",
                ("x", X(stat)),
                ("stat", I18n.Tr("property.max_life", "最大生命"))
            ),
            _ => string.Empty,
        };
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

    private static string GetRetainKeyword() => I18n.Tr("keyword.retain", RetainKeyword);

    private static string GetRetainKeywordEffectText() =>
        I18n.Tr("keyword.retain.effect", RetainKeywordEffectText);

    private static string GetVoidnessKeyword() => I18n.Tr("keyword.voidness", VoidnessKeyword);

    private static string GetVoidnessKeywordEffectText() =>
        I18n.Tr("keyword.voidness.effect", VoidnessKeywordEffectText);

    private static string GetColorlessKeyword() => I18n.Tr("keyword.colorless", ColorlessKeyword);

    private static string GetColorlessKeywordEffectText() =>
        I18n.Tr("keyword.colorless.effect", ColorlessKeywordEffectText);

    private static string GetRebirthKeyword() => I18n.Tr("keyword.rebirth", RebirthKeyword);

    private static string GetRebirthKeywordEffectText() =>
        I18n.Tr("keyword.rebirth.effect", RebirthKeywordEffectText);

    private static string GetExhaustKeywordLine() => $"{GetExhaustKeyword()}。";
    private static string GetRetainKeywordLine() => $"{GetRetainKeyword()}。";
    private static string GetVoidnessKeywordLine() => $"{GetVoidnessKeyword()}。";
    private static string GetColorlessKeywordLine() => $"{GetColorlessKeyword()}。";
}
