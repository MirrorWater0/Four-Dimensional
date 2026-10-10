using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private const float DiscardSelectionMouseEnableProgress = 1f;

    private async Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (IsCardButtonPressSuppressed() && !allowSuppressedPress)
            return;

        if (_isDiscardSelectionActive)
        {
            await HandleDiscardSelectionCardPressedAsync(index);
            return;
        }

        if (
            _endTurnQueued
            || _isPileCardSelectionActive
            || _handLayoutSyncPending
            || IsHandInputBlockedByOverlay()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= (GetActiveHandSkills()?.Length ?? 0)
            || IsCardCommitted(index)
            || IsCardDrawEntryInputBlocked(index)
        )
        {
            return;
        }

        Skill skill = GetActiveHandSkills()?[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return;

        // The logical hand can compact one frame before the visual slots are
        // remapped. Never execute a card from a stale visual index during
        // that window; let the deferred hand refresh repair the mapping first.
        SkillCard visualCard = _cards[index];
        if (
            visualCard != null
            && GodotObject.IsInstanceValid(visualCard)
            && visualCard.CurrentSkill != null
            && !ReferenceEquals(visualCard.CurrentSkill, skill)
        )
        {
            RequestTurnUiRefresh(refreshHover: true);
            return;
        }

        if (_liftedCardIndex != -1 && _liftedCardIndex != index)
            return;

        bool shouldUseManualTargetArrowSelection =
            ShouldUseManualTargetArrowSelection(skill) && HasManualFriendlyTargetCandidates(skill);
        if (shouldUseManualTargetArrowSelection)
        {
            await SelectManualTargetFromHandAndQueueAsync(index, skill);
            return;
        }

        if (_liftedCardIndex == index)
        {
            if (!IsMouseOutsideHandArea())
            {
                return;
            }

            QueueCardPlay(index, skill);
            return;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: false);

        LiftCard(index);
    }

    private Task HandleHandCardIndexShortcutAsync(int index)
    {
        if (_isDiscardSelectionActive)
        {
            return HandleDiscardSelectionCardPressedAsync(index);
        }

        if (
            _endTurnQueued
            || _isPileCardSelectionActive
            || _handLayoutSyncPending
            || IsHandInputBlockedByOverlay()
            || _manualTargetArrowSelectionActive
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= (GetActiveHandSkills()?.Length ?? 0)
            || IsCardDrawEntryInputBlocked(index)
        )
        {
            return Task.CompletedTask;
        }

        Skill skill = GetActiveHandSkills()?[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return Task.CompletedTask;

        SkillCard visualCard = _cards[index];
        if (
            visualCard != null
            && GodotObject.IsInstanceValid(visualCard)
            && visualCard.CurrentSkill != null
            && !ReferenceEquals(visualCard.CurrentSkill, skill)
        )
        {
            RequestTurnUiRefresh(refreshHover: true);
            return Task.CompletedTask;
        }

        if (_liftedCardIndex == index)
        {
            RequestTurnUiRefresh(refreshHover: true);
            return Task.CompletedTask;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: true);

        if (ShouldUseManualTargetArrowSelection(skill) && HasManualFriendlyTargetCandidates(skill))
            return SelectManualTargetFromHandAndQueueAsync(index, skill);

        LiftCard(index);
        return Task.CompletedTask;
    }

    private async Task HandleDiscardSelectionCardPressedAsync(int index)
    {
        if (
            !_isDiscardSelectionActive
            || _isDiscardSelectionCompleting
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
            || IsCardDrawEntryInputBlocked(index)
            || IsDiscardSelectionReturnSlot(index)
        )
        {
            return;
        }

        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && index < hand.Length ? hand[index] : null;
        if (skill == null)
            return;

        if (ConsumeDuplicateDiscardSelectionInput(index))
            return;

        ClearLiftedCard(instant: false);

        if (_discardSelectionSkills.Contains(skill))
        {
            _ = MoveDiscardSelectionCardBackAsync(skill, animateBack: true);
            ArrangeDiscardSelectionSelectedCards();
            RequestTurnUiRefresh(refreshHover: true);
            return;
        }

        if (_discardSelectionSkills.Count >= _discardSelectionTargetCount)
            return;

        AddDiscardSelectionSelectedCard(index, skill);
        ArrangeDiscardSelectionSelectedCards();
        RequestTurnUiRefresh(refreshHover: true);
        await Task.CompletedTask;
    }

    private bool ConsumeDuplicateDiscardSelectionInput(int index)
    {
        ulong now = Time.GetTicksMsec();
        if (
            _lastDiscardSelectionInputIndex == index
            && now >= _lastDiscardSelectionInputMsec
            && now - _lastDiscardSelectionInputMsec <= 50UL
        )
        {
            return true;
        }

        _lastDiscardSelectionInputIndex = index;
        _lastDiscardSelectionInputMsec = now;
        return false;
    }

    private void ResetDiscardSelectionInputGuard()
    {
        _lastDiscardSelectionInputIndex = -1;
        _lastDiscardSelectionInputMsec = 0UL;
    }

    private async Task CompleteDiscardSelectionAsync()
    {
        if (!_isDiscardSelectionActive || _isDiscardSelectionCompleting)
            return;

        _isDiscardSelectionCompleting = true;
        DisableDiscardSelectionCardInputForResolution();
        HideDiscardSelectionScreenMask();
        if (!_discardSelectionExhaustMode)
            RequestTurnUiRefresh();

        int selectedCount = _discardSelectionTransformMode
            ? await TransformSelectedHandCardsAsync()
            : _discardSelectionKeywordMode
                ? await ApplyKeywordToSelectedHandCardsAsync()
                : _discardSelectionExhaustMode
                    ? await ExhaustSelectedHandCardsAsync()
                    : await DiscardSelectedHandCardsAsync();
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        bool returnSelectedCards =
            _discardSelectionKeywordMode || _discardSelectionTransformMode;
        _isDiscardSelectionActive = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionKeywordMode = false;
        _discardSelectionTransformMode = false;
        _discardSelectionTransformSkillId = SkillID.None;
        _discardSelectionTargetCount = 0;
        _discardSelectionCompletion = null;
        ResetDiscardSelectionInputGuard();
        if (returnSelectedCards)
            await ReturnDiscardSelectionCardsAsync();
        else
            ClearDiscardSelectionVisualState(returnCards: false);
        _isDiscardSelectionCompleting = false;
        RequestTurnUiRefresh(refreshHover: true);
        completion?.TrySetResult(selectedCount);
    }

    private void DisableDiscardSelectionCardInputForResolution()
    {
        foreach (SkillCard card in _discardSelectionCards.Values)
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            // The exhaust preview is cloned from this exact visual state. Input is already
            // rejected while the selection is completing, so do not mutate the source card
            // before cloning it; disabling hover/input can emit MouseExited and reset scale.
            if (_discardSelectionExhaustMode)
                continue;

            KillDiscardSelectionArrangeTween(card);
            SetDiscardSelectionCardMouseDetection(card, enabled: false);
        }
    }

    private static void SetDiscardSelectionCardMouseDetection(SkillCard card, bool enabled)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.SetHoverUiEnabled(enabled);
        if (!enabled)
        {
            card.HideHoverUi();
            card.HoverHint.Visible = false;
        }

        card.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        card.Button.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        card.Button.Disabled = !enabled;
    }

    private void CancelDiscardSelection()
    {
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        _isDiscardSelectionActive = false;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionKeywordMode = false;
        _discardSelectionTransformMode = false;
        _discardSelectionTransformSkillId = SkillID.None;
        _discardSelectionTargetCount = 0;
        _discardSelectionCompletion = null;
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionReturnAnimationState();
        ClearDiscardSelectionVisualState(returnCards: true);
        HideDiscardSelectionScreenMask();
        completion?.TrySetResult(0);
    }

    private void AddDiscardSelectionSelectedCard(int index, Skill skill)
    {
        if (skill == null || !IsCardIndexValid(index))
            return;

        _discardSelectionOriginalHandIndexes[skill] = index;
        if (!_discardSelectionSkills.Contains(skill))
            _discardSelectionSkills.Add(skill);

        SkillCard handCard = _cards[index];
        if (handCard == null || !GodotObject.IsInstanceValid(handCard))
            return;

        DetachDrawEntryForInteraction(index, handCard);
        _discardSelectionCards[skill] = handCard;
        _discardSelectionOriginalVisualHandIndexes.Add(index);

        ClearDiscardSelectionHandSlotPreview(index);

        SkillCard card = _discardSelectionCards[skill];
        card.StopBattleMotion();
        card.HoverHint.Visible = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.PointerHoverScaleMultiplier = CardHoverScaleMultiplier;
        card.ZIndex = DiscardSelectionSelectedCardZIndex + _discardSelectionSkills.Count;
    }

    private void ClearDiscardSelectionHandSlotPreview(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        SkillCard handCard = _cards[index];
        if (handCard == null || !GodotObject.IsInstanceValid(handCard))
            return;

        SnapHandCardToBaseVisual(index, handCard);
        handCard.HideHoverUi();
        handCard.HoverHint.Visible = false;
        handCard.StopBattleMotion();
        handCard.Modulate = SkillButton.EnabledModulate;
        bool keepsOriginalHandVisual = _discardSelectionOriginalVisualHandIndexes.Contains(index);
        handCard.Visible = keepsOriginalHandVisual;
        SetCardButtonInputEnabled(handCard, keepsOriginalHandVisual);
    }

    private SkillCard CreateDiscardSelectionActionPreviewCard(Skill skill, SkillCard sourceCard)
    {
        Control overlay = EnsureDiscardSelectionOverlay();
        Control sourceVisual = sourceCard?.CardVisualScaleHost;
        if (
            skill == null
            || sourceCard == null
            || !GodotObject.IsInstanceValid(sourceCard)
            || sourceVisual == null
            || !GodotObject.IsInstanceValid(sourceVisual)
            || overlay == null
            || !GodotObject.IsInstanceValid(overlay)
        )
        {
            return null;
        }

        // The selected hand card is rendered by VisualTransform while its root remains
        // in the original hand slot. Transfer the rendered transform by its centre,
        // rather than its top-left position, so the overlay preview does not shift when
        // its own pivot and scale are applied.
        Vector2 visualGlobalCenter =
            sourceVisual.GetGlobalTransformWithCanvas() * sourceVisual.PivotOffset;
        Vector2 previewScale = sourceCard.GetCenteredVisualTransformGlobalScale();
        float globalRotation = sourceCard.GetCenteredVisualTransformGlobalRotation();
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "DiscardSelectionActionCard";
        card.ConfigureDisplayScale(previewScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.PreviewCharacterName = skill.OwnerCharater?.CharacterName;
        card.PreviewCharacterKey = (skill.OwnerCharater as PlayerCharacter)?.CharacterKey;
        overlay.AddChild(card);

        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.Visible = true;
        card.Scale = previewScale;
        card.Rotation = globalRotation;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        SetCardPivotCenterAt(card, visualGlobalCenter);
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        return card;
    }

    private void ArrangeDiscardSelectionSelectedCards()
    {
        int count = _discardSelectionSkills.Count;
        Vector2 selectedScale = GetDiscardSelectionCardScale();
        for (int order = 0; order < count; order++)
        {
            Skill skill = _discardSelectionSkills[order];
            if (skill == null || !_discardSelectionCards.TryGetValue(skill, out SkillCard card))
                continue;
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            Vector2 targetScale = selectedScale;
            Vector2 targetPosition = GetDiscardSelectionSelectedPosition(
                selectedScale,
                order,
                count
            );
            card.Modulate = SkillButton.EnabledModulate;

            int handIndex = _discardSelectionOriginalHandIndexes[skill];
            bool usesOriginalHandVisual = _discardSelectionOriginalVisualHandIndexes.Contains(
                handIndex
            );
            if (HasDiscardSelectionArrangeTarget(
                card,
                targetPosition,
                targetScale,
                usesOriginalHandVisual
            ))
            {
                continue;
            }

            if (
                _discardSelectionArrangeTweens.TryGetValue(card, out Tween existingTween)
                && existingTween != null
                && existingTween.IsValid()
            )
            {
                existingTween.Kill();
            }

            // The card root must remain in its hand slot. Move and scale the shared
            // VisualTransform instead of the CanvasGroup so this path uses the same centred
            // scale host as every other card interaction.
            CanvasItem movingVisual = usesOriginalHandVisual
                ? card.CardVisualScaleHost
                : card;
            if (movingVisual == null || !GodotObject.IsInstanceValid(movingVisual))
                continue;

            // Original hand cards keep their root in the hand slot, so their Button follows
            // VisualTransform safely while the selection layout tween runs. Do not disable
            // hover/input here: RefreshTurnUi re-enters this method, and repeatedly clearing
            // HoverUi made pointer hover wait for the full 0.32-second arrange animation.
            SetDiscardSelectionCardMouseDetection(card, enabled: usesOriginalHandVisual);

            if (ReferenceEquals(movingVisual, card))
            {
                card.ZIndex = DiscardSelectionSelectedCardZIndex + order;
                card.HoverHint.Visible = false;
            }
            else
            {
                card.SetHoverUiEnabled(true);
                ApplyHandCardLayer(
                    handIndex,
                    GetHandOrderForSlotIndex(handIndex)
                );
            }

            Tween tween;
            if (usesOriginalHandVisual)
            {
                Vector2 targetCenter = targetPosition + BattleCardBaseSize * targetScale * 0.5f;
                tween = card.TweenCenteredVisualTransformLayout(
                    card.GetCenteredVisualTransformLocalPosition(targetCenter),
                    targetScale,
                    CardPlayMoveDuration,
                    Tween.TransitionType.Cubic,
                    Tween.EaseType.Out
                );
            }
            else
            {
                tween = movingVisual.CreateTween();
                tween.SetParallel(true);
                tween
                    .TweenProperty(movingVisual, "global_position", targetPosition, CardPlayMoveDuration)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                tween
                    .TweenProperty(movingVisual, "scale", targetScale, CardPlayMoveDuration)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
            }
            _discardSelectionArrangeTweens[card] = tween;
            _discardSelectionArrangeTargets[card] = (
                targetPosition,
                targetScale,
                usesOriginalHandVisual
            );
            tween
                .TweenCallback(
                    Callable.From(
                        () =>
                            TryEnableDiscardSelectionCardMouseDetection(skill, card, tween)
                    )
                )
                .SetDelay(CardPlayMoveDuration * DiscardSelectionMouseEnableProgress);
            tween.Finished += () =>
            {
                if (
                    _discardSelectionArrangeTweens.TryGetValue(card, out Tween currentTween)
                    && ReferenceEquals(currentTween, tween)
                )
                {
                    TryEnableDiscardSelectionCardMouseDetection(skill, card, tween);
                    _discardSelectionArrangeTweens.Remove(card);
                }
            };
        }
    }

    private bool HasDiscardSelectionArrangeTarget(
        SkillCard card,
        Vector2 position,
        Vector2 scale,
        bool usesOriginalHandVisual
    )
    {
        if (
            card == null
            || !_discardSelectionArrangeTargets.TryGetValue(card, out var target)
            || target.UsesOriginalHandVisual != usesOriginalHandVisual
        )
        {
            return false;
        }

        return target.Position.DistanceSquaredTo(position) < 0.25f
            && target.Scale.DistanceSquaredTo(scale) < 0.0001f;
    }

    private void TryEnableDiscardSelectionCardMouseDetection(
        Skill skill,
        SkillCard card,
        Tween tween
    )
    {
        if (
            !_isDiscardSelectionActive
            || _isDiscardSelectionCompleting
            || !_discardSelectionArrangeTweens.TryGetValue(card, out Tween currentTween)
            || !ReferenceEquals(currentTween, tween)
            || !_discardSelectionCards.TryGetValue(skill, out SkillCard selectedCard)
            || !ReferenceEquals(selectedCard, card)
            || !_discardSelectionSkills.Contains(skill)
        )
        {
            return;
        }

        SetDiscardSelectionCardMouseDetection(card, enabled: true);
    }

    private Vector2 GetDiscardSelectionSelectedPosition(Vector2 scale, int order, int count)
    {
        count = Math.Max(1, count);
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scaledSize = BattleCardBaseSize * scale;
        float availableWidth = Math.Max(0f, viewportSize.X - scaledSize.X - 96f);
        float step =
            count <= 1
                ? 0f
                : Math.Min(
                    scaledSize.X + DiscardSelectionSelectedGap,
                    Math.Min(DiscardSelectionSelectedMaxStep, availableWidth / (count - 1))
                );
        float x = viewportSize.X * 0.5f - scaledSize.X * 0.5f + (order - (count - 1) * 0.5f) * step;
        float y =
            viewportSize.Y * 0.5f - scaledSize.Y * 0.5f + DiscardSelectionSelectedVerticalOffset;
        return new Vector2(x, y);
    }

    private Vector2 GetDiscardSelectionCardScale()
    {
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            Rect2 rect = slot.GetGlobalRect();
            if (rect.Size.X <= 1f || rect.Size.Y <= 1f)
                continue;

            return new Vector2(
                rect.Size.X / BattleCardBaseSize.X,
                rect.Size.Y / BattleCardBaseSize.Y
            );
        }

        return BattleCardScale;
    }

    private Task MoveDiscardSelectionCardBackAsync(Skill skill, bool animateBack)
    {
        if (skill == null)
            return Task.CompletedTask;

        _discardSelectionCards.TryGetValue(skill, out SkillCard previewCard);
        _discardSelectionCards.Remove(skill);
        int originalIndex = _discardSelectionOriginalHandIndexes.TryGetValue(
            skill,
            out int storedOriginalIndex
        )
            ? storedOriginalIndex
            : -1;
        bool usesOriginalHandVisual =
            originalIndex >= 0
            && _discardSelectionOriginalVisualHandIndexes.Contains(originalIndex);

        int restoredIndex = -1;
        if (_discardSelectionSkills.Remove(skill))
            restoredIndex = RestoreActiveHandSkillFromDiscardSelection(skill);
        _discardSelectionOriginalHandIndexes.Remove(skill);

        if (!animateBack || restoredIndex < 0)
        {
            if (usesOriginalHandVisual)
            {
                KillDiscardSelectionArrangeTween(previewCard);
                ResetOriginalSelectionCardVisual(previewCard);
                _discardSelectionOriginalVisualHandIndexes.Remove(originalIndex);
                return Task.CompletedTask;
            }

            if (restoredIndex >= 0)
                _discardSelectionReturningHandIndexes.Add(restoredIndex);

            if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
            {
                KillDiscardSelectionArrangeTween(previewCard);
                previewCard.QueueFree();
            }
            return Task.CompletedTask;
        }

        if (previewCard == null || !GodotObject.IsInstanceValid(previewCard))
        {
            RequestTurnUiRefresh(refreshHover: true);
            return Task.CompletedTask;
        }

        _discardSelectionReturningHandIndexes.Add(restoredIndex);
        _discardSelectionFlyingReturnHandIndexes.Add(restoredIndex);
        if (usesOriginalHandVisual)
        {
            RequestTurnUiRefresh();
            return AnimateOriginalSelectionCardReturnAsync(previewCard, restoredIndex);
        }

        _discardSelectionReturningPreviewCards[restoredIndex] = previewCard;
        RequestTurnUiRefresh();
        return AnimateDiscardSelectionPreviewReturnAsync(previewCard, restoredIndex);
    }

    private async Task AnimateOriginalSelectionCardReturnAsync(SkillCard card, int handIndex)
    {
        try
        {
            if (
                card == null
                || !GodotObject.IsInstanceValid(card)
                || !IsCardIndexValid(handIndex)
                || card.CardVisualScaleHost == null
                || !GodotObject.IsInstanceValid(card.CardVisualScaleHost)
            )
            {
                return;
            }

            KillDiscardSelectionArrangeTween(card);
            Control visual = card.CardVisualScaleHost;
            card.SetHoverUiEnabled(false);
            Vector2 visualGlobalCenter = visual.GetGlobalTransformWithCanvas() * visual.PivotOffset;
            Vector2 visualGlobalScale = card.GetCenteredVisualTransformGlobalScale();
            float visualGlobalRotation = card.GetCenteredVisualTransformGlobalRotation();
            SnapHandCardToBaseVisual(handIndex, card);
            _discardSelectionOriginalVisualHandIndexes.Remove(handIndex);
            LayoutActionCards(instant: false);
            card.TweenCenteredVisualTransformLayout(
                card.GetCenteredVisualTransformLocalPosition(visualGlobalCenter),
                visualGlobalScale,
                0f,
                instant: true,
                transition: Tween.TransitionType.Cubic,
                ease: Tween.EaseType.Out
            );
            card.SetCenteredVisualTransformGlobalRotation(visualGlobalRotation);
            card.Button.Disabled = true;
            card.HoverHint.Visible = false;
            card.MouseFilter = MouseFilterEnum.Ignore;
            card.Button.MouseFilter = MouseFilterEnum.Ignore;
            ApplyHandCardLayer(handIndex, GetHandOrderForSlotIndex(handIndex));

            Tween returnTween = card.TweenCenteredScale(
                Vector2.One,
                CardPlayMoveDuration,
                Tween.TransitionType.Cubic,
                Tween.EaseType.Out
            );
            _discardSelectionArrangeTweens[card] = returnTween;
            returnTween
                .TweenProperty(visual, "position", Vector2.Zero, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            returnTween
                .TweenProperty(visual, "rotation", 0f, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);

            await ToSignal(
                GetTree().CreateTimer(CardPlayMoveDuration),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            ResetOriginalSelectionCardVisual(card);
            _discardSelectionOriginalVisualHandIndexes.Remove(handIndex);
            _discardSelectionFlyingReturnHandIndexes.Remove(handIndex);
            _discardSelectionReturningHandIndexes.Remove(handIndex);
            if (IsCardIndexValid(handIndex))
                ApplyHandCardLayer(handIndex, GetHandOrderForSlotIndex(handIndex));
            if (IsInsideTree())
                RequestTurnUiRefresh(refreshHover: true);
        }
    }

    private void ResetOriginalSelectionCardVisual(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        KillDiscardSelectionArrangeTween(card);
        Control visual = card.CardVisualScaleHost;
        if (visual != null && GodotObject.IsInstanceValid(visual))
            card.ResetCenteredVisualTransformLayout();

        card.Visible = true;
        card.Modulate = SkillButton.EnabledModulate;
        card.PointerHoverScaleMultiplier = 1.08f;
        card.SetHoverUiEnabled(true);
        card.MouseFilter = MouseFilterEnum.Stop;
        card.Button.MouseFilter = MouseFilterEnum.Stop;
        card.Button.Disabled = false;
        card.HoverHint.Visible = false;
    }

    private async Task AnimateDiscardSelectionPreviewReturnAsync(
        SkillCard previewCard,
        int targetIndex
    )
    {
        SkillCard movingCard = null;
        try
        {
            if (
                !IsInsideTree()
                || previewCard == null
                || !GodotObject.IsInstanceValid(previewCard)
                || !IsCardIndexValid(targetIndex)
            )
            {
                return;
            }

            Control slot = _cardSlots[targetIndex];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                return;

            movingCard = _cards[targetIndex];
            if (movingCard == null || !GodotObject.IsInstanceValid(movingCard))
                return;

            Vector2 startGlobal = previewCard.GlobalPosition;
            Vector2 startScale = previewCard.Scale;
            if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
            {
                KillDiscardSelectionArrangeTween(previewCard);
                previewCard.QueueFree();
            }
            previewCard = null;
            _discardSelectionReturningPreviewCards.Remove(targetIndex);

            LayoutActionCards(instant: false);
            Vector2? targetPosition = _cardSlotLayoutTargets[targetIndex];
            if (!targetPosition.HasValue)
            {
                LayoutActionCards(instant: true);
                targetPosition = _cardSlotLayoutTargets[targetIndex];
            }
            if (!targetPosition.HasValue)
                return;

            _cardSlotLayoutTweens[targetIndex]?.Kill();
            _cardSlotLayoutTweens[targetIndex] = null;
            _cardSlotLayoutFollowActive[targetIndex] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[targetIndex] = 0f;

            Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
            slot.Position = GetCardRowLocalPosition(startGlobal + cardSize * 0.5f) - cardSize * 0.5f;
            slot.Rotation = 0f;
            slot.Scale = Vector2.One;

            Node oldParent = movingCard.GetParent();
            if (oldParent != slot)
            {
                oldParent?.RemoveChild(movingCard);
                slot.AddChild(movingCard);
            }

            Skill[] hand = GetActiveHandSkills();
            Skill skill = hand != null && targetIndex < hand.Length ? hand[targetIndex] : null;
            movingCard.RestoreDisplayState();
            if (skill != null)
            {
                movingCard.SetSkill(skill);
                movingCard.CharacterName.Text = GetSkillOwnerDisplayName(skill);
                movingCard.SetEnergyCostAffordable(skill.CanUseCurrentEnergy());
            }
            movingCard.StopBattleMotion();
            movingCard.Position = Vector2.Zero;
            movingCard.Scale = startScale;
            movingCard.Rotation = 0f;
            movingCard.PivotOffset = BattleCardBaseSize * 0.5f;
            movingCard.Visible = true;
            movingCard.Modulate = SkillButton.EnabledModulate;
            movingCard.HoverHint.Visible = false;
            movingCard.MouseFilter = MouseFilterEnum.Ignore;
            movingCard.Button.MouseFilter = MouseFilterEnum.Ignore;
            movingCard.Button.Disabled = true;
            movingCard.ZIndex = DiscardSelectionSelectedCardZIndex;
            ApplyHandCardLayer(targetIndex, GetHandOrderForSlotIndex(targetIndex));

            _cardSlotLayoutTargets[targetIndex] = targetPosition;
            _cardSlotLayoutRotationTargets[targetIndex] = 0f;
            _cardSlotLayoutFollowActive[targetIndex] = false;

            float moveDuration = Math.Min(
                GetHandLayoutTweenDuration(slot.Position, targetPosition.Value),
                CardPlayMoveDuration
            );
            Tween returnTween = slot.CreateTween();
            _cardSlotLayoutTweens[targetIndex] = returnTween;
            returnTween.SetParallel(true);
            returnTween
                .TweenProperty(slot, "position", targetPosition.Value, moveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            returnTween
                .TweenProperty(movingCard, "scale", BattleCardScale, moveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);

            await ToSignal(
                GetTree().CreateTimer(moveDuration),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            _discardSelectionReturningPreviewCards.Remove(targetIndex);
            FinishDiscardSelectionReturnArrival(targetIndex, movingCard);
            _discardSelectionFlyingReturnHandIndexes.Remove(targetIndex);
            _discardSelectionReturningHandIndexes.Remove(targetIndex);
            RevealDiscardSelectionReturnedHandCard(targetIndex);
            if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
            {
                KillDiscardSelectionArrangeTween(previewCard);
                previewCard.QueueFree();
            }
            if (IsInsideTree())
                RequestTurnUiRefresh(refreshHover: true);
        }
    }

    private void FinishDiscardSelectionReturnArrival(int index, SkillCard card)
    {
        if (!IsCardIndexValid(index))
            return;

        CancelDrawEntryPreview(index);
        ClearDrawEntryState(index, revealCard: false);

        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
        {
            if (_cardSlotLayoutTargets[index].HasValue)
            {
                slot.Position = _cardSlotLayoutTargets[index].Value;
                slot.Rotation = _cardSlotLayoutRotationTargets[index] ?? 0f;
            }
            slot.Scale = Vector2.One;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        }

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Position = Vector2.Zero;
        card.Rotation = 0f;
        card.Scale = BattleCardScale;
    }

    private void RevealDiscardSelectionReturnedHandCard(int targetIndex)
    {
        if (!IsCardIndexValid(targetIndex))
            return;

        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && targetIndex < hand.Length ? hand[targetIndex] : null;
        SkillCard handCard = _cards[targetIndex];
        Control slot = _cardSlots[targetIndex];
        if (
            skill == null
            || handCard == null
            || !GodotObject.IsInstanceValid(handCard)
            || slot == null
            || !GodotObject.IsInstanceValid(slot)
        )
        {
            return;
        }

        _cardSlotLayoutTweens[targetIndex]?.Kill();
        _cardSlotLayoutTweens[targetIndex] = null;
        _cardSlotLayoutFollowActive[targetIndex] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[targetIndex] = 0f;

        if (handCard.GetParent() != slot)
        {
            handCard.GetParent()?.RemoveChild(handCard);
            slot.AddChild(handCard);
        }

        handCard.Position = Vector2.Zero;
        handCard.Rotation = 0f;
        handCard.Scale = BattleCardScale;
        handCard.PivotOffset = BattleCardBaseSize * 0.5f;
        handCard.Visible = true;
        handCard.Modulate = SkillButton.EnabledModulate;
        handCard.SetSkill(skill);
        handCard.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        handCard.SetEnergyCostAffordable(skill.CanUseCurrentEnergy());
        handCard.SetPlayableHighlight(false, instant: true);
        handCard.SetHoverUiEnabled(true);
        handCard.HoverHint.Visible = false;
        handCard.MouseFilter = MouseFilterEnum.Stop;
        handCard.Button.MouseFilter = MouseFilterEnum.Stop;
        ApplyHandCardLayer(targetIndex, GetHandOrderForSlotIndex(targetIndex));
        SetCardButtonInputEnabled(
            handCard,
            _isDiscardSelectionActive
                && !_isDiscardSelectionCompleting
                && !IsCardDrawEntryInputBlocked(targetIndex)
        );
    }

    private int RestoreActiveHandSkillFromDiscardSelection(Skill skill)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || skill == null)
            return -1;

        int existingIndex = Array.IndexOf(hand, skill);
        if (existingIndex >= 0)
            return existingIndex;

        int restoreIndex = -1;
        if (
            _discardSelectionOriginalHandIndexes.TryGetValue(skill, out int originalIndex)
            && originalIndex >= 0
            && originalIndex < hand.Length
            && hand[originalIndex] == null
        )
        {
            restoreIndex = originalIndex;
        }
        else
        {
            restoreIndex = CompactHandInPlace(hand);
        }

        if (restoreIndex < 0)
            return -1;

        if (skill.OwnerCharater == null || !GodotObject.IsInstanceValid(skill.OwnerCharater))
            skill.OwnerCharater = _activePlayer;
        skill.UpdateDescription();
        hand[restoreIndex] = skill;
        InvalidateDiscardSelectionSkillOwner(skill);
        return restoreIndex;
    }

    private static int CompactHandInPlace(Skill[] hand)
    {
        if (hand == null)
            return -1;

        int next = 0;
        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill == null)
                continue;

            if (next != i)
            {
                hand[next] = skill;
                hand[i] = null;
            }
            next++;
        }

        for (int i = next; i < hand.Length; i++)
            hand[i] = null;

        return next < hand.Length ? next : -1;
    }

    private void InvalidateDiscardSelectionSkillOwner(Skill skill)
    {
        if (skill?.OwnerCharater is PlayerCharacter owner)
            owner.InvalidateSkillTooltipCache();
        if (_activePlayer != null && GodotObject.IsInstanceValid(_activePlayer))
            _activePlayer.InvalidateSkillTooltipCache();
    }

    private void ClearDiscardSelectionSelectedCards()
    {
        ClearDiscardSelectionVisualState(returnCards: true);
    }

    private async Task ReturnDiscardSelectionCardsAsync()
    {
        _skillRemovalBuffer.Clear();
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
            _skillRemovalBuffer.Add(_discardSelectionSkills[i]);

        var returnTasks = new List<Task>(_skillRemovalBuffer.Count);
        for (int i = 0; i < _skillRemovalBuffer.Count; i++)
        {
            returnTasks.Add(
                MoveDiscardSelectionCardBackAsync(
                    _skillRemovalBuffer[i],
                    animateBack: true
                )
            );
        }
        _skillRemovalBuffer.Clear();

        if (returnTasks.Count > 0)
            await Task.WhenAll(returnTasks);

        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        _discardSelectionOriginalHandIndexes.Clear();
        _discardSelectionOriginalVisualHandIndexes.Clear();
    }

    private void ClearDiscardSelectionVisualState(bool returnCards)
    {
        if (returnCards)
        {
            _skillRemovalBuffer.Clear();
            for (int i = 0; i < _discardSelectionSkills.Count; i++)
                _skillRemovalBuffer.Add(_discardSelectionSkills[i]);
            for (int i = 0; i < _skillRemovalBuffer.Count; i++)
            {
                Skill skill = _skillRemovalBuffer[i];
                _ = MoveDiscardSelectionCardBackAsync(skill, animateBack: false);
            }
            _skillRemovalBuffer.Clear();

            if (_discardSelectionReturningHandIndexes.Count > 0)
                RefreshTurnUi();
        }
        else
        {
            _handCardReturnBuffer.Clear();
            foreach (SkillCard card in _discardSelectionCards.Values)
                _handCardReturnBuffer.Add(card);
            for (int i = 0; i < _handCardReturnBuffer.Count; i++)
            {
                SkillCard card = _handCardReturnBuffer[i];
                ResetOriginalSelectionCardVisual(card);
                if (card == null || !GodotObject.IsInstanceValid(card))
                    continue;

                card.Visible = false;
                SetCardButtonInputEnabled(card, false);
            }
            _handCardReturnBuffer.Clear();
        }

        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        _discardSelectionOriginalHandIndexes.Clear();
        _discardSelectionOriginalVisualHandIndexes.Clear();
        ClearDiscardSelectionReturnAnimationState();
    }

    private void ClearDiscardSelectionReturnAnimationState()
    {
        _visibleHandSlotIndexesBuffer.AsSpan().Clear();
        int returningCount = 0;
        foreach (int index in _discardSelectionReturningHandIndexes)
        {
            if (returningCount >= _visibleHandSlotIndexesBuffer.Length)
                break;

            _visibleHandSlotIndexesBuffer[returningCount++] = index;
        }
        _discardSelectionReturningHandIndexes.Clear();
        for (int i = 0; i < returningCount; i++)
        {
            int index = _visibleHandSlotIndexesBuffer[i];
            _discardSelectionFlyingReturnHandIndexes.Remove(index);
            RevealDiscardSelectionReturnedHandCard(index);
        }

        _handCardReturnBuffer.Clear();
        foreach (SkillCard card in _discardSelectionReturningPreviewCards.Values)
            _handCardReturnBuffer.Add(card);
        for (int i = 0; i < _handCardReturnBuffer.Count; i++)
        {
            SkillCard card = _handCardReturnBuffer[i];
            if (card != null && GodotObject.IsInstanceValid(card))
            {
                KillDiscardSelectionArrangeTween(card);
                card.QueueFree();
            }
        }
        _handCardReturnBuffer.Clear();

        _discardSelectionReturningPreviewCards.Clear();
        _discardSelectionFlyingReturnHandIndexes.Clear();
    }

    private void KillDiscardSelectionArrangeTween(SkillCard card)
    {
        if (card == null)
            return;

        if (
            _discardSelectionArrangeTweens.TryGetValue(card, out Tween tween)
            && tween != null
            && tween.IsValid()
        )
        {
            tween.Kill();
        }
        _discardSelectionArrangeTweens.Remove(card);
        _discardSelectionArrangeTargets.Remove(card);
    }

    private void HideDiscardedHandCardAfterSelection(int index, SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.ResetState();
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Position = Vector2.Zero;
        card.Scale = BattleCardScale;
        card.Modulate = SkillButton.EnabledModulate;
        card.Visible = false;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        card.ZIndex = 0;
        ResetCardDisplayTracking(index);
    }

    private Control EnsureDiscardSelectionOverlay()
    {
        _discardSelectionOverlay ??= GetParent().GetNode<Control>("DiscardSelectionOverlay");
        _discardSelectionOverlay.Visible = true;
        return _discardSelectionOverlay;
    }

    private void EnsureDiscardSelectionScreenMask()
    {
        if (!GodotObject.IsInstanceValid(_discardSelectionScreenMask))
        {
            _discardSelectionScreenMask = EnsureCardPlayOverlay()?.GetNode<ColorRect>("DiscardSelectionScreenMask");
            if (_discardSelectionScreenMask != null)
                _discardSelectionScreenMaskColor = _discardSelectionScreenMask.Color;
        }
        ApplyDiscardSelectionScreenMaskVisibleState();
    }

    private void HideDiscardSelectionScreenMask()
    {
        ResetDiscardSelectionTemporaryHideState();
        if (
            _discardSelectionScreenMask != null
            && GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            _discardSelectionScreenMask.Visible = false;
        }

    }

    private async Task<int> DiscardSelectedHandCardsAsync()
    {
        if (_discardSelectionSkills.Count == 0)
            return 0;

        List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)> entries =
            _discardSelectionEntryBuffer;
        entries.Clear();
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
        {
            Skill skill = _discardSelectionSkills[i];
            if (skill == null)
                continue;

            _discardSelectionCards.TryGetValue(skill, out SkillCard sourceCard);
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
            {
                continue;
            }

            SkillCard actionCard = CreateDiscardSelectionActionPreviewCard(skill, sourceCard);
            if (actionCard == null)
                continue;

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter ?? _activePlayer;
            if (_discardSelectionOriginalHandIndexes.TryGetValue(skill, out int originalIndex))
            {
                _discardSelectionOriginalVisualHandIndexes.Remove(originalIndex);
                _discardSelectionFlyingToDiscardHandIndexes.Add(originalIndex);
            }
            sourceCard.Visible = false;
            SetCardButtonInputEnabled(sourceCard, false);
            sourceCard.HideHoverUi();
            actionCard.ZIndex = PlayedCardZIndex + entries.Count + 1;
            entries.Add((skill, owner, actionCard));
        }

        if (entries.Count == 0)
        {
            for (int i = 0; i < _discardSelectionSkills.Count; i++)
            {
                Skill skill = _discardSelectionSkills[i];
                RestoreActiveHandSkillFromDiscardSelection(skill);
            }
            return 0;
        }

        foreach (var entry in entries)
        {
            entry.Card.Button.Disabled = true;
            entry.Card.HoverHint.Visible = false;
        }

        List<Task> flyTasks = _discardSelectionFlyTasks;
        flyTasks.Clear();
        bool previousFreezeHandLayout = _freezeHandLayout;
        int selectedCount = entries.Count;
        _freezeHandLayout = true;
        try
        {
            foreach (var entry in entries)
            {
                if (entry.Card == null)
                {
                    entry.Card?.Vanish();
                    continue;
                }

                KillDiscardSelectionArrangeTween(entry.Card);
                entry.Card.ZIndex = PlayedCardZIndex + flyTasks.Count + 1;
                flyTasks.Add(PlayCardDiscardFlyAsync(entry.Card));
            }

            for (int i = 0; i < flyTasks.Count; i++)
                await flyTasks[i];

            _skillRemovalBuffer.Clear();
            for (int i = 0; i < entries.Count; i++)
                _skillRemovalBuffer.Add(entries[i].Skill);
            _discardSelectionFlyingToDiscardHandIndexes.Clear();
            RemoveActiveHandSkillsForSelection(_skillRemovalBuffer);

            foreach (var entry in entries)
            {
                BattleNode?.DiscardBattleSkill(entry.Owner, entry.Skill, forceDiscard: true);
            }
            _skillRemovalBuffer.Clear();
            CompactActiveHandAfterDiscardSelection();

            foreach (var entry in entries)
            {
                if (entry.Card != null && GodotObject.IsInstanceValid(entry.Card))
                {
                    KillDiscardSelectionArrangeTween(entry.Card);
                    entry.Card.QueueFree();
                }
            }
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
            _discardSelectionFlyingToDiscardHandIndexes.Clear();
            flyTasks.Clear();
            entries.Clear();
        }

        return selectedCount;
    }

    private Task<int> ApplyKeywordToSelectedHandCardsAsync()
    {
        if (_discardSelectionSkills.Count == 0 || BattleNode == null)
            return Task.FromResult(0);

        int applied = 0;
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
        {
            Skill skill = _discardSelectionSkills[i];
            if (skill?.SkillId is not SkillID)
                continue;

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter ?? _activePlayer;
            if (owner == null)
                continue;

            BattleNode.AddBattleCardKeyword(skill, _discardSelectionKeyword);
            skill.UpdateDescription();
            owner.InvalidateSkillTooltipCache();
            applied++;
        }

        _activePlayer?.InvalidateSkillTooltipCache();
        RequestTurnUiRefresh();
        return Task.FromResult(applied);
    }

    private async Task<int> TransformSelectedHandCardsAsync()
    {
        if (_discardSelectionSkills.Count == 0 || BattleNode == null)
            return 0;

        var transformedEntries = new List<(
            int SelectionIndex,
            Skill Source,
            Skill Replacement,
            SkillCard Card,
            int OriginalIndex
        )>();
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
        {
            Skill sourceSkill = _discardSelectionSkills[i];
            if (sourceSkill == null)
                continue;

            if (
                !BattleNode.TryCreateTransformedBattleSkill(
                    sourceSkill,
                    _discardSelectionTransformSkillId,
                    _activePlayer,
                    out Skill replacementSkill
                )
            )
            {
                continue;
            }

            _discardSelectionCards.TryGetValue(sourceSkill, out SkillCard previewCard);
            int originalIndex = _discardSelectionOriginalHandIndexes.TryGetValue(
                sourceSkill,
                out int storedIndex
            )
                ? storedIndex
                : -1;
            transformedEntries.Add(
                (i, sourceSkill, replacementSkill, previewCard, originalIndex)
            );
        }

        var animationTasks = new List<Task>(transformedEntries.Count);
        for (int i = 0; i < transformedEntries.Count; i++)
        {
            var entry = transformedEntries[i];
            if (
                entry.Card == null
                || !GodotObject.IsInstanceValid(entry.Card)
                || !entry.Card.IsInsideTree()
            )
            {
                continue;
            }

            SkillCard card = entry.Card;
            Skill replacement = entry.Replacement;
            animationTasks.Add(
                CardTransformShineVfx.PlayOnCardAsync(
                    card,
                    () => card.SetSkill(replacement),
                    shortVersion: true
                )
            );
        }

        if (animationTasks.Count > 0)
            await Task.WhenAll(animationTasks);

        int transformedCount = 0;
        Skill[] hand = GetActiveHandSkills();
        for (int i = 0; i < transformedEntries.Count; i++)
        {
            var entry = transformedEntries[i];
            if (
                hand == null
                || entry.OriginalIndex < 0
                || entry.OriginalIndex >= hand.Length
                || !ReferenceEquals(hand[entry.OriginalIndex], entry.Source)
            )
            {
                continue;
            }

            hand[entry.OriginalIndex] = entry.Replacement;
            _discardSelectionSkills[entry.SelectionIndex] = entry.Replacement;
            _discardSelectionCards.Remove(entry.Source);
            _discardSelectionCards[entry.Replacement] = entry.Card;
            _discardSelectionOriginalHandIndexes.Remove(entry.Source);
            _discardSelectionOriginalHandIndexes[entry.Replacement] = entry.OriginalIndex;
            transformedCount++;
        }

        _activePlayer?.InvalidateSkillTooltipCache();
        RequestTurnUiRefresh(refreshHover: true);
        return transformedCount;
    }

    private async Task<int> ExhaustSelectedHandCardsAsync()
    {
        if (_discardSelectionSkills.Count == 0)
            return 0;

        List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)> entries =
            _discardSelectionEntryBuffer;
        entries.Clear();
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
        {
            Skill skill = _discardSelectionSkills[i];
            if (skill == null)
                continue;

            _discardSelectionCards.TryGetValue(skill, out SkillCard sourceCard);
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
            {
                continue;
            }

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter ?? _activePlayer;
            // Keep the source slot detached until the exhaust animation completes. The
            // remaining hand has already filled this card's old position while it is
            // selected; adding it back to the layout here causes a one-frame shove.
            entries.Add((skill, owner, sourceCard));
        }

        if (entries.Count == 0)
        {
            for (int i = 0; i < _discardSelectionSkills.Count; i++)
            {
                Skill skill = _discardSelectionSkills[i];
                RestoreActiveHandSkillFromDiscardSelection(skill);
            }
            return 0;
        }

        bool previousFreezeHandLayout = _freezeHandLayout;
        int selectedCount = entries.Count;
        _freezeHandLayout = true;
        try
        {
            bool playedAny = false;
            int order = 0;
            foreach (var entry in entries)
            {
                if (entry.Card == null)
                    continue;

                KillDiscardSelectionArrangeTween(entry.Card);
                entry.Card.ZIndex = PlayedCardZIndex + order + 1;
                entry.Card.PlayExhaustEffectAtCurrentVisualTransform(CardPlayVanishDuration);
                playedAny = true;
                order++;
            }

            if (playedAny)
            {
                await ToSignal(
                    GetTree().CreateTimer(CardPlayVanishDuration),
                    SceneTreeTimer.SignalName.Timeout
                );
            }

            _skillRemovalBuffer.Clear();
            for (int i = 0; i < entries.Count; i++)
                _skillRemovalBuffer.Add(entries[i].Skill);
            RemoveActiveHandSkillsForSelection(_skillRemovalBuffer);
            BattleNode?.ExhaustPlayerTeamBattleSkills(_skillRemovalBuffer, _activePlayer);
            _skillRemovalBuffer.Clear();
            CompactActiveHandAfterDiscardSelection();
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
            entries.Clear();
        }

        return selectedCount;
    }

    private void CompactActiveHandAfterDiscardSelection()
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return;

        CompactHandInPlace(hand);

        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill?.OwnerCharater is PlayerCharacter owner)
                owner.InvalidateSkillTooltipCache();
        }
        _activePlayer?.InvalidateSkillTooltipCache();
    }

    private void RemoveActiveHandSkillsForSelection(IReadOnlyList<Skill> skills)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || skills == null || skills.Count == 0)
            return;

        for (int handIndex = 0; handIndex < hand.Length; handIndex++)
        {
            Skill handSkill = hand[handIndex];
            if (handSkill == null)
                continue;

            for (int selectedIndex = 0; selectedIndex < skills.Count; selectedIndex++)
            {
                if (!ReferenceEquals(handSkill, skills[selectedIndex]))
                    continue;

                hand[handIndex] = null;
                break;
            }
        }
    }


}
