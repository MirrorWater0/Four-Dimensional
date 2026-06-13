using Godot;

public partial class VoidRotor : EnemyCharacter
{
    private const int PassivePowerGain = 1;

    public const string PassiveNameText = "虚空蚀咒";
    public static string PassiveDescriptionText =>
        $"己方阵营回合结束时：己方全阵获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "VoidRotor";

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

    public override void Passive(Skill skill)
    {
        if (BattleNode == null)
            return;

        using var effectSource = BeginEffectSource("被动");
        foreach (Character target in BattleNode.GetTeamCharacters(IsPlayer, includeSummons: true))
        {
            if (target == null || target.State != CharacterState.Normal)
                continue;

            if (BattleNode.QueueEnemyPhaseEndPowerGain(target, PassivePowerGain, this, "被动"))
            {
                continue;
            }

            _ = target.IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
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

        MaxLife = 43;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.VoidRotorAttack, SkillID.VoidRotorSurvive, SkillID.VoidRotorSpecial];

        PassiveName = global::VoidRotor.PassiveNameText;
        PassiveDescription = global::VoidRotor.PassiveDescriptionText;
    }
}

public partial class VoidRotorAttack : Skill
{
    private const int BaseDamage = 4;
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
            AttackStep(baseDamage: BaseDamage, multiplier: PowerMultiplier, times: 3),
            ApplyBuffHostile(Buff.BuffName.Weaken, 1, HostileTargetReference.AttackKey)
        );
    }
}

public partial class VoidRotorSurvive : Skill
{
    private const int BaseBlock = 19;

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
            AddStatusCardsStep(SkillID.DazeStatus, 2)
        );
    }
}

public partial class VoidRotorSpecial : Skill
{
    private const int BaseDamage = 5;
    private const int DazeCardsPerTarget = 2;

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
            AttackStep(baseDamage: BaseDamage, multiplier: 1, times: 2),
            AddStatusCardsStep(
                SkillID.DazeStatus,
                DazeCardsPerTarget,
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}
