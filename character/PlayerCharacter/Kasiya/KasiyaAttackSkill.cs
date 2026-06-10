using System;
using System.Linq;
using Godot;

public partial class KasiyaAttackSkill { }

public partial class Determination : Skill
{
    private const int BaseDamage = 7;
    public override int EnergyCost => 2;

    public Determination()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "剑意已决";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ApplyBuffHostile(Buff.BuffName.Vulnerable, 2, HostileTargetReference.AttackKey)
        );
    }
}

public partial class Smite : Skill
{
    private const int BaseDamage = 7;
    private const int SurvivalDown = 5;

    public Smite()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "绝域剑杀";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            LowerTargetPropertyStep(
                PropertyType.Survivability,
                SurvivalDown,
                HostileTargetReference.One
            ),
            AttackStep(baseDamage: BaseDamage)
        );
    }
}

public partial class Charge : Skill
{
    private const int BaseDamage = 4;

    public Charge()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "冲锋";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            BlockStep(
                baseBlock: _ =>
                    OwnerCharater?.BattleNode?.GetLastRecordedDamageFromCurrentEffectSource(
                        source: OwnerCharater,
                        target: GetAttackTarget(),
                        includeBlockedDamage: true
                    ) ?? 0,
                describe: false,
                multiplier: 0
            ),
            TextStep(
                I18n.Format(
                    "skill.charge.text.block_from_damage_and_survivability",
                    "获得等同于此次造成伤害+{survivability}的格挡。",
                    ("survivability", X(StatX.Survivability))
                )
            )
        );
    }
}

public partial class Vower : Skill
{
    private const int BaseDamage = 7;

    public Vower()
        : base(Skill.SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "誓约者";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            CarryStep(target: TargetReference.ManualFriendly, skillIndex: 2)
        );
    }
}

public partial class VulnerablePurge : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 7;

    public VulnerablePurge()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "弱点突破";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: 1,
                target: HostileTargetReference.All,
                targetCondition: character =>
                    character?.HurtBuffs?.Any(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Vulnerable
                        && buff.Stack > 0
                    ) == true,
                conditionText: $"拥有{Buff.BuffName.Vulnerable.GetDescription()}"
            )
        );
    }
}

public partial class VulnerabilityStrike : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 5;

    public VulnerabilityStrike()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "易伤追击";

    private static bool TargetHasVulnerable(Character target) =>
        target?.HurtBuffs?.Any(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Vulnerable && buff.Stack > 0
        ) == true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            ConditionStep(
                () => TargetHasVulnerable(GetAttackTarget()),
                $"若目标拥有{Buff.BuffName.Vulnerable.GetDescription()}",
                AttackStep(
                    target: HostileTargetReference.AttackKey,
                    baseDamage: BaseDamage,
                    prefix: "额外造成"
                )
            )
        );
    }
}

public class TerminateLight : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BaseDamage = 0;

    public TerminateLight()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终末之光";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 4),
            HurtFriendly(6)
        );
    }
}

public class VulnerabilityConversion : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public VulnerabilityConversion()
        : base(SkillTypes.Attack)
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
            AttackStep(baseDamage: 7, multiplier: 2),
            ModifyPropertyStep(
                PropertyType.Power,
                _ => GetTotalHostileVulnerableStacks(),
                TargetReference.Self
            ),
            TextStep(
                I18n.Format(
                    "skill.vulnerability_conversion.text.total_vulnerable_power",
                    "获得等同于敌方所有角色{buff}层数总和的力量。",
                    ("buff", Buff.BuffName.Vulnerable.GetDescription())
                )
            )
        );
    }
}
