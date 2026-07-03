using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void RefreshTurnUi()
    {
        if (!_uiBuilt)
            return;

        bool updateLayout = !_suppressNextRefreshLayout;
        _suppressNextRefreshLayout = false;
        Skill[] previousDisplayedSkills = _displayedSkills.ToArray();
        Vector2?[] previousSlotPositions = CaptureCardSlotPositions();
        float?[] previousSlotRotations = CaptureCardSlotRotations();
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || _activePlayer.State == Character.CharacterState.Dying
        )
        {
            ClearCardEnergyPreview();
            _statusLabel.Text = "等待行动";
            PositionStatusLabel();
            ClearLiftedCard(instant: true);
            ClearCardQueue(resetCards: false);
            ResetCardDisplayTracking();
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null)
                    continue;

                ResetCardMotion(i, instant: true);
                _cards[i].Visible = false;
                _cards[i].SetHandIndexBadge(0, visible: false);
                SetCardButtonInputEnabled(_cards[i], false);
            }
            ClearHandCardIndexBadges();

            if (_endTurnButton != null)
                _endTurnButton.Disabled = true;
            if (_drawPileButton != null)
            {
                _drawPileButton.Text = string.Empty;
                _drawPileButton.TooltipText = "抽牌堆";
                _drawPileButton.Disabled = true;
                SetPileButtonCount(_drawPileButton, 0);
                SyncPileButtonVisualState(_drawPileButton);
            }
            if (_discardPileButton != null)
            {
                _discardPileButton.Text = string.Empty;
                _discardPileButton.TooltipText = "弃牌堆";
                _discardPileButton.Disabled = true;
                SetPileButtonCount(_discardPileButton, 0);
                SyncPileButtonVisualState(_discardPileButton);
            }
            if (_exhaustedPileButton != null)
            {
                _exhaustedPileButton.Text = string.Empty;
                _exhaustedPileButton.TooltipText = "消耗牌堆";
                _exhaustedPileButton.Disabled = true;
                SetPileButtonCount(_exhaustedPileButton, 0);
                SyncPileButtonVisualState(_exhaustedPileButton);
            }
            return;
        }

        _statusLabel.Text = BuildPlayerTeamTurnStatusText();
        PositionStatusLabel();

        Skill[] hand = GetActiveHandSkills();
        PruneHandCardsNotInHand(hand);
        SyncHandSlotIdentities(hand);
        bool hasAnyDrawEntryBusy = IsAnyCardDrawEntryBusy();
        bool manualTargetSelectionPending = IsManualTargetSelectionPending();
        bool pileSelectionViewMode =
            _isPileCardSelectionActive && _pileOverlayContentTemporarilyHidden;
        bool canInteractWithHandCards =
            !_isResolvingCard
            && !_endTurnQueued
            && !_isPileCardSelectionActive
            && !manualTargetSelectionPending;

        for (int i = 0; i < _cards.Length; i++)
        {
            Skill skill = hand != null && i < hand.Length ? hand[i] : null;
            SkillCard card = skill != null ? GetOrCreateHandCardForSkill(skill, i) : _cards[i];
            bool isLiftedSkill = skill != null && ReferenceEquals(skill, _liftedCardSkill);

            if (skill != null && card != null && !isLiftedSkill)
                AssignHandCardToSlot(card, i);

            if (card != null && _turnEndStatusTriggerCardIndexes.Contains(i))
            {
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (card != null && _pendingHandStatusExhaustIndexes.Contains(i))
            {
                card.Visible = true;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (card != null && IsCardCommitted(i))
            {
                card.Visible = true;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (skill == null)
            {
                _hiddenPendingDrawEntrySlotIndexes.Remove(i);
                if (card != null)
                {
                    card.Visible = false;
                    SetCardButtonInputEnabled(card, false);
                    card.SetPlayableHighlight(false, instant: true);
                }
                ResetCardMotion(i, instant: true);
                ResetCardDisplayTracking(i);
                continue;
            }

            if (card == null)
                continue;

            if (skill.OwnerCharater == null || !GodotObject.IsInstanceValid(skill.OwnerCharater))
                skill.OwnerCharater = _activePlayer;

            bool isNewCardForHand =
                !_cardDisplayInitialized[i] || !ReferenceEquals(_displayedSkills[i], skill);
            int previousSlotIndex = FindPreviousDisplayedSkillIndex(previousDisplayedSkills, skill);
            bool isDiscardReturnFlying = _discardSelectionFlyingReturnHandIndexes.Contains(i);
            bool isDiscardReturnPending = IsDiscardSelectionReturnSlot(i);
            bool hideForDiscardReturn =
                _discardSelectionReturningHandIndexes.Contains(i) && !isDiscardReturnFlying;
            bool movedFromExistingHandCard = previousSlotIndex >= 0;
            bool movedFromPendingReorder = PreparePendingHandReorderMove(i, skill);
            bool movedFromAnotherSlot =
                isNewCardForHand && !movedFromPendingReorder && movedFromExistingHandCard;
            bool shouldAnimate =
                isNewCardForHand
                && !isLiftedSkill
                && !movedFromPendingReorder
                && !movedFromAnotherSlot
                && !hideForDiscardReturn
                && !isDiscardReturnPending;
            bool shouldResetDisplayState =
                !isLiftedSkill
                && !isDiscardReturnFlying
                && !movedFromExistingHandCard
                && (!card.Visible || isNewCardForHand);

            if (shouldAnimate)
                PrepareNewHandCardSlotForDrawEntry(i);
            else if (movedFromAnotherSlot)
                PrepareMovedHandCardSlotFromPreviousPosition(
                    i,
                    previousSlotIndex,
                    previousSlotPositions,
                    previousSlotRotations
                );
            bool hideForDrawEntry =
                _hiddenPendingDrawEntrySlotIndexes.Contains(i)
                || _pendingDrawEntryAnimations.Contains(i);
            if (shouldResetDisplayState)
            {
                card.RestoreDisplayState(resetPlayableHighlight: false);
                SnapHandCardToBaseVisual(i, card);
            }
            if (!isLiftedSkill)
                card.Visible = !hideForDrawEntry && !hideForDiscardReturn;

            card.SetSkill(skill);
            string ownerDisplayName = GetSkillOwnerDisplayName(skill);
            if (card.CharacterName.Text != ownerDisplayName)
                card.CharacterName.Text = ownerDisplayName;

            _displayedSkills[i] = skill;
            _displayedSkillIds[i] = skill.SkillId;
            _cardDisplayInitialized[i] = true;

            if (isDiscardReturnFlying)
            {
                card.Visible = true;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (hideForDrawEntry || hideForDiscardReturn)
            {
                if (!isLiftedSkill)
                    card.Visible = false;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (pileSelectionViewMode)
            {
                card.SetHoverUiEnabled(true);
                SetCardButtonInputEnabled(card, false);
                card.Modulate = SkillButton.EnabledModulate;
                card.SetEnergyCostAffordable(skill.CanUseCurrentEnergy());
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            bool isArrowSelectedCard =
                _manualTargetArrowSelectionActive && i == _manualTargetArrowCardIndex;
            bool isDrawEntryBusy = IsCardDrawEntryBusy(i);
            bool canUseCurrentEnergy = skill.CanUseCurrentEnergy();
            bool isDiscardSelectionCandidate =
                _isDiscardSelectionActive
                && !_isDiscardSelectionCompleting
                && skill != null
                && !hasAnyDrawEntryBusy;
            bool canInteract =
                _liftedCardIndex != -1
                    ? i == _liftedCardIndex && canInteractWithHandCards
                    : isDiscardSelectionCandidate || canInteractWithHandCards;
            card.SetHoverUiEnabled(
                !_manualTargetArrowSelectionActive
                && (i == _liftedCardIndex || _liftedCardIndex == -1)
            );
            bool shouldShowPlayableHighlight =
                !_isDiscardSelectionActive
                && !isArrowSelectedCard
                && canInteract
                && skill.CanBePlayed
                && canUseCurrentEnergy;
            SetCardButtonInputEnabled(card, canInteract && !isArrowSelectedCard);
            card.Modulate = SkillButton.EnabledModulate;
            card.SetEnergyCostAffordable(canUseCurrentEnergy);
            card.SetPlayableHighlight(
                shouldShowPlayableHighlight,
                instant: movedFromAnotherSlot || movedFromPendingReorder
            );
        }

        RefreshHandCardIndexBadges(hand);

        if (_endTurnButton != null)
        {
            bool showEndTurnDuringHiddenPileSelection =
                _isPileCardSelectionActive && _pileOverlayContentTemporarilyHidden;
            _endTurnButton.Visible =
                !_isPileCardSelectionActive || showEndTurnDuringHiddenPileSelection;
            if (_isDiscardSelectionActive)
            {
                _endTurnButton.Text = _isDiscardSelectionCompleting
                    ? _discardSelectionExhaustMode
                        ? "消耗中"
                        : "丢弃中"
                    : "确认";
                _endTurnButton.Disabled =
                    _isDiscardSelectionCompleting
                    || _discardSelectionSkills.Count < _discardSelectionTargetCount;
            }
            else if (_isPileCardSelectionActive)
            {
                _endTurnButton.Text = "确认";
                _endTurnButton.Disabled =
                    _pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount;
            }
            else
            {
                _endTurnButton.Text = "结束回合";
                _endTurnButton.Disabled =
                    _isResolvingCard
                    || _endTurnQueued
                    || IsManualTargetSelectionPending();
            }
        }

        UpdatePileButtons();
        SyncDiscardSelectionHideButton();

        bool delayHandLayoutForShuffleDraw =
            updateLayout && !_freezeHandLayout && ShouldDelayHandLayoutForShuffleDraw();
        if (delayHandLayoutForShuffleDraw)
            ScheduleHandLayoutAfterShuffleDrawDelay();
        else if (updateLayout && !_freezeHandLayout && !manualTargetSelectionPending)
            LayoutActionCards();
        if (
            _isDiscardSelectionActive
            && !_isDiscardSelectionCompleting
            && _discardSelectionSkills.Count > 0
        )
        {
            ArrangeDiscardSelectionSelectedCards();
        }
    }

    private void SnapHandCardToBaseVisual(int index, SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.StopBattleMotion();
        card.HideHoverUi();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Position = Vector2.Zero;
        card.Scale = BattleCardScale;
        card.Rotation = 0f;
        card.HoverHint.Visible = false;

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot))
            slot.Scale = Vector2.One;
    }

    private SkillCard GetOrCreateHandCardForSkill(Skill skill, int index)
    {
        if (skill == null || !IsCardIndexValid(index))
            return null;

        PruneInvalidHandCardIdentities();
        if (
            _handCardsBySkill.TryGetValue(skill, out SkillCard existing)
            && existing != null
            && GodotObject.IsInstanceValid(existing)
        )
        {
            return existing;
        }

        SkillCard card = _cards[index];
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || (card.CurrentSkill != null && !ReferenceEquals(card.CurrentSkill, skill))
            || _handCardsBySkill.ContainsValue(card)
        )
        {
            card = TakeBattleCardFromPool(index);
        }

        _handCardsBySkill[skill] = card;
        return card;
    }

    private void SyncHandSlotIdentities(Skill[] hand)
    {
        PruneInvalidHandSlotIdentities();
        if (hand == null)
            return;

        int max = Math.Min(hand.Length, _cardSlots.Length);
        var activeSkills = new HashSet<Skill>(hand.Take(max).Where(skill => skill != null));
        foreach (Skill skill in _handSlotsBySkill.Keys.ToArray())
        {
            if (!activeSkills.Contains(skill) && !ReferenceEquals(skill, _liftedCardSkill))
                _handSlotsBySkill.Remove(skill);
        }

        for (int i = 0; i < max; i++)
        {
            Skill skill = hand[i];
            if (skill == null)
                continue;

            if (
                _handSlotsBySkill.TryGetValue(skill, out Control existingSlot)
                && existingSlot != null
                && GodotObject.IsInstanceValid(existingSlot)
            )
            {
                int currentIndex = FindCardSlotIndex(existingSlot);
                if (IsCardIndexValid(currentIndex) && currentIndex != i)
                    MoveHandSlotIdentity(currentIndex, i, skill, activeSkills);
            }
        }

        for (int i = 0; i < max; i++)
        {
            Skill skill = hand[i];
            if (skill == null || _handSlotsBySkill.ContainsKey(skill))
                continue;

            if (_cardSlots[i] != null && GodotObject.IsInstanceValid(_cardSlots[i]))
            {
                _handSlotsBySkill[skill] = _cardSlots[i];
            }
        }
    }

    private void MoveHandSlotIdentity(
        int previousIndex,
        int newIndex,
        Skill skill,
        HashSet<Skill> activeSkills
    )
    {
        if (
            !IsCardIndexValid(previousIndex)
            || !IsCardIndexValid(newIndex)
            || previousIndex == newIndex
        )
        {
            return;
        }

        Skill previousOccupant = _displayedSkills[newIndex];

        SwapHandSlotArrays(previousIndex, newIndex);

        _handSlotsBySkill[skill] = _cardSlots[newIndex];
        if (
            previousOccupant != null
            && activeSkills?.Contains(previousOccupant) == true
            && _cardSlots[previousIndex] != null
        )
        {
            _handSlotsBySkill[previousOccupant] = _cardSlots[previousIndex];
        }

        WireBattleCard(_cards[newIndex], newIndex);
        WireBattleCard(_cards[previousIndex], previousIndex);
    }

    private void SwapHandSlotArrays(int a, int b)
    {
        (_cardSlots[a], _cardSlots[b]) = (_cardSlots[b], _cardSlots[a]);
        (_cards[a], _cards[b]) = (_cards[b], _cards[a]);
        (_cardSlotLayoutTweens[a], _cardSlotLayoutTweens[b]) = (
            _cardSlotLayoutTweens[b],
            _cardSlotLayoutTweens[a]
        );
        (_cardSlotLayoutTargets[a], _cardSlotLayoutTargets[b]) = (
            _cardSlotLayoutTargets[b],
            _cardSlotLayoutTargets[a]
        );
        (_cardSlotLayoutRotationTargets[a], _cardSlotLayoutRotationTargets[b]) = (
            _cardSlotLayoutRotationTargets[b],
            _cardSlotLayoutRotationTargets[a]
        );
        (_cardSlotLayoutFollowActive[a], _cardSlotLayoutFollowActive[b]) = (
            _cardSlotLayoutFollowActive[b],
            _cardSlotLayoutFollowActive[a]
        );
        (_cardSlotLayoutPixelsPerSecondOverrides[a], _cardSlotLayoutPixelsPerSecondOverrides[b]) =
            (_cardSlotLayoutPixelsPerSecondOverrides[b], _cardSlotLayoutPixelsPerSecondOverrides[a]);
        (_displayedSkills[a], _displayedSkills[b]) = (_displayedSkills[b], _displayedSkills[a]);
        (_displayedSkillIds[a], _displayedSkillIds[b]) = (_displayedSkillIds[b], _displayedSkillIds[a]);
        (_cardDisplayInitialized[a], _cardDisplayInitialized[b]) = (
            _cardDisplayInitialized[b],
            _cardDisplayInitialized[a]
        );
        (_cardHoverPreviewActive[a], _cardHoverPreviewActive[b]) = (
            _cardHoverPreviewActive[b],
            _cardHoverPreviewActive[a]
        );

        SwapIndexReference(ref _hoveredCardIndex, a, b);
        SwapIndexReference(ref _liftedCardIndex, a, b);
        SwapIndexReference(ref _cardFootMarkerHoverIndex, a, b);
        SwapIndexReference(ref _manualTargetArrowCardIndex, a, b);
        SwapIndexSetMembership(_turnEndStatusTriggerCardIndexes, a, b);
        SwapIndexSetMembership(_pendingHandStatusExhaustIndexes, a, b);
        SwapIndexSetMembership(_discardSelectionReturningHandIndexes, a, b);
        SwapIndexSetMembership(_discardSelectionFlyingReturnHandIndexes, a, b);
        SwapIndexSetMembership(_drawEntrySlotIndexes, a, b);
        SwapIndexSetMembership(_hiddenPendingDrawEntrySlotIndexes, a, b);
        SwapIndexSetMembership(_pendingDrawEntryAnimations, a, b);
        SwapIndexSetMembership(_drawEntryFromPlayedCardOrigin, a, b);
        SwapIndexMapValues(_drawEntrySlotOrders, a, b);
        SwapIndexMapValues(_customDrawEntryStartPositions, a, b);
        SwapIndexMapValues(_drawEntryStartCenters, a, b);
        SwapIndexMapValues(_drawEntryPreviewCards, a, b);
        SwapIndexMapValues(_discardSelectionReturningPreviewCards, a, b);
    }

    private static void SwapIndexReference(ref int index, int a, int b)
    {
        if (index == a)
            index = b;
        else if (index == b)
            index = a;
    }

    private static void SwapIndexSetMembership(HashSet<int> set, int a, int b)
    {
        if (set == null || a == b)
            return;

        bool hasA = set.Remove(a);
        bool hasB = set.Remove(b);
        if (hasA)
            set.Add(b);
        if (hasB)
            set.Add(a);
    }

    private static void SwapIndexMapValues<T>(Dictionary<int, T> map, int a, int b)
    {
        if (map == null || a == b)
            return;

        bool hasA = map.TryGetValue(a, out T valueA);
        bool hasB = map.TryGetValue(b, out T valueB);
        if (hasA)
            map[b] = valueA;
        else
            map.Remove(b);

        if (hasB)
            map[a] = valueB;
        else
            map.Remove(a);
    }

    private void ClearHandSlotIndexState(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryState(index, revealCard: false);
        _customDrawEntryStartPositions.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        _pendingDrawEntryAnimations.Remove(index);
        CancelDrawEntryPreview(index);
        _turnEndStatusTriggerCardIndexes.Remove(index);
        _pendingHandStatusExhaustIndexes.Remove(index);
        _discardSelectionReturningHandIndexes.Remove(index);
        _discardSelectionFlyingReturnHandIndexes.Remove(index);
        _drawEntrySlotIndexes.Remove(index);
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        _cardHoverPreviewActive[index] = false;
    }

    private void PruneInvalidHandSlotIdentities()
    {
        foreach (Skill skill in _handSlotsBySkill.Keys.ToArray())
        {
            Control slot = _handSlotsBySkill[skill];
            if (skill == null || slot == null || !GodotObject.IsInstanceValid(slot))
                _handSlotsBySkill.Remove(skill);
        }
    }

    private int FindCardSlotIndex(Control slot)
    {
        if (slot == null)
            return -1;

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            if (ReferenceEquals(_cardSlots[i], slot))
                return i;
        }

        return -1;
    }

    private void AssignHandCardToSlot(SkillCard card, int index)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || !IsCardIndexValid(index))
            return;

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        int currentIndex = FindCardSlotIndex(card);
        if (IsCardIndexValid(currentIndex) && currentIndex != index)
            _cards[currentIndex] = null;

        SkillCard displaced = _cards[index];
        if (
            displaced != null
            && GodotObject.IsInstanceValid(displaced)
            && !ReferenceEquals(displaced, card)
            && !_handCardsBySkill.ContainsValue(displaced)
        )
        {
            displaced.Visible = false;
        }

        Node parent = card.GetParent();
        if (parent != slot)
        {
            Vector2 globalPosition = card.GlobalPosition;
            Vector2 scale = card.Scale;
            float rotation = card.Rotation;
            parent?.RemoveChild(card);
            slot.AddChild(card);
            card.GlobalPosition = globalPosition;
            card.Scale = scale;
            card.Rotation = rotation;
        }

        WireBattleCard(card, index);
        _cards[index] = card;
    }

    private void RemoveHandCardIdentity(Skill skill, SkillCard card = null)
    {
        if (skill != null)
            _handCardsBySkill.Remove(skill);

        if (card != null)
        {
            foreach (Skill key in _handCardsBySkill
                         .Where(pair => ReferenceEquals(pair.Value, card))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _handCardsBySkill.Remove(key);
            }
        }
    }

    private void PruneInvalidHandCardIdentities()
    {
        foreach (Skill skill in _handCardsBySkill.Keys.ToArray())
        {
            SkillCard card = _handCardsBySkill[skill];
            if (skill == null || card == null || !GodotObject.IsInstanceValid(card))
                _handCardsBySkill.Remove(skill);
        }
    }

    private void PruneHandCardsNotInHand(Skill[] hand)
    {
        var activeSkills = new HashSet<Skill>(
            hand?.Where(skill => skill != null) ?? Enumerable.Empty<Skill>()
        );

        foreach (Skill skill in _handCardsBySkill.Keys.ToArray())
        {
            if (activeSkills.Contains(skill))
                continue;

            SkillCard card = _handCardsBySkill[skill];
            if (ReferenceEquals(skill, _liftedCardSkill) || ReferenceEquals(card, _liftedCard))
                continue;

            _handCardsBySkill.Remove(skill);
            if (card != null && GodotObject.IsInstanceValid(card) && FindCardSlotIndex(card) >= 0)
                ReturnBattleCardToPool(card);
        }
    }

    private void SyncIndexStateForSkillMove(Skill skill, int previousIndex, int newIndex)
    {
        if (skill == null || previousIndex == newIndex)
            return;
        if (!IsCardIndexValid(previousIndex) || !IsCardIndexValid(newIndex))
            return;

        if (_hoveredCardIndex == previousIndex)
            _hoveredCardIndex = newIndex;
        if (_liftedCardIndex == previousIndex)
            _liftedCardIndex = newIndex;
        if (ReferenceEquals(_liftedCardSkill, skill))
            _liftedCardIndex = newIndex;
        if (_cardFootMarkerHoverIndex == previousIndex)
            _cardFootMarkerHoverIndex = newIndex;
        if (_manualTargetArrowCardIndex == previousIndex)
            _manualTargetArrowCardIndex = newIndex;

        MoveIndexFlag(_turnEndStatusTriggerCardIndexes, previousIndex, newIndex);
        MoveIndexFlag(_pendingHandStatusExhaustIndexes, previousIndex, newIndex);
        MoveIndexFlag(_discardSelectionReturningHandIndexes, previousIndex, newIndex);
        MoveIndexFlag(_discardSelectionFlyingReturnHandIndexes, previousIndex, newIndex);
        MoveIndexFlag(_drawEntrySlotIndexes, previousIndex, newIndex);
        MoveIndexFlag(_hiddenPendingDrawEntrySlotIndexes, previousIndex, newIndex);
        MoveIndexFlag(_pendingDrawEntryAnimations, previousIndex, newIndex);
        MoveIndexFlag(_drawEntryFromPlayedCardOrigin, previousIndex, newIndex);
        MoveIndexValue(_drawEntrySlotOrders, previousIndex, newIndex);
        MoveIndexValue(_customDrawEntryStartPositions, previousIndex, newIndex);
        MoveIndexValue(_drawEntryStartCenters, previousIndex, newIndex);
        MoveIndexValue(_drawEntryPreviewCards, previousIndex, newIndex);
        MoveIndexValue(_discardSelectionReturningPreviewCards, previousIndex, newIndex);

        _cardHoverPreviewActive[newIndex] = _cardHoverPreviewActive[previousIndex];
        _cardHoverPreviewActive[previousIndex] = false;
    }

    private static void MoveIndexFlag(HashSet<int> set, int previousIndex, int newIndex)
    {
        if (set == null || !set.Remove(previousIndex))
            return;

        set.Add(newIndex);
    }

    private static void MoveIndexValue<T>(Dictionary<int, T> map, int previousIndex, int newIndex)
    {
        if (map == null || !map.TryGetValue(previousIndex, out T value))
            return;

        map.Remove(previousIndex);
        map[newIndex] = value;
    }

    private int FindCardSlotIndex(SkillCard card)
    {
        if (card == null)
            return -1;

        for (int i = 0; i < _cards.Length; i++)
        {
            if (ReferenceEquals(_cards[i], card))
                return i;
        }

        return -1;
    }

    private int FindPreviousDisplayedSkillIndex(Skill[] previousDisplayedSkills, Skill skill)
    {
        if (skill == null || previousDisplayedSkills == null)
            return -1;

        for (int i = 0; i < previousDisplayedSkills.Length; i++)
        {
            if (ReferenceEquals(previousDisplayedSkills[i], skill))
                return i;
        }

        return -1;
    }

    private string BuildPlayerTeamTurnStatusText()
    {
        if (_isDiscardSelectionActive)
        {
            if (_isDiscardSelectionCompleting)
                return _discardSelectionExhaustMode ? "正在消耗所选牌" : "正在丢弃所选牌";

            int remaining = Math.Max(
                0,
                _discardSelectionTargetCount - _discardSelectionSkills.Count
            );
            if (_discardSelectionExhaustMode)
                return remaining > 0 ? $"选择{remaining}张牌消耗" : "确认消耗所选牌";

            return remaining > 0 ? $"选择{remaining}张牌丢弃" : "确认丢弃所选牌";
        }

        if (_isPileCardSelectionActive)
        {
            int remaining = Math.Max(
                0,
                _pileCardSelectionTargetCount - _pileCardSelectionIndexes.Count
            );
            string pileName = GetPileTitle(_pileCardSelectionKind);
            if (_pileCardSelectionAction == PileCardSelectionAction.Exhaust)
                return remaining > 0 ? $"从{pileName}选择{remaining}张牌消耗" : "正在消耗所选牌";

            return remaining > 0 ? $"从{pileName}选择{remaining}张牌加入手牌" : "正在加入手牌";
        }

        if (_isResolvingEndTurn)
            return "等待行动";

        int playerEnergy = BattleNode?.PlayerEnergy ?? 0;
        return $"玩家回合 | 能量 {playerEnergy}";
    }

    private static string GetSkillOwnerDisplayName(Skill skill)
    {
        if (skill?.IsStatusCard == true)
            return string.Empty;

        Character owner = skill?.OwnerCharater;
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return string.Empty;

        return GetCharacterEnergyDisplayName(owner);
    }

    private static string GetCharacterEnergyDisplayName(Character character, string suffix = null)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return suffix ?? string.Empty;

        string text = character.CharacterName;
        return string.IsNullOrWhiteSpace(suffix) ? text : $"{text} | {suffix}";
    }


}
