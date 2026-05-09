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
        NormalizeTakenSkillsForPool(ref info);
        EnsureTakenSkillsAreTracked(ref info);
    }

    private static void MigrateBreakStrikeToNightingale()
    {
        if (PlayerCharacters == null || PlayerCharacters.Length == 0)
            return;

        int nightingaleIndex = Array.FindIndex(
            PlayerCharacters,
            info => string.Equals(info.CharacterName, "Nightingale", StringComparison.OrdinalIgnoreCase)
        );
        if (nightingaleIndex < 0)
            return;

        bool transferUnlock = false;

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            if (i == nightingaleIndex)
                continue;

            info.GainedSkills ??= new List<SkillID>();
            if (info.GainedSkills.Remove(SkillID.BreakStrike))
                transferUnlock = true;

            if (info.TakenSkills != null)
            {
                for (int slot = 0; slot < info.TakenSkills.Length; slot++)
                {
                    if (info.TakenSkills[slot] != SkillID.BreakStrike)
                        continue;

                    info.TakenSkills[slot] = default;
                    transferUnlock = true;
                }
            }

            PlayerCharacters[i] = info;
        }

        if (!transferUnlock)
            return;

        var nightingale = PlayerCharacters[nightingaleIndex];
        nightingale.GainedSkills ??= new List<SkillID>();
        if (!nightingale.GainedSkills.Contains(SkillID.BreakStrike))
            nightingale.GainedSkills.Add(SkillID.BreakStrike);
        PlayerCharacters[nightingaleIndex] = nightingale;
    }

    private static void NormalizeTakenSkillsForPool(ref PlayerInfoStructure info)
    {
        if (info.TakenSkills == null || info.TakenSkills.Length == 0)
            return;

        var pool = info.AllSkills ?? Array.Empty<SkillID>();
        if (pool.Length == 0)
            return;

        var poolSet = pool.ToHashSet();
        poolSet.Add(SkillID.BasicAttack);
        poolSet.Add(SkillID.BasicDefense);
        poolSet.Add(SkillID.BasicSpecial);
        var validGained = (info.GainedSkills ?? new List<SkillID>())
            .Where(poolSet.Contains)
            .Distinct()
            .ToList();

        SkillID?[] normalized = new SkillID?[info.TakenSkills.Length];
        HashSet<SkillID> equipped = new();

        for (int i = 0; i < info.TakenSkills.Length; i++)
        {
            SkillID current = info.TakenSkills[i];
            if (!poolSet.Contains(current) || !equipped.Add(current))
                continue;

            normalized[i] = current;
        }

        for (int i = 0; i < normalized.Length; i++)
        {
            if (normalized[i].HasValue)
                continue;

            normalized[i] = PickReplacementSkill(pool, validGained, equipped);
            equipped.Add(normalized[i]!.Value);
        }

        info.TakenSkills = normalized.Select(skill => skill ?? pool[0]).ToArray();
    }

    private static SkillID PickReplacementSkill(
        SkillID[] pool,
        List<SkillID> validGained,
        HashSet<SkillID> equipped
    )
    {
        foreach (var skillId in validGained)
        {
            if (!equipped.Contains(skillId))
                return skillId;
        }

        foreach (var skillId in pool)
        {
            if (!equipped.Contains(skillId))
                return skillId;
        }

        return pool[0];
    }

    private static void EnsureTakenSkillsAreTracked(ref PlayerInfoStructure info)
    {
        info.GainedSkills ??= new List<SkillID>();
        foreach (var skillId in info.TakenSkills ?? Array.Empty<SkillID>())
        {
            if (!info.GainedSkills.Contains(skillId))
                info.GainedSkills.Add(skillId);
        }

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
