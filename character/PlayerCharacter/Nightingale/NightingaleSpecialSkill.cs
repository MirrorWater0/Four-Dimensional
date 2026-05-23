using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSpecialSkill { }

public partial class NightingaleEnergy : Skill
{
    private const int EnergyGain = 1;

    public NightingaleEnergy()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "安息之歌";
    public override int EnergyCost => 0;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(EnergyGain),
            ModifyPropertyStep(PropertyType.Power, 1)
        );
    }
}

public partial class TempoSurge : Skill
{
    public override bool ExhaustsAfterUse => base.ExhaustsAfterUse;

    public TempoSurge()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "疾奏";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Survivability, 3, TargetReference.All)
        );
    }
}

public partial class LongNight : Skill
{
    public override string SkillName { get; set; } = "长夜";
    public override int EnergyCost => 3;
    public override bool ExhaustsAfterUse => true;

    public LongNight()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            CarryStep(target: TargetReference.Previous, skillIndex: 3),
            CarryStep(target: TargetReference.Next, skillIndex: 3)
        );
    }
}

public partial class RequiemBloom : Skill
{
    private const int PowerGain = 3;
    public override bool ExhaustsAfterUse => true;

    private const int ExtraTurnStacks = 1;

    public RequiemBloom()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "安魂花";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ModifyPropertyStep(PropertyType.Power, PowerGain),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraTurn,
                stacks: ExtraTurnStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class CurtainCallMoment : Skill
{
    private const int WeakenStacks = 2;
    private const int InvisibleStacks = 2;
    private const int ExtraTurnStacks = 1;
    public override bool ExhaustsAfterUse => true;

    public CurtainCallMoment()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "落幕时刻";
    public override int EnergyCost => 3;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.All
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraTurn,
                stacks: ExtraTurnStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class SunMoonCycle : Skill
{
    private const int DrawCount = 2;
    private const int CardRefreshStacks = 2;
    public override bool ExhaustsAfterUse => true;

    public SunMoonCycle()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "日月轮回";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(DrawCount),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: CardRefreshStacks,
                target: TargetReference.All
            )
        );
    }
}

public partial class Swift : Skill
{
    private const int SwiftStacks = 1;

    public Swift()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "迅捷";
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Swift,
                stacks: SwiftStacks,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Swift,
                stacks: SwiftStacks,
                target: TargetReference.Previous
            )
        );
    }
}

public partial class ShadowForm : Skill
{
    private const int ShadowStacks = 2;

    public ShadowForm()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "暗影形态";
    public override int EnergyCost => 4;
    public override bool ExhaustsAfterUse => true;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: 5,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Shadow,
                stacks: ShadowStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class BrightestMoment : Skill
{
    private const int SurvivabilityGainPerInvisible = 2;
    private int _lostInvisibleStacks;
    public override bool ExhaustsAfterUse => true;

    public BrightestMoment()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "至亮时刻";
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep(
                I18n.Tr(
                    "skill.brightest_moment.text.lose_invisible_gain_survivability",
                    "失去所有隐身层数,每失去1层隐身,全阵获得2点生存."
                )
            ),
            CustomStep(
                _ =>
                {
                    _lostInvisibleStacks = GetInvisibleStacks();
                    return Task.CompletedTask;
                },
                _ => Array.Empty<string>()
            ),
            ModifyPropertyStep(
                PropertyType.Survivability,
                skill =>
                    ((BrightestMoment)skill)._lostInvisibleStacks * SurvivabilityGainPerInvisible,
                TargetReference.All
            ),
            CustomStep(
                _ =>
                {
                    RemoveAllInvisibleStacks();
                    _lostInvisibleStacks = 0;
                    return Task.CompletedTask;
                },
                _ => Array.Empty<string>()
            )
        );
    }

    private int GetInvisibleStacks()
    {
        var invisible = OwnerCharater?.StartActionBuffs?.FirstOrDefault(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Invisible && buff.Stack > 0
        );
        return invisible?.Stack ?? 0;
    }

    private void RemoveAllInvisibleStacks()
    {
        var invisible = OwnerCharater?.StartActionBuffs?.FirstOrDefault(buff =>
            buff != null && buff.ThisBuffName == Buff.BuffName.Invisible && buff.Stack > 0
        );
        if (invisible == null)
            return;

        invisible.Stack = 0;
        if (invisible.BuffIcon != null && GodotObject.IsInstanceValid(invisible.BuffIcon))
            invisible.BuffIcon.QueueFree();

        OwnerCharater.StartActionBuffs.Remove(invisible);
        OwnerCharater.InvalidateBuffTooltipCache();
        OwnerCharater.BattleNode?.RefreshEnemyIntentionPreviews();
    }
}

public partial class EternalDarkSkill : Skill
{
    private const int EternalDarkStacks = 2;

    public EternalDarkSkill()
        : base(SkillTypes.Special)
    {
        UpdateDescription();
    }

    public override string SkillName { get; set; } = "永暗";
    public override bool ExhaustsAfterUse => true;
    public override int EnergyCost => 1;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: 1,
                target: TargetReference.Self
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.EternalDark,
                stacks: EternalDarkStacks,
                target: TargetReference.Self
            )
        );
    }
}
