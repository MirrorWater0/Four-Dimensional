using System;
using System.Threading.Tasks;
using Godot;

public partial class Inexorability : EnemyCharacter
{
    private const int HurtPowerDown = 2;
    private const int AllyDyingPowerGain = 2;

    public const string PassiveNameText = "不可违逆";
    public static string PassiveDescriptionText =>
        $"受到攻击时：攻击者失去{HurtPowerDown}点力量。\n"
        + $"敌方有角色进入濒死时：获得{AllyDyingPowerGain}点力量。";

    private Func<Character, Character, Task> _allyDyingHandler;

    public override string CharacterName { get; set; } = "Inexorability";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        _allyDyingHandler ??= OnHostileDying;
        if (BattleNode != null && !BattleNode.DyingEmitList.Contains(_allyDyingHandler))
            BattleNode.DyingEmitList.Add(_allyDyingHandler);
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _allyDyingHandler != null)
            BattleNode.DyingEmitList.Remove(_allyDyingHandler);
        base._ExitTree();
    }

    public override async Task GetHurt(
        float damage,
        Character source = null,
        bool canTriggerThorn = true,
        DamageKind damageKind = DamageKind.Other
    )
    {
        await base.GetHurt(damage, source, canTriggerThorn, damageKind);

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

    private Task OnHostileDying(Character target, Character source)
    {
        if (
            target == null
            || target.IsPlayer == IsPlayer
            || target.BattleNode != BattleNode
            || State != CharacterState.Normal
        )
        {
            return Task.CompletedTask;
        }

        TriggerPassive(null);
        return Task.CompletedTask;
    }

    public override void Passive(Skill skill)
    {
        using var effectSource = BeginEffectSource("被动");
        _ = IncreaseProperties(PropertyType.Power, AllyDyingPowerGain, this);
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

        MaxLife = 110;
        Power = 12;
        Survivability = 12;
        Speed = 11;
        SpecialIntentThreshold = 4;

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
    private const int SelfEnergyGain = 2;
    private const int AdjacentEnergyLoss = 1;

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
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 1, byBehindRow: true),
            EnergyStep(SelfEnergyGain),
            EnergyStep(-AdjacentEnergyLoss, RelativeTarget(1)),
            EnergyStep(-AdjacentEnergyLoss, RelativeTarget(-1))
        );
    }
}

public partial class InexorabilitySurvive : Skill
{
    private const int BaseBlock = 16;
    private const int SurvivabilityDown = 3;
    private const int SelfEnergyGain = 1;

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
            BlockStep(relativeIndex: 0, baseBlock: BaseBlock, survivabilityMultiplier: 1),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                target: HostileTargets(1, byBehindRow: true),
                permanent: false
            ),
            EnergyStep(SelfEnergyGain)
        );
    }
}

public partial class InexorabilitySpecial : Skill
{
    private const int SelfPowerGain = 4;
    private const int BaseDamage = 8;
    private const int PowerMultiplier = 2;

    public InexorabilitySpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终局律令";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            BlockStep(0, 8),
            AoeDamageStep(
                baseDamage: BaseDamage,
                powerMultiplier: PowerMultiplier,
                target: HostileTargetsEachRowLast()
            )
        );
    }
}
