using System;
using System.Threading.Tasks;
using Godot;

public partial class RedHusk : EnemyCharacter
{
    private const int StartAutoArmorStacks = 12;

    public override void Initialize()
    {
        base.Initialize();
        BattleNode.StartEffectList.Add(StartPassive);
    }

    public Task StartPassive()
    {
        HurtBuff.BuffAdd(Buff.BuffName.AutoArmor, this, StartAutoArmorStacks);
        return Task.CompletedTask;
    }
}

public partial class RedHuskAttack : Skill
{
    private const int BaseDamage = 20;
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
                maxTargets: 1,
                byBehindRow: true
            )
        );
    }
}

public partial class RedHuskSurvive : Skill
{
    private const int BaseBlock = 3;
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
            BlockFriendlyByRelativeStep(0, BaseBlock, 2),
            LowerTargetPropertyStep(
                PropertyType.Power,
                PowerDown,
                maxTargets: 1,
                permanent: false,
                byBehindRow: true
            ),
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivabilityDown,
                maxTargets: 1,
                permanent: false,
                byBehindRow: true
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
                index: 0,
                dyingFilter: false
            ),
            EnergyTimesGateStep(
                energyCost: RebirthEnergyCost,
                onPassSteps:
                [
                    ApplyBuffFriendly(
                        buffName: Buff.BuffName.RebirthI,
                        stacks: RebirthStacks,
                        index: 0,
                        dyingFilter: false
                    ),
                    ModifyPropertyStep(PropertyType.Survivability, 5),
                ]
            )
        );
    }
}
