using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音墙";
    private const int BaseBlock = 5;

    public SoundBarrier()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            CarryStep(target: TargetReference.Next, skillIndex: 1)
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 0;

    public SonicDeflection()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "声波偏转";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Self
            ),
            ModifyPropertyStep(PropertyType.Survivability, -3, TargetReference.Self)
        );
    }
}

public partial class DeflectionShield : Skill
{
    private const int BaseBlock = 8;
    private const int DamageImmuneStacks = 1;

    public DeflectionShield()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "偏折之盾";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
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
    private const int BaseBlock = 8;

    public ResonantWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "电磁排斥";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: TargetReference.All
            ),
            BlockStep(baseBlock: BaseBlock)
        );
    }
}

public partial class DissonantField : Skill
{
    private const int BaseBlock = 8;
    private const int WeakenStacks = 2;
    private const int MaxTargets = 2;

    public DissonantField()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "失谐力场";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargets(MaxTargets)
            )
        );
    }
}

public partial class Shelter : Skill
{
    private const int BaseBlock = 0;
    private const int CardRefreshStacks = 1;

    public Shelter()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护幕";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 1),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: CardRefreshStacks,
                target: TargetReference.All
            )
        );
    }
}
