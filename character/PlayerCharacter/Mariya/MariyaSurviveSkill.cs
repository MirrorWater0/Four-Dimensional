using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 7;
    private const int PowerGain = 4;

    public FinalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终守";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockFriendlyByRelativeStep(0, BaseBlock),
            ModifyPropertyStep(
                type: PropertyType.Power,
                value: PowerGain,
                target: AbsoluteTarget(AbsoluteFriendlySelector.BackMost),
                dyingFilter: true
            )
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 7;
    private const int SurvivabilityGain = 4;

    public CrystalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockFriendlyByRelativeStep(relativeIndex: 0, baseBlock: BaseBlock),
            BlockFriendlyByRelativeStep(relativeIndex: -1, baseBlock: BaseBlock),
            BlockFriendlyByRelativeStep(relativeIndex: 1, baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class QuietVeil : Skill
{
    private const int InvisibleStacks = 2;
    private const int MaxLifeGain = 10;
    private const int SurvivabilityGain = 6;
    private const int BaseHeal = 5;

    public QuietVeil()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "静影庇护";

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
            ModifyPropertyStep(PropertyType.MaxLife, MaxLifeGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            HealFriendlyStep(baseHeal: BaseHeal, target: RelativeTarget(0))
        );
    }
}
