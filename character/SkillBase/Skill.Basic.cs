public partial class BasicAttack : Skill
{
    private const int BaseDamage = 4;
    private const int PowerMultiplier = 1;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_attack.name", "基础攻击");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: V(nameof(BaseDamage), BaseDamage),
                multiplier: V(nameof(PowerMultiplier), PowerMultiplier)
            )
        );
    }
}

public partial class BasicDefense : Skill
{
    private const int BaseBlock = 4;
    private const int SurvivabilityMultiplier = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.basic_defense.name", "基础防御");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                baseBlock: V(nameof(BaseBlock), BaseBlock),
                multiplier: V(nameof(SurvivabilityMultiplier), SurvivabilityMultiplier)
            )
        );
    }
}

public partial class BasicGuard : Skill
{
    private const int BaseBlock = 2;
    private const int SurvivabilityMultiplier = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_guard.name", "护卫");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.ManualFriendly,
                baseBlock: V(nameof(BaseBlock), BaseBlock),
                multiplier: V(nameof(SurvivabilityMultiplier), SurvivabilityMultiplier)
            )
        );
    }
}

public partial class BasicSpecial : Skill
{
    private const int PowerGain = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.basic_special.name", "基础特殊");
    public override int EnergyCost => Cost(2);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, V(nameof(PowerGain), PowerGain)),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}

public partial class KasiyaBasicSpecial : Skill
{
    private const int PowerGain = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.kasiya_basic_special.name", "侵袭");
    public override int EnergyCost => Cost(2);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(Buff.BuffName.Vulnerable, V("VulnerableStacks", 2)),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}

public partial class EchoBasicSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.echo_basic_special.name", "解离");
    public override int EnergyCost => Cost(0);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                Buff.BuffName.Weaken,
                V("WeakenStacks", 1),
                HostileTargetReference.All
            )
        );
    }
}

public partial class MariyaBasicSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.mariya_basic_special.name", "治愈");
    public override int EnergyCost => Cost(1);
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, HealStep(V("Heal", 6), TargetReference.ManualFriendly));
    }
}

public partial class NightingaleBasicSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.nightingale_basic_special.name", "隐藏");
    public override int EnergyCost => Cost(0);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(Buff.BuffName.Invisible, V("InvisibleStacks", 1)),
            DrawCardsStep(V("DrawCount", 1))
        );
    }
}
