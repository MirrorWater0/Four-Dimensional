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
    public static PackedScene _Mariya = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Mariya/Mariya.tscn"
    );
    public static PackedScene _Nightingale = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Nightingale/Nightingale.tscn"
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

            if (!existingLayer.HasNode("EquipmentTip"))
            {
                var equipmentTip0 = TipScene.Instantiate<Tip>();
                equipmentTip0.Name = "EquipmentTip";
                equipmentTip0.FollowMouse = true;
                equipmentTip0.AnchorOffset = new Vector2(-20f, -20f);
                existingLayer.AddChild(equipmentTip0);
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

        var equipmentTip = TipScene.Instantiate<Tip>();
        equipmentTip.Name = "EquipmentTip";
        equipmentTip.FollowMouse = true;
        equipmentTip.AnchorOffset = new Vector2(-20f, -20f);

        layer.AddChild(tip);
        layer.AddChild(buffTip);
        layer.AddChild(equipmentTip);
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, layer);
    }

    public void NewStart()
    {
        GameInfo.PlayerCharacters =
        [
            new PlayerCharacterRegistry().Echo,
            new PlayerCharacterRegistry().Kasiya,
            new PlayerCharacterRegistry().Mariya,
            new PlayerCharacterRegistry().Nightingale,
        ];
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
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
        // Give the first character the whole roster's skill pool for pagination tests.
        // SkillID[] rosterSkills = GameInfo
        //     .PlayerCharacters.Where(info => info.AllSkills != null)
        //     .SelectMany(info => info.AllSkills)
        //     .Distinct()
        //     .ToArray();
        // AddTestSkills(0, rosterSkills);
        GameInfo.ElectricityCoin += 999;
    }

    private static void AddTestSkills(int characterIndex, params SkillID[] skills)
    {
        if (GameInfo.PlayerCharacters == null)
            return;
        if (characterIndex < 0 || characterIndex >= GameInfo.PlayerCharacters.Length)
            return;

        var info = GameInfo.PlayerCharacters[characterIndex];
        info.GainedSkills ??= new List<SkillID>();
        info.GainedSkills.AddRange(skills);
        info.GainedSkills = info.GainedSkills.Distinct().ToList();
        GameInfo.PlayerCharacters[characterIndex] = info;
    }

    public void continueGame()
    {
        try
        {
            SaveSystem.LoadAll();
        }
        catch (Exception e)
        {
            GD.PushError($"ContinueGame load failed: {e}");
            return;
        }

        var err = GetTree().ChangeSceneToFile("res://Map/Map.tscn");
        if (err != Error.Ok)
            GD.PushError($"ContinueGame scene switch failed: {err}");
    }

    public void falseTest()
    {
        Battle.Istest = false;
        Start();
    }
}

public partial class PlayerCharacterRegistry
{
    public PlayerInfoStructure Echo = new PlayerInfoStructure()
    {
        CharacterName = "Echo",
        PassiveName = global::Echo.PassiveNameText,
        PassiveDescription = global::Echo.PassiveDescriptionText,
        LifeMax = 50,
        Power = 9,
        Survivability = 11,
        Speed = 10,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/EchoPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = "Kasiya",
        PassiveName = global::Kasiya.PassiveNameText,
        PassiveDescription = global::Kasiya.PassiveDescriptionText,
        LifeMax = 60,
        Power = 12,
        Survivability = 12,
        Speed = 8,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
    public PlayerInfoStructure Mariya = new PlayerInfoStructure()
    {
        CharacterName = "Mariya",
        PassiveName = global::Mariya.PassiveNameText,
        PassiveDescription = global::Mariya.PassiveDescriptionText,
        LifeMax = 45,
        Power = 9,
        Survivability = 10,
        Speed = 9,
        CharacterScenePath = "res://character/PlayerCharacter/Mariya/Mariya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Mariya/MariyaPortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };

    public PlayerInfoStructure Nightingale = new PlayerInfoStructure()
    {
        CharacterName = "Nightingale",
        PassiveName = global::Nightingale.PassiveNameText,
        PassiveDescription = global::Nightingale.PassiveDescriptionText,
        LifeMax = 50,
        Power = 11,
        Survivability = 10,
        Speed = 11,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        TakenSkills = [SkillID.BasicAttack, SkillID.BasicDefense, SkillID.BasicSpecial],
    };
}
