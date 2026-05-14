using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class War : EnemyCharacter
{
    private const int PassiveSummonsPerSide = 1;

    internal static readonly PackedScene ThrallScene = GD.Load<PackedScene>(
        "res://character/EnemyCharacter/WarThrall.tscn"
    );

    public const string PassiveNameText = "战争号令";
    public static string PassiveDescriptionText =>
        $"战斗开始时：在上一空位和下一空位各召唤{PassiveSummonsPerSide}个召唤物。";

    public override string CharacterName { get; set; } = "War";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    private Task StartPassive()
    {
        if (BattleNode == null || ThrallScene == null)
            return Task.CompletedTask;

        using var _ = BeginEffectSource("被动");
        var previous = ThrallScene.Instantiate<WarThrall>();
        BattleNode.AddSummon(previous, this, -1);

        var next = ThrallScene.Instantiate<WarThrall>();
        BattleNode.AddSummon(next, this, 1);
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

        MaxLife = 400;
        Power = 15;
        Survivability = 15;
        Speed = 12;
        SkillIDs = [SkillID.WarAttack, SkillID.WarSurvive, SkillID.WarSpecial];

        PassiveName = global::War.PassiveNameText;
        PassiveDescription = global::War.PassiveDescriptionText;
    }
}

public partial class WarAttack : Skill
{
    private const int BaseDamage = -15;
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
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 2),
            SummonStep(0, War.ThrallScene),
            ModifySummonPropertyStep(PropertyType.Power, ThrallPowerGain)
        );
    }
}

public partial class WarSurvive : Skill
{
    private const int BaseBlock = 13;
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
            BlockStep(0, BaseBlock),
            SummonStep(9, War.ThrallScene),
            BlockSummonsStep(baseBlock: ThrallBlock),
            ModifyPropertyStep(PropertyType.Survivability, SelfSurvivabilityGain)
        );
    }
}

public partial class WarSpecial : Skill
{
    private const int ThrallPowerGain = 3;

    public WarSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "死亡行军";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0),
            HealStep(baseHeal: 0, target: TargetReference.Self),
            SummonStep(1, War.ThrallScene),
            SummonStep(-1, War.ThrallScene),
            ModifySummonPropertyStep(PropertyType.Power, ThrallPowerGain),
            ModifyPropertyStep(PropertyType.Power, 4)
        );
    }
}
