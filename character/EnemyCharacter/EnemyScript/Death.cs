using Godot;

public partial class Death : EnemyCharacter
{
    private const int DisasterStacks = 4;

    public const string PassiveNameText = "终末游行";
    public static string PassiveDescriptionText =>
        $"回合开始时：随机一名敌人获得{DisasterStacks}层{Buff.BuffName.Disaster.GetDescription()}。";

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
            normalOnly: true,
            dyingFilter: true
        );
        int targetIndex =
            targets.Length > 0 ? BattleNode?.BattleIntentionRandom?.Next(targets.Length) ?? 0 : -1;
        Character target = targetIndex >= 0 ? targets[targetIndex] : null;
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

        MaxLife = 567;
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
    private const int BaseDamage = 10;
    private const int HitCount = 2;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "双魂裁决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, times: HitCount),
            ApplyBuffHostile(Buff.BuffName.Weaken, V("WeakenStacks", 1))
        );
    }
}

public partial class DeathSurvive : Skill
{
    private const int BaseBlock = 33;
    private const int Heal = 15;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "死寂";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 2)),
            HealStep(Heal, target: TargetReference.Self),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount", 3), BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class DeathSpecial : Skill
{
    private const int SelfPowerGain = 2;
    private const int DisasterStacks = 4;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "终焉宣告";
    public override int EnemySpecialIntentionCooldown => 3;


    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ApplyBuffHostile(Buff.BuffName.Disaster, DisasterStacks, HostileTargetReference.All),
            AddCardsStep(SkillID.PlagueStatus, V("PlagueCount", 2))
        );
    }
}
