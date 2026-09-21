using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private const int HandHoverPreviewDelayMs = 55;

    private void StartHandHoverValidationTimer()
    {
        if (_handHoverValidationTimer != null && GodotObject.IsInstanceValid(_handHoverValidationTimer))
            return;

        _handHoverValidationTimer = new Timer
        {
            Name = "HandHoverValidationTimer",
            WaitTime = HandHoverValidationIntervalSeconds,
            OneShot = false,
        };
        _handHoverValidationTimer.Timeout += ValidateHandHoverUnderMouse;
        AddChild(_handHoverValidationTimer);
        _handHoverValidationTimer.Start();
    }

    private void ValidateHandHoverUnderMouse() => ValidateHandHoverUnderMouse(force: false);

    private void ValidateHandHoverUnderMouse(bool force)
    {
        if (!_uiBuilt || !Visible)
            return;

        Vector2 pointerPosition = GetHandCardPointerPosition();
        bool handMotionActive = IsAnyHandLayoutFollowerActive() || IsAnyCardDrawEntryBusy();
        if (
            !force
            && _handHoverValidationPointerInitialized
            && !handMotionActive
            && _lastHandHoverValidationPointerPosition.DistanceSquaredTo(pointerPosition) < 0.25f
        )
        {
            return;
        }

        _handHoverValidationPointerInitialized = true;
        _lastHandHoverValidationPointerPosition = pointerPosition;
        bool canResolveHover =
            !_suppressHandHoverUntilMouseMove
            && !_endTurnQueued
            && !IsHandInputBlockedByOverlay()
            && _liftedCardIndex == -1;
        int hoveredIndex = canResolveHover ? ResolveHandCardHoverIndex(pointerPosition) : -1;
        ClearOrphanedHandCardHoverVisuals(hoveredIndex);

        // Do not wait for another mouse-motion event to correct a missed MouseExited signal.
        // A new version also invalidates a deferred refresh queued before this validation.
        _queuedHoverRefreshVersion = 0;
        _deferredHoverRefreshVersion++;
        if (canResolveHover)
            ApplyResolvedCardHover(hoveredIndex);
    }

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
            && _liftedCardIndex == _manualTargetArrowCardIndex;
        if (card == null || !card.Visible || (card.Button.Disabled && !isManualTargetLiftedCard))
        {
            ClearLiftedCard(instant: true);
            return;
        }

        if (isManualTargetLiftedCard)
        {
            RotateLiftedCardTowardNeutral(card, delta);
            card.ZIndex = LiftedCardZIndex;
            return;
        }

        Vector2 targetGlobalPosition = GetLiftedCardMouseTargetPosition(card);
        MoveLiftedCardTowardTarget(card, targetGlobalPosition, delta);
        RotateLiftedCardTowardNeutral(card, delta);
        card.ZIndex = LiftedCardZIndex;
    }

    private Vector2 GetLiftedCardMouseTargetPosition(SkillCard card)
    {
        return GetGlobalMousePosition() - _liftedCardMouseOffset;
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

    private static void RotateLiftedCardTowardNeutral(SkillCard card, float delta)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (Math.Abs(card.Rotation) <= LiftedCardRotationSnapDistance)
        {
            card.Rotation = 0f;
            return;
        }

        float followRatio =
            1f - Mathf.Exp(-LiftedCardRotationFollowSharpness * Math.Max(0f, delta));
        card.Rotation = Mathf.LerpAngle(card.Rotation, 0f, followRatio);
    }

    private bool SetCardHovered(int index, bool hovered)
    {
        if (!IsCardIndexValid(index))
            return false;

        if (hovered && _hoveredCardIndex == index)
        {
            if (
                _isDiscardSelectionActive
                && _discardSelectionOriginalVisualHandIndexes.Contains(index)
            )
            {
                SkillCard trackedCard = _cards[index];
                if (trackedCard != null && GodotObject.IsInstanceValid(trackedCard))
                    trackedCard.EnsureCenteredPointerHoverState();
            }
            return false;
        }

        if (_suppressHandHoverUntilMouseMove && hovered)
            return false;

        if (IsCardDrawEntryInputBlocked(index) || _liftedCardIndex != -1)
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
                int previousHoveredIndex = _hoveredCardIndex;
                if (previousHoveredIndex != -1 && previousHoveredIndex != index)
                {
                    ClearHandCardHoverMotion(previousHoveredIndex, instant: false);
                    if (_discardSelectionOriginalVisualHandIndexes.Contains(previousHoveredIndex))
                    {
                        ApplyHandCardLayer(
                            previousHoveredIndex,
                            GetHandOrderForSlotIndex(previousHoveredIndex)
                        );
                    }
                }

                _hoveredCardIndex = index;
                handCard.HoverHint.Visible = true;
                handCard.ZIndex = HandCardHoverZIndex;
                AudioManager.PlayCardHover(handCard);
                bool isSelectedCard = _discardSelectionOriginalVisualHandIndexes.Contains(index);
                if (isSelectedCard)
                    handCard.SetCenteredPointerHoverState(true);
                else
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

            bool wasSelectedCard = _discardSelectionOriginalVisualHandIndexes.Contains(index);
            if (wasSelectedCard)
            {
                handCard.SetCenteredPointerHoverState(false);
                handCard.HideHoverUi();
                ApplyHandCardLayer(index, GetHandOrderForSlotIndex(index));
            }
            else
            {
                ResetCardMotion(index, instant: false);
            }
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
            if (IsCardCommitted(index))
                return false;

            if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            {
                ClearCardEnergyPreview();
                ResetCardMotion(_hoveredCardIndex, instant: false);
            }

            _hoveredCardIndex = index;
            card.HoverHint.Visible = true;
            card.ZIndex = HandCardHoverZIndex;
            AudioManager.PlayCardHover(card);
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
            || _endTurnQueued
        )
            return;

        if (_queuedHoverRefreshVersion != 0)
            return;

        int version = ++_deferredHoverRefreshVersion;
        _queuedHoverRefreshVersion = version;
        CallDeferred(nameof(RefreshCardHoverUnderMouse), version);
    }

    private void RefreshCardHoverUnderMouse(int version)
    {
        if (version == _queuedHoverRefreshVersion)
            _queuedHoverRefreshVersion = 0;

        if (
            version != _deferredHoverRefreshVersion
            || !_uiBuilt
            || !Visible
            || _suppressHandHoverUntilMouseMove
            || _endTurnQueued
            || IsHandInputBlockedByOverlay()
            || _liftedCardIndex != -1
        )
        {
            return;
        }

        Vector2 mousePosition = GetHandCardPointerPosition();
        int hoveredIndex = ResolveHandCardHoverIndex(mousePosition);
        ApplyResolvedCardHover(hoveredIndex);
    }

    private void ApplyResolvedCardHover(int hoveredIndex)
    {
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
            HideAllCardHoverPreviewsExcept(hoveredIndex);
            SetCardHovered(hoveredIndex, true);
            SetCardHoverPreviewActive(hoveredIndex, true);
            return;
        }

        if (_hoveredCardIndex != -1)
        {
            int previousHoveredIndex = _hoveredCardIndex;
            SetCardHovered(previousHoveredIndex, false);
            SetCardHoverPreviewActive(previousHoveredIndex, false);
            return;
        }

        HideAllCardHoverPreviews();
    }

    private void ClearOrphanedHandCardHoverVisuals(int preservedIndex)
    {
        bool clearedAny = false;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == preservedIndex || i == _liftedCardIndex)
                continue;

            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool hasTrackedHover = _hoveredCardIndex == i;
            bool hasVisibleHover =
                card.HoverHint != null
                && GodotObject.IsInstanceValid(card.HoverHint)
                && card.HoverHint.Visible;
            if (!hasTrackedHover && !hasVisibleHover)
                continue;

            ClearHandCardHoverMotion(i, instant: false);
            if (_discardSelectionOriginalVisualHandIndexes.Contains(i))
                ApplyHandCardLayer(i, GetHandOrderForSlotIndex(i));
            clearedAny = true;
        }

        if (!clearedAny)
            return;

        LayoutActionCards(instant: false);
        ApplyHandCardHoverInputOrder();
    }

    private int ResolveHandCardHoverIndex(Vector2 viewportPosition)
    {
        for (int i = _discardSelectionSkills.Count - 1; i >= 0; i--)
        {
            Skill skill = _discardSelectionSkills[i];
            if (
                skill == null
                || !_discardSelectionOriginalHandIndexes.TryGetValue(skill, out int index)
                || !_discardSelectionOriginalVisualHandIndexes.Contains(index)
                || !CanHoverHandCardAt(index)
            )
            {
                continue;
            }

            if (IsHandCardPointerInside(index, viewportPosition))
                return index;
        }

        int visibleCount = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard: true
        );
        if (visibleCount <= 0)
            return -1;

        if (
            _hoveredCardIndex >= 0
            && FindVisibleHandOrder(_visibleHandSlotIndexesBuffer, visibleCount, _hoveredCardIndex)
                >= 0
            && CanHoverHandCardAt(_hoveredCardIndex)
            && IsHandCardPointerInside(_hoveredCardIndex, viewportPosition)
        )
        {
            return _hoveredCardIndex;
        }

        List<int> inputPriorityOrder = BuildHandCardInputPriorityOrder();
        for (int order = inputPriorityOrder.Count - 1; order >= 0; order--)
        {
            int index = inputPriorityOrder[order];
            if (CanHoverHandCardAt(index) && IsHandCardPointerInside(index, viewportPosition))
                return index;
        }

        return -1;
    }

    private Vector2 GetHandCardPointerPosition()
    {
        if (!IsInstanceValid(this))
            return Vector2.Zero;

        return MouseTrail.GetResponsiveViewportMousePosition(GetViewport(), GetWindow());
    }

    private bool TryHandleResponsiveHandCardPress(InputEventMouseButton mouseButton)
    {
        if (
            mouseButton == null
            || !mouseButton.Pressed
            || mouseButton.ButtonIndex != MouseButton.Left
            || !_uiBuilt
            || !Visible
            || _endTurnQueued
            || _handLayoutSyncPending
            || IsHandInputBlockedByOverlay()
            || _manualTargetArrowSelectionActive
        )
        {
            return false;
        }

        Vector2 viewportPosition = GetHandCardPointerPosition();
        int index = ResolveHandCardHoverIndex(viewportPosition);
        if (index < 0)
            return false;

        ApplyResolvedCardHover(index);
        return TryStartHandCardPress(index);
    }

    private bool CanHoverHandCardAt(int index)
    {
        if (!IsCardIndexValid(index) || IsCardCommitted(index) || IsCardDrawEntryInputBlocked(index))
            return false;

        SkillCard card = _cards[index];
        return card != null
            && GodotObject.IsInstanceValid(card)
            && card.Visible
            && card.Button != null
            && !card.Button.Disabled;
    }

    private bool IsHandCardPointerInside(int index, Vector2 viewportPosition)
    {
        SkillCard card = IsCardIndexValid(index) ? _cards[index] : null;
        if (card != null && GodotObject.IsInstanceValid(card) && card.Visible)
            return IsViewportPointInsideSkillCard(card, viewportPosition);

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot) && slot.Visible)
            return IsViewportPointInsideControl(slot, viewportPosition);

        return false;
    }

    private static bool IsViewportPointInsideSkillCard(
        SkillCard card,
        Vector2 viewportPosition
    )
    {
        Control hitControl =
            card.Button != null && GodotObject.IsInstanceValid(card.Button)
                ? card.Button
                : card;

        float padding = Math.Max(6f, hitControl.Size.Y * 0.02f);
        return IsViewportPointInsideControl(hitControl, viewportPosition, padding);
    }

    private static bool IsViewportPointInsideControl(
        Control control,
        Vector2 viewportPosition,
        float localPadding = 0f
    )
    {
        if (
            control == null
            || !GodotObject.IsInstanceValid(control)
            || !control.Visible
            || !control.IsInsideTree()
        )
        {
            return false;
        }

        Transform2D transform = control.GetGlobalTransformWithCanvas();
        float determinant = transform.Determinant();
        if (!Mathf.IsFinite(determinant) || Mathf.IsZeroApprox(determinant))
            return false;

        Vector2 localPosition = transform.AffineInverse() * viewportPosition;
        return new Rect2(Vector2.Zero, control.Size).Grow(Math.Max(0f, localPadding))
            .HasPoint(localPosition);
    }

    private static Rect2 GetControlViewportAabb(Control control)
    {
        if (
            control == null
            || !GodotObject.IsInstanceValid(control)
            || !control.Visible
            || !control.IsInsideTree()
        )
        {
            return new Rect2();
        }

        Transform2D transform = control.GetGlobalTransformWithCanvas();
        Vector2 topLeft = transform * Vector2.Zero;
        Vector2 topRight = transform * new Vector2(control.Size.X, 0f);
        Vector2 bottomLeft = transform * new Vector2(0f, control.Size.Y);
        Vector2 bottomRight = transform * control.Size;
        Vector2 min = new(
            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)),
            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y))
        );
        Vector2 max = new(
            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)),
            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y))
        );
        return new Rect2(min, max - min);
    }

    private Skill GetHandSkill(int index)
    {
        Skill[] hand = GetActiveHandSkills();
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || hand == null
            || index < 0
            || index >= hand.Length
        )
        {
            return null;
        }

        return hand[index];
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
        if (active && _manualTargetArrowSelectionActive && index != _liftedCardIndex)
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
                UpdateCardFootMarkerHover(
                    index,
                    GetHandSkill(index)?.OwnerCharater as PlayerCharacter
                );
                UpdateCardPreviewTriggeredBuffHighlights(index, GetHandSkill(index));
                return;
            }

            if (_pendingCardHoverPreviewIndex == index)
                return;

            _pendingCardHoverPreviewIndex = index;
            int version = ++_cardHoverPreviewRequestVersion;
            _ = ShowCardHoverPreviewDelayed(index, version);
            return;
        }

        CancelPendingCardHoverPreview(index);

        if (!_cardHoverPreviewActive[index])
            return;

        _cardHoverPreviewActive[index] = false;
        card.HideSkillPreview();
        ClearCardPreviewTriggeredBuffHighlights(index);
        if (_cardFootMarkerHoverIndex == index)
            ClearCardFootMarkerHover();
    }

    private async Task ShowCardHoverPreviewDelayed(int index, int version)
    {
        if (HandHoverPreviewDelayMs > 0)
        {
            SceneTree tree = GetTree();
            if (tree == null)
                return;

            await ToSignal(
                tree.CreateTimer(HandHoverPreviewDelayMs / 1000.0f),
                SceneTreeTimer.SignalName.Timeout
            );
        }

        if (
            version != _cardHoverPreviewRequestVersion
            || _pendingCardHoverPreviewIndex != index
            || (_hoveredCardIndex != index && _liftedCardIndex != index)
            || !IsCardIndexValid(index)
            || (_manualTargetArrowSelectionActive && index != _liftedCardIndex)
        )
        {
            return;
        }

        SkillCard card = _cards[index];
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || !card.Visible
            || IsCardCommitted(index)
            || IsCardDrawEntryInputBlocked(index)
        )
        {
            return;
        }

        _pendingCardHoverPreviewIndex = -1;
        ShowCardHoverPreviewNow(index);
    }

    private void ShowCardHoverPreviewNow(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        CancelPendingCardHoverPreview(index);

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        _cardHoverPreviewActive[index] = true;
        card.HoverHint.Visible = index != _liftedCardIndex;
        card.ShowSkillPreview();
        UpdateCardPreviewTriggeredBuffHighlights(index, GetHandSkill(index));
        UpdateCardFootMarkerHover(index, GetHandSkill(index)?.OwnerCharater as PlayerCharacter);
    }

    private void CancelPendingCardHoverPreview(int index = -1)
    {
        if (index >= 0 && _pendingCardHoverPreviewIndex != index)
            return;

        if (_pendingCardHoverPreviewIndex == -1)
            return;

        _pendingCardHoverPreviewIndex = -1;
        _cardHoverPreviewRequestVersion++;
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
        CancelPendingCardHoverPreview();
        ClearCardEnergyPreview();
        ClearCardFootMarkerHover();
        ClearCardPreviewTriggeredBuffHighlights();
        for (int i = 0; i < _cards.Length; i++)
            HideCardHoverPreview(i);
    }

    private void HideAllCardHoverPreviewsExcept(int preservedIndex)
    {
        if (_pendingCardHoverPreviewIndex != preservedIndex)
            CancelPendingCardHoverPreview();
        ClearCardEnergyPreview();
        if (_cardFootMarkerHoverIndex != preservedIndex)
            ClearCardFootMarkerHover();
        if (_cardPreviewHighlightedBuffsIndex != preservedIndex)
            ClearCardPreviewTriggeredBuffHighlights();

        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == preservedIndex)
                continue;

            HideCardHoverPreview(i);
        }
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

        CancelPendingCardHoverPreview(index);

        if (_cardPreviewHighlightedBuffsIndex == index)
            ClearCardPreviewTriggeredBuffHighlights(index);

        if (!_cardHoverPreviewActive[index])
            return;

        if (_cardFootMarkerHoverIndex == index)
            ClearCardFootMarkerHover();

        _cardHoverPreviewActive[index] = false;

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
            card.HideSkillPreview();
    }

    private void UpdateCardPreviewTriggeredBuffHighlights(int index, Skill skill)
    {
        if (!IsCardIndexValid(index) || !_cardHoverPreviewActive[index])
        {
            ClearCardPreviewTriggeredBuffHighlights(index);
            return;
        }

        if (_cardPreviewHighlightedBuffsIndex != -1 && _cardPreviewHighlightedBuffsIndex != index)
            ClearCardPreviewTriggeredBuffHighlights();

        var nextHighlights = new HashSet<Buff>(
            CardPreviewBuffHighlighter.FindTriggeredBuffs(skill)
        );
        foreach (Buff buff in _cardPreviewHighlightedBuffs)
        {
            if (!nextHighlights.Contains(buff))
                buff.SetCardPreviewHighlight(false);
        }
        foreach (Buff buff in nextHighlights)
        {
            if (!_cardPreviewHighlightedBuffs.Contains(buff))
                buff.SetCardPreviewHighlight(true);
        }

        _cardPreviewHighlightedBuffs.Clear();
        _cardPreviewHighlightedBuffs.UnionWith(nextHighlights);
        _cardPreviewHighlightedBuffsIndex =
            _cardPreviewHighlightedBuffs.Count > 0 ? index : -1;
    }

    private void ClearCardPreviewTriggeredBuffHighlights(int index = -1)
    {
        if (index >= 0 && _cardPreviewHighlightedBuffsIndex != index)
            return;

        foreach (Buff buff in _cardPreviewHighlightedBuffs)
            buff.SetCardPreviewHighlight(false);

        _cardPreviewHighlightedBuffs.Clear();
        _cardPreviewHighlightedBuffsIndex = -1;
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
        if (_isDiscardSelectionActive && _discardSelectionOriginalVisualHandIndexes.Contains(index))
        {
            card.SetCenteredPointerHoverState(false, instant);
            card.HideHoverUi();
            ApplyHandCardLayer(index, GetHandOrderForSlotIndex(index));
            return;
        }

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

        while (_queuedCardPlays.Count > 0)
        {
            QueuedCardPlay play = _queuedCardPlays.Dequeue();
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            RestoreHandSlotForQueuedPlay(play);
            if (resetCards && play != null && play.IsHandCard)
                ResetCardMotion(play.Index, instant: true);
        }

        while (_queuedFollowUpCardPlays.Count > 0)
        {
            QueuedCardPlay play = _queuedFollowUpCardPlays.Dequeue();
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            RestoreHandSlotForQueuedPlay(play);
        }

        _skillRemovalBuffer.Clear();
        foreach (Skill skill in _queuedCardSkills)
            _skillRemovalBuffer.Add(skill);
        for (int i = 0; i < _skillRemovalBuffer.Count; i++)
        {
            Skill skill = _skillRemovalBuffer[i];
            int index = FindCurrentHandSkillIndex(skill);
            if (resetCards && IsCardIndexValid(index))
                ResetCardMotion(index, instant: true);
        }
        _queuedCardSkills.Clear();
        _skillRemovalBuffer.Clear();
    }

    private bool IsCardCommitted(int index)
    {
        Skill skill = GetHandSkill(index);
        return skill != null && _queuedCardSkills.Contains(skill);
    }

    private bool IsAnyCardDrawEntryBusy()
    {
        return _hiddenPendingDrawEntrySlotIndexes.Count > 0
            || _pendingDrawEntryAnimations.Count > 0
            || _drawEntryPreviewCards.Count > 0
            || _drawEntrySlotIndexes.Count > 0;
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

    private bool IsCardDrawEntryInputBlocked(int index)
    {
        return IsCardIndexValid(index)
            && (
                _hiddenPendingDrawEntrySlotIndexes.Contains(index)
                || _pendingDrawEntryAnimations.Contains(index)
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

            Rect2 slotRect = GetControlViewportAabb(slot);
            if (slotRect.Size == Vector2.Zero)
                continue;
            handRect = handRect.HasValue ? MergeRect(handRect.Value, slotRect) : slotRect;
        }

        if (!handRect.HasValue)
        {
            if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
                return true;

            handRect = GetControlViewportAabb(_cardRow);
        }

        return !handRect.Value.Grow(HandAreaPadding).HasPoint(GetHandCardPointerPosition());
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
