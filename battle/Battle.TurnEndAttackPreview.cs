using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Battle
{
    [Export] public Vector2 TurnEndAttackPreviewOffset = new(0f, -450f);
    private const float TurnEndAttackPreviewHeaderGap = 24f;
    private readonly Dictionary<ulong, VBoxContainer> _turnEndAttackPreviewPanels = new();
    private double _turnEndAttackPreviewElapsed;

    internal sealed class TurnEndAttackPreview
    {
        public Character Target;
        public int Damage;
        public int Blocked;
        public int Hits;
        public bool Approximate;
        public bool Truncated;
    }

    private sealed class TurnEndPreviewState
    {
        public int Life;
        public int Block;
        public readonly Dictionary<Buff, int> Stacks = new();

        public TurnEndPreviewState(Character character)
        {
            Life = character.Life;
            Block = character.Block;
        }

        public int GetStack(Buff buff) => Stacks.TryGetValue(buff, out int stack) ? stack : buff.Stack;
    }

    internal TurnEndAttackPreview[] BuildTurnEndAttackPreview()
    {
        Character[] characters = GetOrderedTeamCharacters(false, includeSummons: true)
            .Concat(GetOrderedTeamCharacters(true, includeSummons: true))
            .Where(character => character != null && GodotObject.IsInstanceValid(character)
                && character.State == Character.CharacterState.Normal && character.Life > 0)
            .Distinct().ToArray();
        var states = characters.ToDictionary(character => character, character => new TurnEndPreviewState(character));
        var totals = new Dictionary<Character, TurnEndAttackPreview>();
        var outgoingState = new AttackBuff.PreviewState();
        // Arbitrary event handlers are not executed during preview: they may change live gameplay.
        bool approximate = DyingEmitList.Count > 0 || characters.Any(character =>
            character.AttackBuffs.Any(buff => buff.Stack > 0 && buff.ThisBuffName == Buff.BuffName.Shadow)
            || character.EndActionBuffs.Any(buff => buff.Stack > 0 && buff.ThisBuffName == Buff.BuffName.Sanctuary));
        int simulatedHits = 0;

        foreach (PlayerCharacter player in GetPlayerPhaseActionOrder())
        {
            long count = (long)AttackCountBuff.GetCount(player) + AttackCountBuff.GetCount(player, temporary: true);
            for (long hit = 0; hit < count; hit++)
            {
                if (!EnemiesList.Any(enemy => states.TryGetValue(enemy, out var enemyState) && enemyState.Life > 0)
                    || !PlayersList.Any(ally => states.TryGetValue(ally, out var allyState) && allyState.Life > 0))
                    return totals.Values.ToArray();
                if (!states.TryGetValue(player, out TurnEndPreviewState attacker) || attacker.Life <= 0)
                    break;
                // Filter simulated deaths before applying invisible/taunt fallback rules.
                Character target = Skill.FilterHostileTargetSequence(characters.Where(character =>
                    !character.IsPlayer && states[character].Life > 0), applyTaunt: true).FirstOrDefault();
                if (target == null)
                    break;
                if (++simulatedHits > 4096)
                {
                    // Bound UI work even if a custom Step grants millions of zero-damage attacks.
                    foreach (TurnEndAttackPreview entry in totals.Values)
                        entry.Truncated = true;
                    return totals.Values.ToArray();
                }

                if (!totals.TryGetValue(target, out TurnEndAttackPreview total))
                    totals[target] = total = new TurnEndAttackPreview { Target = target, Approximate = approximate };
                total.Hits++;
                float damage = Skill.PreviewTurnEndAttackDamage(player, target, outgoingState);
                TurnEndPreviewState defender = states[target];
                int armor = 0;
                foreach (HurtBuff buff in target.HurtBuffs)
                {
                    int stack = defender.GetStack(buff);
                    if (stack <= 0)
                        continue;
                    switch (buff.ThisBuffName)
                    {
                        case Buff.BuffName.DamageImmune:
                            damage = 0;
                            defender.Stacks[buff] = stack - 1;
                            break;
                        case Buff.BuffName.Vulnerable:
                            damage *= 1.5f;
                            break;
                        case Buff.BuffName.Thorn:
                            ApplyPreviewIncomingDamage(player, attacker, stack);
                            break;
                        case Buff.BuffName.AutoArmor:
                            armor += stack;
                            break;
                    }
                }
                int incoming = Math.Max((int)damage, 0);
                total.Blocked += Math.Min(incoming, defender.Block);
                total.Damage += Math.Min(Math.Max(incoming - defender.Block, 0), defender.Life);
                ApplyPreviewDamageAndRebirth(target, defender, incoming);
                defender.Block = Math.Clamp(defender.Block + armor, 0, 99999);
                foreach (Character dead in new[] { player, target }.Where(character => states[character].Life <= 0))
                    foreach (SummonCharacter summon in dead.Summons)
                        if (states.TryGetValue(summon, out TurnEndPreviewState summonState))
                            summonState.Life = 0;
            }
        }
        return totals.Values.ToArray();
    }

    private static void ApplyPreviewIncomingDamage(Character target, TurnEndPreviewState state,
        float damage)
    {
        foreach (HurtBuff buff in target.HurtBuffs)
        {
            int stack = state.GetStack(buff);
            if (stack <= 0)
                continue;
            if (buff.ThisBuffName == Buff.BuffName.DamageImmune)
            {
                damage = 0;
                state.Stacks[buff] = stack - 1;
            }
        }
        ApplyPreviewDamageAndRebirth(target, state, Math.Max((int)damage, 0));
    }

    private static void ApplyPreviewDamageAndRebirth(Character target, TurnEndPreviewState state, int damage)
    {
        state.Life -= Math.Min(Math.Max(damage - state.Block, 0), state.Life);
        state.Block = Math.Max(state.Block - damage, 0);
        if (state.Life > 0)
            return;
        foreach (DyingBuff buff in target.DyingBuffs)
        {
            int stack = state.GetStack(buff);
            if (buff.ThisBuffName != Buff.BuffName.RebirthI || stack <= 0)
                continue;
            state.Life = Math.Min(state.Life + Math.Clamp(target.BattleMaxLife / 2, 0, 999), target.BattleMaxLife);
            state.Stacks[buff] = stack - 1;
        }
    }

    internal void RefreshTurnEndAttackPreview()
    {
        bool show = IsBattleAlive() && !HasBattleEnded() && IsResolvingPlayerTeamActionPhase
            && CharacterControl?.CanShowTurnEndAttackPreview == true;
        var usedTargets = new HashSet<ulong>();
        foreach (var pair in _turnEndAttackPreviewPanels.ToArray())
        {
            if (!GodotObject.IsInstanceValid(pair.Value))
                _turnEndAttackPreviewPanels.Remove(pair.Key);
            else if (!show)
                pair.Value.Visible = false;
        }
        if (!show)
            return;

        foreach (TurnEndAttackPreview entry in BuildTurnEndAttackPreview())
        {
            ulong id = entry.Target.GetInstanceId();
            usedTargets.Add(id);
            if (!_turnEndAttackPreviewPanels.TryGetValue(id, out VBoxContainer panel))
            {
                panel = PreviewEffectDisplay.CreatePanel(showRowBackground: false);
                panel.Name = "TurnEndAttackPreview";
                entry.Target.AddChild(panel);
                _turnEndAttackPreviewPanels[id] = panel;
            }
            var effects = new List<Skill.PreviewEffectEntry> {
                Skill.PreviewEffectEntry.Damage(entry.Target, entry.Damage, entry.Hits, 1, null),
                Skill.PreviewEffectEntry.Message(entry.Target, entry.Truncated ? "回合结束（部分预览）"
                    : entry.Approximate ? "回合结束（估算）" : "回合结束", null),
            };
            if (entry.Blocked > 0)
                effects.Add(Skill.PreviewEffectEntry.Message(entry.Target, $"格挡吸收 {entry.Blocked}", null));
            // Keep the preview at its final scale; the shared pop-in scale can cross into the header.
            panel.Visible = true;
            PreviewEffectDisplay.ShowPanel(panel, effects, Vector2.Zero, TurnEndAttackPreviewOffset);
            PositionTurnEndAttackPreview(panel, entry.Target);
        }
        foreach (var pair in _turnEndAttackPreviewPanels)
            if (!usedTargets.Contains(pair.Key))
                pair.Value.Visible = false;
    }

    private void PositionTurnEndAttackPreview(VBoxContainer panel, Character target)
    {
        float headerTop = float.PositiveInfinity;
        foreach (string nodePath in new[] { "Intention", "LifeBar" })
        {
            Control header = target.GetNodeOrNull<Control>(nodePath);
            if (header == null || !header.Visible)
                continue;
            // Controls such as Intention have zero size; include their visible icons and labels.
            foreach (Control control in header.GetChildren().OfType<Control>().Prepend(header))
            {
                if (!control.Visible)
                    continue;
                Transform2D transform = target.GetGlobalTransform().AffineInverse() * control.GetGlobalTransform();
                headerTop = Mathf.Min(headerTop, (transform * Vector2.Zero).Y);
            }
        }
        float y = Mathf.Min(TurnEndAttackPreviewOffset.Y,
            headerTop - TurnEndAttackPreviewHeaderGap - panel.Size.Y);
        panel.Position = new Vector2(panel.Position.X, y);
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
        foreach (VBoxContainer panel in _turnEndAttackPreviewPanels.Values)
            if (GodotObject.IsInstanceValid(panel))
                panel.QueueFree();
        _turnEndAttackPreviewPanels.Clear();
    }
}
