using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class War : EnemyCharacter
{
    internal static readonly PackedScene ThrallScene = GD.Load<PackedScene>(
        "res://character/EnemyCharacter/WarThrall.tscn"
    );

    public const string PassiveNameText = "战争号令";
    public static string PassiveDescriptionText =>
        "回合开始时：优先在有存活玩家角色的排随机召唤1个召唤物。";

    public override string CharacterName { get; set; } = "War";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        _ = SummonAtRandomEmptySlot();
    }

    private Task SummonAtRandomEmptySlot()
    {
        if (BattleNode == null || ThrallScene == null)
            return Task.CompletedTask;

        using var _ = BeginEffectSource("被动");
        var thrall = ThrallScene.Instantiate<WarThrall>();
        BattleNode.AddSummon(thrall, this, SummonPositionMode.RandomHasEnemy);
        return Task.CompletedTask;
    }

    internal static WarThrall[] GetLivingThralls(Character owner)
    {
        if (owner?.Summons == null)
            return Array.Empty<WarThrall>();

        return owner
            .Summons.OfType<WarThrall>()
            .Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
            )
            .ToArray();
    }

    internal static int GetLivingThrallCount(Character owner) => GetLivingThralls(owner).Length;

    internal static bool HasThrallOnSide(Character owner, int side)
    {
        if (owner == null || side == 0)
            return false;

        return GetLivingThralls(owner)
            .Any(x =>
                side < 0
                    ? x.PositionIndex < owner.PositionIndex
                    : x.PositionIndex > owner.PositionIndex
            );
    }
}

public partial class WarRegedit : EnemyRegedit
{
    public WarRegedit()
    {
        CharacterName = "War";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/War.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/War.tscn");

        MaxLife = 226;
        Power = 9;
        Survivability = 7;
        Speed = 10;
        SkillIDs = [SkillID.WarAttack, SkillID.WarSurvive, SkillID.WarSpecial];

        PassiveName = global::War.PassiveNameText;
        PassiveDescription = global::War.PassiveDescriptionText;
    }
}

public partial class WarAttack : Skill
{
    private const int BaseDamage = 0;
    private const int ThrallPowerGain = 2;

    public WarAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "全军出击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: 2,
                target: HostileTargetReference.Random
            ),
            ModifySummonPropertyStep(PropertyType.Power, ThrallPowerGain)
        );
    }
}

public partial class WarSurvive : Skill
{
    private const int BaseBlock = 7;
    private const int SelfSurvivabilityGain = 2;
    private const int ThrallBlock = 0;

    public WarSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "逝者军势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            BlockSummonsStep(baseBlock: ThrallBlock),
            HealStep(0),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain)
        );
    }
}

public partial class WarSpecial : Skill
{
    public WarSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "死亡行军";
    public override int EnergyCost => 7;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 0),
            SummonStep(SummonPositionMode.RandomHasEnemy, War.ThrallScene),
            SummonStep(SummonPositionMode.RandomHasEnemy, War.ThrallScene),
            ModifyPropertyStep(PropertyType.Power, 2)
        );
    }
}
