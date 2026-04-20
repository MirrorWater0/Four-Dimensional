using System;
using System.Linq;
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

public class VulnerabilityConversion : Skill
{
    private const int EnergyCost = 2;
    private const string TargetKey = "vulnerability_conversion_target";

    public VulnerabilityConversion()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "易伤转化";

    private static int GetVulnerableStacks(Character target) =>
        target
            ?.HurtBuffs?.FirstOrDefault(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Vulnerable && buff.Stack > 0
            )
            ?.Stack ?? 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 2, storeAs: TargetKey),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ModifyPropertyStep(
                    PropertyType.Power,
                    _ => GetVulnerableStacks(GetStoredTarget(TargetKey)),
                    RelativeTarget(0)
                ),
                TextStep($"获得等同于目标{Buff.BuffName.Vulnerable.GetDescription()}层数的力量。")
            )
        );
    }
}
