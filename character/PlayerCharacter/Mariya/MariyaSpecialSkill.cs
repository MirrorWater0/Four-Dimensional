using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MariyaSpecialSkill { }

public partial class RebirthPrayer : Skill
{
    private const int BaseRebirthHeal = 22;
    private const string SharedTargetKey = "治疗目标";

    public RebirthPrayer()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "复苏祷告";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.FrontMost,
                preferNonFull: true,
                rebirth: true,
                storeAs: SharedTargetKey
            ),
            BlockStep(SharedTargetKey, baseBlock: 10),
            ModifyPropertyStep(SharedTargetKey, type: PropertyType.MaxLife, value: 10)
        );
    }
}

public partial class Sacrifice : Skill
{
    int basisDamage = 25;
    int allyHurt = 15;
    int DeMax = 15;
    public override string SkillName { get; set; } = "献祭";
    public override int EnergyCost => 2;

    public Sacrifice()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HurtFriendly(allyHurt, all: true),
            ModifyPropertyStep(
                type: PropertyType.MaxLife,
                value: -DeMax,
                target: TargetReference.All
            ),
            AoeDamageStep(baseDamage: basisDamage, powerMultiplier: 2, target: HostileTargets(0))
        );
    }
}

public partial class RearlineRevival : Skill
{
    private const int BaseRebirthHeal = 22;
    private const int TargetCount = 2;

    public RearlineRevival()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "死者苏生";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.BackMost,
                preferNonFull: true,
                rebirth: true,
                repeatCount: TargetCount
            )
        );
    }
}

public partial class GroupHealing : Skill
{
    private const int BaseHeal = 15;

    public GroupHealing()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣光沐浴";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(type: PropertyType.MaxLife, value: 15, target: TargetReference.All),
            HealStep(
                baseHeal: BaseHeal,
                target: TargetReference.All,
                preferNonFull: false,
                includeSummonsWhenAll: false
            )
        );
    }
}

public partial class Ragnarok : Skill
{
    private const int PowerGain = 2;
    private const int DivinityStacks = 2;
    public override bool ExhaustsAfterUse => true;

    public Ragnarok()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "诸神黄昏";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class SanctuaryForm : Skill
{
    private const int SanctuaryStacks = 1;

    public SanctuaryForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣域形态";
    public override int EnergyCost => 4;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(baseHeal: 20, target: TargetReference.All, preferNonFull: true, rebirth: true),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Sanctuary,
                stacks: SanctuaryStacks,
                target: TargetReference.All
            )
        );
    }
}
