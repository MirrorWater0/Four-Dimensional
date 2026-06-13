using Godot;

public partial class FearWorm : EnemyCharacter
{
    private const int PassivePowerGain = 1;

    public const string PassiveNameText = "蜕皮";
    public static string PassiveDescriptionText =>
        $"回合结束时：获得{PassivePowerGain}点力量。";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        TriggerPassive(null);
        base.OnTurnEnd();
    }

    public override async void Passive(Skill skill)
    {
        if (BattleNode?.QueueEnemyPhaseEndPowerGain(this, PassivePowerGain, this, "被动") == true)
            return;

        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class FearWormRegedit : EnemyRegedit
{
    public FearWormRegedit()
    {
        CharacterName = "FearWorm";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/FearWorm.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/FearWorm.tscn");

        MaxLife = 28;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormTermin];

        PassiveName = global::FearWorm.PassiveNameText;
        PassiveDescription = global::FearWorm.PassiveDescriptionText;
    }
}

public partial class FearWormAttack : Skill
{
    private const int BaseDamage = 4;
    private const int VulnerableStacks = 2;
    private const int MaxTargets = 3;
    private const int EnergyGain = 1;

    public FearWormAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恐惧咬噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            AttackStep(
                baseDamage: BaseDamage,
                target: HostileTargetReference.All,
                times: 1,
                clampMax: 999
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class FearWormSurvive : Skill
{
    private const int BaseBlock = 12;

    public FearWormSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "潜伏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, 2, TargetReference.Previous),
            ModifyPropertyStep(PropertyType.Power, 3, TargetReference.Self)
        );
    }
}

public partial class FearWormTermin : Skill
{
    private const int BaseDamage = 12;
    private const int StunStacks = 2;

    public FearWormTermin()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "梦魇缠绕";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(BaseDamage));
    }
}
