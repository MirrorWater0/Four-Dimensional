using System;
using System.Linq;
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
        await Attack1(OwnerPower);
        while (OwnerCharater.Energy > 0)
        {
            OwnerCharater.UpdataEnergy(-CostPerCast);
            await Attack1(OwnerPower);
            IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGainPerCast);
        }
    }

    public override void UpdateDescription()
    {
        int energy = OwnerEnergy;
        int bonusCasts = Math.Max(0, (int)Math.Ceiling((double)energy / CostPerCast));
        int castTimes = 1 + bonusCasts;
        int totalPowerGain = bonusCasts * PowerGainPerCast;

        string energyX = X(StatX.Energy);
        string castTimesBasis = CostPerCast == 1 ? $"1+{energyX}" : $"1+ceil({energyX}/{CostPerCast})";
        string castTimesText = WithBattleTotal(castTimesBasis, castTimes);
        string perCastDamageText = XWithBattleTotal(StatX.Power, OwnerPower);
        string totalPowerGainBasis = CostPerCast == 1 ? $"{PowerGainPerCast}*{energyX}" : $"{PowerGainPerCast}*ceil({energyX}/{CostPerCast})";
        string totalPowerGainText = WithBattleTotal(totalPowerGainBasis, totalPowerGain);

        SetDescriptionLines(
            $"施放次数：{castTimesText}。",
            $"每次伤害：{perCastDamageText}。",
            $"每次消耗：{CostPerCast}点能量。",
            $"每次提升：{PowerGainPerCast}点{GetColoredPropertyLabel(PropertyType.Power)}。",
            $"总力量提升：{totalPowerGainText}。"
        );
    }
}

public class SonicBoom : Skill
{
    private const int BaseDamage = 4;
    private const int EnergyCost = 2;

    public SonicBoom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "音爆";

    public override async Task Effect()
    {
        await base.Effect();

        if (OwnerCharater?.BattleNode == null)
            return;

        var targets = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.EnemiesList.Cast<Character>()
            : OwnerCharater.BattleNode.PlayersList.Cast<Character>();

        int damage = BaseDamage + OwnerPower;
        foreach (
            var target in targets.Where(x =>
                x != null && x.State == Character.CharacterState.Normal
            )
        )
        {
            _ = Attack3(damage, target, 1);
        }

        if (OwnerCharater.Energy >= EnergyCost)
        {
            OwnerCharater.UpdataEnergy(-EnergyCost);
            foreach (
                var target in targets.Where(x =>
                    x != null && x.State == Character.CharacterState.Normal
                )
            )
            {
                _ = Attack3(damage, target, 1);
            }
        }

        await Task.Delay(400);
    }

    public override void UpdateDescription()
    {
        int waves = OwnerEnergy >= EnergyCost ? 2 : 1;
        int totalDamage = BaseDamage + OwnerPower;
        string damageText = BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power);
        string wavesText = WithBattleTotal("次数", waves);
        SetDescriptionLines(
            $"对所有敌人造成{damageText}点伤害。",
            $"当前{wavesText}次。",
            $"若能量>={EnergyCost}：额外消耗{EnergyCost}点能量。",
            $"并追加1次。"
        );
    }
}

public class PhaseEcho : Skill
{
    private const int EnergyCost = 1;
    private const int DamageImmuneStacks = 2;
    private const int BaseBlock = 12;

    public PhaseEcho()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "相位回声";

    public override async Task Effect()
    {
        await base.Effect();

        if (OwnerCharater == null)
            return;

        if (OwnerCharater.Energy >= EnergyCost)
        {
            OwnerCharater.UpdataEnergy(-EnergyCost);
            OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
            HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, OwnerCharater, DamageImmuneStacks);
        }
        else
        {
            OwnerCharater.UpdataBlock(BaseBlock / 2 + OwnerSurvivability);
            HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, OwnerCharater, 1);
        }

        await Task.Delay(200);
    }

    public override void UpdateDescription()
    {
        int fullBlock = BaseBlock + OwnerSurvivability;
        int lowBlock = BaseBlock / 2 + OwnerSurvivability;

        string fullBlockText = BasePlusXWithBattleTotal(BaseBlock, fullBlock, StatX.Survivability);
        string lowBlockText = BasePlusXWithBattleTotal(
            BaseBlock / 2,
            lowBlock,
            StatX.Survivability
        );
        SetDescriptionLines(
            $"若能量>={EnergyCost}：消耗{EnergyCost}点能量。",
            $"获得{fullBlockText}点格挡。",
            $"获得{DamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。",
            $"否则：获得{lowBlockText}点格挡。",
            $"获得1层{Buff.BuffName.DamageImmune.GetDescription()}。"
        );
    }
}
