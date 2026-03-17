using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MariyaSpecialSkill { }

public partial class RebirthPrayer : Skill
{
    private const int EnergyCost = 2;
    private const int BaseRebirthHeal = 12;
    private const int GainEnergy = 1;
    private const string SharedTargetKey = "rebirth_target";

    public RebirthPrayer()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "复苏祷告";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                HealFriendlyAbsolute(
                    baseHeal: BaseRebirthHeal,
                    survivabilityMultiplier: 0,
                    selector: AbsoluteFriendlySelector.FrontMost,
                    preferNonFull: true,
                    rebirth: true,
                    targetKey: SharedTargetKey
                ),
                FriendlyEnergyRelativeStep(
                    delta: GainEnergy,
                    relative: 0,
                    dyingFilter: true,
                    targetKey: SharedTargetKey
                )
            )
        );
    }
}

public partial class Sacrifice : Skill
{
    int basisDamage = 30;
    int allyHurt = 15;
    int DeMax = 15;
    int energyCost = 2;
    public override string SkillName { get; set; } = "献祭";

    public Sacrifice()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                energyCost,
                null,
                null,
                HurtFriendly(allyHurt, all: true),
                ModifyPropertyAbsoluteStep(
                    type: PropertyType.MaxLife,
                    value: -DeMax,
                    selector: PropertyAbsoluteSelector.All,
                    dyingFilter: false
                ),
                AoeDamageStep(baseDamage: basisDamage, powerMultiplier: 1, maxTargets: 0)
            )
        );
    }
}

