using System;
using System.Threading.Tasks;
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

    public override async Task Effect()
    {
        await base.Effect();

        do
        {
            await Attack1(OwnerPower);
            IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGainPerCast);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-CostPerCast);
        } while (OwnerCharater.Energy > 0);
    }

    public override void UpdateDescription()
    {
        int energy = Math.Max(OwnerCharater?.Energy ?? 0, 0);
        int castTimes = Math.Max(1, (int)Math.Ceiling((double)energy / CostPerCast));
        int totalPowerGain = castTimes * PowerGainPerCast;
        SetDescriptionLines(
            $"连续施放：预计施放{castTimes}次。每次造成{Math.Clamp(OwnerPower, 0, 9999)}点伤害；每次施放消耗{CostPerCast}点能量。",
            $"每次施放获得{PowerGainPerCast}点力量（总计{totalPowerGain}点）。"
        );
    }
}
