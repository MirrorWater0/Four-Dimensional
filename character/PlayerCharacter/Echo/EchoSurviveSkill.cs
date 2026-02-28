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
            SelfBlockStep(BaseBlock),
            ConditionGateStep(
                condition: _ => times > 0,
                async skill =>
                {
                    await Task.Delay(200);
                    times--;
                    await skill.Carry(skill.GetAllyByRelative(1), 0);
                },
                describe: _ => new[] { $"连携下一位角色使用攻击技能。剩余{times}次" },
                stopOnFail: false
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
            SelfBlockStep(BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                index: 0,
                dyingFilter: false
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
            SelfBlockStep(BaseBlock)
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
                index: 0,
                dyingFilter: false
            ),
            SelfBlockStep(BaseBlock),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

