using Godot;

public partial class EchoAbilitySkill : Node { }

public class VoidForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int VoidStacks = 1;

    public override string SkillName { get; set; } = "虚无形态";
    public override int EnergyCost => Cost(4);

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

public partial class CursePower : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int CursePowerStacks = 1;

    public override string SkillName { get; set; } = "咒力";
    public override int EnergyCost => Cost(2);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.CursePower,
                stacks: CursePowerStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class WeakeningField : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int WeakeningFieldStacks = 3;

    public override string SkillName { get; set; } = "虚弱立场";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.WeakeningField,
                stacks: WeakeningFieldStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class EternalCore : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int EnergyStorageStacks = 3;

    public override string SkillName { get; set; } = "永恒核心";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.EnergyStorage,
                stacks: EnergyStorageStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class EchoForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int EchoStacks = 1;

    public override string SkillName { get; set; } = "回响形态";
    public override int EnergyCost => Cost(4);

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
