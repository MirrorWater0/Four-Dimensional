using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct StarterBonusTalentPointResult(
    bool granted,
    string[] characterNames,
    int amountPerCharacter
)
{
    public bool Granted { get; } = granted;
    public string[] CharacterNames { get; } = characterNames ?? Array.Empty<string>();
    public int AmountPerCharacter { get; } = amountPerCharacter;
}

public enum StarterBonusOption
{
    None = 0,
    Blessing = 1,
    ExtraBattleSkillRewards = 2,
    RandomRareSkill = 3,
    RandomRelic = 4,
    RandomTalentPoints = 5,
    TransformTwoCards = 6,
}

public static partial class GameInfo
{
    public const int EarlyBattleExtraSkillRewardBattles = 3;
    public const int StarterBonusChoiceCount = 3;
    private const int StarterBonusRareSkillSeedSalt = unchecked((int)0x57A7E8B1);
    private const int StarterBonusOptionPoolSeedSalt = unchecked((int)0x5A7E8203);
    private const int StarterBonusTalentPointSeedSalt = unchecked((int)0x3C91F4A7);
    public const int StarterBonusTalentPointRecipientCount = 2;
    public const int StarterBonusTalentPointAmountPerCharacter = 1;
    public const int RandomTalentPointsElectricityCost = 50;
    public const int TransformTwoCardsElectricityCost = 100;
    public const int TransformTwoCardsCount = 2;

    public static RelicID? LastStarterBonusGrantedRelic { get; private set; }
    public static StarterBonusTalentPointResult LastStarterBonusTalentGrantResult { get; private set; }

    private static readonly StarterBonusOption[] StarterBonusOptionPool =
    [
        StarterBonusOption.Blessing,
        StarterBonusOption.ExtraBattleSkillRewards,
        StarterBonusOption.RandomRareSkill,
        StarterBonusOption.RandomRelic,
        StarterBonusOption.RandomTalentPoints,
        StarterBonusOption.TransformTwoCards,
    ];

    public static int GetStarterBonusElectricityCost(StarterBonusOption option) =>
        option switch
        {
            StarterBonusOption.RandomTalentPoints => RandomTalentPointsElectricityCost,
            StarterBonusOption.TransformTwoCards => TransformTwoCardsElectricityCost,
            _ => 0,
        };

    public static bool HasEarlyBattleExtraSkillReward =>
        SelectedStarterBonus == StarterBonusOption.ExtraBattleSkillRewards;

    public static int GetBattleSkillRewardGroupCount(LevelNode node = null)
    {
        int groups = 1;
        if (
            HasEarlyBattleExtraSkillReward
            && GetRegionalBattleOrdinal(node) <= EarlyBattleExtraSkillRewardBattles
        )
        {
            groups += 1;
        }

        return groups;
    }

    private static int GetRegionalBattleOrdinal(LevelNode node)
    {
        if (node != null)
            return GameInfo.GetNodeRegionBattleQueueIndex(node) + 1;

        return GetCompletedBattleCount() + 1;
    }

    public static StarterBonusOption[] RollStarterBonusOptions()
    {
        var pool = new List<StarterBonusOption>(StarterBonusOptionPool);
        var rng = new Random(Seed ^ StarterBonusOptionPoolSeedSalt);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (pool[i], pool[swapIndex]) = (pool[swapIndex], pool[i]);
        }

