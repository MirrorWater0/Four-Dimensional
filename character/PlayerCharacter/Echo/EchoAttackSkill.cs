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
        SetDescriptionText(
            $"最多攻击{MaxTargets}个目标，每个目标先受到1次{Math.Clamp(OwnerPower, 0, 9999)}点伤害；若能量大于0，则额外消耗{ExtraRoundCost}点能量再攻击1轮。"
        );
    }
}
