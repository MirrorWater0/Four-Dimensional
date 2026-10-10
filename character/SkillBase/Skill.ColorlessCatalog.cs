using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ColorlessSkillAttribute : Attribute { }

public partial class Skill
{
    private static readonly Lazy<SkillID[]> ColorlessSkillPool = new(BuildColorlessSkillPool);

    public virtual bool IsColorless => false;

    public static SkillID[] GetColorlessSkillPool() => ColorlessSkillPool.Value;

    public static bool IsColorlessSkill(SkillID skillId) =>
        ColorlessSkillPool.Value.Contains(skillId);

    private static SkillID[] BuildColorlessSkillPool()
    {
        var pool = new List<SkillID>();
        foreach (SkillID skillId in Enum.GetValues<SkillID>())
        {
            FieldInfo field = typeof(SkillID).GetField(skillId.ToString());
            if (field?.GetCustomAttribute<ColorlessSkillAttribute>() == null || !IsSkillRegistered(skillId))
                continue;

            pool.Add(skillId);
        }

        return pool.Distinct().ToArray();
    }
}
