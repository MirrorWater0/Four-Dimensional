using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 5;

    public override string SkillName { get; set; } = "冲击波";

    public ShockWave()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                maxTargets: 0
            ),
            SelfBlockStep(BaseBlock)
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 5;
    private const int SurvivabilityGain = 5;

    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            SelfBlockStep(baseBlock: 0)
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    private int GainPower = 3;
    public override string SkillName { get; set; } = "绝对防御";
    int basisBlock = 4;

    public AbsouluteDefense()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            SelfBlockStep(basisBlock),
            SelfBlockStep(basisBlock),
            ModifyPropertyStep(PropertyType.Power, GainPower)
        );
    }
}

public partial class TauntingGuard : Skill
{
    private const int TauntStacks = 2;
    private const int BaseBlock = 6;

    public override string SkillName { get; set; } = "嘲讽守势";

    public TauntingGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Taunt,
                stacks: TauntStacks,
                index: 0,
                dyingFilter: false
            ),
            SelfBlockStep(BaseBlock)
        );
    }
}

