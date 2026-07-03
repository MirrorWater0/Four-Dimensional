using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    private const int StartEnergyGain = 2;
    private const int RebirthStacks = 1;

    public const string PassiveNameText = "重生律动";
    public static string PassiveBaseDescriptionText =>
        $"第1次与第3次回合开始时：获得{RebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}。";

    private int _turnCount;
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
        using var _ = BeginEffectSource("被动");
        BattleNode?.UpdataEnergy(this, StartEnergyGain, this);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        _turnCount++;
        if (_turnCount == 1 || _turnCount == 3)
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
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Evil.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Evil.tscn");

        MaxLife = 53;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive];
        OpeningIntentionSkillIDs = [SkillID.EvilAttack];
        PassiveName = global::Evil.PassiveNameText;
        PassiveDescription = global::Evil.PassiveBaseDescriptionText;
    }
}

public partial class EvilAttack : Skill
{
    private const int HitDamage = 7;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { set; get; } = "流影二段";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(HitDamage, times: 2));
    }
}

public partial class EvilSurvive : Skill
{
    private const int PowerGain = 3;
    private const int BaseBlock = 11;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "扭曲";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class EvilTermin : Skill
{
    private const int AttackTimes = 5;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { set; get; } = "虚空终结";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => AttackTimes,
                loopSteps: [AttackStep(baseDamage: 7, multiplier: 1, clampMax: 9999)]
            )
        );
    }
}
