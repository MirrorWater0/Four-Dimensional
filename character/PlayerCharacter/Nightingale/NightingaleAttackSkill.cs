using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 12;
    int GainPower = 3;
    bool hasInvisible =>
        OwnerCharater?.StartActionBuffs?.Any(x => x.ThisBuffName == Buff.BuffName.Invisible)
        == true;

    public ShadowAmbush()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "影袭";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            ConditionStep(
                () => hasInvisible,
                $"拥有{Buff.BuffName.Invisible.GetDescription()}",
                AttackPrimaryStep(baseDamage: BaseDamage, prefix: "额外造成"),
                EnergyStep(1)
            ),
            ModifyPropertyStep(PropertyType.Power, GainPower)
        );
    }
}

public partial class ShadowExecution : Skill
{
    private const int BaseDamage = 20;
    private const int DoubleStrikeBaseDamage = 0;
    private const string KillTargetKey = "目标";

    public ShadowExecution()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "处决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: KillTargetKey),
            ConditionStep(
                () => DidStoredTargetEnterDyingSinceStored(KillTargetKey),
                "击杀目标",
                AttackPrimaryStep(baseDamage: DoubleStrikeBaseDamage, times: 2)
            )
        );
    }
}

public partial class BreakStrike : Skill
{
    private const int BaseDamage = 11;

    public BreakStrike()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "破击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    var targets = ChosetargetByOrder(byBehindRow: false);
                    var target = targets[0];
                    if (target == null || target == OwnerCharater?.BattleNode?.dummy)
                        return Task.CompletedTask;

                    if (target.Block > 0)
                        target.UpdataBlock(-target.Block, source: OwnerCharater);

                    return Task.CompletedTask;
                },
                _ => new[] { "去掉目标的格挡。" }
            ),
            AttackPrimaryStep(baseDamage: BaseDamage)
        );
    }
}

public partial class StasisBlade : Skill
{
    private const int BaseDamage = 15;
    private const int SpeedDown = 5;
    private const int FirstCastExtraSpeedDown = 3;
    int times = 1;

    public StasisBlade()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "凝滞之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            LowerTargetPropertyStep(PropertyType.Speed, SpeedDown),
            EnergyTimesGateStep(
                0,
                times,
                LowerTargetPropertyStep(PropertyType.Speed, FirstCastExtraSpeedDown)
            )
        );
    }
}

public partial class ContinuousPierce : Skill
{
    private const int BaseDamage = 10;
    private const int SelfDamage = 7;

    private bool IsAtFullLife =>
        OwnerCharater != null && OwnerCharater.Life >= OwnerCharater.BattleMaxLife;

    public ContinuousPierce()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "连续贯穿";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            ConditionStep(
                () => IsAtFullLife,
                "满血",
                AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, prefix: "额外造成", times: 2),
                HurtFriendly(SelfDamage, 0)
            )
        );
    }
}

public partial class RuinBlade : Skill
{
    private const int BaseDamage = 6;
    private int times = 1;

    public RuinBlade()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "破灭之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 1, times: 2),
            EnergyTimesGateStep(0, times, CarryStep(target: RelativeTarget(-1), skillIndex: 0))
        );
    }
}
