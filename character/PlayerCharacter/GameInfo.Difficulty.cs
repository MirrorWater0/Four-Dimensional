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
    public const int MaxDifficulty = 5;

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
            activeBonusText = "\u65e0\u5f00\u5c40\u589e\u76ca";

        return $"\u96be\u5ea6 {difficulty}\uff1a{activeBonusText}";
    }

    public static string BuildDifficultyTooltipText(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        List<GameDifficultyBonus> activeBonuses = GetActiveDifficultyBonuses(difficulty).ToList();
        var lines = new List<string> { $"[b]\u96be\u5ea6 {difficulty}[/b]" };

        if (activeBonuses.Count == 0)
        {
            lines.Add("\u5f53\u524d\u6ca1\u6709\u5f00\u5c40\u589e\u76ca\u3002");
        }
        else
        {
            lines.Add("\u5f53\u524d\u4fdd\u7559\u7684\u5f00\u5c40\u589e\u76ca\uff1a");
            lines.AddRange(activeBonuses.Select(bonus => $"- {GetDifficultyBonusLabel(bonus)}"));
        }

        if (difficulty < MaxDifficulty)
        {
            List<GameDifficultyBonus> nextBonuses = GetActiveDifficultyBonuses(difficulty + 1).ToList();
            List<GameDifficultyBonus> lostBonuses = activeBonuses
                .Where(bonus => !nextBonuses.Contains(bonus))
                .ToList();

            if (lostBonuses.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add(
                    $"\u63d0\u9ad8\u5230\u96be\u5ea6 {difficulty + 1} \u540e\u4f1a\u5931\u53bb\uff1a"
                );
                lines.AddRange(lostBonuses.Select(bonus => $"- {GetDifficultyBonusLabel(bonus)}"));
            }
        }

        return string.Join("\n", lines);
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

        if (difficulty <= 4)
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
            GameDifficultyBonus.RandomRelics => "\u5f00\u5c402\u968f\u673a\u9057\u7269",
            GameDifficultyBonus.FreeRetreat =>
                "\u64a4\u9000\u4e0d\u6d88\u8017\u6838\u5fc3\u80fd\u6e90",
            GameDifficultyBonus.RandomTalentPoints =>
                "\u5f00\u5c40\u968f\u673a2\u540d\u89d2\u8272\u5404+1\u5929\u8d4b\u70b9",
            GameDifficultyBonus.ElectricityCoin => "\u5f00\u5c40+100\u7535\u529b\u5e01",
            GameDifficultyBonus.PlayerStats =>
                "\u5168\u5458\u529b\u91cf/\u751f\u5b58/\u901f\u5ea6+1\uff0c\u8840\u91cf+5",
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
