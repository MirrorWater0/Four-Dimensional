using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class Attack : Skill
{
    private const int HitDamage = 5;

    public Attack() : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }


    public override string SkillName { set; get; } = "流影二段";

    public async override Task Effect()
    {
        await base.Effect();
        await Attack2(HitDamage);
    }

    public override void UpdateDescription()
    {
        SetDescriptionText($"对前方敌人发动二段连续攻击，每段造成{HitDamage}点伤害。");
    }
}
