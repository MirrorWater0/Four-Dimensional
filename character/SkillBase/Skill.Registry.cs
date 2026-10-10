using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

/// <summary>
/// 将技能实现类绑定到 <see cref="SkillID"/>。类名默认与枚举同名；
/// 不匹配时在类上标注 <see cref="SkillDefinitionAttribute"/>，或写入 <see cref="TypeNameOverrides"/>。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SkillDefinitionAttribute : Attribute
{
    public SkillID Id { get; }

    public SkillDefinitionAttribute(SkillID id)
    {
        Id = id;
    }
}

public partial class Skill
{
    private static readonly IReadOnlyDictionary<SkillID, SkillID> SkillIdAliases =
        new Dictionary<SkillID, SkillID> { [SkillID.DeSurviveSkill] = SkillID.ShockWave };

    private static readonly IReadOnlyDictionary<string, SkillID> TypeNameOverrides =
        new Dictionary<string, SkillID>(StringComparer.Ordinal)
        {
            ["Shelter"] = SkillID.ResonanceShelter,
            ["ArcTrack"] = SkillID.ConcordSlash,
            ["EternalDarkSkill"] = SkillID.EternalDark,
        };

    // Keep old numeric IDs for saves, while removing player attack cards from every factory/pool.
    private static readonly HashSet<SkillID> UnregisteredAttackCards = new();

    private static readonly Dictionary<SkillID, Func<Skill>> SkillFactories =
        BuildSkillFactories();

    static Skill()
    {
        // Build all factories before inspecting card types: descriptions can create related cards.
        foreach (var pair in SkillFactories)
        {
            FieldInfo idField = typeof(SkillID).GetField(pair.Key.ToString());
            bool isPlayerCard = idField?.GetCustomAttribute<PlayerSkillAttribute>() != null
                || idField?.GetCustomAttribute<ColorlessSkillAttribute>() != null;
            if (isPlayerCard && pair.Value().SkillType == SkillTypes.Attack)
                UnregisteredAttackCards.Add(pair.Key);
        }
        foreach (SkillID id in UnregisteredAttackCards)
            SkillFactories.Remove(id);
    }

    public static bool IsSkillRegistered(SkillID skillId) =>
        SkillFactories.ContainsKey(SkillIdAliases.TryGetValue(skillId, out var alias) ? alias : skillId);

    public static Skill GetSkill(SkillID skillID)
    {
        if (!TryCreateSkill(skillID, out Skill skill))
            return null;

        return skill;
    }

    private static bool TryCreateSkill(SkillID skillID, out Skill skill)
    {
        SkillID resolvedId = SkillIdAliases.TryGetValue(skillID, out SkillID alias)
            ? alias
            : skillID;
        if (!SkillFactories.TryGetValue(resolvedId, out Func<Skill> factory))
        {
            skill = null;
            return false;
        }

        skill = factory();
        skill.SkillId = skillID;
        return true;
    }

    private static Dictionary<SkillID, Func<Skill>> BuildSkillFactories()
    {
        var factories = new Dictionary<SkillID, Func<Skill>>();
        foreach (Type type in typeof(Skill).Assembly.GetTypes())
        {
            if (
                type.IsAbstract
                || type.IsNested
                || !typeof(Skill).IsAssignableFrom(type)
                || type == typeof(Skill)
            )
            {
                continue;
            }

            if (!TryResolveSkillId(type, out SkillID skillId))
            {
#if DEBUG
                GD.PushWarning($"Skill registry skipped unmapped type: {type.FullName}");
#endif
                continue;
            }

            if (factories.ContainsKey(skillId))
            {
                GD.PushError(
                    $"Skill registry duplicate mapping for {skillId}: {factories[skillId].Method?.DeclaringType?.Name} vs {type.Name}"
                );
                continue;
            }

            Type implementationType = type;
            factories[skillId] = () => (Skill)Activator.CreateInstance(implementationType);
        }

#if DEBUG
        ValidateSkillRegistry(factories);
#endif
        return factories;
    }

    private static bool TryResolveSkillId(Type skillType, out SkillID skillId)
    {
        var definition = skillType.GetCustomAttribute<SkillDefinitionAttribute>();
        if (definition != null)
        {
            skillId = definition.Id;
            return true;
        }

        if (TypeNameOverrides.TryGetValue(skillType.Name, out skillId))
            return true;

        if (Enum.TryParse(skillType.Name, out skillId))
            return true;

        if (
            skillType.Name.EndsWith("Skill", StringComparison.Ordinal)
            && Enum.TryParse(skillType.Name[..^"Skill".Length], out skillId)
        )
            return true;

        skillId = default;
        return false;
    }

#if DEBUG
    private static void ValidateSkillRegistry(Dictionary<SkillID, Func<Skill>> factories)
    {
        foreach (SkillID skillId in Enum.GetValues<SkillID>())
        {
            if (skillId == SkillID.None)
                continue;

            SkillID resolvedId = SkillIdAliases.TryGetValue(skillId, out SkillID alias)
                ? alias
                : skillId;
            if (!factories.ContainsKey(resolvedId) && !UnregisteredAttackCards.Contains(resolvedId))
                GD.PushWarning($"Skill registry missing implementation for {skillId}");
        }
    }
#endif
}
