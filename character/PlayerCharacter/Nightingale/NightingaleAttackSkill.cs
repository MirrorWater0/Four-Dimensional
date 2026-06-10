using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 5;
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
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.One),
            ConditionStep(
                AnyAttackTargetDying,
                "击杀任一目标",
                AttackStep(baseDamage: DoubleStrikeBaseDamage, times: 1)
            )
        );
    }

    private bool AnyAttackTargetDying()
    {
        return GetAttackTargets()
            .Any(target => target != null && target.State == Character.CharacterState.Dying);
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
                    var targets = ChosetargetByOrder(byBehindRow: false, applyTaunt: true);
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
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 7;

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
            DrawCardsStep(1)
        );
    }
}

public partial class ContinuousPierce : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 7;

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
                ApplyBuffHostile(Buff.BuffName.Vulnerable, 1),
                ApplyBuffHostile(Buff.BuffName.Weaken, 2)
            )
        );
    }
}

public partial class RuinBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
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

public partial class NightfallFlurry : Skill
{
    private const int BaseDamage = 0;
    private const int PowerMultiplier = 1;

    public NightfallFlurry()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "夜幕连袭";
    public override int EnergyCost => 2;
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: PowerMultiplier,
                target: HostileTargetReference.All
            ),
            CarryStep(target: TargetReference.Next, skillIndex: 3)
        );
    }
}
