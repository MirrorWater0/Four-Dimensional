using System;
using System.Collections.Generic;

public partial class Battle {
    private sealed class CharacterRandomStreams {
        public int Seed { get; }
        public Random Intention { get; }
        public Random Target { get; }
        public Random Effect { get; }

        public CharacterRandomStreams(int seed) {
            Seed = seed;
            Intention = new Random(HashSeed(seed, unchecked((int)0x13572468)));
            Target = new Random(HashSeed(seed, unchecked((int)0x3F29A6B1)));
            Effect = new Random(HashSeed(seed, unchecked((int)0x71C5D903)));
        }
    }

    private readonly Dictionary<Character, CharacterRandomStreams> _characterRandomStreams = new();
    private readonly Dictionary<string, int> _characterRandomSpawnCounts = new();
    private Random _playerSummonPositionRandom;
    private Random _enemySummonPositionRandom;

    public Random GetEnemyIntentionRandom(EnemyCharacter enemy) => GetCharacterRandomStreams(enemy).Intention;

    public Random GetTargetRandom(Character character) => GetCharacterRandomStreams(character).Target;

    public Random GetEffectRandom(Character character) => GetCharacterRandomStreams(character).Effect;

    private CharacterRandomStreams GetCharacterRandomStreams(Character character) {
        ArgumentNullException.ThrowIfNull(character);
        if (_characterRandomStreams.TryGetValue(character, out var streams))
            return streams;

        // Capture identity at spawn; later movement and other actors' random draws cannot change it.
        string key = character is PlayerCharacter player
            ? $"player:{player.CharacterIndex}"
            : $"{(character.IsPlayer ? "player" : "enemy")}:{character.GetType().FullName}:{character.PositionIndex}";
        if (character is SummonCharacter { Summoner: { } summoner })
            key = $"summon:{GetCharacterRandomStreams(summoner).Seed}:{key}";

        int spawnIndex = _characterRandomSpawnCounts.GetValueOrDefault(key);
        _characterRandomSpawnCounts[key] = spawnIndex + 1;
        streams = new CharacterRandomStreams(HashSeed(GetBattleBaseSeed(), StableStringHash(key), spawnIndex));
        _characterRandomStreams[character] = streams;
        return streams;
    }

    private Random GetSummonPositionRandom(bool isPlayer) {
        if (isPlayer)
            return _playerSummonPositionRandom ??= CreateBattleRandom(unchecked((int)0x26D8B451));

        return _enemySummonPositionRandom ??= CreateBattleRandom(unchecked((int)0x694F0C27));
    }

    private void InitializeBattleRandomStreams() {
        _characterRandomStreams.Clear();
        _characterRandomSpawnCounts.Clear();
        _playerSummonPositionRandom = null;
        _enemySummonPositionRandom = null;
    }
}
