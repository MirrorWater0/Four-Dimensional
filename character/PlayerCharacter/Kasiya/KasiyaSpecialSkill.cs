using Godot;
using System;
using System.Threading.Tasks;

public partial class KasiyaSpecialSkill : Node
{
    
}

public class TerminateLight : Skill
{
    public TerminateLight() : base(SkillTypes.Special)
    {
        Description = "发动终极攻击，造成（10+战斗力）倍基础伤害的毁灭性打击。";
    }

    public override string SkillName { get; set; } = "终末之光";

    public async override Task Effect()
    {
        await base.Effect();
        await Attack1(10 + OwnerCharater.BattlePower);
    }
}