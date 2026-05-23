using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public readonly struct TalentNodeDefinition(
    string id,
    string displayName,
    string description,
    string effectDescription,
    int stage,
    int cost,
    Vector2 position,
    params string[] prerequisites
)
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;
    public string EffectDescription { get; } = effectDescription;
    public int Stage { get; } = stage;
    public int Cost { get; } = cost;
    public Vector2 Position { get; } = position;
    public IReadOnlyList<string> Prerequisites { get; } = prerequisites ?? [];
}

public static class TalentTree
{
    public const string CoreNodeSuffix = ".Core";
    public const string SpeedNodeSuffix = ".Attack1";
    public const string PowerBranchNodeSuffix = ".Survive1";
    public const string PassiveUpgradeNodeSuffix = ".Attack2";
    public const string SurvivabilityBranchNodeSuffix = ".Survive2";

    private static readonly Dictionary<string, TalentNodeDefinition[]> CharacterNodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Echo"] = BuildCharacterNodes(
                "Echo",
                "回声原点",
                "调频加速",
                "余响增幅",
                "折返信标",
                "频段守护"
            ),
            ["Kasiya"] = BuildCharacterNodes(
                "Kasiya",
                "战术核心",
                "阵列加速",
                "强袭矩阵",
                "防线展开",
                "护盾校准"
            ),
            ["Mariya"] = BuildCharacterNodes(
                "Mariya",
                "祈愿起点",
                "祈福加速",
                "星光裁断",
                "复苏脉冲",
                "圣辉庇护"
            ),
            ["Nightingale"] = BuildCharacterNodes(
                "Nightingale",
                "暮色枢纽",
                "月影加速",
                "月相锋刃",
                "悖论回响",
                "日冕屏障"
            ),
        };

    public static IReadOnlyList<TalentNodeDefinition> GetNodes(string characterName)
    {
        return !string.IsNullOrWhiteSpace(characterName)
            && CharacterNodes.TryGetValue(characterName, out var nodes)
            ? nodes
            : Array.Empty<TalentNodeDefinition>();
    }

    public static bool HasUnlocked(PlayerInfoStructure info, string talentId)
    {
        return info.UnlockedTalents?.Contains(talentId) == true;
    }

    public static bool CanUnlock(
        PlayerInfoStructure info,
        TalentNodeDefinition node,
        out string reason
    )
    {
        info.UnlockedTalents ??= new List<string>();

        if (HasUnlocked(info, node.Id))
        {
            reason = "已点亮";
            return false;
        }

        if (info.TalentPoints < node.Cost)
        {
            reason = $"需要 {node.Cost} 点天赋点";
            return false;
        }

        foreach (string prerequisite in node.Prerequisites)
        {
            if (!HasUnlocked(info, prerequisite))
            {
                reason = "需要先点亮前置天赋";
                return false;
            }
        }

        reason = "可以点亮";
        return true;
    }

    public static bool TryUnlock(
        ref PlayerInfoStructure info,
        string talentId,
        out string message
    )
    {
        info.UnlockedTalents ??= new List<string>();

        var node = GetNodes(info.CharacterName).FirstOrDefault(talent => talent.Id == talentId);
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            message = "未找到天赋";
            return false;
        }

        if (!CanUnlock(info, node, out message))
            return false;

        info.TalentPoints -= node.Cost;
        info.UnlockedTalents.Add(node.Id);
        message = $"点亮天赋：{node.DisplayName}";
        return true;
    }

    public static void AddTalentPoints(ref PlayerInfoStructure info, int amount)
    {
        info.TalentPoints = Math.Max(0, info.TalentPoints + amount);
    }

    public static int GetEffectivePower(PlayerInfoStructure info) =>
        info.Power + GetPowerBonus(info);

    public static int GetEffectiveSurvivability(PlayerInfoStructure info) =>
        info.Survivability + GetSurvivabilityBonus(info);

    public static int GetEffectiveSpeed(PlayerInfoStructure info) =>
        info.Speed + GetSpeedBonus(info);

    public static bool HasPassiveUpgrade(PlayerInfoStructure info) =>
        HasUnlocked(info, GetPassiveUpgradeTalentId(info.CharacterName));

    public static string GetPassiveUpgradeTalentId(string characterName) =>
        string.IsNullOrWhiteSpace(characterName)
            ? string.Empty
            : $"{characterName}{PassiveUpgradeNodeSuffix}";

    public static string GetPassiveDescription(PlayerInfoStructure info)
    {
        string description = string.IsNullOrWhiteSpace(info.PassiveDescription)
            ? "-"
            : info.PassiveDescription;
        return AppendPassiveUpgradeDescription(info.CharacterName, description, HasPassiveUpgrade(info));
    }

    public static string AppendPassiveUpgradeDescription(
        string characterName,
        string description,
        bool hasPassiveUpgrade
    )
    {
        if (!hasPassiveUpgrade)
            return description;

        string upgradeDescription = GetPassiveUpgradeEffectDescription(characterName);
        return string.IsNullOrWhiteSpace(description)
            ? upgradeDescription
            : $"{description}\n{upgradeDescription}";
    }

    private static int GetPowerBonus(PlayerInfoStructure info)
    {
        int bonus = 0;
        if (HasUnlocked(info, $"{info.CharacterName}{CoreNodeSuffix}"))
            bonus += 1;
        if (HasUnlocked(info, $"{info.CharacterName}{PowerBranchNodeSuffix}"))
            bonus += 2;
        return bonus;
    }

    private static int GetSurvivabilityBonus(PlayerInfoStructure info)
    {
        int bonus = 0;
        if (HasUnlocked(info, $"{info.CharacterName}{CoreNodeSuffix}"))
            bonus += 1;
        if (HasUnlocked(info, $"{info.CharacterName}{SurvivabilityBranchNodeSuffix}"))
            bonus += 2;
        return bonus;
    }

    private static int GetSpeedBonus(PlayerInfoStructure info)
    {
        return HasUnlocked(info, $"{info.CharacterName}{SpeedNodeSuffix}") ? 1 : 0;
    }

    private static TalentNodeDefinition[] BuildCharacterNodes(
        string characterName,
        string coreName,
        string speedName,
        string powerBranchName,
        string passiveUpgradeName,
        string survivabilityBranchName
    )
    {
        string coreId = $"{characterName}{CoreNodeSuffix}";
        string speedId = $"{characterName}{SpeedNodeSuffix}";
        string powerBranchId = $"{characterName}{PowerBranchNodeSuffix}";
        string passiveUpgradeId = $"{characterName}{PassiveUpgradeNodeSuffix}";
        string survivabilityBranchId = $"{characterName}{SurvivabilityBranchNodeSuffix}";

        return
        [
            new TalentNodeDefinition(
                coreId,
                coreName,
                "第一阶段天赋节点，后续天赋需要从这里展开。",
                "+1 力量，+1 生存。",
                0,
                1,
                new Vector2(330f, 304f)
            ),
            new TalentNodeDefinition(
                speedId,
                speedName,
                "第二阶段天赋节点，进一步提升行动节奏。",
                "+1 速度。",
                1,
                1,
                new Vector2(330f, 184f),
                coreId
            ),
            new TalentNodeDefinition(
                powerBranchId,
                powerBranchName,
                "左分支天赋节点，强化输出能力。",
                "+2 力量。",
                2,
                1,
                new Vector2(178f, 64f),
                speedId
            ),
            new TalentNodeDefinition(
                passiveUpgradeId,
                passiveUpgradeName,
                "中间的最终阶段天赋节点，强化角色被动。",
                GetPassiveUpgradeEffectDescription(characterName),
                2,
                1,
                new Vector2(330f, 64f),
                speedId
            ),
            new TalentNodeDefinition(
                survivabilityBranchId,
                survivabilityBranchName,
                "右分支天赋节点，强化生存能力。",
                "+2 生存。",
                2,
                1,
                new Vector2(482f, 64f),
                speedId
            ),
        ];
    }

    public static string GetPassiveUpgradeEffectDescription(string characterName)
    {
        return characterName switch
        {
            "Echo" => "被动强化：第一次回合开始时额外获得1点能量。",
            "Nightingale" => "被动强化：追击前若目标有易伤，则给予1层易伤。",
            "Mariya" => "被动强化：被动治疗量+8。",
            "Kasiya" => "被动强化：其他角色打出特殊技能时获得1点生存。",
            _ => "被动强化。",
        };
    }
}
