using System;
using System.Threading.Tasks;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    Label label => field ??= GetNode<Label>("Label");
    public override PackedScene CharaterScene { get; set; } = StartInterface._Kasiya;
    public override string CharaterName { get; set; } = "Kasiya";

    public override void Initialize()
    {
        base.Initialize();

        BattleNode.UsedSkills.ItemAdded += item =>
        {
            Passive(item);
        };
    }

    public override void Passive(Skill skill)
    {
        if (skill.OwnerCharater == this & skill.SkillType == Skill.SkillTypes.Survive)
        {
            Recovery(20);
        }
    }

}
