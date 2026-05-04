using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 12;
    private const int BaseHeal = 10;

    public MendSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "愈合斩";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            HealStep(
                baseHeal: BaseHeal,
                target: AbsoluteTarget(AbsoluteFriendlySelector.FrontMost),
                preferNonFull: true,
                rebirth: false
            )
        );
    }
}

public partial class SwapSlash : Skill
{
    private const int BaseDamage = 18;

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
            AttackPrimaryStep(baseDamage: BaseDamage),
            SwapPositionFriendlyStep(relativeIndexA: -1, relativeIndexB: 1)
        );
    }
}

public partial class SiphonSlash : Skill
{
    private const int BaseDamage = 8;
    private const string AttackTargetKey = "siphon_target";

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
            AttackPrimaryStep(baseDamage: BaseDamage, storeAs: AttackTargetKey),
            HealStep(
                baseHeal: _ =>
                    (
                        OwnerCharater?.BattleNode?.GetLastRecordedDamageFromCurrentEffectSource(
                            source: OwnerCharater,
                            target: GetStoredTarget(AttackTargetKey)
                        ) ?? 0
                    ) / 2,
                target: AbsoluteTarget(AbsoluteFriendlySelector.LowestLife),
                descriptionOverride: $"回复生命值最低的己方角色等同于此次造成伤害一半的生命。"
            )
        );
    }
}

public partial class ShatterSlash : Skill
{
    private const int BaseDamage = 13;
    private const int RequiredHitCount = 5;
    private const int RebirthStacks = 1;
    private const int NextAllySelfDamage = 25;

    private int _recordedHitCount;

    public ShatterSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "斩破";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    // Record enemy hits before follow-up ally damage changes the battlefield state.
                    _recordedHitCount = ChosetargetByOrder(byBehindRow: false).Length;
                    return Task.CompletedTask;
                },
                _ => Array.Empty<string>()
            ),
            AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(0)),
            ConditionStep(
                () => _recordedHitCount >= RequiredHitCount,
                $"命中至少{RequiredHitCount}个敌人",
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.RebirthI,
                    stacks: RebirthStacks,
                    target: RelativeTarget(1)
                )
            ),
            HurtFriendly(NextAllySelfDamage, index: 1)
        );
    }
}

public partial class ChargedBlade : Skill
{
    private const int BaseDamage = 14;
    private const int MaxTargets = 2;
    private const int SurvivabilityLoss = 3;
    int times = 1;

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
            AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(MaxTargets)),
            EnergyTimesGateStep(
                0,
                () => times,
                v => times = v,
                AttackPrimaryStep(baseDamage: BaseDamage),
                ModifyPropertyStep(PropertyType.Survivability, -SurvivabilityLoss)
            )
        );
    }
}

public partial class CrescentWind : Skill
{
    private const int BaseDamage = 9;
    private const int WeakenStacks = 2;

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
            AoeDamageStep(
                baseDamage: BaseDamage,
                powerMultiplier: 1,
                target: HostileTargetsEachRowFirst()
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetsEachRowFirst()
            )
        );
    }
}
