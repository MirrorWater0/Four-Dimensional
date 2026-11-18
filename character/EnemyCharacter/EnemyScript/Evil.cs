using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    private static int RebirthStacks => EnemyTuning.GetRequiredInt("Evil", nameof(RebirthStacks));
    private static int PassiveTriggerInterval => EnemyTuning.GetRequiredInt("Evil", nameof(PassiveTriggerInterval));

    public const string PassiveNameText = "重生律动";
    public static string PassiveBaseDescriptionText =>
        $"每{PassiveTriggerInterval}回合开始时：获得{RebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}。";

    private int _turnStartCount;
    public override string CharacterName { get; set; } = "Evil";

    public override void _Ready()
    {
        base._Ready();
    }

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveBaseDescriptionText;
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
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, RebirthStacks, this);
    }
}

public partial class EvilRegedit : EnemyRegedit
{
    public EvilRegedit()
    {
        CharacterName = "Evil";
        PortaitPath = "res://asset/EnemyCharater/Evil.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn");
        SkillIDs = [SkillID.EvilAttack];
        // 前两轮进攻，第三轮扭曲施加全阵脆弱；随机池不提前抽到脆弱。
        IntentionCycle = [SkillID.EvilAttack, SkillID.EvilAttack, SkillID.EvilSurvive, random];
        PassiveName = global::Evil.PassiveNameText;
        PassiveDescription = global::Evil.PassiveBaseDescriptionText;
    }
}

public partial class EvilAttack : Skill
{
    private int HitDamage => V(nameof(HitDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { set; get; } = "流影二段";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(HitDamage, times: V("HitCount"), multiplier: V("Multiplier")));
    }
}

public partial class EvilSurvive : Skill
{
    private int PowerGain => V(nameof(PowerGain));
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "扭曲";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.All)
        );
    }
}

public partial class EvilTermin : Skill
{
    private int AttackTimes => V(nameof(AttackTimes));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { set; get; } = "虚空终结";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => AttackTimes,
                loopSteps: [AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), clampMax: V("ClampMax"), times: V("HitCount"))]
            )
        );
    }
}
