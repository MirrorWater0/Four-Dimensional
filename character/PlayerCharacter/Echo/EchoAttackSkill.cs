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

    public override async Task Effect()
    {
        await base.Effect();
        Character[] targets = Chosetarget1();
        int hitCount = Math.Min(MaxTargets, targets.Length);
        for (int i = 0; i < hitCount; i++)
        {
            _ = Attack3(OwnerPower, targets[i], 1);
        }
        OwnerCharater.UpdataBlock(OwnerSurvivability + BlockPerTarget * hitCount);
        await Task.Delay(400);
    }

    public override void UpdateDescription()
    {
        int targetCount = IsInBattle ? Math.Min(MaxTargets, Chosetarget1().Length) : 0;
        int totalBlock = IsInBattle ? (OwnerSurvivability + BlockPerTarget * targetCount) : 0;

        string targetCountText = WithBattleTotal("目标数", targetCount);
        string perTargetDamageText = XWithBattleTotal(StatX.Power, OwnerPower);
        string totalBlockText = WithBattleTotal(
            $"{X(StatX.Survivability)}+目标数*{BlockPerTarget}",
            totalBlock,
            clampMax: 999
        );

        SetDescriptionLines(
            $"最多{MaxTargets}个目标。",
            $"当前命中{targetCountText}个。",
            $"每个目标造成{perTargetDamageText}点伤害。",
            $"获得：{totalBlockText}点格挡。"
        );
    }
}

public partial class ResonantSlash : Skill
{
    private const int BaseDamage = 3;

    public ResonantSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "共振斩击";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack2(BaseDamage + OwnerPower);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        SetDescriptionLines($"二段攻击。", $"每段造成{damageText}点伤害。");
    }
}

public partial class EchoPuncture : Skill
{
    private const int BaseDamage = 4;
    private const int VulnerableStacks = 2;

    public EchoPuncture()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声穿刺";

    public override async Task Effect()
    {
        await base.Effect();

        await Attack1(BaseDamage + OwnerPower);
        var targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        HurtBuff.BuffAdd(Buff.BuffName.Vulnerable, targets[0], VulnerableStacks);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        SetDescriptionLines(
            $"造成{damageText}点伤害。",
            $"给予目标{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}。"
        );
    }
}
