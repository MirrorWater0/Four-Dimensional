using System;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    private const int CostPerCast = 1;
    private const int PowerGainPerCast = 1;

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
            AttackPrimaryStep(baseDamage: 3, powerMultiplier: 1),
            EnergyTimesWhileStep(
                energyCost: CostPerCast,
                loopSteps:
                [
                    AttackPrimaryStep(baseDamage: -3, powerMultiplier: 1),
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerCast),
                ]
            )
        );
    }
}

public class SonicBoom : Skill
{
    private const int BaseDamage = 0;
    private const int EnergyCost = 4;
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
            AoeDamageStep(baseDamage: 6, target: HostileTargets(0)),
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(0), times: ExtraTimes)
            )
        );
    }
}

public class PhaseEcho : Skill
{
    int damage = 20;
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
                    AoeDamageStep(baseDamage: damage),
                    ModifyPropertyStep(PropertyType.Power, PowerGain),
                ]
            )
        );
    }
}

public class ReverbChain : Skill
{
    private const int EnergyCost = 3;
    private const int BaseDamage = -2;

    public ReverbChain()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声连奏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps:
                [
                    TextStep("释放x次(x为本场战斗中其他己方角色的行动次数)。"),
                    EnergyTimesWhileStep(
                        energyCost: 0,
                        times: GetLoopTimes,
                        loopSteps: [AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 1)]
                    ),
                ]
            )
        );
    }

    private int GetLoopTimes() =>
        OwnerCharater?.BattleNode?.GetAlliedActionCountExcludingSelf(OwnerCharater) ?? 0;
}

public class VoidForm : Skill
{
    private const int EnergyCost = 6;
    private const int VoidStacks = 2;

    public VoidForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚无形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.Void,
                    stacks: VoidStacks,
                    target: RelativeTarget(0)
                )
            )
        );
    }
}

public class EchoForm : Skill
{
    private const int EnergyCost = 6;
    private const int EchoStacks = 1;

    public EchoForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响形态";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                EnergyCost,
                null,
                null,
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.Echo,
                    stacks: EchoStacks,
                    target: RelativeTarget(0)
                )
            )
        );
    }
}
