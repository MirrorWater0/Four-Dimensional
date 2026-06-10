using Godot;

public partial class WarThrall : SummonCharacter
{
    internal const int MaxLifeStat = 20;
    internal const int PowerStat = 0;
    internal const int BasePowerContributionStat = 0;
    internal const int SurvivabilityStat = 0;
    internal const int SpeedStat = 0;

    public override string CharacterName { get; set; } = "战仆";

    public static string GetPassiveDescription()
    {
        var attack = Skill.GetSkill(SkillID.WarThrallAttack);
        attack.SetPreviewStats(PowerStat, SurvivabilityStat, 1, isPlayer: false);
        attack.UpdateDescription();
        return BuildPassiveDescription(
            maxLife: MaxLifeStat,
            power: PowerStat,
            survivability: SurvivabilityStat,
            speed: SpeedStat,
            attack
        );
    }

    public override void Initialize()
    {
        PassiveName = "召唤物";
        PassiveDescription = GetPassiveDescription();
        Skills = [Skill.GetSkill(SkillID.WarThrallAttack)];
        SetBaseCombatStatContributions(BasePowerContributionStat, SurvivabilityStat);
        SetCombatStats(PowerStat, SurvivabilityStat, SpeedStat, MaxLifeStat);
        base.Initialize();
    }
}

public partial class WarThrallAttack : Skill
{
    private const int BaseDamage = 4;
    private const int SelfPowerGain = 1;

    public WarThrallAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "冲锋陷阵";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}
