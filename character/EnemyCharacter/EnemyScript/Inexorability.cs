using System;
using System.Threading.Tasks;
using Godot;

public partial class Inexorability : EnemyCharacter
{
    private const int HurtPowerDown = 1;

    public const string PassiveNameText = "不可违逆";
    public static string PassiveDescriptionText => $"受到攻击时：攻击者失去{HurtPowerDown}点力量。";

    public override string CharacterName { get; set; } = "Inexorability";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override async Task GetHurt(
        float damage,
        Character source = null,
        DamageKind damageKind = DamageKind.Other
    )
    {
        await base.GetHurt(damage, source, damageKind);

        if (
            damageKind != DamageKind.Attack
            || source == null
            || !GodotObject.IsInstanceValid(source)
            || source == this
            || source.State == CharacterState.Dying
        )
        {
            return;
        }

        using var _ = BeginEffectSource("被动");
        await source.DescendingProperties(PropertyType.Power, HurtPowerDown, this);
    }
}

public partial class InexorabilityRegedit : EnemyRegedit
{
    public InexorabilityRegedit()
    {
        CharacterName = "Inexorability";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Inexorability.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Inexorability.tscn");

        MaxLife = 65;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs =
        [
            SkillID.InexorabilityAttack,
            SkillID.InexorabilitySurvive,
            SkillID.InexorabilitySpecial,
        ];

        PassiveName = global::Inexorability.PassiveNameText;
        PassiveDescription = global::Inexorability.PassiveDescriptionText;
    }
}

public partial class InexorabilityAttack : Skill
{
    private const int BaseDamage = 18;
    private const int SelfEnergyGain = 1;

    public InexorabilityAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "命定压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1),
            EnergyStep(SelfEnergyGain)
        );
    }
}

public partial class InexorabilitySurvive : Skill
{
    private const int BaseBlock = 10;
    private const int PowerGain = 3;

    public InexorabilitySurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "存续封缄";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class InexorabilitySpecial : Skill
{
    private const int BaseDamage = 10;
    private const int PowerMultiplier = 1;

    public InexorabilitySpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终局律令";
    public override int EnergyCost => 5;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: PowerMultiplier,
                target: HostileTargetReference.All
            )
        );
    }
}
