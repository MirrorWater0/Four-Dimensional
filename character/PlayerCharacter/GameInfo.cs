using System;
using System.Collections.Generic;
using Godot;

public static partial class GameInfo
{
    public static PlayerInfoStructure[] PlayerCharacters;
    public static int Seed = 1113;
    public static Random IntentionRandom = new Random(Seed);
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
