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
        int totalDamage = BaseDamage + 2 * OwnerPower;
        string damageText = BasePlusXWithBattleTotal(
            BaseDamage,
            totalDamage,
            StatX.Power,
            xMultiplier: 2,
            clampMax: 999
        );

        int thisCastPowerBonusTimes = UsedTimes > 0 && OwnerEnergy >= EnergyCost ? 1 : 0;

        SetDescriptionLines(
            $"造成{damageText}点伤害（受到2倍{GetColoredPropertyLabel(PropertyType.Power)}加成）。",
            $"若剩余强化>0且能量>={EnergyCost}：消耗{EnergyCost}点能量。",
            $"获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}。",
            $"触发次数：{thisCastPowerBonusTimes}。",
            $"剩余强化次数：{UsedTimes}。"
        );
    }
}

public class HolySeal : Skill
{
    private const int BaseDamage = 8;
    private const int StunStacks = 1;
    private const int EnergyCost = 2;

    public HolySeal()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣光封印";

    public override async Task Effect()
    {
        await base.Effect();

        var targets = Chosetarget1();
        if (targets.Length == 0)
            return;

        var target = targets[0];
        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);

        await AttackAnimation(target);
        await target.GetHurt(damage);
        await Task.Delay(100);

        if (OwnerCharater.Energy < EnergyCost)
            return;

        OwnerCharater.UpdataEnergy(-EnergyCost);
        StartActionBuff.BuffAdd(Buff.BuffName.Stun, target, StunStacks);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        int thisCastStunTimes = OwnerEnergy >= EnergyCost ? 1 : 0;

        SetDescriptionLines(
            $"造成{damageText}点伤害。",
            $"若能量>={EnergyCost}：消耗{EnergyCost}点能量，使目标获得{StunStacks}层{Buff.BuffName.Stun.GetDescription()}。",
            $"触发次数：{thisCastStunTimes}。"
        );
    }
}
