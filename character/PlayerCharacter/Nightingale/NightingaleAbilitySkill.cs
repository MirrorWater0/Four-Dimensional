public partial class NightingaleAbilitySkill { }

public partial class Search : AbilitySkill
{
    private const int SearchStacks = 1;

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
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(
                PropertyType.Survivability,
                V("SurvivabilityGain", 3),
                TargetReference.All
            )
        );
    }
}

public partial class Swift : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int SwiftStacks = 1;

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
    private const int ShadowStacks = 1;

    public override string SkillName { get; set; } = "暗影形态";
    public override int EnergyCost => 4;

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
    private const int EternalDarkStacks = 2;
    public override string SkillName { get; set; } = "永暗";
    public override int EnergyCost => 1;

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
