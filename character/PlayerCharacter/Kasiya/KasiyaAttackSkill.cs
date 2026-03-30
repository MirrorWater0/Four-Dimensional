using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaAttackSkill { }

public partial class Determination : Skill
{
    private const int DamageImmuneStacks = 1;
    private const int BaseDamage = 10;

    public Determination()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "剑意已决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                index: 0,
                dyingFilter: false
            )
        );
    }
}

public partial class Smite : Skill
{
    private const int BaseDamage = 15;
    private const int SurvivalDown = 5;
    int times = 1;

    public Smite()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "绝域剑杀";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivalDown),
            AttackPrimaryStep(baseDamage: BaseDamage),
            EnergyTimesGateStep(
                0,
                () => times,
                value => times = value,
                LowerTargetPropertyStep(PropertyType.Survivability, SurvivalDown)
            )
        );
    }
}

public partial class Charge : Skill
{
    private const int BaseDamage = 6;

    public Charge()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "无畏冲锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: "charge_target"),
            BlockFriendlyByRelativeStep(
                relativeIndex: 0,
                baseBlock: _ =>
                    OwnerCharater?.BattleNode?.GetLastRecordedDamageFromCurrentEffectSource(
                        source: OwnerCharater,
                        target: GetStoredTarget("charge_target")
                    ) ?? 0,
                describe: false
            ),
            TextStep($"获得等同于此次造成伤害+{X(StatX.Survivability)}的格挡。")
        );
    }
}

public partial class Vower : Skill
{
    private const int BaseDamage = 8;
    int times = 2;

    public Vower()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "誓约者";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            EnergyTimesGateStep(
                0,
                () => times,
                value => times = value,
                CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1)
            )
        );
    }
}
