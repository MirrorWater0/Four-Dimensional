using System;
using System.Linq;
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
                target: RelativeTarget(0)
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
                times,
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

    public override string SkillName { get; set; } = "冲锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: "charge_target"),
            BlockStep(
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
            EnergyTimesGateStep(0, times, CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1))
        );
    }
}

public partial class VulnerablePurge : Skill
{
    private const int BaseDamage = 20;

    public VulnerablePurge()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "致命弱点";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(
                baseDamage: BaseDamage,
                powerMultiplier: 1,
                maxTargets: 0,
                targetCondition: character =>
                    character?.HurtBuffs?.Any(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Vulnerable
                        && buff.Stack > 0
                    ) == true,
                targetConditionDescription: $"拥有{Buff.BuffName.Vulnerable.GetDescription()}"
            )
        );
    }
}

public partial class VulnerabilityStrike : Skill
{
    private const int BaseDamage = 10;
    private const string TargetKey = "vulnerability_strike_target";
    private bool _targetHadVulnerable;

    public VulnerabilityStrike()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "易伤追击";

    private bool ConsumeVulnerableSnapshotOrCheckCurrentTarget()
    {
        if (_targetHadVulnerable)
        {
            _targetHadVulnerable = false;
            return true;
        }

        return TargetHasVulnerable(GetStoredTarget(TargetKey));
    }

    private static bool TargetHasVulnerable(Character target) =>
        target?.HurtBuffs?.Any(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Vulnerable && buff.Stack > 0
        ) == true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    var target = ChosetargetByOrder(byBehindRow: false).FirstOrDefault();
                    _targetHadVulnerable = TargetHasVulnerable(target);
                    return System.Threading.Tasks.Task.CompletedTask;
                },
                _ => Array.Empty<string>()
            ),
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: TargetKey),
            ConditionStep(
                ConsumeVulnerableSnapshotOrCheckCurrentTarget,
                $"出手前目标拥有{Buff.BuffName.Vulnerable.GetDescription()}",
                AttackPrimaryStep(
                    baseDamage: BaseDamage,
                    prefix: "额外造成",
                    target: StoredTarget(TargetKey)
                )
            )
        );
    }
}
