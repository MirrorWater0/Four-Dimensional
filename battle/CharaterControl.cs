using Godot;
using System;

public partial class CharaterControl : Control
{
	public Battle BattleNode => field??= GetNode<Battle>("/root/Battle");
	public Frame CharaterFrame1 => field??= GetNode<Frame>("frame1");
	public Frame CharaterFrame2 => field??= GetNode<Frame>("frame2");
	public Frame CharaterFrame3 => field??= GetNode<Frame>("frame3");
	public Frame CharaterFrame4 => field??= GetNode<Frame>("frame4");

	public Frame[] CharatersControl => new [] { CharaterFrame1, CharaterFrame2, CharaterFrame3 , CharaterFrame4};
	public async override void _Ready()
	{
		await ToSignal(GetTree().CreateTimer(0.1f), "timeout");//等待battle节点准备角色
		Connect();

	}
	
	public override void _Process(double delta)
	{
	}

	public void Connect()
	{
		for (int i = 0; i < CharatersControl.Length; i++)
		{
			BattleNode.Players[i].SelfFrame = CharatersControl[i];
			var skillButtons = CharatersControl[i].SkillButtonContainer;
			BattleNode.Players[i].SkillButtonControl = skillButtons;
			for (int j = 0; j < skillButtons.GetChildCount(); j++)
			{
				var skillButton = skillButtons.GetChild<SkillButton>(j);
				skillButton.Connect(Button.SignalName.Pressed,Callable.From(BattleNode.Players[i].TakenSkills[j].Effect));
				skillButton.NameLabel.Text = BattleNode.Players[i].TakenSkills[j].SkillName;
			}
		}
	}

	public void DisableAll()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			for (int j = 0; j < GetChild<Frame>(i).SkillButtonContainer.GetChildCount(); j++)
			{
				CharatersControl[i].SkillButtonContainer.GetChild<Button>(j).Disabled = true;
				CharatersControl[i].SkillButtonContainer.GetChild<Button>(j).GetChild<Label>(0).Modulate =
					new Color(1, 1, 1, 0.3f);
			}
		}
	}
}
