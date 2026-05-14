using System;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    private const int PaidEnergyPerCast = 1;
    private const int PowerGainPerCast = 1;

    public EchonicResonance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响共鸣";
    public override int EnergyCost => XEnergyCost;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 1),
            EnergyTimesWhileStep(
                paidEnergyPerLoop: PaidEnergyPerCast,
                loopSteps:
                [
                    AttackPrimaryStep(baseDamage: -2, powerMultiplier: 1),
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerCast),
                ]
            )
        );
    }
}

public class SonicBoom : Skill
{
    private const int BaseDamage = 0;
    private const int ExtraTimes = 2;

    public SonicBoom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "音爆";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: 3, target: HostileTargets(0)),
            AoeDamageStep(baseDamage: BaseDamage, target: HostileTargets(0), times: ExtraTimes)
        );
    }
}

public class PhaseEcho : Skill
{
    int damage = 17;
    int PowerGain = -2;

    public PhaseEcho()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "相位回声";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AoeDamageStep(baseDamage: damage),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public class ReverbChain : Skill
{
    private const int BaseDamage = -5;

    public ReverbChain()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声连奏";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep("释放x次(x为本场战斗中其他己方角色的行动次数)。"),
            EnergyTimesWhileStep(
                times: GetLoopTimes,
                loopSteps: [AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 1)]
            )
        );
    }

    private int GetLoopTimes() =>
        OwnerCharater?.BattleNode?.GetAlliedActionCountExcludingSelf(OwnerCharater) ?? 0;
}

public class VoidForm : Skill
{
    private const int VoidStacks = 2;

    public VoidForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚无形态";
    public override int EnergyCost => 4;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Void,
                stacks: VoidStacks,
                target: TargetReference.Self
            )
        );
    }
}

public class EchoForm : Skill
{
    private const int EchoStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public EchoForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响形态";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Echo,
                stacks: EchoStacks,
                target: TargetReference.Self
            )
        );
    }
}
