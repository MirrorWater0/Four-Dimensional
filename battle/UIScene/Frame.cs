using Godot;
using System;

public partial class Frame : TextureRect
{
	public Label NameLabel => field??=GetNode<Label>("Name");
	public TextureRect Potrait => field ??= GetNode("Texture") as TextureRect;
	public Control SkillButtonContainer => field ??= GetNode("SkillControl") as Control;
	public Label CombNum => field ??= GetNode("ComboNum") as Label;
	public Label CarryNum => field ??= GetNode("CarryNum") as Label;
	public Label Power => field ??= GetNode("PowerRate") as Label;
	public Label Defence => field ??= GetNode("DefenceRate") as Label;
	public Area2D Detector1 => field ??= GetNode("SkillControl/skill1/Detector") as Area2D;
	public Area2D Detector2 => field ??= GetNode("SkillControl/skill2/Detector") as Area2D;
	public Area2D Detector3 => field ??= GetNode("SkillControl/skill3/Detector") as Area2D;
	public Area2D Detector4 => field ??= GetNode("SkillControl/skill4/Detector") as Area2D;
	public SkillButton SkillButton1 => field ??= GetNode("SkillControl/skill1") as SkillButton;
	public SkillButton SkillButton2 => field ??= GetNode("SkillControl/skill2") as SkillButton;
	public SkillButton SkillButton3 => field ??= GetNode("SkillControl/skill3") as SkillButton;
	public override void _Ready()
	{
		for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++) //初始化每个按钮的PositionIndex
		{
			var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
			skillButton.PositionIndex = skillButton.Position;
		}
	}

    public override void _Process(double delta)
    {
        
    }

	public void SortButtons()
	{
		for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
		{
			var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
			CreateTween().TweenProperty(skillButton, "position", skillButton.PositionIndex,0.3f);
		}
	}
}
