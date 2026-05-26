using System;
using System.Threading.Tasks;
using Godot;

public partial class RedHusk : EnemyCharacter
{
    private const int StartAutoArmorStacks = 5;

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

        MaxLife = 66;
        Power = 17;
        Survivability = 15;
        Speed = 7;
        SkillIDs = [SkillID.RedHuskAttack, SkillID.RedHuskSurvive, SkillID.RedHuskSpecial];

        PassiveName = global::RedHusk.PassiveNameText;
        PassiveDescription = global::RedHusk.PassiveDescriptionText;
    }
}

public partial class RedHuskAttack : Skill
{
    private const int BaseDamage = 0;
    private const int VulnerableStacks = 2;

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
            AttackStep(baseDamage: BaseDamage, multiplier: 1, target: HostileTargetReference.Two),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class RedHuskSurvive : Skill
{
    private const int BaseBlock = 0;
    private const int SurvivabilityDown = 3;

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
            ModifyPropertyStep(PropertyType.Power, 5)
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
    public override int EnergyCost => 4;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: 0),
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
