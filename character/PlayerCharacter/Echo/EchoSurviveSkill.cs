using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音墙";
    private const int EnergyGain = 1;
    private const int BaseBlock = 5;

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
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = -2;

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
                target: TargetReference.Self
            ),
            ModifyPropertyStep(PropertyType.Survivability, -2, TargetReference.Self)
        );
    }
}

public partial class TuningStance : Skill
{
    private const int BaseBlock = 7;
    public override int EnergyCost => 2;

    public TuningStance()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "韵律";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            EnergyStep(1, TargetReference.All)
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 2;
    private const int BaseBlock = 1;

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
                target: TargetReference.Self
            ),
            BlockStep(0, BaseBlock, 1)
        );
    }
}

public partial class DissonantField : Skill
{
    private const int BaseBlock = 5;
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
    private const int BaseBlock = 2;
    public override int EnergyCost => 2;

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
            EnergyStep(1, TargetReference.Previous),
            CarryStep(target: TargetReference.Previous, skillIndex: 2)
        );
    }
}

public partial class Shelter : Skill
{
    private const int BaseBlock = -3;
    private const int CardRefreshStacks = 1;

    public Shelter()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护幕";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock, survivabilityMultiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.CardRefresh,
                stacks: CardRefreshStacks,
                target: TargetReference.All
            ),
            DrawReserveStep(1)
        );
    }
}
