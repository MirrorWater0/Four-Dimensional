using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSpecialSkill { }

public partial class TempoSurge : Skill
{
    private const int SpeedGain = 3;
    int cost = 2;

    public TempoSurge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "疾奏";

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Speed, SpeedGain);
        if (OwnerCharater.Energy >= cost)
        {
            OwnerCharater.UpdataEnergy(-cost);
            await Carry(GetAllyByRelative(-1), 0);
        }
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            $"获得+{SpeedGain}{GetColoredPropertyLabel(PropertyType.Speed)}。",
            $"消耗{cost}点能量:连携上一个队友攻击技能。"
        );
    }
}

public partial class LongNight : Skill
{
    int DeSurvive = 3;
    int deSpeed = 3;
    int times = 1;
    int energyCost1 = 2;
    int energyCost2 = 1;
    int Inpower = 5;
    public override string SkillName { get; set; } = "长夜";

    public LongNight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        if (OwnerCharater.Energy >= energyCost1 && times > 0)
        {
            await Carry(GetAllyByRelative(-1), 2);
            await Carry(GetAllyByRelative(1), 2);
            OwnerCharater.UpdataEnergy(-DeSurvive);
            IncreaseProperties(OwnerCharater, PropertyType.Speed, -deSpeed);
        }

        if (OwnerCharater.Energy >= energyCost2)
        {
            OwnerCharater.UpdataEnergy(-energyCost2);
            IncreaseProperties(OwnerCharater, PropertyType.Power, Inpower);
        }
    }

    public override void UpdateDescription()
    {
        SetDescriptionLines(
            $"若能量>={energyCost1}：消耗{DeSurvive}点能量，连携前一位队友和后一位队友使用其特殊技能(剩余触发次数：{times})。",
            $"自身-{deSpeed}点{GetColoredPropertyLabel(PropertyType.Speed)};",
            $"-{DeSurvive}点{GetColoredPropertyLabel(PropertyType.Survivability)}。",
            $"若能量>={energyCost2}：额外消耗{energyCost2}点能量，获得+{Inpower}{GetColoredPropertyLabel(PropertyType.Power)}（当前：{times}）。"
        );
    }
}
