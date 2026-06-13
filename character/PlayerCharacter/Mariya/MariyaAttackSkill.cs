using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 7;
    private const int BaseHeal = 3;
    public override bool ExhaustsAfterUse => true;

    public MendSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

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

    public SwapSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "斩断裂隙";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 2),
            HurtFriendly(3, TargetReference.All)
        );
    }
}

public partial class SiphonSlash : Skill
{
    private const int BaseDamage = 5;

    public SiphonSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

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
    private const int BaseDamage = 9;
    private const int RequiredHitCount = 4;
    private const int RebirthStacks = 1;
    private const int NextAllySelfDamage = 23;

    private int _recordedHitCount;

    public ShatterSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "斩破";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(4), SelectDrawPileCardsToHandStep(1));
    }
}

public partial class ChargedBlade : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 6;
    private const int SurvivabilityLoss = 3;

    public ChargedBlade()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

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

    public CrescentWind()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "新月之风";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1, target: HostileTargetReference.All),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class ArcTrack : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 2;

    public ArcTrack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "弧形轨迹";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ApplyBuffFriendly(Buff.BuffName.ExtraDraw, 1, TargetReference.Others)
        );
    }
}

public partial class Sacrifice : Skill
{
    int basisDamage = 5;
    int allyHurt = 10;
    int DeMax = 10;
    public override string SkillName { get; set; } = "献祭";
    public override int EnergyCost => 2;

    public Sacrifice()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HurtFriendly(allyHurt, TargetReference.All),
            AttackStep(baseDamage: basisDamage, multiplier: 2, target: HostileTargetReference.All)
        );
    }
}
