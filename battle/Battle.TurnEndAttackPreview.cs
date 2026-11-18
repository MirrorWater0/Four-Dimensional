using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Battle
{
    private readonly Dictionary<ulong, Character> _actionOutcomePreviewTargets = new();
    private double _turnEndAttackPreviewElapsed;

    internal sealed class TurnEndAttackPreview
    {
        public Character Target;
        public int Damage;
        public int Blocked;
        public int Hits;
        public int BlockGained;
        public bool Approximate;
        public bool Truncated;
    }

    private sealed class ActionPreviewSimulation : Skill.ActionPreviewContext {
        private sealed class State {
            public int Life, Block, Power, Survivability;
            public readonly Dictionary<Buff.BuffName, int> Buffs = new();
            public readonly List<Buff.BuffName> HurtOrder = new();
            public State(Character character) {
                Life = character.Life;
                Block = character.Block;
                foreach (Buff buff in character.StartActionBuffs.Cast<Buff>()
                    .Concat(character.EndActionBuffs).Concat(character.AttackBuffs)
                    .Concat(character.HurtBuffs).Concat(character.SpecialBuffs)
                    .Concat(character.SkillBuffs).Concat(character.DyingBuffs)) {
                    Buffs[buff.ThisBuffName] = Get(buff.ThisBuffName) + buff.Stack;
                }
                HurtOrder.AddRange(character.HurtBuffs.Select(buff => buff.ThisBuffName).Distinct());
            }
            public int Get(Buff.BuffName name) => Buffs.GetValueOrDefault(name);
        }

        private readonly Character[] _characters;
        private readonly Dictionary<Character, State> _states;
        private readonly Dictionary<Character, TurnEndAttackPreview> _totals = new();
        private readonly Battle _battle;
        private readonly HashSet<Skill> _activeCards = new();
        private readonly HashSet<Skill> _additionallyTriggeredCards = new();
        private readonly HashSet<Skill> _initialExtraExecutionsConsumed = new();
        private readonly Dictionary<Skill, int> _executionCounts = new();
        private readonly Dictionary<SkillBuff, int> _echoExtraExecutions = new();
        private readonly Dictionary<Character, int> _talentExhaustRemainders;
        private readonly HashSet<Character> _talentFirstPowerShares;
        private readonly HashSet<Skill> _exhaustedCards = new();
        private readonly HashSet<Skill> _removedActionCards = new();
        private int _additionalActionDepth;
        private bool _approximate;
        private int _work;
        public ActionPreviewSimulation(Battle battle) {
            _battle = battle;
            _talentExhaustRemainders = new Dictionary<Character, int>(battle._talentExhaustRemainders);
            _talentFirstPowerShares = new HashSet<Character>(battle._talentFirstPowerShares);
            _characters = battle.GetOrderedTeamCharacters(false, includeSummons: true)
                .Concat(battle.GetOrderedTeamCharacters(true, includeSummons: true))
                .Where(character => character != null && GodotObject.IsInstanceValid(character)
                    && character.State == Character.CharacterState.Normal && character.Life > 0)
                .Distinct().ToArray();
            _states = _characters.ToDictionary(character => character, character => new State(character));
            _approximate = battle.DyingEmitList.Count > 0 || _characters.Any(character =>
                character.AttackBuffs.Any(buff => buff.Stack > 0 && buff.ThisBuffName == Buff.BuffName.CursePower)
                || character.EndActionBuffs.Any(buff => buff.Stack > 0 && buff.ThisBuffName == Buff.BuffName.Sanctuary));
        }
        public override bool IsAlive(Character target) => target != null
            && _states.TryGetValue(target, out var state) && state.Life > 0;
        public override bool ContinuePlan(Character owner) => IsAlive(owner) && _work < 4096;
        public override void MarkApproximate() => _approximate = true;
        public override int ExecutionCount(Skill skill) => _executionCounts.GetValueOrDefault(skill)
            + _battle.GetBattleCardExecutionCount(skill);
        public override void RecordExecution(Skill skill) =>
            _executionCounts[skill] = _executionCounts.GetValueOrDefault(skill) + 1;
        public override bool ConsumeInitialExtraExecutions(Skill skill) => _initialExtraExecutionsConsumed.Add(skill);
        public override int ConsumeEchoActionCardExtraExecutions(Skill skill) {
            int extraExecutions = 0;
            foreach (SkillBuff buff in skill.OwnerCharater.SkillBuffs.Where(buff => buff.ThisBuffName == Buff.BuffName.Echo)) {
                int consumed = _echoExtraExecutions.GetValueOrDefault(buff);
                if (!buff.CanTriggerForCardPreview(skill, consumed))
                    continue;
                _echoExtraExecutions[buff] = consumed + 1;
                extraExecutions++;
            }
            return extraExecutions;
        }
        private bool IsAvailable(Skill skill) => !_removedActionCards.Contains(skill)
            && !_exhaustedCards.Contains(skill)
            && (skill.ExhaustExecutionLimit <= 0 || ExecutionCount(skill) < skill.ExhaustExecutionLimit);
        public void PreviewAction(InstalledActionCard entry, bool isAdditionalTrigger = false) {
            var skill = entry.Skill;
            if (!ContinuePlan(skill.OwnerCharater) || !IsAvailable(skill) || _activeCards.Contains(skill)
                || (isAdditionalTrigger && !_additionallyTriggeredCards.Add(skill)))
                return;
            _activeCards.Add(skill);
            _work++;
            try {
                if (!SkipStunnedAction(skill.OwnerCharater)) {
                    skill.PreviewInstalledAction(entry, this, includeInitialExtraExecutions: !isAdditionalTrigger);
                    if (!_removedActionCards.Contains(skill) && skill.ExhaustExecutionLimit > 0
                        && ExecutionCount(skill) >= skill.ExhaustExecutionLimit
                        && _exhaustedCards.Add(skill))
                        PreviewCardExhausted();
                }
            }
            finally {
                _activeCards.Remove(skill);
            }
        }
        private void PreviewCardExhausted() {
            foreach (var owner in TalentOwners(Buff.BuffName.TalentExhaustPower).ToArray()) {
                int count = _talentExhaustRemainders.GetValueOrDefault(owner) + 1;
                _talentExhaustRemainders[owner] = count % 4;
                if (count >= 4)
                    GainPower(owner, count / 4);
            }
            foreach (var owner in TalentOwners(Buff.BuffName.TalentExhaustDamage).ToArray()) {
                Character[] enemies = _characters.Where(character => !character.IsPlayer && IsAlive(character)).ToArray();
                if (enemies.Length == 1)
                    ApplyDamage(enemies[0], 2, owner, false, true);
                else if (enemies.Length > 1)
                    // The target is chosen at execution; preview must not advance gameplay RNG.
                    MarkApproximate();
            }
            // Other exhaust effects can generate cards or change stats beyond this shadow world.
            if (_characters.Any(character => IsAlive(character)
                && (_states[character].Get(Buff.BuffName.Recycling) > 0
                    || _states[character].Get(Buff.BuffName.ExhaustShield) > 0
                    || _states[character].Get(Buff.BuffName.Demon) > 0)))
                MarkApproximate();
        }
        public override void TriggerInstalledCards(Character target) {
            if (target is not PlayerCharacter player || !IsAlive(player))
                return;
            PreviewAdditionalActions(_battle.GetActionCardExecutionOrder(player));
        }
        public override void ReturnActionCardsToHand(Character target) {
            // Returned cards leave the rack even when a full hand sends them to discard.
            RemoveActionCards(target, exhaust: false);
        }

        public override void RemoveActionCards(Character target, bool exhaust) {
            if (target is not PlayerCharacter player)
                return;
            foreach (var entry in _battle.GetInstalledActionCards(player)) {
                if (_exhaustedCards.Contains(entry.Skill) || !_removedActionCards.Add(entry.Skill))
                    continue;
                if (exhaust && _exhaustedCards.Add(entry.Skill))
                    PreviewCardExhausted();
            }
        }
        public override void TriggerRightActionCard(Skill skill) {
            var entry = _battle.GetRightActionCard(skill, IsAvailable);
            if (entry != null)
                PreviewAdditionalActions(new[] { entry });
        }
        private void PreviewAdditionalActions(IEnumerable<InstalledActionCard> cards) {
            if (_additionalActionDepth++ == 0)
                _additionallyTriggeredCards.Clear();
            try {
                foreach (var entry in cards)
                    PreviewAction(entry, isAdditionalTrigger: true);
            }
            finally {
                if (--_additionalActionDepth == 0)
                    _additionallyTriggeredCards.Clear();
            }
        }
        public override int PowerDelta(Character target) => _states.GetValueOrDefault(target)?.Power ?? 0;
        public override int SurvivabilityDelta(Character target) => _states.GetValueOrDefault(target)?.Survivability ?? 0;
        public override Character[] HostileTargets(Character owner, bool behind, bool taunt, bool invisible) {
            IEnumerable<Character> targets = _characters.Where(target => target.IsPlayer != owner.IsPlayer && IsAlive(target));
            targets = behind ? targets.OrderByDescending(target => target.PositionIndex) : targets.OrderBy(target => target.PositionIndex);
            var all = targets.ToArray();
            if (invisible) {
                var visible = all.Where(target => _states[target].Get(Buff.BuffName.Invisible) <= 0).ToArray();
                if (visible.Length > 0) all = visible;
            }
            if (taunt) {
                var taunting = all.Where(target => _states[target].Get(Buff.BuffName.Taunt) > 0).ToArray();
                if (taunting.Length > 0) all = taunting;
            }
            return all;
        }
        public override Character FriendlyFallback(Skill skill) => _characters
            .Where(target => target.IsPlayer == skill.OwnerCharater.IsPlayer && IsAlive(target)
                && (!skill.ManualFriendlyTargetExcludesSelf() || target != skill.OwnerCharater))
            .OrderBy(target => (double)_states[target].Life / Math.Max(1, target.BattleMaxLife))
            .ThenBy(target => target.PositionIndex).FirstOrDefault();
        public override int OutgoingDamage(Character owner, int damage) {
            if (!_states.TryGetValue(owner, out var state)) return 0;
            if (state.Get(Buff.BuffName.Divinity) > 0)
                damage = (int)MathF.Floor(damage * (owner.IsPlayer && TalentCount(Buff.BuffName.TalentDivinity) > 0 ? 2.5f : 2f));
            if (state.Get(Buff.BuffName.Weaken) > 0) damage = (int)MathF.Floor(damage * 0.75f);
            return Math.Clamp(damage, 0, 9999);
        }
        public bool SkipStunnedAction(Character owner) {
            if (!_states.TryGetValue(owner, out var state) || state.Get(Buff.BuffName.Stun) <= 0) return false;
            state.Buffs[Buff.BuffName.Stun]--;
            return true;
        }
        public override void Apply(Skill.PreviewEffectEntry entry) {
            if (++_work > 4096 || entry.Target == null || !_states.TryGetValue(entry.Target, out var state)) return;
            if (!IsAlive(entry.Target) && entry.Kind != Skill.PreviewEffectKind.Heal) return;
            switch (entry.Kind) {
                case Skill.PreviewEffectKind.Damage:
                    int hits = Math.Max(1, entry.HitCount);
                    // Raw plan preview omits vulnerability; apply defender reactions individually per hit.
                    for (int hit = 0; hit < hits && _work < 4096 && IsAlive(entry.Target); hit++) {
                        if (entry.Source != null && !IsAlive(entry.Source)) break;
                        _work++;
                        int damage = entry.Value / hits + (hit < entry.Value % hits ? 1 : 0);
                        bool attack = entry.Target.IsPlayer != entry.Source?.IsPlayer;
                        ApplyDamage(entry.Target, damage, entry.Source, attack, record: !entry.Target.IsPlayer);
                    }
                    break;
                case Skill.PreviewEffectKind.Block:
                    GainBlock(entry.Target, entry.Value);
                    break;
                case Skill.PreviewEffectKind.Heal:
                    if (IsAlive(entry.Target)) state.Life = Math.Min(entry.Target.BattleMaxLife,
                        state.Life + entry.Value + (entry.Value > 0 ? TalentCount(Buff.BuffName.TalentHealingPlus) : 0));
                    else MarkApproximate(); // Explicit revive eligibility is handled by gameplay only.
                    break;
                case Skill.PreviewEffectKind.Property:
                    if (entry.PropertyType == PropertyType.Power)
                        GainPower(entry.Target, entry.Value);
                    else if (entry.PropertyType == PropertyType.Survivability) state.Survivability += entry.Value;
                    else MarkApproximate();
                    break;
                case Skill.PreviewEffectKind.Buff:
                    if (entry.BuffName is not Buff.BuffName name) break;
                    if (Buff.IsDebuff(name) && state.Get(Buff.BuffName.DebuffImmunity) > 0) {
                        state.Buffs[Buff.BuffName.DebuffImmunity]--;
                        break;
                    }
                    bool entersStealth = name == Buff.BuffName.Invisible && entry.Value > 0 && state.Get(name) <= 0;
                    state.Buffs[name] = Math.Max(0, state.Get(name) + entry.Value);
                    if (name == Buff.BuffName.Invisible && entry.Value > 0)
                        foreach (var owner in _characters.Where(character => IsAlive(character)
                            && _states[character].Get(Buff.BuffName.Shadow) > 0).ToArray())
                            foreach (var ally in _characters.Where(character => IsAlive(character)
                                && character.IsPlayer == owner.IsPlayer).ToArray())
                                GainPower(ally, _states[owner].Get(Buff.BuffName.Shadow));
                    if (entersStealth && state.Get(name) > 0
                        && TalentOwners(Buff.BuffName.TalentStealthPower).Contains(entry.Target))
                        GainPower(entry.Target, 1);
                    if (entry.Value > 0 && !entry.Target.IsPlayer) {
                        if (name == Buff.BuffName.Vulnerable)
                            foreach (var owner in TalentOwners(Buff.BuffName.TalentVulnerableShock))
                                ApplyDamage(entry.Target, 5, owner, false, true);
                        if (name == Buff.BuffName.Weaken && entry.Source?.IsPlayer != false)
                            foreach (var owner in TalentOwners(Buff.BuffName.TalentWeakenGuard))
                                foreach (var ally in _characters.Where(character => character.IsPlayer))
                                    GainBlock(ally, entry.Value);
                    }
                    if (name is Buff.BuffName.Vulnerable or Buff.BuffName.DamageImmune or Buff.BuffName.Thorn
                        or Buff.BuffName.AutoArmor or Buff.BuffName.Taunt) {
                        if (!state.HurtOrder.Contains(name)) state.HurtOrder.Add(name);
                    }
                    else if (name is not (Buff.BuffName.Stun or Buff.BuffName.Weaken or Buff.BuffName.Frail or Buff.BuffName.Broken or Buff.BuffName.Divinity
                        or Buff.BuffName.Invisible or Buff.BuffName.Shadow or Buff.BuffName.ExtraPower or Buff.BuffName.RebirthI
                        or Buff.BuffName.Beacon) && !TalentTree.IsTalentBuff(name)) MarkApproximate();
                    break;
                case Skill.PreviewEffectKind.Message:
                    MarkApproximate();
                    break;
            }
        }
        private TurnEndAttackPreview GetTotal(Character target) {
            if (!_totals.TryGetValue(target, out var total))
                _totals[target] = total = new TurnEndAttackPreview { Target = target };
            return total;
        }
        private IEnumerable<Character> TalentOwners(Buff.BuffName name) => _characters.Where(character =>
            character.IsPlayer && !character.IsSummon && IsAlive(character) && _states[character].Get(name) > 0);
        private int TalentCount(Buff.BuffName name) => TalentOwners(name).Count();
        private void GainPower(Character target, int amount) {
            if (!IsAlive(target)) return;
            State state = _states[target];
            int gained = amount + (amount > 0 ? state.Get(Buff.BuffName.ExtraPower) : 0);
            state.Power += gained;
            if (gained <= 0 || !TalentOwners(Buff.BuffName.TalentFirstPowerShare).Contains(target)
                || !_talentFirstPowerShares.Add(target)) return;
            foreach (var ally in _characters.Where(ally => ally != target && ally.IsPlayer && IsAlive(ally)).ToArray())
                GainPower(ally, gained);
        }
        private void GainBlock(Character target, int amount, bool shareBeacon = true) {
            if (!IsAlive(target)) return;
            State state = _states[target];
            if (amount > 0 && state.Get(Buff.BuffName.Broken) > 0) return;
            amount = EndActionBuff.ModifyBlockGain(amount, state.Get(Buff.BuffName.Frail) > 0);
            int previous = state.Block;
            state.Block = Math.Clamp(state.Block + amount, 0, 999);
            int gained = Math.Max(0, state.Block - previous);
            if (gained <= 0) return;
            // Sum newly gained block, even if a later action consumes it; do not include existing block.
            if (target.IsPlayer) GetTotal(target).BlockGained += gained;
            int beacon = state.Get(Buff.BuffName.Beacon);
            if (!shareBeacon || beacon <= 0) return;
            foreach (var ally in _characters.Where(ally => ally != target && ally.IsPlayer == target.IsPlayer))
                GainBlock(ally, beacon, shareBeacon: false);
        }
        private void ApplyDamage(Character target, int incoming, Character source, bool attack, bool record) {
            State state = _states[target];
            float damage = incoming;
            int armor = 0;
            foreach (var name in state.HurtOrder) {
                int stack = state.Get(name);
                if (stack <= 0) continue;
                switch (name) {
                    case Buff.BuffName.DamageImmune:
                        damage = 0; state.Buffs[name]--; break;
                    case Buff.BuffName.Vulnerable when attack:
                        damage *= 1.5f + (!target.IsPlayer ? 0.2f * TalentCount(Buff.BuffName.TalentVulnerableAmplify) : 0f); break;
                    case Buff.BuffName.Thorn when attack && source != target && IsAlive(source):
                        ApplyDamage(source, stack, target, false, false); break;
                    case Buff.BuffName.AutoArmor when attack:
                        armor += stack; break;
                }
            }
            int value = Math.Max(0, (int)damage);
            int blocked = Math.Min(state.Block, value);
            int loss = Math.Min(Math.Max(value - state.Block, 0), state.Life);
            if (record) {
                var total = GetTotal(target);
                total.Hits++;
                total.Blocked += blocked;
                total.Damage += loss;
            }
            state.Life -= loss;
            state.Block = Math.Max(0, state.Block - value);
            GainBlock(target, armor);
            if (state.Life <= 0 && state.Get(Buff.BuffName.RebirthI) > 0) {
                state.Buffs[Buff.BuffName.RebirthI]--;
                state.Life = Math.Clamp(target.BattleMaxLife / 2, 0, 999);
            }
            if (state.Life <= 0)
                foreach (var summon in target.Summons)
                    if (_states.TryGetValue(summon, out var summonState)) summonState.Life = 0;
        }
        public TurnEndAttackPreview[] Result() {
            foreach (var total in _totals.Values) {
                total.Approximate = _approximate;
                total.Truncated = _work >= 4096;
            }
            return _totals.Values.ToArray();
        }
    }

    internal TurnEndAttackPreview[] BuildTurnEndAttackPreview() =>
        BuildActionOutcomePreview().Where(entry => entry.Hits > 0).ToArray();

    private TurnEndAttackPreview[] BuildActionOutcomePreview() {
        var simulation = new ActionPreviewSimulation(this);
        foreach (var player in GetPlayerPhaseActionOrder()) {
            foreach (var entry in GetActionCardExecutionOrder(player)) {
                if (!simulation.ContinuePlan(player)) break;
                if (!EnemiesList.Any(simulation.IsAlive) || !PlayersList.Any(simulation.IsAlive))
                    return simulation.Result();
                simulation.PreviewAction(entry);
            }
        }
        return simulation.Result();
    }

    internal void RefreshTurnEndAttackPreview()
    {
        bool show = CanShowActionDamagePreview();
        var usedTargets = new HashSet<ulong>();
        foreach (var pair in _actionOutcomePreviewTargets.ToArray())
        {
            if (!GodotObject.IsInstanceValid(pair.Value))
                _actionOutcomePreviewTargets.Remove(pair.Key);
            else if (!show)
                pair.Value.HideActionOutcomePreview();
        }
        if (!show) {
            RefreshIncomingDamagePreview();
            return;
        }

        TurnEndAttackPreview[] outcomes = BuildActionOutcomePreview();
        RefreshIncomingDamagePreview();
        foreach (TurnEndAttackPreview entry in outcomes)
        {
            ulong id = entry.Target.GetInstanceId();
            usedTargets.Add(id);
            _actionOutcomePreviewTargets[id] = entry.Target;
            entry.Target.ShowActionOutcomePreview(entry.Damage, entry.Hits, entry.Blocked,
                entry.BlockGained, entry.Approximate, entry.Truncated);
        }
        foreach (var pair in _actionOutcomePreviewTargets.ToArray())
            if (!usedTargets.Contains(pair.Key))
            {
                pair.Value.HideActionOutcomePreview();
                _actionOutcomePreviewTargets.Remove(pair.Key);
            }
    }

    private void PollTurnEndAttackPreview(double delta)
    {
        _turnEndAttackPreviewElapsed += delta;
        if (_turnEndAttackPreviewElapsed < 0.1)
            return;
        _turnEndAttackPreviewElapsed = 0;
        RefreshTurnEndAttackPreview();
    }

    private void FreeTurnEndAttackPreview()
    {
        foreach (Character target in _actionOutcomePreviewTargets.Values)
            if (GodotObject.IsInstanceValid(target))
                target.HideActionOutcomePreview(immediate: true);
        _actionOutcomePreviewTargets.Clear();
    }
}
