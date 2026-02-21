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
            await Attack1(OwnerPower);
            IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGainPerCast);
            if (OwnerCharater.Energy > 0)
                OwnerCharater.UpdataEnergy(-CostPerCast);
        }
    }

    public override void UpdateDescription()
    {
        int energy = Math.Max(OwnerEnergy, 0);
        int castTimes = Math.Max(1, (int)Math.Ceiling((double)energy / CostPerCast));
        int totalPowerGain = castTimes * PowerGainPerCast;
        SetDescriptionLines(
            $"施放{castTimes}次；每次造成{Math.Clamp(OwnerPower, 0, 999)}点伤害；每次消耗{CostPerCast}点能量。",
            $"每次获得{PowerGainPerCast}点{GetColoredPropertyLabel(PropertyType.Power)}（总计{totalPowerGain}点）。"
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
        int damage = Math.Clamp(BaseDamage + OwnerPower, 0, 9999);
        int waves = OwnerEnergy >= EnergyCost ? 2 : 1;
        SetDescriptionLines(
            $"对所有敌人造成{damage}点伤害；当前{waves}次。",
            $"若能量>={EnergyCost}：额外消耗{EnergyCost}点能量并追加1次。"
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
        int fullBlock = Math.Clamp(BaseBlock + OwnerSurvivability, 0, 999);
        int lowBlock = Math.Clamp(BaseBlock / 2 + OwnerSurvivability, 0, 999);
        SetDescriptionLines(
            $"若能量>={EnergyCost}：消耗{EnergyCost}点能量；获得{fullBlock}点格挡；获得{DamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。",
            $"否则：获得{lowBlock}点格挡；获得1层{Buff.BuffName.DamageImmune.GetDescription()}。"
        );
    }
}
