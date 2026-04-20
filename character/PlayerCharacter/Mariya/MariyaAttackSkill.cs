using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 14;
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
                dyingFilter: false,
                preferNonFull: true,
                rebirth: false
            )
        );
    }
}

public partial class SwapSlash : Skill
{
    private const int BaseDamage = 10;

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
    private const int BaseDamage = 10;
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
    private const int BaseDamage = 15;
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
            AoeDamageStep(baseDamage: BaseDamage, maxTargets: 0),
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
