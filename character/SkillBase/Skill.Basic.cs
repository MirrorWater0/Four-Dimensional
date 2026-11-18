public partial class BasicAttack : Skill
{
    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_attack.name", "基础攻击");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount"))
        );
    }
}

public partial class BasicDefense : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int SurvivabilityMultiplier => V(nameof(SurvivabilityMultiplier));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.basic_defense.name", "基础防御");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                baseBlock: V("BaseBlock"),
                multiplier: V("SurvivabilityMultiplier")
            )
        );
    }
}

public partial class BasicGuard : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int SurvivabilityMultiplier => V(nameof(SurvivabilityMultiplier));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = I18n.Tr("skill.basic_guard.name", "护卫");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: V("BaseBlock"),
                multiplier: V("SurvivabilityMultiplier")
            )
        );
    }
}

public partial class KasiyaBasicSpecial : Skill
{

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.kasiya_basic_special.name", "侵袭");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(Buff.BuffName.Vulnerable, V("VulnerableStacks"), HostileTargetReference.Manual),
            PlayDrawPileCardStep(SkillTypes.Attack, random: true)
        );
    }
}

public partial class EchoBasicSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.echo_basic_special.name", "解离");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                Buff.BuffName.Weaken,
                V("WeakenStacks"),
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

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, HealStep(V("Heal"), TargetReference.ManualFriendly));
    }
}

public partial class NightingaleBasicSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.nightingale_basic_special.name", "隐藏");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(Buff.BuffName.Invisible, V("InvisibleStacks")),
            DrawCardsStep(V("DrawCount"))
        );
    }
}
