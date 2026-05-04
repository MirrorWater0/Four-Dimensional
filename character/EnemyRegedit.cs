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
    BlackHawk,
    Inexorability,
}
