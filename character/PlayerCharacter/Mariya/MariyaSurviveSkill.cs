using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 5;
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
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(
                type: PropertyType.Power,
                value: PowerGain,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class RebirthPrayer : Skill
{
    private const int BaseRebirthHeal = 10;

    public RebirthPrayer()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "复苏祷告";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.ManualFriendly,
                preferNonFull: true,
                rebirth: true
            ),
            BlockStep(target: TargetReference.HealKey, baseBlock: 0),
            ModifyPropertyStep(
                target: TargetReference.HealKey,
                type: PropertyType.MaxLife,
                value: 8
            )
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 5;

    public CrystalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, target: TargetReference.All));
    }
}

public partial class StillWaterMirror : Skill
{
    private const int BaseBlock = 8;
    private const int SurvivabilityGain = 4;

    public StillWaterMirror()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "明镜止水";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(
                type: PropertyType.Survivability,
                value: SurvivabilityGain,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class QuietVeil : Skill
{
    private const int InvisibleStacks = 2;
    private const int MaxLifeGain = 8;
    private const int SurvivabilityGain = 3;
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
                target: TargetReference.Self
            ),
            ModifyPropertyStep(PropertyType.MaxLife, MaxLifeGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            HealStep(baseHeal: BaseHeal, target: TargetReference.Self)
        );
    }
}

public partial class EnergyRelay : Skill
{
    public EnergyRelay()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "能量接续";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: 0),
            EnergyStep(delta: 1, target: TargetReference.Next),
            EnergyStep(delta: 1, target: TargetReference.Previous)
        );
    }
}

public partial class TouchOfGod : Skill
{
    private const int BaseBlock = 0;
    private const int DivinityStacks = 1;
    public override int EnergyCost => 1;

    public TouchOfGod()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "上帝之触";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.Self
            )
        );
    }
}
