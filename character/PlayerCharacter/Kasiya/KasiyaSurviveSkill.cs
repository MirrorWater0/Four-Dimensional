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
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(
            BaseBlock,
            totalBlock,
            StatX.Survivability,
            clampMax: 999
        );
        SetDescriptionLines(
            $"使所有敌人获得{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}。",
            $"获得{blockText}点格挡。"
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
        IncreaseProperties(OwnerCharater, PropertyType.Survivability, SurvivabilityGain);
        OwnerCharater.UpdataBlock(OwnerCharater.BattleSurvivability);
    }

    public override void UpdateDescription()
    {
        int totalBlock = OwnerSurvivability + SurvivabilityGain;
        string blockText = BasePlusXWithBattleTotal(
            SurvivabilityGain,
            totalBlock,
            StatX.Survivability
        );
        SetDescriptionLines(
            $"获得{blockText}点格挡。",
            $"获得+{PowerGain}{GetColoredPropertyLabel(PropertyType.Power)}。",
            $"获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivability)}。"
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    private int GainPower = 3;
    public override string SkillName { get; set; } = "绝对防御";
    int basisBlock = 4;

    public AbsouluteDefense()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(basisBlock + OwnerCharater.BattleSurvivability);
        OwnerCharater.UpdataBlock(basisBlock + OwnerCharater.BattleSurvivability);
        IncreaseProperties(OwnerCharater, PropertyType.Power, GainPower);
    }

    public override void UpdateDescription()
    {
        int totalBlock = basisBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(
            basisBlock,
            totalBlock,
            StatX.Survivability,
            clampMax: 999
        );
        SetDescriptionLines(
            $"获得{blockText}点格挡（共{2}次）。",
            $"获得+{GainPower}{GetColoredPropertyLabel(PropertyType.Power)}。"
        );
    }
}

public partial class TauntingGuard : Skill
{
    private const int TauntStacks = 2;
    private const int BaseBlock = 6;

    public override string SkillName { get; set; } = "嘲讽守势";

    public TauntingGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override async Task Effect()
    {
        await base.Effect();
        HurtBuff.BuffAdd(Buff.BuffName.Taunt, OwnerCharater, TauntStacks);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        string blockText = BasePlusXWithBattleTotal(
            BaseBlock,
            totalBlock,
            StatX.Survivability,
            clampMax: 999
        );

        SetDescriptionLines(
            $"使自己获得{TauntStacks}层{Buff.BuffName.Taunt.GetDescription()}。",
            $"获得{blockText}点格挡。"
        );
    }
}
