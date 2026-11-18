using System.Linq;
using Godot;

public partial class Havoc : EnemyCharacter
{
    private static int PassiveTriggerInterval => EnemyTuning.GetRequiredInt("Havoc", nameof(PassiveTriggerInterval));
    private static int PassiveDisasterStacks => EnemyTuning.GetRequiredInt("Havoc", nameof(PassiveDisasterStacks));
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
        PortaitPath = "res://asset/EnemyCharater/Havoc_v4.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Havoc.tscn");
        SkillIDs = [SkillID.HavocAttack, SkillID.HavocSpecial];
        // 第四轮天灾之证施加全阵脆弱，第五轮崩坏回响全阵爆发。
        IntentionCycle = [SkillID.HavocAttack, SkillID.HavocAttack, random, SkillID.HavocSurvive, SkillID.HavocSpecial, SkillID.HavocAttack];

        PassiveName = global::Havoc.PassiveNameText;
        PassiveDescription = global::Havoc.PassiveDescriptionText;
    }
}

public partial class HavocAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "裂壳横扫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, times: V("HitCount"), multiplier: V("Multiplier")));
    }
}

public partial class HavocSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "天灾之证";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.All)
        );
    }
}

public partial class HavocSpecial : Skill
{

    private int SelfPowerGain => V(nameof(SelfPowerGain));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "崩坏回响";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), target: HostileTargetReference.All, times: V("HitCount")),
            AddCardsStep(SkillID.WoundStatus, V("PlagueCount"), BattleCardPileTarget.HandCards),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
