using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ChoseCharater : CanvasLayer
{
    public static PackedScene _Echo = (PackedScene)
        ResourceLoader.Load("res://character/PlayerCharacter/Echo/Echo.tscn");
    public static PackedScene _Kasiya = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Kasiya/kasiya.tscn"
    );

    public override void _Ready()
    {
        GameInfo.PlayerCharacters = new PlayerInfoStructure[4];

        // Initialize Kasiya characters (indices 0 and 1)
        for (int i = 0; i < 2; i++)
        {
            PlayerInfoStructure kasiyaStructure = new PlayerInfoStructure();
            kasiyaStructure.CharacterScenePath =
                "res://character/PlayerCharacter/Kasiya/kasiya.tscn";
            kasiyaStructure.LifeMax = 50;
            kasiyaStructure.Power = 10;
            kasiyaStructure.Survivability = 10;
            kasiyaStructure.Speed = 10;
            GameInfo.PlayerCharacters[i] = kasiyaStructure;
            GameInfo.PlayerCharacters[i].PortaitPath =
                "res://asset/PlayerCharater/Kasiya/Kasiya.png";
        }

        // Initialize Echo characters (indices 2 and 3)
        for (int i = 2; i < 4; i++)
        {
            PlayerInfoStructure echoStructure = new PlayerInfoStructure();
            echoStructure.CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn";
            echoStructure.LifeMax = 50;
            echoStructure.Power = 10;
            echoStructure.Survivability = 10;
            echoStructure.Speed = 10;
            GameInfo.PlayerCharacters[i] = echoStructure;
            GameInfo.PlayerCharacters[i].PortaitPath = "res://asset/PlayerCharater/Echo/Echo.png";
        }

        for (int i = 0; i < 4; i++)
        {
            GameInfo.PlayerCharacters[i].TakenSkills[0] = SkillID.SacredOnslaught;
            GameInfo.PlayerCharacters[i].TakenSkills[1] = SkillID.SoundBarrier;
            GameInfo.PlayerCharacters[i].TakenSkills[2] = SkillID.EchonicResonance;
            GameInfo.PlayerCharacters[i].PositionIndex = i + 1;

            GameInfo
                .PlayerCharacters[i]
                .GainedSkills.AddRange(GameInfo.PlayerCharacters[i].TakenSkills);
        }
        test();
    }

    public void Start()
    {
        Battle.Istest = false;
        GameInfo.InitNewGame();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void test()
    {
        GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.ReNewedSpirit);
        GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.Determination);
        GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.TerminateLight);
    }
}
