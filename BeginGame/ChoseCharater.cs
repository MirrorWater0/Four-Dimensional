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
        PlayerInfoStructure _echoStructure = new PlayerInfoStructure();
        _echoStructure.CharacterScene = _Echo;
        _echoStructure.LifeMax = 50;
        _echoStructure.Power = 10;
        _echoStructure.Survivability = 10;
        _echoStructure.Speed = 10;
        PlayerInfoStructure _kasiyaStructure = new PlayerInfoStructure();
        _kasiyaStructure.CharacterScene = _Kasiya;
        _kasiyaStructure.LifeMax = 50;
        _kasiyaStructure.Power = 10;
        _kasiyaStructure.Survivability = 10;
        _kasiyaStructure.Speed = 10;
        GameInfo.PlayerCharaters = new PlayerInfoStructure[]{_kasiyaStructure, _kasiyaStructure,_echoStructure, _echoStructure};

        for (int i = 0;i < 4; i++)
        {
            var character = GameInfo.PlayerCharaters[i];
            GameInfo.PlayerCharaters[i].GainedSkills.AddRange(character.TakenSkills);
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
        GameInfo.PlayerCharaters[0].GainedSkills.Add(new ReNewedSpirit(null));
    }
}
