using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Death : EnemyCharacter
{
    private const int DisasterStacks = 5;
    private const float MoveDuration = 0.22f;

    public const string PassiveNameText = "终末游行";
    public static string PassiveDescriptionText =>
        $"回合开始时：令目标获得{DisasterStacks}层{Buff.BuffName.Disaster.GetDescription()}。\n"
        + "回合结束时：随机移动到其他位置。";

    public override string CharacterName { get; set; } = "Death";

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

    protected override async Task ResolveTurnEndPhaseAsync()
    {
        await base.ResolveTurnEndPhaseAsync();
        await MoveToRandomOtherPosition();
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
    }

    public override void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        Character[] targets = ChooseHostileTargetsByOrder(
            returnDummyWhenEmpty: false,
            normalOnly: false,
            dyingFilter: true
        );
        Character target = targets.Length > 0 ? targets[0] : null;
        if (target == null)
            return;

        EndActionBuff.BuffAdd(Buff.BuffName.Disaster, target, DisasterStacks, this);
    }

    private async Task MoveToRandomOtherPosition()
    {
        if (BattleNode == null || State == CharacterState.Dying)
            return;

        int[] occupied = BattleNode
            .GetOrderedTeamCharacters(IsPlayer, includeSummons: true, dyingFilter: true)
            .Where(x => x != null && x != this)
            .Select(x => x.PositionIndex)
            .ToArray();
        int[] candidates = Enumerable
            .Range(1, 9)
            .Where(x => x != PositionIndex && !occupied.Contains(x))
            .ToArray();
        if (candidates.Length == 0)
            return;

        Random random = BattleNode.BattleIntentionRandom ?? new Random();
        int nextPosition = candidates[random.Next(candidates.Length)];
        PositionIndex = nextPosition;
        Vector2 targetPosition = ComputeBattlePosition(nextPosition, IsPlayer);
        ZIndex = GetBattleRow(nextPosition);

        Tween tween = CreateTween();
        tween.TweenProperty(this, "position", targetPosition, MoveDuration);
        await ToSignal(tween, "finished");
        Position = targetPosition;
        OriginalPosition = targetPosition;
        BattleNode.EnemiesList = BattleNode.EnemiesList.OrderBy(x => x.PositionIndex).ToList();
        BattleNode.RefreshTurnOrderPreview();
    }

    private static Vector2 ComputeBattlePosition(int positionIndex, bool isPlayer)
    {
        const float gapY = 140f;
        const float gapX = 280f;
        const float skew = 10f;
        const float rowOffset = 100f;
        int row = GetBattleRow(positionIndex);
        int col = GetBattleCol(positionIndex);
        int side = isPlayer ? -1 : 1;
        float xPos = col * gapX * side - (row * skew - rowOffset * (row - 1));
        return new Vector2(xPos, row * gapY);
    }

    private static int GetBattleRow(int positionIndex) =>
        positionIndex > 0 ? (positionIndex - 1) % 3 : 0;

    private static int GetBattleCol(int positionIndex) =>
        positionIndex > 0 ? (positionIndex - 1) / 3 : 0;
}

public partial class DeathRegedit : EnemyRegedit
{
    public DeathRegedit()
    {
        CharacterName = "Death";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/Death.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Death.tscn");

        MaxLife = 405;
        Power = 9;
        Survivability = 14;
        Speed = 16;
        SkillIDs = [SkillID.DeathAttack, SkillID.DeathSurvive, SkillID.DeathSpecial];

        PassiveName = global::Death.PassiveNameText;
        PassiveDescription = global::Death.PassiveDescriptionText;
    }
}

public partial class DeathAttack : Skill
{
    private const int BaseDamage = 0;
    private const int HitCount = 2;

    public DeathAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "双魂裁决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.Two, times: HitCount),
            ApplyBuffHostile(Buff.BuffName.Weaken, 1)
        );
    }
}

public partial class DeathSurvive : Skill
{
    private const int BaseBlock = 15;
    private const int Heal = 8;

    public DeathSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "死寂";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            HealStep(Heal, target: TargetReference.Self)
        );
    }
}

public partial class DeathSpecial : Skill
{
    private const int SelfPowerGain = 3;
    private const int DisasterStacks = 4;

    public DeathSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终焉宣告";
    public override int EnergyCost => 9;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain),
            ApplyBuffHostile(Buff.BuffName.Disaster, DisasterStacks, HostileTargetReference.All),
            AddStatusCardsToDrawPileStep(SkillID.PlagueStatus, 2, HostileTargetReference.All)
        );
    }
}
