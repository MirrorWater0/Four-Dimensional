using System.Threading.Tasks;
using Godot;

public partial class Arrogance : EnemyCharacter
{
    private const int StartStunStacks = 2;

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

        MaxLife = 125;
        Power = 15;
        Survivability = 15;
        Speed = 50;
        SkillIDs = [SkillID.ArroganceAttack, SkillID.ArroganceSurvive, SkillID.ArroganceSpecial];

        PassiveName = global::Arrogance.PassiveNameText;
        PassiveDescription = global::Arrogance.PassiveDescriptionText;
    }
}

public partial class ArroganceAttack : Skill
{
    private const int BaseDamage = 4;

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
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.Two),
            LowerTargetPropertyStep(PropertyType.Survivability, 5, HostileTargetReference.AttackKey)
        );
    }
}

public partial class ArroganceSurvive : Skill
{
    private const int BaseBlock = 0;
    private const int VulnerableStacks = 1;

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
            ModifyPropertyStep(PropertyType.Survivability, 2),
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
    public override int EnergyCost => 10;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            HealStep(baseHeal: 0, target: TargetReference.Self),
            ModifyPropertyStep(PropertyType.Power, 2),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Pursuit,
                stacks: PursuitStacks,
                target: TargetReference.Self
            )
        );
    }
}
