using System;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 5;
    private const int BaseHeal = 4;
    private const int SurvivabilityGain = 2;

    public MendSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "愈合斩";

    public override async Task Effect()
    {
        await base.Effect();
        await Attack1(BaseDamage + OwnerPower);
        OwnerCharater.Recover(BaseHeal);
        IncreaseProperties(OwnerCharater, PropertyType.Survivability, SurvivabilityGain);
        await Task.Delay(150);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        int totalHeal = BaseHeal + OwnerSurvivability;

        SetDescriptionLines(
            $"造成{BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power)}点伤害。",
            $"回复{BasePlusXWithBattleTotal(BaseHeal, totalHeal, StatX.Survivability)}点生命（对自己）。",
            $"获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivability)}。"
        );
    }
}
