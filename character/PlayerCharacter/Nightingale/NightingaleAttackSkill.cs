using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 6;
    int GainPower = 3;

    public ShadowAmbush()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "影袭";

    public override async Task Effect()
    {
        await base.Effect();

        bool isInvisible =
            OwnerCharater?.StartActionBuffs?.Any(x => x.ThisBuffName == Buff.BuffName.Invisible)
            == true;
        int damage = BaseDamage + OwnerPower;
        await Attack1(damage);

        if (isInvisible)
            await Attack1(damage);

        IncreaseProperties(OwnerCharater, PropertyType.Power, GainPower);
    }

    public override void UpdateDescription()
    {
        int totalDamage = BaseDamage + OwnerPower;
        bool isInvisible =
            OwnerCharater?.StartActionBuffs?.Any(x => x.ThisBuffName == Buff.BuffName.Invisible)
            == true;

        SetDescriptionLines(
            $"造成{BasePlusXWithBattleTotal(BaseDamage, totalDamage, StatX.Power)}点伤害。",
            $"若自身拥有{Buff.BuffName.Invisible.GetDescription()}：额外造成一次伤害。",
            $"获得{GainPower}点{PropertyType.Power.GetDescription()}。"
        );
    }
}
