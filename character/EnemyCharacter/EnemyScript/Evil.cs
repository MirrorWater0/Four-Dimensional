using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    private const int StartEnergyGain = 1;
    private const int StartRebirthStacks = 1;
    private const int TriggerCount = 3;

    public const string PassiveNameText = "重生律动";
    public static string PassiveBaseDescriptionText =>
        $"初始：获得{StartEnergyGain}点能量。获得{StartRebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}。\n"
        + $"每行动{TriggerCount}次：获得{StartRebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}。";

    private int Count = 0;
    private string _basePassiveDescription;
    public override string CharacterName { get; set; } = "Evil";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveBaseDescriptionText;
        _basePassiveDescription = PassiveBaseDescriptionText;
        UpdatePassiveDescription();
        using var _ = BeginEffectSource("被动");
        UpdataEnergy(StartEnergyGain, this);
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, StartRebirthStacks, this);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        Count++;
        TriggerPassive(null);
        UpdatePassiveDescription();
    }

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (Count >= TriggerCount)
        {
            Count = 0;
            DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, StartRebirthStacks, this);
        }
    }

    private void UpdatePassiveDescription()
    {
        if (string.IsNullOrWhiteSpace(_basePassiveDescription))
            _basePassiveDescription = PassiveDescription ?? string.Empty;

        PassiveDescription = $"{_basePassiveDescription}\n当前计数：{Count}/{TriggerCount}";
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

        MaxLife = 45;
        Power = 10;
        Survivability = 7;
        Speed = 5;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.EvilAttack, SkillID.EvilSurvive, SkillID.EvilTermin];

        PassiveName = global::Evil.PassiveNameText;
        PassiveDescription = global::Evil.PassiveBaseDescriptionText;
    }
}

public partial class EvilAttack : Skill
{
    private const int HitDamage = 2;

    public EvilAttack()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "流影二段";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(HitDamage, times: 2),
            ModifyPropertyStep(PropertyType.Power, 1)
        );
    }
}

public partial class EvilSurvive : Skill
{
    private const int SurvivabilityGain = 3;
    private const int BaseBlock = 8;
    private const int DescendingNum = 4;

    public EvilSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "扭曲";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            LowerTargetPropertyStep(PropertyType.Survivability, DescendingNum)
        );
    }
}

public partial class EvilTermin : Skill
{
    private const int PaidEnergyPerHit = 1;

    public EvilTermin()
        : base(Skill.SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "虚空终结";
    public override int EnergyCost => XEnergyCost;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, clampMax: 9999),
            EnergyTimesWhileStep(
                paidEnergyPerLoop: PaidEnergyPerHit,
                loopSteps: [AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, clampMax: 9999)]
            )
        );
    }
}
