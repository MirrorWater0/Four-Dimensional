using Godot;

public partial class KasiyaSpecialSkill : Node { }

public partial class ReadyStance : Skill
{
    private const int EnergyGain = 5;

    public override string SkillName { get; set; } = "能量爆发";
    public override int EnergyCost => 3;

    public ReadyStance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, EnergyStep(EnergyGain));
    }
}

public class HolySeal : Skill
{
    private const int StunStacks = 1;

    public HolySeal()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣光封印";
    public override int EnergyCost => 2;

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
    private const int BarricadeStacks = 1;

    public AegisPledge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "壁垒誓约";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Barricade,
                stacks: BarricadeStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class HopeBeacon : Skill
{
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

public class DemonForm : Skill
{
    private const int DemonStacks = 5;

    public DemonForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恶魔形态";
    public override int EnergyCost => 4;
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
