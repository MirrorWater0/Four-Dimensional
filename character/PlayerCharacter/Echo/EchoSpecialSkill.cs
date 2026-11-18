using Godot;

public partial class EchoSpecialSkill : Node { }

public partial class FinalResonance : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.final_resonance.name", "终奏");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TriggerInstalledCardsStep(target: TargetReference.ManualFriendly,
                returnToHandAfterTrigger: true)
        );
    }
}

public partial class Overdraw : Skill
{
    private int NoDrawStacks => V(nameof(NoDrawStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "透支";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount")),
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

    public override bool IntrinsicRetainsAtTurnEndInHand => true;

    public override SkillTypes SkillType => SkillTypes.Special;
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "韵律";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, SelectDiscardPileCardsToHandStep(V("SelectDiscardCount")));
    }
}

public partial class Purity : Skill
{
    private int EnergyGain => V(nameof(EnergyGain));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "纯净";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}

public partial class SonicDeflection : Skill
{
    private int DamageImmuneStacks => V(nameof(DamageImmuneStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "声波偏转";

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
                -V("SurvivabilityLoss"),
                TargetReference.Self
            )
        );
    }
}

public partial class HarmonicBarrier : Skill
{
    private int DamageImmuneStacks => V(nameof(DamageImmuneStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "和声屏障";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.All
            )
        );
    }
}
