using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class Skill {
    protected SkillStep UninstallAttackTargetActionCardsStep() => new UninstallAttackTargetActionCardsSkillStep();

    private sealed class UninstallAttackTargetActionCardsSkillStep : SkillStep {
        public override Task Execute(Skill skill) {
            // Use the actual attack targets, including a target killed by the preceding damage.
            foreach (var target in skill?.GetAttackTargets() ?? Array.Empty<Character>()) {
                if (target is PlayerCharacter player && GodotObject.IsInstanceValid(player))
                    skill.OwnerCharater?.BattleNode?.UninstallAllActionCards(player);
            }
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill) {
            yield return I18n.Tr("skill.step.uninstall_target_action_cards", "卸下目标的所有行动牌（放入弃牌堆）。");
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill) =>
            (skill?.GetAttackTargets() ?? Array.Empty<Character>())
                .Where(target => target is PlayerCharacter && GodotObject.IsInstanceValid(target));

        public override IEnumerable<PreviewEffectEntry> PreviewEffects(Skill skill, PreviewDamageContext context) {
            foreach (var target in PreviewTargets(skill))
                yield return PreviewEffectEntry.Message(target,
                    I18n.Tr("skill.step.uninstall_target_action_cards", "卸下目标的所有行动牌（放入弃牌堆）。"),
                    skill.OwnerCharater);
        }
    }

    protected SkillStep TriggerRightActionCardStep() => new TriggerRightActionCardSkillStep();

    private sealed class TriggerRightActionCardSkillStep : SkillStep {
        public override Task Execute(Skill skill) =>
            skill?.OwnerCharater?.BattleNode?.TriggerRightActionCardAsync(skill) ?? Task.CompletedTask;

        public override IEnumerable<string> Describe(Skill skill) {
            yield return I18n.Tr("skill.step.trigger_right_action_card", "触发右边的行动牌效果一次。");
        }
    }

    /// <summary>Player permanent cards without Immediate install a persistent plan. Enemy intentions remain immediate.</summary>
    public virtual bool IsActionCard => !HasImmediateKeyword && SkillType == SkillTypes.Permanent
        && (OwnerCharater != null ? OwnerCharater is PlayerCharacter : IsPlayerCatalogCard());
    private int? _actionPaidEnergy;
    private int _actionInitialExtraExecutions;

    private bool IsPlayerCatalogCard() {
        if (SkillId is not SkillID id)
            return false;
        if (id is SkillID.BasicAttack or SkillID.BasicDefense or SkillID.BasicGuard or SkillID.DeSurviveSkill)
            return true;
        FieldInfo field = typeof(SkillID).GetField(id.ToString());
        return field?.GetCustomAttribute<PlayerSkillAttribute>() != null;
    }

    internal void PrepareActionCardPreviewTargets(Character friendlyTarget, int? paidEnergy,
        bool refreshRandomTargets = false) {
        if (!IsActionCard || OwnerCharater == null || !GodotObject.IsInstanceValid(OwnerCharater)
            || OwnerCharater.State == Character.CharacterState.Dying || ResolvesExhaustsAfterUse)
            return;
        var previousFriendly = _manualFriendlyTarget;
        var previousEnergy = _actionPaidEnergy;
        try {
            _actionPaidEnergy = paidEnergy;
            if (paidEnergy.HasValue)
                PrepareAutomaticFriendlyTarget(friendlyTarget);
            if (refreshRandomTargets)
                ClearPreviewableRandomHostileTargets();
            // Draw during gameplay preparation, so hover queries never advance the target RNG.
            LockPreviewTargetsForExecution();
        }
        finally {
            _manualFriendlyTarget = previousFriendly;
            _actionPaidEnergy = previousEnergy;
            // Keep the random choices, but allow dead targets to be replaced during resolution.
            ClearLockedExecutionTargets();
        }
    }

    internal Task ExecuteInstalledActionAsync(Character friendlyTarget, int paidEnergy,
        bool includeInitialExtraExecutions = true) => OwnerCharater?.BattleNode is Battle battle
        ? battle.ExecuteBattleEffectAsync(() => ExecuteInstalledActionCoreAsync(friendlyTarget,
            paidEnergy, includeInitialExtraExecutions), $"{SkillName}（持续行动）", consumeFrameBudget: false)
        : ExecuteInstalledActionCoreAsync(friendlyTarget, paidEnergy, includeInitialExtraExecutions);

    private async Task ExecuteInstalledActionCoreAsync(Character friendlyTarget, int paidEnergy,
        bool includeInitialExtraExecutions) {
        Character owner = OwnerCharater;
        if (owner == null || !GodotObject.IsInstanceValid(owner) || owner.State == Character.CharacterState.Dying
            || ResolvesExhaustsAfterUse)
            return;
        using var source = owner.BattleNode?.PushEffectSource(owner, $"{SkillName}（持续行动）");
        _actionPaidEnergy = paidEnergy;
        ClearLockedExecutionTargets();
        PrepareAutomaticFriendlyTarget(friendlyTarget);
        try {
            var stun = owner.SkillBuffs?.FirstOrDefault(buff => buff != null
                && buff.ThisBuffName == Buff.BuffName.Stun && buff.Stack > 0);
            if (stun != null) {
                await stun.Trigger(this);
                return;
            }
            // Execute the effect plan, never the card-play lifecycle: no energy payment, on-play buffs, or installation.
            SkillPlan plan = GetPlan();
            if (plan != null) {
                int extraExecutions = includeInitialExtraExecutions ? _actionInitialExtraExecutions : 0;
                if (includeInitialExtraExecutions)
                    _actionInitialExtraExecutions = 0;
                foreach (var buff in owner.SkillBuffs?.OfType<SkillBuff>() ?? Enumerable.Empty<SkillBuff>()) {
                    if (buff.TryTriggerEchoForActionCard(this))
                        extraExecutions++;
                }

                await plan.Execute();
                try {
                    for (int i = 0; i < extraExecutions; i++) {
                        if (owner.State == Character.CharacterState.Dying
                            || owner.BattleNode?.ShouldAbortSkillResolution() == true || owner.BattleNode?.HasBattleEnded() == true)
                            break;
                        await plan.Execute();
                    }
                }
                finally {
                    // A normal trigger consumes one use; its echoes finish before exhaustion and are free.
                    owner.BattleNode?.RecordBattleCardExecution(this);
                }
            }
        }
        finally {
            _actionPaidEnergy = null;
            ClearLockedExecutionTargets();
            ClearManualFriendlyTarget();
        }
    }

    private void PrepareAutomaticFriendlyTarget(Character friendlyTarget) {
        SetManualFriendlyTarget(friendlyTarget);
        if (!RequiresManualFriendlyTarget() || HasManualFriendlyTarget())
            return;
        Character owner = OwnerCharater;
        Character fallback = owner?.BattleNode?.GetTeamCharacters(true, includeSummons: true)
            .Where(target => target != null && GodotObject.IsInstanceValid(target)
                && (!ManualFriendlyTargetExcludesSelf() || target != owner)
                && (ManualFriendlyTargetAllowsDying() || target.State != Character.CharacterState.Dying))
            .OrderBy(target => (double)target.Life / Math.Max(1, target.BattleMaxLife))
            .ThenBy(target => target.PositionIndex).FirstOrDefault();
        SetManualFriendlyTarget(fallback);
    }
}
