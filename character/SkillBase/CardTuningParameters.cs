using System;
using System.Collections.Generic;

public static class CardTuningParameters {
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase) {
        ["EnergyCost"] = "玩家费用（-1 为 X）", ["EnemyEnergyCost"] = "敌方费用（-1 为 X）",
        ["ExhaustAfterExecutions"] = "执行几次后消耗（0 为不消耗）",
        ["BaseDamage"] = "基础伤害", ["BaseBlock"] = "基础格挡", ["BaseHeal"] = "基础治疗",
        ["Multiplier"] = "属性倍率", ["HitCount"] = "攻击次数", ["LoopCount"] = "循环次数",
        ["PowerMultiplier"] = "力量倍率", ["SurvivabilityMultiplier"] = "生存倍率",
        ["PowerGain"] = "力量增加", ["SurvivabilityGain"] = "生存增加",
        ["BlockPerPower"] = "每获得1点力量所需格挡",
        ["SurvivabilityLoss"] = "生存减少", ["SelfPowerGain"] = "自身力量增加",
        ["Spower"] = "自身力量增加",
        ["SelfSurvivabilityGain"] = "自身生存增加", ["AllyPowerGain"] = "友方力量增加",
        ["AllySurvivabilityGain"] = "友方生存增加", ["EnergyGain"] = "能量增加",
        ["AllyEnergyGain"] = "友方能量增加", ["DrawCount"] = "抽牌数量",
        ["DiscardCount"] = "弃牌数量", ["ExhaustCount"] = "消耗牌数量",
        ["SelectDiscardCount"] = "从弃牌堆选牌数量", ["SelectDrawCount"] = "从抽牌堆选牌数量",
        ["DrawPilePickCount"] = "从抽牌堆选牌数量", ["HandKeywordCount"] = "赋予手牌关键词数量",
        ["FriendlyDamage"] = "友方受到伤害", ["HealDivisor"] = "伤害转治疗除数",
        ["BaseRebirthHeal"] = "复生基础治疗", ["DoubleStrikeBaseDamage"] = "第二段基础伤害",
        ["EnergyLossOnDraw"] = "抽到时失去能量", ["TeamDamageAtTurnEnd"] = "回合结束全阵伤害",
        ["SurvivabilityLossAmount"] = "削减目标生存", ["WeakenStacksPerExtraDraw"] = "每次追加抽牌所需虚弱层数",
        ["PowerGainPerCast"] = "每次执行增加力量", ["PowerGainPerLoop"] = "每次循环增加力量",
        ["InvisibleSurvivabilityGain"] = "每层隐身转化生存", ["InvisiblePowerGain"] = "隐身角色力量增加",
        ["DiscardDazeCount"] = "加入弃牌堆的晕眩数量", ["selfStacks"] = "自身易伤层数",
        ["SurvivabilityGainPerInvisible"] = "每层隐身转化生存", ["BaseSurvivabilityGain"] = "基础生存增加",
        ["Heal"] = "治疗量", ["HealAmount"] = "治疗量", ["HitDamage"] = "每次攻击基础伤害",
        ["AttackTimes"] = "攻击循环次数", ["ClampMax"] = "数值上限", ["ThrallBlock"] = "战仆格挡",
        ["ThrallBlockMultiplier"] = "战仆格挡生存倍率",
        ["DazeCardsPerTarget"] = "每个目标加入晕眩数量", ["VoidCardsInserted"] = "加入虚空牌数量",
        ["RandomCardCount"] = "随机牌数量", ["PowerDown"] = "力量削减", ["SurvivabilityDown"] = "生存削减",
        ["BladeCount"] = "利刃数量／攻击次数", ["CalmnessCount"] = "镇静牌数量",
        ["DefenceFocusCount"] = "防御专注牌数量", ["VoidCount"] = "虚空牌数量",
        ["DazeCount"] = "晕眩牌数量", ["WoundCount"] = "伤口牌数量", ["PlagueCount"] = "瘟疫牌数量",
    };

    public static string GetLabel(string key) {
        if (Labels.TryGetValue(key, out string label))
            return label;
        foreach (string suffix in new[] { "Stacks", "Count" }) {
            if (key.EndsWith(suffix, StringComparison.Ordinal)
                && Enum.TryParse<Buff.BuffName>(key[..^suffix.Length], out var buff))
                return Buff.GetBuffDisplayName(buff) + (suffix == "Stacks" ? "层数" : "牌数量");
        }
        return key;
    }

    public static int GetMinimum(string key) => key is "EnergyCost" or "EnemyEnergyCost" ? Skill.XEnergyCost
        : key is "HealDivisor" or "WeakenStacksPerExtraDraw" or "BlockPerPower" ? 1
        : key is "ExhaustAfterExecutions" || key.EndsWith("Count") || key.EndsWith("Times") ? 0
        : int.MinValue;

    public static void Validate(string key, int value) {
        if (value < GetMinimum(key))
            throw new InvalidOperationException($"{GetLabel(key)}不能小于 {GetMinimum(key)}。");
    }

    public static int GetInitialValue(string key) => Math.Max(0, GetMinimum(key));
}
