using System;
using Godot;

public partial class Demon : EnemyCharacter
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    public override void Initialize()
    {
        BattleLifemax = 50;
        BattlePower = 15;
        BattleSurvivability = 15;
        Speed = 13;
        Skills = new Skill[] { new Attack(), new Combo() };

        base.Initialize();
        DyingBuff.BuffAdd(Buff.BuffName.Rebirth, this, 1);
        UpdataEnerge(1);
    }
}
