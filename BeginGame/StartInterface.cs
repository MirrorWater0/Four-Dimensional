using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class StartInterface : CanvasLayer
{
    public static PackedScene TipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
    public static PackedScene _Echo = (PackedScene)
        ResourceLoader.Load("res://character/PlayerCharacter/Echo/Echo.tscn");
    public static PackedScene _Kasiya = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Kasiya/kasiya.tscn"
    );

    public override void _Ready()
    {
        var existingLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
        {
            if (!existingLayer.HasNode("Tip"))
            {
                var tip0 = TipScene.Instantiate<Tip>();
                tip0.Name = "Tip";
                tip0.FollowMouse = true;
                tip0.AnchorOffset = new Vector2(20f, 20f);
                existingLayer.AddChild(tip0);
            }

            if (!existingLayer.HasNode("BuffTip"))
            {
                var buffTip0 = TipScene.Instantiate<Tip>();
                buffTip0.Name = "BuffTip";
                buffTip0.FollowMouse = true;
                buffTip0.AnchorOffset = new Vector2(-20f, 20f);
                existingLayer.AddChild(buffTip0);
            }
            return;
        }

        CanvasLayer layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        var tip = TipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);

        var buffTip = TipScene.Instantiate<Tip>();
        buffTip.Name = "BuffTip";
        buffTip.FollowMouse = true;
        buffTip.AnchorOffset = new Vector2(-20f, 20f);

        layer.AddChild(tip);
        layer.AddChild(buffTip);
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, layer);
    }

    public void NewStart()
    {
        GameInfo.PlayerCharacters =
        [
            new PlayerCharacterRegistry().Echo,
            new PlayerCharacterRegistry().Kasiya,
            new PlayerCharacterRegistry().Echo,
            new PlayerCharacterRegistry().Kasiya,
        ];
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            info.PositionIndex = i + 1;
            info.GainedSkills.AddRange(info.TakenSkills);
            info.GainedSkills = info.GainedSkills.Distinct().ToList();
            GameInfo.PlayerCharacters[i] = info;
        }
        test();
    }

    public void Start()
    {
        NewStart();
        GameInfo.InitNewGame();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void test()
    {
        GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.Smite);
        GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.ShockWave);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.TerminateLight);
    }

    public void continueGame()
    {
        SaveSystem.LoadAll();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void TestBattle()
    {
        NewStart();
        Battle.Istest = true;
        GameInfo.InitNewGame();
        GetTree().ChangeSceneToFile("res://battle/Battle.tscn");
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Echo = new PlayerInfoStructure()
    {
        CharacterName = "Echo",
        LifeMax = 50,
        Power = 9,
        Survivability = 11,
        Speed = 12,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/Echo.png",
        TakenSkills = [SkillID.SacredOnslaught, SkillID.SoundBarrier, SkillID.EchonicResonance],
        AllSkills = [
            SkillID.SacredOnslaught,
            SkillID.SoundBarrier,
            SkillID.EchonicResonance,
        ]
    };
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = "Kasiya",
        LifeMax = 50,
        Power = 12,
        Survivability = 12,
        Speed = 9,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/Kasiya.png",
        TakenSkills = [SkillID.Determination, SkillID.ReNewedSpirit, SkillID.TerminateLight],
        AllSkills =
        [
            SkillID.Determination,
            SkillID.Charge,
            SkillID.Smite,
            SkillID.ReNewedSpirit,
            SkillID.AbsouluteDefense,
            SkillID.ShockWave,
            SkillID.TerminateLight,
        ],
    };
}
