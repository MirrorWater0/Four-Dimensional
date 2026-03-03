using System;
using Godot;

public abstract partial class EnemyRegedit
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

    public EnemyRegedit GetRegedit() => (EnemyRegedit)MemberwiseClone();
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
        Power = 12;
        Survivability = 13;
        Speed = 8;

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
        CharacterName = "FearWorm";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/FearWorm.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/FearWorm.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 60;
        Power = 15;
        Survivability = 9;
        Speed = 13;

        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormSurvive, SkillID.FearWormTermin];

        PassiveName = "蜕皮";
        PassiveDescription =
            $"初始：获得1层{Buff.BuffName.DebuffImmunity.GetDescription()}。"
            + $"\n回合结束时：获得{2}点力量。";
    }
}

public partial class ArmonRegedit : EnemyRegedit
{
    public ArmonRegedit()
    {
        CharacterName = "Armon";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Armon.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Armon.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 80;
        Power = 13;
        Survivability = 13;
        Speed = 9;

        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSurvive, SkillID.ArmonSpecial];

        PassiveName = "矩阵核心";
        PassiveDescription =
            $"回合开始时全阵获得格挡。" + $"\n回合结束时：全阵获得格挡，数值为自身生存。";
    }
}

public enum EnemiesEnum
{
    Evil,
    FearWorm,
    Armon,
}