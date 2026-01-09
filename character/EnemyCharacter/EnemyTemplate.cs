using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class EnemyTemplate : Charater
{
	private ProgressBar _lifebar;
	public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
	Label label => field ??= GetNode<Label>("Label");

	public override void _Ready()
	{
		base._Ready();
		IsPlayer = false;
	}

	
	public override void Initialize()
	{
		base.Initialize();
	}
	

	public async override void StartAction()
	{
		base.StartAction();
		Random random = new Random();
		int i = random.Next(0, TakenSkills.Length);
		TakenSkills[i].Effect();
	}
	
	public override void GetHurt(float damage)
	{
		base.GetHurt(damage);
		Tween tween = CreateTween();
		tween.TweenProperty(this,"position",OriginalPosition + 20*Vector2.Right,0.3f);
		tween.TweenProperty(this,"position",OriginalPosition,0.2f);
	}
	
	public override void Dying()
	{
		BattleNode.EnemiesDyingNum++;
		base.Dying();
	}
}
