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

        var target = GetAllyByIndex(OwnerCharater.BattleNode.PlayersList.Count - 1);
        IncreaseProperties(target, PropertyType.Power, PowerGain);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。",
            $"使最后存活的队友获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}。"
        );
    }
}
