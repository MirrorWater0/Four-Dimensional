using System;
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

public partial class ReadyStance : Skill
{
    private const int BaseBlock = -3;
    private const int SurvivabilityMultiplier = 1;
    private const int EnergyGain = 5;

    public override string SkillName { get; set; } = "\u80fd\u91cf\u7206\u53d1";
    public override int EnergyCost => 3;

    public ReadyStance()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                relativeIndex: 0,
                baseBlock: BaseBlock,
                survivabilityMultiplier: SurvivabilityMultiplier
            ),
            EnergyStep(EnergyGain)
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
    private int _capturedVulnerableStacks;

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
                target: HostileTargets(1)
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
                _ => new[] { "格挡翻倍。" }
            )
        );
    }
}
