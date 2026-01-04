using Godot;
using System;

public partial class Smite : Skill
{
    public Smite(Charater owner):base(Skill.SkillTypes.Attack,owner){}
    public override string SkillName { get; set; } = "绝域剑杀";

    public override void Effect()
    {
        base.Effect();
        DescendingProperties(Chosetarget1()[0],PropertyType.Defence,0,0.3f);
        Attack1(3);
        OwnerCharater.EndAction();
    }
    
}
