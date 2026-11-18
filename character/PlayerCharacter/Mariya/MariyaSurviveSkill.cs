using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int PowerGain => V(nameof(PowerGain));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "终守";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(
                type: PropertyType.Power,
                value: PowerGain,
                target: TargetReference.Self
            )
        );
    }
}

public partial class RebirthPrayer : Skill
{
    private int BaseRebirthHeal => V(nameof(BaseRebirthHeal));
    private int BaseHeal => V(nameof(BaseHeal));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "复苏祷告";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(
                baseHeal: BaseRebirthHeal,
                target: TargetReference.ManualFriendly,
                preferNonFull: true
            ),
            HealStep(baseHeal: BaseHeal, target: TargetReference.LowestLife)
        );
    }
}

public partial class CrystalGuard : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(baseBlock: BaseBlock, target: TargetReference.All, multiplier: V("Multiplier")));
    }
}

public partial class StillWaterMirror : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "明镜止水";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            AddCardsStep(SkillID.Calmness, V("CalmnessCount"))
        );
    }
}

public partial class EnergyRelay : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "能量接续";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: V("BaseBlock"), multiplier: V("Multiplier")),
            ApplyBuffFriendly(Buff.BuffName.NextEnergy, V("NextEnergyStacks"))
        );
    }
}
