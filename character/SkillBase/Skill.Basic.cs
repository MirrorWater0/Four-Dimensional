public partial class BasicAttack : Skill
{
    private const int BaseDamage = 16;
    private const int PowerMultiplier = 1;

    public BasicAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "基础攻击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: PowerMultiplier)
        );
    }
}

public partial class BasicDefense : Skill
{
    private const int BaseBlock = 18;
    private const int SurvivabilityMultiplier = 1;

    public BasicDefense()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "基础防御";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                relativeIndex: 0,
                baseBlock: BaseBlock,
                survivabilityMultiplier: SurvivabilityMultiplier
            )
        );
    }
}

public partial class BasicSpecial : Skill
{
    private const int PowerGain = 2;
    private const int DeSur = 5;
    private const int EnergyCost = 3;

    public BasicSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "基础特殊";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(PropertyType.Survivability, DeSur),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps: [CarryStep(target: RelativeTarget(1), skillIndex: 0)]
            )
        );
    }
}
