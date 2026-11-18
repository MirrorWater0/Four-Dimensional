public partial class MariyaAbilitySkill { }

public partial class Prediction : AbilitySkill
{
    private int PredictionStacks => V(nameof(PredictionStacks));

    public override string SkillName { get; set; } = "预测";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Prediction,
                stacks: PredictionStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class HolyOfHolies : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int SourceStacks => V(nameof(SourceStacks));

    public override string SkillName { get; set; } = "至圣";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(Buff.BuffName.Source, SourceStacks)
        );
    }
}

public partial class SanctuaryForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int SanctuaryStacks => V(nameof(SanctuaryStacks));

    public override string SkillName { get; set; } = "圣域形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(buffName: Buff.BuffName.Sanctuary, stacks: SanctuaryStacks)
        );
    }
}

public partial class Foresight : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;

    public override string SkillName { get; set; } = "预见";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(buffName: Buff.BuffName.Foresight, stacks: 1)
        );
    }
}
