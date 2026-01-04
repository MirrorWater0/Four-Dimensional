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
		Power = 20;
		Survivability = 20;
		Speed = 18;
		Skills = new Skill[] {new Attack(this),new Combo(this) };
		DyingBuffs.Add(new Buff(this,new Charater[]{this},Buff.BuffType.Rebirth,1));
		base.Initialize();
    }

	

}
