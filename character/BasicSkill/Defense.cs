using Godot;
using System;
using System.Threading.Tasks;

public partial class Defense : Skill
{
    private const int BaseBlock = 30;

    public Defense():base(Skill.SkillTypes.Defence)
    {
        UpdateDescription();
    }
    public override string SkillName { set; get; } = "坚不可摧";

    public async override Task Effect()
    {
        await base.Effect();
        OwnerCharater.UpdataBlock(BaseBlock + OwnerSurvivability);
    }

    public override void UpdateDescription()
    {
        SetDescriptionText(
            $"获得格挡，数值为{Math.Clamp(BaseBlock + OwnerSurvivability, 0, 9999)}。"
        );
    }
}
