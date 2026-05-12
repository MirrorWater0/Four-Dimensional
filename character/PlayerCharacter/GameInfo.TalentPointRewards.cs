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
    private const int EliteTalentPointRewardAmount = 1;
    private const int EliteTalentPointRewardSeedSalt = unchecked((int)0x71A7E17E);

    public static TalentPointRewardResult TryGrantEliteTalentPointReward(LevelNode node)
    {
        if (node == null || node.Type != LevelNode.LevelType.Elite)
            return default;

        return GrantRandomTalentPointReward(node.RandomNum, EliteTalentPointRewardAmount);
    }

    private static TalentPointRewardResult GrantRandomTalentPointReward(int seed, int amount)
    {
        if (amount <= 0 || PlayerCharacters == null || PlayerCharacters.Length == 0)
            return default;

        List<int> candidateIndices = PlayerCharacters
            .Select((player, index) => new { player, index })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.player.CharacterName))
            .Select(entry => entry.index)
            .ToList();

        if (candidateIndices.Count == 0)
            return default;

        var rng = new Random(seed ^ EliteTalentPointRewardSeedSalt);
        int characterIndex = candidateIndices[rng.Next(candidateIndices.Count)];
        var info = PlayerCharacters[characterIndex];
        TalentTree.AddTalentPoints(ref info, amount);
        PlayerCharacters[characterIndex] = info;

        return new TalentPointRewardResult(
            granted: true,
            characterIndex,
            info.CharacterName,
            amount
        );
    }
}
