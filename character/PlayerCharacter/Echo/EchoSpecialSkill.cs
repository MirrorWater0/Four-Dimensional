using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    private const int CostPerCast = 1;
    private const int PowerGainPerCast = 1;
    int desurive = 1;

    public EchonicResonance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响共鸣";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1),
            EnergyTimesWhileStep(
                energyCost: CostPerCast,
                loopSteps:
                [
                    AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1),
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerCast),
                ]
            )
        );
    }
}

public class SonicBoom : Skill
{
    private const int BaseDamage = 0;
    private const int EnergyCost = 6;
    private const int ExtraTimes = 2;

    public SonicBoom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "音爆";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: 6, maxTargets: 0),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                AoeDamageStep(baseDamage: BaseDamage, maxTargets: 0, times: ExtraTimes)
            )
        );
    }
}

public class PhaseEcho : Skill
{
    int damage = 15;
    int PowerGain = -4;
    int EnergyCost = 1;

    public PhaseEcho()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "相位回声";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                [
                    AoeDamageStep(baseDamage: damage, maxTargets: 4),
                    ModifyPropertyStep(PropertyType.Power, PowerGain),
                ]
            )
        );
    }
}
