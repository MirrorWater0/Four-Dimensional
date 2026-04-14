public partial class BasicAttack : Skill
{
    private const int BaseDamage = 20;
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
    private const int BaseBlock = 20;
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
    private const int PowerGain = 3;
    private const int DeSur = 3;
    private const int EnergyCost = 4;

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
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps: [CarryRelativeAllyStep(relativeIndex: 1, skillIndex: 0)]
            )
        );
    }
}
