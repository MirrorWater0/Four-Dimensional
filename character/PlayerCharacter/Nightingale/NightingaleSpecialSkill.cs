using System.Threading.Tasks;

public partial class NightingaleSpecialSkill { }

public partial class TempoSurge : Skill
{
    private const int SpeedGain = 3;
    int PowerGain = 4;
    int cost = 3;

    public TempoSurge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "疾奏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Speed, SpeedGain),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            EnergyTimesGateStep(
                cost,
                null,
                null,
                CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1, dyingFilter: true)
            )
        );
    }
}

public partial class LongNight : Skill
{
    int DeSurvive = 3;
    int deSpeed = 3;
    int energyCost1 = 2;
    int energyCost2 = 1;
    int Inpower = 5;
    public override string SkillName { get; set; } = "长夜";

    public LongNight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyTimesGateStep(
                energyCost1,
                null,
                null,
                CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 2, dyingFilter: false),
                CarryRelativeAllyStep(relativeIndex: 1, skillIndex: 2, dyingFilter: false),
                ModifyPropertyStep(PropertyType.Survivability, -DeSurvive),
                ModifyPropertyStep(PropertyType.Speed, -deSpeed)
            ),
            EnergyTimesGateStep(
                energyCost2,
                null,
                null,
                ModifyPropertyStep(PropertyType.Power, Inpower)
            )
        );
    }
}
