using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 5;
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
    public override int EnergyCost => Cost(2);
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
            BlockStep(target: TargetReference.HealKey, baseBlock: V("BaseBlock", 7))
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 5;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, target: TargetReference.All));
    }
}

public partial class StillWaterMirror : Skill
{
    private const int BaseBlock = 7;

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

public partial class EnergyRelay : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override int EnergyCost => Cost(1);

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "能量接续";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: V("BaseBlock", 7)),
            ApplyBuffFriendly(Buff.BuffName.NextEnergy, V("NextEnergyStacks", 2))
        );
    }
}
