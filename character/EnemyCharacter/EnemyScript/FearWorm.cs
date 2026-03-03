using System;
using System.Linq;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    public override void Initialize()
    {
        base.Initialize();
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, this, 1);
    }

    public override async void Passive(Skill skill)
    {
        await IncreaseProperties(PropertyType.Power, 2);
    }

    public override void EndAction()
    {
        base.EndAction();
        Passive(new Skill(Skill.SkillTypes.Survive));
    }
}

public partial class FearWormAttack : Skill
{
    private const int BaseDamage = 2;
    private const int VulnerableStacks = 1;
    private const int MaxTargets = 3;
    private const int EnergyGain = 1;

    public FearWormAttack()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恐惧咬噬";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            AoeDamageStep(baseDamage: BaseDamage, maxTargets: MaxTargets, times: 1, clampMax: 999),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                maxTargets: MaxTargets,
                energyCost: 0
            )
        );
    }
}

public partial class FearWormSurvive : Skill
{
    private const int DebuffImmunityStacks = 1;
    private const int BaseBlock = 10;

    public FearWormSurvive()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "蜕皮潜伏";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                index: 0,
                dyingFilter: false,
                energyCost: 0
            ),
            SelfBlockStep(BaseBlock),
            CarryRelativeAllyStep(relativeIndex: 1, skillIndex: 0, dyingFilter: false)
        );
    }
}

public partial class FearWormTermin : Skill
{
    private const int BaseDamage = 8;
    private const int PowerDown = 3;
    private const int StunStacks = 1;
    private const int Cost = 2;

    public FearWormTermin()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "梦魇缠绕";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(PropertyType.Power, PowerDown),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                maxTargets: 1,
                energyCost: Cost
            ),
            AttackPrimaryStep(BaseDamage)
        );
    }
}
