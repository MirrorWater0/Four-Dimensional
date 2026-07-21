using Godot;

public partial class KasiyaSpecialSkill : Node { }

public partial class ReadyStance : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "能量爆发";
    public override int EnergyCost => 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DoubleEnergyStep(),
            AddCardsStep(
                SkillID.VoidStatus,
                V("VoidCount", 1),
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}

public class HolySeal : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int StunStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "圣光封印";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                target: HostileTargetReference.One
            )
        );
    }
}

public class TacticalPreparation : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int ExtraDrawStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "战术整备";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount", 2)),
            AddCardsStep(
                SkillID.VoidStatus,
                V("VoidCount", 1),
                BattleCardPileTarget.DiscardPileCards
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: ExtraDrawStacks,
                target: TargetReference.All
            )
        );
    }
}

public class RadiantOverload : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int DazeCount = 1;
    private const int EnergyGain = 3;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "辉光";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddCardsStep(SkillID.DazeStatus, DazeCount),
            EnergyStep(EnergyGain)
        );
    }
}
