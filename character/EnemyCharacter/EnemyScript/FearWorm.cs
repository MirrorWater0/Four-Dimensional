using System;
using System.Linq;
using Godot;

public partial class FearWorm : EnemyCharacter
{
    private const int PassiveDebuffImmunityStacks = 1;
    private const int PassiveEndActionPowerGain = 2;

    public const string PassiveNameText = "蜕皮";
    public static string PassiveDescriptionText =>
        $"初始：获得{PassiveDebuffImmunityStacks}层{Buff.BuffName.DebuffImmunity.GetDescription()}。\n"
        + $"回合结束时：获得{PassiveEndActionPowerGain}点力量。";

    public override void Initialize()
    {
        base.Initialize();
        PassiveName = PassiveNameText;
        PassiveDescription = PassiveDescriptionText;
        using var _ = BeginEffectSource("被动");
        SpecialBuff.BuffAdd(Buff.BuffName.DebuffImmunity, this, PassiveDebuffImmunityStacks, this);
    }

    public override async void Passive(Skill skill)
    {
        using var _ = BeginEffectSource("被动");
        await IncreaseProperties(PropertyType.Power, PassiveEndActionPowerGain, this);
    }

    public override void OnTurnEnd()
    {
        TriggerPassive(new Skill(Skill.SkillTypes.Survive));
        base.OnTurnEnd();
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
            AoeDamageStep(
                baseDamage: BaseDamage,
                target: HostileTargets(MaxTargets),
                times: 1,
                clampMax: 999
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(MaxTargets)
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
                target: RelativeTarget(0)
            ),
            BlockStep(0, BaseBlock),
            CarryStep(target: RelativeTarget(1), skillIndex: 0)
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
            EnergyTimesGateStep(
                energyCost: Cost,
                onPassSteps:
                [
                    ApplyBuffHostile(
                        buffName: Buff.BuffName.Stun,
                        stacks: StunStacks,
                        target: HostileTargets(1)
                    ),
                ]
            ),
            LowerTargetPropertyStep(PropertyType.Power, PowerDown),
            AttackPrimaryStep(BaseDamage)
        );
    }
}
