using System;
using System.Threading.Tasks;
using Godot;

public partial class EchoDefenceSkill { }

public partial class SoundBarrier : Skill
{
    public override string SkillName { get; set; } = "音墙";

    public override SkillTypes SkillType => SkillTypes.Special;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddCardsToHandStep(
                SkillID.DefenceFocus,
                V("DefenceFocusCount", 3),
                TargetReference.ManualFriendly
            )
        );
    }
}

public partial class RelayShift : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override int EnergyCost => Cost(2);

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 7)),
            CarryStep(target: TargetReference.Previous, skillIndex: 2),
            CarryStep(target: TargetReference.Next, skillIndex: 2)
        );
    }
}



public partial class DeflectionShield : Skill
{
    private const int BaseBlock = 7;
    private const int DamageImmuneStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "偏折之盾";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier", 1)
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DamageImmune,
                stacks: DamageImmuneStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class ResonantWard : Skill
{
    private const int DebuffImmunityStacks = 2;
    private const int BaseBlock = 7;
    public override int EnergyCost => Cost(2);
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "电磁排斥";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class DissonantField : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 7;
    private const int WeakenStacks = 2;
    public override int EnergyCost => Cost(2);

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "失谐力场";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                baseBlock: BaseBlock,
                multiplier: V("Multiplier", 1),
                target: TargetReference.ManualFriendly
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.All
            )
        );
    }
}

public partial class Shelter : Skill
{
    private const int BaseBlock = 5;
    private const int CardRefreshStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "护幕";
    public override int EnergyCost => Cost(1);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.ManualFriendly,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier", 1)
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: CardRefreshStacks,
                target: TargetReference.Others
            )
        );
    }
}

public partial class SoundPickup : Skill
{
    private const int BaseBlock = 7;
    private const int DrawPilePickCount = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "拾音";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier", 1)
            ),
            SelectDrawPileCardsToHandStep(DrawPilePickCount)
        );
    }
}

public partial class LingeringTone : Skill
{
    private const int BaseBlock = 7;
    private const int HandKeywordCount = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "留音";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.ManualFriendly,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier", 1)
            ),
            SelectCardsAddKeywordStep(
                BattleCardKeyword.Retain,
                HandKeywordCount,
                BattleCardPileTarget.HandCards
            )
        );
    }
}
