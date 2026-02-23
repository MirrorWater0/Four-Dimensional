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

        var target = GetLastAliveAllyOrSelf();
        IncreaseProperties(target, PropertyType.Power, PowerGain);

        await Task.Delay(150);
    }

    private Character GetLastAliveAllyOrSelf()
    {
        if (OwnerCharater?.BattleNode == null)
            return OwnerCharater;

        var allies = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.PlayersList.Cast<Character>()
            : OwnerCharater.BattleNode.EnemiesList.Cast<Character>();

        return allies
                .Where(x =>
                    x != null && x != OwnerCharater && x.State == Character.CharacterState.Normal
                )
                .OrderBy(x => x.PositionIndex)
                .LastOrDefault()
            ?? OwnerCharater;
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。",
            $"使最后存活的队友获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}（若没有队友则作用于自己）。"
        );
    }
}
