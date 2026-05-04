using System;
using System.Threading.Tasks;
using Godot;

public partial class RedHusk : EnemyCharacter
{
    private const int StartAutoArmorStacks = 8;

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

        MaxLife = 80;
        Power = 15;
        Survivability = 15;
        Speed = 8;
        SpecialIntentThreshold = 3;

        SkillIDs = [SkillID.RedHuskAttack, SkillID.RedHuskSurvive, SkillID.RedHuskSpecial];

        PassiveName = global::RedHusk.PassiveNameText;
        PassiveDescription = global::RedHusk.PassiveDescriptionText;
    }
}

public partial class RedHuskAttack : Skill
{
    private const int BaseDamage = 18;
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
            AttackPrimaryStep(baseDamage: BaseDamage, byBehindRow: true),
            ModifyPropertyStep(PropertyType.Power, 4),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(1, byBehindRow: true)
            )
        );
    }
}

public partial class RedHuskSurvive : Skill
{
    private const int BaseBlock = 1;
    private const int PowerDown = 3;
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
            BlockStep(0, BaseBlock, 2),
            LowerTargetPropertyStep(
                PropertyType.Power,
                PowerDown,
                target: HostileTargets(1, byBehindRow: true),
                permanent: false
            ),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                target: HostileTargets(1, byBehindRow: true),
                permanent: false
            )
        );
    }
}

public partial class RedHuskSpecial : Skill
{
    private const int AutoArmorStacks = 8;
    private const int RebirthStacks = 1;
    private const int RebirthEnergyCost = 3;

    public RedHuskSpecial()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "护壳重生";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.AutoArmor,
                stacks: AutoArmorStacks,
                target: RelativeTarget(0)
            ),
            EnergyTimesGateStep(
                energyCost: RebirthEnergyCost,
                onPassSteps:
                [
                    ApplyBuffFriendly(
                        buffName: Buff.BuffName.RebirthI,
                        stacks: RebirthStacks,
                        target: RelativeTarget(0)
                    ),
                    ModifyPropertyStep(PropertyType.Survivability, 5),
                ]
            )
        );
    }
}
