using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void RefreshSkillOwners(PlayerCharacter player)
    {
        Skill[] hand = player == _activePlayer ? GetActiveHandSkills() : player?.Skills;
        if (hand == null)
            return;

        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
                continue;

            if (
                hand[i].OwnerCharater == null
                || !GodotObject.IsInstanceValid(hand[i].OwnerCharater)
            )
                hand[i].OwnerCharater = player;
            hand[i].UpdateDescription();
        }
    }

    private static bool HasActiveStun(Character character)
    {
        return character?.SkillBuffs?.Any(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Stun && x.Stack > 0
            ) == true;
    }

    private void ResetCardDisplayTracking()
    {
        _handCardsBySkill.Clear();
        _handSlotsBySkill.Clear();

        for (int i = 0; i < _cards.Length; i++)
            ClearDrawEntryState(i, revealCard: false);

        _customDrawEntryStartPositions.Clear();
        _drawEntryStartCenters.Clear();
        _drawEntryFromPlayedCardOrigin.Clear();
        _pendingDrawEntryAnimations.Clear();
        foreach (int index in _drawEntryPreviewCards.Keys.ToArray())
            CancelDrawEntryPreview(index);
        _pendingHandReorderStarts.Clear();
        for (int i = 0; i < _displayedSkillIds.Length; i++)
        {
            if (_cardSlots[i] != null && GodotObject.IsInstanceValid(_cardSlots[i]))
                _cardSlots[i].Scale = Vector2.One;
            _displayedSkills[i] = null;
            _displayedSkillIds[i] = null;
            _cardDisplayInitialized[i] = false;
        }
    }

    private void ResetCardDisplayTracking(int index)
    {
        if (index < 0 || index >= _displayedSkillIds.Length)
            return;

        Skill displayedSkill = _displayedSkills[index];
        SkillCard slotCard = _cards[index];
        if (
            displayedSkill != null
            && _handCardsBySkill.TryGetValue(displayedSkill, out SkillCard identityCard)
            && ReferenceEquals(identityCard, slotCard)
        )
        {
            RemoveHandCardIdentity(displayedSkill, slotCard);
        }
        else if (slotCard != null)
        {
            RemoveHandCardIdentity(null, slotCard);
        }

        if (displayedSkill != null)
            _handSlotsBySkill.Remove(displayedSkill);

        ClearDrawEntryState(index, revealCard: false);
        _customDrawEntryStartPositions.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        _pendingDrawEntryAnimations.Remove(index);
        CancelDrawEntryPreview(index);
        if (_cardSlots[index] != null && GodotObject.IsInstanceValid(_cardSlots[index]))
            _cardSlots[index].Scale = Vector2.One;
        _displayedSkills[index] = null;
        _displayedSkillIds[index] = null;
        _cardDisplayInitialized[index] = false;
    }

    private void OnEndTurnPressed()
    {
        if (_isDiscardSelectionActive)
        {
            if (_isDiscardSelectionCompleting)
                return;

            _ = CompleteDiscardSelectionAsync();
            return;
        }

        if (_isPileCardSelectionActive)
        {
            _ = CompletePileCardSelectionAsync();
            return;
        }

        _ = QueueEndTurnAsync();
    }

    private bool CanUseEndTurnShortcut()
    {
        return Visible
            && _uiBuilt
            && !IsDebugConsoleOpen()
            && _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _endTurnButton != null
            && !_endTurnButton.Disabled;
    }

    private async Task QueueEndTurnAsync()
    {
        if (!CanQueueEndTurn())
            return;

        _endTurnQueued = true;
        ClearLiftedCard(instant: false);
        RefreshTurnUi();
        BattleNode?.RecordAutomationSnapshot("end_turn_queued");

        if (
            !_isProcessingCardQueue
            && _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
        )
            await ExecuteQueuedEndTurnAsync();
    }

    private bool CanQueueEndTurn()
    {
        return _activePlayer != null
            && !_isResolvingCard
            && !_endTurnQueued
            && !_isPileCardSelectionActive
            && !IsManualTargetSelectionPending()
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }

    private Task ExecuteQueuedEndTurnAsync()
    {
        if (!_endTurnQueued || !CanExecuteQueuedEndTurn())
        {
            _endTurnQueued = false;
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        _endTurnQueued = false;
        PlayerCharacter endingPlayer = _activePlayer;
        ClearLiftedCard(instant: false);
        _isResolvingCard = true;
        _isResolvingEndTurn = true;
        _freezeHandLayout = true;
        _suppressNextRefreshLayout = true;
        RefreshTurnUi();
        BattleNode?.RecordAutomationSnapshot("end_turn_execute");

        if (
            !GodotObject.IsInstanceValid(this)
            || endingPlayer == null
            || !GodotObject.IsInstanceValid(endingPlayer)
            || _activePlayer != endingPlayer
        )
        {
            _isResolvingEndTurn = false;
            return Task.CompletedTask;
        }

        endingPlayer.EndAction();
        return Task.CompletedTask;
    }

    private bool CanExecuteQueuedEndTurn()
    {
        return _activePlayer != null
            && !_isResolvingCard
            && !_isProcessingCardQueue
            && _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }

}
