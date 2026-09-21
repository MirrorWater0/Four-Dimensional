using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private const int StartDebuffImmunityStacks = 1;

    public const string PassiveNameText = "寄生馈赠";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartDebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。";

    public override string CharacterName { get; set; } = "AlienBody";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    private Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, this, StartDebuffImmunityStacks, this);
        return Task.CompletedTask;
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

        MaxLife = 32;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.AlienBodyAttack, SkillID.AlienBodySurvive];

        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
    }
}

public partial class AlienBodyAttack : Skill
{
    private const int BaseDamage = 13;

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "异体冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(BaseDamage),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                V("SurvivabilityLoss", 2),
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private const int BaseBlock = 8;
    private const int SurvivabilityDown = 4;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "异体护壳";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ModifyPropertyStep(PropertyType.Power, 2),
            AddCardsStep(
                SkillID.DazeStatus,
                V("DazeCount", 2),
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}

public partial class AlienBodySpecial : Skill
{
    private const int PowerDown = 1;
    private const int SurvivabilityDown = 1;

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "共生连携";
    public override int EnergyCost => Cost(4);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage", 16), target: HostileTargetReference.RandomPreview),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount", 1)),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, HostileTargetReference.One),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.One
            )
        );
    }
}
