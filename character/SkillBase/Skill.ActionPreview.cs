using System;
using System.Collections.Generic;
using System.Linq;

public partial class Skill {
    // A scoped shadow world: preview never writes character stats, buffs, piles or execution counts.
    internal abstract class ActionPreviewContext {
        public abstract bool IsAlive(Character target);
        public abstract Character[] HostileTargets(Character owner, bool behind, bool taunt, bool invisible);
        public abstract Character FriendlyFallback(Skill skill);
        public abstract int PowerDelta(Character target);
        public abstract int SurvivabilityDelta(Character target);
        public abstract int OutgoingDamage(Character owner, int damage);
        public abstract bool ContinuePlan(Character owner);
        public abstract void Apply(PreviewEffectEntry entry);
        public abstract void MarkApproximate();
        public abstract void TriggerInstalledCards(Character target);
        public abstract void RemoveActionCards(Character target, bool exhaust);
        public abstract void ReturnActionCardsToHand(Character target);
        public abstract void TriggerRightActionCard(Skill skill);
        public abstract int ExecutionCount(Skill skill);
        public abstract void RecordExecution(Skill skill);
        public abstract bool ConsumeInitialExtraExecutions(Skill skill);
        public abstract int ConsumeEchoActionCardExtraExecutions(Skill skill);
    }

    private ActionPreviewContext _actionPreview;

    internal void PreviewInstalledAction(Battle.InstalledActionCard entry, ActionPreviewContext context,
        bool includeInitialExtraExecutions = true) {
        if (ExhaustExecutionLimit > 0 && context.ExecutionCount(this) >= ExhaustExecutionLimit)
            return;
        var previousContext = _actionPreview;
        var previousFriendly = _manualFriendlyTarget;
        var previousEnergy = _actionPaidEnergy;
        bool previousLock = _useLockedHostileTargetsForExecution;
        var targets = new Dictionary<string, Character>(_storedTargets);
        var targetArrays = new Dictionary<string, Character[]>(_storedTargetArrays);
        var randomTargets = new Dictionary<string, Character[]>(_previewableRandomHostileTargets);
        try {
            _actionPreview = context;
            _actionPaidEnergy = entry.PaidEnergy;
            _useLockedHostileTargetsForExecution = false;
            _manualFriendlyTarget = entry.FriendlyTarget;
            if (RequiresManualFriendlyTarget()
                && (!HasManualFriendlyTarget() || !context.IsAlive(_manualFriendlyTarget)))
                _manualFriendlyTarget = context.FriendlyFallback(this);
            var plan = GetPlan();
            if (plan == null)
                return;
            int repeats = 1 + (includeInitialExtraExecutions && context.ConsumeInitialExtraExecutions(this)
                ? _actionInitialExtraExecutions : 0);
            repeats += context.ConsumeEchoActionCardExtraExecutions(this);
            bool executed = false;
            for (int i = 0; i < repeats && context.ContinuePlan(OwnerCharater); i++) {
                _storedTargets.Clear();
                _storedTargetArrays.Clear();
                // Retain existing random preview choices; unknown/random effects are labelled as estimates.
                plan.PreviewActionSteps(context);
                executed = true;
            }
            // Exhaust once after the normal plan and all echoes, matching actual resolution.
            if (executed)
                context.RecordExecution(this);
        }
        finally {
            _actionPreview = previousContext;
            _manualFriendlyTarget = previousFriendly;
            _actionPaidEnergy = previousEnergy;
            _useLockedHostileTargetsForExecution = previousLock;
            _storedTargets.Clear();
            foreach (var pair in targets) _storedTargets[pair.Key] = pair.Value;
            _storedTargetArrays.Clear();
            foreach (var pair in targetArrays) _storedTargetArrays[pair.Key] = pair.Value;
            _previewableRandomHostileTargets.Clear();
            foreach (var pair in randomTargets) _previewableRandomHostileTargets[pair.Key] = pair.Value;
        }
    }

    protected sealed partial class SkillPlan {
        internal void PreviewActionSteps(ActionPreviewContext simulation) {
            var context = new PreviewDamageContext(_skill.OwnerCharater, false, simulation);
            PreviewSteps(_steps, simulation, context);
        }

        private void PreviewSteps(SkillStep[] steps, ActionPreviewContext simulation, PreviewDamageContext context) {
            foreach (var step in steps) {
                if (!simulation.ContinuePlan(_skill.OwnerCharater))
                    break;
                if (step is TriggerInstalledCardsSkillStep trigger) {
                    foreach (var target in trigger.PreviewTargets(_skill)) {
                        simulation.TriggerInstalledCards(target);
                        if (trigger.ReturnToHandAfterTrigger)
                            simulation.ReturnActionCardsToHand(target);
                        if (trigger.DiscardAfterTrigger || trigger.ExhaustAfterTrigger)
                            simulation.RemoveActionCards(target, trigger.ExhaustAfterTrigger);
                    }
                    continue;
                }
                if (step is TriggerRightActionCardSkillStep) {
                    simulation.TriggerRightActionCard(_skill);
                    continue;
                }
                if (step is EnergyTimesWhileSkillStep loop) {
                    if (loop.HasCustomTimes) simulation.MarkApproximate();
                    int count = Math.Min(4096, loop.ActionPreviewLoopCount(_skill));
                    for (int i = 0; i < count && simulation.ContinuePlan(_skill.OwnerCharater); i++)
                        PreviewSteps(loop.ActionPreviewSteps, simulation, context);
                    continue;
                }
                if (step is ConditionSkillStep condition) {
                    simulation.MarkApproximate();
                    PreviewSteps(condition.ActionPreviewSteps, simulation, context);
                    continue;
                }
                if (step is BranchSkillStep branch) {
                    simulation.MarkApproximate();
                    PreviewSteps(branch.ActionPreviewSteps,
                        simulation, context);
                    continue;
                }
                if (step is EnergyTimesGateSkillStep gate) {
                    simulation.MarkApproximate();
                    PreviewSteps(gate.ActionPreviewSteps(_skill), simulation, context);
                    continue;
                }
                // Delegate effects, branches, summons and card-pile changes may affect later plans.
                // Their gameplay callbacks must never be run from UI preview.
                if (step is not (HostileAttackSkillStep or DoubleStrikeSkillStep or BlockFriendlySkillStep
                    or BlockSummonsSkillStep or HealFriendlySkillStep or HealSummonsSkillStep
                    or ApplyBuffHostileSkillStep or ApplyBuffFriendlySkillStep or ApplyBuffSummonsSkillStep
                    or ModifyFriendlyPropertySkillStep or ModifySummonPropertySkillStep
                    or LowerTargetPropertySkillStep or TextSkillStep))
                    simulation.MarkApproximate();
                foreach (var effect in step?.PreviewEffects(_skill, context) ?? Array.Empty<PreviewEffectEntry>()) {
                    if (!simulation.ContinuePlan(_skill.OwnerCharater))
                        break;
                    simulation.Apply(effect);
                }
            }
        }
    }
}
