using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ColorlessSkillAttribute : Attribute { }

public partial class Skill
{
    private static readonly SkillID[] ColorlessSkillPool = BuildColorlessSkillPool();

    public virtual bool IsColorless => false;

    public static SkillID[] GetColorlessSkillPool() => ColorlessSkillPool;

    public static bool IsColorlessSkill(SkillID skillId) =>
        ColorlessSkillPool.Contains(skillId);

    private static SkillID[] BuildColorlessSkillPool()
    {
        var pool = new List<SkillID>();
        foreach (SkillID skillId in Enum.GetValues<SkillID>())
        {
            FieldInfo field = typeof(SkillID).GetField(skillId.ToString());
            if (field?.GetCustomAttribute<ColorlessSkillAttribute>() == null)
                continue;

            pool.Add(skillId);
        }

        return pool.Distinct().ToArray();
    }
}
