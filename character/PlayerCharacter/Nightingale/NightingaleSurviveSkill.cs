using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class VeilStep : Skill
{
    private const int InvisibleStacks = 3;
    private const int BaseBlock = 8;

    public VeilStep()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "夜幕潜行";

    public override async Task Effect()
    {
        await base.Effect();
        StartActionBuff.BuffAdd(Buff.BuffName.Invisible, OwnerCharater, InvisibleStacks);
        GetAllyByRelative(-1, true).UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"获得{InvisibleStacks}层{Buff.BuffName.Invisible.GetDescription()}。",
            $"令上一个存活的角色获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。"
        );
    }
}

public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 3;
    private const int BaseBlock = 8;
    int Inpower = 4;

    public FlashOfLight()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "闪耀之光";

    public override async Task Effect()
    {
        await base.Effect();
        StartActionBuff.BuffAdd(Buff.BuffName.Vulnerable, Chosetarget1()[0], VulnerableStacks);
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
        IncreaseProperties(OwnerCharater, PropertyType.Power, Inpower);
    }

    public override void UpdateDescription()
    {
        int totalBlock = BaseBlock + OwnerSurvivability;
        SetDescriptionLines(
            $"获得{VulnerableStacks}层{Buff.BuffName.Vulnerable.GetDescription()}。",
            $"令自己获得{BasePlusXWithBattleTotal(BaseBlock, totalBlock, StatX.Survivability, clampMax: 999)}点格挡。",
            $"使自己获得+{Inpower}{GetColoredPropertyLabel(PropertyType.Power)}。"
        );
    }
}

public partial class Swift : Skill
{
    int InSpeed = 2;
    int InSur = 3;

    public Swift()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "迅捷";

    public override async Task Effect()
    {
        await base.Effect();
        IncreaseProperties(OwnerCharater, PropertyType.Speed, InSpeed);
        for (int i = 0; i < GetAllAllyWithOrder(true).Length; i++)
        {
            IncreaseProperties(GetAllAllyWithOrder()[i], PropertyType.Survivability, InSur);
        }
    }
}
