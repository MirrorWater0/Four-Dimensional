using Godot;
using System;

public partial class Demon : EnemyTemplate
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


    public override void Initialize()
    {
		Lifemax = 50;
		Power = 15;
		Survivability = 15;
		Speed = 18;
		TakenSkills = new Skill[] {new Attack(this),new Combo(this) };
		
		base.Initialize();
		DyingBuff.BuffAdd(Buff.BuffName.Rebirth,this,1);
		UpdataEnerge(1);
    }

	

}
