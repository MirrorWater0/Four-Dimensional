using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class VeilStep : Skill
{
    private const int InvisibleStacks = 3;
    private const int BaseBlock = 8;

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
                index: 0,
                dyingFilter: false
            ),
            BlockFriendlyByRelativeStep(relativeIndex: 1, baseBlock: BaseBlock, dyingFilter: true),
            HealFriendlyRelative(baseHeal: 5, index: 0)
        );
    }
}

public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 3;
    private const int BaseBlock = 8;
    int Inpower = 4;

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
                maxTargets: 1
            ),
            BlockFriendlyByRelativeStep(0, BaseBlock),
            ModifyPropertyStep(PropertyType.Power, Inpower)
        );
    }
}

public partial class Swift : Skill
{
    private const int SpeedGain = 2;
    private const int SurvivabilityGain = 3;

    public Swift()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "迅捷";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Speed, SpeedGain),
            ModifyPropertyAbsoluteStep(
                type: PropertyType.Survivability,
                value: SurvivabilityGain,
                selector: PropertyAbsoluteSelector.All,
                dyingFilter: true
            )
        );
    }
}

public partial class StarWard : Skill
{
    private const int BaseBlock = 15;
    private const int ExtraPowerStacks = 2;

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
            BlockFriendlyByRelativeStep(
                relativeIndex: -1,
                baseBlock: BaseBlock,
                survivabilityMultiplier: 0
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraPower,
                stacks: ExtraPowerStacks,
                index: 0,
                dyingFilter: false
            )
        );
    }
}
