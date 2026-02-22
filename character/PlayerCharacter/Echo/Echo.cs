using System;
using Godot;

public partial class Echo : PlayerCharacter
{
    public override PackedScene CharaterScene { get; set; } = StartInterface._Echo;
    Label label => field ??= GetNode<Label>("Label");
    public override string CharacterName { get; set; } = "Echo";
    public override string PassiveName => "余响";
    public override string PassiveDescription =>
        $"使用生存技能时：获得{1}点能量。" + $"使用非生存技能时：获得{2}点力量。";

    public override void _Ready()
    {
        base._Ready();
        label.Text = PositionIndex.ToString();
    }

    public override void Initialize()
    {
        base.Initialize();
        BattleNode.UsedSkills.ItemAdded += Passive;
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
        if (skill.OwnerCharater != this)
            return;

        if (skill.SkillType != Skill.SkillTypes.Survive)
        {
            skill.IncreaseProperties(this, Skill.PropertyType.Power, 2);
            return;
        }

        UpdataEnergy(1);
    }
}
