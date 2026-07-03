using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum GameDifficultyBonus
{
    RandomRelics = 1,
    PlayerStats = 2,
    ElectricityCoin = 3,
    RandomTalentPoints = 5,
}

public enum GameDifficultyPenalty
{
    BattleStartDaze = 1,
    ReducedRegionLifeRecovery = 2,
}

public static partial class GameInfo
{
    public static int Difficulty;

    public const int MinDifficulty = 0;
    public const int MaxDifficulty = 5;

    private const int StarterRelicCount = 1;
    private const int StarterElectricityCoinBonus = 100;
    private const int StarterPropertyBonus = 1;
    private const int StarterLifeMaxBonus = 3;
    private const int StarterTalentPointCharacterCount = 2;
    private const int StarterTalentPointAmount = 1;
    private const int BattleStartDazeCount = 2;
    private const float ReducedRegionLifeRecoveryPercent = 0.8f;

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

    public static void ApplyDifficultyRunStartPenalties()
    {
        if (!IsDifficultyPenaltyActive(GameDifficultyPenalty.ReducedRegionLifeRecovery))
            return;

        SetPartyLifeToMaxLifePercent(ReducedRegionLifeRecoveryPercent);
    }

    public static int ApplyDifficultyRegionEntryLife(int targetLevel)
    {
        if (!IsDifficultyPenaltyActive(GameDifficultyPenalty.ReducedRegionLifeRecovery))
            return RefillPartyLife();

        return targetLevel <= 0
            ? SetPartyLifeToMaxLifePercent(ReducedRegionLifeRecoveryPercent)
            : HealPartyByMaxLifePercent(ReducedRegionLifeRecoveryPercent);
    }

    public static void ApplyDifficultyBattleStartPenalty(Battle battle)
    {
        if (
            !IsDifficultyPenaltyActive(GameDifficultyPenalty.BattleStartDaze)
            || battle == null
            || BattleStartDazeCount <= 0
        )
        {
            return;
        }

        battle.EnsureDifficultyBattleStartDazeCards(BattleStartDazeCount);
    }

    public static bool IsDifficultyBonusActive(GameDifficultyBonus bonus)
    {
        return GetActiveDifficultyBonuses(Difficulty).Contains(bonus);
    }

    public static bool IsDifficultyPenaltyActive(GameDifficultyPenalty penalty)
    {
        return GetActiveDifficultyPenalties(Difficulty).Contains(penalty);
    }

    public static string BuildDifficultySummaryText(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        var summaryParts = new List<string>();
        string activeBonusText = string.Join(
            " / ",
            GetActiveDifficultyBonuses(difficulty).Select(GetDifficultyBonusLabel)
        );
        if (!string.IsNullOrWhiteSpace(activeBonusText))
            summaryParts.Add(activeBonusText);

        string activePenaltyText = string.Join(
            " / ",
            GetActiveDifficultyPenalties(difficulty).Select(GetDifficultyPenaltyLabel)
        );
        if (!string.IsNullOrWhiteSpace(activePenaltyText))
            summaryParts.Add(activePenaltyText);

        string summaryBody = summaryParts.Count > 0
            ? string.Join(" / ", summaryParts)
            : "无开局增益";

        return $"难度 {difficulty}：{summaryBody}";
    }

    public static string BuildDifficultyTooltipText(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);
        List<GameDifficultyBonus> activeBonuses = GetActiveDifficultyBonuses(difficulty).ToList();
        List<GameDifficultyPenalty> activePenalties = GetActiveDifficultyPenalties(difficulty).ToList();
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

        if (activePenalties.Count == 0)
        {
            lines.Add("当前没有难度惩罚。");
        }
        else
        {
            lines.Add("当前难度惩罚：");
            lines.AddRange(activePenalties.Select(penalty => $"- {GetDifficultyPenaltyLabel(penalty)}"));
        }

        if (difficulty < MaxDifficulty)
        {
            List<GameDifficultyBonus> nextBonuses = GetActiveDifficultyBonuses(difficulty + 1).ToList();
            List<GameDifficultyBonus> lostBonuses = activeBonuses
                .Where(bonus => !nextBonuses.Contains(bonus))
                .ToList();
            List<GameDifficultyPenalty> nextPenalties = GetActiveDifficultyPenalties(difficulty + 1).ToList();
            List<GameDifficultyPenalty> gainedPenalties = nextPenalties
                .Where(penalty => !activePenalties.Contains(penalty))
                .ToList();

            if (lostBonuses.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"提高到难度 {difficulty + 1} 后会失去：");
                lines.AddRange(lostBonuses.Select(bonus => $"- {GetDifficultyBonusLabel(bonus)}"));
            }

            if (gainedPenalties.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add($"提高到难度 {difficulty + 1} 后会新增：");
                lines.AddRange(
                    gainedPenalties.Select(penalty => $"- {GetDifficultyPenaltyLabel(penalty)}")
                );
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
            yield return GameDifficultyBonus.RandomTalentPoints;
            yield break;
        }

        if (difficulty <= 3)
            yield return GameDifficultyBonus.RandomTalentPoints;
        if (difficulty <= 2)
            yield return GameDifficultyBonus.RandomRelics;
        if (difficulty <= 1)
            yield return GameDifficultyBonus.PlayerStats;
        if (difficulty <= 0)
            yield return GameDifficultyBonus.ElectricityCoin;
    }

    private static IEnumerable<GameDifficultyPenalty> GetActiveDifficultyPenalties(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);

        if (difficulty >= 4)
            yield return GameDifficultyPenalty.BattleStartDaze;
        if (difficulty >= 5)
            yield return GameDifficultyPenalty.ReducedRegionLifeRecovery;
    }

    private static string GetDifficultyBonusLabel(GameDifficultyBonus bonus)
    {
        return bonus switch
        {
            GameDifficultyBonus.RandomRelics => "开局1随机遗物",
            GameDifficultyBonus.RandomTalentPoints =>
                "开局随机2名角色各+1天赋点",
            GameDifficultyBonus.ElectricityCoin => "开局+100电力币",
            GameDifficultyBonus.PlayerStats =>
                "全员力量/生存+1，血量+3",
            _ => string.Empty,
        };
    }

    private static string GetDifficultyPenaltyLabel(GameDifficultyPenalty penalty)
    {
        string dazeName =
            Skill.GetSkill(SkillID.DazeStatus)?.SkillName
            ?? I18n.Tr("skill.daze_status.name", "晕眩");

        return penalty switch
        {
            GameDifficultyPenalty.BattleStartDaze =>
                $"战斗开始时抽牌堆加入{BattleStartDazeCount}张{dazeName}",
            GameDifficultyPenalty.ReducedRegionLifeRecovery =>
                "进入区域一时以80%生命开局；进入区域二时保留剩余生命并额外恢复80%生命",
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
            RelicID? relicId = GameInfo.DrawStarterRelicFromQueue();
            if (!relicId.HasValue)
                return;

            int amount = Relic.GetAcquireAmount(relicId.Value);
            AddRelicCount(relicId.Value, amount);
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
            info.LifeMax += StarterLifeMaxBonus;
            if (info.LifeInitialized)
                info.Life += StarterLifeMaxBonus;
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
