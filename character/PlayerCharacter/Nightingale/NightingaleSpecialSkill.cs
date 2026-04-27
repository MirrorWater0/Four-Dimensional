using System.Threading.Tasks;

public partial class NightingaleSpecialSkill { }

public partial class TempoSurge : Skill
{
    private const int SpeedGain = 3;
    int PowerGain = 4;
    int cost = 3;

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
                cost,
                null,
                null,
                CarryStep(target: RelativeTarget(-1), skillIndex: 1)
            )
        );
    }
}

public partial class LongNight : Skill
{
    int DeSurvive = 3;
    int deSpeed = 3;
    int energyCost1 = 2;
    int energyCost2 = 1;
    int Inpower = 5;
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
                energyCost1,
                null,
                null,
                CarryStep(target: RelativeTarget(-1), skillIndex: 2),
                CarryStep(target: RelativeTarget(1), skillIndex: 2),
                ModifyPropertyStep(PropertyType.Survivability, -DeSurvive),
                ModifyPropertyStep(PropertyType.Speed, -deSpeed)
            ),
            EnergyTimesGateStep(
                energyCost2,
                null,
                null,
                ModifyPropertyStep(PropertyType.Power, Inpower)
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
