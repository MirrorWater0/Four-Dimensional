using Godot;

public partial class KasiyaSpecialSkill : Node { }

public partial class BattleCommand : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } =
        I18n.Tr("skill.battle_command.name", "战术号令");

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TriggerInstalledCardsStep(target: TargetReference.ManualFriendly,
                exhaustAfterTrigger: true)
        );
    }
}

public partial class ReadyStance : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "能量爆发";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DoubleEnergyStep(),
            AddCardsStep(
                SkillID.VoidStatus,
                V("VoidCount"),
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}

public class HolySeal : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int StunStacks => V(nameof(StunStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "圣光封印";

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
    private int ExtraDrawStacks => V(nameof(ExtraDrawStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "战术整备";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount")),
            AddCardsStep(
                SkillID.VoidStatus,
                V("VoidCount"),
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
    public override SkillRarity Rarity => SkillRarity.Common;
    private int DazeCount => V(nameof(DazeCount));
    private int EnergyGain => V(nameof(EnergyGain));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "辉光";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddCardsStep(SkillID.DazeStatus, DazeCount),
            AddCardsStep(SkillID.DazeStatus, V("DiscardDazeCount"), BattleCardPileTarget.DiscardPileCards),
            EnergyStep(EnergyGain)
        );
    }
}
