using Godot;

public partial class EchoSpecialSkill : Node { }

public partial class Overdraw : Skill
{
    private const int NoDrawStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "透支";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount", 0)),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.NoDraw,
                stacks: NoDrawStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class TuningStance : Skill
{
    public override int EnergyCost => 1;
    public override bool IntrinsicRetainsAtTurnEndInHand => true;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "韵律";
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, SelectDiscardPileCardsToHandStep(V("SelectDiscardCount", 2)));
    }
}

public partial class RelayShift : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 6)),
            CarryStep(target: TargetReference.Previous, skillIndex: 2),
            CarryStep(target: TargetReference.Previous, skillIndex: 1)
        );
    }
}

public partial class Purity : Skill
{
    private const int EnergyGain = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "纯净";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}
