public partial class NightingaleAbilitySkill { }

public partial class Search : AbilitySkill
{
    private int SearchStacks => V(nameof(SearchStacks));

    public override string SkillName { get; set; } = "搜寻";
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Search,
                stacks: SearchStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class TempoSurge : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override string SkillName { get; set; } = "疾奏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(
                PropertyType.Survivability,
                V("SurvivabilityGain"),
                TargetReference.All
            )
        );
    }
}

public partial class Swift : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int SwiftStacks => V(nameof(SwiftStacks));

    public override string SkillName { get; set; } = "迅捷";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Swift,
                stacks: SwiftStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class ShadowForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int ShadowStacks => V(nameof(ShadowStacks));

    public override string SkillName { get; set; } = "暗影形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Shadow,
                stacks: ShadowStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class EternalDarkSkill : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int EternalDarkStacks => V(nameof(EternalDarkStacks));
    public override string SkillName { get; set; } = "永暗";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.EternalDark,
                stacks: EternalDarkStacks,
                target: TargetReference.Self
            )
        );
    }
}
