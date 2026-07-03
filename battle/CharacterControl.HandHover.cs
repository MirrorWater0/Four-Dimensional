using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void UpdateLiftedCardPosition(float delta)
    {
        if (_liftedCardIndex == -1)
        {
            UpdateProcessState();
            return;
        }

        if (!IsCardIndexValid(_liftedCardIndex))
        {
            ClearLiftedCard(instant: true);
            return;
        }

        SkillCard card = _liftedCard ?? _cards[_liftedCardIndex];
        bool isManualTargetLiftedCard =
            _manualTargetArrowUsesLiftedCard
            && _manualTargetArrowSelectionActive
            && _liftedCardIndex == _manualTargetArrowCardIndex;
        if (card == null || !card.Visible || (card.Button.Disabled && !isManualTargetLiftedCard))
        {
            ClearLiftedCard(instant: true);
            return;
        }

        if (isManualTargetLiftedCard)
        {
            card.ZIndex = LiftedCardZIndex;
            return;
        }

        Vector2 targetGlobalPosition = GetLiftedCardMouseTargetPosition(card);
        MoveLiftedCardTowardTarget(card, targetGlobalPosition, delta);
        card.ZIndex = LiftedCardZIndex;
    }

    private Vector2 GetLiftedCardMouseTargetPosition(SkillCard card)
    {
        Vector2 fallbackMousePosition =
            card != null && GodotObject.IsInstanceValid(card)
                ? card.GlobalPosition + _liftedCardMouseOffset
                : Vector2.Zero;
        Vector2 mousePosition = GetViewport()?.GetMousePosition() ?? fallbackMousePosition;
        return mousePosition - _liftedCardMouseOffset;
    }

    private static void MoveLiftedCardTowardTarget(
        SkillCard card,
        Vector2 targetGlobalPosition,
        float delta
    )
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 currentPosition = card.GlobalPosition;
        if (
            currentPosition.DistanceSquaredTo(targetGlobalPosition)
            <= LiftedCardMouseFollowSnapDistance * LiftedCardMouseFollowSnapDistance
        )
        {
            card.GlobalPosition = targetGlobalPosition;
            return;
        }

        float followRatio =
            1f - Mathf.Exp(-LiftedCardMouseFollowSharpness * Math.Max(0f, delta));
        card.GlobalPosition = currentPosition.Lerp(targetGlobalPosition, followRatio);
    }

    private bool SetCardHovered(int index, bool hovered)
    {
        if (!IsCardIndexValid(index))
            return false;

        if (hovered && _hoveredCardIndex == index)
            return false;

        if (_suppressHandHoverUntilMouseMove && hovered)
            return false;

        if (IsCardDrawEntryBusy(index) || _liftedCardIndex != -1)
        {
            if (!hovered)
                ClearHandCardHoverUiOnly(index);
            return false;
        }

        if (_isDiscardSelectionActive)
        {
            SkillCard handCard = _cards[index];
            if (handCard == null || !handCard.Visible)
                return false;

            if (hovered)
            {
                _hoveredCardIndex = index;
                handCard.HoverHint.Visible = true;
                handCard.ZIndex = HandCardHoverZIndex;
                handCard.TweenBattleMotion(
                    new Vector2(0f, CardHoverLiftY),
                    BattleCardScale * CardHoverScaleMultiplier
                );
                LayoutActionCards(instant: false);
                ApplyHandCardHoverInputOrder();
                return true;
            }

            if (_hoveredCardIndex == index)
                _hoveredCardIndex = -1;
            else
                return false;

            ResetCardMotion(index, instant: false);
            LayoutActionCards(instant: false);
            ApplyHandCardHoverInputOrder();
            return true;
        }

        SkillCard card = _cards[index];
        if (card == null || !card.Visible)
            return false;

        if (_manualTargetArrowSelectionActive)
        {
            HideCardHoverPreview(index);
            card.HideHoverUi();
            return false;
        }

        if (hovered)
        {
            if (_isResolvingCard || IsCardCommitted(index))
                return false;

            if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            {
                ClearCardEnergyPreview();
                ResetCardMotion(_hoveredCardIndex, instant: false);
            }

            _hoveredCardIndex = index;
            card.HoverHint.Visible = true;
            card.ZIndex = HandCardHoverZIndex;
            ShowCardEnergyPreview(GetHandSkill(index));
            card.TweenBattleMotion(
                new Vector2(0f, CardHoverLiftY),
                BattleCardScale * CardHoverScaleMultiplier
            );
            LayoutActionCards(instant: false);
            ApplyHandCardHoverInputOrder();
            return true;
        }

        if (_hoveredCardIndex != index)
            return false;

        _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        ResetCardMotion(index, instant: false);
        LayoutActionCards(instant: false);
        ApplyHandCardHoverInputOrder();
        return true;
    }

    private void ScheduleCardHoverRefresh()
    {
        if (
            !_uiBuilt
            || !Visible
            || _suppressHandHoverUntilMouseMove
            || _isResolvingCard
            || _endTurnQueued
        )
            return;

        int version = ++_deferredHoverRefreshVersion;
        CallDeferred(nameof(RefreshCardHoverUnderMouse), version);
    }

    private void RefreshCardHoverUnderMouse(int version)
    {
        if (
            version != _deferredHoverRefreshVersion
            || !_uiBuilt
            || !Visible
            || _suppressHandHoverUntilMouseMove
            || _isResolvingCard
            || _endTurnQueued
            || (_isPileCardSelectionActive && !_pileOverlayContentTemporarilyHidden)
            || IsManualTargetSelectionPending()
            || _liftedCardIndex != -1
        )
        {
            return;
        }

        Vector2 mousePosition = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
        int hoveredIndex = ResolveHandCardHoverIndex(mousePosition);
        if (
            hoveredIndex < 0
            && _hoveredCardIndex != -1
            && IsAnyCardDrawEntryBusy()
            && CanHoverHandCardAt(_hoveredCardIndex)
        )
        {
            SetCardHoverPreviewActive(_hoveredCardIndex, true);
            return;
        }

        if (hoveredIndex >= 0)
        {
            if (SetCardHovered(hoveredIndex, true))
                SetCardHoverPreviewActive(hoveredIndex, true);
            return;
        }

        if (_hoveredCardIndex != -1)
        {
            int previousHoveredIndex = _hoveredCardIndex;
            if (SetCardHovered(previousHoveredIndex, false))
                SetCardHoverPreviewActive(previousHoveredIndex, false);
        }
    }

    private int ResolveHandCardHoverIndex(Vector2 mousePosition)
    {
        List<int> probeOrder = BuildHandCardInputPriorityOrder();
        for (int i = probeOrder.Count - 1; i >= 0; i--)
        {
            int index = probeOrder[i];
            if (CanHoverHandCardAt(index) && GetHandCardHoverRect(index).HasPoint(mousePosition))
                return index;
        }

        return -1;
    }

    private bool CanHoverHandCardAt(int index)
    {
        if (!IsCardIndexValid(index) || IsCardCommitted(index) || IsCardDrawEntryBusy(index))
            return false;

        SkillCard card = _cards[index];
        return card != null
            && GodotObject.IsInstanceValid(card)
            && card.Visible
            && card.Button != null
            && !card.Button.Disabled;
    }

    private Rect2 GetHandCardHoverRect(int index)
    {
        SkillCard card = IsCardIndexValid(index) ? _cards[index] : null;
        if (card != null && GodotObject.IsInstanceValid(card) && card.Visible)
            return GetSkillCardHoverHitRect(card);

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot) && slot.Visible)
            return slot.GetGlobalRect();

        return new Rect2();
    }

    private static Rect2 GetSkillCardHoverHitRect(SkillCard card)
    {
        Rect2 rect =
            card.Button != null && GodotObject.IsInstanceValid(card.Button)
                ? card.Button.GetGlobalRect()
                : card.GetGlobalRect();
        if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
            return rect;

        // Scaled hand cards and CanvasGroup fit margin extend slightly beyond the base button rect.
        float padding = Math.Max(6f, rect.Size.Y * 0.02f);
        return rect.Grow(padding);
    }

    private Skill GetHandSkill(int index)
    {
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || GetActiveHandSkills() == null
            || index < 0
            || index >= GetActiveHandSkills().Length
        )
        {
            return null;
        }

        return GetActiveHandSkills()[index];
    }

    private int FindCurrentHandSkillIndex(Skill skill)
    {
        if (skill == null)
            return -1;

        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return -1;

        int length = Math.Min(hand.Length, _cards.Length);
        for (int i = 0; i < length; i++)
        {
            if (ReferenceEquals(hand[i], skill))
                return i;
        }

        return -1;
    }

    private void ShowCardEnergyPreview(Skill skill)
    {
        ClearCardEnergyPreview();
    }

    private void ClearCardEnergyPreview()
    {
        if (_energyPreviewCharacter != null && GodotObject.IsInstanceValid(_energyPreviewCharacter))
            _energyPreviewCharacter.SetEnergyUsePreviewVisible(false);

        _energyPreviewCharacter = null;
    }

    private void SetCardHoverPreviewActive(int index, bool active)
    {
        if (active && _manualTargetArrowSelectionActive)
        {
            HideCardHoverPreview(index);
            return;
        }

        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (active)
        {
            if (_cardHoverPreviewActive[index])
            {
                card.ShowSkillPreview();
                UpdateCardFootMarkerHover(
                    index,
                    GetHandSkill(index)?.OwnerCharater as PlayerCharacter
                );
                return;
            }

            _cardHoverPreviewActive[index] = true;
            card.ShowSkillPreview();
            UpdateCardFootMarkerHover(index, GetHandSkill(index)?.OwnerCharater as PlayerCharacter);
        }
        else
        {
            if (!_cardHoverPreviewActive[index])
                return;

            _cardHoverPreviewActive[index] = false;
            card.HideSkillPreview();
            if (_cardFootMarkerHoverIndex == index)
                ClearCardFootMarkerHover();
        }
    }

    private void UpdateCardFootMarkerHover(int index, PlayerCharacter player)
    {
        if (_cardFootMarkerHoverIndex == index && _cardFootMarkerHoverPlayer == player)
            return;

        ClearCardFootMarkerHover();
        _cardFootMarkerHoverIndex = index;
        _cardFootMarkerHoverPlayer = player;
        player?.SetFootMarkerCardHover(true);
    }

    private void ClearCardFootMarkerHover()
    {
        _cardFootMarkerHoverPlayer?.SetFootMarkerCardHover(false);
        _cardFootMarkerHoverPlayer = null;
        _cardFootMarkerHoverIndex = -1;
    }

    private void HideAllCardHoverPreviews()
    {
        ClearCardEnergyPreview();
        ClearCardFootMarkerHover();
        for (int i = 0; i < _cards.Length; i++)
            HideCardHoverPreview(i);
    }

    private void SetCardHoverUiEnabled(bool enabled)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card != null && GodotObject.IsInstanceValid(card))
                card.SetHoverUiEnabled(enabled);
        }
    }

    private void HideCardHoverPreview(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_cardHoverPreviewActive[index] && _cardFootMarkerHoverIndex == index)
            ClearCardFootMarkerHover();

        _cardHoverPreviewActive[index] = false;

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
            card.HideSkillPreview();
    }

    private void ResetCardMotion(
        int index,
        bool instant,
        float motionDuration = HandCardResetMotionDuration
    )
    {
        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;
        if (_liftedCardIndex == index)
            _liftedCardIndex = -1;

        HideCardHoverPreview(index);
        ClearDrawEntryState(index, revealCard: false);
        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
            slot.Scale = Vector2.One;

        Panel hoverHint = card.HoverHint;
        if (hoverHint != null && GodotObject.IsInstanceValid(hoverHint))
            hoverHint.Visible = false;

        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.ZIndex = 0;
        card.TweenBattleMotion(
            Vector2.Zero,
            BattleCardScale,
            instant ? 0f : Math.Max(0f, motionDuration),
            instant
        );
    }

    private void ClearHandCardHoverMotion(int index, bool instant)
    {
        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (index == _liftedCardIndex)
        {
            ClearHandCardHoverUiOnly(index);
            return;
        }

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        HideCardHoverPreview(index);
        card.HideHoverUi();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.ZIndex = 0;
        card.TweenBattleMotion(Vector2.Zero, BattleCardScale, instant ? 0f : 0.16f, instant);
    }

    private void ClearHandCardHoverUiOnly(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        if (index != _liftedCardIndex)
            HideCardHoverPreview(index);

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.HideHoverUi();
        card.HoverHint.Visible = false;
    }

    private void ClearAllHandCardHoverMotionExcept(int excludedIndex, bool instant)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == excludedIndex || i == _liftedCardIndex || IsCardCommitted(i))
                continue;

            ClearHandCardHoverMotion(i, instant);
        }
    }

    private void ClearCardQueue(bool resetCards)
    {
        HideManualTargetPicker();
        if (
            _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
            && _queuedCardSkills.Count == 0
        )
            return;

        foreach (QueuedCardPlay play in _queuedCardPlays.ToArray())
        {
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            RestoreHandSlotForQueuedPlay(play);
            if (resetCards && play != null && play.IsHandCard)
                ResetCardMotion(play.Index, instant: true);
        }

        _queuedCardPlays.Clear();
        foreach (QueuedCardPlay play in _queuedFollowUpCardPlays.ToArray())
        {
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            RestoreHandSlotForQueuedPlay(play);
        }
        _queuedFollowUpCardPlays.Clear();

        foreach (Skill skill in _queuedCardSkills.ToArray())
        {
            int index = FindCurrentHandSkillIndex(skill);
            if (resetCards && IsCardIndexValid(index))
                ResetCardMotion(index, instant: true);
        }
        _queuedCardSkills.Clear();
    }

    private bool IsCardCommitted(int index)
    {
        Skill skill = GetHandSkill(index);
        return skill != null && _queuedCardSkills.Contains(skill);
    }

    private bool IsAnyCardDrawEntryBusy()
    {
        return _hiddenPendingDrawEntrySlotIndexes.Count > 0
            || _pendingDrawEntryAnimations.Count > 0;
    }

    private bool IsCardDrawEntryBusy(int index)
    {
        return IsCardIndexValid(index)
            && (
                _hiddenPendingDrawEntrySlotIndexes.Contains(index)
                || _pendingDrawEntryAnimations.Contains(index)
                || _drawEntryPreviewCards.ContainsKey(index)
            );
    }

    private bool IsMouseOutsideHandArea()
    {
        Rect2? handRect = null;
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            Rect2 slotRect = slot.GetGlobalRect();
            handRect = handRect.HasValue ? MergeRect(handRect.Value, slotRect) : slotRect;
        }

        if (!handRect.HasValue)
        {
            if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
                return true;

            handRect = _cardRow.GetGlobalRect();
        }

        return !handRect.Value.Grow(HandAreaPadding).HasPoint(GetViewport().GetMousePosition());
    }

    private static Rect2 MergeRect(Rect2 a, Rect2 b)
    {
        Vector2 start = new(
            Mathf.Min(a.Position.X, b.Position.X),
            Mathf.Min(a.Position.Y, b.Position.Y)
        );
        Vector2 end = new(Mathf.Max(a.End.X, b.End.X), Mathf.Max(a.End.Y, b.End.Y));
        return new Rect2(start, end - start);
    }

    private bool IsCardIndexValid(int index)
    {
        return index >= 0 && index < _cards.Length;
    }


}
