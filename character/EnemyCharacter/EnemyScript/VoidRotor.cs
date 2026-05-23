using Godot;

public partial class VoidRotor : EnemyCharacter
{
    private const int PassiveWeakenStacks = 1;

    public const string PassiveNameText = "虚空蚀咒";
    public static string PassiveDescriptionText =>
        $"回合开始时：给予敌方全阵{PassiveWeakenStacks}层{Buff.BuffName.Weaken.GetDescription()}。";

    public override string CharacterName { get; set; } = "VoidRotor";

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

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        Character[] targets = ChooseHostileTargetsByOrder(returnDummyWhenEmpty: false);
        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (target == null)
                continue;

            AttackBuff.BuffAdd(Buff.BuffName.Weaken, target, PassiveWeakenStacks, this);
        }
    }
}

public partial class VoidRotorRegedit : EnemyRegedit
{
    public VoidRotorRegedit()
    {
        CharacterName = "VoidRotor";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/VoidRotor.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/VoidRotor.tscn");

        MaxLife = 70;
        Power = 9;
        Survivability = 20;
        Speed = 9;
        SkillIDs = [SkillID.VoidRotorAttack, SkillID.VoidRotorSurvive, SkillID.VoidRotorSpecial];

        PassiveName = global::VoidRotor.PassiveNameText;
        PassiveDescription = global::VoidRotor.PassiveDescriptionText;
    }
}

public partial class VoidRotorAttack : Skill
{
    private const int BaseDamage = 0;
    private const int PowerMultiplier = 1;

    public VoidRotorAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "裂刃斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: PowerMultiplier, times: 3)
        );
    }
}

public partial class VoidRotorSurvive : Skill
{
    private const int BaseBlock = 0;

    public VoidRotorSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "蚀甲压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            AddStatusCardsToDrawPileStep(SkillID.StunStatus, 2, HostileTargetReference.One)
        );
    }
}

public partial class VoidRotorSpecial : Skill
{
    private const int BaseDamage = 0;
    private const int StunCardsPerTarget = 2;
    private const int RandomTargetCount = 2;

    public VoidRotorSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚界灌注";
    public override int EnergyCost => 5;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1, times: 3),
            AddStatusCardsToDrawPileStep(
                SkillID.StunStatus,
                StunCardsPerTarget,
                HostileTargetReference.Two
            )
        );
    }
}
