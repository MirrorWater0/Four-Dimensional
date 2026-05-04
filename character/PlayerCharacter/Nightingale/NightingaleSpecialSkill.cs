public partial class NightingaleSpecialSkill { }

public partial class TempoSurge : Skill
{
    private const int SpeedGain = 3;
    private const int PowerGain = 4;
    private const int EnergyCost = 3;

    public TempoSurge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "疾奏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Speed, SpeedGain),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                CarryStep(target: RelativeTarget(-1), skillIndex: 1)
            )
        );
    }
}

public partial class LongNight : Skill
{
    private const int SurvivabilityLoss = 3;
    private const int SpeedLoss = 3;
    private const int PrimaryEnergyCost = 2;
    private const int SecondaryEnergyCost = 1;
    private const int PowerGain = 5;

    public override string SkillName { get; set; } = "长夜";

    public LongNight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                PrimaryEnergyCost,
                null,
                null,
                CarryStep(target: RelativeTarget(-1), skillIndex: 2),
                CarryStep(target: RelativeTarget(1), skillIndex: 2),
                ModifyPropertyStep(PropertyType.Survivability, -SurvivabilityLoss),
                ModifyPropertyStep(PropertyType.Speed, -SpeedLoss)
            ),
            EnergyTimesGateStep(
                SecondaryEnergyCost,
                null,
                null,
                ModifyPropertyStep(PropertyType.Power, PowerGain)
            )
        );
    }
}

public partial class RequiemBloom : Skill
{
    private const int PowerGain = 3;
    private const int SurvivabilityLoss = 4;
    private const int RebirthStacks = 1;
    private const int ExtraTurnStacks = 1;

    public RequiemBloom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "安魂花";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, -SurvivabilityLoss),
            ConditionStep(
                condition: () => GetAllAllyWithOrder(dyingFilter: true).Length >= 3,
                conditionDescription: "己方有3个角色存活",
                onPassSteps:
                [
                    ApplyBuffFriendly(
                        buffName: Buff.BuffName.RebirthI,
                        stacks: RebirthStacks,
                        target: RelativeTarget(0)
                    ),
                ]
            ),
            EnergyTimesGateStep(
                3,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.ExtraTurn,
                    stacks: ExtraTurnStacks,
                    target: RelativeTarget(0)
                )
            )
        );
    }
}

public partial class CurtainCallMoment : Skill
{
    private const int WeakenStacks = 2;
    private const int InvisibleStacks = 5;
    private const int EnergyCost = 3;
    private const int ExtraTurnStacks = 1;

    public CurtainCallMoment()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "落幕时刻";

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
                target: RelativeTarget(0)
            ),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.ExtraTurn,
                    stacks: ExtraTurnStacks,
                    target: RelativeTarget(1)
                )
            )
        );
    }
}

public partial class ShadowForm : Skill
{
    private const int EnergyCost = 6;
    private const int ShadowStacks = 2;

    public ShadowForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "暗影形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.Invisible,
                    stacks: 5,
                    target: RelativeTarget(0)
                ),
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.Shadow,
                    stacks: ShadowStacks,
                    target: RelativeTarget(0)
                )
            )
        );
    }
}
