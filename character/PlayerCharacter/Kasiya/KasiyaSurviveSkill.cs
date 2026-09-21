using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private const int VulnerableStacks = 1;
    private const int BaseBlock = 0;
    public override int EnergyCost => XEnergyCost;
    public override string SkillName { get; set; } = "冲击波";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                loopSteps:
                [
                    BlockStep(baseBlock: BaseBlock),
                    ApplyBuffHostile(
                        buffName: Buff.BuffName.Vulnerable,
                        stacks: VulnerableStacks,
                        target: HostileTargetReference.All
                    ),
                    ApplyBuffHostile(
                        buffName: Buff.BuffName.Weaken,
                        stacks: VulnerableStacks,
                        target: HostileTargetReference.All
                    ),
                ]
            )
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 2;
    private const int SurvivabilityGain = 2;

    public override string SkillName { get; set; } = "重振精神";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: V("BaseBlock", 0)),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    public override string SkillName { get; set; } = "绝对防御";
    public override int EnergyCost => XEnergyCost;
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            WhileStep(
                loopSteps: [BlockStep(baseBlock: V("BaseBlock", 6), multiplier: V("Multiplier", 1))]
            ),
            ApplyBuffFriendly(Buff.BuffName.Taunt, V("TauntStacks", 1))
        );
    }
}

public partial class TauntingGuard : Skill
{
    private const int TauntStacks = 2;
    private const int BaseBlock = 5;

    public override string SkillName { get; set; } = "嘲讽守势";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Taunt,
                stacks: TauntStacks,
                target: TargetReference.Self
            ),
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 1))
        );
    }
}

public partial class WeakpointBulwark : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 7;

    public override string SkillName { get; set; } = "蓄势待发";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock),
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
                    EndActionBuff.BuffAdd(name, target, stacks, source);
                    break;
            }
        }
    }
}

public partial class Purification : Skill
{
    public override string SkillName { get; set; } = "净化";

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 0)),
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

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: 0, target: TargetReference.ManualFriendly, describe: false),
            new DoubleBlockStep()
        );
    }

    private sealed class DoubleBlockStep : SkillStep
    {
        public override Task Execute(Skill skill)
        {
            Character target = skill?.GetManualFriendlyTarget();
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
            yield return "令选择的己方角色的格挡翻倍。";
        }
    }
}

public partial class Resistance : Skill
{
    public override string SkillName { get; set; } = "抵御";
    public override int EnergyCost => Cost(2);
    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 4), TargetReference.ManualFriendly),
            ExhaustCardsStep(BattleCardPileTarget.HandCards, V("ExhaustCount", 2), false)
        );
    }
}
