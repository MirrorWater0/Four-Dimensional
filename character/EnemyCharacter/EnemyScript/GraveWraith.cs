using Godot;

public partial class GraveWraith : EnemyCharacter
{
    private const int PassiveSurvivabilityDown = 99;

    public const string PassiveNameText = "凋零威压";
    public static string PassiveDescriptionText =>
        $"回合开始时：减少目标{PassiveSurvivabilityDown}点生存。";

    public override string CharacterName { get; set; } = "冥骨游魂";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        TriggerPassive(null);
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (BattleNode == null)
            return;

        Character[] targets = ChooseHostileTargetsByOrder(
            returnDummyWhenEmpty: false,
            normalOnly: false,
            dyingFilter: true
        );
        Character target = targets.Length > 0 ? targets[0] : null;
        if (target == null)
            return;

        await target.DescendingProperties(
            PropertyType.Survivability,
            PassiveSurvivabilityDown,
            this
        );
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

        MaxLife = 70;
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
    private const int BaseDamage = 33;

    public GraveWraithAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "蚀骨噬咬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage));
    }
}

public partial class GraveWraithSurvive : Skill
{
    private const int BaseBlock = 28;

    public GraveWraithSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "骨壳蜷护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, 2, HostileTargetReference.All)
        );
    }
}

public partial class GraveWraithSpecial : Skill
{
    public GraveWraithSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "冥骸觉醒";
    public override int EnergyCost => 7;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 33, multiplier: 1),
            LowerTargetPropertyStep(PropertyType.Survivability, 99, HostileTargetReference.All)
        );
    }
}
