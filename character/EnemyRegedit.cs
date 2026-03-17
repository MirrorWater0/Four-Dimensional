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
        MaxLife = 60;
        Power = 12;
        Survivability = 13;
        Speed = 8;

        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive, SkillID.EvilTermin];

        PassiveName = "重生律动";
        PassiveDescription =
            $"初始：获得{1}点能量。获得一层{Buff.BuffName.RebirthI.GetDescription()}。\n每行动{3}次：获得1层{Buff.BuffName.RebirthI.GetDescription()}。";
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
        MaxLife = 70;
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
        MaxLife = 90;
        Power = 13;
        Survivability = 14;
        Speed = 9;

        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSurvive, SkillID.ArmonSpecial];

        PassiveName = "矩阵核心";
        PassiveDescription =
            $"回合开始时全阵获得格挡。" + $"\n回合结束时：全阵获得格挡，数值为自身生存。";
    }
}

public partial class AlienBodyRegedit : EnemyRegedit
{
    public AlienBodyRegedit()
    {
        CharacterName = "AlienBody";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/AlienBody.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AlienBody.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 70;
        Power = 10;
        Survivability = 11;
        Speed = 8;

        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive, SkillID.AlienBodySpecial];

        PassiveName = "寄生馈赠";
        PassiveDescription = "回合结束时：上一位非濒死队友获得3点力量。";
    }
}

public partial class ArroganceRegedit : EnemyRegedit
{
    public ArroganceRegedit()
    {
        CharacterName = "Arrogance";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Arrogance.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Arrogance.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 195;
        Power = 25;
        Survivability = 30;
        Speed = 50;

        SkillIDs = [SkillID.ArroganceAttack, SkillID.ArroganceSurvive, SkillID.ArroganceSpecial];

        PassiveName = "傲慢";
        PassiveDescription = $"战斗开始时：获得{2}层{Buff.BuffName.Stun.GetDescription()}。";
    }
}

public enum EnemiesEnum
{
    Evil,
    FearWorm,
    Armon,
    Arrogance,
    AlienBody,
}
