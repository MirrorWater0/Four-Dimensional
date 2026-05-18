using Godot;

public partial class EchoSpecialSkill : Node { }

public partial class TuningStance : Skill
{
    public override int EnergyCost => 2;

    public TuningStance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "韵律";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(1, TargetReference.All));
    }
}

public partial class RelayShift : Skill
{
    public override int EnergyCost => 2;

    public RelayShift()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            SwapPositionFriendlyStep(relativeIndexA: 0, relativeIndexB: 1),
            EnergyStep(2, TargetReference.Previous),
            CarryStep(target: TargetReference.Previous, skillIndex: 2)
        );
    }
}

public class VoidForm : Skill
{
    private const int VoidStacks = 2;

    public VoidForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚无形态";
    public override int EnergyCost => 4;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Void,
                stacks: VoidStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class EchoForm : Skill
{
    private const int EchoStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public EchoForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响形态";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Echo,
                stacks: EchoStacks,
                target: TargetReference.Self
            )
        );
    }
}
