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
            EnergyGateStep(cost, consume: true),
            CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1, dyingFilter: true)
        );
    }
}

public partial class LongNight : Skill
{
    int DeSurvive = 3;
    int deSpeed = 3;
    int times = 1;
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
            ConditionGateStep(
                condition: _ => OwnerEnergy >= energyCost1 && times > 0,
                async skill =>
                {
                    await CarryRelativeAllyStep(
                            relativeIndex: -1,
                            skillIndex: 2,
                            dyingFilter: false,
                            describe: false
                        )
                        .Execute(skill);
                    await CarryRelativeAllyStep(
                            relativeIndex: 1,
                            skillIndex: 2,
                            dyingFilter: false,
                            describe: false
                        )
                        .Execute(skill);
                    await EnergyStep(-DeSurvive).Execute(skill);
                    await ModifyPropertyStep(PropertyType.Speed, -deSpeed).Execute(skill);
                },
                describe: _ =>
                    new[]
                    {
                        $"若能量>={energyCost1}：消耗{DeSurvive}点能量，连携前一位队友和后一位队友使用其特殊技能(剩余触发次数：{times})。",
                        $"自身{DeltaPropertyText(PropertyType.Speed, -deSpeed)}。",
                    },
                stopOnFail: false
            ),
            EnergyGateStep(energyCost2, consume: true),
            ModifyPropertyStep(PropertyType.Power, Inpower)
        );
    }
}

