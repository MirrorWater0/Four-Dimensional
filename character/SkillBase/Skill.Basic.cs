public partial class BasicAttack : Skill
{
    private const int BaseDamage = 4;
    private const int PowerMultiplier = 1;

    public BasicAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_attack.name", "基础攻击");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, multiplier: PowerMultiplier));
    }
}

public partial class BasicDefense : Skill
{
    private const int BaseBlock = 4;
    private const int SurvivabilityMultiplier = 1;

    public BasicDefense()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } =
        I18n.Tr("skill.basic_defense.name", "基础防御");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: SurvivabilityMultiplier)
        );
    }
}

public partial class BasicGuard : Skill
{
    private const int BaseBlock = 2;
    private const int SurvivabilityMultiplier = 1;

    public BasicGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_guard.name", "护卫");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.ManualFriendly,
                baseBlock: BaseBlock,
                multiplier: SurvivabilityMultiplier
            )
        );
    }
}

public partial class BasicSpecial : Skill
{
    private const int PowerGain = 1;

    public BasicSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } =
        I18n.Tr("skill.basic_special.name", "基础特殊");
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}
