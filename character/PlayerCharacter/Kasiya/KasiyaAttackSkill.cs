using Godot;
using System;

public partial class KasiyaAttackSkill
{

}

public partial class Determination : Skill
{
    public Determination(Charater owner): base(SkillTypes.Attack, owner) 
    {
        OwnerCharater = owner;
    }

    public override string SkillName { get; set; } = "剑意已决";
    public override void Effect()
    {
        base.Effect();
        Attack1(6);
        OwnerCharater.HurtBuffs.Add(new Buff(OwnerCharater,[],Buff.BuffType.DamageImmune,1));
        OwnerCharater.EndAction();
    }
}

public partial class Smite : Skill
{
    public Smite(Charater owner):base(Skill.SkillTypes.Attack,owner){}
    public override string SkillName { get; set; } = "绝域剑杀";

    public override void Effect()
    {
        base.Effect();
        DescendingProperties(Chosetarget1()[0],PropertyType.Survivalibility,2);
        Attack1(3);
        OwnerCharater.EndAction();
    }
    
}