using System.Threading.Tasks;
using Godot;

public partial class Ferociouess : EnemyCharacter
{
    private const int StartDamageImmuneStacks = 3;
    private const int TurnEndDamageImmuneStacks = 1;

    public const string PassiveNameText = "骨骼硬化";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。\n"
        + $"阵营回合结束时：获得{TurnEndDamageImmuneStacks}层{Buff.BuffName.DamageImmune.GetDescription()}。";

    public override string CharacterName { get; set; } = "Ferociouess";

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
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, StartDamageImmuneStacks, this);
        return Task.CompletedTask;
    }

    public override void OnTurnEnd()
    {
        using var _ = BeginEffectSource("被动");
        HurtBuff.BuffAdd(Buff.BuffName.DamageImmune, this, TurnEndDamageImmuneStacks, this);
        base.OnTurnEnd();
    }
}

public partial class FerociouessRegedit : EnemyRegedit
{
    public FerociouessRegedit()
    {
        CharacterName = "Ferociouess";
        PType = EnemyPositionType.FrontRow;
        PortaitPath = "res://asset/EnemyCharater/Ferociouess.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/Ferociouess.tscn");

        MaxLife = 15;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.FerociouessAttack, SkillID.FerociouessSurvive];

        PassiveName = global::Ferociouess.PassiveNameText;
        PassiveDescription = global::Ferociouess.PassiveDescriptionText;
    }
}

public partial class FerociouessAttack : Skill
{
    private const int BaseDamage = 10;
    private const int SelfPowerGain = 2;

    public FerociouessAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "凶恶冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, target: HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Power, SelfPowerGain)
        );
    }
}

public partial class FerociouessSurvive : Skill
{
    private const int BaseBlock = 10;
    private const int DamageImmuneStacks = 0;
    private const int VulnerableStacks = 2;

    public FerociouessSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "骨骼免疫";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Self
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class FerociouessSpecial : Skill
{
    private const int HitCount = 2;

    public FerociouessSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "狂暴";
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 5, times: HitCount, target: HostileTargetReference.All)
        );
    }
}
