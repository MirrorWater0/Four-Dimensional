using System;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }

public partial class VeilStep : Skill
{
    private const int InvisibleStacks = 3;
    private const int BaseBlock = 6;
    public override int EnergyCost => 0;

    public override SkillTypes SkillType => SkillTypes.Survive;

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

public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 0;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "闪耀之光";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            ApplyBuffHostile(
                buffName: Buff.BuffName.Vulnerable,
                stacks: VulnerableStacks,
                target: HostileTargetReference.One
            ),
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock)
        );
    }
}

public partial class AfterimageWard : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int AfterimageStacks = 1;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "月落残影";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock),
            BlockStep(target: TargetReference.Next, baseBlock: BaseBlock),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.Afterimage,
                stacks: AfterimageStacks,
                target: TargetReference.All
            )
        );
    }
}

public partial class StarWard : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int ExtraPowerStacks = 1;
    public override int EnergyCost => 2;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "星辉守势";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(baseBlock: BaseBlock, multiplier: V("Multiplier", 1)),
            ApplyBuffFriendly(
                buffName: Buff.BuffName.ExtraPower,
                stacks: ExtraPowerStacks,
                target: TargetReference.ManualFriendly
            )
        );
    }
}

public partial class TwilightParadox : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    private const int BaseBlock = 6;
    private const int VulnerableStacks = 9;
    private const int selfStacks = 3;
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "暮光悖论";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier", 1)),
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

public partial class BladeDance : Skill
{
    private const int BladeCount = 3;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "刀刃之舞";
    public override int EnergyCost => 2;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 4)),
            AddCardsToHandStep(SkillID.Blade, BladeCount, TargetReference.ManualFriendly)
        );
    }
}

public partial class WardGift : Skill
{
    private const int BaseBlock = 4;

    public override SkillTypes SkillType => SkillTypes.Survive;
    public override int EnergyCost => 0;
    public override string SkillName { get; set; } = "护障转赠";

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock, multiplier: V("Multiplier", 1)),
            CustomStep(
                skill =>
                {
                    Character caster = skill?.OwnerCharater;
                    Character ally = skill?.GetManualFriendlyTarget();
                    if (caster?.BattleNode == null || ally == null)
                        return Task.CompletedTask;

                    int selfBlock = caster.Block;
                    if (selfBlock <= 0)
                        return Task.CompletedTask;

                    int previousBlock = ally.Block;
                    ally.UpdataBlock(selfBlock, source: caster);
                    int gainedBlock = Math.Max(0, ally.Block - previousBlock);
                    SpecialBuff.TriggerBeaconBlockShare(ally, gainedBlock, caster);
                    caster.UpdataBlock(-selfBlock, source: caster);

                    return Task.CompletedTask;
                },
                _ => new[] { "将自身所有格挡转移给该角色。" }
            )
        );
    }
}

public partial class LongNight : Skill
{
    public override SkillRarity Rarity => SkillRarity.Uncommon;
    public override string SkillName { get; set; } = "长夜";
    public override int EnergyCost => 2;
    public override bool ExhaustsAfterUse => true;

    public override SkillTypes SkillType => SkillTypes.Survive;

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 4)),
            CarryStep(target: TargetReference.Previous, skillIndex: 3),
            CarryStep(target: TargetReference.Next, skillIndex: 3)
        );
    }
}

public partial class ShadowBladeWard : Skill
{
    private const int BaseBlock = 6;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "隐刃守势";

    protected override SkillPlan BuildPlan()
    {
        string bladeName = Skill.GetSkill(SkillID.Blade)?.SkillName ?? "利刃";
        string invisibleName = Buff.BuffName.Invisible.GetDescription();

        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.Self, baseBlock: BaseBlock, multiplier: V("Multiplier", 1)),
            CustomStep(
                skill =>
                {
                    Character caster = skill?.OwnerCharater;
                    Battle battle = caster?.BattleNode;
                    if (battle == null)
                        return Task.CompletedTask;

                    bool addedAny = false;
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

                        battle.AddPlayerBattleStatusCards(
                            player,
                            SkillID.Blade,
                            1,
                            BattleCardPileTarget.HandCards,
                            caster
                        );
                        addedAny = true;
                    }

                    if (addedAny)
                        battle.NotifyPlayerTeamBattleCardsGeneratedByFriendlySkill();

                    return Task.CompletedTask;
                },
                _ =>
                    new[]
                    {
                        $"每一位拥有{invisibleName}的角色在手牌中获得1张{bladeName}。",
                    }
            )
        );
    }
}