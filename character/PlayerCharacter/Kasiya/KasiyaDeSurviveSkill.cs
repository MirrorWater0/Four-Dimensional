using Godot;
using System;

public partial class KasiyaDeSurviveSkill
{
}

public partial class ReNewedSpirit : Skill
{
    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit(Charater owner) : base(SkillTypes.Defence, owner)
    {
        OwnerCharater = owner;
        Description = "提升自身2点战斗力和2点生存能力。";
    }

    public async override void Effect()
    {
        base.Effect();
        IncreaseProperties(OwnerCharater,PropertyType.Power,2);
        IncreaseProperties(OwnerCharater,PropertyType.Survivalibility,2);
        
        OwnerCharater.EndAction();
    }
}
