using Godot;

public partial class EchoSpecialSkill : Node { }

public partial class TuningStance : Skill
{
    public override int EnergyCost => 1;
    public override bool IntrinsicRetainsAtTurnEndInHand => true;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "韵律";
    public override bool ExhaustsAfterUse => true;
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, SelectDiscardPileCardsToHandStep(2));
    }
}

public partial class RelayShift : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(2),
            CarryStep(target: TargetReference.Previous, skillIndex: 2)
        );
    }
}

public class VoidForm : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int VoidStacks = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "虚无形态";
    public override int EnergyCost => 4;
    public override bool ExhaustsAfterUse => true;

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

public partial class Purity : Skill
{
    private const int EnergyGain = 2;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "纯净";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}

public partial class CursePower : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int CursePowerStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "咒力";
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;

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

public partial class WeakeningField : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int WeakeningFieldStacks = 3;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "虚弱立场";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

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

public partial class EternalCore : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int EnergyStorageStacks = 3;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "永恒核心";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

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

public class EchoForm : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int EchoStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "回响形态";
    public override int EnergyCost => 4;

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
