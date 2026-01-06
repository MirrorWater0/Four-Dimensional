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
    }

    public async override void Effect()
    {
        base.Effect();
        IncreaseProperties(OwnerCharater,PropertyType.Power,1);
        IncreaseProperties(OwnerCharater,PropertyType.Survivalibility,1);
        
        OwnerCharater.EndAction();
    }
}
