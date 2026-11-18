using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class War : EnemyCharacter
{
    private static int TurnEndSummonCount =>
        EnemyTuning.GetRequiredInt("War", nameof(TurnEndSummonCount));
    internal static readonly PackedScene ThrallScene = GD.Load<PackedScene>(
        "res://character/EnemyCharacter/WarThrall.tscn"
    );

    public const string PassiveNameText = "战争号令";
    public static string PassiveDescriptionText =>
        $"阵营回合结束时：优先在有存活玩家角色的排随机召唤{TurnEndSummonCount}个召唤物。";

    public override string CharacterName { get; set; } = "War";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
    }

    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        BattleNode?.QueueBattleEffect(SummonAtRandomEmptySlot, "战争号令");
    }

    private Task SummonAtRandomEmptySlot()
    {
        if (BattleNode == null || ThrallScene == null)
            return Task.CompletedTask;

        using var _ = BeginEffectSource("被动");
        for (int i = 0; i < TurnEndSummonCount; i++)
        {
            var thrall = ThrallScene.Instantiate<WarThrall>();
            if (BattleNode.AddSummon(thrall, this, SummonPositionMode.RandomHasEnemy) == null)
                break;
        }
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
        PositionIndex = Battle.MaxEnemyFormationSlots;
        PortaitPath = "res://asset/EnemyCharater/War.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/War.tscn");
        SkillIDs = [SkillID.WarAttack, SkillID.WarSurvive];
        // 先建立战仆与成长，第五轮死亡行军施加覆盖四轮的全阵脆弱。
        IntentionCycle =
        [
            SkillID.WarAttack,
            SkillID.WarSurvive,
            random,
            SkillID.WarAttack,
            SkillID.WarSpecial,
            SkillID.WarSurvive,
        ];

        PassiveName = global::War.PassiveNameText;
        PassiveDescription = global::War.PassiveDescriptionText;
    }
}

public partial class WarAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "全军出击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: V("HitCount"), multiplier: V("Multiplier"))
        );
    }
}

public partial class WarSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int SelfSurvivabilityGain => V(nameof(SelfSurvivabilityGain));
    private int ThrallBlock => V(nameof(ThrallBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "逝者军势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            BlockSummonsStep(baseBlock: ThrallBlock, multiplier: V("ThrallBlockMultiplier")),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain),
            ModifyPropertyStep(PropertyType.Power, V("Spower"))
        );
    }
}

public partial class WarSpecial : Skill
{
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "死亡行军";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: V("BaseDamage"),
                multiplier: V("Multiplier"),
                times: V("HitCount")
            ),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain")),
            ModifySummonPropertyStep(PropertyType.Power, 2),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.All)
        );
    }
}
