using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void LayoutActionCards()
    {
        LayoutActionCards(instant: false);
    }

    private void LayoutActionCards(bool instant)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        if (_manualTargetArrowSelectionActive)
            return;

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        int handCount = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard: true
        );
        float rowWidth = Math.Max(_cardRow.Size.X, 0f);
        float rowHeight = Math.Max(_cardRow.Size.Y, cardSize.Y);
        float cardY = GetHandCardY(rowHeight, cardSize);
        Vector2 hiddenPosition = new(-cardSize.X * 2f, cardY);
        BuildHandLayoutTargetPositions(
            _visibleHandSlotIndexesBuffer,
            handCount,
            cardSize,
            rowWidth,
            rowHeight
        );
        Array.Fill(_handLayoutOrderBySlotIndex, -1);
        for (int order = 0; order < handCount; order++)
            _handLayoutOrderBySlotIndex[_visibleHandSlotIndexesBuffer[order]] = order;

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            if (_turnEndStatusTriggerCardIndexes.Contains(i))
                continue;

            bool isDetachedFromHand =
                IsCardDetachedFromHandLayout(i)
                || IsCardCommitted(i);
            bool isWaitingForDrawEntry = IsHandCardWaitingForDrawEntry(i);
            slot.Size = cardSize;
            slot.CustomMinimumSize = cardSize;
            slot.PivotOffsetRatio = Vector2.Zero;
            slot.PivotOffset = cardSize * 0.5f;
            bool preservesHandLayer = _discardSelectionOriginalVisualHandIndexes.Contains(i);
            if (!preservesHandLayer)
            {
                if (_handLayoutOrderBySlotIndex[i] >= 0)
                    ApplyHandCardLayer(i, _handLayoutOrderBySlotIndex[i]);
                else
                    ApplyHandCardLayer(i, 0);
            }

            if (isWaitingForDrawEntry)
            {
                Vector2 entryTargetPosition = _handLayoutTargetPositionValid[i]
                    ? _handLayoutTargetPositions[i]
                    : GetPendingDrawEntryLandingPosition(
                        i,
                        cardSize,
                        rowWidth,
                        rowHeight,
                        out _
                    );
                float entryTargetRotation = _handLayoutTargetPositionValid[i]
                    ? _handLayoutTargetRotations[i]
                    : 0f;
                MoveCardSlotTo(
                    i,
                    entryTargetPosition,
                    entryTargetRotation,
                    instant || !_layoutInitialized,
                    true
                );
                continue;
            }

            if (isDetachedFromHand)
            {
                _cardSlotLayoutTweens[i]?.Kill();
                _cardSlotLayoutTweens[i] = null;
                _cardSlotLayoutTargets[i] = null;
                _cardSlotLayoutRotationTargets[i] = null;
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                ClearDrawEntryArc(i);
                ClearDrawEntryState(i, revealCard: true);
                continue;
            }

            Vector2 targetPosition = _handLayoutTargetPositionValid[i]
                ? _handLayoutTargetPositions[i]
                : hiddenPosition;
            float targetRotation = _handLayoutTargetPositionValid[i]
                ? _handLayoutTargetRotations[i]
                : 0f;
            if (_discardSelectionFlyingReturnHandIndexes.Contains(i))
            {
                _cardSlotLayoutTargets[i] = targetPosition;
                _cardSlotLayoutRotationTargets[i] = targetRotation;
                if (!_cardSlotLayoutFollowActive[i])
                {
                    _cardSlotLayoutFollowActive[i] = true;
                    UpdateProcessState();
                }
                continue;
            }

            bool drawEntry = _drawEntrySlotIndexes.Contains(i);
            MoveCardSlotTo(i, targetPosition, targetRotation, instant || !_layoutInitialized, drawEntry);
        }

        _layoutInitialized = true;
    }

    private void BuildHandLayoutTargetPositions(
        int[] visibleSlotIndexes,
        int handCount,
        Vector2 cardSize,
        float rowWidth,
        float rowHeight
    )
    {
        Array.Fill(_handLayoutTargetPositionValid, false);
        Array.Fill(_handLayoutTargetRotations, 0f);
        if (visibleSlotIndexes == null || handCount <= 0)
            return;

        float cardStep = HandLayout.GetCardStep(handCount, cardSize, rowWidth);
        float totalWidth =
            handCount > 1 ? cardSize.X + cardStep * (handCount - 1) : handCount * cardSize.X;
        float x = (rowWidth - totalWidth) / 2f;
        float cardY = GetHandCardY(rowHeight, cardSize);
        int hoveredOrder = FindVisibleHandOrder(visibleSlotIndexes, handCount, _hoveredCardIndex);
        float overlapWidth = Math.Max(0f, cardSize.X - cardStep);
        // A baseline push keeps hover noticeable even when cards barely overlap.
        float hoverSpread = Mathf.Clamp(
            HandCardHoverSpreadBase + overlapWidth * HandCardHoverSpreadRatio,
            0f,
            HandCardMaxHoverSpread
        );
        for (int order = 0; order < handCount; order++)
        {
            int slotIndex = visibleSlotIndexes[order];
            float targetX = x + cardStep * order;
            if (hoveredOrder != -1 && order != hoveredOrder)
            {
                int distance = Math.Abs(order - hoveredOrder);
                float distanceFalloff = 1f / distance;
                targetX += Math.Sign(order - hoveredOrder) * hoverSpread * distanceFalloff;
            }

            _handLayoutTargetPositionValid[slotIndex] = true;
            _handLayoutTargetPositions[slotIndex] = new Vector2(targetX, cardY);
        }
    }

    private static int FindVisibleHandOrder(int[] visibleSlotIndexes, int handCount, int slotIndex)
    {
        if (visibleSlotIndexes == null || slotIndex < 0)
            return -1;

        int count = Math.Min(handCount, visibleSlotIndexes.Length);
        for (int order = 0; order < count; order++)
        {
            if (visibleSlotIndexes[order] == slotIndex)
                return order;
        }

        return -1;
    }

    private float GetHandCardY(float rowHeight, Vector2 cardSize)
    {
        return Math.Max(0f, rowHeight - cardSize.Y) + HandCardYOffset;
    }

    private void UpdateHandLayoutFollowers(float delta)
    {
        if (delta <= 0f || _cardSlots == null)
            return;

        bool hasActiveFollower = false;
        float followRatio = 1f - Mathf.Exp(-HandLayoutFollowSharpness * delta);
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            if (
                !_cardSlotLayoutFollowActive[i]
                || !_cardSlotLayoutTargets[i].HasValue
                || !_cardSlotLayoutRotationTargets[i].HasValue
            )
            {
                continue;
            }

            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
            {
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                ClearDrawEntryArc(i);
                continue;
            }

            Vector2 targetPosition = _cardSlotLayoutTargets[i].Value;
            float targetRotation = _cardSlotLayoutRotationTargets[i].Value;
            float distance = slot.Position.DistanceTo(targetPosition);
            float rotationDistance = Math.Abs(slot.Rotation - targetRotation);
            if (
                distance <= HandLayoutFollowSnapDistance
                && rotationDistance <= HandLayoutFollowSnapRotation
            )
            {
                slot.Position = targetPosition;
                slot.Rotation = targetRotation;
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                continue;
            }

            bool hasSpeedOverride = _cardSlotLayoutPixelsPerSecondOverrides[i] > 0f;
            float pixelsPerSecond = hasSpeedOverride
                ? _cardSlotLayoutPixelsPerSecondOverrides[i]
                : HandLayoutPixelsPerSecond;
            float maxMove = pixelsPerSecond * delta;
            float move = Math.Min(
                maxMove,
                hasSpeedOverride
                    ? distance
                    : Math.Max(HandLayoutFollowSnapDistance, distance * followRatio)
            );
            bool drawEntryFollower =
                _drawEntryPreviewCards.ContainsKey(i)
                || _drawEntrySlotIndexes.Contains(i)
                || _discardSelectionFlyingReturnHandIndexes.Contains(i);
            slot.Position = drawEntryFollower
                ? slot.Position.Lerp(targetPosition, followRatio)
                : slot.Position.MoveToward(targetPosition, move);
            slot.Rotation = Mathf.Lerp(slot.Rotation, targetRotation, followRatio);
            hasActiveFollower = true;
        }

        if (!hasActiveFollower)
            UpdateProcessState();
    }

    private void ClearDrawEntryArc(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        _drawEntryArcActive[index] = false;
        _drawEntryArcElapsed[index] = 0f;
        _drawEntryArcDurations[index] = 0f;
        _drawEntryArcVelocities[index] = Vector2.Zero;
        _drawEntryArcOvershootPositions[index] = Vector2.Zero;
        _drawEntryArcTargetPositions[index] = Vector2.Zero;
    }

    private static Vector2 CubicBezier(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        float t
    )
    {
        float inv = 1f - t;
        return a * (inv * inv * inv)
            + b * (3f * inv * inv * t)
            + c * (3f * inv * t * t)
            + d * (t * t * t);
    }

    private void UpdateProcessState()
    {
        SetProcess(
            _liftedCardIndex != -1
                || IsAnyHandLayoutFollowerActive()
                || _pileOverlaySmoothScrollActive
                || _isDiscardSelectionActive
        );
    }

    private bool IsAnyHandLayoutFollowerActive()
    {
        for (int i = 0; i < _cardSlotLayoutFollowActive.Length; i++)
        {
            if (_cardSlotLayoutFollowActive[i])
                return true;
        }

        return false;
    }

    private void ApplyHandCardLayer(int index, int handOrder)
    {
        if (!IsCardIndexValid(index))
            return;

        Control slot = _cardSlots[index];
        SkillCard card = _cards[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        int layer = handOrder + 1;
        if (index == _hoveredCardIndex)
            layer = HandCardHoverZIndex;
        else if (index == _liftedCardIndex || IsCardCommitted(index))
            layer = Math.Max(slot.ZIndex, PlayedCardZIndex);
        slot.ZIndex = layer;
        if (card != null && GodotObject.IsInstanceValid(card))
            card.ZIndex = layer;
    }

    private List<int> BuildHandCardInputPriorityOrder()
    {
        int visibleCount = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard: true
        );
        List<int> ordered = _handCardInputPriorityOrderBuffer;
        ordered.Clear();
        for (int i = 0; i < visibleCount; i++)
        {
            int index = _visibleHandSlotIndexesBuffer[i];
            ordered.Add(index);
        }

        if (_hoveredCardIndex >= 0 && ordered.Remove(_hoveredCardIndex))
            ordered.Add(_hoveredCardIndex);

        return ordered;
    }

    private int GetHandCardInputInsertIndex()
    {
        if (
            _handInputBlocker != null
            && GodotObject.IsInstanceValid(_handInputBlocker)
            && _handInputBlocker.GetParent() == _cardRow
        )
        {
            return _handInputBlocker.GetIndex();
        }

        return _cardRow?.GetChildCount() ?? 0;
    }

    private void SyncHandCardInputOrder()
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        List<int> orderedVisible = BuildHandCardInputPriorityOrder();
        if (orderedVisible.Count == 0)
            return;

        foreach (int slotIndex in orderedVisible)
        {
            Control slot = _cardSlots[slotIndex];
            if (slot == null || !GodotObject.IsInstanceValid(slot) || slot.GetParent() != _cardRow)
                continue;

            int insertIndex = GetHandCardInputInsertIndex();
            if (slot.GetIndex() < insertIndex)
                insertIndex--;

            insertIndex = Math.Clamp(insertIndex, 0, Math.Max(0, _cardRow.GetChildCount() - 1));
            _cardRow.MoveChild(slot, insertIndex);
        }
    }

    private void ApplyHandCardHoverInputOrder()
    {
        SyncHandCardInputOrder();
        ScheduleCardHoverRefresh();
    }

    private int GetHandOrderForSlotIndex(int index)
    {
        int visibleCount = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard: true
        );
        for (int order = 0; order < visibleCount; order++)
        {
            if (_visibleHandSlotIndexesBuffer[order] == index)
                return order;
        }

        return Math.Max(0, index);
    }

    private void MoveCardSlotTo(
        int index,
        Vector2 targetPosition,
        float targetRotation,
        bool instant,
        bool drawEntry = false,
        float layoutDelay = 0f
    )
    {
        if (index < 0 || index >= _cardSlots.Length)
            return;

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        if (drawEntry)
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = targetPosition;
            _cardSlotLayoutRotationTargets[index] = targetRotation;
            slot.Scale = Vector2.One;

            if (_drawEntryPreviewCards.ContainsKey(index))
            {
                _cardSlotLayoutFollowActive[index] = true;
                UpdateProcessState();
            }
            else if (!_pendingDrawEntryAnimations.Contains(index))
            {
                StartDrawEntryPreviewAnimation(index, targetPosition, targetPosition, layoutDelay);
            }

            return;
        }

        if (
            instant
            || (
                slot.Position.DistanceSquaredTo(targetPosition) < 0.25f
                && Math.Abs(slot.Rotation - targetRotation) < 0.002f
            )
        )
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = targetPosition;
            _cardSlotLayoutRotationTargets[index] = targetRotation;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
            ClearDrawEntryArc(index);
            ClearDrawEntryState(index, revealCard: true);
            CancelDrawEntryPreview(index);
            slot.Position = targetPosition;
            slot.Rotation = targetRotation;
            slot.Scale = Vector2.One;
            return;
        }

        if (
            _cardSlotLayoutTargets[index].HasValue
            && _cardSlotLayoutTargets[index].Value.DistanceSquaredTo(targetPosition) < 0.25f
            && _cardSlotLayoutRotationTargets[index].HasValue
            && Math.Abs(_cardSlotLayoutRotationTargets[index].Value - targetRotation) < 0.002f
            && (_cardSlotLayoutTweens[index] != null || _cardSlotLayoutFollowActive[index])
        )
        {
            return;
        }

        if (_cardSlotLayoutTweens[index] != null)
        {
            _cardSlotLayoutTweens[index]?.Kill();
            HideCardDrawEntryTrail(index);
        }

        CancelDrawEntryPreview(index);
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = targetPosition;
        _cardSlotLayoutRotationTargets[index] = targetRotation;
        _cardSlotLayoutFollowActive[index] = true;
        ClearDrawEntryArc(index);
        slot.Scale = Vector2.One;
        UpdateProcessState();
    }

    private static float GetHandLayoutTweenDuration(Vector2 startPosition, Vector2 targetPosition)
    {
        float distance = startPosition.DistanceTo(targetPosition);
        return Mathf.Clamp(
            distance / HandLayoutPixelsPerSecond,
            HandLayoutMinTweenDuration,
            HandLayoutMaxTweenDuration
        );
    }

    private float GetDrawEntryDelay(int index)
    {
        return _drawEntrySlotOrders.TryGetValue(index, out int order)
            ? GetDrawEntryDelayForOrder(order, GetDrawEntryBatchCount())
            : 0f;
    }

    private int GetDrawEntryBatchCount() => Math.Max(1, _drawEntrySlotOrders.Count);

    private static float GetDrawEntryTweenDuration(
        Vector2 startPosition,
        Vector2 targetPosition,
        int batchCount
    )
    {
        float distance = startPosition.DistanceTo(targetPosition);
        float duration = Mathf.Clamp(
            distance / HandDrawEntryPixelsPerSecond,
            HandDrawEntryMinTweenDuration,
            HandDrawEntryMaxTweenDuration
        );

        if (batchCount >= HandDrawEntryCompactDurationThreshold)
            duration *= HandDrawEntryManyCardDurationScale;

        return Mathf.Max(0.16f, duration);
    }

    private static float GetDrawEntryDelayForOrder(int order, int batchCount)
    {
        order = Math.Max(0, order);
        batchCount = Math.Max(1, batchCount);
        if (order == 0 || batchCount <= 1)
            return 0f;

        float dynamicStagger = Math.Min(
            HandDrawEntryStagger,
            HandDrawEntryMaxTotalStaggerDuration / Math.Max(1, batchCount - 1)
        );
        dynamicStagger = Mathf.Clamp(dynamicStagger, HandDrawEntryMinStagger, HandDrawEntryStagger);
        return order * dynamicStagger;
    }

    private void StartDrawEntryPreviewAnimation(
        int index,
        Vector2 targetPosition,
        Vector2 entryTarget,
        float layoutDelay
    )
    {
        if (!IsCardIndexValid(index))
            return;

        int version = ++_drawEntryAnimationVersions[index];
        _pendingDrawEntryAnimations.Add(index);
        _ = PlayDrawEntryPreviewAnimationAsync(
            index,
            targetPosition,
            entryTarget,
            layoutDelay,
            version
        );
    }


    private async Task PlayDrawEntryPreviewAnimationAsync(
        int index,
        Vector2 targetPosition,
        Vector2 entryTarget,
        float layoutDelay,
        int version
    )
    {
        float delay = GetDrawEntryDelay(index) + GetPendingShuffleDrawEntryDelay();
        delay = Math.Max(delay, layoutDelay);
        if (delay > 0f && IsInsideTree())
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

        if (!CanStartDrawEntryAnimation(index, version))
            return;

        Vector2 currentTargetPosition = GetDrawEntryTargetSlotPosition(index, targetPosition);
        float currentTargetRotation = GetDrawEntryTargetSlotRotation(
            index,
            _handLayoutTargetPositionValid[index] ? _handLayoutTargetRotations[index] : 0f
        );
        SkillCard movingCard = ActivateDrawEntryCardInHandSlot(
            index,
            currentTargetPosition,
            currentTargetRotation
        );
        if (movingCard == null)
        {
            _pendingDrawEntryAnimations.Remove(index);
            FinishDrawEntryAnimation(index, version, revealCard: true);
            return;
        }

        _pendingDrawEntryAnimations.Remove(index);
        // The card becomes interactable as soon as its staggered entry starts. Rebuild the
        // playable state now instead of waiting for the card to reach its final hand position.
        RequestTurnUiRefresh();

        if (!CanContinueDrawEntryAnimation(index, version, movingCard))
            return;

        if (!_drawEntryFromPlayedCardOrigin.Contains(index))
            PulsePileButtonReceive(_drawPileButton);
        movingCard.Visible = true;
        movingCard.Modulate = Colors.White;

        Vector2 startCenter = GetDrawEntryStartCenter(index);
        Vector2 targetCenter = GetDrawEntryTargetCenter(index, currentTargetPosition);

        bool enableTrailParticles = _drawEntrySlotIndexes.Count <= 4;
        PrepareDrawEntryPreviewTrail(
            movingCard,
            out Line trail,
            out GpuParticles2D particles,
            enableTrailParticles
        );
        UpdateTrailParticlesRotation(particles, targetCenter - startCenter);

        await WaitForDrawEntrySlotArrivalAsync(index, version, movingCard, particles);
        if (!IsInsideTree() || !CanContinueDrawEntryAnimation(index, version, movingCard))
            return;

        FinishDrawEntrySlotArrival(index, movingCard);
        _ = FadeAndHideDrawEntryPreviewTrailAsync(trail, particles);
        if (
            _drawEntryPreviewCards.TryGetValue(index, out SkillCard currentPreview)
            && currentPreview == movingCard
        )
        {
            _drawEntryPreviewCards.Remove(index);
        }
        FinishDrawEntryAnimation(index, version, revealCard: false);
        ScheduleCardHoverRefresh();
    }

    private SkillCard ActivateDrawEntryCardInHandSlot(
        int index,
        Vector2 targetPosition,
        float targetRotation
    )
    {
        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && index < hand.Length ? hand[index] : null;
        if (skill == null)
            return null;

        CancelDrawEntryPreview(index, invalidateAnimation: false);

        SkillCard card = _cards[index];
        Control slot = _cardSlots[index];
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || slot == null
            || !GodotObject.IsInstanceValid(slot)
        )
        {
            return null;
        }

        Node oldParent = card.GetParent();
        if (oldParent != slot)
        {
            oldParent?.RemoveChild(card);
            slot.AddChild(card);
        }

        slot.ClipContents = false;
        slot.Position = GetDrawEntryStartSlotPosition(index);
        slot.Rotation = GetDrawEntryStartRotation(index);
        slot.Scale = Vector2.One;

        card.RestoreDisplayState();
        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Stop;
        SetCardButtonInputEnabled(card, true);
        card.HoverHint.Visible = false;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Scale = BattleCardScale;
        card.Rotation = 0f;
        card.Position = Vector2.Zero;
        ApplyHandCardLayer(index, GetHandOrderForSlotIndex(index));
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _cardSlotLayoutTargets[index] = targetPosition;
        _cardSlotLayoutRotationTargets[index] = targetRotation;
        _cardSlotLayoutFollowActive[index] = true;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = HandDrawEntryPixelsPerSecond;
        UpdateProcessState();
        _drawEntryPreviewCards[index] = card;
        return card;
    }

    private async Task WaitForDrawEntrySlotArrivalAsync(
        int index,
        int version,
        SkillCard movingCard,
        GpuParticles2D particles
    )
    {
        Vector2 previousCenter =
            movingCard.GetGlobalTransformWithCanvas() * movingCard.PivotOffset;
        while (
            CanContinueDrawEntryAnimation(index, version, movingCard)
            && _cardSlotLayoutFollowActive[index]
            && IsInsideTree()
        )
        {
            Vector2 currentCenter =
                movingCard.GetGlobalTransformWithCanvas() * movingCard.PivotOffset;
            Vector2 targetCenter = GetDrawEntryTargetCenter(
                index,
                GetDrawEntryTargetSlotPosition(index, Vector2.Zero)
            );
            Vector2 velocity = currentCenter - previousCenter;
            UpdateTrailParticlesRotation(
                particles,
                velocity.LengthSquared() > 0.01f ? velocity : targetCenter - currentCenter
            );
            previousCenter = currentCenter;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void DetachDrawEntryForInteraction(int index, SkillCard card)
    {
        if (
            !IsCardIndexValid(index)
            || card == null
            || !GodotObject.IsInstanceValid(card)
            || !_drawEntryPreviewCards.TryGetValue(index, out SkillCard previewCard)
            || !ReferenceEquals(previewCard, card)
        )
        {
            return;
        }

        _drawEntryAnimationVersions[index]++;
        _drawEntryPreviewCards.Remove(index);
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryState(index, revealCard: true, hideTrail: true);
    }

    private void FinishDrawEntrySlotArrival(int index, SkillCard card)
    {
        if (!IsCardIndexValid(index) || card == null || !GodotObject.IsInstanceValid(card))
            return;

        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
        {
            slot.Position = GetDrawEntryTargetSlotPosition(index, slot.Position);
            slot.Rotation = GetDrawEntryTargetSlotRotation(
                index,
                _handLayoutTargetPositionValid[index] ? _handLayoutTargetRotations[index] : slot.Rotation
            );
            slot.Scale = Vector2.One;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
            ClearDrawEntryArc(index);
        }

        card.Rotation = 0f;
        bool preserveHoverMotion =
            _hoveredCardIndex == index
            && _liftedCardIndex == -1
            && CanHoverHandCardAt(index);
        if (!preserveHoverMotion)
        {
            card.Position = Vector2.Zero;
            card.Scale = BattleCardScale;
        }
    }

    private async Task ReturnDrawEntryCardToSlotSmoothAsync(int index, SkillCard card)
    {
        if (!ReturnDrawEntryCardToSlot(index, card, preserveGlobalCenter: true))
            return;

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        float distance = card.Position.DistanceTo(Vector2.Zero);
        if (distance <= 0.5f || !card.IsInsideTree())
        {
            card.Position = Vector2.Zero;
            return;
        }

        float duration = Mathf.Clamp(distance / HandDrawEntryPixelsPerSecond, 0.045f, 0.12f);
        Tween settleTween = card.CreateTween();
        settleTween
            .TweenProperty(card, "position", Vector2.Zero, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(settleTween, Tween.SignalName.Finished);

        if (card != null && GodotObject.IsInstanceValid(card))
            card.Position = Vector2.Zero;
    }

    private bool ReturnDrawEntryCardToSlot(
        int index,
        SkillCard card,
        bool preserveGlobalCenter = false
    )
    {
        if (
            !IsCardIndexValid(index)
            || card == null
            || !GodotObject.IsInstanceValid(card)
            || _cardSlots[index] == null
            || !GodotObject.IsInstanceValid(_cardSlots[index])
        )
        {
            return false;
        }

        Vector2 globalCenter = preserveGlobalCenter
            ? card.GetGlobalTransformWithCanvas() * card.PivotOffset
            : Vector2.Zero;
        Node parent = card.GetParent();
        parent?.RemoveChild(card);
        _cardSlots[index].AddChild(card);
        card.Position = Vector2.Zero;
        card.Rotation = 0f;
        card.Scale = BattleCardScale;
        if (preserveGlobalCenter)
            SetCardPivotCenterAt(card, globalCenter);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Stop;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        _cards[index] = card;
        return true;
    }

    private int GetDrawEntryOrder(int index) =>
        _drawEntrySlotOrders.TryGetValue(index, out int order) ? order : 0;

    private Vector2 GetDrawEntryStartCenter()
    {
        if (
            _drawPileButton != null
            && GodotObject.IsInstanceValid(_drawPileButton)
            && _drawPileButton.IsInsideTree()
        )
        {
            return GetPileButtonVisualCenter(_drawPileButton);
        }

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        return GetCardRowGlobalPosition(new Vector2(-cardSize.X * 0.85f, 0f)) + cardSize * 0.5f;
    }

    private Vector2 GetDrawEntryStartCenter(int index)
    {
        return _drawEntryStartCenters.TryGetValue(index, out Vector2 center)
            ? center
            : GetDrawEntryStartCenter();
    }

    private Vector2 GetDrawEntryStartCenterForOrder(int order)
    {
        Vector2 baseCenter = GetDrawEntryStartCenter();
        if (order <= 0)
            return baseCenter;

        float side = (order & 1) == 0 ? -1f : 1f;
        float x = side * (14f + (order % 3) * 9f);
        float y = -Math.Min(40f, 8f + order * 4f) + Mathf.Sin(order * 1.21f) * 6f;
        return baseCenter + new Vector2(x, y);
    }

    private float GetDrawEntryStartRotation(int index)
    {
        int order = GetDrawEntryOrder(index);
        float side = (order & 1) == 0 ? -1f : 1f;
        return Mathf.DegToRad(side * (4f + (order % 3) * 2.5f));
    }

    private Vector2 GetDrawEntryStartSlotPosition(int index)
    {
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        return GetCardRowLocalPosition(GetDrawEntryStartCenter(index)) - cardSize * 0.5f;
    }

    private Vector2 GetDrawEntryTargetSlotPosition(int index, Vector2 fallbackTargetPosition)
    {
        return IsCardIndexValid(index) && _cardSlotLayoutTargets[index].HasValue
            ? _cardSlotLayoutTargets[index].Value
            : fallbackTargetPosition;
    }

    private float GetDrawEntryTargetSlotRotation(int index, float fallbackTargetRotation = 0f)
    {
        return IsCardIndexValid(index) && _cardSlotLayoutRotationTargets[index].HasValue
            ? _cardSlotLayoutRotationTargets[index].Value
            : fallbackTargetRotation;
    }

    private Vector2 GetDrawEntryTargetCenter(int index, Vector2 fallbackTargetPosition)
    {
        Vector2 landingOffset = new(0f, HandDrawEntryLandingYOffset);
        Vector2 targetPosition = GetDrawEntryTargetSlotPosition(index, fallbackTargetPosition);
        return GetCardRowGlobalPosition(targetPosition)
            + BattleCardBaseSize * BattleCardScale * 0.5f
            + landingOffset;
    }

    private Vector2 GetCardRowGlobalPosition(Vector2 localPosition)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return localPosition;

        return _cardRow.GetGlobalTransformWithCanvas() * localPosition;
    }

    private Vector2 GetCardRowLocalPosition(Vector2 globalPosition)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return globalPosition;

        return _cardRow.GetGlobalTransformWithCanvas().AffineInverse() * globalPosition;
    }

    private void FinishDrawEntryAnimation(int index, int version, bool revealCard)
    {
        if (!IsCardIndexValid(index) || _drawEntryAnimationVersions[index] != version)
            return;

        ClearDrawEntryState(index, revealCard, hideTrail: true);
        if (!IsAnyCardDrawEntryBusy())
        {
            // The pointer can already be over the final card when its Button changes from
            // disabled/ignored to interactive. Godot does not guarantee a new MouseEntered
            // event in that case, so finish the batch with a synchronous input-state rebuild
            // and force the stationary-pointer validator to run once more.
            _handHoverValidationPointerInitialized = false;
            RefreshTurnUi();
            ScheduleCardHoverRefresh();
            return;
        }

        RequestTurnUiRefresh(refreshHover: true);
    }

    private bool CanContinueDrawEntryAnimation(int index, int version, SkillCard previewCard)
    {
        return IsCardIndexValid(index)
            && _drawEntryAnimationVersions[index] == version
            && _drawEntrySlotIndexes.Contains(index)
            && previewCard != null
            && GodotObject.IsInstanceValid(previewCard);
    }

    private bool CanStartDrawEntryAnimation(int index, int version)
    {
        return IsCardIndexValid(index)
            && _drawEntryAnimationVersions[index] == version
            && _drawEntrySlotIndexes.Contains(index);
    }

    private void CancelDrawEntryPreview(int index, bool invalidateAnimation = true)
    {
        if (!IsCardIndexValid(index))
            return;

        if (invalidateAnimation)
            _drawEntryAnimationVersions[index]++;
        _pendingDrawEntryAnimations.Remove(index);
        if (!_drawEntryPreviewCards.TryGetValue(index, out SkillCard previewCard))
            return;

        _drawEntryPreviewCards.Remove(index);
        if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
            ReturnDrawEntryCardToSlot(index, previewCard);
    }

    private float GetPendingShuffleDrawEntryDelay()
    {
        return GetRemainingShuffleDrawEntryDelay();
    }

    private float GetRemainingShuffleDrawEntryDelay()
    {
        if (_shuffleDrawEntryDelayUntilMsec == 0)
            return 0f;

        ulong now = Time.GetTicksMsec();
        if (now >= _shuffleDrawEntryDelayUntilMsec)
        {
            _shuffleDrawEntryDelayUntilMsec = 0;
            return 0f;
        }

        return (_shuffleDrawEntryDelayUntilMsec - now) / 1000f;
    }

    private bool ShouldDelayHandLayoutForShuffleDraw()
    {
        return _hiddenPendingDrawEntrySlotIndexes.Count > 0
            && GetRemainingShuffleDrawEntryDelay() > 0f;
    }

    private void ScheduleHandLayoutAfterShuffleDrawDelay()
    {
        float delay = GetRemainingShuffleDrawEntryDelay();
        if (delay <= 0f || !IsInsideTree())
            return;

        int version = ++_shuffleDelayedLayoutRefreshVersion;
        _ = RefreshTurnUiAfterShuffleDrawDelayAsync(version, delay);
    }

    private async Task RefreshTurnUiAfterShuffleDrawDelayAsync(int version, float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        if (version != _shuffleDelayedLayoutRefreshVersion || !IsInsideTree())
            return;

        RequestTurnUiRefresh(refreshHover: true);
    }

    private int[] GetVisibleHandSlotIndexes(bool excludeLiftedCard = false)
    {
        int count = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard
        );
        if (count <= 0)
            return Array.Empty<int>();

        int[] indexes = new int[count];
        Array.Copy(_visibleHandSlotIndexesBuffer, indexes, count);
        return indexes;
    }

    private int FillVisibleHandSlotIndexes(int[] buffer, bool excludeLiftedCard = false)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || buffer == null || buffer.Length == 0)
            return 0;

        int max = Math.Min(hand.Length, _cards.Length);
        int count = 0;
        for (int i = 0; i < max; i++)
        {
            if (excludeLiftedCard && IsCardDetachedFromHandLayout(i))
                continue;
            if (_turnEndStatusTriggerCardIndexes.Contains(i))
                continue;
            if (hand[i] != null)
                buffer[count++] = i;
        }

        return count;
    }

    private bool IsHandCardWaitingForDrawEntry(int index)
    {
        return IsCardIndexValid(index)
            && (
                _hiddenPendingDrawEntrySlotIndexes.Contains(index)
                || _pendingDrawEntryAnimations.Contains(index)
            );
    }

    private Vector2 GetPendingDrawEntryLandingPosition(
        int entryIndex,
        Vector2 cardSize,
        float rowWidth,
        float rowHeight
    )
    {
        return GetPendingDrawEntryLandingPosition(
            entryIndex,
            cardSize,
            rowWidth,
            rowHeight,
            out _
        );
    }

    private Vector2 GetPendingDrawEntryLandingPosition(
        int entryIndex,
        Vector2 cardSize,
        float rowWidth,
        float rowHeight,
        out float targetRotation
    )
    {
        targetRotation = 0f;
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return new Vector2((rowWidth - cardSize.X) * 0.5f, GetHandCardY(rowHeight, cardSize));

        int max = Math.Min(hand.Length, _cards.Length);
        int entryOrder = -1;
        int logicalCount = 0;
        for (int i = 0; i < max; i++)
        {
            if (
                hand[i] == null
                || _turnEndStatusTriggerCardIndexes.Contains(i)
                || IsCardDetachedFromHandLayout(i)
            )
            {
                continue;
            }

            if (i == entryIndex)
                entryOrder = logicalCount;
            logicalCount++;
        }

        if (entryOrder < 0 || logicalCount <= 0)
            return new Vector2((rowWidth - cardSize.X) * 0.5f, GetHandCardY(rowHeight, cardSize));

        float cardStep = HandLayout.GetCardStep(logicalCount, cardSize, rowWidth);
        float totalWidth =
            logicalCount > 1
                ? cardSize.X + cardStep * (logicalCount - 1)
                : cardSize.X;
        float x = (rowWidth - totalWidth) * 0.5f + cardStep * entryOrder;
        return new Vector2(x, GetHandCardY(rowHeight, cardSize));
    }

    private bool IsCardDetachedFromHandLayout(int index)
    {
        if (!IsCardIndexValid(index))
            return false;

        return index == _liftedCardIndex
            || _discardSelectionOriginalVisualHandIndexes.Contains(index)
            || _discardSelectionFlyingToDiscardHandIndexes.Contains(index)
            || (
                _manualTargetArrowSelectionActive
                && index == _manualTargetArrowCardIndex
            );
    }

    private void CaptureCardSlotPositions(Vector2?[] positions)
    {
        if (positions == null)
            return;

        Array.Clear(positions, 0, positions.Length);
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            positions[i] = slot.Position;
        }
    }

    private void CaptureCardSlotRotations(float?[] rotations)
    {
        if (rotations == null)
            return;

        Array.Clear(rotations, 0, rotations.Length);
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            rotations[i] = slot.Rotation;
        }
    }

    private bool IsDiscardSelectionReturnSlot(int index) =>
        _discardSelectionReturningHandIndexes.Contains(index)
        || _discardSelectionFlyingReturnHandIndexes.Contains(index);

    private bool TryDetachDrawEntryStateForHandReorder(int index, out Vector2 startPosition)
    {
        startPosition = Vector2.Zero;
        if (
            !IsCardIndexValid(index)
            || !IsCardDrawEntryBusy(index)
            || _cardSlots[index] == null
            || !GodotObject.IsInstanceValid(_cardSlots[index])
        )
        {
            return false;
        }

        SkillCard card = _cards[index];
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        if (card != null && GodotObject.IsInstanceValid(card) && card.Visible)
        {
            Vector2 globalCenter = card.GetGlobalTransformWithCanvas() * card.PivotOffset;
            startPosition = GetCardRowLocalPosition(globalCenter) - cardSize * 0.5f;
        }
        else if (_hiddenPendingDrawEntrySlotIndexes.Contains(index))
        {
            startPosition = GetDrawEntryStartSlotPosition(index);
        }
        else if (_cardSlotLayoutTargets[index].HasValue)
        {
            startPosition = _cardSlotLayoutTargets[index].Value;
        }
        else
        {
            startPosition = _cardSlots[index].Position;
        }

        CancelDrawEntryPreview(index);
        ClearDrawEntryState(index, revealCard: true);
        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryArc(index);
        _cardSlots[index].Position = startPosition;
        _cardSlots[index].Rotation = 0f;
        _cardSlots[index].Scale = Vector2.One;

        if (card != null && GodotObject.IsInstanceValid(card))
        {
            if (card.GetParent() != _cardSlots[index])
            {
                card.GetParent()?.RemoveChild(card);
                _cardSlots[index].AddChild(card);
            }

            card.Position = Vector2.Zero;
            card.Rotation = 0f;
            card.Scale = BattleCardScale;
            card.Visible = true;
            card.HoverHint.Visible = false;
        }

        startPosition = NormalizeHandReorderStartPosition(startPosition);

        return true;
    }

    private void PrepareNewHandCardSlotForDrawEntry(int index)
    {
        if (IsDiscardSelectionReturnSlot(index))
            return;

        if (
            index < 0
            || index >= _cardSlots.Length
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return;
        }

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        int drawOrder = _drawEntrySlotOrders.Count;
        if (!_drawEntryStartCenters.ContainsKey(index))
        {
            if (
                _customDrawEntryStartPositions.TryGetValue(index, out Vector2 customStartPosition)
            )
            {
                _drawEntryStartCenters[index] =
                    GetCardRowGlobalPosition(customStartPosition) + cardSize * 0.5f;
                _customDrawEntryStartPositions.Remove(index);
            }
            else
            {
                _drawEntryStartCenters[index] = GetDrawEntryStartCenter();
            }
        }
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryArc(index);
        _drawEntrySlotIndexes.Add(index);
        _drawEntrySlotOrders[index] = drawOrder;
        _hiddenPendingDrawEntrySlotIndexes.Add(index);

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
        {
            card.Visible = false;
            SetCardButtonInputEnabled(card, false);
            card.HoverHint.Visible = false;
        }
    }

    private void PrepareMovedHandCardSlotFromPreviousPosition(
        int index,
        int previousIndex,
        Vector2?[] previousSlotPositions,
        float?[] previousSlotRotations
    )
    {
        if (
            index < 0
            || index >= _cardSlots.Length
            || previousIndex < 0
            || previousSlotPositions == null
            || previousIndex >= previousSlotPositions.Length
            || !previousSlotPositions[previousIndex].HasValue
        )
        {
            return;
        }

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryArc(index);
        CancelDrawEntryPreview(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        slot.Scale = Vector2.One;
        slot.Position = NormalizeHandReorderStartPosition(
            previousSlotPositions[previousIndex].Value
        );
        slot.Rotation = previousSlotRotations != null
            && previousIndex < previousSlotRotations.Length
            && previousSlotRotations[previousIndex].HasValue
                ? previousSlotRotations[previousIndex].Value
                : 0f;
    }

    private bool PreparePendingHandReorderMove(int index, Skill skill)
    {
        if (
            skill == null
            || !_pendingHandReorderStarts.TryGetValue(skill, out var start)
            || index < 0
            || index >= _cardSlots.Length
        )
        {
            return false;
        }

        _pendingHandReorderStarts.Remove(skill);

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return false;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        ClearDrawEntryArc(index);
        CancelDrawEntryPreview(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        slot.Scale = Vector2.One;
        slot.Position = start.Position;
        slot.Rotation = start.Rotation;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = start.PixelsPerSecond;
        return true;
    }

    private Vector2 NormalizeHandReorderStartPosition(Vector2 position)
    {
        position.Y = GetCurrentHandCardY();
        return position;
    }

    private float GetCurrentHandCardY()
    {
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        float rowHeight = Math.Max(
            _cardRow != null && GodotObject.IsInstanceValid(_cardRow)
                ? _cardRow.Size.Y
                : cardSize.Y,
            cardSize.Y
        );
        return GetHandCardY(rowHeight, cardSize);
    }

    private void ClearDrawEntryState(int index, bool revealCard, bool hideTrail = true)
    {
        if (!IsCardIndexValid(index))
            return;

        if (hideTrail)
            HideCardDrawEntryTrail(index);

        bool wasHidden = _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _pendingDrawEntryAnimations.Remove(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _drawEntryFromPlayedCardOrigin.Remove(index);
        ClearDrawEntryArc(index);
        if (_cardSlots[index] != null && GodotObject.IsInstanceValid(_cardSlots[index]))
            _cardSlots[index].Scale = Vector2.One;

        if (!revealCard || !wasHidden)
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Visible = true;
    }

    private void HideCardDrawEntryTrail(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        HideCardDrawEntryTrail(_cards[index]);
    }

    private static void HideCardDrawEntryTrail(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Line trail = card.DrawTrail;
        if (trail != null && GodotObject.IsInstanceValid(trail))
        {
            trail.Visible = false;
            trail.ClearPoints();
            trail.Modulate = Colors.White;
            trail.Width = HandDrawTrailWidth;
            trail.ManualPreviewMode = false;
            if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                trail.Target.Visible = false;
        }

        HideCardTrailParticles(card.DrawTrailParticles);
    }

    private static void PrepareDrawEntryPreviewTrail(
        SkillCard card,
        out Line trail,
        out GpuParticles2D particles,
        bool enableParticles = true
    )
    {
        trail = null;
        particles = null;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Node2D target = card.DrawTrailTarget;
        trail = card.DrawTrail;
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || trail == null
            || !GodotObject.IsInstanceValid(trail)
        )
        {
            return;
        }

        target.Visible = true;
        target.Position = card.PivotOffset;
        trail.Target = target;
        trail.ManualPreviewMode = false;
        trail.Visible = true;
        trail.GlobalPosition = Vector2.Zero;
        trail.Modulate = Colors.White;
        trail.Width = HandDrawTrailWidth;
        trail.ClearPoints();

        if (!enableParticles)
            return;

        particles = card.DrawTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        particles.Emitting = true;
    }

    private async Task FadeAndHideDrawEntryPreviewTrailAsync(Line trail, GpuParticles2D particles)
    {
        if (particles != null && GodotObject.IsInstanceValid(particles))
            particles.Emitting = false;

        if (trail == null || !GodotObject.IsInstanceValid(trail))
        {
            HideCardTrailParticles(particles);
            return;
        }

        trail.ManualPreviewMode = true;
        float startWidth = trail.Width;
        Tween tween = trail.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(fade =>
            {
                if (trail == null || !GodotObject.IsInstanceValid(trail))
                    return;

                trail.Modulate = new Color(1f, 1f, 1f, 1f - fade);
                trail.Width = Mathf.Lerp(startWidth, 0.5f, fade);
            }),
            0f,
            1f,
            HandDrawTrailFadeDuration
        );
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (trail != null && GodotObject.IsInstanceValid(trail))
                {
                    trail.Visible = false;
                    trail.ClearPoints();
                    trail.Modulate = Colors.White;
                    trail.Width = HandDrawTrailWidth;
                    if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                        trail.Target.Visible = false;
                }

                HideCardTrailParticles(particles);
            })
        );

        await ToSignal(tween, Tween.SignalName.Finished);
    }


}
