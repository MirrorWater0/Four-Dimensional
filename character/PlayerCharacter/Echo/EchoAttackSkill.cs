using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    private const int BaseDamage = 7;
    private const int MaxTargets = 4;

    public SacredOnslaught()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "圣域冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: 1, times: 1),
            CustomStep(
                _ =>
                {
                    ApplyVulnerableByTargetWeaken();
                    return Task.CompletedTask;
                },
                _ =>
                {
                    return new[]
                    {
                        $"给予攻击目标等同于目标{Buff.BuffName.Weaken.GetDescription()}层数的{Buff.BuffName.Vulnerable.GetDescription()}。",
                    }.Where(line => !string.IsNullOrWhiteSpace(line));
                }
            )
        );
    }

    private void ApplyVulnerableByTargetWeaken()
    {
        foreach (var target in GetAttackTargets())
        {
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || target.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            int weakenStacks = GetWeakenStacks(target);
            if (weakenStacks <= 0)
                continue;

            HurtBuff.BuffAdd(Buff.BuffName.Vulnerable, target, weakenStacks, OwnerCharater);
        }
    }

    private static int GetWeakenStacks(Character target) =>
        target
            ?.AttackBuffs?.Where(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Weaken && buff.Stack > 0
            )
            .Sum(buff => buff.Stack) ?? 0;
}

public partial class ResonantSlash : Skill
{
    private const int BaseDamage = 7;
    private const int UpgradeDamageBonus = 2;

    public ResonantSlash()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "共振斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: UpAdd(BaseDamage, UpgradeDamageBonus), times: 1),
            ApplyBuffHostile(Buff.BuffName.Weaken, 2, HostileTargetReference.AttackKey)
        );
    }
}

public partial class EchoPuncture : Skill
{
    private const int BaseDamage = 0;
    private const int VulnerableStacks = 1;

    public EchoPuncture()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声穿刺";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: 2),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            )
        );
    }
}

public partial class Extract : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 7;

    public Extract()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "萃取";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, AttackStep(baseDamage: BaseDamage), DrawCardsStep(2));
    }
}

public partial class BladeOfSlaughter : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 7;

    public BladeOfSlaughter()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "弑杀之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            CarryStep(target: TargetReference.Previous, skillIndex: 1)
        );
    }
}

public partial class DisasterImpact : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private const int BaseDamage = 7;
    private const int WeakenStacksPerExtraDraw = 2;

    public DisasterImpact()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "灵魂汲取";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage),
            TextStep(GetExtraDrawFromTargetWeakenText()),
            DrawCardsStep(_ => GetExtraDrawStacksFromAttackTarget())
        );
    }

    private static string GetExtraDrawFromTargetWeakenText()
    {
        string weakenText = Buff.BuffName.Weaken.GetDescription();
        string extraDrawText = Buff.GetBuffDisplayName(Buff.BuffName.ExtraDraw);
        return $"攻击目标每有{WeakenStacksPerExtraDraw}层{weakenText}，抽{1}张。";
    }

    private int GetExtraDrawStacksFromAttackTarget() =>
        GetAttackTargetWeakenStacks() / WeakenStacksPerExtraDraw;

    private int GetAttackTargetWeakenStacks()
    {
        Character target = ChosetargetByOrder(byBehindRow: false, applyTaunt: true)
            .FirstOrDefault();
        return target
                ?.AttackBuffs?.FirstOrDefault(buff =>
                    buff != null && buff.ThisBuffName == Buff.BuffName.Weaken && buff.Stack > 0
                )
                ?.Stack ?? 0;
    }
}

public class EchonicResonance : Skill
{
    private const int PowerGainPerCast = 1;

    public EchonicResonance()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回响共鸣";
    public override int EnergyCost => XEnergyCost;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                loopSteps:
                [
                    AttackStep(baseDamage: 0, multiplier: 1),
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerCast),
                ]
            )
        );
    }
}

public class SonicBoom : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 0;
    private const int ExtraTimes = 2;

    public SonicBoom()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "音爆";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => 4,
                loopSteps: [AttackStep(BaseDamage, target: HostileTargetReference.All)]
            )
        );
    }
}

public class PhaseEcho : Skill
{
    int damage = 12;
    int PowerGain = -2;

    public PhaseEcho()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "相位回声";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: damage, target: HostileTargetReference.All),
            ModifyPropertyStep(PropertyType.Power, PowerGain)
        );
    }
}

public class ReverbChain : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseDamage = 0;

    public ReverbChain()
        : base(SkillTypes.Attack)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "回声连奏";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep(
                I18n.Tr(
                    "skill.reverb_chain.text.loop_by_allied_actions",
                    "释放x次(x为本场战斗经过的回合数)。"
                )
            ),
            WhileStep(
                times: GetLoopTimes,
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: 1)]
            )
        );
    }

    private int GetLoopTimes() =>
        OwnerCharater?.BattleNode?.GetElapsedTurnCount(OwnerCharater) ?? 0;
}
