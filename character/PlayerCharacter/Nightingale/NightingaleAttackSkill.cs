using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleAttackSkill { }

public partial class ShadowAmbush : Skill
{
    private const int BaseDamage = 6;
    int GainPower = 3;
    bool hasInvisible =>
        OwnerCharater?.StartActionBuffs?.Any(x => x.ThisBuffName == Buff.BuffName.Invisible)
        == true;

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
            ConditionStep(
                () => hasInvisible,
                $"自身拥有{Buff.BuffName.Invisible.GetDescription()}",
                AttackPrimaryStep(baseDamage: BaseDamage, prefix: "额外造成")
            ),
            ModifyPropertyStep(PropertyType.Power, GainPower)
        );
    }
}

public partial class ShadowExecution : Skill
{
    private const int BaseDamage = 15;
    private const int DoubleStrikeBaseDamage = 5;
    private const string KillTargetKey = "目标";

    public ShadowExecution()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "影处决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, targetKey: KillTargetKey),
            ConditionStep(
                () => GetHostileTargetBind(KillTargetKey)?.State == Character.CharacterState.Dying,
                "击杀目标",
                DoubleStrikeStep(baseDamage: DoubleStrikeBaseDamage)
            )
        );
    }
}
