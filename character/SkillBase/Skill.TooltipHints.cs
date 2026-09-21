using System;
using System.Collections.Generic;
using System.Reflection;

public partial class Skill
{
    public enum SkillTooltipKeyword
    {
        Carry,
        Voidness,
        Rebirth,
    }

    public sealed class SkillTooltipHints
    {
        private readonly List<SkillID> _relatedSkillIds = new();
        private readonly List<Buff.BuffName> _buffs = new();
        private readonly HashSet<SkillTooltipKeyword> _keywords = new();
        private readonly HashSet<StatX> _statVariables = new();

        public IReadOnlyList<SkillID> RelatedSkillIds => _relatedSkillIds;
        public IReadOnlyList<Buff.BuffName> Buffs => _buffs;
        public IReadOnlyCollection<SkillTooltipKeyword> Keywords => _keywords;
        public IReadOnlyCollection<StatX> StatVariables => _statVariables;

        public void AddRelatedSkillId(SkillID skillId)
        {
            if (_relatedSkillIds.Contains(skillId))
                return;

            _relatedSkillIds.Add(skillId);
        }

        public void AddBuff(Buff.BuffName buffName)
        {
            if (_buffs.Contains(buffName))
                return;

            _buffs.Add(buffName);
        }

        public void AddKeyword(SkillTooltipKeyword keyword) => _keywords.Add(keyword);

        public void AddStatVariable(StatX stat) => _statVariables.Add(stat);
    }

    public SkillTooltipHints CollectTooltipHints()
    {
        var hints = new SkillTooltipHints();
        if (ResolvesExhaustsAtTurnEndInHand)
            hints.AddKeyword(SkillTooltipKeyword.Voidness);

        GetPlan()?.CollectTooltipHints(this, hints);

        foreach (SkillID skillId in hints.RelatedSkillIds)
        {
            Skill related = GetSkill(skillId);
            if (related?.ResolvesExhaustsAtTurnEndInHand == true)
                hints.AddKeyword(SkillTooltipKeyword.Voidness);
        }

        return hints;
    }

    public IReadOnlyList<SkillID> GetRelatedPreviewSkillIds() =>
        CollectTooltipHints().RelatedSkillIds;

    protected sealed partial class SkillPlan
    {
        internal void CollectTooltipHints(Skill skill, SkillTooltipHints hints)
        {
            foreach (SkillStep step in _steps)
                CollectTooltipHintsFromStep(skill, step, hints);
        }
    }

    private static void CollectTooltipHintsFromStep(
        Skill skill,
        SkillStep step,
        SkillTooltipHints hints
    )
    {
        if (step == null)
            return;

        switch (step)
        {
            case AddCardsSkillStep addCardsStep:
                CollectAddCardsHints(addCardsStep, hints);
                break;
            case TransformCardsSkillStep transformCardsStep:
                CollectTransformCardsHints(skill, transformCardsStep, hints);
                break;
            case ApplyBuffHostileSkillStep:
            case ApplyBuffFriendlySkillStep:
            case ApplyBuffSummonsSkillStep:
                CollectBuffStepHints(skill, step, hints);
                break;
            case CarrySkillStepImpl:
                hints.AddKeyword(SkillTooltipKeyword.Carry);
                break;
            case HealFriendlySkillStep:
                if (GetStepField<bool>(step, "_rebirth"))
                    hints.AddKeyword(SkillTooltipKeyword.Rebirth);
                break;
            case HostileAttackSkillStep:
                if (
                    GetStepField<int>(step, "_powerMultiplier") > 0
                    || GetStepField<Func<Skill, int>>(step, "_baseDamageFunc") != null
                )
                    hints.AddStatVariable(StatX.Power);
                break;
            case BlockFriendlySkillStep:
                if (
                    GetStepField<int>(step, "_survivabilityMultiplier") > 0
                    || GetStepField<Func<Skill, int>>(step, "_baseBlockProvider") != null
                )
                    hints.AddStatVariable(StatX.Survivability);
                break;
            case ModifyFriendlyPropertySkillStep:
                CollectModifyPropertyHints(step, hints);
                break;
            case EnergySkillStep:
                if (GetStepField<Func<Skill, int>>(step, "_deltaProvider") != null)
                    hints.AddStatVariable(StatX.Energy);
                break;
            case EnergyTimesGateSkillStep:
            case EnergyTimesWhileSkillStep:
                hints.AddStatVariable(StatX.Energy);
                break;
            case ConditionSkillStep:
                foreach (
                    SkillStep nestedStep in GetStepField<SkillStep[]>(step, "_onPassSteps")
                        ?? Array.Empty<SkillStep>()
                )
                    CollectTooltipHintsFromStep(skill, nestedStep, hints);
                break;
            case BranchSkillStep:
                foreach (
                    SkillStep nestedStep in GetStepField<SkillStep[]>(step, "_onPassSteps")
                        ?? Array.Empty<SkillStep>()
                )
                    CollectTooltipHintsFromStep(skill, nestedStep, hints);
                foreach (
                    SkillStep nestedStep in GetStepField<SkillStep[]>(step, "_onFailSteps")
                        ?? Array.Empty<SkillStep>()
                )
                    CollectTooltipHintsFromStep(skill, nestedStep, hints);
                break;
        }
    }

    private static void CollectAddCardsHints(AddCardsSkillStep step, SkillTooltipHints hints)
    {
        if (GetStepField<int>(step, "_count") <= 0)
            return;

        if (GetStepField<bool>(step, "_random"))
            return;

        SkillID skillId = GetStepField<SkillID>(step, "_statusSkillId");
        if (skillId == SkillID.None)
            return;

        hints.AddRelatedSkillId(skillId);
    }

    private static void CollectTransformCardsHints(
        Skill skill,
        TransformCardsSkillStep step,
        SkillTooltipHints hints
    )
    {
        int count = ResolveStepBaseValue(
            skill,
            GetStepField<int>(step, "_count"),
            GetStepField<Func<Skill, int>>(step, "_countProvider")
        );
        if (count <= 0)
            return;

        SkillID skillId = GetStepField<SkillID>(step, "_replacementSkillId");
        if (skillId != SkillID.None)
            hints.AddRelatedSkillId(skillId);
    }

    private static void CollectBuffStepHints(Skill skill, SkillStep step, SkillTooltipHints hints)
    {
        Buff.BuffName buffName = GetStepField<Buff.BuffName>(step, "_buffName");
        int stacks = ResolveStepStacksForTooltip(skill, step);
        if (stacks == 0)
            return;

        hints.AddBuff(buffName);
    }

    private static int ResolveStepStacksForTooltip(Skill skill, SkillStep step)
    {
        int stacks = GetStepField<int>(step, "_stacks");
        Func<Skill, int> stacksFunc = GetStepField<Func<Skill, int>>(step, "_stacksFunc");
        return ResolveStepBaseValue(skill, stacks, stacksFunc);
    }

    private static void CollectModifyPropertyHints(SkillStep step, SkillTooltipHints hints)
    {
        if (GetStepField<Func<Skill, int>>(step, "_valueProvider") == null)
            return;

        switch (GetStepField<PropertyType>(step, "_type"))
        {
            case PropertyType.Power:
                hints.AddStatVariable(StatX.Power);
                break;
            case PropertyType.Survivability:
                hints.AddStatVariable(StatX.Survivability);
                break;
        }
    }

    private static T GetStepField<T>(SkillStep step, string fieldName)
    {
        if (step == null)
            return default;

        FieldInfo field = step.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            return default;

        object value = field.GetValue(step);
        return value is T typed ? typed : default;
    }
}
