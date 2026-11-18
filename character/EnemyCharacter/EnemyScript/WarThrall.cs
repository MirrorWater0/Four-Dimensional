using System;
using Godot;

public partial class WarThrall : SummonCharacter
{
    public override string CharacterName { get; set; } = "战仆";

    public static string GetPassiveDescription() => BuildPassiveDescription(
        EnemyTuning.GetRequiredInt(nameof(WarThrall), "MaxLife"),
        EnemyTuning.GetRequiredInt(nameof(WarThrall), "Power"),
        EnemyTuning.GetRequiredInt(nameof(WarThrall), "Survivability")
    );

    public override void Initialize()
    {
        Registry = new WarThrallRegedit();
        base.Initialize();
    }
}

public partial class WarThrallAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int SelfPowerGain => V(nameof(SelfPowerGain));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "冲锋陷阵";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
