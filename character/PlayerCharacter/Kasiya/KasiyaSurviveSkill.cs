using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = -3;

    public override string SkillName { get; set; } = "冲击波";

    public ShockWave()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargets(9)
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: VulnerableStacks,
                target: HostileTargets(9)
            ),
            BlockStep(0, BaseBlock)
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 5;
    private const int SurvivabilityGain = 5;
    public override bool ExhaustsAfterUse => true;

    public override string SkillName { get; set; } = "重振精神";

    public ReNewedSpirit()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(relativeIndex: 0, baseBlock: -3),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    public override string SkillName { get; set; } = "绝对防御";
    int basisBlock = -3;

    public AbsouluteDefense()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, basisBlock, survivabilityMultiplier: 3),
            ApplyBuffFriendly(buffName: Buff.BuffName.Taunt, stacks: 1, target: TargetReference.Self)
        );
    }
}

public partial class TauntingGuard : Skill
{
    private const int TauntStacks = 2;
    private const int BaseBlock = 1;

    public override string SkillName { get; set; } = "嘲讽守势";

    public TauntingGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Taunt,
                stacks: TauntStacks,
                target: TargetReference.Self
            ),
            BlockStep(0, BaseBlock, survivabilityMultiplier: 2)
        );
    }
}

public partial class WeakpointBulwark : Skill
{
    private const int BaseBlock = 10;

    public override string SkillName { get; set; } = "蓄势待发";

    public WeakpointBulwark()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            new DoubleEnemyFormationVulnerableStep()
        );
    }

    private sealed class DoubleEnemyFormationVulnerableStep : SkillStep
    {
        public override Task Execute(Skill skill)
        {
            foreach (var target in GetTargets(skill))
            {
                int vulnerableStacks = GetVulnerableStacks(target);
                if (vulnerableStacks > 0)
                    HurtBuff.BuffAdd(
                        Buff.BuffName.Vulnerable,
                        target,
                        vulnerableStacks,
                        skill?.OwnerCharater
                    );
            }

            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
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

        private static int GetVulnerableStacks(Character target)
        {
            return target
                    ?.HurtBuffs?.FirstOrDefault(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Vulnerable
                        && buff.Stack > 0
                    )
                    ?.Stack ?? 0;
        }
    }
}

public partial class BarrierDuplication : Skill
{
    public override string SkillName { get; set; } = "固守";

    public BarrierDuplication()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    int currentBlock = OwnerCharater?.Block ?? 0;
                    if (currentBlock > 0)
                        OwnerCharater?.UpdataBlock(currentBlock, source: OwnerCharater);

                    return Task.CompletedTask;
                },
                _ => new[] { "格挡翻倍。" }
            )
        );
    }
}
