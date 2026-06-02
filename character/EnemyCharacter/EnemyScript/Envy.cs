using System;
using System.Threading.Tasks;
using Godot;

public partial class Envy : EnemyCharacter
{
    private Action<Character, PropertyType, int, Character> _propertyIncreasedHandler;

    public const string PassiveNameText = "嫉妒";
    public static string PassiveDescriptionText => "敌方获得属性时：获得等量同名属性。";

    public override string CharacterName { get; set; } = "嫉妒";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;

        if (BattleNode == null)
            return;

        _propertyIncreasedHandler ??= TriggerPassive;
        BattleNode.PropertyIncreased -= _propertyIncreasedHandler;
        BattleNode.PropertyIncreased += _propertyIncreasedHandler;
    }

    public override void _ExitTree()
    {
        if (BattleNode != null && _propertyIncreasedHandler != null)
            BattleNode.PropertyIncreased -= _propertyIncreasedHandler;
        base._ExitTree();
    }

    private async void TriggerPassive(
        Character target,
        PropertyType type,
        int value,
        Character source
    )
    {
        if (
            value <= 0
            || State == CharacterState.Dying
            || target == null
            || target.BattleNode != BattleNode
            || target.IsPlayer == IsPlayer
        )
        {
            return;
        }

        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(type, value, this);
    }
}

public partial class EnvyEliteRegedit : EnemyRegedit
{
    public EnvyEliteRegedit()
    {
        CharacterName = "嫉妒";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Envy.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Envy.tscn");

        MaxLife = 223;
        Power = 14;
        Survivability = 17;
        Speed = 37;
        SkillIDs = [SkillID.EnvyEliteAttack, SkillID.EnvyEliteSurvive, SkillID.EnvyEliteSpecial];

        PassiveName = global::Envy.PassiveNameText;
        PassiveDescription = global::Envy.PassiveDescriptionText;
    }
}

public partial class EnvyEliteAttack : Skill
{
    private const int BaseDamage = 0;
    private const int SurvivabilityDown = 1;

    public EnvyEliteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "觊觎刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.Three),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class EnvyEliteSurvive : Skill
{
    private const int BaseBlock = 10;
    private const int SurvivabilityGain = 4;

    public EnvyEliteSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "藏锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ApplyBuffHostile(Buff.BuffName.Weaken, 1, HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class EnvyEliteSpecial : Skill
{
    private const int BaseDamage = 4;
    private const int PowerDown = 2;
    private const int SelfPowerGain = 2;

    public EnvyEliteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "夺辉";
    public override int EnergyCost => 8;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.EachRowLast),
            LowerTargetPropertyStep(
                PropertyType.Power,
                PowerDown,
                HostileTargetReference.AttackKey
            ),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            HealStep(0)
        );
    }
}
