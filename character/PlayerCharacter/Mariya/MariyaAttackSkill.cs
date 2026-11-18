using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int BaseHeal => V(nameof(BaseHeal));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "愈合之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            HealStep(
                baseHeal: BaseHeal,
                target: TargetReference.All,
                preferNonFull: true,
                rebirth: false
            )
        );
    }
}

public partial class SwapSlash : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "斩断裂隙";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            HurtFriendly(V("FriendlyDamage"), TargetReference.All)
        );
    }
}

public partial class SiphonSlash : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "汲生之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            HealStep(
                _ => GetSiphonHealAmount(),
                target: TargetReference.Self,
                descriptionOverride: $"回复等同于此次造成伤害的 1/{V("HealDivisor")} 的生命。"
            )
        );
    }

    private int GetSiphonHealAmount()
    {
        int damage =
            OwnerCharater?.BattleNode?.GetLastRecordedDamageFromCurrentEffectSource(
                source: OwnerCharater,
                target: GetAttackTarget()
            ) ?? 0;
        if (damage > 0)
            return Math.Max(0, damage / V("HealDivisor"));

        return Math.Max(0, DamageFromPower(BaseDamage, V("Multiplier")) / V("HealDivisor"));
    }
}

public partial class ShatterSlash : Skill
{

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "斩破";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")), SelectDrawPileCardsToHandStep(V("SelectDrawCount")));
    }
}

public partial class ChargedBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "聚能之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep("X为回合结束时剩余能量。"),
            WhileStep(
                times: () => Math.Max(0, OwnerEnergy),
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount"))]
            )
        );
    }
}

public partial class CrescentWind : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));
    private int WeakenStacks => V(nameof(WeakenStacks));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "新月之风";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), target: HostileTargetReference.All, times: V("HitCount")),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class ArcTrack : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "弧形轨迹";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount")),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount"), BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class RenewalFlurry : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "回春连刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep("x为本场战斗己方角色恢复血量的总次数。"),
            WhileStep(
                times: GetAlliedHealCount,
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount"))]
            )
        );
    }

    private int GetAlliedHealCount() =>
        OwnerCharater?.BattleNode?.PlayerBattleHealCount ?? 0;
}

public partial class Sacrifice : Skill
{
    private int basisDamage => V("BaseDamage");
    private int allyHurt => V("FriendlyDamage");
    public override string SkillName { get; set; } = "献祭";

    public override SkillTypes SkillType => SkillTypes.Attack;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HurtFriendly(allyHurt, TargetReference.All),
            AttackStep(baseDamage: basisDamage, multiplier: V("Multiplier"), target: HostileTargetReference.All, times: V("HitCount"))
        );
    }
}