        int takeCount = Math.Min(StarterBonusChoiceCount, pool.Count);
        return pool.Take(takeCount).ToArray();
    }

    public static void ApplyStarterBonus(StarterBonusOption choice)
    {
        SelectedStarterBonus = choice;
        PendingStarterBonusChoice = false;
        LastStarterBonusGrantedRelic = null;
        LastStarterBonusTalentGrantResult = default;

        switch (choice)
        {
            case StarterBonusOption.Blessing:
                SetRelicCount(RelicID.Blessing, Relic.GetAcquireAmount(RelicID.Blessing));
                break;
            case StarterBonusOption.ExtraBattleSkillRewards:
                break;
            case StarterBonusOption.RandomRareSkill:
                GrantRandomStarterRareSkill();
                break;
            case StarterBonusOption.RandomRelic:
                LastStarterBonusGrantedRelic = DrawStarterRelicFromQueue();
                if (LastStarterBonusGrantedRelic.HasValue)
                {
                    SetRelicCount(
                        LastStarterBonusGrantedRelic.Value,
                        Relic.GetAcquireAmount(LastStarterBonusGrantedRelic.Value)
                    );
                }
                break;
            case StarterBonusOption.RandomTalentPoints:
                LastStarterBonusTalentGrantResult = TryGrantStarterBonusTalentPointReward();
                break;
            case StarterBonusOption.TransformTwoCards:
                break;
        }
    }

    public static int GetCompletedBattleCount()
    {
        return CompletedLevelNodeRecords?.Values.Count(record =>
                record != null
                && record.NodeType
                    is LevelNode.LevelType.Normal
                        or LevelNode.LevelType.Elite
                        or LevelNode.LevelType.Boss
            ) ?? 0;
    }

    private static void GrantRandomStarterRareSkill()
    {
        NormalizePlayerCharacters();
        if (PlayerCharacters == null || PlayerCharacters.Length == 0)
            return;

        var candidates = new List<(int CharacterIndex, SkillID SkillId)>();
        for (int i = 0; i < PlayerCharacters.Length; i++)
        {
            var info = PlayerCharacters[i];
            SkillID[] pool = (info.AllSkills ?? Array.Empty<SkillID>())
                .Where(skillId => !IsBasicSkill(skillId))
                .Where(skillId => Skill.GetRarity(skillId) == Skill.SkillRarity.Rare)
                .Distinct()
                .ToArray();
            if (pool.Length == 0)
                continue;

            info.GainedSkills ??= new List<SkillID>();
            foreach (SkillID skillId in pool)
            {
                if (!info.GainedSkills.Contains(skillId))
                    candidates.Add((i, skillId));
            }
        }

        if (candidates.Count == 0)
            return;

        var rng = new Random(Seed ^ StarterBonusRareSkillSeedSalt);
        (int characterIndex, SkillID pickedSkillId) = candidates[rng.Next(candidates.Count)];

        var target = PlayerCharacters[characterIndex];
        target.GainedSkills ??= new List<SkillID>();
        if (!target.GainedSkills.Contains(pickedSkillId))
            target.GainedSkills.Add(pickedSkillId);
        PlayerCharacters[characterIndex] = target;
    }

    public static StarterBonusTalentPointResult PreviewStarterBonusTalentPointReward() =>
        BuildStarterBonusTalentPointReward(grant: false);

    public static StarterBonusTalentPointResult TryGrantStarterBonusTalentPointReward() =>
        BuildStarterBonusTalentPointReward(grant: true);

    private static StarterBonusTalentPointResult BuildStarterBonusTalentPointReward(bool grant)
    {
        NormalizePlayerCharacters();
        if (PlayerCharacters == null || PlayerCharacters.Length == 0)
            return default;

        List<int> candidates = GetTalentPointRewardCandidateIndices();
        if (candidates.Count < StarterBonusTalentPointRecipientCount)
            return default;

        var pool = new List<int>(candidates);
        var rng = new Random(Seed ^ StarterBonusTalentPointSeedSalt);
        var picked = new List<int>(StarterBonusTalentPointRecipientCount);
        for (int i = 0; i < StarterBonusTalentPointRecipientCount; i++)
        {
            int pickIndex = rng.Next(pool.Count);
            picked.Add(pool[pickIndex]);
            pool.RemoveAt(pickIndex);
        }

        var names = new string[picked.Count];
        for (int i = 0; i < picked.Count; i++)
        {
            int characterIndex = picked[i];
            var info = PlayerCharacters[characterIndex];
            if (grant)
            {
                TalentTree.AddTalentPoints(ref info, StarterBonusTalentPointAmountPerCharacter);
                PlayerCharacters[characterIndex] = info;
            }

            names[i] = info.CharacterName;
        }

        return new StarterBonusTalentPointResult(
            granted: true,
            names,
            StarterBonusTalentPointAmountPerCharacter
        );
    }
}
