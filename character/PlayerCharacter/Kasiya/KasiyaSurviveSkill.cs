using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaDeSurviveSkill { }

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 5;
    private const int SurvivabilityGain = 5;

    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit()
        : base(SkillTypes.Defence)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGain);
        IncreaseProperties(OwnerCharater, PropertyType.Survivalibility, SurvivabilityGain);
        OwnerCharater.UpdataBlock(OwnerCharater.BattleSurvivability);
    }

    public override void UpdateDescription()
    {
        int block = Math.Clamp(OwnerSurvivability + SurvivabilityGain, 0, 9999);
        SetDescriptionLines(
            $"{}加成触发1次（+{PowerGain}），生存加成触发1次（+{SurvivabilityGain}），并获得{block}点格挡。"
        );
    }
}
