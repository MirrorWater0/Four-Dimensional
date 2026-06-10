using System.Threading.Tasks;
using Godot;

public partial class Arrogance : EnemyCharacter
{
    private const int StartStunStacks = 1;

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
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Arrogance.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Arrogance.tscn");

        MaxLife = 173;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.ArroganceAttack, SkillID.ArroganceSurvive, SkillID.ArroganceSpecial];
        OpeningIntentionSkillIDs = [SkillID.ArroganceSpecial, SkillID.ArroganceAttack];
        PassiveName = global::Arrogance.PassiveNameText;
        PassiveDescription = global::Arrogance.PassiveDescriptionText;
    }
}

public partial class ArroganceAttack : Skill
{
    private const int BaseDamage = 12;

    public ArroganceAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "双重压制";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            LowerTargetPropertyStep(PropertyType.Survivability, 4, HostileTargetReference.AttackKey)
        );
    }
}

public partial class ArroganceSurvive : Skill
{
    private const int BaseBlock = 20;
    private const int VulnerableStacks = 2;

    public ArroganceSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "暗黑吞噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
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
    private const int PursuitStacks = 3;

    public ArroganceSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "虚无追击";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(baseHeal: 10, target: TargetReference.Self),
            ModifyPropertyStep(PropertyType.Power, 3),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Pursuit,
                stacks: PursuitStacks,
                target: TargetReference.Self
            )
        );
    }
}
