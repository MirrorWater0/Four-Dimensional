using System;
using System.Threading.Tasks;
using Godot;

public partial class RedHusk : EnemyCharacter
{
    private const int StartAutoArmorStacks = 3;

    public const string PassiveNameText = "赤壳护盾";
    public static string PassiveDescriptionText =>
        $"战斗开始时：获得{StartAutoArmorStacks}层{Buff.BuffName.AutoArmor.GetDescription()}。";

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
        HurtBuff.BuffAdd(Buff.BuffName.AutoArmor, this, StartAutoArmorStacks, this);
        return Task.CompletedTask;
    }
}

public partial class RedHuskRegedit : EnemyRegedit
{
    public RedHuskRegedit()
    {
        CharacterName = "RedHusk";
        PType = EnemyPositionType.BackRow;
        PortaitPath = "res://asset/EnemyCharater/RedHusk.png";
        CharacterScene = GD.Load<PackedScene>("res://character/EnemyCharacter/RedHusk.tscn");

        MaxLife = 50;
        Power = 0;
        Survivability = 0;
        BasePowerContribution = 0;
        BaseSurvivabilityContribution = 0;
        SkillIDs = [SkillID.RedHuskAttack, SkillID.RedHuskSurvive, SkillID.RedHuskSpecial];

        PassiveName = global::RedHusk.PassiveNameText;
        PassiveDescription = global::RedHusk.PassiveDescriptionText;
    }
}

public partial class RedHuskAttack : Skill
{
    private const int BaseDamage = 7;

    public RedHuskAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "重拳突刺";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1, target: HostileTargetReference.All)
        );
    }
}

public partial class RedHuskSurvive : Skill
{
    private const int BaseBlock = 20;

    public RedHuskSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "聚合压缩";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: 2),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, 2, HostileTargetReference.All)
        );
    }
}

public partial class RedHuskSpecial : Skill
{
    private const int AutoArmorStacks = 3;
    private const int RebirthStacks = 1;

    public RedHuskSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护壳重生";
    public override int EnergyCost => 7;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 14),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.AutoArmor,
                stacks: AutoArmorStacks,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.RebirthI,
                stacks: RebirthStacks,
                target: TargetReference.Self
            )
        );
    }
}
