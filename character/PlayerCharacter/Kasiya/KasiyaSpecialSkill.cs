using System.Linq;
using Godot;

public partial class KasiyaSpecialSkill : Node { }

public class TerminateLight : Skill
{
    private const int BaseDamage = 5;
    private const int PowerGain = 5;

    public TerminateLight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终末之光";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage, powerMultiplier: 3),
            HurtFriendly(17, 0),
            ModifyPropertyStep(PropertyType.Power, -2),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public class HolySeal : Skill
{
    private const int BaseDamage = 6;
    private const int StunStacks = 1;

    public HolySeal()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣光封印";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: BaseDamage),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Stun,
                stacks: StunStacks,
                target: HostileTargets(1)
            )
        );
    }
}

public class AegisPledge : Skill
{
    private const int PowerGain = 3;
    private const int BarricadeStacks = 1;

    public AegisPledge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "壁垒誓约";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Barricade,
                stacks: BarricadeStacks,
                target: RelativeTarget(0)
            )
        );
    }
}

public class VulnerabilityConversion : Skill
{
    public VulnerabilityConversion()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "万军取敌";
    public override int EnergyCost => 2;

    private static int GetVulnerableStacks(Character target) =>
        target
            ?.HurtBuffs?.FirstOrDefault(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Vulnerable && buff.Stack > 0
            )
            ?.Stack ?? 0;

    private int GetTotalHostileVulnerableStacks()
    {
        return OwnerCharater
                ?.BattleNode?.GetOrderedTeamCharacters(
                    !OwnerCharater.IsPlayer,
                    includeSummons: true,
                    dyingFilter: true
                )
                ?.Sum(GetVulnerableStacks) ?? 0;
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackPrimaryStep(baseDamage: 0, powerMultiplier: 2),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: 1,
                target: HostileTargets(1)
            ),
            ModifyPropertyStep(
                PropertyType.Power,
                _ => GetTotalHostileVulnerableStacks(),
                RelativeTarget(0)
            ),
            TextStep(
                $"获得等同于敌方所有角色{Buff.BuffName.Vulnerable.GetDescription()}层数总和的力量。"
            )
        );
    }
}

public class DemonForm : Skill
{
    private const int DemonStacks = 6;

    public DemonForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "恶魔形态";
    public override int EnergyCost => 6;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Demon,
                stacks: DemonStacks,
                target: RelativeTarget(0)
            )
        );
    }
}
