using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class VeilStep : Skill
{
    private const int InvisibleStacks = 2;
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
