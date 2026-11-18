using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SacredOnslaught : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override string SkillName { get; set; } = "圣域冲击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                multiplier: V("Multiplier"),
                times: V("HitCount")
            ),
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
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "共振斩击";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, times: V("HitCount"), multiplier: V("Multiplier")),
            ApplyBuffHostile(
                Buff.BuffName.Weaken,
                V("WeakenStacks"),
                HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class EchoPuncture : Skill
{
    private int BaseDamage => V(nameof(BaseDamage));
    private int VulnerableStacks => V(nameof(VulnerableStacks));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "回声穿刺";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                times: V("HitCount"),
                target: HostileTargetReference.All,
                multiplier: V("Multiplier")
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.AttackKey
            )
        );
    }
}

public partial class Extract : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private int BaseDamage => V(nameof(BaseDamage));
    private int ExtraDrawStacks => V(nameof(ExtraDrawStacks));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "萃取";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: ExtraDrawStacks,
                target: TargetReference.Previous
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: ExtraDrawStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class BladeOfSlaughter : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "弑杀之刃";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount")),
            PlayDrawPileCardStep(SkillTypes.Attack)
        );
    }
}

public partial class DisasterImpact : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int BaseDamage => V(nameof(BaseDamage));
    private int WeakenStacksPerExtraDraw => V(nameof(WeakenStacksPerExtraDraw));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "灵魂汲取";

    protected override SkillPlan BuildPlan()
    {
        int extraDrawStacks = 0;
        return new SkillPlan(
            this,
            AttackStep(
                baseDamage: BaseDamage,
                onTargetsResolved: targets =>
                    extraDrawStacks = GetTargetWeakenStacks(targets.FirstOrDefault()) / WeakenStacksPerExtraDraw,
                multiplier: V("Multiplier"),
                times: V("HitCount")
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: _ => extraDrawStacks,
                target: TargetReference.Self,
                hideDescription: true
            ),
            TextStep(GetExtraDrawFromTargetWeakenText())
        );
    }

    private string GetExtraDrawFromTargetWeakenText()
    {
        string weakenText = Buff.BuffName.Weaken.GetDescription();
        string extraDrawText = Buff.GetBuffDisplayName(Buff.BuffName.ExtraDraw);
        return $"攻击目标每有{WeakenStacksPerExtraDraw}层{weakenText}，获得{1}层{extraDrawText}。";
    }

    private static int GetTargetWeakenStacks(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return 0;

        return target
                ?.AttackBuffs?.FirstOrDefault(buff =>
                    buff != null && buff.ThisBuffName == Buff.BuffName.Weaken && buff.Stack > 0
                )
                ?.Stack ?? 0;
    }
}

public class EchonicResonance : Skill
{
    private int PowerGainPerCast => V(nameof(PowerGainPerCast));

    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "回响共鸣";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                loopSteps:
                [
                    AttackStep(baseDamage: V("BaseDamage"), multiplier: V("Multiplier"), times: V("HitCount")),
                    ModifyPropertyStep(PropertyType.Power, PowerGainPerCast),
                ]
            )
        );
    }
}

public class SonicBoom : Skill
{
    public override SkillRarity Rarity => SkillRarity.Common;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "音爆";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                times: () => V("LoopCount"),
                loopSteps: [AttackStep(BaseDamage, target: HostileTargetReference.All, multiplier: V("Multiplier"), times: V("HitCount"))]
            )
        );
    }
}

public class PhaseEcho : Skill
{
    private int damage => V("BaseDamage");

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "相位回声";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AttackStep(baseDamage: damage, target: HostileTargetReference.One, multiplier: V("Multiplier"), times: V("HitCount")),
            ShuffleDiscardPileIntoDrawPileStep()
        );
    }
}

public class ReverbChain : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseDamage => V(nameof(BaseDamage));

    public override SkillTypes SkillType => SkillTypes.Attack;

    public override string SkillName { get; set; } = "回声连奏";

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
                loopSteps: [AttackStep(baseDamage: BaseDamage, multiplier: V("Multiplier"), times: V("HitCount"))]
            )
        );
    }

    private int GetLoopTimes() =>
        OwnerCharater?.BattleNode?.GetElapsedTurnCount(OwnerCharater) ?? 0;
}
