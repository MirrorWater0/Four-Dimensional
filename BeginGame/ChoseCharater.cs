using Godot;
using System;
using System.Collections.Generic;

public partial class ChoseCharater : CanvasLayer
{
    static public PackedScene _Echo = (PackedScene)ResourceLoader.Load("res://character/PlayerCharacter/Echo/Echo.tscn");
    static public PackedScene _Kasiya = ResourceLoader.Load<PackedScene>("res://character/PlayerCharacter/Kasiya/kasiya.tscn");
    public override void _Ready()
    {
        Echo _echo1 = _Echo.Instantiate() as Echo;
        _echo1.Skills = new Skill[] { new Attack(_echo1),new FollowingLight(_echo1),new SacredOnslaught(_echo1),new Combo(_echo1) };
        Echo _echo2 = _Echo.Instantiate() as Echo;
        _echo2.Skills = new Skill[] { new Attack(_echo2),new FollowingLight(_echo2),new SacredOnslaught(_echo2),new Combo(_echo2) };
        Kasiya kasiya1 = _Kasiya.Instantiate() as Kasiya;
        kasiya1.Skills = new Skill[] { new Attack(kasiya1),new Determination(kasiya1),new Defense(kasiya1),new Smite(kasiya1) };
        Kasiya kasiya2 = _Kasiya.Instantiate() as Kasiya;
        kasiya2.Skills = new Skill[] { new Attack(kasiya2),new Determination(kasiya2),new Defense(kasiya2),new Smite(kasiya2) };
        PlayerInfo.PlayerCharaters = new PlayerCharater[]{kasiya1, kasiya2,_echo1,_echo2};
        InitializePostion();
    }
    
    public void Start()
    {
        Battle.Istest = false;
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void InitializePostion()
    {
        for (int i = 0; i < PlayerInfo.PlayerCharaters.Length; i++)
        {
            var charater = PlayerInfo.PlayerCharaters[i];
            charater.PositionIndex = i + 1;
            charater.Lifemax = 50;
            charater.Power = 10;
            charater.Survivability = 10;
            charater.UntakeSkills = new List<Skill>() { new Attack(charater), new Combo(charater)};
        }
        PlayerInfo.PlayerCharaters[0].UntakeSkills.Add(new ReNewedSpirit(PlayerInfo.PlayerCharaters[0]));
    }
}
