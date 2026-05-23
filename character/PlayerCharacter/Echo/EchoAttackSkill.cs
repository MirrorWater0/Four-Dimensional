using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    private const int MaxTargets = 4;
    private const int BlockPerTarget = 4;

    public SacredOnslaught()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣域冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: 0,
                multiplier: 1,
                target: HostileTargets(MaxTargets),
                times: 1
            ),
            CustomStep(
                async __ =>
                {
                    int hitCount = Math.Min(
                        MaxTargets,
                        ChosetargetByOrder(byBehindRow: false).Length
                    );
                    OwnerCharater?.UpdataBlock(
                        Math.Clamp(OwnerSurvivability + BlockPerTarget * hitCount, 0, 999),
                        source: OwnerCharater
                    );
                    await Task.Delay(400);
                },
                __ =>
                {
                    int targetCount = IsInBattle
                        ? Math.Min(MaxTargets, ChosetargetByOrder(byBehindRow: false).Length)
                        : 0;
                    int totalBlock = IsInBattle
                        ? (OwnerSurvivability + BlockPerTarget * targetCount)
                        : 0;

                    string targetCountText = Total("目标数", targetCount);
                    string totalBlockText = WithBattleTotal(
                        $"{X(StatX.Survivability)}+目标数*{BlockPerTarget}",
                        totalBlock,
                        clampMax: 999
                    );

                    return new[]
                    {
                        $"当前命中{targetCountText}个。",
                        $"获得：{totalBlockText}点格挡。",
                    };
                }
            )
        );
    }
}

public partial class ResonantSlash : Skill
{
    private const int BaseDamage = 0;
    private const int UpgradeDamageBonus = 2;

    public ResonantSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "共振斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: UpAdd(BaseDamage, UpgradeDamageBonus), times: 2),
            ApplyBuffHostile(Buff.BuffName.Weaken, 2, HostileTargetReference.One)
        );
    }
}

public partial class EchoPuncture : Skill
{
    private const int BaseDamage = 0;
    private const int VulnerableStacks = 2;

    public EchoPuncture()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声穿刺";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: 2),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            )
        );
    }
}

public partial class Extract : Skill
{
    private const int BaseDamage = 7;

    public Extract()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "萃取";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            EnergyStep(_ => GetTotalHostileWeakenStacks()),
            TextStep(
                I18n.Format(
                    "skill.extract.text.total_weaken_energy",
                    "获得等同于敌方全阵{buff}层数总和的能量。",
                    ("buff", Buff.BuffName.Weaken.GetDescription())
                )
            )
        );
    }

    private static int GetWeakenStacks(Character target) =>
        target
            ?.AttackBuffs?.FirstOrDefault(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Weaken && buff.Stack > 0
            )
            ?.Stack ?? 0;

    private int GetTotalHostileWeakenStacks()
    {
        return OwnerCharater
                ?.BattleNode?.GetOrderedTeamCharacters(
                    !OwnerCharater.IsPlayer,
                    includeSummons: true,
                    dyingFilter: true
                )
                ?.Sum(GetWeakenStacks) ?? 0;
    }
}

public partial class BladeOfSlaughter : Skill
{
    private const int BaseDamage = 7;

    public BladeOfSlaughter()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "弑杀之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            CarryStep(target: TargetReference.Previous, skillIndex: 1)
        );
    }
}

public partial class DisasterImpact : Skill
{
    private const int BaseDamage = 0;
    private const int DisasterStacks = 10;

    public DisasterImpact()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "灾厄冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Disaster,
                stacks: DisasterStacks,
                target: HostileTargetReference.One
            )
        );
    }
}

public class EchonicResonance : Skill
{
    private const int PaidEnergyPerCast = 1;
    private const int PowerGainPerCast = 1;

    public EchonicResonance()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响共鸣";
    public override int EnergyCost => XEnergyCost;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesWhileStep(
                paidEnergyPerLoop: PaidEnergyPerCast,
                loopSteps:
                [
                    AttackStep(baseDamage: 0, multiplier: 1),
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
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "音爆";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: 0,
                target: HostileTargetReference.All
            ),
            AttackStep(
                baseDamage: BaseDamage,
                target: HostileTargetReference.All,
                times: ExtraTimes
            )
        );
    }
}

public class PhaseEcho : Skill
{
    int damage = 10;
    int PowerGain = -2;

    public PhaseEcho()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "相位回声";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: damage,
                target: HostileTargetReference.All
            ),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public class ReverbChain : Skill
{
    private const int BaseDamage = 0;

    public ReverbChain()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声连奏";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep(
                I18n.Tr(
                    "skill.reverb_chain.text.loop_by_allied_actions",
                    "释放x次(x为本场战斗中其他己方角色的行动次数)。"
                )
            ),
            EnergyTimesWhileStep(
                times: GetLoopTimes,
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: 1)]
            )
        );
    }

    private int GetLoopTimes() =>
        OwnerCharater?.BattleNode?.GetAlliedActionCountExcludingSelf(OwnerCharater) ?? 0;
}
