using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 5;

    public override string SkillName { get; set; } = "冲击波";

    public ShockWave()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();

        if (OwnerCharater?.BattleNode == null)
            return;

        var targets = OwnerCharater.IsPlayer
            ? OwnerCharater.BattleNode.EnemiesList.Cast<Character>()
            : OwnerCharater.BattleNode.PlayersList.Cast<Character>();

        foreach (
            var target in targets.Where(x =>
                x != null && x.State == Character.CharacterState.Normal
            )
        )
        {
            HurtBuff.BuffAdd(Buff.BuffName.Vulnerable, target, VulnerableStacks);
        }

        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        int block = Math.Clamp(BaseBlock + OwnerSurvivability, 0, 999);
        SetDescriptionLines(
            $"使所有敌人获得{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}；"
                + $"获得{block}点格挡。"
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 5;
    private const int SurvivabilityGain = 5;

    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Power, PowerGain);
        IncreaseProperties(OwnerCharater, PropertyType.Survivalibility, SurvivabilityGain);
        OwnerCharater.UpdataBlock(OwnerCharater.BattleSurvivability);
    }

    public override void UpdateDescription()
    {
        int block = Math.Clamp(OwnerSurvivability + SurvivabilityGain, 0, 9999);
        SetDescriptionLines(
            $"获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}、+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivalibility)}；获得{block}点格挡。"
        );
    }
}
