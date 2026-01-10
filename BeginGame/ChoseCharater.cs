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
        Echo _echo1 = _Echo.Instantiate() as Echo;
        _echo1.TakenSkills = new Skill[] { new Attack(_echo1),new FollowingLight(_echo1),new SacredOnslaught(_echo1) };
        
        Echo _echo2 = _Echo.Instantiate() as Echo;
        _echo2.TakenSkills = new Skill[] { new Attack(_echo2),new FollowingLight(_echo2),new SacredOnslaught(_echo2) };
        Kasiya kasiya1 = _Kasiya.Instantiate() as Kasiya;
        kasiya1.TakenSkills = new Skill[] { new Attack(kasiya1),new Defense(kasiya1),new TerminateLight(kasiya1) };
        kasiya1.GainedSkills.Add(new ReNewedSpirit(kasiya1));
        kasiya1.GainedSkills.Add(new Determination(kasiya1));
        Kasiya kasiya2 = _Kasiya.Instantiate() as Kasiya;
        kasiya2.TakenSkills = new Skill[] { new Attack(kasiya2),new Defense(kasiya2),new TerminateLight(kasiya2)};
        kasiya2.GainedSkills.Add(new Cast(kasiya2));
        PlayerInfo.PlayerCharaters = new PlayerCharacter[]{kasiya1, kasiya2,_echo1,_echo2};

        for (int i = 0;i < 4; i++)
        {
            var character = PlayerInfo.PlayerCharaters[i];
            character.GainedSkills.AddRange(character.TakenSkills);
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
