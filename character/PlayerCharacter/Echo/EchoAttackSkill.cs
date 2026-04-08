using System;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    private const int MaxTargets = 4;
    private const int BlockPerTarget = 5;

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
            AoeDamageStep(baseDamage: 0, powerMultiplier: 1, maxTargets: MaxTargets, times: 1),
            CustomStep(
                async __ =>
                {
                    int hitCount = Math.Min(MaxTargets, ChosetargetByOrder(byBehindRow: false).Length);
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
    private const int BaseDamage = 3;
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
            DoubleStrikeStep(baseDamage: UpAdd(BaseDamage, UpgradeDamageBonus))
        );
    }
}

public partial class EchoPuncture : Skill
{
    private const int BaseDamage = 10;
    private const int VulnerableStacks = 2;
    int times = 1;
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
            AttackPrimaryStep(baseDamage: BaseDamage),
            EnergyTimesGateStep(
                0,
                times,
                DoubleStrikeStep(baseDamage: 0)
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                maxTargets: 1
            )
        );
    }
}
