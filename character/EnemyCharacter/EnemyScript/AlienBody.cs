using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private const int PassiveTriggerTurn = 2;
    private const int PassivePowerDown = 1;
    private const int PassiveSurvivabilityDown = 1;

    private int _turnStartCount;

    public const string PassiveNameText = "寄生馈赠";
    public static string PassiveDescriptionText =>
        $"第{PassiveTriggerTurn}次回合开始时：永久降低目标{PassivePowerDown}点力量和{PassiveSurvivabilityDown}点生存。";

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

        var target = SelectPassiveTarget();
        if (target == null)
            return;

        await target.DescendingProperties(PropertyType.Power, PassivePowerDown, this);
        await target.DescendingProperties(
            PropertyType.Survivability,
            PassiveSurvivabilityDown,
            this
        );

        ApplyPermanentPropertyLoss(target, PropertyType.Power, PassivePowerDown);
        ApplyPermanentPropertyLoss(
            target,
            PropertyType.Survivability,
            PassiveSurvivabilityDown
        );
    }

    private static void ApplyPermanentPropertyLoss(
        Character target,
        PropertyType type,
        int loss
    )
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
            case PropertyType.Power:
                info.Power -= loss;
                break;
            case PropertyType.Survivability:
                info.Survivability -= loss;
                break;
            default:
                return;
        }

        GameInfo.PlayerCharacters[index] = info;
    }

    private Character SelectPassiveTarget()
    {
        if (BattleNode == null)
            return null;

        int ownerRow = GetBattleRow(PositionIndex);
        Character[] ordered = BattleNode
            .GetOrderedTeamCharacters(!IsPlayer, includeSummons: true, dyingFilter: true)
            .Where(x => x != null)
            .OrderBy(x => Mathf.Abs(GetBattleRow(x.PositionIndex) - ownerRow))
            .ThenBy(x => GetBattleRow(x.PositionIndex))
            .ThenBy(x => GetBattleCol(x.PositionIndex))
            .ToArray();
        if (ordered.Length == 0)
            return null;

        Character[] visibleTargets = ordered.Where(x => !HasInvisibleBuff(x)).ToArray();
        Character[] selectableTargets = visibleTargets.Length > 0 ? visibleTargets : ordered;
        Character[] tauntTargets = selectableTargets.Where(HasTauntBuff).ToArray();
        Character[] targets = tauntTargets.Length > 0 ? tauntTargets : selectableTargets;

        return targets.FirstOrDefault();
    }

    private static int GetBattleRow(int positionIndex) =>
        positionIndex > 0 ? (positionIndex - 1) % 3 : 0;

    private static int GetBattleCol(int positionIndex) =>
        positionIndex > 0 ? (positionIndex - 1) / 3 : 0;

    private static bool HasInvisibleBuff(Character target) =>
        target?.StartActionBuffs?.Any(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Invisible && buff.Stack > 0
        ) == true;

    private static bool HasTauntBuff(Character target) =>
        target?.HurtBuffs?.Any(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Taunt && buff.Stack > 0
        ) == true;
}

public partial class AlienBodyRegedit : EnemyRegedit
{
    public AlienBodyRegedit()
    {
        CharacterName = "AlienBody";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/AlienBody.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AlienBody.tscn");

        MaxLife = 60;
        Power = 10;
        Survivability = 11;
        Speed = 8;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive, SkillID.AlienBodySpecial];

        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
    }
}

public partial class AlienBodyAttack : Skill
{
    private const int BaseDamage = 8;
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
            AttackPrimaryStep(BaseDamage),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown)
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private const int BaseBlock = 13;
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
            BlockStep(0, BaseBlock),
            LowerTargetPropertyStep(PropertyType.Survivability, SurvivabilityDown)
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
            CarryStep(target: RelativeTarget(-1), skillIndex: 1),
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
