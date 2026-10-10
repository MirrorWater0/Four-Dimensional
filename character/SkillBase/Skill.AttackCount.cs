using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class Skill
{
    /// <summary>Change the normal or this-turn-only attack count. Negative amounts reduce it, clamped at zero.</summary>
    protected SkillStep ModifyAttackCountStep(
        int amount,
        bool temporary = false,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false,
        [CallerArgumentExpression(nameof(amount))] string amountExpression = null
    ) => new ModifyAttackCountSkillStep(
        amount, temporary, TargetValue(target), includeSummonsWhenAll, VFromExpression(amountExpression, amount)
    );

    protected SkillStep ModifyAttackCountStep(
        Func<Skill, int> amount,
        bool temporary = false,
        TargetReference target = TargetReference.Self,
        bool includeSummonsWhenAll = false
    ) => new ModifyAttackCountSkillStep(0, temporary, TargetValue(target), includeSummonsWhenAll, amount);

    private sealed class ModifyAttackCountSkillStep : SkillStep
    {
        private readonly Buff.BuffName _buffName;
        private readonly int _stacks;
        private readonly Func<Skill, int> _stacksFunc;
        private readonly TargetSelection _target;
        private readonly bool _temporary;
        private readonly bool _includeSummonsWhenAll;

        public ModifyAttackCountSkillStep(int amount, bool temporary, TargetSelection target,
            bool includeSummonsWhenAll, Func<Skill, int> provider)
        {
            _stacks = amount;
            _stacksFunc = provider;
            _temporary = temporary;
            _buffName = temporary ? Buff.BuffName.TemporaryAttackCount : Buff.BuffName.AttackCount;
            _target = target;
            _includeSummonsWhenAll = includeSummonsWhenAll;
        }

        public override Task Execute(Skill skill)
        {
            int amount = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            foreach (Character target in PreviewTargets(skill))
                AttackCountBuff.Modify(target, amount, _temporary, skill.OwnerCharater);
            return Task.CompletedTask;
        }

        public override IEnumerable<string> Describe(Skill skill)
        {
            int amount = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            string action = amount < 0 ? "减少" : "获得";
            string target = IsSelfFriendlyTarget(_target) ? "" : $"{FriendlyTargetTextForDescription(_target)}";
            string duration = _temporary ? "（仅本回合生效）" : "（本场战斗持续生效）";
            yield return $"{target}{action}{Math.Abs((long)amount)}次{Buff.GetBuffDisplayName(_buffName)}{duration}。";
        }

        public override IEnumerable<Character> PreviewTargets(Skill skill) =>
            skill.ResolveFriendlyTargets(_target, dyingFilter: true, includeSummonsWhenAll: _includeSummonsWhenAll);

        public override IEnumerable<PreviewEffectEntry> PreviewEffects(Skill skill, PreviewDamageContext context)
        {
            int amount = ResolveStepBaseValue(skill, _stacks, _stacksFunc);
            foreach (Character target in PreviewTargets(skill).Where(target => target != null))
            {
                int previous = AttackCountBuff.GetCount(target, _temporary);
                int next = (int)Math.Clamp((long)previous + amount, 0, int.MaxValue);
                if (next != previous)
                    yield return PreviewEffectEntry.Buff(target, _buffName, next - previous, skill.OwnerCharater);
            }
        }
    }

    // Kept outside the card registry: automatic attacks spend no energy and emit no card-use events.
    internal static Task ExecuteTurnEndAttackAsync(Character owner)
    {
        var attack = CreatePlaceholder(SkillTypes.Attack);
        attack.OwnerCharater = owner;
        return attack.Attack(attack.DamageFromPower(0, 1, 9999), playHitEffectForFirstHit: true);
    }

    internal static int PreviewTurnEndAttackDamage(Character owner, Character target,
        AttackBuff.PreviewState state)
    {
        var attack = CreatePlaceholder(SkillTypes.Attack);
        attack.OwnerCharater = owner;
        int damage = Math.Clamp(attack.DamageFromPower(0, 1, 9999), 0, 9999);
        return Math.Clamp(AttackBuff.ApplyOutgoingDamageModifiers(owner, damage, target,
            previewState: state), 0, 9999);
    }
}
