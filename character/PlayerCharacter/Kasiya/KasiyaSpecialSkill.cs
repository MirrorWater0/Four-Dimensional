using System;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSpecialSkill : Node { }

public class TerminateLight : Skill
{
    private const int BaseDamage = 10;
    private int UsedTimes = 1;
    private const int EnergyCost = 2;
    private const int PowerGain = 5;

    public TerminateLight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终末之光";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(BaseDamage + 2 * OwnerPower);
        if (UsedTimes > 0 && OwnerCharater.Energy >= EnergyCost)
        {
            IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGain);
            OwnerCharater.UpdataEnergy(-EnergyCost);
            UsedTimes--;
        }
    }

    public override void UpdateDescription()
    {
        int damage = Math.Clamp(BaseDamage + 2 * OwnerPower, 0, 9999);
        int thisCastPowerBonusTimes =
            UsedTimes > 0 && (OwnerCharater?.Energy ?? 0) >= EnergyCost ? 1 : 0;
        int thisCastPowerBonus = thisCastPowerBonusTimes * PowerGain;
        SetDescriptionText(
            $"造成{damage}点伤害。本次战斗力加成触发{thisCastPowerBonusTimes}次（+{thisCastPowerBonus}）；剩余强化机会{UsedTimes}次。触发条件：能量不少于{EnergyCost}。"
        );
    }
}
