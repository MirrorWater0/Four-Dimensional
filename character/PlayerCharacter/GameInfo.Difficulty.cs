using System;
using System.Collections.Generic;
using System.Linq;

public enum GameDifficultyBonus
{
    RandomRelics = 1,
    PlayerStats = 2,
    ElectricityCoin = 3,
    FreeRetreat = 4,
    RandomTalentPoints = 5,
}

public static partial class GameInfo
{
    public static int Difficulty;

    public const int MinDifficulty = 0;
    public const int MaxDifficulty = 6;

    private const int StarterRelicCount = 2;
    private const int StarterElectricityCoinBonus = 100;
    private const int StarterPropertyBonus = 1;
    private const int StarterLifeMaxBonus = 5;
    private const int StarterTalentPointCharacterCount = 2;
    private const int StarterTalentPointAmount = 1;

    public static void ApplyDifficultyStartBonuses()
    {
        Difficulty = Math.Clamp(Difficulty, MinDifficulty, MaxDifficulty);
        var rng = new Random(BuildDifficultyBonusSeed());

        if (IsDifficultyBonusActive(GameDifficultyBonus.RandomRelics))
            AddRandomStarterRelics(rng, StarterRelicCount);

        if (IsDifficultyBonusActive(GameDifficultyBonus.ElectricityCoin))
            ElectricityCoin += StarterElectricityCoinBonus;

        if (IsDifficultyBonusActive(GameDifficultyBonus.PlayerStats))
            ApplyStarterPlayerStatBonus();

        if (IsDifficultyBonusActive(GameDifficultyBonus.RandomTalentPoints))
            GrantRandomStarterTalentPoints(
                rng,
                StarterTalentPointCharacterCount,
                StarterTalentPointAmount
            );
    }

    public static bool IsDifficultyBonusActive(GameDifficultyBonus bonus)
    {
        return GetActiveDifficultyBonuses(Difficulty).Contains(bonus);
    }

    public static string BuildDifficultySummaryText(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        string activeBonusText = string.Join(
            " / ",
            GetActiveDifficultyBonuses(difficulty).Select(GetDifficultyBonusLabel)
        );

        if (string.IsNullOrWhiteSpace(activeBonusText))
            activeBonusText = "无开局增益";

        return $"难度 {difficulty}：{activeBonusText}";
    }

    private static IEnumerable<GameDifficultyBonus> GetActiveDifficultyBonuses(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);

        if (difficulty <= 0)
        {
            yield return GameDifficultyBonus.RandomRelics;
            yield return GameDifficultyBonus.PlayerStats;
            yield return GameDifficultyBonus.ElectricityCoin;
            yield return GameDifficultyBonus.FreeRetreat;
            yield return GameDifficultyBonus.RandomTalentPoints;
            yield break;
        }

        if (difficulty <= 5)
            yield return GameDifficultyBonus.RandomTalentPoints;
        if (difficulty <= 3)
            yield return GameDifficultyBonus.RandomRelics;
        if (difficulty <= 2)
            yield return GameDifficultyBonus.PlayerStats;
        if (difficulty <= 1)
            yield return GameDifficultyBonus.ElectricityCoin;
    }

    private static string GetDifficultyBonusLabel(GameDifficultyBonus bonus)
    {
        return bonus switch
        {
            GameDifficultyBonus.RandomRelics => "开局2随机遗物",
            GameDifficultyBonus.FreeRetreat => "\u64a4\u9000\u4e0d\u8017\u6838\u5fc3\u80fd\u6e90",
            GameDifficultyBonus.RandomTalentPoints =>
                "\u5f00\u5c40\u968f\u673a2\u540d\u89d2\u8272\u5404+1\u5929\u8d4b\u70b9",
            GameDifficultyBonus.ElectricityCoin => "开局+100电力币",
            GameDifficultyBonus.PlayerStats => "全员力量/生存/速度+1，血量+5",
            _ => string.Empty,
        };
    }

    private static int BuildDifficultyBonusSeed()
    {
        unchecked
        {
            return (Seed * 397) ^ (Difficulty * 7919) ^ 0x4D1F2B3C;
        }
    }

    private static void AddRandomStarterRelics(Random rng, int count)
    {
        Relics ??= new Dictionary<RelicID, int>();

        for (int i = 0; i < count; i++)
        {
            var pool = Relic.GetUnownedOfferPool();
            if (pool == null || pool.Length == 0)
                return;

            RelicID relicId = PickRandom(pool, rng);
            int amount = Relic.GetAcquireAmount(relicId);
            if (!Relics.TryGetValue(relicId, out int currentAmount))
                Relics[relicId] = amount;
            else
                Relics[relicId] = currentAmount + amount;
        }
    }

    private static void ApplyStarterPlayerStatBonus()
    {
        if (PlayerCharacters == null)
            return;

        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            info.Power += StarterPropertyBonus;
            info.Survivability += StarterPropertyBonus;
            info.Speed += StarterPropertyBonus;
            info.LifeMax += StarterLifeMaxBonus;
            PlayerCharacters[i] = info;
        }
    }

    private static void GrantRandomStarterTalentPoints(Random rng, int characterCount, int amount)
    {
        if (
            PlayerCharacters == null
            || PlayerCharacters.Length == 0
            || characterCount <= 0
            || amount <= 0
        )
        {
            return;
        }

        List<int> candidateIndices = PlayerCharacters
            .Select((player, index) => new { player, index })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.player.CharacterName))
            .Select(entry => entry.index)
            .ToList();

        int grantCount = Math.Min(characterCount, candidateIndices.Count);
        for (int i = 0; i < grantCount; i++)
        {
            int poolIndex = rng.Next(candidateIndices.Count);
            int characterIndex = candidateIndices[poolIndex];
            candidateIndices.RemoveAt(poolIndex);

            var info = PlayerCharacters[characterIndex];
            TalentTree.AddTalentPoints(ref info, amount);
            PlayerCharacters[characterIndex] = info;
        }
    }

    private static T PickRandom<T>(IReadOnlyList<T> pool, Random rng)
    {
        rng ??= new Random();
        return pool[rng.Next(pool.Count)];
    }
}
