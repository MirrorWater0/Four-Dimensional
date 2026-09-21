using Godot;

public partial class GraveWraith : EnemyCharacter
{
    private const int PassiveHeal = 7;

    public const string PassiveNameText = "凋零威压";
    public static string PassiveDescriptionText =>
        $"造成未被格挡的伤害时：恢复{PassiveHeal}点生命。";

    public override string CharacterName { get; set; } = "冥骨游魂";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnDealUnblockedDamage(
        Character target,
        int actualDamage,
        DamageKind damageKind
    )
    {
        if (target == null || actualDamage <= 0 || target == this)
            return;

        using var _ = BeginEffectSource("被动");
        Recover(PassiveHeal, source: this);
    }
}

public partial class GraveWraithRegedit : EnemyRegedit
{
    public GraveWraithRegedit()
    {
        CharacterName = "冥骨游魂";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/GraveWraith.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/GraveWraith.tscn");

        MaxLife = 152;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs =
        [
            SkillID.GraveWraithAttack,
            SkillID.GraveWraithSurvive,
            SkillID.GraveWraithSpecial,
        ];

        PassiveName = global::GraveWraith.PassiveNameText;
        PassiveDescription = global::GraveWraith.PassiveDescriptionText;
    }
}

public partial class GraveWraithAttack : Skill
{
    private const int BaseDamage = 28;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "蚀骨噬咬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                99,
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class GraveWraithSurvive : Skill
{
    private const int BaseBlock = 28;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "骨壳蜷护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 2)),
            ApplyBuffHostile(
                Buff.BuffName.Vulnerable,
                V("VulnerableStacks", 2),
                HostileTargetReference.All
            )
        );
    }
}

public partial class GraveWraithSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "冥骸觉醒";
    public override int EnemySpecialIntentionCooldown => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: V("BaseDamage", 18),
                multiplier: V("Multiplier", 1),
                target: HostileTargetReference.All
            ),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain", 5))
        );
    }
}
