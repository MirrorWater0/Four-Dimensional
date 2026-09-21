using Godot;

public partial class EchoSpecialSkill : Node { }

public partial class Overdraw : Skill
{
    private const int NoDrawStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "透支";
    public override int EnergyCost => Cost(0);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount", 3)),
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
    public override int EnergyCost => Cost(1);
    public override bool IntrinsicRetainsAtTurnEndInHand => true;

    public override SkillTypes SkillType => SkillTypes.Special;
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "韵律";
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, SelectDiscardPileCardsToHandStep(V("SelectDiscardCount", 2)));
    }
}

public partial class Purity : Skill
{
    private const int EnergyGain = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "纯净";
    public override int EnergyCost => Cost(0);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 0;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "声波偏转";
    public override int EnergyCost => Cost(2);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.ManualFriendly
            ),
            ModifyPropertyStep(
                PropertyType.Survivability,
                -V("SurvivabilityLoss", 3),
                TargetReference.Self
            )
        );
    }
}
