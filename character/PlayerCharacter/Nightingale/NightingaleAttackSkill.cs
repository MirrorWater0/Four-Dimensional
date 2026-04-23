using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 14;
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
                $"自身拥有{Buff.BuffName.Invisible.GetDescription()}",
                AttackPrimaryStep(baseDamage: BaseDamage, prefix: "额外造成")
            ),
            ModifyPropertyStep(PropertyType.Power, GainPower)
        );
    }
}

public partial class ShadowExecution : Skill
{
    private const int BaseDamage = 15;
    private const int DoubleStrikeBaseDamage = 5;
    private const string KillTargetKey = "目标";

    public ShadowExecution()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "影处决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: KillTargetKey),
            ConditionStep(
                () => GetStoredTarget(KillTargetKey)?.State == Character.CharacterState.Dying,
                "击杀目标",
                AttackPrimaryStep(baseDamage: DoubleStrikeBaseDamage, times: 2)
            )
        );
    }
}

public partial class BreakStrike : Skill
{
    private const int BaseDamage = 5;

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
    private const int BaseDamage = 17;
    private const int SpeedDown = 4;
    private const int FirstCastExtraSpeedDown = 3;
    private const string TargetKey = "攻击目标";
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
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: TargetKey),
            LowerTargetPropertyStep(PropertyType.Speed, SpeedDown, target: StoredTarget(TargetKey)),
            EnergyTimesGateStep(
                0,
                times,
                LowerTargetPropertyStep(
                    PropertyType.Speed,
                    FirstCastExtraSpeedDown,
                    target: StoredTarget(TargetKey)
                )
            )
        );
    }
}

public partial class ContinuousPierce : Skill
{
    private const int BaseDamage = 5;
    private const int SelfDamage = 10;

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
                "自身满血",
                AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, prefix: "额外造成", times: 2),
                HurtFriendly(SelfDamage, 0)
            )
        );
    }
}
