using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle {
    [ExportGroup("Action Cards")]
    public const int InitialActionCardSlotsPerCharacter = 2;
    public const int MaxActionCardSlotsPerCharacter = 4;
    [Export(PropertyHint.Range, "1,4,1")] public int ActionCardSlotsPerCharacter { get; set; } = InitialActionCardSlotsPerCharacter;
    public int EffectiveActionCardSlotsPerCharacter => Math.Clamp(ActionCardSlotsPerCharacter, 1, MaxActionCardSlotsPerCharacter);
    [Export(PropertyHint.Range, "0,1,0.01")] public float ActionCardTriggerDelay { get; set; } = 0.12f;

    public sealed record InstalledActionCard(Skill Skill, Character FriendlyTarget, int PaidEnergy);
    private readonly Dictionary<PlayerCharacter, List<InstalledActionCard>> _installedActionCards = new();
    private readonly HashSet<Skill> _activeActionCards = new();
    private readonly HashSet<Skill> _additionallyTriggeredCards = new();
    private int _additionalActionDepth;
    public bool IsResolvingActionCards { get; private set; }
    public event Action<PlayerCharacter> ActionCardsChanged;
    internal event Action<PlayerCharacter, Skill> ActionCardInstalled;
    public event Action<PlayerCharacter, int> ActionCardTriggered;
    public event Action<PlayerCharacter, Skill> ActionCardUninstalled;
    public event Action<PlayerCharacter, Skill> ActionCardExhausted;
    public BattleActionCardsUi ActionCardsUi => GetNodeOrNull<BattleActionCardsUi>("ActionCardsLayer/ActionCardsUi");

    public InstalledActionCard[] GetInstalledActionCards(PlayerCharacter owner) =>
        owner != null && _installedActionCards.TryGetValue(owner, out var cards) ? cards.ToArray() : [];

    internal InstalledActionCard[] GetActionCardExecutionOrder(PlayerCharacter owner) =>
        GetInstalledActionCards(owner).Reverse().ToArray();

    internal InstalledActionCard GetRightActionCard(Skill skill, Func<Skill, bool> isAvailable = null) {
        if (skill?.OwnerCharater is not PlayerCharacter owner)
            return null;
        var cards = GetInstalledActionCards(owner)
            .Where(entry => isAvailable == null || isAvailable(entry.Skill)).ToArray();
        int index = Array.FindIndex(cards, entry => ReferenceEquals(entry.Skill, skill));
        return index >= 0 && index + 1 < cards.Length ? cards[index + 1] : null;
    }

    internal Task TriggerRightActionCardAsync(Skill skill) {
        var entry = GetRightActionCard(skill);
        return entry == null ? Task.CompletedTask : TriggerAdditionalActionCardsAsync(
            (PlayerCharacter)skill.OwnerCharater, new[] { entry });
    }

    public int GetInstalledActionCardCount(PlayerCharacter owner) =>
        owner != null && _installedActionCards.TryGetValue(owner, out var cards) ? cards.Count : 0;

    public bool HasInstalledActionCards(PlayerCharacter owner) =>
        GetInstalledActionCardCount(owner) > 0;

    public bool IsActionCardInstalled(Skill skill) => skill != null
        && _installedActionCards.Values.Any(cards => cards.Any(entry => ReferenceEquals(entry.Skill, skill)));

    private void PrepareActionCardPreviewTargets(bool refreshRandomTargets = false) {
        foreach (var entry in _installedActionCards.Values.SelectMany(cards => cards).ToArray()) {
            if (!_activeActionCards.Contains(entry.Skill))
                entry.Skill.PrepareActionCardPreviewTargets(entry.FriendlyTarget, entry.PaidEnergy,
                    refreshRandomTargets);
        }
        foreach (var skill in _playerTeamBattleHand ?? Array.Empty<Skill>()) {
            if (skill?.IsActionCard == true && !IsActionCardInstalled(skill)
                && !_activeActionCards.Contains(skill))
                skill.PrepareActionCardPreviewTargets(skill.GetManualFriendlyTarget(), null,
                    refreshRandomTargets);
        }
    }

    public bool TryGetInstalledActionPaidEnergy(Skill skill, out int paidEnergy) {
        var entry = _installedActionCards.Values.SelectMany(cards => cards)
            .FirstOrDefault(entry => ReferenceEquals(entry.Skill, skill));
        paidEnergy = entry?.PaidEnergy ?? 0;
        return entry != null;
    }

    internal Task<bool> PrepareActionCardInstallationAsync(Skill skill) => Task.FromResult(
        skill?.IsActionCard != true || CanInstallActionCard(skill));

    // No slot reservation remains to cancel; keep the entry point for queued-play recovery callers.
    internal void CancelActionCardInstallation(Skill skill) { }

    internal bool CanInstallActionCard(Skill skill) {
        if (skill?.IsActionCard != true || skill.OwnerCharater is not PlayerCharacter owner
            || owner.State == Character.CharacterState.Dying || IsActionCardInstalled(skill))
            return false;
        return true;
    }

    internal bool InstallActionCard(Skill skill, int paidEnergy) {
        if (!CanInstallActionCard(skill))
            return false;
        var owner = (PlayerCharacter)skill.OwnerCharater;
        if (!_installedActionCards.TryGetValue(owner, out var cards))
            _installedActionCards[owner] = cards = new();
        GetOrCreateBattleCardInstanceId(skill);
        var entry = new InstalledActionCard(skill, skill.GetManualFriendlyTarget(), paidEnergy);
        InstalledActionCard removed = null;
        while (cards.Count >= EffectiveActionCardSlotsPerCharacter) {
            removed = cards[0];
            cards.RemoveAt(0);
            // Capture the old rack visual before ActionCardsChanged remaps the surviving slots.
            ActionCardUninstalled?.Invoke(owner, removed.Skill);
            DiscardBattleSkill(owner, removed.Skill, forceDiscard: true);
        }
        cards.Add(entry); // Oldest first; each installation becomes the newest card.
        ActionCardsChanged?.Invoke(owner);
        ActionCardInstalled?.Invoke(owner, skill);
        NotifyHandPreviewContextChanged();
        RecordAutomationEvent("action_card_installed", new Dictionary<string, object> {
            ["owner"] = CharacterAutomationId(owner), ["skillId"] = skill.SkillId?.ToString(),
            ["instanceId"] = skill.BattleCardInstanceId, ["replaced"] = removed?.Skill.SkillId?.ToString(),
        });
        return true;
    }

    internal void UninstallAllActionCards(PlayerCharacter owner) {
        if (owner == null || !GodotObject.IsInstanceValid(owner)
            || !_installedActionCards.TryGetValue(owner, out var cards) || cards.Count == 0)
            return;

        var removed = cards.ToArray();
        cards.Clear();
        foreach (var entry in removed) {
            // Keep the old rack visuals until every outgoing card has started its animation.
            ActionCardUninstalled?.Invoke(owner, entry.Skill);
            DiscardBattleSkill(owner, entry.Skill, forceDiscard: true);
        }
        ActionCardsChanged?.Invoke(owner);
        NotifyHandPreviewContextChanged();
        RecordAutomationEvent("action_cards_uninstalled", new Dictionary<string, object> {
            ["owner"] = CharacterAutomationId(owner), ["count"] = removed.Length,
        });
    }

    internal void ReturnInstalledActionCardsToHand(PlayerCharacter owner) {
        if (owner == null || !GodotObject.IsInstanceValid(owner)
            || !_installedActionCards.TryGetValue(owner, out var cards) || cards.Count == 0)
            return;

        var returned = new List<Skill>();
        int returnedCount = cards.Count;
        foreach (var entry in cards.ToArray()) {
            cards.Remove(entry);
            entry.Skill.UpdateDescription();
            SkillCard sourceCard = ActionCardsUi?.GetRack(owner)?.GetInstalledCardVisual(entry.Skill);
            if (AddPlayerTeamBattleHandCardOrDiscard(entry.Skill, HandCardEntryOrigin.PlayedCard,
                    owner, sourceCard, returnedCount - cards.Count - 1) >= 0)
                returned.Add(entry.Skill);
        }

        owner.InvalidateSkillTooltipCache();
        ActionCardsChanged?.Invoke(owner);
        NotifyHandPreviewContextChanged();
        CharacterControl?.PrepareHandCardDrawEntry(GetPlayerTeamBattleHandEntryIndexes(returned),
            HandCardEntryOrigin.PlayedCard, owner);
        CharacterControl?.RefreshCurrentTurnUi();
        RecordAutomationEvent("action_cards_returned_to_hand", new Dictionary<string, object> {
            ["owner"] = CharacterAutomationId(owner), ["count"] = returnedCount,
        });
    }

    internal void ExhaustAllActionCards(PlayerCharacter owner) {
        if (owner == null || !GodotObject.IsInstanceValid(owner)
            || !_installedActionCards.TryGetValue(owner, out var cards) || cards.Count == 0)
            return;

        var removed = cards.ToArray();
        cards.Clear();
        foreach (var entry in removed)
            ActionCardExhausted?.Invoke(owner, entry.Skill);
        ExhaustPlayerTeamBattleSkills(removed.Select(entry => entry.Skill), owner);
        ActionCardsChanged?.Invoke(owner);
        NotifyHandPreviewContextChanged();
        RecordAutomationEvent("action_cards_exhausted", new Dictionary<string, object> {
            ["owner"] = CharacterAutomationId(owner), ["count"] = removed.Length,
        });
    }

    internal async Task ResolveInstalledActionCardsAsync(CancellationToken token) {
        if (IsResolvingActionCards)
            return;
        // Freeze the whole round's work so generated/carried cards cannot recursively extend the queue.
        var queue = GetPlayerPhaseActionOrder().SelectMany(owner => GetActionCardExecutionOrder(owner)
            .Select(entry => (Owner: owner, Entry: entry))).ToArray();
        IsResolvingActionCards = true;
        try {
            foreach (var item in queue) {
                if (!CanContinue(token) || HasBattleEnded())
                    break;
                if (!IsCharacterAlive(item.Owner) || !IsActionCardInstalled(item.Entry.Skill))
                    continue;
                await TriggerInstalledActionCardAsync(item.Owner, item.Entry, isAdditionalTrigger: false);
                if (!CanContinue(token) || HasBattleEnded())
                    break;
                if (ActionCardTriggerDelay > 0 && !await DelayOrCancel((int)(ActionCardTriggerDelay * 1000), token))
                    break;
            }
        }
        finally {
            IsResolvingActionCards = false;
        }
    }

    internal Task TriggerInstalledCardsAsync(PlayerCharacter owner) =>
        TriggerAdditionalActionCardsAsync(owner, GetActionCardExecutionOrder(owner));

    private async Task TriggerAdditionalActionCardsAsync(PlayerCharacter owner,
        InstalledActionCard[] cards) {
        if (!IsCharacterAlive(owner) || cards.Length == 0)
            return;
        bool wasResolving = IsResolvingActionCards;
        IsResolvingActionCards = true;
        if (_additionalActionDepth++ == 0)
            _additionallyTriggeredCards.Clear();
        try {
            foreach (var entry in cards) {
                if (!CanContinue(_lifetimeCts.Token) || HasBattleEnded() || !IsCharacterAlive(owner))
                    break;
                await TriggerInstalledActionCardAsync(owner, entry, isAdditionalTrigger: true);
            }
        }
        finally {
            if (--_additionalActionDepth == 0)
                _additionallyTriggeredCards.Clear();
            IsResolvingActionCards = wasResolving;
        }
    }

    private async Task TriggerInstalledActionCardAsync(PlayerCharacter owner, InstalledActionCard entry, bool isAdditionalTrigger) {
        // Extra installed-card triggers cannot re-enter a card already on the call stack.
        if (!IsActionCardInstalled(entry.Skill) || _activeActionCards.Contains(entry.Skill)
            || (isAdditionalTrigger && !_additionallyTriggeredCards.Add(entry.Skill)))
            return;
        _activeActionCards.Add(entry.Skill);
        try {
            int index = Array.FindIndex(GetInstalledActionCards(owner),
                current => ReferenceEquals(current.Skill, entry.Skill));
            ActionCardTriggered?.Invoke(owner, index);
            await entry.Skill.ExecuteInstalledActionAsync(entry.FriendlyTarget, entry.PaidEnergy,
                includeInitialExtraExecutions: !isAdditionalTrigger);
            if (entry.Skill.ResolvesExhaustsAfterUse)
                ExhaustInstalledActionCard(owner, entry.Skill);
            await ResolvePendingTalentEffectsAsync();
        }
        finally {
            _activeActionCards.Remove(entry.Skill);
        }
    }

    private void ClearActionCards() {
        _installedActionCards.Clear();
        _activeActionCards.Clear();
        _additionallyTriggeredCards.Clear();
    }

    private void ExhaustInstalledActionCard(PlayerCharacter owner, Skill skill) {
        if (!_installedActionCards.TryGetValue(owner, out var cards))
            return;
        int removed = cards.RemoveAll(entry => ReferenceEquals(entry.Skill, skill));
        if (removed == 0)
            return;
        // Snapshot the rack card before pile effects or layout refresh reuse its slot.
        ActionCardExhausted?.Invoke(owner, skill);
        DiscardBattleSkill(owner, skill);
        ActionCardsChanged?.Invoke(owner);
        NotifyHandPreviewContextChanged();
    }
}
