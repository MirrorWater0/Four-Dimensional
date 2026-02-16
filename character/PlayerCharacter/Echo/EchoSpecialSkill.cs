using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoSpecialSkill : Node { }

public class EchonicResonance : Skill
{
    private const int CostPerCast = 1;
    private const int PowerGainPerCast = 2;

    public EchonicResonance()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响共鸣";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(OwnerPower);
        while (OwnerCharater.Energy > 0)
        {
            await Attack1(OwnerPower);
            IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGainPerCast);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-CostPerCast);
        }
    }

    public override void UpdateDescription()
    {
        int energy = Math.Max(OwnerCharater?.Energy ?? 0, 0);
        int castTimes = Math.Max(1, (int)Math.Ceiling((double)energy / CostPerCast));
        int totalPowerGain = castTimes * PowerGainPerCast;
        SetDescriptionLines(
            $"施放{castTimes}次；每次造成{Math.Clamp(OwnerPower, 0, 9999)}点伤害；每次消耗{CostPerCast}点能量。",
            $"每次获得{PowerGainPerCast}点{GetColoredPropertyLabel(PropertyType.Power)}（总计{totalPowerGain}点）。"
        );
    }
}
