using Godot;

public partial class Death : EnemyCharacter
{
    private static int DisasterStacks => EnemyTuning.GetRequiredInt("Death", nameof(DisasterStacks));

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
            targets.Length > 0 ? BattleNode?.GetEffectRandom(this).Next(targets.Length) ?? 0 : -1;
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
        PortaitPath = "res://asset/EnemyCharater/Death.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Death.tscn");
        SkillIDs = [SkillID.DeathAttack, SkillID.DeathSurvive, SkillID.DeathSpecial, SkillID.DeathDisarm];
        // 第三轮死寂施加全阵脆弱，跨过断魂衔接下一轮双魂裁决。
        IntentionCycle = [SkillID.DeathAttack, SkillID.DeathSpecial, SkillID.DeathSurvive, SkillID.DeathDisarm];
        IntentionCycleInsertions = [(SkillID.DeathFortify, 10)];

        PassiveName = global::Death.PassiveNameText;
        PassiveDescription = global::Death.PassiveDescriptionText;
    }
}

public partial class DeathAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int HitCount => V(nameof(HitCount));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "双魂裁决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, times: HitCount, multiplier: V("Multiplier")),
            ApplyBuffHostile(Buff.BuffName.Weaken, V("WeakenStacks"),target: HostileTargetReference.AttackKey),
            AddCardsStep(SkillID.WoundStatus,V("statusNum"),BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public partial class DeathSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int Heal => V(nameof(Heal));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "死寂";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            HealStep(Heal, target: TargetReference.Self),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.All)
        );
    }
}

public partial class DeathSpecial : Skill
{
    private int SelfPowerGain => V(nameof(SelfPowerGain));
    private int DisasterStacks => V(nameof(DisasterStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "终焉宣告";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ApplyBuffHostile(Buff.BuffName.Disaster, DisasterStacks, HostileTargetReference.All),
            AddCardsStep(SkillID.PlagueStatus, V("PlagueCount"))
        );
    }
}

public partial class DeathDisarm : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "断魂";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            UninstallAttackTargetActionCardsStep()
        );
    }
}

public partial class DeathFortify : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int SelfPowerGain => V(nameof(SelfPowerGain));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "终末武装";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, target: TargetReference.Self, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain, target: TargetReference.Self)
        );
    }
}
