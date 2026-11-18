using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

public partial class Skill {
    private Character _manualHostileTarget;

    public bool RequiresManualHostileTarget() => GetPlan()?.RequiresManualHostileTarget(
        OwnerCharater?.IsPlayer == true) == true;
    public bool RequiresManualTargetSelection() => RequiresManualHostileTarget() || RequiresManualFriendlyTarget();
    public Character[] GetManualHostileTargetCandidates() => OwnerCharater?.BattleNode == null
        ? Array.Empty<Character>()
        : OwnerCharater.BattleNode.GetTeamCharacters(!OwnerCharater.IsPlayer, includeSummons: true)
            .Where(target => target != null && GodotObject.IsInstanceValid(target)
                && target.Life > 0 && IsSelectableHostileTarget(OwnerCharater, target))
            .ToArray();
    public Character GetManualHostileTarget() => _manualHostileTarget != null
        && GodotObject.IsInstanceValid(_manualHostileTarget)
        && GetManualHostileTargetCandidates().Contains(_manualHostileTarget) ? _manualHostileTarget : null;
    public Character GetManualTargetSelection() => RequiresManualHostileTarget()
        ? GetManualHostileTarget() : GetManualFriendlyTarget();
    public bool HasManualTargetSelection() => GetManualTargetSelection() != null;
    public void SetManualTargetSelection(Character target) {
        if (RequiresManualHostileTarget())
            _manualHostileTarget = GetManualHostileTargetCandidates().Contains(target) ? target : null;
        else
            SetManualFriendlyTarget(target);
    }
    public void ClearManualTargetSelection() {
        _manualHostileTarget = null;
        ClearManualFriendlyTarget();
    }

    private static bool IsManualHostileTargetSelection(HostileTargetSelection target, bool includePlayerDefaults) =>
        target.Kind == HostileTargetSelection._Kind.Manual
        || includePlayerDefaults && (target.Kind == HostileTargetSelection._Kind.DefaultRule
            || target.Kind == HostileTargetSelection._Kind.Ordered
                && target.MaxTargets == 1 && !target.ByBehindRow);

    protected sealed partial class SkillPlan {
        public bool RequiresManualHostileTarget(bool includeDefaultRule) =>
            _steps.Any(step => StepRequiresManualHostileTarget(step, includeDefaultRule));
    }

    private static bool StepRequiresManualHostileTarget(SkillStep step, bool includeDefaultRule) {
        if (step == null) return false;
        foreach (var field in step.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
            object value = field.GetValue(step);
            if (value is HostileTargetSelection target
                && IsManualHostileTargetSelection(target, includeDefaultRule))
                return true;
            if (value is SkillStep nested && StepRequiresManualHostileTarget(nested, includeDefaultRule))
                return true;
            if (value is IEnumerable<SkillStep> steps
                && steps.Any(nestedStep => StepRequiresManualHostileTarget(nestedStep, includeDefaultRule)))
                return true;
        }
        return false;
    }
}
