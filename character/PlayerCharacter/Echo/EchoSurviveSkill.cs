using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音障防护";
    private const int EnergyGain = 1;
    private const int BaseBlock = 10;
    int times = 2;

    public SoundBarrier()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            BlockStep(0, BaseBlock),
            EnergyTimesGateStep(
                0,
                times,
                CustomStep(
                    async skill =>
                    {
                        await Task.Delay(200);
                        await skill.Carry(skill.GetAllyByRelative(1), 0);
                    },
                    _ => new[] { "连携下一位角色使用攻击技能。" }
                )
            )
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 1;
    private const int BaseBlock = 6;

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
            BlockStep(0, BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: RelativeTarget(0)
            )
        );
    }
}

public partial class TuningStance : Skill
{
    private const int PowerGain = 5;
    private const int BaseBlock = 5;

    public TuningStance()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "韵律姿态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            BlockStep(0, BaseBlock)
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 2;
    private const int BaseBlock = 6;
    int PowerGain = 2;

    public ResonantWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响护佑";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: RelativeTarget(0)
            ),
            BlockStep(0, BaseBlock),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

