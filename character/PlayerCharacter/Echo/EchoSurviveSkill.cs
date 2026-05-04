using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音墙";
    private const int EnergyGain = 1;
    private const int BaseBlock = 8;
    int times = 2;

    public SoundBarrier()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            BlockStep(0, BaseBlock),
            EnergyTimesGateStep(0, times, CarryStep(target: RelativeTarget(1), skillIndex: 0))
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 1;

    public SonicDeflection()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "声波偏转";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock, survivabilityMultiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: RelativeTarget(0)
            ),
            ModifyPropertyStep(PropertyType.Survivability, -2, RelativeTarget(0))
        );
    }
}

public partial class TuningStance : Skill
{
    private const int PowerGain = 5;
    private const int BaseBlock = 10;

    public TuningStance()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "韵律姿态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            BlockStep(0, BaseBlock)
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 2;
    private const int BaseBlock = 4;
    int PowerGain = 2;

    public ResonantWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响护佑";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: RelativeTarget(0)
            ),
            BlockStep(0, BaseBlock, 2),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class DissonantField : Skill
{
    private const int BaseBlock = 8;
    private const int WeakenStacks = 2;
    private const int MaxTargets = 2;

    public DissonantField()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "失谐力场";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock, survivabilityMultiplier: 1),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargets(MaxTargets)
            )
        );
    }
}

public partial class RelayShift : Skill
{
    private const int BaseBlock = 5;
    int times = 3;

    public RelayShift()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            SwapPositionFriendlyStep(relativeIndexA: 0, relativeIndexB: 1),
            BlockStep(relativeIndex: -1, baseBlock: BaseBlock),
            EnergyStep(1, RelativeTarget(-1)),
            EnergyStep(-1, RelativeTarget(0)),
            EnergyTimesGateStep(0, times, CarryStep(target: RelativeTarget(-1), skillIndex: 1))
        );
    }
}
