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
        int targetCount = OwnerCharater == null ? 0 : Math.Min(MaxTargets, Chosetarget1().Length);
        int block = Math.Clamp(BlockPerTarget * targetCount, 0, 999);

        SetDescriptionLines(
            $"最多{MaxTargets}个目标；当前命中{targetCount}个；每个目标造成{Math.Clamp(OwnerPower, 0, 9999)}点伤害。",
            $"获得：生存+目标数*{BlockPerTarget}点格挡，共{block}点格挡。"
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
        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);
        SetDescriptionLines($"二段攻击；每段造成{damage}点伤害。");
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
        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);
        SetDescriptionLines(
            $"造成{damage}点伤害。"
                + $"给予目标{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}"
        );
    }
}
