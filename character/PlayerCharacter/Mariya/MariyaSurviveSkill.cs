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
            ModifyPropertyAbsoluteStep(
                type: PropertyType.Power,
                value: PowerGain,
                selector: PropertyAbsoluteSelector.BackMost,
                dyingFilter: true
            )
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 7;
    private const int SurvivabilityGain = 5;

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
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            BlockFriendlyByRelativeStep(relativeIndex: -1, baseBlock: BaseBlock),
            BlockFriendlyByRelativeStep(relativeIndex: 1, baseBlock: BaseBlock)
        );
    }
}
