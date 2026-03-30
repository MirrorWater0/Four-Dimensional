using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 8;
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
            HealFriendlyStep(
                baseHeal: BaseHeal,
                survivabilityMultiplier: 0,
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
            HealFriendlyStep(
                baseHeal: _ =>
                    (
                        OwnerCharater?.BattleNode?.GetLastRecordedDamageFromCurrentEffectSource(
                            source: OwnerCharater,
                            target: GetStoredTarget(AttackTargetKey)
                        ) ?? 0
                    ) / 2,
                target: RelativeTarget(0),
                descriptionOverride: $"回复等同于此次造成伤害一半+{X(StatX.Survivability)}的生命。"
            )
        );
    }
}
