using Godot;
using System;
using System.Threading.Tasks;

public partial class KasiyaDeSurviveSkill
{
}

public partial class ReNewedSpirit : Skill
{
    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit() : base(SkillTypes.Defence)
    {
        Description = "提升自身5点战斗力和5点生存能力。";
    }

    public async override Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater,PropertyType.Power,5);
        IncreaseProperties(OwnerCharater,PropertyType.Survivalibility,5);
        OwnerCharater.UpdataBlock(OwnerCharater.BattleSurvivability);
        OwnerCharater.EndAction();
    }
}
