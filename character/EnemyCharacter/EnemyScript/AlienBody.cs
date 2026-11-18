using System.Threading.Tasks;
using Godot;

public partial class AlienBody : EnemyCharacter
{
    private static int StartDebuffImmunityStacks => EnemyTuning.GetRequiredInt("AlienBody", nameof(StartDebuffImmunityStacks));

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
        PortaitPath = "res://asset/EnemyCharater/AlienBody.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/AlienBody.tscn");
        SkillIDs = [SkillID.AlienBodyAttack];
        // 已有减生存，只有缺少优先施加者的多人阵容启用第三轮护壳脆弱。
        IntentionCycle = [random, SkillID.AlienBodyAttack, SkillID.AlienBodySurvive];
        RandomizeIntentionCycleStart = true;
        PassiveName = global::AlienBody.PassiveNameText;
        PassiveDescription = global::AlienBody.PassiveDescriptionText;
    }
}

public partial class AlienBodyAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "异体冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                V("SurvivabilityLoss"),
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class AlienBodySurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "异体护壳";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain")),
            AddCardsStep(
                SkillID.DazeStatus,
                V("DazeCount"),
                BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}

public partial class AlienBodySpecial : Skill
{
    private int PowerDown => V(nameof(PowerDown));
    private int SurvivabilityDown => V(nameof(SurvivabilityDown));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = I18n.Tr("skill.alien_body_special.name", "异体压制");

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage"), target: HostileTargetReference.RandomPreview, multiplier: V("Multiplier"), times: V("HitCount")),
            AddCardsStep(SkillID.DazeStatus, V("DazeCount")),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown, HostileTargetReference.One),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                HostileTargetReference.One
            )
        );
    }
}
