using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class KasiyaSurviveSkill { }

public partial class ShockWave : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 5;

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
                maxTargets: 9
            ),
            BlockStep(0, BaseBlock)
        );
    }
}

public partial class ReNewedSpirit : Skill
{
    private const int PowerGain = 5;
    private const int SurvivabilityGain = 5;

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
            BlockStep(relativeIndex: 0, baseBlock: 12),
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain)
        );
    }
}

public partial class AbsouluteDefense : Skill
{
    public override string SkillName { get; set; } = "绝对防御";
    int basisBlock = 0;

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
            ApplyBuffFriendly(buffName: Buff.BuffName.Taunt, stacks: 1, target: RelativeTarget(0))
        );
    }
}

public partial class TauntingGuard : Skill
{
    private const int TauntStacks = 2;
    private const int BaseBlock = 6;

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
                target: RelativeTarget(0)
            ),
            BlockStep(0, BaseBlock, survivabilityMultiplier: 2)
        );
    }
}

public partial class WeakpointBulwark : Skill
{
    private const int BaseBlock = 15;
    private int _capturedVulnerableStacks;

    public override string SkillName { get; set; } = "弱点壁垒";

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
            CustomStep(
                _ =>
                {
                    var target = ChosetargetByOrder(byBehindRow: false).FirstOrDefault();
                    _capturedVulnerableStacks =
                        target == null || target == OwnerCharater?.BattleNode?.dummy
                            ? 0
                            : target
                                .HurtBuffs?.FirstOrDefault(buff =>
                                    buff != null
                                    && buff.ThisBuffName == Buff.BuffName.Vulnerable
                                    && buff.Stack > 0
                                )
                                ?.Stack ?? 0;

                    return Task.CompletedTask;
                },
                _ => new[] { $"令目标的{Buff.BuffName.Vulnerable.GetDescription()}层数翻倍。" }
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: _ => _capturedVulnerableStacks,
                maxTargets: 1
            )
        );
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
                _ => new[] { "令自己的格挡翻倍。" }
            )
        );
    }
}
