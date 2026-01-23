using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ChoseCharater : CanvasLayer
{
    static public PackedScene _Echo = (PackedScene)ResourceLoader.Load("res://character/PlayerCharacter/Echo/Echo.tscn");
    static public PackedScene _Kasiya = ResourceLoader.Load<PackedScene>("res://character/PlayerCharacter/Kasiya/kasiya.tscn");
    public override void _Ready()
    {
        GameInfo.PlayerCharacters = new PlayerInfoStructure[4];
        
        // Initialize Kasiya characters (indices 0 and 1)
        for (int i = 0; i < 2; i++)
        {
            PlayerInfoStructure kasiyaStructure = new PlayerInfoStructure();
            kasiyaStructure.CharacterScene = _Kasiya;
            kasiyaStructure.LifeMax = 50;
            kasiyaStructure.Power = 10;
            kasiyaStructure.Survivability = 10;
            kasiyaStructure.Speed = 10;
            GameInfo.PlayerCharacters[i] = kasiyaStructure;
        }
        
        // Initialize Echo characters (indices 2 and 3)
        for (int i = 2; i < 4; i++)
        {
            PlayerInfoStructure echoStructure = new PlayerInfoStructure();
            echoStructure.CharacterScene = _Echo;
            echoStructure.LifeMax = 50;
            echoStructure.Power = 10;
            echoStructure.Survivability = 10;
            echoStructure.Speed = 10;
            GameInfo.PlayerCharacters[i] = echoStructure;
        }

        for (int i = 0; i < 4; i++)
        {
            GameInfo.PlayerCharacters[i].TakenSkills[0] = new Attack();
            GameInfo.PlayerCharacters[i].TakenSkills[1] = new Defense();
            GameInfo.PlayerCharacters[i].TakenSkills[2] = new SacredOnslaught();
            GameInfo.PlayerCharacters[i].PositionIndex = i + 1;

            GameInfo.PlayerCharacters[i].GainedSkills.AddRange(GameInfo.PlayerCharacters[i].TakenSkills);
        }
        InitializePostion();
    }
    
    public void Start()
    {
        Battle.Istest = false;
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void InitializePostion()
    {
        GameInfo.PlayerCharacters[0].GainedSkills.Add(new ReNewedSpirit());
    }
}
