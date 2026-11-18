using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static partial class GameInfo
{
    public const int WorldLineCount = 5;
    public const int WorldLineSwitchLimit = 3;
    public const int FirstEliteRoadStage = 3; // Zero-based: elites start at the fourth node.
    private const int CurrentWorldLineMapVersion = 1;

    public static int CurrentWorldLine;
    public static int WorldLineSwitchesRemaining = WorldLineSwitchLimit;
    public static int WorldLineMapVersion;
    public static int WorldLineMapRegion = -1;
    public static Dictionary<Vector2I, LevelNode.LevelType> WorldLineNodeTypes = new();

    private readonly struct WorldLineProfile
    {
        public readonly string Name;
        public readonly int Normal;
        public readonly int Event;
        public readonly int Shop;
        public readonly int Rest;
        public readonly int Elite;
        public readonly int Treasure;

        public WorldLineProfile(
            string name,
            int normal,
            int @event,
            int shop,
            int rest,
            int elite,
            int treasure
        )
        {
            Name = name;
            Normal = normal;
            Event = @event;
            Shop = shop;
            Rest = rest;
            Elite = elite;
            Treasure = treasure;
        }

        public int Total => Normal + Event + Shop + Rest + Elite + Treasure;
    }

    private static readonly WorldLineProfile[] WorldLineProfiles =
    {
        new("稳定线", 48, 16, 12, 10, 8, 6),
        new("回响线", 28, 30, 10, 8, 14, 10),
        new("商贸线", 30, 12, 30, 10, 8, 10),
        new("猎杀线", 24, 12, 8, 8, 36, 12),
        new("秘藏线", 28, 16, 8, 10, 12, 26),
    };

    private static readonly WorldLineProfile[] OpeningWorldLineProfiles =
        WorldLineProfiles.Select(BuildOpeningWorldLineProfile).ToArray();

    private static WorldLineProfile BuildOpeningWorldLineProfile(WorldLineProfile profile)
    {
        int[] weights = [profile.Normal, profile.Event, profile.Shop, profile.Rest, 0, profile.Treasure];
        int total = weights.Sum();
        int[] percentages = weights.Select(weight => weight * 100 / total).ToArray();
        // Distribute rounding remainders once, so displayed percentages and actual
        // integer draw weights are identical and always sum to 100.
        int remaining = 100 - percentages.Sum();
        foreach (int index in Enumerable.Range(0, weights.Length)
            .OrderByDescending(index => weights[index] * 100 % total).Take(remaining))
            percentages[index]++;

        return new WorldLineProfile(profile.Name, percentages[0], percentages[1],
            percentages[2], percentages[3], percentages[4], percentages[5]);
    }

    private static WorldLineProfile GetWorldLineProfile(int worldLine, int stageIndex)
    {
        int index = NormalizeWorldLine(worldLine);
        return stageIndex < FirstEliteRoadStage
            ? OpeningWorldLineProfiles[index] : WorldLineProfiles[index];
    }

    public static void ResetWorldLineState()
    {
        CurrentWorldLine = 0;
        WorldLineSwitchesRemaining = WorldLineSwitchLimit;
        WorldLineMapVersion = 0;
        WorldLineMapRegion = -1;
        WorldLineNodeTypes = new();
    }

    public static void PrepareWorldLineMap(int stageCount)
    {
        if (WorldLineMapVersion < CurrentWorldLineMapVersion)
        {
            MigrateLegacyWorldLineMap(stageCount);
            WorldLineMapVersion = CurrentWorldLineMapVersion;
            WorldLineMapRegion = CurrentLevel;
        }
        else if (WorldLineMapRegion != CurrentLevel)
        {
            CurrentWorldLine = 0;
            WorldLineSwitchesRemaining = WorldLineSwitchLimit;
            WorldLineNodeTypes.Clear();
            WorldLineMapRegion = CurrentLevel;
        }

        CurrentWorldLine = NormalizeWorldLine(CurrentWorldLine);
        WorldLineSwitchesRemaining = Math.Clamp(WorldLineSwitchesRemaining, 0, WorldLineSwitchLimit);
    }

    private static void MigrateLegacyWorldLineMap(int stageCount)
    {
        // Collapse the route already taken in a branching save onto slot zero.
        // Keep completion records and encounter visit indices attached to it.
        var oldStates = FirstLevelState.ToArray();
        var currentRecords = CompletedLevelNodeRecords.Values
            .Where(record => record != null && record.MapLevel == CurrentLevel)
            .ToArray();
        var visitedCoordinates = oldStates
            .Where(pair => pair.Value == LevelNode.LevelState.Completed)
            .Select(pair => pair.Key)
            .ToHashSet();
        foreach (var record in currentRecords)
            visitedCoordinates.Add(record.Coordinate);

        int lastCompleted = visitedCoordinates.Count == 0
            ? -1
            : Math.Clamp(visitedCoordinates.Max(coordinate => coordinate.X), -1, stageCount - 1);

        foreach (var record in currentRecords)
        {
            CompletedLevelNodeRecords.Remove(GetLevelNodeRecordKey(CurrentLevel, record.Coordinate));
            record.Coordinate = new Vector2I(record.Coordinate.X, 0);
            CompletedLevelNodeRecords[GetLevelNodeRecordKey(CurrentLevel, record.Coordinate)] = record;
        }

        RemapLegacyRoadIndices(NodeNormalBattleVisitIndices, visitedCoordinates);
        RemapLegacyRoadIndices(NodeEliteBattleVisitIndices, visitedCoordinates);
        RemapLegacyRoadIndices(NodeRelicQueueStarts, visitedCoordinates);
        RemapLegacyRoadIndices(NodeRelicQueueSlots, visitedCoordinates);

        WorldLineNodeTypes.Clear();
        FirstLevelState.Clear();
        for (int stage = 0; stage < stageCount; stage++)
        {
            var coordinate = new Vector2I(stage, 0);
            FirstLevelState[coordinate] = stage <= lastCompleted
                ? LevelNode.LevelState.Completed
                : stage == lastCompleted + 1
                    ? LevelNode.LevelState.Unlocked
                    : LevelNode.LevelState.Locked;
            if (stage <= lastCompleted)
            {
                var record = currentRecords.FirstOrDefault(item => item.Coordinate.X == stage);
                WorldLineNodeTypes[coordinate] = record?.NodeType
                    ?? (stage == stageCount - 1 ? LevelNode.LevelType.Boss : LevelNode.LevelType.Normal);
            }
        }

        CurrentWorldLine = 0;
        WorldLineSwitchesRemaining = WorldLineSwitchLimit;
    }

    private static void RemapLegacyRoadIndices(
        Dictionary<Vector2I, int> indices,
        HashSet<Vector2I> visitedCoordinates
    )
    {
        var visited = indices.Where(pair => visitedCoordinates.Contains(pair.Key)).ToArray();
        indices.Clear();
        foreach (var pair in visited)
            indices[new Vector2I(pair.Key.X, 0)] = pair.Value;
    }

    public static string GetWorldLineName(int worldLine = -1)
    {
        int index = NormalizeWorldLine(worldLine < 0 ? CurrentWorldLine : worldLine);
        return WorldLineProfiles[index].Name;
    }

    public static int NormalizeWorldLine(int worldLine)
    {
        return Math.Clamp(worldLine, 0, WorldLineCount - 1);
    }

    public static bool TrySwitchWorldLine(int worldLine)
    {
        if (worldLine < 0 || worldLine >= WorldLineCount)
            return false;

        int nextWorldLine = worldLine;
        if (nextWorldLine == CurrentWorldLine || WorldLineSwitchesRemaining <= 0)
            return false;

        CurrentWorldLine = nextWorldLine;
        WorldLineSwitchesRemaining--;
        return true;
    }

    public static int GetWorldLineProbability(int worldLine, LevelNode.LevelType type, int stageIndex)
    {
        WorldLineProfile profile = GetWorldLineProfile(worldLine, stageIndex);
        return type switch
        {
            LevelNode.LevelType.Event => profile.Event,
            LevelNode.LevelType.Shop => profile.Shop,
            LevelNode.LevelType.Rest => profile.Rest,
            LevelNode.LevelType.Elite => profile.Elite,
            LevelNode.LevelType.Treasure => profile.Treasure,
            LevelNode.LevelType.Normal => profile.Normal,
            _ => 0,
        };
    }

    public static LevelNode.LevelType RollWorldLineNodeType(int worldLine, Random rng, int stageIndex)
    {
        WorldLineProfile profile = GetWorldLineProfile(worldLine, stageIndex);
        int roll = rng.Next(profile.Total);
        if ((roll -= profile.Normal) < 0)
            return LevelNode.LevelType.Normal;
        if ((roll -= profile.Event) < 0)
            return LevelNode.LevelType.Event;
        if ((roll -= profile.Shop) < 0)
            return LevelNode.LevelType.Shop;
        if ((roll -= profile.Rest) < 0)
            return LevelNode.LevelType.Rest;
        if ((roll -= profile.Elite) < 0)
            return LevelNode.LevelType.Elite;
        return LevelNode.LevelType.Treasure;
    }

    public static Random CreateWorldLineRng(int stageIndex, int worldLine = -1)
    {
        unchecked
        {
            int line = NormalizeWorldLine(worldLine < 0 ? CurrentWorldLine : worldLine);
            int seed = Seed;
            seed = (seed * 397) ^ (CurrentLevel * 7919);
            seed = (seed * 397) ^ (line * 104729);
            seed = (seed * 397) ^ (stageIndex * 15485863);
            return new Random(seed);
        }
    }
}
