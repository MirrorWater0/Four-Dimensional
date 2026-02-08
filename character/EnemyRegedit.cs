using System;
using Godot;

[GlobalClass]
public abstract partial class EnemyRegedit : Resource
{
    public enum EnemyType
    {
        FrontRow,
        BackRow,
    }

    [Export]
    public EnemyType Type;

    [Export]
    public string CharacterName;

    [Export]
    public string PortaitPath;

    [Export]
    public PackedScene CharacterScene;

    public int PositionIndex;
    public EnemyRegedit() { }
}

[GlobalClass]
public partial class EvilRegedit : EnemyRegedit
{
    public EvilRegedit()
    {
        CharacterName = "Evil";
        Type = EnemyType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Evil.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn");
    }
}
