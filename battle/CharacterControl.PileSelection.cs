using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void ConfigurePileSelectionCard(SkillCard card, BattlePileKind kind, int pileIndex)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        ClearPileSelectionCardBinding(card);
        bool selectable =
            _isPileCardSelectionActive
            && kind == _pileCardSelectionKind
            && pileIndex >= 0;
        if (!selectable)
            return;

        _pileCardSelectionCards[pileIndex] = card;
        card.Button.ToggleMode = true;
        bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
        card.Button.ButtonPressed = selected;
        card.Button.Disabled = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(selected);
        Action handler = () => _ = HandlePileCardSelectionPressedAsync(pileIndex);
        _pileCardSelectionPressHandlers[card] = handler;
        card.Button.Pressed += handler;
    }

    private void ClearPileSelectionCardBinding(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (_pileCardSelectionPressHandlers.TryGetValue(card, out Action handler))
        {
            card.Button.Pressed -= handler;
            _pileCardSelectionPressHandlers.Remove(card);
        }

        foreach (int key in _pileCardSelectionCards
            .Where(pair => pair.Value == card)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _pileCardSelectionCards.Remove(key);
        }

        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(false, instant: true);
    }

    private Task HandlePileCardSelectionPressedAsync(int pileIndex)
    {
        if (
            !_isPileCardSelectionActive
            || pileIndex < 0
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
        )
        {
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Contains(pileIndex))
        {
            _pileCardSelectionIndexes.Remove(pileIndex);
            RefreshPileCardSelectionVisuals();
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Count >= _pileCardSelectionTargetCount)
        {
            _pileCardSelectionIndexes.RemoveAt(_pileCardSelectionIndexes.Count - 1);
        }

        _pileCardSelectionIndexes.Add(pileIndex);
        RefreshPileCardSelectionVisuals();
        RefreshTurnUi();
        return Task.CompletedTask;
    }

    private async Task CompletePileCardSelectionAsync()
    {
        if (!_isPileCardSelectionActive)
            return;

        if (_pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount)
        {
            RefreshPileCardSelectionVisuals();
            RefreshTurnUi();
            return;
        }

        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        bool exhaustSelected = _pileCardSelectionAction == PileCardSelectionAction.Exhaust;
        int[] selectedIndexes = _pileCardSelectionIndexes.ToArray();
        BattlePileKind selectedKind = _pileCardSelectionKind;
        PlayerCharacter selectingPlayer = _activePlayer;

        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        ClearAllPileSelectionCardBindings();
        HidePileOverlay();
        RefreshTurnUi();

        int selectedCount = exhaustSelected
            ? await ExhaustSelectedPileCardsWithAnimationAsync(
                selectingPlayer,
                selectedKind,
                selectedIndexes
            )
            : MoveSelectedPileCardsToHand(selectedKind, selectedIndexes);
        completion?.TrySetResult(selectedCount);
    }

    private async Task<int> ExhaustSelectedPileCardsWithAnimationAsync(
        PlayerCharacter player,
        BattlePileKind kind,
        int[] selectedIndexes
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || selectedIndexes == null
            || selectedIndexes.Length == 0
        )
        {
            return 0;
        }

        Battle.BattleCardPileEntry[] pile = GetPileEntriesForSelection(player, kind);
        var animationEntries = new List<StatusCardExhaustAnimationEntry>();
        foreach (int index in selectedIndexes.Distinct().OrderBy(x => x))
        {
            if (index < 0 || index >= pile.Length)
                continue;

            Battle.BattleCardPileEntry entry = pile[index];
            PlayerCharacter owner = IsStatusSkillId(entry.SkillId)
                ? player
                : entry.Owner ?? player;
            animationEntries.Add(new StatusCardExhaustAnimationEntry(owner, entry.SkillId));
        }

        if (animationEntries.Count == 0)
            return 0;

        await PlayOwnedCardExhaustPreviewAnimationAsync(animationEntries);
        return ExhaustSelectedPileCards(kind, selectedIndexes);
    }

    private int ExhaustSelectedPileCards(
        BattlePileKind kind,
        IEnumerable<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        BattleCardPileTarget pileTarget = kind == BattlePileKind.Discard
            ? BattleCardPileTarget.DiscardPileCards
            : BattleCardPileTarget.DrawPileCards;
        int exhaustedCount = 0;
        int removedBefore = 0;
        foreach (int originalIndex in (selectedIndexes ?? Array.Empty<int>()).Distinct().OrderBy(x => x))
        {
            int currentIndex = originalIndex - removedBefore;
            if (!BattleNode.TryExhaustBattlePileCard(pileTarget, currentIndex, refreshUi: false))
                continue;

            exhaustedCount++;
            removedBefore++;
        }

        return exhaustedCount;
    }

    private int MoveSelectedPileCardsToHand(
        BattlePileKind kind,
        IEnumerable<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        int movedCount = 0;
        int removedBefore = 0;
        foreach (int originalIndex in (selectedIndexes ?? Array.Empty<int>()).Distinct().OrderBy(x => x))
        {
            int currentIndex = originalIndex - removedBefore;
            int handIndex = GetActiveHandFirstEmptyIndex();
            bool moved = kind switch
            {
                BattlePileKind.Draw => BattleNode.TryMoveDrawPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                BattlePileKind.Discard => BattleNode.TryMoveDiscardPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                _ => false,
            };
            if (!moved)
                continue;

            SetHandEntryStartPositionFromPileKind(handIndex, kind);
            movedCount++;
            removedBefore++;
        }

        return movedCount;
    }

    private int GetActiveHandFirstEmptyIndex()
    {
        Skill[] hand = GetActiveHandSkills();
        return hand == null ? -1 : Array.FindIndex(hand, skill => skill == null);
    }

    private int[] GetNextEmptyHandSlotIndexes(int count)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || count <= 0)
            return Array.Empty<int>();

        var slots = new List<int>();
        for (int i = 0; i < hand.Length && slots.Count < count; i++)
        {
            if (hand[i] == null)
                slots.Add(i);
        }

        return slots.ToArray();
    }

    private Vector2? GetPlayedCardDrawEntryStartGlobalCenter()
    {
        Vector2? liveCenter = TryGetQueuedPlayCardGlobalCenter(_activeExecutingCardPlay);
        if (liveCenter.HasValue)
            return liveCenter;

        liveCenter = TryGetQueuedPlayCardGlobalCenter(_activeQueuedCardPlay);
        if (liveCenter.HasValue)
            return liveCenter;

        return _activeExecutingCardPlay?.CachedDrawEntryStartCenter
            ?? _activeQueuedCardPlay?.CachedDrawEntryStartCenter;
    }

    private static Vector2? TryGetQueuedPlayCardGlobalCenter(QueuedCardPlay play)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return null;

        return card.GetGlobalRect().GetCenter();
    }

    private void SetHandEntryStartPositionFromPileKind(int handIndex, BattlePileKind kind)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        Button sourceButton = kind switch
        {
            BattlePileKind.Draw => _drawPileButton,
            BattlePileKind.Discard => _discardPileButton,
            BattlePileKind.Exhausted => _exhaustedPileButton,
            _ => null,
        };
        Vector2? start = GetHandEntryStartPositionFromButton(sourceButton);
        if (start.HasValue)
            _customDrawEntryStartPositions[handIndex] = start.Value;
    }

    private Vector2? GetHandEntryStartPositionFromButton(Button button)
    {
        if (
            button == null
            || !GodotObject.IsInstanceValid(button)
            || !button.IsInsideTree()
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return null;
        }

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        Vector2 sourceCenter = GetPileButtonVisualCenter(button);
        Vector2 cardRowOrigin = _cardRow.GetGlobalRect().Position;
        return sourceCenter - cardRowOrigin - cardSize * 0.5f;
    }

    private void RefreshPileCardSelectionVisuals()
    {
        foreach ((int pileIndex, SkillCard card) in _pileCardSelectionCards)
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
            card.Button.ButtonPressed = selected;
            card.Modulate = SkillButton.EnabledModulate;
            card.SetPlayableHighlight(selected);
        }
        SyncPileOverlaySelectionButtons();
    }

    private void CancelPileCardSelection()
    {
        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        _pileOverlayContentTemporarilyHidden = false;
        ClearAllPileSelectionCardBindings();
        SyncPileOverlaySelectionButtons();
        completion?.TrySetResult(0);
    }

    private void ClearAllPileSelectionCardBindings()
    {
        foreach ((SkillCard card, Action handler) in _pileCardSelectionPressHandlers.ToArray())
        {
            if (card != null && GodotObject.IsInstanceValid(card))
            {
                card.Button.Pressed -= handler;
                card.Button.ToggleMode = false;
                card.Button.ButtonPressed = false;
                card.Modulate = SkillButton.EnabledModulate;
                card.SetPlayableHighlight(false, instant: true);
            }
        }

        _pileCardSelectionPressHandlers.Clear();
    }

}
