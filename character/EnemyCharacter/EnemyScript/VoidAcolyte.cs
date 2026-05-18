using System.Threading.Tasks;
using Godot;

public partial class VoidAcolyte : EnemyCharacter
{
    private const int StartRebirthStacks = 9;

    public const string PassiveNameText = "空壳复苏";
    public static string PassiveDescriptionText =>
        $"战斗开始时: 获得{StartRebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}.";

    public override string CharacterName { get; set; } = "虚空侍从";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode?.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        DyingBuff.BuffAdd(Buff.BuffName.RebirthI, this, StartRebirthStacks, this);
        return Task.CompletedTask;
    }
}

public partial class VoidAcolyteRegedit : EnemyRegedit
{
    public VoidAcolyteRegedit()
    {
        CharacterName = "虚空侍从";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/VoidAcolyte.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/VoidAcolyte.tscn");

        MaxLife = 23;
        Power = 8;
        Survivability = 5;
        Speed = 8;
        SkillIDs =
        [
            SkillID.VoidAcolyteAttack,
            SkillID.VoidAcolyteSurvive,
            SkillID.VoidAcolyteSpecial,
        ];

        PassiveName = global::VoidAcolyte.PassiveNameText;
        PassiveDescription = global::VoidAcolyte.PassiveDescriptionText;
    }
}

public partial class VoidAcolyteAttack : Skill
{
    private const int BaseDamage = 15;

    public VoidAcolyteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "空洞刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackPrimaryStep(baseDamage: BaseDamage, 2));
    }
}

public partial class VoidAcolyteSurvive : Skill
{
    private const int BaseBlock = 10;
    private const int PowerGain = 5;

    public VoidAcolyteSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "空壳蓄势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public partial class VoidAcolyteSpecial : Skill
{
    private const string TargetKey = "void-acolyte-target";
    private const int VoidCardsInserted = 2;

    public VoidAcolyteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚空灌注";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(30, powerMultiplier: 1, storeAs: TargetKey),
            AddStatusCardsToDrawPileStep(SkillID.VoidStatus, VoidCardsInserted, TargetKey)
        );
    }
}
