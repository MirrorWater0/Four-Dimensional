using Godot;
using System;
using System.Collections.Generic;

static public partial class GameInfo
{
    static public PlayerInfoStructure[] PlayerCharacters;
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
}
