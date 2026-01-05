using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class PlayerCharater : Charater
{
	public Frame SelfFrame;
	public Control SkillButtonControl;
	public List<Skill> UntakeSkills;
	public bool Istest;
	public async override void Initialize()
	{
		base.Initialize();
		IsPlayer = true;
		await Task.Delay(200);//等待CharaterControl执行connnect
	}
	

	public override void StartAction()
	{
		base.StartAction();
		for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
		{
			SkillButtonControl.GetChild<SkillButton>(j).Enable();
			SkillButtonControl.GetChild<Button>(j).GetChild<Label>(0).Modulate = new Color(1, 1, 1, 1f);
			
		}

		BattleNode.RetreatButton.Disabled = false;
	}

	public override void EndAction()
	{
		BattleNode.RetreatButton.Disabled = true;
		DisableSkill();
		base.EndAction();
	}

	public override void GetHurt(float damage)
	{
		base.GetHurt(damage);
		Tween tween = CreateTween();
		tween.TweenProperty(this,"position",OriginalPosition + 20*Vector2.Left,0.3f);
		tween.TweenProperty(this,"position",OriginalPosition,0.2f);
	}

	public override void DisableSkill()
	{
		GD.Print("ButtonNum:",SkillButtonControl.GetChildCount());
		for (int j = 0; j < SkillButtonControl.GetChildCount(); j++)
		{
			SkillButtonControl.GetChild<Button>(j).Disabled = true;
			SkillButtonControl.GetChild<Button>(j).GetChild<Label>(0).Modulate = new Color(1, 1, 1, 0.3f);
		}
	}

	public override void Dying()
	{
		BattleNode.PlayerDyingNum++;
		base.Dying();
	}
	

}
