using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private async Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (_suppressCardButtonPressUntilLeftRelease && !allowSuppressedPress)
            return;

        if (_isDiscardSelectionActive)
        {
            await HandleDiscardSelectionCardPressedAsync(index);
            return;
        }

        if (
            _isResolvingCard
            || _endTurnQueued
            || _isPileCardSelectionActive
            || IsManualTargetSelectionPending()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= (GetActiveHandSkills()?.Length ?? 0)
        )
        {
            return;
        }

        Skill skill = GetActiveHandSkills()?[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return;

        if (_liftedCardIndex != -1 && _liftedCardIndex != index)
            return;

        bool shouldUseManualTargetArrowSelection = ShouldUseManualTargetArrowSelection(skill);
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
            _isResolvingCard
            || _endTurnQueued
            || _isPileCardSelectionActive
            || IsManualTargetSelectionPending()
            || _manualTargetArrowSelectionActive
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= (GetActiveHandSkills()?.Length ?? 0)
        )
        {
            return Task.CompletedTask;
        }

        Skill skill = GetActiveHandSkills()?[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return Task.CompletedTask;

        if (_liftedCardIndex == index)
        {
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: true);

        if (ShouldUseManualTargetArrowSelection(skill))
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
            || IsAnyCardDrawEntryBusy()
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

        if (_discardSelectionSkills.Count >= _discardSelectionTargetCount)
            return;

        AddDiscardSelectionSelectedCard(index, skill);
        ArrangeDiscardSelectionSelectedCards();
        RefreshTurnUi();
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
        HideDiscardSelectionScreenMask();
        RefreshTurnUi();

        int selectedCount = _discardSelectionExhaustMode
            ? await ExhaustSelectedHandCardsAsync()
            : await DiscardSelectedHandCardsAsync();
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        _isDiscardSelectionActive = false;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionTargetCount = 0;
        _discardSelectionCompletion = null;
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionVisualState(returnCards: false);
        RefreshTurnUi();
        completion?.TrySetResult(selectedCount);
    }

    private void CancelDiscardSelection()
    {
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        _isDiscardSelectionActive = false;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
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

        if (!_discardSelectionSkills.Contains(skill))
            _discardSelectionSkills.Add(skill);

        if (!_discardSelectionCards.ContainsKey(skill))
        {
            SkillCard previewCard = CreateDiscardSelectionPreviewCard(index, skill);
            if (previewCard == null)
                return;

            _discardSelectionCards[skill] = previewCard;
        }

        ClearDiscardSelectionHandSlotPreview(index);
        ClearActiveHandSkillForDiscardSelection(index, skill);

        SkillCard card = _discardSelectionCards[skill];
        card.StopBattleMotion();
        card.HoverHint.Visible = false;
        card.Modulate = SkillButton.EnabledModulate;
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

        handCard.HideHoverUi();
        handCard.HoverHint.Visible = false;
        handCard.StopBattleMotion();
        handCard.Modulate = SkillButton.EnabledModulate;
        handCard.Visible = false;
        SetCardButtonInputEnabled(handCard, false);
    }

    private SkillCard CreateDiscardSelectionPreviewCard(int index, Skill skill)
    {
        Control overlay = EnsureDiscardSelectionOverlay();
        if (skill == null || overlay == null || !GodotObject.IsInstanceValid(overlay))
            return null;

        Vector2 previewScale = GetDiscardSelectionCardScale();
        Vector2 globalPosition = GetDiscardSelectionPreviewStartPosition(index);
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"DiscardSelectionCard{index}";
        card.ConfigureDisplayScale(previewScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.PreviewCharacterName = skill.OwnerCharater?.CharacterName;
        card.PreviewCharacterKey = (skill.OwnerCharater as PlayerCharacter)?.CharacterKey;
        overlay.AddChild(card);

        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.Visible = true;
        card.GlobalPosition = globalPosition;
        card.Scale = previewScale;
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.MouseFilter = MouseFilterEnum.Stop;
        card.Button.MouseFilter = MouseFilterEnum.Stop;
        card.Button.Disabled = false;
        card.HoverHint.Visible = false;
        card.Button.Pressed += () =>
        {
            if (
                _isDiscardSelectionActive
                && !_isDiscardSelectionCompleting
                && _discardSelectionSkills.Contains(skill)
            )
            {
                MoveDiscardSelectionCardBack(skill, animateBack: true);
                ArrangeDiscardSelectionSelectedCards();
                RefreshTurnUi();
            }
        };
        return card;
    }

    private Vector2 GetDiscardSelectionPreviewStartPosition(int index)
    {
        SkillCard sourceCard = IsCardIndexValid(index) ? _cards[index] : null;
        if (sourceCard != null && GodotObject.IsInstanceValid(sourceCard) && sourceCard.Visible)
            return sourceCard.GlobalPosition;

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot))
            return slot.GlobalPosition;

        return GetDiscardSelectionSelectedPosition(GetDiscardSelectionCardScale(), 0, 1);
    }

    private void ArrangeDiscardSelectionSelectedCards()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        int count = selectedSkills.Length;
        Vector2 selectedScale = GetDiscardSelectionCardScale();
        for (int order = 0; order < count; order++)
        {
            Skill skill = selectedSkills[order];
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
            card.ZIndex = DiscardSelectionSelectedCardZIndex + order;
            card.Button.Disabled = false;
            card.HoverHint.Visible = false;
            card.Modulate = SkillButton.EnabledModulate;

            Tween tween = card.CreateTween();
            tween.SetParallel(true);
            tween
                .TweenProperty(card, "global_position", targetPosition, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween
                .TweenProperty(card, "scale", targetScale, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }
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

    private void MoveDiscardSelectionCardBack(Skill skill, bool animateBack)
    {
        if (skill == null)
            return;

        _discardSelectionCards.TryGetValue(skill, out SkillCard previewCard);
        _discardSelectionCards.Remove(skill);

        int restoredIndex = -1;
        if (_discardSelectionSkills.Remove(skill))
            restoredIndex = RestoreActiveHandSkillFromDiscardSelection(skill);

        if (!animateBack || restoredIndex < 0)
        {
            if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
                previewCard.QueueFree();
            return;
        }

        if (previewCard == null || !GodotObject.IsInstanceValid(previewCard))
        {
            RefreshTurnUi();
            return;
        }

        _discardSelectionReturningHandIndexes.Add(restoredIndex);
        _discardSelectionFlyingReturnHandIndexes.Add(restoredIndex);
        _discardSelectionReturningPreviewCards[restoredIndex] = previewCard;
        SkillCard handCard = IsCardIndexValid(restoredIndex) ? _cards[restoredIndex] : null;
        if (handCard != null && GodotObject.IsInstanceValid(handCard))
            handCard.ZIndex = DiscardSelectionSelectedCardZIndex;
        RefreshTurnUi();
        _ = AnimateDiscardSelectionPreviewReturnAsync(previewCard, restoredIndex);
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
                previewCard.QueueFree();
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
            _cardSlotLayoutFollowActive[targetIndex] = true;
            UpdateProcessState();

            float moveDuration = GetHandLayoutTweenDuration(slot.Position, targetPosition.Value);
            Tween scaleTween = movingCard.CreateTween();
            scaleTween
                .TweenProperty(movingCard, "scale", BattleCardScale, moveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);

            await WaitForDiscardSelectionReturnArrivalAsync(targetIndex, movingCard);
        }
        finally
        {
            _discardSelectionReturningPreviewCards.Remove(targetIndex);
            FinishDiscardSelectionReturnArrival(targetIndex, movingCard);
            _discardSelectionFlyingReturnHandIndexes.Remove(targetIndex);
            _discardSelectionReturningHandIndexes.Remove(targetIndex);
            RevealDiscardSelectionReturnedHandCard(targetIndex);
            if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
                previewCard.QueueFree();
            if (IsInsideTree())
                RefreshTurnUi();
        }
    }

    private async Task WaitForDiscardSelectionReturnArrivalAsync(int index, SkillCard movingCard)
    {
        while (
            IsCardIndexValid(index)
            && _discardSelectionFlyingReturnHandIndexes.Contains(index)
            && _cardSlotLayoutFollowActive[index]
            && movingCard != null
            && GodotObject.IsInstanceValid(movingCard)
            && IsInsideTree()
        )
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
                && !IsAnyCardDrawEntryBusy()
        );
    }

    private void ClearActiveHandSkillForDiscardSelection(int index, Skill skill)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || index < 0 || index >= hand.Length)
            return;

        if (!ReferenceEquals(hand[index], skill))
            return;

        hand[index] = null;
        _displayedSkills[index] = null;
        _displayedSkillIds[index] = null;
        InvalidateDiscardSelectionSkillOwner(skill);
    }

    private int RestoreActiveHandSkillFromDiscardSelection(Skill skill)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || skill == null)
            return -1;

        int restoreIndex = CompactHandInPlace(hand);
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

    private void ClearDiscardSelectionVisualState(bool returnCards)
    {
        if (returnCards)
        {
            foreach (Skill skill in _discardSelectionSkills.ToArray())
                MoveDiscardSelectionCardBack(skill, animateBack: false);
        }
        else
        {
            foreach (SkillCard card in _discardSelectionCards.Values.ToArray())
            {
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.QueueFree();
            }
        }

        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        ClearDiscardSelectionReturnAnimationState();
    }

    private void ClearDiscardSelectionReturnAnimationState()
    {
        int[] returningIndexes = _discardSelectionReturningHandIndexes.ToArray();
        _discardSelectionReturningHandIndexes.Clear();
        foreach (int index in returningIndexes)
        {
            _discardSelectionFlyingReturnHandIndexes.Remove(index);
            RevealDiscardSelectionReturnedHandCard(index);
        }

        foreach (SkillCard card in _discardSelectionReturningPreviewCards.Values.ToArray())
        {
            if (card != null && GodotObject.IsInstanceValid(card))
                card.QueueFree();
        }

        _discardSelectionReturningPreviewCards.Clear();
        _discardSelectionFlyingReturnHandIndexes.Clear();
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
        Node parent = GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        const string overlayName = "DiscardSelectionOverlay";
        Control overlay =
            _discardSelectionOverlay != null
            && GodotObject.IsInstanceValid(_discardSelectionOverlay)
                ? _discardSelectionOverlay
                : parent.GetNodeOrNull<Control>(overlayName);

        if (overlay == null)
        {
            overlay = new Control
            {
                Name = overlayName,
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = DiscardSelectionOverlayZIndex,
                TopLevel = false,
            };
            overlay.SetAnchorsPreset(LayoutPreset.FullRect);
            if (parent is Control parentControl)
                overlay.Size = parentControl.Size;
            else
                overlay.Size = GetViewport().GetVisibleRect().Size;
            parent.AddChild(overlay);
        }

        overlay.Visible = true;
        overlay.ZIndex = DiscardSelectionOverlayZIndex;
        overlay.MouseFilter = MouseFilterEnum.Ignore;
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.Size = GetViewport().GetVisibleRect().Size;
        if (
            _endTurnButton != null
            && GodotObject.IsInstanceValid(_endTurnButton)
            && _endTurnButton.GetParent() is Node actionButtonsRoot
            && actionButtonsRoot.GetParent() == parent
        )
        {
            int buttonIndex = actionButtonsRoot.GetIndex();
            parent.MoveChild(overlay, Math.Max(0, buttonIndex));
        }
        _discardSelectionOverlay = overlay;
        return overlay;
    }

    private void EnsureDiscardSelectionScreenMask()
    {
        if (
            _discardSelectionScreenMask != null
            && GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            ApplyDiscardSelectionScreenMaskVisibleState();
            return;
        }

        Node parent = EnsureCardPlayOverlay();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return;

        _discardSelectionScreenMask = new ColorRect
        {
            Name = "DiscardSelectionScreenMask",
            Color = new Color(0f, 0f, 0f, DiscardSelectionScreenMaskMaxAlpha),
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = DiscardSelectionOverlayZIndex - 1,
        };
        _discardSelectionScreenMask.SetAnchorsPreset(LayoutPreset.FullRect);
        _discardSelectionScreenMask.Size = GetViewport().GetVisibleRect().Size;
        parent.AddChild(_discardSelectionScreenMask);
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
            _discardSelectionScreenMask.QueueFree();
        }

        _discardSelectionScreenMask = null;
    }

    private async Task<int> DiscardSelectedHandCardsAsync()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        if (selectedSkills.Length == 0)
            return 0;

        var entries = new List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)>();
        foreach (Skill skill in selectedSkills)
        {
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
            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;
            sourceCard.Modulate = SkillButton.EnabledModulate;
            sourceCard.ZIndex = PlayedCardZIndex + entries.Count + 1;
            entries.Add((skill, owner, sourceCard));
        }

        if (entries.Count == 0)
        {
            foreach (Skill skill in selectedSkills)
                RestoreActiveHandSkillFromDiscardSelection(skill);
            return 0;
        }

        foreach (var entry in entries)
        {
            entry.Card.Button.Disabled = true;
            entry.Card.HoverHint.Visible = false;
        }

        var flyTasks = new List<Task>();
        bool previousFreezeHandLayout = _freezeHandLayout;
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

                entry.Card.ZIndex = PlayedCardZIndex + flyTasks.Count + 1;
                flyTasks.Add(PlayCardDiscardFlyAsync(entry.Card));
            }

            if (flyTasks.Count > 0)
                await Task.WhenAll(flyTasks);

            foreach (var entry in entries)
            {
                BattleNode?.DiscardBattleSkill(entry.Owner, entry.Skill, forceDiscard: true);
            }
            CompactActiveHandAfterDiscardSelection();

            foreach (var entry in entries)
            {
                if (entry.Card != null && GodotObject.IsInstanceValid(entry.Card))
                    entry.Card.QueueFree();
            }
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
        }

        return entries.Count;
    }

    private async Task<int> ExhaustSelectedHandCardsAsync()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        if (selectedSkills.Length == 0)
            return 0;

        var entries = new List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)>();
        foreach (Skill skill in selectedSkills)
        {
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
            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;
            sourceCard.Modulate = SkillButton.EnabledModulate;
            sourceCard.ZIndex = PlayedCardZIndex + entries.Count + 1;
            entries.Add((skill, owner, sourceCard));
        }

        if (entries.Count == 0)
        {
            foreach (Skill skill in selectedSkills)
                RestoreActiveHandSkillFromDiscardSelection(skill);
            return 0;
        }

        bool previousFreezeHandLayout = _freezeHandLayout;
        _freezeHandLayout = true;
        try
        {
            bool playedAny = false;
            int order = 0;
            foreach (var entry in entries)
            {
                if (entry.Card == null)
                    continue;

                entry.Card.ZIndex = PlayedCardZIndex + order + 1;
                entry.Card.PlayExhaustEffect(CardPlayVanishDuration);
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

            BattleNode?.ExhaustPlayerTeamBattleSkills(
                entries.Select(entry => entry.Skill),
                _activePlayer
            );
            CompactActiveHandAfterDiscardSelection();

            foreach (var entry in entries)
            {
                if (entry.Card != null && GodotObject.IsInstanceValid(entry.Card))
                    entry.Card.QueueFree();
            }
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
        }

        return entries.Count;
    }

    private void CompactActiveHandAfterDiscardSelection()
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return;

        CompactHandInPlace(hand);

        foreach (
            PlayerCharacter owner in hand.Select(skill => skill?.OwnerCharater)
                .OfType<PlayerCharacter>()
                .Distinct()
        )
        {
            owner.InvalidateSkillTooltipCache();
        }
        _activePlayer?.InvalidateSkillTooltipCache();
    }


}
