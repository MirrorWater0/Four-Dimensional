using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSpecialSkill { }

public partial class VeilStep : Skill
{
    private int InvisibleStacks => V(nameof(InvisibleStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "夜幕潜行";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Invisible,
                stacks: InvisibleStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class TwilightParadox : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int VulnerableStacks => V(nameof(VulnerableStacks));
    private int selfStacks => V(nameof(selfStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "暮光悖论";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Vulnerable,
                stacks: selfStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class NightingaleEnergy : Skill
{
    private int EnergyGain => V(nameof(EnergyGain));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "安息之歌";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            EnergyStep(V("EnergyGain")),
            ModifyPropertyStep(PropertyType.Power, V("PowerGain"))
        );
    }
}

public partial class RequiemBloom : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int PowerGain => V(nameof(PowerGain));
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "安魂花";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            DrawCardsStep(V("DrawCount")),
            DiscardCardsStep(V("DiscardCount")),
            ModifyPropertyStep(PropertyType.Power, PowerGain,TargetReference.All)
        );
    }
}

public partial class LongNight : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override string SkillName { get; set; } = "长夜";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            PlayDrawPileCardStep(SkillTypes.Attack),
            PlayDrawPileCardStep(SkillTypes.Attack)
        );
    }
}

public partial class CurtainCallMoment : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int WeakenStacks => V(nameof(WeakenStacks));
    private int InvisibleStacks => V(nameof(InvisibleStacks));
    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "落幕时刻";

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
            )
        );
    }
}

public partial class SunMoonCycle : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int DrawCount => V(nameof(DrawCount));
    private int CardRefreshStacks => V(nameof(CardRefreshStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "日月轮回";

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

public partial class BrightestMoment : Skill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int SurvivabilityGainPerInvisible => V(nameof(SurvivabilityGainPerInvisible));
    private int _lostInvisibleStacks;

    public override string SkillName { get; set; } = "至亮时刻";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            TextStep(
                I18n.Format(
                    "skill.brightest_moment.text.lose_invisible_gain_survivability",
                    "失去所有隐身层数，每失去1层隐身，全阵获得{amount}点生存。",
                    ("amount", SurvivabilityGainPerInvisible)
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
        OwnerCharater.BattleNode?.RetargetEnemyDamageIntentionsAfterInvisibleEnds(OwnerCharater);
        OwnerCharater.BattleNode?.NotifyHandPreviewContextChanged();
    }
}
