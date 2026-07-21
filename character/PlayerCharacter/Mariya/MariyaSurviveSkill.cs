using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 4;
    private const int PowerGain = 3;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "终守";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock),
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
    private const int BaseRebirthHeal = 5;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "复苏祷告";
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.ManualFriendly,
                preferNonFull: true
            ),
            BlockStep(target: TargetReference.HealKey, baseBlock: V("BaseBlock", 6))
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 4;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, target: TargetReference.All));
    }
}

public partial class StillWaterMirror : Skill
{
    private const int BaseBlock = 6;
    private const int SurvivabilityGain = 4;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "明镜止水";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount", 2))
        );
    }
}

public partial class QuietVeil : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int InvisibleStacks = 2;
    private const int SurvivabilityGain = 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

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
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class EnergyRelay : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override int EnergyCost => 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "能量接续";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: V("BaseBlock", 6)),
            ApplyBuffFriendly(Buff.BuffName.NextEnergy, V("NextEnergyStacks", 2))
        );
    }
}

public partial class TouchOfGod : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private const int BaseBlock = 0;
    private const int DivinityStacks = 1;
    public override int EnergyCost => 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

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
