using Godot;

public partial class KasiyaAbilitySkill : Node { }

public class Recycling : AbilitySkill
{
    private const int RecyclingStacks = 1;

    public override string SkillName { get; set; } = "循环利用";
    public override int EnergyCost => Cost(3);
    public override SkillRarity Rarity => SkillRarity.Rare;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Recycling,
                stacks: RecyclingStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class AegisPledge : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BarricadeStacks = 1;

    public override string SkillName { get; set; } = "壁垒";
    public override int EnergyCost => Cost(3);

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
    private const int BeaconStacks = 2;

    public override string SkillName { get; set; } = "希望灯塔";
    public override int EnergyCost => Cost(1);

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
    private const int PowerGain = 3;
    public override int EnergyCost => Cost(2);
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
    private const int DemonStacks = 1;

    public override string SkillName { get; set; } = "恶魔形态";
    public override int EnergyCost => Cost(4);

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
    private const int ExhaustShieldStacks = 2;

    public override string SkillName { get; set; } = "烬盾誓约";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExhaustShield,
                stacks: ExhaustShieldStacks,
                target: TargetReference.All
            )
        );
    }
}
