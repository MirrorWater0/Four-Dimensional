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
        Description = "发动终极攻击，造成（10+战斗力）倍基础伤害的毁灭性打击。";
    }

    public override string SkillName { get; set; } = "终末之光";

    public override void Effect()
    {
        base.Effect();
        Attack1(10 + OwnerCharater.BattlePower);
        OwnerCharater.EndAction();
    }
}