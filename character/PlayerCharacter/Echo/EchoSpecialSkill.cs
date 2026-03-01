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
                    LowerTargetPropertyStep(PropertyType.Survivability, desurive),
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
            AoeDamageStep(baseDamage: BaseDamage, maxTargets: 0),
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
    private const int EnergyCost = 2;
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 12;
    int PowerGain = 4;

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
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            CustomStep(
                async skill =>
                {
                    if (OwnerEnergy >= EnergyCost && TrySpendEnergy(EnergyCost))
                    {
                        await SelfBlockStep(BaseBlock).Execute(skill);
                        await ApplyBuffFriendlyAbsolute(
                                buffName: Buff.BuffName.DamageImmune,
                                stacks: DamageImmuneStacks,
                                index: 0,
                                dyingFilter: false
                            )
                            .Execute(skill);
                    }
                    else
                    {
                        await SelfBlockStep(BaseBlock / 2).Execute(skill);
                        await ApplyBuffFriendlyAbsolute(
                                buffName: Buff.BuffName.DamageImmune,
                                stacks: 1,
                                index: 0,
                                dyingFilter: false
                            )
                            .Execute(skill);
                    }

                    await Task.Delay(200);
                },
                _ =>
                {
                    string fullBlockText = BlockFromSurvivabilityText(BaseBlock);
                    string lowBlockText = BlockFromSurvivabilityText(BaseBlock / 2);
                    return new[]
                    {
                        $"若能量>={EnergyCost}：消耗{EnergyCost}点能量。",
                        $"获得{fullBlockText}点格挡。",
                        BuffLine(Buff.BuffName.DamageImmune, DamageImmuneStacks),
                        $"否则：获得{lowBlockText}点格挡。",
                        BuffLine(Buff.BuffName.DamageImmune, 1),
                    };
                }
            )
        );
    }
}
