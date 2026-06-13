using Godot;

public partial class KasiyaSpecialSkill : Node { }

public partial class ReadyStance : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "能量爆发";
    public override int EnergyCost => 1;

    public ReadyStance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DoubleEnergyStep(),
            AddStatusCardsStep(SkillID.VoidStatus, 1, BattleCardPileTarget.DiscardPileCards)
        );
    }
}

public class HolySeal : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int StunStacks = 1;

    public HolySeal()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

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

public class AegisPledge : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BarricadeStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public AegisPledge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "壁垒";
    public override int EnergyCost => 3;

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

public class HopeBeacon : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BeaconStacks = 1;

    public HopeBeacon()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "希望灯塔";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

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

public class WarGodWill : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int PowerGain = 3;

    public WarGodWill()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "战神意志";
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain, TargetReference.All)
        );
    }
}

public class TacticalPreparation : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int ExtraDrawStacks = 1;

    public TacticalPreparation()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "战术整备";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(2),
            AddStatusCardsStep(SkillID.VoidStatus, 1, BattleCardPileTarget.DiscardPileCards),
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

    public RadiantOverload()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "辉光";
    public override int EnergyCost => 1;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddStatusCardsStep(SkillID.DazeStatus, DazeCount),
            EnergyStep(EnergyGain)
        );
    }
}

public class DemonForm : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int DemonStacks = 3;

    public DemonForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恶魔形态";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

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
