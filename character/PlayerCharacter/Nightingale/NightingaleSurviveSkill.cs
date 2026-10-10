using System;
using System.Threading.Tasks;
using Godot;

public partial class NightingaleSurviveSkill { }



public partial class FlashOfLight : Skill
{
    private const int VulnerableStacks = 2;
    private const int BaseBlock = 5;

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
    private const int BaseBlock = 7;
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
    private const int BaseBlock = 7;
    private const int ExtraPowerStacks = 1;
    public override int EnergyCost => Cost(2);

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

public partial class BladeDance : Skill
{
    private const int BladeCount = 3;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "刀刃之舞";
    public override int EnergyCost => Cost(2);

    protected override SkillPlan BuildPlan()
    {
        return new SkillPlan(
            this,
            BlockStep(V("BaseBlock", 5)),
            ModifyAttackCountStep(BladeCount, temporary: true, target: TargetReference.ManualFriendly)
        );
    }
}

public partial class WardGift : Skill
{
    private const int BaseBlock = 5;

    public override SkillTypes SkillType => SkillTypes.Survive;
    public override int EnergyCost => Cost(0);
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

                    ally.UpdataBlock(selfBlock, source: caster);
                    caster.UpdataBlock(-selfBlock, source: caster);

                    return Task.CompletedTask;
                },
                _ => new[] { "将自身所有格挡转移给该角色。" }
            )
        );
    }
}



public partial class ShadowBladeWard : Skill
{
    private const int BaseBlock = 7;

    public override SkillTypes SkillType => SkillTypes.Survive;

    public override string SkillName { get; set; } = "隐刃守势";
    public override int EnergyCost => Cost(0);
    protected override SkillPlan BuildPlan()
    {
        string attackName = Buff.GetBuffDisplayName(Buff.BuffName.TemporaryAttackCount);
        string invisibleName = Buff.BuffName.Invisible.GetDescription();

        return new SkillPlan(
            this,
            BlockStep(target: TargetReference.ManualFriendly, baseBlock: BaseBlock, multiplier: V("Multiplier", 1)),
            CustomStep(
                skill =>
                {
                    Character caster = skill?.OwnerCharater;
                    Battle battle = caster?.BattleNode;
                    if (battle == null)
                        return Task.CompletedTask;

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

                        AttackCountBuff.Modify(player, 1, temporary: true, source: caster);
                    }

                    return Task.CompletedTask;
                },
                _ =>
                    new[]
                    {
                        $"每一位拥有{invisibleName}的角色获得1次{attackName}（仅本回合生效）。",
                    }
            )
        );
    }
}
