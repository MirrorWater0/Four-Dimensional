using Godot;
using System;
using System.Threading.Tasks;

public partial class ReNewedSpirit : Skill
{
    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit(Charater owner) : base(SkillTypes.Special, owner)
    {
        OwnerCharater = owner;
    }

    public async override void Effect()
    {
        base.Effect();
        IncreaseProperties(OwnerCharater,PropertyType.Power,500,1);
        IncreaseProperties(OwnerCharater,PropertyType.Defence,500,1);
        
        OwnerCharater.EndAction();
        await Task.Delay(1000);
    }
}
