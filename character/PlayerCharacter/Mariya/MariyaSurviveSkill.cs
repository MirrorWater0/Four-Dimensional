using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 7;
    private const int PowerGain = 4;

    public FinalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终守";

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);

        var target = GetAllyByIndex(OwnerCharater.BattleNode.PlayersList.Count - 1, true);
        IncreaseProperties(target, PropertyType.Power, PowerGain);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。",
            $"使最后存活的角色获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}。"
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 7;
    private const int SurvivabilityGain = 3;

    public CrystalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "水晶守护";

    public override async Task Effect()
    {
        await base.Effect();
        var allys = GetAllAllyWithOrder(true);
        for (int i = 0; i < allys.Length; i++)
        {
            if (allys[i] == OwnerCharater)
                continue;
            allys[i].UpdataBlock(BaseBlock + OwnerSurvivability);
        }
        IncreaseProperties(
            GetAllyByRelative(-1, true),
            PropertyType.Survivability,
            SurvivabilityGain
        );
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"全队除自己以外获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。",
            $"使上一个存活的角色获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivability)}。"
        );
    }
}
