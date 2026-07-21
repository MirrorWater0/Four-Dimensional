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

        MaxLife = 71;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        HasAttackVulnerableIntention = true;
        SkillIDs = [SkillID.FearWormAttack, SkillID.FearWormTermin];
        OpeningIntentionSkillIDs = [SkillID.FearWormTermin];
        PassiveName = global::FearWorm.PassiveNameText;
        PassiveDescription = global::FearWorm.PassiveDescriptionText;
    }
}

public partial class FearWormAttack : Skill
{
    private const int BaseDamage = 4;
    private const int VulnerableStacks = 2;
    private const int MaxTargets = 3;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "恐惧咬噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                target: HostileTargetReference.All,
                times: V("HitCount", 1),
                clampMax: V("ClampMax", 999)
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

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "潜伏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain", 3), TargetReference.Self)
        );
    }
}

public partial class FearWormTermin : Skill
{
    private const int BaseDamage = 14;
    private const int StunStacks = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "梦魇缠绕";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(BaseDamage));
    }
}
