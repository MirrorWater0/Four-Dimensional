using System;
using System.Collections.Generic;
using Godot;

public static partial class GameInfo
{
    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 1113;
    public static Random IntentionRandom = new Random(Seed); 
    public static Dictionary<Vector2I, LevelNode.LevelState> FirstLevelState = new();
    public static void InitNewGame()
    {
        FirstLevelState.Clear();
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                FirstLevelState[new Vector2I(x, y)] = LevelNode.LevelState.Locked;
            }
        }
        FirstLevelState[new Vector2I(0, 0)] = LevelNode.LevelState.Unlocked;
        FirstLevelState[new Vector2I(0, 1)] = LevelNode.LevelState.Unlocked;
        FirstLevelState[new Vector2I(0, 2)] = LevelNode.LevelState.Unlocked;
    }
}

public struct PlayerInfoStructure
{
    public PlayerInfoStructure() { }

    public PackedScene CharacterScene;
    public int LifeMax;
    public int Power;
    public int Survivability;
    public int Speed;
    public List<Skill> GainedSkills = new List<Skill>();
    public Skill[] TakenSkills = new Skill[3];
    public int PositionIndex;
    public string PortaitPath;
}
