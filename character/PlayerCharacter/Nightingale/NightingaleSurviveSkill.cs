using System;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class FlashOfLight : Skill
{
    private int VulnerableStacks => V(nameof(VulnerableStacks));
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "闪耀之光";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.RandomPreview
            ),
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier"))
        );
    }
}

public partial class AfterimageWard : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private int BaseBlock => V(nameof(BaseBlock));
    private int AfterimageStacks => V(nameof(AfterimageStacks));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "月落残影";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier")),
            BlockStep(target: TargetReference.Next, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Afterimage,
                stacks: AfterimageStacks,
                target: TargetReference.Next
            )
        );
    }
}

public partial class StarWard : AbilitySkill
{
    public override SkillRarity Rarity => SkillRarity.Rare;
    private int ExtraPowerStacks => V(nameof(ExtraPowerStacks));

    public override string SkillName { get; set; } = "星辉守势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraPower,
                stacks: ExtraPowerStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class BladeDance : Skill
{
    private int BladeCount => V(nameof(BladeCount));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "刀刃之舞";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            AddCardsToHandStep(SkillID.Blade, BladeCount)
        );
    }
}

public partial class WardGift : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));
    private int BrokenStacks => V(nameof(BrokenStacks));

    public override SkillTypes SkillType => SkillTypes.Special;

    public override string SkillName { get; set; } = "护障转赠";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Broken,
                stacks: BrokenStacks,
                target: TargetReference.Self
            )
        );
    }
}

public partial class ShadowBladeWard : Skill
{
    private int BaseBlock => V(nameof(BaseBlock));

    public override SkillTypes SkillType => SkillTypes.Permanent;

    public override string SkillName { get; set; } = "隐刃守势";

    protected override SkillPlan BuildPlan()
    {
        string powerName = "力量";
        string invisibleName = Buff.BuffName.Invisible.GetDescription();

        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier")),
            CustomStep(
                async skill =>
                {
                    Character caster = skill?.OwnerCharater;
                    Battle battle = caster?.BattleNode;
                    if (battle == null)
                        return;

                    foreach (
                        Character ally in battle.GetOrderedTeamCharacters(
                            isPlayer: true,
                            includeSummons: false,
                            dyingFilter: true
                        )
                    )
                    {
                        if (
                            ally is not PlayerCharacter player
                            || !player.HasActiveStartActionBuff(Buff.BuffName.Invisible)
                        )
                            continue;

                        await player.IncreaseProperties(PropertyType.Power, V("InvisiblePowerGain"), caster);
                    }

                    return;
                },
                _ =>
                    new[]
                    {
                        $"每一位拥有{invisibleName}的角色获得{V("InvisiblePowerGain")}点{powerName}。",
                    }
            )
        );
    }
}
