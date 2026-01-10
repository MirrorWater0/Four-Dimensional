using Godot;
using System;
using System.Threading.Tasks;

public partial class KasiyaAttackSkill
{

}

public partial class Determination : Skill
{
    public Determination(Charater owner): base(SkillTypes.Attack, owner)
    {
        OwnerCharater = owner;
        Description = "发动强力攻击，造成6倍基础伤害+战斗力的伤害，并获得1次伤害免疫。";
    }

    public override string SkillName { get; set; } = "剑意已决";
    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(6);
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune,OwnerCharater,2);
        OwnerCharater.EndAction();
    }
}

public partial class Smite : Skill
{
    public Smite(Charater owner):base(Skill.SkillTypes.Attack,owner)
    {
        Description = "降低目标2点生存能力，然后发动攻击，造成3倍基础伤害+战斗力的伤害。";
    }
    public override string SkillName { get; set; } = "绝域剑杀";

    public override async Task Effect()
    {
        await base.Effect();
        await DescendingProperties(Chosetarget1()[0],PropertyType.Survivalibility,2);
        await Attack1(3);
        OwnerCharater.EndAction();
    }
    
}


public partial class Cast : Skill
{
    public Cast(Charater owner):base(Skill.SkillTypes.Attack,owner)
    {
        Description = "获得等于攻击伤害的格挡";

    }

    public override string SkillName { get; set; } = "投射";
    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(10);
        OwnerCharater.UpdataBlock(10+OwnerCharater.BattlePower);
        OwnerCharater.EndAction();
    }
}