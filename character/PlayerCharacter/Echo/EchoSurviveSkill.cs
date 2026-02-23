using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音障防护";
    private const int EnergyGain = 1;
    private const int BaseBlock = 10;
    int times = 2;

    public SoundBarrier()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataEnergy(EnergyGain);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        await Task.Delay(200);
        if (times > 0)
        {
            times--;
            await Carry(OwnerCharater.BattleNode.PlayersList[0], 0);
        }
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"恢复{EnergyGain}点能量。",
            $"获得{blockText}点格挡。",
            $"连携下一位角色使用攻击技能。剩余{times}次"
        );
    }
}

public partial class SonicDeflection : Skill
{
    private const int DamageImmuneStacks = 1;
    private const int BaseBlock = 6;

    public SonicDeflection()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "声波偏转";

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, OwnerCharater, DamageImmuneStacks);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"获得{blockText}点格挡。",
            $"获得{DamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。"
        );
    }
}

public partial class TuningStance : Skill
{
    private const int PowerGain = 2;
    private const int BaseBlock = 5;

    public TuningStance()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "调音姿态";

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGain);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}。",
            $"获得{blockText}点格挡。"
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 2;
    private const int BaseBlock = 6;

    public ResonantWard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响护佑";

    public override async Task Effect()
    {
        await base.Effect();
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, OwnerCharater, DebuffImmunityStacks);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability);
        SetDescriptionLines(
            $"获得{DebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。",
            $"获得{blockText}点格挡。"
        );
    }
}
