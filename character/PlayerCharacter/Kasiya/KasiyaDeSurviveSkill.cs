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
        Description = "提升自身2点战斗力和2点生存能力。";
    }

    public async override Task Effect()
    {
        await base.Effect();
        await IncreaseProperties(OwnerCharater,PropertyType.Power,2);
        await IncreaseProperties(OwnerCharater,PropertyType.Survivalibility,2);
        
        OwnerCharater.EndAction();
    }
}
