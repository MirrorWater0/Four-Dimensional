using System;
using System.Collections.Generic;
using System.Linq;

public static partial class GameInfo
{
    public static void NormalizePlayerCharacters()
    {
        if (PlayerCharacters == null)
        {
            return;
        }

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            NormalizePlayerInfo(ref info, i + 1);
            PlayerCharacters[i] = info;
        }
    }

    public static void SeedTakenSkillsAsGained()
    {
        if (PlayerCharacters == null)
        {
            return;
        }

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            info.GainedSkills ??= new List<SkillID>();
            foreach (var skillId in info.TakenSkills ?? Array.Empty<SkillID>())
            {
                if (!info.GainedSkills.Contains(skillId))
                {
                    info.GainedSkills.Add(skillId);
                }
            }

            info.GainedSkills = info.GainedSkills.Distinct().ToList();
            PlayerCharacters[i] = info;
        }
    }

    private static void NormalizePlayerInfo(ref PlayerInfoStructure info, int defaultPositionIndex)
    {
        info.GainedSkills ??= new List<SkillID>();
        info.TakenSkills = NormalizeArray(info.TakenSkills, 3);
        info.Equipments = NormalizeArray(info.Equipments, 2);
        if (info.PositionIndex <= 0)
        {
            info.PositionIndex = defaultPositionIndex;
        }

        info.AllSkills = Skill.GetPlayerSkillPool(info.CharacterName);
    }

    private static T[] NormalizeArray<T>(T[] source, int length)
    {
        if (source != null && source.Length == length)
        {
            return source;
        }

        var normalized = new T[length];
        if (source != null)
        {
            Array.Copy(source, normalized, Math.Min(source.Length, normalized.Length));
        }

        return normalized;
    }
}
