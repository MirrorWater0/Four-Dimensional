using System;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    private const int ExtraRoundCost = 1;
    private const int MaxTargets = 4;

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
        for (int i = 0; i < Math.Min(MaxTargets, targets.Length); i++)
        {
            _ = Attack3(OwnerPower, targets[i], 1);
        }
        if (OwnerCharater.Energy > 0)
        {
            OwnerCharater.UpdataEnergy(-ExtraRoundCost);
            for (int i = 0; i < Math.Min(MaxTargets, targets.Length); i++)
            {
                _ = Attack3(OwnerPower, targets[i], 1);
            }
        }
        await Task.Delay(400);
    }

    public override void UpdateDescription()
    {
        int targetCount = OwnerCharater == null ? 0 : Math.Min(MaxTargets, Chosetarget1().Length);
        int rounds = (OwnerCharater?.Energy ?? 0) > 0 ? 2 : 1;

        SetDescriptionLines(
            $"最多{MaxTargets}个目标；当前命中{targetCount}个；每轮每个目标造成{Math.Clamp(OwnerPower, 0, 9999)}点伤害。",
            $"当前{rounds}轮；若能量>0，额外消耗{ExtraRoundCost}点能量并追加1轮。"
        );
    }
}
