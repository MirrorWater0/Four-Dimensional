using Godot;
using System;

public partial class Echo : PlayerCharater
{
	public override PackedScene CharaterScene { get; set; } = ChoseCharater._Echo;
	Label label => field ??= GetNode<Label>("Label");
	public override string CharaterName { get; set; } = "Echo";

	public override void _Ready()
	{
		if(Istest) test();
		base._Ready();
		label.Text = PositionIndex.ToString();
		
	}

	public void test()
	{
		TakenSkills = new Skill[] { new FollowingLight(this),new SacredOnslaught(this),new Combo(this) };
		BattleLifemax = 2000;
		BattlePower = 1300;
		BattleSurvivability = 1400;
		Speed = 20;
	}

	public override void StartAction()
	{
		base.StartAction();
	}
}
