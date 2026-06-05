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
    public const int BossDyingCoreEnergyPenaltyDifficulty = 6;

    private const int StarterRelicCount = 1;
    private const int StarterElectricityCoinBonus = 100;
    private const int StarterPropertyBonus = 1;
    private const int StarterLifeMaxBonus = 3;
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

        if (IsBossDyingCoreEnergyPenaltyActive(difficulty))
            activeBonusText += " / Boss战濒死损失15核心能源";

        return $"难度 {difficulty}：{activeBonusText}";
    }

    public static string BuildDifficultyTooltipText(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        List<GameDifficultyBonus> activeBonuses = GetActiveDifficultyBonuses(difficulty).ToList();
        var lines = new List<string> { $"[b]难度 {difficulty}[/b]" };

        if (activeBonuses.Count == 0)
        {
            lines.Add("当前没有开局增益。");
        }
        else
        {
            lines.Add("当前保留的开局增益：");
            lines.AddRange(activeBonuses.Select(bonus => $"- {GetDifficultyBonusLabel(bonus)}"));
        }

        if (IsBossDyingCoreEnergyPenaltyActive(difficulty))
        {
            lines.Add(string.Empty);
            lines.Add("额外规则：");
            lines.Add("- Boss战中角色濒死时损失15点核心能源。");
        }

        if (difficulty < MaxDifficulty)
        {
            List<GameDifficultyBonus> nextBonuses = GetActiveDifficultyBonuses(difficulty + 1).ToList();
            List<GameDifficultyBonus> lostBonuses = activeBonuses
                .Where(bonus => !nextBonuses.Contains(bonus))
                .ToList();
            bool gainsBossDyingPenalty =
                !IsBossDyingCoreEnergyPenaltyActive(difficulty)
                && IsBossDyingCoreEnergyPenaltyActive(difficulty + 1);

            if (lostBonuses.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add(
                    $"提高到难度 {difficulty + 1} 后会失去："
                );
                lines.AddRange(lostBonuses.Select(bonus => $"- {GetDifficultyBonusLabel(bonus)}"));
            }

            if (gainsBossDyingPenalty)
            {
                lines.Add(string.Empty);
                lines.Add($"提高到难度 {difficulty + 1} 后会新增：");
                lines.Add("- Boss战中角色濒死时损失15点核心能源。");
            }
        }

        return string.Join("\n", lines);
    }

    public static bool IsBossDyingCoreEnergyPenaltyActive() =>
        IsBossDyingCoreEnergyPenaltyActive(Difficulty);

    public static bool IsBossDyingCoreEnergyPenaltyActive(int difficulty) =>
        difficulty >= BossDyingCoreEnergyPenaltyDifficulty;

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
            GameDifficultyBonus.RandomRelics => "开局1随机遗物",
            GameDifficultyBonus.FreeRetreat =>
                "撤退不消耗核心能源",
            GameDifficultyBonus.RandomTalentPoints =>
                "开局随机2名角色各+1天赋点",
            GameDifficultyBonus.ElectricityCoin => "开局+100电力币",
            GameDifficultyBonus.PlayerStats =>
                "全员力量/生存/速度+1，血量+3",
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
        Relics ??= new List<RelicStack>();

        for (int i = 0; i < count; i++)
        {
            var pool = Relic.GetUnownedOfferPool();
            if (pool == null || pool.Length == 0)
                return;

            RelicID relicId = PickRandom(pool, rng);
            int amount = Relic.GetAcquireAmount(relicId);
            AddRelicCount(relicId, amount);
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
