public partial class BasicAttack : Skill
{
    private const int BaseDamage = 7;
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
    private const int BaseBlock = 7;
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

public partial class BasicGuard : Skill
{
    private const int BaseBlock = 3;
    private const int SurvivabilityMultiplier = 1;

    public BasicGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护卫";
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.ManualFriendly,
                baseBlock: BaseBlock,
                survivabilityMultiplier: SurvivabilityMultiplier
            )
        );
    }
}

public partial class BasicSpecial : Skill
{
    private const int PowerGain = 2;
    private const int DeSur = 4;

    public BasicSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "基础特殊";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(PropertyType.Survivability, DeSur),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}
