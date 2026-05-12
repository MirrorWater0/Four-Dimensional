using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MariyaSurviveSkill { }

public partial class FinalGuard : Skill
{
    private const int BaseBlock = 9;
    private const int PowerGain = 4;

    public FinalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "终守";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(0, BaseBlock),
            ModifyPropertyStep(
                type: PropertyType.Power,
                value: PowerGain,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class CrystalGuard : Skill
{
    private const int BaseBlock = 2;

    public CrystalGuard()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "水晶守护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(this, BlockStep(TargetReference.All, baseBlock: BaseBlock));
    }
}

public partial class QuietVeil : Skill
{
    private const int InvisibleStacks = 2;
    private const int MaxLifeGain = 10;
    private const int SurvivabilityGain = 6;
    private const int BaseHeal = 5;

    public QuietVeil()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "静影庇护";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            ),
            ModifyPropertyStep(PropertyType.MaxLife, MaxLifeGain),
            ModifyPropertyStep(PropertyType.Survivability, SurvivabilityGain),
            HealStep(baseHeal: BaseHeal, target: TargetReference.Self)
        );
    }
}

public partial class EnergyTransfer : Skill
{
    private const int BaseBlock = 9;
    private const int AllyEnergyGain = 2;

    public EnergyTransfer()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "能量传输";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(relativeIndex: 0, baseBlock: BaseBlock),
            EnergyStep(delta: AllyEnergyGain, target: TargetReference.ManualFriendly)
        );
    }
}

public partial class EnergyRelay : Skill
{
    private int _cachedTransferEnergy;

    public EnergyRelay()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "能量接续";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CustomStep(
                _ =>
                {
                    _cachedTransferEnergy = ResolveTransferEnergyAmount();
                    return Task.CompletedTask;
                },
                _ => Array.Empty<string>()
            ),
            BlockStep(relativeIndex: 0, baseBlock: 5),
            BlockStep(relativeIndex: -1, baseBlock: 5),
            EnergyStep(delta: _ => _cachedTransferEnergy, target: TargetReference.Next),
            EnergyStep(delta: _ => -_cachedTransferEnergy, target: TargetReference.Previous),
            TextStep("将上一位角色的全部能量转移给下一位。")
        );
    }

    private int ResolveTransferEnergyAmount()
    {
        if (OwnerCharater?.BattleNode == null)
            return 0;

        Character previous = GetAllyByRelative(-1, dyingFilter: true);
        Character next = GetAllyByRelative(1, dyingFilter: true);
        if (previous == null || next == null || previous == next)
            return 0;

        return Math.Max(0, previous.Energy);
    }
}

public partial class TouchOfGod : Skill
{
    private const int BaseBlock = 15;
    private const int DivinityStacks = 1;

    public TouchOfGod()
        : base(SkillTypes.Survive)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "上帝之触";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(relativeIndex: 0, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Divinity,
                stacks: DivinityStacks,
                target: TargetReference.Self
            )
        );
    }
}
