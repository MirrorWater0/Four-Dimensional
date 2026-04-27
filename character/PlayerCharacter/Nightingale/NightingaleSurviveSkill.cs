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
                target: RelativeTarget(0)
            ),
            BlockStep(relativeIndex: 1, baseBlock: BaseBlock, survivabilityMultiplier: 2)
        );
    }
}

public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 8;
    int Inpower = 3;

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
                target: HostileTargetsEachRowFirst()
            ),
            BlockStep(0, BaseBlock),
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
            ModifyPropertyStep(
                type: PropertyType.Survivability,
                value: SurvivabilityGain,
                target: AbsoluteTarget(AbsoluteFriendlySelector.All)
            )
        );
    }
}

public partial class AfterimageWard : Skill
{
    private const int BaseBlock = 13;
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
            BlockStep(relativeIndex: 0, baseBlock: BaseBlock),
            BlockStep(relativeIndex: 1, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Afterimage,
                stacks: AfterimageStacks,
                target: RelativeTarget(0)
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Afterimage,
                stacks: AfterimageStacks,
                target: RelativeTarget(1)
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
            BlockStep(relativeIndex: -1, baseBlock: BaseBlock, survivabilityMultiplier: 0),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraPower,
                stacks: ExtraPowerStacks,
                target: RelativeTarget(0)
            )
        );
    }
}

public partial class TwilightParadox : Skill
{
    private const int BaseBlock = 14;
    private const int VulnerableStacks = 15;
    private const int DamageImmuneStacks = 5;

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
            BlockStep(relativeIndex: -1, baseBlock: BaseBlock),
            BlockStep(relativeIndex: -2, baseBlock: BaseBlock),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(1)
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: HostileTargets(1)
            )
        );
    }
}
