using System;
using System.Collections.Generic;

public static class EnemyTuningParameters {
    private static readonly Dictionary<string, string> Labels = new() {
        ["MaxLife"] = "基础生命上限",
        ["Power"] = "初始力量",
        ["Survivability"] = "初始生存",
        ["BasePowerContribution"] = "基础力量加成",
        ["BaseSurvivabilityContribution"] = "基础生存加成",
        ["StartDebuffImmunityStacks"] = "开场负面免疫层数",
        ["TurnEndBaseBlock"] = "回合结束全阵格挡",
        ["PassivePowerGain"] = "被动获得力量",
        ["StartStunStacks"] = "开场眩晕层数",
        ["RebirthStacks"] = "被动重生层数",
        ["TurnEndSummonCount"] = "回合结束召唤数量",
        ["StartDamageImmuneStacks"] = "开场伤害免疫层数",
        ["TurnEndDamageImmuneStacks"] = "回合结束伤害免疫层数",
        ["PassiveHeal"] = "被动恢复生命",
        ["DisasterStacks"] = "被动灾厄层数",
        ["StartFearStacks"] = "开场恐惧层数",
        ["PassiveTriggerInterval"] = "被动触发间隔（回合）",
        ["PassiveDisasterStacks"] = "被动灾厄层数",
        ["HurtPowerDown"] = "受击时降低攻击者力量",
        ["StartBarricadeStacks"] = "开场壁垒层数",
        ["StartBlock"] = "开场格挡",
        ["PassiveMaxLifeDown"] = "被动降低生命上限",
        ["StartAutoArmorStacks"] = "开场自动护甲层数",
        ["StartThornStacks"] = "开场荆棘层数",
        ["ActionTriggerCount"] = "被动触发间隔（行动次数）",
        ["TriggerThornStacks"] = "被动获得荆棘层数",
        ["StartRebirthStacks"] = "开场重生层数",
    };

    public static string GetLabel(string key) => Labels.GetValueOrDefault(key, key);
    public static int GetSortOrder(string key) => key switch {
        "MaxLife" => 0,
        "Power" => 1,
        "Survivability" => 2,
        "BasePowerContribution" => 3,
        "BaseSurvivabilityContribution" => 4,
        _ => 5,
    };
    public static int GetMinimum(string key) => key switch {
        "MaxLife" or "PassiveTriggerInterval" or "ActionTriggerCount" => 1,
        "Power" or "Survivability" or "BasePowerContribution" or "BaseSurvivabilityContribution" => int.MinValue,
        _ => 0,
    };

    public static void Validate(string key, int value) {
        if (value < GetMinimum(key))
            throw new InvalidOperationException($"{GetLabel(key)}不能小于 {GetMinimum(key)}。");
    }

    public static int GetInitialValue(string key) => Math.Max(0, GetMinimum(key));
}
