using Godot;
using System;

public partial class KasiyaSpecialSkill : Node
{
    
}

public class TerminateLight : Skill
{
    public TerminateLight(Charater owner) : base(SkillTypes.Special, owner)
    {
        OwnerCharater = owner;
    }

    public override string SkillName { get; set; } = "终末之光";

    public override void Effect()
    {
        base.Effect();
        Attack1(10 + OwnerCharater.BattlePower);
        OwnerCharater.EndAction();
    }
}