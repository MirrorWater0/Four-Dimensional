using System;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl : Control
{
    public Battle BattleNode => field ??= GetParent<Battle>();
    public Frame CharaterFrame1 => field ??= GetNode<Frame>("frame1");
    public Frame CharaterFrame2 => field ??= GetNode<Frame>("frame2");
    public Frame CharaterFrame3 => field ??= GetNode<Frame>("frame3");
    public Frame CharaterFrame4 => field ??= GetNode<Frame>("frame4");

    public Frame[] CharactersControl =>
        new[] { CharaterFrame1, CharaterFrame2, CharaterFrame3, CharaterFrame4 };

    public override async void _Ready()
    {
    }

    public void Connect()
    {
        for (int i = 0; i < CharactersControl.Length && i < BattleNode.PlayersList.Count; i++)
        {
            BattleNode.PlayersList[i].SelfFrame = CharactersControl[i];
            var skillButtons = CharactersControl[i].SkillButtonContainer;
            CharactersControl[i].NameLabel.Text = BattleNode.PlayersList[i].CharacterName;
            BattleNode.PlayersList[i].SkillButtonControl = skillButtons;
            for (int j = 0; j < skillButtons.GetChildCount(); j++)
            {
                var skillButton = skillButtons.GetChild<SkillButton>(j);
                var skill = BattleNode.PlayersList[i].Skills[j];

                skillButton.SelfSkill = skill;

                // Create a synchronous wrapper for the async Effect method
                async void OnSkillButtonPressed()
                {
                    await skillButton.SelfSkill.Effect();
                    skillButton.SelfSkill.OwnerCharater.EndAction();
                }
                skillButton.Connect(Button.SignalName.Pressed, Callable.From(OnSkillButtonPressed));
            }
        }
    }

    public void DisableAll()
    {
        for (int i = 0; i < GetChildCount(); i++)
        {
            for (int j = 0; j < GetChild<Frame>(i).SkillButtonContainer.GetChildCount(); j++)
            {
                var skillButton = CharactersControl[i].SkillButtonContainer.GetChild<SkillButton>(j);
                skillButton.Disabled = true;
                skillButton.Modulate = SkillButton.DisabledModulate;
            }
        }
    }
}
