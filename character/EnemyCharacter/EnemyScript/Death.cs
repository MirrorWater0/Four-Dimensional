using Godot;

public partial class Death : EnemyCharacter
{
    private const int DisasterStacks = 5;

    public const string PassiveNameText = "终末游行";
    public static string PassiveDescriptionText =>
        $"回合开始时：令目标获得{DisasterStacks}层{Buff.BuffName.Disaster.GetDescription()}。";

    public override string CharacterName { get; set; } = "Death";

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

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
    }

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        Character[] targets = ChooseHostileTargetsByOrder(
            returnDummyWhenEmpty: false,
            normalOnly: false,
            dyingFilter: true
        );
        Character target = targets.Length > 0 ? targets[0] : null;
        if (target == null)
            return;

        EndActionBuff.BuffAdd(Buff.BuffName.Disaster, target, DisasterStacks, this);
    }
}

public partial class DeathRegedit : EnemyRegedit
{
    public DeathRegedit()
    {
        CharacterName = "Death";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Death.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Death.tscn");

        MaxLife = 405;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.DeathAttack, SkillID.DeathSurvive, SkillID.DeathSpecial];

        PassiveName = global::Death.PassiveNameText;
        PassiveDescription = global::Death.PassiveDescriptionText;
    }
}

public partial class DeathAttack : Skill
{
    private const int BaseDamage = 9;
    private const int HitCount = 2;

    public DeathAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "双魂裁决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, times: HitCount),
            ApplyBuffHostile(Buff.BuffName.Weaken, 1)
        );
    }
}

public partial class DeathSurvive : Skill
{
    private const int BaseBlock = 43;
    private const int Heal = 8;

    public DeathSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "死寂";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            HealStep(Heal, target: TargetReference.Self)
        );
    }
}

public partial class DeathSpecial : Skill
{
    private const int SelfPowerGain = 3;
    private const int DisasterStacks = 4;

    public DeathSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终焉宣告";
    public override int EnergyCost => 10;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ApplyBuffHostile(Buff.BuffName.Disaster, DisasterStacks, HostileTargetReference.All),
            AddStatusCardsToDrawPileStep(SkillID.PlagueStatus, 2, HostileTargetReference.All)
        );
    }
}
