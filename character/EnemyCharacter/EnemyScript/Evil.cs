using System;
using System.Threading.Tasks;
using Godot;

public partial class Evil : EnemyCharacter
{
    private const int TriggerCount = 3;
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
        _basePassiveDescription ??= PassiveDescription ?? string.Empty;
        UpdatePassiveDescription();
        UpdataEnergy(1);
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, 1);
    }

    public override void StartAction()
    {
        Count++;
        Passive(null);
        UpdatePassiveDescription();
        base.StartAction();
    }

    public override void Passive(Skill skill)
    {
        if (Count >= TriggerCount)
        {
            Count = 0;
            DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, 1);
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
    private const int HitDamage = 5;

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
            BlockFriendlyByRelativeStep(0, BaseBlock),
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

    public override string SkillName { set; get; } = "回响时刻";

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
