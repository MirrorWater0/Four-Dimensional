using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private int WeakenStacks => V(nameof(WeakenStacks));
    private int BaseBlock => V(nameof(BaseBlock));

    public override string SkillName { get; set; } = "冲击波";

    public override SkillTypes SkillType => SkillTypes.Permanent;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private int PowerGain => V(nameof(PowerGain));
    private int SurvivabilityGain => V(nameof(SurvivabilityGain));

    public override string SkillName { get; set; } = "重振精神";

    public override SkillTypes SkillType => SkillTypes.Permanent;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: V("BaseBlock"), multiplier: V("Multiplier")),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    public override string SkillName { get; set; } = "绝对防御";

    public override SkillRarity Rarity => SkillRarity.Rare;

    public override SkillTypes SkillType => SkillTypes.Permanent;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                loopSteps: [BlockStep(baseBlock: V("BaseBlock"), multiplier: V("Multiplier"))]
            ),
            ApplyBuffFriendly(Buff.BuffName.Taunt, V("TauntStacks"))
        );
    }
}

public partial class TauntingGuard : Skill
{
    private int TauntStacks => V(nameof(TauntStacks));
    private int BaseBlock => V(nameof(BaseBlock));

    public override string SkillName { get; set; } = "嘲讽守势";

    public override SkillTypes SkillType => SkillTypes.Permanent;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Taunt,
                stacks: TauntStacks,
                target: TargetReference.Self
            ),
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier"))
        );
    }
}

public partial class WeakpointBulwark : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override string SkillName { get; set; } = "蓄势待发";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            new DoubleEnemyFormationVulnerableStep()
        );
    }

    private sealed class DoubleEnemyFormationVulnerableStep : SkillStep
    {
        public override Task Execute(Skill skill)
        {
            foreach (var target in GetTargets(skill))
            {
                DoubleDebuffs(target, skill?.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            if (skill != null || skill == null)
            {
                yield return "令敌方全阵的负面状态层数翻倍。";
                yield break;
            }

            yield return $"令敌方全阵的{Buff.BuffName.Vulnerable.GetDescription()}层数翻倍。";
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill)
        {
            return GetTargets(skill);
        }

        public override IEnumerable<Character> PreviewHostileDebuffTargets(Skill skill)
        {
            return GetTargets(skill);
        }

        private static Character[] GetTargets(Skill skill)
        {
            Character dummy = skill?.OwnerCharater?.BattleNode?.dummy;
            return skill
                    ?.ChosetargetByOrder(byBehindRow: false)
                    .Where(target => target != null && target != dummy)
                    .ToArray() ?? Array.Empty<Character>();
        }

        private static void DoubleDebuffs(Character target, Character source)
        {
            if (target == null)
                return;

            foreach (var buff in GetNegativeBuffs(target).ToArray())
                AddDebuffStacks(buff.ThisBuffName, target, buff.Stack, source);
        }

        private static IEnumerable<Buff> GetNegativeBuffs(Character target)
        {
            IEnumerable<Buff> buffs = Enumerable.Empty<Buff>();
            if (target.HurtBuffs != null)
                buffs = buffs.Concat(target.HurtBuffs);
            if (target.AttackBuffs != null)
                buffs = buffs.Concat(target.AttackBuffs);
            if (target.SkillBuffs != null)
                buffs = buffs.Concat(target.SkillBuffs);
            if (target.EndActionBuffs != null)
                buffs = buffs.Concat(target.EndActionBuffs);

            return buffs.Where(buff =>
                buff != null && buff.Stack > 0 && Buff.IsDebuff(buff.ThisBuffName)
            );
        }

        private static void AddDebuffStacks(
            Buff.BuffName name,
            Character target,
            int stacks,
            Character source
        )
        {
            if (stacks <= 0)
                return;

            switch (name)
            {
                case Buff.BuffName.Vulnerable:
                    HurtBuff.BuffAdd(name, target, stacks, source);
                    break;
                case Buff.BuffName.Weaken:
                    AttackBuff.BuffAdd(name, target, stacks, source);
                    break;
                case Buff.BuffName.Stun:
                    SkillBuff.BuffAdd(name, target, stacks, source);
                    break;
                case Buff.BuffName.Disaster:
                case Buff.BuffName.Frail:
                    EndActionBuff.BuffAdd(name, target, stacks, source);
                    break;
            }
        }
    }
}

public partial class Purification : Skill
{
    public override string SkillName { get; set; } = "净化";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                skill =>
                    skill?.OwnerCharater?.BattleNode?.ExhaustAllPlayerBattleStatusCardsAsync(
                        skill.OwnerCharater
                    ) ?? Task.CompletedTask,
                _ => new[] { "消耗所有状态牌。" }
            )
        );
    }
}

public partial class BarrierDuplication : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override string SkillName { get; set; } = "固守";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: V("BaseBlock"), target: TargetReference.Self, describe: false, multiplier: V("Multiplier")),
            new DoubleBlockStep()
        );
    }

    private sealed class DoubleBlockStep : SkillStep
    {
        public override Task Execute(Skill skill)
        {
            Character target = skill?.OwnerCharater;
            if (target == null)
                return Task.CompletedTask;

            int currentBlock = target.Block;
            if (currentBlock > 0)
            {
                target.UpdataBlock(currentBlock, source: skill.OwnerCharater);
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            yield return "令自身的格挡翻倍。";
        }
    }
}

public partial class Resistance : Skill
{
    public override string SkillName { get; set; } = "抵御";

    public override SkillTypes SkillType => SkillTypes.Permanent;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock"), TargetReference.Self, multiplier: V("Multiplier")),
            ExhaustCardsStep(BattleCardPileTarget.HandCards, V("ExhaustCount"), false)
        );
    }
}
