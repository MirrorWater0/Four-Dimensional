using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSpecialSkill : Node { }

public class TerminateLight : Skill
{
    private const int BaseDamage = 5;
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
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 3),
            EnergyTimesGateStep(
                EnergyCost,
                UsedTimes,
                ModifyPropertyStep(PropertyType.Power, PowerGain)
            ),
            HurtFriendly(8, 0),
            ModifyPropertyStep(PropertyType.Power, -2)
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
            EnergyTimesGateStep(
                EnergyCost,
                times,
                ApplyBuffHostile(buffName: Buff.BuffName.Stun, stacks: StunStacks, maxTargets: 1)
            )
        );
    }
}

public class AegisPledge : Skill
{
    private const int PowerGain = 3;
    private const int EnergyCost = 3;
    private const int BarricadeStacks = 1;

    public AegisPledge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "壁垒誓约";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.Barricade,
                    stacks: BarricadeStacks,
                    target: RelativeTarget(0)
                )
            )
        );
    }
}
