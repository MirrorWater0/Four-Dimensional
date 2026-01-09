using Godot;
using System;

public partial class Defense : Skill
{
    public Defense(Charater owner):base(Skill.SkillTypes.Defence,owner)
    {
        Description = "获得护盾，护盾值为2倍战斗生存能力。";
    }
    public override string SkillName { set; get; } = "坚不可摧";

    public async override void Effect()
    {
        base.Effect();
        OwnerCharater.UpdataBlock(2*OwnerCharater.BattleSurvivability);
        OwnerCharater.EndAction();
    }
}
