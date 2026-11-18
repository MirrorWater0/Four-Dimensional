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
                V("DefenceFocusCount"),
                TargetReference.ManualFriendly
            )
        );
    }
}

public partial class RelayShift : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "后撤步";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock"), multiplier: V("Multiplier")),
            TriggerInstalledCardsStep(
                target: TargetReference.Next
            )
        );
    }
}

public partial class DeflectionShield : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int DamageImmuneStacks => V(nameof(DamageImmuneStacks));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "偏折之盾";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier")
            ),
            ConditionStep(
                IsEvenTurn,
                "当前为偶数回合",
                ApplyBuffFriendly(
                    buffName: Buff.BuffName.DamageImmune,
                    stacks: DamageImmuneStacks,
                    target: TargetReference.Next
                )
            )
        );
    }

    private bool IsEvenTurn()
    {
        int turnCount = OwnerCharater?.BattleNode?.ElapsedTurnCount ?? 0;
        return turnCount > 0 && turnCount % 2 == 0;
    }
}

public partial class ResonantWard : Skill
{
    private int DebuffImmunityStacks => V(nameof(DebuffImmunityStacks));
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "电磁排斥";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.DebuffImmunity,
                stacks: DebuffImmunityStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class DissonantField : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseBlock => V(nameof(BaseBlock));
    private int WeakenStacks => V(nameof(WeakenStacks));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "失谐力场";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                baseBlock: BaseBlock,
                multiplier: V("Multiplier"),
                target: TargetReference.Self
            ),
            ApplyBuffHostile(
                buffName: Buff.BuffName.Weaken,
                stacks: WeakenStacks,
                target: HostileTargetReference.RandomPreview
            )
        );
    }
}

public partial class Shelter : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int CardRefreshStacks => V(nameof(CardRefreshStacks));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "护幕";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier")
            ),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraDraw,
                stacks: CardRefreshStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class SoundPickup : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "拾音";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier")
            ),
            TriggerRightActionCardStep()
        );
    }
}

public partial class LingeringTone : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int HandKeywordCount => V(nameof(HandKeywordCount));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "留音";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(
                target: TargetReference.Self,
                baseBlock: BaseBlock,
                multiplier: V("Multiplier")
            ),
            SelectCardsAddKeywordStep(
                BattleCardKeyword.Retain,
                HandKeywordCount,
                BattleCardPileTarget.HandCards
            )
        );
    }
}
