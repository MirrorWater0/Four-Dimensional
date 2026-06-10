using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class VeilStep : Skill
{
    private const int InvisibleStacks = 3;
    private const int BaseBlock = 6;
    public override int EnergyCost => 0;

    public VeilStep()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "夜幕潜行";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            ),
            BlockStep(target: TargetReference.Next, baseBlock: BaseBlock, multiplier: 1)
        );
    }
}

public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 0;

    public FlashOfLight()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "闪耀之光";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            ),
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock)
        );
    }
}

public partial class AfterimageWard : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int AfterimageStacks = 1;

    public AfterimageWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "月落残影";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            BlockStep(target: TargetReference.Next, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Afterimage,
                stacks: AfterimageStacks,
                target: TargetReference.All
            )
        );
    }
}

public partial class StarWard : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int ExtraPowerStacks = 2;
    public override int EnergyCost => 2;

    public StarWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "星辉守势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraPower,
                stacks: ExtraPowerStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class TwilightParadox : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int VulnerableStacks = 9;
    private const int selfStacks = 3;
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;

    public TwilightParadox()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "暮光悖论";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: 2),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Vulnerable,
                stacks: selfStacks,
                target: TargetReference.Self
            )
        );
    }
}
