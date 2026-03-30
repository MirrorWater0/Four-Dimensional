using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private const int PassivePowerGain = 3;

    public const string PassiveNameText = "寄生馈赠";
    public static string PassiveDescriptionText =>
        $"回合结束时：上一位非濒死队友获得{PassivePowerGain}点力量。";

    public override string CharacterName { get; set; } = "AlienBody";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void EndAction()
    {
        base.EndAction();
        Passive(null);
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (BattleNode == null)
            return;

        var helper = Skills?.FirstOrDefault();
        if (helper == null)
            return;
        if (helper.OwnerCharater == null)
            helper.OwnerCharater = this;

        var ally = helper.GetAllyByRelative(-1, dyingFilter: true);
        if (ally == null || ally == this)
            return;

        await ally.IncreaseProperties(PropertyType.Power, PassivePowerGain, this);
    }
}

public partial class AlienBodyAttack : Skill
{
    private const int BaseDamage = 10;
    private const int PowerDown = 1;

    public AlienBodyAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "异体冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(BaseDamage),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, permanent: true)
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private const int BaseBlock = 15;
    private const int SurvivabilityDown = 1;

    public AlienBodySurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "异体护壳";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockFriendlyByRelativeStep(0, BaseBlock),
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown, permanent: true)
        );
    }
}

public partial class AlienBodySpecial : Skill
{
    private const int EnergyCost = 3;
    private const int PowerDown = 4;
    private const int SurvivabilityDown = 4;

    public AlienBodySpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "共生连携";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CarryRelativeAllyStep(relativeIndex: -1, skillIndex: 1, dyingFilter: true),
            EnergyTimesGateStep(
                energyCost: EnergyCost,
                onPassSteps:
                [
                    LowerTargetPropertyStep(PropertyType.Power, PowerDown),
                    LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown),
                ]
            )
        );
    }
}
