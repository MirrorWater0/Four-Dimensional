using System.Linq;
using Godot;

public partial class Havoc : EnemyCharacter
{
    private const int PassiveTriggerInterval = 3;
    private const int PassiveDisasterStacks = 6;
    private int _turnStartCount;

    public const string PassiveNameText = "灾厄脉冲";
    public static string PassiveDescriptionText =>
        $"每{PassiveTriggerInterval}回合开始时：血量最高的敌人获得{PassiveDisasterStacks}层{Buff.BuffName.Disaster.GetDescription()}。";

    public override string CharacterName { get; set; } = "Havoc";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        _turnStartCount++;
        if (_turnStartCount % PassiveTriggerInterval == 0)
            TriggerPassive(null);
    }

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        Character target = ChooseHostileTargetsByOrder(
            returnDummyWhenEmpty: false,
            normalOnly: true,
            dyingFilter: true
        )
            .OrderByDescending(character => character.Life)
            .ThenBy(character => character.PositionIndex)
            .FirstOrDefault();
        if (target == null)
            return;

        EndActionBuff.BuffAdd(Buff.BuffName.Disaster, target, PassiveDisasterStacks, this);
    }
}

public partial class HavocRegedit : EnemyRegedit
{
    public HavocRegedit()
    {
        CharacterName = "Havoc";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Havoc_v4.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Havoc.tscn");

        MaxLife = 345;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.HavocAttack, SkillID.HavocSurvive, SkillID.HavocSpecial];

        PassiveName = global::Havoc.PassiveNameText;
        PassiveDescription = global::Havoc.PassiveDescriptionText;
    }
}

public partial class HavocAttack : Skill
{
    private const int BaseDamage = 11;
    private const int WeakenStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂壳横扫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, times: V("HitCount", 2)));
    }
}

public partial class HavocSurvive : Skill
{
    private const int BaseBlock = 22;
    private const int SurvivabilityGain = 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "天灾之证";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 2))
        );
    }
}

public partial class HavocSpecial : Skill
{
    private const int DisasterStacks = 7;
    private const int SelfPowerGain = 4;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "崩坏回响";
    public override int EnemySpecialIntentionCooldown => 3;


    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage", 16), multiplier: V("Multiplier", 1), target: HostileTargetReference.All),
            AddCardsStep(SkillID.WoundStatus, V("PlagueCount", 2), BattleCardPileTarget.HandCards),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
