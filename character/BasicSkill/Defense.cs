using Godot;
using System;
using System.Threading.Tasks;

public partial class Defense : Skill
{
    public Defense():base(Skill.SkillTypes.Defence)
    {
        Description = "获得护盾，护盾值为2倍战斗生存能力。";
    }
    public override string SkillName { set; get; } = "坚不可摧";

    public async override Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(2*OwnerCharater.BattleSurvivability);
        OwnerCharater.EndAction();
    }
}
