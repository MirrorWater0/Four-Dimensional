using System;
using Godot;

public abstract partial class EnemyRegedit : Resource
{
    public enum EnemyType
    {
        FrontRow,
        BackRow,
    }

    public EnemyType Type;

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
}

public partial class EvilRegedit : EnemyRegedit
{
    public EvilRegedit()
    {
        CharacterName = "Evil";
        Type = EnemyType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Evil.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn");

        // Base stats (used for preview / original properties)
        MaxLife = 50;
        Power = 15;
        Survivability = 15;
        Speed = 13;

        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive, SkillID.EvilTermin];
    }
}
