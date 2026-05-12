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
    private static readonly Dictionary<string, TalentNodeDefinition[]> CharacterNodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Echo"] = BuildCharacterNodes("Echo", "回声原点", "余响增幅", "频段守护", "共鸣延展", "折返信标"),
            ["Kasiya"] = BuildCharacterNodes("Kasiya", "战术核心", "强袭矩阵", "护盾校准", "火力递进", "防线展开"),
            ["Mariya"] = BuildCharacterNodes("Mariya", "祈愿起点", "星光裁断", "圣辉庇护", "追光连击", "复苏脉冲"),
            ["Nightingale"] = BuildCharacterNodes("Nightingale", "暮色枢纽", "月相锋刃", "日冕屏障", "轮回加速", "悖论回响"),
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

    private static TalentNodeDefinition[] BuildCharacterNodes(
        string characterName,
        string coreName,
        string attackName,
        string surviveName,
        string attackAdvancedName,
        string surviveAdvancedName
    )
    {
        string coreId = $"{characterName}.Core";
        string attackId = $"{characterName}.Attack1";
        string surviveId = $"{characterName}.Survive1";
        string attackAdvancedId = $"{characterName}.Attack2";
        string surviveAdvancedId = $"{characterName}.Survive2";

        return
        [
            new TalentNodeDefinition(
                coreId,
                coreName,
                "第一阶段天赋节点，后续天赋需要从这里展开。",
                "解锁该角色天赋树主干，开放后续分支节点。",
                0,
                1,
                new Vector2(330f, 304f)
            ),
            new TalentNodeDefinition(
                attackId,
                attackName,
                "第二阶段攻击分支预留节点。",
                "攻击分支预留效果，后续接入该角色攻击技能强化。",
                1,
                1,
                new Vector2(330f, 184f),
                coreId
            ),
            new TalentNodeDefinition(
                surviveId,
                surviveName,
                "第二阶段生存分支预留节点。",
                "生存分支预留效果，后续接入该角色防御或回复强化。",
                2,
                1,
                new Vector2(178f, 64f),
                attackId
            ),
            new TalentNodeDefinition(
                attackAdvancedId,
                attackAdvancedName,
                "第三阶段攻击分支预留节点。",
                "高级攻击分支预留效果，后续接入更强的输出强化。",
                2,
                1,
                new Vector2(330f, 64f),
                attackId
            ),
            new TalentNodeDefinition(
                surviveAdvancedId,
                surviveAdvancedName,
                "第三阶段生存分支预留节点。",
                "高级生存分支预留效果，后续接入更强的生存强化。",
                2,
                1,
                new Vector2(482f, 64f),
                attackId
            ),
        ];
    }
}
