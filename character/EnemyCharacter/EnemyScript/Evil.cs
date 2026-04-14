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
        Passive(null);
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

public partial class EvilAttack : Skill
{
    private const int HitDamage = 4;

    public EvilAttack()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "流影二段";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, DoubleStrikeStep(HitDamage));
    }
}

public partial class EvilSurvive : Skill
{
    private const int SurvivabilityGain = 3;
    private const int BaseBlock = 10;
    private const int DescendingNum = 3;

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
            LowerTargetPropertyStep(PropertyType.Power, DescendingNum)
        );
    }
}

public partial class EvilTermin : Skill
{
    private const int EnergyCostPerHit = 1;

    public EvilTermin()
        : base(Skill.SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { set; get; } = "虚空回响";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, clampMax: 9999),
            EnergyTimesWhileStep(
                energyCost: EnergyCostPerHit,
                loopSteps: [AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1, clampMax: 9999)]
            )
        );
    }
}
