using System.Threading.Tasks;
using Godot;

public partial class VoidAcolyte : EnemyCharacter
{
    private static int StartRebirthStacks => EnemyTuning.GetRequiredInt("VoidAcolyte", nameof(StartRebirthStacks));

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
        PortaitPath = "res://asset/EnemyCharater/VoidAcolyte.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/VoidAcolyte.tscn");
        SkillIDs =
        [
            SkillID.VoidAcolyteAttack,
            SkillID.VoidAcolyteSpecial,
        ];
        // 第三轮蓄势随机施加脆弱，第四轮全阵虚空灌注兑现压力。
        IntentionCycle = [SkillID.VoidAcolyteSurvive, random, SkillID.VoidAcolyteAttack, SkillID.VoidAcolyteSpecial];

        PassiveName = global::VoidAcolyte.PassiveNameText;
        PassiveDescription = global::VoidAcolyte.PassiveDescriptionText;
    }
}

public partial class VoidAcolyteAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "空洞刺击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage, times: V("HitCount"), multiplier: V("Multiplier")));
    }
}

public partial class VoidAcolyteSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int PowerGain => V(nameof(PowerGain));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "空壳蓄势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffHostile(Buff.BuffName.Frail, V("FrailStacks"), HostileTargetReference.RandomPreview)
        );
    }
}

public partial class VoidAcolyteSpecial : Skill
{
    private int VoidCardsInserted => V(nameof(VoidCardsInserted));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "虚空灌注";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(V("BaseDamage"), multiplier: V("Multiplier"), target: HostileTargetReference.All, times: V("HitCount")),
            AddCardsStep(
                SkillID.VoidStatus,
                VoidCardsInserted,
                pileTarget: BattleCardPileTarget.DiscardPileCards
            )
        );
    }
}
