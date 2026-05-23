using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 7;
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
            AttackStep(baseDamage: BaseDamage),
            ConditionStep(
                () => hasInvisible,
                $"拥有{Buff.BuffName.Invisible.GetDescription()}",
                AttackStep(baseDamage: BaseDamage, prefix: "额外造成"),
                EnergyStep(1)
            )
        );
    }
}

public partial class ShadowExecution : Skill
{
    private const int BaseDamage = 10;
    private const int DoubleStrikeBaseDamage = 10;

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
            AttackStep(baseDamage: BaseDamage),
            ConditionStep(
                () => GetAttackTarget()?.State == Character.CharacterState.Dying,
                "击杀目标",
                AttackStep(baseDamage: DoubleStrikeBaseDamage, times: 1)
            )
        );
    }
}

public partial class BreakStrike : Skill
{
    private const int BaseDamage = 7;

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
            AttackStep(baseDamage: BaseDamage)
        );
    }
}

public partial class StasisBlade : Skill
{
    private const int BaseDamage = 7;
    private const int SpeedDown = 4;

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
            AttackStep(baseDamage: BaseDamage),
            LowerTargetPropertyStep(PropertyType.Speed, SpeedDown, HostileTargetReference.One)
        );
    }
}

public partial class ContinuousPierce : Skill
{
    private const int BaseDamage = 7;
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
            AttackStep(baseDamage: BaseDamage),
            ConditionStep(
                () => IsAtFullLife,
                "满血",
                AttackStep(baseDamage: 0, multiplier: 1, prefix: "额外造成", times: 2),
                HurtFriendly(SelfDamage, TargetReference.Self)
            )
        );
    }
}

public partial class RuinBlade : Skill
{
    private const int BaseDamage = 4;

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
            AttackStep(baseDamage: BaseDamage, multiplier: 1, times: 1),
            CarryStep(target: TargetReference.ManualFriendly, skillIndex: 1)
        );
    }
}
