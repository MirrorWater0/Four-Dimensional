using System.Threading.Tasks;
using Godot;

public partial class VoidAcolyte : EnemyCharacter
{
    private const int StartRebirthStacks = 5;

    public const string PassiveNameText = "虚壳复苏";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartRebirthStacks}层{Buff.BuffName.RebirthI.GetDescription()}。";

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

        MaxLife = 20;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
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
    private const int BaseDamage = 11;

    public VoidAcolyteAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "空洞刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, times: 2));
    }
}

public partial class VoidAcolyteSurvive : Skill
{
    private const int BaseBlock = 10;
    private const int PowerGain = 2;

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
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Power, PowerGain, TargetReference.Next)
        );
    }
}

public partial class VoidAcolyteSpecial : Skill
{
    private const int VoidCardsInserted = 2;

    public VoidAcolyteSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚空灌注";
    public override int EnergyCost => 6;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(17, multiplier: 1, target: HostileTargetReference.All),
            AddStatusCardsToDrawPileStep(
                SkillID.VoidStatus,
                VoidCardsInserted,
                HostileTargetReference.AttackKey
            )
        );
    }
}
