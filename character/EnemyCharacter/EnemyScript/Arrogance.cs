using System.Threading.Tasks;
using Godot;

public partial class Arrogance : EnemyCharacter
{
    private static int StartStunStacks => EnemyTuning.GetRequiredInt("Arrogance", nameof(StartStunStacks));

    public const string PassiveNameText = "傲慢";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartStunStacks}层{Buff.BuffName.Stun.GetDescription()}。";

    public override string CharacterName { get; set; } = "Arrogance";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        BattleNode.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        using var _ = BeginEffectSource("被动");
        SkillBuff.BuffAdd(Buff.BuffName.Stun, this, StartStunStacks, this);
        return Task.CompletedTask;
    }
}

public partial class ArroganceRegedit : EnemyRegedit
{
    public ArroganceRegedit()
    {
        CharacterName = "Arrogance";
        PortaitPath = "res://asset/EnemyCharater/Arrogance.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Arrogance.tscn");
        SkillIDs = [SkillID.ArroganceAttack, SkillID.ArroganceSurvive, SkillID.ArroganceSpecial];
        // 眩晕结束后追击蓄势，吞噬铺易伤，再连续两次全阵压制。
        IntentionCycle =
        [
            SkillID.ArroganceSpecial,
            SkillID.ArroganceSurvive,
            SkillID.ArroganceAttack,
        ];
        PassiveName = global::Arrogance.PassiveNameText;
        PassiveDescription = global::Arrogance.PassiveDescriptionText;
    }
}

public partial class ArroganceAttack : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "双重压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount")),
            LowerTargetPropertyStep(PropertyType.Survivability, V("SurvivabilityLoss"), HostileTargetReference.AttackKey)
        );
    }
}

public partial class ArroganceSurvive : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int VulnerableStacks => V(nameof(VulnerableStacks));

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "暗黑吞噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class ArroganceSpecial : Skill
{
    private int PursuitStacks => V(nameof(PursuitStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "虚无追击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(baseHeal: V("BaseHeal"), target: TargetReference.Self),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Pursuit,
                stacks: PursuitStacks,
                target: TargetReference.Self
            )
        );
    }
}
