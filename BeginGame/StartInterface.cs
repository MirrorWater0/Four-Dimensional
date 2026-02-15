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
        CanvasLayer layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        var tip = TipScene.Instantiate();
        tip.Name = "Tip";
        layer.AddChild(tip);
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
            GameInfo.PlayerCharacters[i].PositionIndex = i + 1;
            GameInfo
                .PlayerCharacters[i]
                .GainedSkills.AddRange(GameInfo.PlayerCharacters[i].TakenSkills);
        }
        test();
    }

    public void Start()
    {
        NewStart();
        Battle.Istest = false;
        GameInfo.InitNewGame();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }

    public void test()
    {
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.ReNewedSpirit);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.Determination);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.TerminateLight);
    }

    public void continueGame()
    {
        SaveSystem.LoadAll();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Echo = new PlayerInfoStructure()
    {
        LifeMax = 50,
        Power = 10,
        Survivability = 10,
        Speed = 10,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/Echo.png",
        TakenSkills = [SkillID.SacredOnslaught, SkillID.SoundBarrier, SkillID.EchonicResonance],
    };
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        LifeMax = 50,
        Power = 10,
        Survivability = 10,
        Speed = 10,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/Kasiya.png",
        TakenSkills = [SkillID.Determination, SkillID.ReNewedSpirit, SkillID.TerminateLight],
    };
}
