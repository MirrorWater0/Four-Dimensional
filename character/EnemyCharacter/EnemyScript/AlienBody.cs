using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private const int PassiveMaxTargets = 2;
    private const int PassiveDazeCount = 1;

    public const string PassiveNameText = "寄生馈赠";
    public static string PassiveDescriptionText =>
        $"回合开始时：向至多{PassiveMaxTargets}个目标抽牌堆塞入{PassiveDazeCount}张"
        + $"{I18n.Tr("skill.daze_status.name", "晕眩")}。";

    public override string CharacterName { get; set; } = "AlienBody";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
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
            )
            .Take(PassiveMaxTargets)
            .ToArray();
        if (targets.Length == 0)
            return;

        var affectedTargets = targets
            .Select(target => new { Target = target, Player = ResolveCardPileOwner(target) })
            .Where(x => x.Player != null && x.Player.BattleNode != null)
            .GroupBy(x => x.Target)
            .Select(group => group.First())
            .ToArray();
        if (affectedTargets.Length == 0)
            return;

        foreach (
            var animationGroup in affectedTargets
                .Where(entry => entry.Player.BattleNode.CharacterControl != null)
                .GroupBy(entry => entry.Player.BattleNode.CharacterControl)
        )
        {
            await animationGroup.Key.PlayStatusCardInsertAnimationAsync(
                animationGroup
                    .Select(entry => new CharacterControl.StatusCardInsertAnimationEntry(
                        entry.Target,
                        SkillID.DazeStatus,
                        PassiveDazeCount,
                        this
                    ))
                    .ToArray()
            );
        }

        foreach (var playerGroup in affectedTargets.GroupBy(entry => entry.Player))
        {
            PlayerCharacter player = playerGroup.Key;
            player.BattleNode.AddPlayerBattleStatusCardsToDrawPile(
                player,
                SkillID.DazeStatus,
                PassiveDazeCount * playerGroup.Count(),
                this
            );
        }
    }

    private static PlayerCharacter ResolveCardPileOwner(Character target)
    {
        if (target is PlayerCharacter player)
            return player;

        if (target is SummonCharacter summon)
        {
            if (summon.Summoner is PlayerCharacter summoner)
                return summoner;

            if (summon.LastSummoner is PlayerCharacter lastSummoner)
                return lastSummoner;
        }

        return null;
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

        MaxLife = 27;
        Power = 16;
        Survivability = 14;
        Speed = 5;
        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive, SkillID.AlienBodySpecial];

        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
    }
}

public partial class AlienBodyAttack : Skill
{
    private const int BaseDamage = 0;
    private const int PowerDown = 2;

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
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, HostileTargetReference.AttackKey)
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private const int BaseBlock = 0;
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
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.One
            )
        );
    }
}

public partial class AlienBodySpecial : Skill
{
    private const int PowerDown = 2;
    private const int SurvivabilityDown = 2;

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
            AddStatusCardsToDrawPileStep(SkillID.DazeStatus, 1, HostileTargetReference.All),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, HostileTargetReference.One),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.One
            )
        );
    }
}
