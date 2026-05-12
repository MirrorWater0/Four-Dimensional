public partial class NightingaleSpecialSkill { }

public partial class NightingaleEnergy : Skill
{
    private const int EnergyGain = 1;

    public NightingaleEnergy()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "\u591c\u606f";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}

public partial class TempoSurge : Skill
{
    private const int SpeedGain = 3;
    private const int PowerGain = 4;
    public override bool ExhaustsAfterUse => base.ExhaustsAfterUse;

    public TempoSurge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "疾奏";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Speed, SpeedGain),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            CarryStep(target: TargetReference.Previous, skillIndex: 2)
        );
    }
}

public partial class LongNight : Skill
{
    private const int SpeedLoss = 3;

    public override string SkillName { get; set; } = "长夜";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

    public LongNight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CarryStep(target: TargetReference.Previous, skillIndex: 3),
            CarryStep(target: TargetReference.Next, skillIndex: 3)
        );
    }
}

public partial class RequiemBloom : Skill
{
    private const int PowerGain = 3;
    public override bool ExhaustsAfterUse => true;

    private const int RebirthStacks = 1;
    private const int ExtraTurnStacks = 1;

    public RequiemBloom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "安魂花";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ConditionStep(
                condition: () => GetAllAllyWithOrder(dyingFilter: true).Length >= 3,
                conditionDescription: "己方有3个角色存活",
                onPassSteps:
                [
                    ApplyBuffFriendly(
                        buffName: Buff.BuffName.RebirthI,
                        stacks: RebirthStacks,
                        target: TargetReference.Self
                    ),
                ]
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraTurn,
                stacks: ExtraTurnStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class CurtainCallMoment : Skill
{
    private const int WeakenStacks = 2;
    private const int InvisibleStacks = 5;
    private const int ExtraTurnStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public CurtainCallMoment()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "落幕时刻";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargets(0)
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraTurn,
                stacks: ExtraTurnStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class SunMoonCycle : Skill
{
    private const int DrawReserveGain = 4;

    public SunMoonCycle()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "日月轮回";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, DrawReserveStep(DrawReserveGain));
    }
}

public partial class ShadowForm : Skill
{
    private const int ShadowStacks = 2;

    public ShadowForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "暗影形态";
    public override int EnergyCost => 5;
    public override bool ExhaustsAfterUse => true;
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: 5,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Shadow,
                stacks: ShadowStacks,
                target: TargetReference.Self
            )
        );
    }
}
