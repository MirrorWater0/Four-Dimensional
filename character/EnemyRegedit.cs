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
    public int SpecialIntentThreshold = 3;

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
        MaxLife = 70;
        Power = 13;
        Survivability = 13;
        Speed = 8;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive, SkillID.EvilTermin];

        PassiveName = global::Evil.PassiveNameText;
        PassiveDescription = global::Evil.PassiveBaseDescriptionText;
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
        Speed = 12;
        SpecialIntentThreshold = 2;

        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormSurvive, SkillID.FearWormTermin];

        PassiveName = global::FearWorm.PassiveNameText;
        PassiveDescription = global::FearWorm.PassiveDescriptionText;
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
        MaxLife = 75;
        Power = 10;
        Survivability = 11;
        Speed = 9;
        SpecialIntentThreshold = 2;

        SkillIDs = [SkillID.ArmonAttack, SkillID.ArmonSurvive, SkillID.ArmonSpecial];

        PassiveName = global::Armon.PassiveNameText;
        PassiveDescription = global::Armon.PassiveDescriptionText;
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
        MaxLife = 60;
        Power = 10;
        Survivability = 11;
        Speed = 8;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive, SkillID.AlienBodySpecial];

        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
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
        MaxLife = 210;
        Power = 24;
        Survivability = 30;
        Speed = 50;
        SpecialIntentThreshold = 4;

        SkillIDs = [SkillID.ArroganceAttack, SkillID.ArroganceSurvive, SkillID.ArroganceSpecial];

        PassiveName = global::Arrogance.PassiveNameText;
        PassiveDescription = global::Arrogance.PassiveDescriptionText;
    }
}

public partial class RedHuskRegedit : EnemyRegedit
{
    public RedHuskRegedit()
    {
        CharacterName = "RedHusk";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/RedHusk.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/RedHusk.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 90;
        Power = 14;
        Survivability = 13;
        Speed = 10;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.RedHuskAttack, SkillID.RedHuskSurvive, SkillID.RedHuskSpecial];

        PassiveName = global::RedHusk.PassiveNameText;
        PassiveDescription = global::RedHusk.PassiveDescriptionText;
    }
}

public partial class WarRegedit : EnemyRegedit
{
    public WarRegedit()
    {
        CharacterName = "War";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/War.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/War.tscn");

        MaxLife = 325;
        Power = 20;
        Survivability = 20;
        Speed = 15;
        SpecialIntentThreshold = 2;

        SkillIDs = [SkillID.WarAttack, SkillID.WarSurvive, SkillID.WarSpecial];

        PassiveName = global::War.PassiveNameText;
        PassiveDescription = global::War.PassiveDescriptionText;
    }
}

public partial class FerociouessRegedit : EnemyRegedit
{
    public FerociouessRegedit()
    {
        CharacterName = "Ferociouess";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Ferociouess.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Ferociouess.tscn");

        MaxLife = 60;
        Power = 10;
        Survivability = 14;
        Speed = 10;
        SpecialIntentThreshold = 2;

        SkillIDs =
        [
            SkillID.FerociouessAttack,
            SkillID.FerociouessSurvive,
            SkillID.FerociouessSpecial,
        ];

        PassiveName = global::Ferociouess.PassiveNameText;
        PassiveDescription = global::Ferociouess.PassiveDescriptionText;
    }
}

public partial class TurbineRegedit : EnemyRegedit
{
    public TurbineRegedit()
    {
        CharacterName = "Turbine";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Turbine.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Turbine.tscn");

        MaxLife = 85;
        Power = 12;
        Survivability = 12;
        Speed = 9;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.TurbineAttack, SkillID.TurbineSurvive, SkillID.TurbineSpecial];

        PassiveName = global::Turbine.PassiveNameText;
        PassiveDescription = global::Turbine.PassiveDescriptionText;
    }
}

public enum EnemiesEnum
{
    Evil,
    FearWorm,
    Armon,
    Arrogance,
    AlienBody,
    RedHusk,
    War,
    Ferociouess,
    Turbine,
}
