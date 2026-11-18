using Godot;

public partial class KasiyaAbilitySkill : Node { }

public class Recycling : AbilitySkill
{
    public override string SkillName { get; set; } = "循环利用";

    public override SkillRarity Rarity => SkillRarity.Rare;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Recycling,
                stacks: 1,
                target: TargetReference.Self
            )
        );
    }
}

public class AegisPledge : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int BarricadeStacks => V(nameof(BarricadeStacks));

    public override string SkillName { get; set; } = "壁垒";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Barricade,
                stacks: BarricadeStacks,
                target: TargetReference.All
            )
        );
    }
}

public class HopeBeacon : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BeaconStacks => V(nameof(BeaconStacks));

    public override string SkillName { get; set; } = "希望灯塔";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Beacon,
                stacks: BeaconStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class WarGodWill : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int PowerGain => V(nameof(PowerGain));

    public override string SkillName { get; set; } = "战神意志";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain, TargetReference.All)
        );
    }
}

public class DemonForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int DemonStacks => V(nameof(DemonStacks));

    public override string SkillName { get; set; } = "恶魔形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Demon,
                stacks: DemonStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class ExhaustBulwark : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int ExhaustShieldStacks => V(nameof(ExhaustShieldStacks));

    public override string SkillName { get; set; } = "烬盾誓约";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExhaustShield,
                stacks: ExhaustShieldStacks,
                target: TargetReference.Self
            )
        );
    }
}
