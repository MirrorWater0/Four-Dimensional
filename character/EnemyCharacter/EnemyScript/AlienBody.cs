using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private const int PassiveTriggerTurn = 2;
    private const int PassiveSurvivabilityDown = 1;

    private int _turnStartCount;

    public const string PassiveNameText = "寄生馈赠";
    public static string PassiveDescriptionText =>
        $"第{PassiveTriggerTurn}次回合开始时：永久减少目标{PassiveSurvivabilityDown}点生存。";

    public override string CharacterName { get; set; } = "AlienBody";

    public override void Initialize()
    {
        base.Initialize();
        _turnStartCount = 0;
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        _turnStartCount++;
        if (_turnStartCount == PassiveTriggerTurn)
            TriggerPassive(null);
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        if (BattleNode == null)
            return;

        Character[] targets = ChooseHostileTargetsByOrder(
            returnDummyWhenEmpty: false,
            normalOnly: false,
            dyingFilter: true
        );
        Character target = targets.Length > 0 ? targets[0] : null;
        if (target == null)
            return;

        await target.DescendingProperties(
            PropertyType.Survivability,
            PassiveSurvivabilityDown,
            this
        );

        ApplyPermanentPropertyLoss(target, PropertyType.Survivability, PassiveSurvivabilityDown);
    }

    private static void ApplyPermanentPropertyLoss(Character target, PropertyType type, int loss)
    {
        if (loss <= 0 || target is not PlayerCharacter player)
            return;

        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
            return;

        int index = player.CharacterIndex;
        if (index < 0 || index >= GameInfo.PlayerCharacters.Length)
            return;

        PlayerInfoStructure info = GameInfo.PlayerCharacters[index];
        switch (type)
        {
            case PropertyType.Survivability:
                info.Survivability -= loss;
                break;
            default:
                return;
        }

        GameInfo.PlayerCharacters[index] = info;
    }
}

public partial class AlienBodyRegedit : EnemyRegedit
{
    public AlienBodyRegedit()
    {
        CharacterName = "AlienBody";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/AlienBody.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AlienBody.tscn");

        MaxLife = 26;
        Power = 5;
        Survivability = 5;
        Speed = 5;
        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive, SkillID.AlienBodySpecial];

        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
    }
}

public partial class AlienBodyAttack : Skill
{
    private const int BaseDamage = 10;
    private const int PowerDown = 4;

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
            AttackStep(BaseDamage),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown)
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private const int BaseBlock = 5;
    private const int SurvivabilityDown = 4;

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
            BlockStep(baseBlock: BaseBlock),
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown, HostileTargetReference.One)
        );
    }
}

public partial class AlienBodySpecial : Skill
{
    private const int PowerDown = 4;
    private const int SurvivabilityDown = 4;

    public AlienBodySpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "共生连携";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CarryStep(target: TargetReference.Previous, skillIndex: 2),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, HostileTargetReference.One),
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown, HostileTargetReference.One)
        );
    }
}
