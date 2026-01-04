using Godot;
using System;

public partial class Defense : Skill
{
    public Defense(Charater owner):base(Skill.SkillTypes.Defence,owner){}
    public override string SkillName { set; get; } = "坚不可摧";

    public async override void Effect()
    {
        base.Effect();
        OwnerCharater.UpdataBlock(2*OwnerCharater.BattleSurvivability);
        OwnerCharater.EndAction();
    }
}
