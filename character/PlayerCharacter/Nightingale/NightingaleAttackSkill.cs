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

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            CustomStep(
                async _ =>
                {
                    bool hasInvisible =
                        OwnerCharater?.StartActionBuffs?.Any(x =>
                            x.ThisBuffName == Buff.BuffName.Invisible
                        ) == true;
                    if (!hasInvisible)
                        return;

                    int damage = DamageFromPower(BaseDamage);
                    await Attack1(damage);
                },
                _ =>
                    new[]
                    {
                        $"若自身拥有{Buff.BuffName.Invisible.GetDescription()}：额外造成一次伤害。",
                    }
            ),
            ModifyPropertyStep(PropertyType.Power, GainPower)
        );
    }
}
