using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 7;
    private const int BaseHeal = 2;
    public override bool ExhaustsAfterUse => true;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "愈合之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
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
    private const int BaseDamage = 12;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "斩断裂隙";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier", 2)),
            HurtFriendly(V("FriendlyDamage", 3), TargetReference.All)
        );
    }
}

public partial class SiphonSlash : Skill
{
    private const int BaseDamage = 5;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "汲生之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            HealStep(
                _ => GetSiphonHealAmount(),
                target: TargetReference.Self,
                descriptionOverride: "回复等同于此次造成伤害一半的生命。"
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
            return Math.Max(0, damage / 2);

        return Math.Max(0, DamageFromPower(BaseDamage) / 2);
    }
}

public partial class ShatterSlash : Skill
{

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "斩破";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(V("BaseDamage", 4)), SelectDrawPileCardsToHandStep(V("SelectDrawCount", 1)));
    }
}

public partial class ChargedBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private const int BaseDamage = 4;
    private const int SurvivabilityLoss = 3;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "聚能之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            AttackStep(baseDamage: BaseDamage),
            ModifyPropertyStep(PropertyType.Survivability, -SurvivabilityLoss)
        );
    }
}

public partial class CrescentWind : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 4;
    private const int WeakenStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "新月之风";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier", 1), target: HostileTargetReference.All),
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
    private const int BaseDamage = 7;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "弧形轨迹";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount", 1)),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount", 1), BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class RenewalFlurry : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 9;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "回春连刃";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep("x为本场战斗己方角色恢复血量的总次数。"),
            WhileStep(
                times: GetAlliedHealCount,
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier", 1))]
            )
        );
    }

    private int GetAlliedHealCount() =>
        OwnerCharater?.BattleNode?.PlayerBattleHealCount ?? 0;
}

public partial class Sacrifice : Skill
{
    int basisDamage = 12;
    int allyHurt = 4;
    int DeMax = 10;
    public override string SkillName { get; set; } = "献祭";
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Attack;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HurtFriendly(allyHurt, TargetReference.All),
            AttackStep(baseDamage: basisDamage, multiplier: V("Multiplier", 2), target: HostileTargetReference.All)
        );
    }
}
