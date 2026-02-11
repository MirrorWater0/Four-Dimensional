using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音障防护";

    public SoundBarrier()
        : base(SkillTypes.Defence)
    {
        Description = "创造音波屏障，提升自身3点战斗力和4点生存能力，并获得格挡。";
    }

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Power, 3);
        IncreaseProperties(OwnerCharater, PropertyType.Survivalibility, 4);
        OwnerCharater.UpdataBlock(OwnerCharater.BattleSurvivability);
    }
}
