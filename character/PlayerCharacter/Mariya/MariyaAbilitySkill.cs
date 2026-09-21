public partial class MariyaAbilitySkill { }

public partial class Prediction : AbilitySkill
{
    private const int PredictionStacks = 3;
    public override int EnergyCost => Cost(1);

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
    private const int SourceStacks = 1;

    public override string SkillName { get; set; } = "至圣";
    public override int EnergyCost => Cost(1);

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
    private const int SanctuaryStacks = 1;

    public override string SkillName { get; set; } = "圣域形态";
    public override int EnergyCost => Cost(4);

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
    private const int ForesightStacks = 1;

    public override string SkillName { get; set; } = "预见";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(buffName: Buff.BuffName.Foresight, stacks: ForesightStacks)
        );
    }
}
