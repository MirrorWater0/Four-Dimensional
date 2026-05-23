using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct TalentPointRewardResult(
    bool granted,
    int characterIndex,
    string characterName,
    int amount
)
{
    public bool Granted { get; } = granted;
    public int CharacterIndex { get; } = characterIndex;
    public string CharacterName { get; } = characterName;
    public int Amount { get; } = amount;
}

public static partial class GameInfo
{
    private const int BattleTalentPointRewardAmount = 1;
    private const int BattleTalentPointRewardSeedSalt = unchecked((int)0x71A7E17E);

    public static TalentPointRewardResult PreviewBattleTalentPointReward(LevelNode node)
    {
        if (!CanGrantBattleTalentPointReward(node))
            return default;

        return BuildRandomTalentPointReward(
            node.RandomNum,
            BattleTalentPointRewardAmount,
            grant: false
        );
    }

    public static TalentPointRewardResult TryGrantBattleTalentPointReward(LevelNode node)
    {
        if (!CanGrantBattleTalentPointReward(node))
            return default;

        return BuildRandomTalentPointReward(
            node.RandomNum,
            BattleTalentPointRewardAmount,
            grant: true
        );
    }

    public static TalentPointRewardResult TryGrantEliteTalentPointReward(LevelNode node)
    {
        if (node == null || node.Type != LevelNode.LevelType.Elite)
            return default;

        return TryGrantBattleTalentPointReward(node);
    }

    private static bool CanGrantBattleTalentPointReward(LevelNode node)
    {
        return node?.Type is LevelNode.LevelType.Elite or LevelNode.LevelType.Boss;
    }

    private static TalentPointRewardResult BuildRandomTalentPointReward(
        int seed,
        int amount,
        bool grant
    )
    {
        if (amount <= 0 || PlayerCharacters == null || PlayerCharacters.Length == 0)
            return default;

        List<int> candidateIndices = PlayerCharacters
            .Select((player, index) => new { player, index })
            .Where(entry =>
                !string.IsNullOrWhiteSpace(entry.player.CharacterName)
                && !HasReachedTalentPointRewardLimit(entry.player)
            )
            .Select(entry => entry.index)
            .ToList();

        if (candidateIndices.Count == 0)
            return default;

        var rng = new Random(seed ^ BattleTalentPointRewardSeedSalt);
        int characterIndex = candidateIndices[rng.Next(candidateIndices.Count)];
        var info = PlayerCharacters[characterIndex];
        if (grant)
        {
            TalentTree.AddTalentPoints(ref info, amount);
            PlayerCharacters[characterIndex] = info;
        }

        return new TalentPointRewardResult(
            granted: true,
            characterIndex,
            info.CharacterName,
            amount
        );
    }

    private static bool HasReachedTalentPointRewardLimit(PlayerInfoStructure info)
    {
        var nodes = TalentTree.GetNodes(info.CharacterName);
        if (nodes.Count == 0)
            return true;

        int totalCost = nodes.Sum(node => node.Cost);
        if (totalCost <= 0)
            return true;

        var unlockedTalentIds = new HashSet<string>(
            info.UnlockedTalents ?? [],
            StringComparer.Ordinal
        );
        int spentCost = nodes
            .Where(node => unlockedTalentIds.Contains(node.Id))
            .Sum(node => node.Cost);
        int earnedTalentPoints = Math.Max(0, info.TalentPoints) + spentCost;
        return earnedTalentPoints >= totalCost;
    }
}
