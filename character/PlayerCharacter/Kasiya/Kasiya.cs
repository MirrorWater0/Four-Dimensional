using System;
using System.Threading.Tasks;
using Godot;

public partial class Kasiya : PlayerCharacter
{
    Label label => field ??= GetNode<Label>("Label");
    public override PackedScene CharaterScene { get; set; } = StartInterface._Kasiya;
    public override string CharacterName { get; set; } = "Kasiya";

    public override void Initialize()
    {
        base.Initialize();

        BattleNode.UsedSkills.ItemAdded += Passive;
    }

    public override void Passive(Skill skill)
    {
        if (
            skill.OwnerCharater != this
            && skill.SkillType == Skill.SkillTypes.Attack
            && skill.OwnerCharater.IsPlayer
        )
        {
            Recover(0);
        }
    }
}
