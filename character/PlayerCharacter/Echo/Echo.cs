using System;
using Godot;

public partial class Echo : PlayerCharacter
{
    public override PackedScene CharaterScene { get; set; } = StartInterface._Echo;
    Label label => field ??= GetNode<Label>("Label");
    public override string CharacterName { get; set; } = "Echo";

    public override void _Ready()
    {
        base._Ready();
        label.Text = PositionIndex.ToString();
        SelfFrame.SkillButton2.Pressed += () =>
        {
            Passive(null);
        };
    }

    public override void StartAction()
    {
        base.StartAction();
    }

    public override void EndAction()
    {
        base.EndAction();
    }

    public override void Passive(Skill skill)
    {
        UpdataEnergy(1);
    }
}
