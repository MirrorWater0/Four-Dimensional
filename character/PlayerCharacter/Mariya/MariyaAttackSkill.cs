using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaAttackSkill { }

public partial class MendSlash : Skill
{
    private const int BaseDamage = 8;
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
        GetAllAllyWithOrder(true).FirstOrDefault(x => x.Life < x.BattleMaxLife)?.Recover(10);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        int totalHeal = BaseHeal + OwnerSurvivability;

        SetDescriptionLines(
            $"造成{BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power)}点伤害。",
            $"对占位最靠前的非满血角色回复{BaseHeal}点生命。",
            $"获得+{SurvivabilityGain}{GetColoredPropertyLabel(PropertyType.Survivability)}。"
        );
    }
}
