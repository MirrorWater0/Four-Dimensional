using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音墙";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddCardsToHandStep(SkillID.DefenceFocus, 3, TargetReference.ManualFriendly)
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 0;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "声波偏转";
    public override int EnergyCost => 2;
    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.ManualFriendly
            ),
            ModifyPropertyStep(PropertyType.Survivability, -3, TargetReference.Self)
        );
    }
}

public partial class DeflectionShield : Skill
{
    private const int BaseBlock = 6;
    private const int DamageImmuneStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "偏折之盾";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 1;
    private const int BaseBlock = 6;
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "电磁排斥";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: TargetReference.All
            )
        );
    }
}

public partial class DissonantField : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int WeakenStacks = 2;
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "失谐力场";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class Shelter : Skill
{
    private const int BaseBlock = 4;
    private const int CardRefreshStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "护幕";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: CardRefreshStacks,
                target: TargetReference.Others
            )
        );
    }
}
