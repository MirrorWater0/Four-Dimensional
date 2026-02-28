using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSpecialSkill : Node { }

public class TerminateLight : Skill
{
    private const int BaseDamage = 10;
    private int UsedTimes = 2;
    private const int EnergyCost = 2;
    private const int PowerGain = 5;

    public TerminateLight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终末之光";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 2),
            ConditionGateStep(
                condition: _ => UsedTimes > 0,
                describe: _ => new[] { $"剩余强化次数：{UsedTimes}。" }
            ),
            EnergyGateStep(EnergyCost, consume: true),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ConditionGateStep(
                condition: _ => true,
                onPass: _ =>
                {
                    UsedTimes--;
                    return Task.CompletedTask;
                },
                describe: _ => Array.Empty<string>(),
                stopOnFail: false
            )
        );
    }
}

public class HolySeal : Skill
{
    private const int BaseDamage = 8;
    private const int StunStacks = 1;
    private const int EnergyCost = 2;
    public int times = 2;

    public HolySeal()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣光封印";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            ConditionGateStep(
                condition: _ => times > 0,
                describe: _ => new[] { $"触发次数：{times}。" }
            ),
            EnergyGateStep(EnergyCost, consume: true),
            ConditionGateStep(
                condition: _ => true,
                onPass: _ =>
                {
                    times--;
                    return Task.CompletedTask;
                },
                describe: _ => Array.Empty<string>(),
                stopOnFail: false
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                maxTargets: 1,
                energyCost: 0
            )
        );
    }
}

