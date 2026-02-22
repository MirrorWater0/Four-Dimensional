using System;
using Godot;

public abstract partial class EnemyRegedit : Resource
{
    public enum EnemyPositionType
    {
        FrontRow,
        BackRow,
    }

    public EnemyPositionType PType;

    public string CharacterName;

    public string PortaitPath;

    public PackedScene CharacterScene;

    public int PositionIndex;
    public SkillID[] SkillIDs = Array.Empty<SkillID>();
    public int Power;
    public int Survivability;
    public int MaxLife;
    public int Speed;

    public EnemyRegedit() { }

    public string PassiveName;
    public string PassiveDescription;
}

public partial class EvilRegedit : EnemyRegedit
{
    public EvilRegedit()
    {
        CharacterName = "Evil";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Evil.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 50;
        Power = 15;
        Survivability = 15;
        Speed = 13;

        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive, SkillID.EvilTermin];

        PassiveName = "重生律动";
        PassiveDescription =
            $"初始：获得1层{Buff.BuffName.RebirthI.GetDescription()}。\n每行动2次：获得1层{Buff.BuffName.RebirthI.GetDescription()}。\n{Buff.GetBuffEffectText(Buff.BuffName.RebirthI)}";
    }
}

public partial class FearWormRegedit : EnemyRegedit
{
    public FearWormRegedit()
    {
        CharacterName = "恐惧虫";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/FearWorm.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/FearWorm.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 40;
        Power = 15;
        Survivability = 20;
        Speed = 16;

        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormSurvive, SkillID.FearWormTermin];

        PassiveName = "恐惧蜕皮";
        PassiveDescription =
            $"初始：获得1层{Buff.BuffName.DebuffImmunity.GetDescription()}。\n{Buff.GetBuffEffectText(Buff.BuffName.DebuffImmunity)}";
    }
}
