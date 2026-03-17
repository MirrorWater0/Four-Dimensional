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
            new PlayerCharacterRegistry().Mariya,
            new PlayerCharacterRegistry().Nightingale,
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
        GameInfo.Relic.Add(RelicID.Blessing, 3);
        GameInfo.PlayerCharacters[3].GainedSkills.Add(SkillID.ShadowExecution);
        GameInfo.PlayerCharacters[2].GainedSkills.Add(SkillID.SwapSlash);
        // GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.Smite);
        // GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.Vower);
        // GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.TauntingGuard);
        // GameInfo.PlayerCharacters[1].GainedSkills.Add(SkillID.HolySeal);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.ResonantWard);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.SonicBoom);
        // GameInfo.PlayerCharacters[0].GainedSkills.Add(SkillID.PhaseEcho);
        // GameInfo.PlayerCharacters[3].GainedSkills.Add(SkillID.LongNight);
        // GameInfo.PlayerCharacters[2].GainedSkills.Add(SkillID.Sacrifice);
        // GameInfo.PlayerCharacters[2].GainedSkills.Add(SkillID.CrystalGuard);
        // GameInfo.PlayerCharacters[3].GainedSkills.Add(SkillID.Swift);
    }

    public void continueGame()
    {
        SaveSystem.LoadAll();
        GetTree().ChangeSceneToFile("res://Map/Map.tscn");
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
        PassiveName = "余响",
        PassiveDescription = $"使用生存技能时：获得{1}点能量。\n使用非生存技能时：获得{2}点力量。",
        LifeMax = 50,
        Power = 9,
        Survivability = 11,
        Speed = 10,
        CharacterScenePath = "res://character/PlayerCharacter/Echo/Echo.tscn",
        PortaitPath = "res://asset/PlayerCharater/Echo/EchoPortrait.png",
        TakenSkills = [SkillID.BreakStrike, SkillID.SoundBarrier, SkillID.EchonicResonance],
        AllSkills =
        [
            SkillID.SacredOnslaught,
            SkillID.ResonantSlash,
            SkillID.EchoPuncture,
            SkillID.BreakStrike,
            SkillID.SoundBarrier,
            SkillID.SonicDeflection,
            SkillID.TuningStance,
            SkillID.ResonantWard,
            SkillID.EchonicResonance,
            SkillID.SonicBoom,
            SkillID.PhaseEcho,
            SkillID.ResonantSlash,
            SkillID.ResonantWard,
        ],
    };
    public PlayerInfoStructure Kasiya = new PlayerInfoStructure()
    {
        CharacterName = "Kasiya",
        PassiveName = "坚毅",
        PassiveDescription = "当其他队友使用攻击技能：回复一次生命。",
        LifeMax = 60,
        Power = 12,
        Survivability = 12,
        Speed = 8,
        CharacterScenePath = "res://character/PlayerCharacter/Kasiya/kasiya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Kasiya/KasiyaPortrait.png",
        TakenSkills = [SkillID.Determination, SkillID.ReNewedSpirit, SkillID.TerminateLight],
        AllSkills =
        [
            SkillID.Determination,
            SkillID.Charge,
            SkillID.Smite,
            SkillID.ReNewedSpirit,
            SkillID.AbsouluteDefense,
            SkillID.ShockWave,
            SkillID.TauntingGuard,
            SkillID.TerminateLight,
            SkillID.HolySeal,
        ],
    };
    public PlayerInfoStructure Mariya = new PlayerInfoStructure()
    {
        CharacterName = "Mariya",
        PassiveName = "治愈",
        PassiveDescription = $"自己回合结束时：回复最低生命队友{4}点。",
        LifeMax = 45,
        Power = 9,
        Survivability = 10,
        Speed = 9,
        CharacterScenePath = "res://character/PlayerCharacter/Mariya/Mariya.tscn",
        PortaitPath = "res://asset/PlayerCharater/Mariya/MariyaPortrait.png",
        TakenSkills = [SkillID.MendSlash, SkillID.FinalGuard, SkillID.RebirthPrayer],
        AllSkills =
        [
            SkillID.MendSlash,
            SkillID.FinalGuard,
            SkillID.CrystalGuard,
            SkillID.RebirthPrayer,
            SkillID.Sacrifice,
        ],
    };

    public PlayerInfoStructure Nightingale = new PlayerInfoStructure()
    {
        CharacterName = "Nightingale",
        PassiveName = "夜光",
        PassiveDescription =
            $"队友结束回合时：追击一次:造成{PropertyType.Power.GetDescription()}点伤害。",
        LifeMax = 50,
        Power = 11,
        Survivability = 10,
        Speed = 11,
        CharacterScenePath = "res://character/PlayerCharacter/Nightingale/Nightingale.tscn",
        PortaitPath = "res://asset/PlayerCharater/Nightingale/NightingalePortrait.png",
        TakenSkills = [SkillID.ShadowAmbush, SkillID.VeilStep, SkillID.TempoSurge],
        AllSkills =
        [
            SkillID.ShadowAmbush,
            SkillID.ShadowExecution,
            SkillID.VeilStep,
            SkillID.Swift,
            SkillID.StarWard,
            SkillID.TempoSurge,
            SkillID.LongNight,
        ],
    };
}
