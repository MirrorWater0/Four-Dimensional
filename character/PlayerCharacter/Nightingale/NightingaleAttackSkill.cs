using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    bool hasInvisible =>
        OwnerCharater?.StartActionBuffs?.Any(x => x.ThisBuffName == Buff.BuffName.Invisible)
        == true;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "影袭";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ConditionStep(
                () => hasInvisible,
                $"拥有{Buff.BuffName.Invisible.GetDescription()}",
                AttackStep(baseDamage: BaseDamage, prefix: "额外造成", multiplier: V("Multiplier"), times: V("HitCount"))
            )
        );
    }
}

public partial class ShadowExecution : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int DoubleStrikeBaseDamage => V(nameof(DoubleStrikeBaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "处决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ConditionStep(
                AnyAttackTargetDying,
                "击杀任一目标",
                AttackStep(baseDamage: DoubleStrikeBaseDamage, target: HostileTargetReference.Random,
                    times: V("HitCount"), multiplier: V("Multiplier"))
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
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "破击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    Character target = GetManualHostileTarget();
                    if (target == null || target == OwnerCharater?.BattleNode?.dummy)
                        return Task.CompletedTask;

                    if (target.Block > 0)
                        target.UpdataBlock(-target.Block, source: OwnerCharater);

                    return Task.CompletedTask;
                },
                _ => new[] { "去掉目标的格挡。" }
            ),
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount"))
        );
    }
}

public partial class StasisBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "凝滞之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            SelectDiscardPileCardsToHandStep(V("SelectDiscardCount"))
        );
    }
}

public partial class ContinuousPierce : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    private bool IsAtFullLife =>
        OwnerCharater != null && OwnerCharater.Life >= OwnerCharater.BattleMaxLife;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "连续贯穿";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ApplyBuffFriendly(Buff.BuffName.Invisible, V("InvisibleStacks"), TargetReference.ManualFriendly)
        );
    }
}

public partial class RuinBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "破灭之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            AddCardsStep(SkillID.None, V("RandomCardCount"), BattleCardPileTarget.HandCards),
            ExhaustCardsStep(BattleCardPileTarget.DrawPileCards, V("ExhaustCount"))
        );
    }
}

public partial class NightfallFlurry : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int PowerMultiplier => V(nameof(PowerMultiplier));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "夜幕连袭";

    public override SkillRarity Rarity => SkillRarity.Uncommon;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: PowerMultiplier,
                target: HostileTargetReference.All,
                times: V("HitCount")
            ),
            PlayDrawPileCardStep(SkillTypes.Permanent)
        );
    }
}

public partial class BladeSalvo : Skill
{
    private int BladeCount => V(nameof(BladeCount));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "利刃齐发";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this,AttackStep(V("basisdamge")), AddCardsStep(SkillID.Blade, BladeCount,BattleCardPileTarget.HandCards));
    }
}
