using System;
using System.Collections.Generic;
using System.Linq;

public enum GameDifficultyBonus
{
    RandomRelics = 1,
    RandomEquipment = 2,
    PlayerStats = 3,
    ElectricityCoin = 4,
    FreeRetreat = 5,
}

public static partial class GameInfo
{
    public static int Difficulty;

    public const int MinDifficulty = 0;
    public const int MaxDifficulty = 5;

    private const int StarterRelicCount = 2;
    private const int StarterEquipmentCount = 1;
    private const int StarterElectricityCoinBonus = 100;
    private const int StarterPropertyBonus = 1;
    private const int StarterLifeMaxBonus = 5;

    public static void ApplyDifficultyStartBonuses()
    {
        Difficulty = Math.Clamp(Difficulty, MinDifficulty, MaxDifficulty);
        var rng = new Random(BuildDifficultyBonusSeed());

        if (IsDifficultyBonusActive(GameDifficultyBonus.RandomRelics))
            AddRandomStarterRelics(rng, StarterRelicCount);

        if (IsDifficultyBonusActive(GameDifficultyBonus.RandomEquipment))
            AddRandomStarterEquipments(rng, StarterEquipmentCount);

        if (IsDifficultyBonusActive(GameDifficultyBonus.ElectricityCoin))
            ElectricityCoin += StarterElectricityCoinBonus;

        if (IsDifficultyBonusActive(GameDifficultyBonus.PlayerStats))
            ApplyStarterPlayerStatBonus();
    }

    public static bool IsDifficultyBonusActive(GameDifficultyBonus bonus)
    {
        int bonusOrder = (int)bonus;
        return bonusOrder > 0 && bonusOrder <= GetActiveDifficultyBonusCount(Difficulty);
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

    private static int GetActiveDifficultyBonusCount(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, MinDifficulty, MaxDifficulty);

        // Keep the user's anchor points: difficulty 0 has all bonuses, difficulty 1 keeps 1-4.
        if (difficulty <= 0)
            return 5;

        return Math.Clamp(5 - difficulty, 0, 5);
    }

    private static IEnumerable<GameDifficultyBonus> GetActiveDifficultyBonuses(int difficulty)
    {
        int count = GetActiveDifficultyBonusCount(difficulty);
        for (int i = 1; i <= count; i++)
            yield return (GameDifficultyBonus)i;
    }

    private static string GetDifficultyBonusLabel(GameDifficultyBonus bonus)
    {
        return bonus switch
        {
            GameDifficultyBonus.RandomRelics => "开局2随机遗物",
            GameDifficultyBonus.RandomEquipment => "开局1随机装备",
            GameDifficultyBonus.FreeRetreat => "撤退不耗跃迁能量",
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
            int amount = Relic.GetInitialNum(relicId);
            if (!Relics.TryGetValue(relicId, out int currentAmount))
                Relics[relicId] = amount;
            else if (amount > 0)
                Relics[relicId] = currentAmount + amount;
        }
    }

    private static void AddRandomStarterEquipments(Random rng, int count)
    {
        if (Equipment.Catalog == null || Equipment.Catalog.Length == 0)
            return;

        OwnedEquipments ??= new List<Equipment>();
        for (int i = 0; i < count; i++)
            OwnedEquipments.Add(Equipment.Clone(PickRandom(Equipment.Catalog, rng)));
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

    private static T PickRandom<T>(IReadOnlyList<T> pool, Random rng)
    {
        rng ??= new Random();
        return pool[rng.Next(pool.Count)];
    }
}
