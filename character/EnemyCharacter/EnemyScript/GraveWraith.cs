using Godot;

public partial class GraveWraith : EnemyCharacter
{
    private static int PassiveHeal => EnemyTuning.GetRequiredInt("GraveWraith", nameof(PassiveHeal));

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
        PortaitPath = "res://asset/EnemyCharater/GraveWraith.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/GraveWraith.tscn");
        SkillIDs =
        [
            SkillID.GraveWraithAttack,
            SkillID.GraveWraithSurvive,
            SkillID.GraveWraithSpecial,
        ];
        // 噬咬削生存，蜷护施加易伤，觉醒全阵攻击后再噬咬。
        IntentionCycle = [SkillID.GraveWraithAttack, SkillID.GraveWraithSurvive, SkillID.GraveWraithSpecial, SkillID.GraveWraithAttack, random];

        PassiveName = global::GraveWraith.PassiveNameText;
        PassiveDescription = global::GraveWraith.PassiveDescriptionText;
    }
}

public partial class GraveWraithAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "蚀骨噬咬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                V("SurvivabilityLossAmount"),
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class GraveWraithSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "骨壳蜷护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(
                Buff.BuffName.Vulnerable,
                V("VulnerableStacks"),
                HostileTargetReference.All
            )
        );
    }
}

public partial class GraveWraithSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "冥骸觉醒";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: V("BaseDamage"),
                multiplier: V("Multiplier"),
                target: HostileTargetReference.All,
                times: V("HitCount")
            ),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"))
        );
    }
}
