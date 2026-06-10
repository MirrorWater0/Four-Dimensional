using System;
using System.Collections.Generic;
using System.Linq;

public static partial class GameInfo
{
    public const int DefaultPlayerPartySize = 3;

    private const int MaxFormationSlots = 9;
    private static readonly int[] DefaultPlayerFormationSlots = [1, 2, 3];

    private static readonly SkillID[] StarterBattleDeckBase =
    [
        SkillID.BasicGuard,
        SkillID.BasicDefense,
        SkillID.BasicAttack,
        SkillID.BasicAttack,
    ];

    private static readonly HashSet<SkillID> BasicSkillIds =
    [
        SkillID.BasicAttack,
        SkillID.BasicDefense,
        SkillID.BasicGuard,
        SkillID.BasicSpecial,
        SkillID.KasiyaBasicSpecial,
        SkillID.EchoBasicSpecial,
        SkillID.MariyaBasicSpecial,
        SkillID.NightingaleBasicSpecial,
    ];

    public static void NormalizePlayerCharacters()
    {
        if (PlayerCharacters == null)
        {
            return;
        }

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            NormalizePlayerInfo(ref info, GetDefaultPlayerFormationPosition(i));
            PlayerCharacters[i] = info;
        }

        NormalizeDefaultPlayerFormation();

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            NormalizePlayerInfo(ref info, GetDefaultPlayerFormationPosition(i));
            PlayerCharacters[i] = info;
        }
    }

    public static int GetDefaultPlayerFormationPosition(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < DefaultPlayerFormationSlots.Length)
            return DefaultPlayerFormationSlots[playerIndex];

        return playerIndex + 1;
    }

    private static void NormalizeDefaultPlayerFormation()
    {
        if (
            PlayerCharacters == null
            || PlayerCharacters.Length == 0
            || PlayerCharacters.Length > DefaultPlayerFormationSlots.Length
        )
        {
            return;
        }

        HashSet<int> occupied = new();

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            int positionIndex = info.PositionIndex;

            if (!CanUseFormationPosition(positionIndex, occupied))
            {
                positionIndex = PickDefaultFormationPosition(occupied, i);
                info.PositionIndex = positionIndex;
                PlayerCharacters[i] = info;
            }

            occupied.Add(positionIndex);
        }
    }

    private static int PickDefaultFormationPosition(
        HashSet<int> occupied,
        int playerIndex
    )
    {
        int preferredPosition = GetDefaultPlayerFormationPosition(playerIndex);
        if (CanUseFormationPosition(preferredPosition, occupied))
            return preferredPosition;

        foreach (int positionIndex in DefaultPlayerFormationSlots)
        {
            if (CanUseFormationPosition(positionIndex, occupied))
                return positionIndex;
        }

        for (int positionIndex = 1; positionIndex <= MaxFormationSlots; positionIndex++)
        {
            if (CanUseFormationPosition(positionIndex, occupied))
                return positionIndex;
        }

        for (int positionIndex = 1; positionIndex <= MaxFormationSlots; positionIndex++)
        {
            if (!occupied.Contains(positionIndex))
                return positionIndex;
        }

        return preferredPosition;
    }

    private static bool CanUseFormationPosition(
        int positionIndex,
        HashSet<int> occupied
    )
    {
        if (positionIndex <= 0 || positionIndex > MaxFormationSlots)
            return false;
        return !occupied.Contains(positionIndex);
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
            if (IsStarterBasicOnlyDeck(info.GainedSkills))
            {
                info.GainedSkills = new List<SkillID>(GetStarterBattleDeck(info));
            }
            else
            {
                foreach (var skillId in info.TakenSkills ?? Array.Empty<SkillID>())
                {
                    if (!info.GainedSkills.Contains(skillId))
                    {
                        info.GainedSkills.Add(skillId);
                    }
                }
            }

            PlayerCharacters[i] = info;
        }
    }

    private static void NormalizePlayerInfo(ref PlayerInfoStructure info, int defaultPositionIndex)
    {
        info.GainedSkills ??= new List<SkillID>();
        info.UnlockedTalents ??= new List<string>();
        info.TalentPoints = Math.Max(0, info.TalentPoints);
        info.TakenSkills = NormalizeArray(info.TakenSkills, 3);
        info.LifeMax = Math.Max(1, info.LifeMax);
        if (!info.LifeInitialized)
        {
            info.Life = info.LifeMax;
            info.LifeInitialized = true;
        }
        else
        {
            info.Life = Math.Clamp(info.Life, 0, info.LifeMax);
        }
        if (info.PositionIndex <= 0)
        {
            info.PositionIndex = defaultPositionIndex;
        }

        info.AllSkills = Skill.GetPlayerSkillPool(info.CharacterName, info.CharacterScenePath);
        NormalizeTakenSkillsForPool(ref info);
        NormalizeStarterBattleDeckIfNeeded(ref info);
        NormalizeStarterTakenSpecialIfNeeded(ref info);
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
        poolSet.Add(SkillID.BasicGuard);
        poolSet.Add(SkillID.BasicSpecial);
        poolSet.Add(SkillID.KasiyaBasicSpecial);
        poolSet.Add(SkillID.EchoBasicSpecial);
        poolSet.Add(SkillID.MariyaBasicSpecial);
        poolSet.Add(SkillID.NightingaleBasicSpecial);
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

    private static bool IsStarterBasicOnlyDeck(List<SkillID> gainedSkills)
    {
        return gainedSkills == null
            || gainedSkills.Count == 0
            || gainedSkills.All(BasicSkillIds.Contains);
    }

    private static SkillID[] GetStarterBattleDeck(PlayerInfoStructure info)
    {
        return StarterBattleDeckBase.Append(GetStarterSpecialSkill(info)).ToArray();
    }

    private static SkillID GetStarterSpecialSkill(PlayerInfoStructure info)
    {
        return Skill.TryResolvePlayerCharacterKey(
            info.CharacterName,
            info.CharacterScenePath,
            out PlayerCharacterKey key
        )
            ? key switch
        {
            PlayerCharacterKey.Echo => SkillID.EchoBasicSpecial,
            PlayerCharacterKey.Kasiya => SkillID.KasiyaBasicSpecial,
            PlayerCharacterKey.Mariya => SkillID.MariyaBasicSpecial,
            PlayerCharacterKey.Nightingale => SkillID.NightingaleBasicSpecial,
            _ => SkillID.BasicSpecial,
        }
            : SkillID.BasicSpecial;
    }

    private static void NormalizeStarterBattleDeckIfNeeded(ref PlayerInfoStructure info)
    {
        info.GainedSkills ??= new List<SkillID>();
        if (IsStarterBasicOnlyDeck(info.GainedSkills))
            info.GainedSkills = new List<SkillID>(GetStarterBattleDeck(info));
    }

    private static void NormalizeStarterTakenSpecialIfNeeded(ref PlayerInfoStructure info)
    {
        if (info.TakenSkills == null || info.TakenSkills.Length == 0)
            return;

        SkillID starterSpecial = GetStarterSpecialSkill(info);
        if (starterSpecial == SkillID.BasicSpecial)
            return;

        for (int i = 0; i < info.TakenSkills.Length; i++)
        {
            if (info.TakenSkills[i] != SkillID.BasicSpecial)
                continue;

            info.TakenSkills[i] = starterSpecial;
            return;
        }
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
