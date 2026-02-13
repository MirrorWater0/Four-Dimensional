using System;
using Godot;

public partial class Evil : EnemyCharacter
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    public override void Initialize()
    {
        BattleLifemax = 50;
        BattlePower = 15;
        BattleSurvivability = 15;
        Speed = 13;
        Skills = new Skill[] { new Attack(), new Combo() };

        base.Initialize();
        DyingBuff.BuffAdd(Buff.BuffName.Rebirth, this, 1);
        UpdataEnergy(1);
    }
}
