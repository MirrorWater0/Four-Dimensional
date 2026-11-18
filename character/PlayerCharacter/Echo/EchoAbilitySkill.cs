using Godot;

public partial class EchoAbilitySkill : Node { }

public class VoidForm : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int VoidStacks => V(nameof(VoidStacks));

    public override string SkillName { get; set; } = "虚无形态";

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
    private int CursePowerStacks => V(nameof(CursePowerStacks));

    public override string SkillName { get; set; } = "咒力";

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
    private int WeakeningFieldStacks => V(nameof(WeakeningFieldStacks));

    public override string SkillName { get; set; } = "虚弱立场";

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
    private int EnergyStorageStacks => V(nameof(EnergyStorageStacks));

    public override string SkillName { get; set; } = "永恒核心";

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
    private int EchoStacks => V(nameof(EchoStacks));

    public override string SkillName { get; set; } = "回响形态";

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
