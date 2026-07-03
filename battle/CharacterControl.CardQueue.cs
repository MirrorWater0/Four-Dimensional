using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private async Task SelectManualTargetFromHandAndQueueAsync(int index, Skill skill)
    {
        if (
            skill == null
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
        )
        {
            return;
        }

        if (!HasManualFriendlyTargetCandidates(skill))
        {
            QueueCardPlay(index, skill);
            return;
        }

        skill.ClearManualFriendlyTarget();
        SkillCard sourceCard = _cards[index];
        PrepareManualTargetArrowLiftedCard(index, skill);
        Character target = await ShowManualTargetArrowPickerAsync(skill, sourceCard, index);
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            skill.ClearManualFriendlyTarget();
            RefreshTurnUi();
            return;
        }

        skill.SetManualFriendlyTarget(target);
        if (!skill.HasManualFriendlyTarget())
        {
            skill.ClearManualFriendlyTarget();
            RefreshTurnUi();
            return;
        }

        QueueCardPlay(index, skill, keepManualFriendlyTarget: true);
    }

    private void ClearCardButtonPressSuppression()
    {
        _suppressCardButtonPressUntilLeftRelease = false;
    }

    private void SuppressHandHoverUntilMouseMove()
    {
        _suppressHandHoverUntilMouseMove = true;
        _handHoverSuppressionMousePosition = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
        _deferredHoverRefreshVersion++;
    }

    private void QueueCardPlay(int index, Skill skill, bool keepManualFriendlyTarget = false)
    {
        if (
            skill == null
            || _endTurnQueued
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
        )
        {
            return;
        }

        if (!keepManualFriendlyTarget)
            skill.ClearManualFriendlyTarget();

        DetachDrawEntryStateForImmediateInteraction(index);

        Character actor = skill.OwnerCharater ?? _activePlayer;
        bool hadStun = HasActiveStun(actor);
        if (!hadStun && !skill.TrySpendDisplayedEnergy())
        {
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        SkillCard sourceCard = _cards[index];
        PrepareHandVisualsBeforeQueuedPlay(index, sourceCard);

        SkillCard card = DetachHandCardForPlay(index);
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            skill.RefundDisplayedEnergy();
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.Modulate = QueuedCardModulate;
        card.ZIndex = PlayedCardZIndex + _queuedCardPlays.Count + 1;
        card.StopBattleMotion();

        var play = new QueuedCardPlay
        {
            Actor = actor,
            Index = index,
            Skill = skill,
            SkillId = skill.SkillId,
            SkillType = skill.SkillType,
            HadStun = hadStun,
            Card = card,
            IsHandCard = true,
            MoveToCenterBeforeEffect = true,
        };

        _queuedCardPlays.Enqueue(play);
        _queuedCardSkills.Add(skill);
        BattleNode?.RecordAutomationEvent(
            "card_queued",
            new Dictionary<string, object>
            {
                ["slot"] = index + 1,
                ["actor"] = Battle.CharacterAutomationId(actor),
                ["skillId"] = skill.SkillId?.ToString(),
                ["skillName"] = skill.SkillName,
                ["skillType"] = skill.SkillType.ToString(),
            }
        );

        ResolveHandSlotAfterQueuedPlay(play);
        RefreshQueuedPlayCardLayers();
        RefreshTurnUi();
        _ = ProcessCardQueueAsync();
    }

    private void PrepareHandVisualsBeforeQueuedPlay(int playedIndex, SkillCard playedCard)
    {
        if (_liftedCardIndex != -1 && _liftedCardIndex != playedIndex)
            ClearLiftedCard(instant: true);
        else if (_liftedCardIndex == playedIndex)
        {
            _liftedCardIndex = -1;
            _liftedCardSkill = null;
            _liftedCard = null;
            _manualTargetArrowUsesLiftedCard = false;
        }

        _suppressCardButtonPressUntilLeftRelease = false;
        SetHandInputBlockerVisible(false);
        if (!_manualTargetArrowSelectionActive)
            SetCardHoverUiEnabled(true);

        if (_hoveredCardIndex == playedIndex)
            _hoveredCardIndex = -1;
        else if (_hoveredCardIndex != -1)
            ClearHandCardHoverMotion(_hoveredCardIndex, instant: false);

        ClearAllHandCardHoverMotionExcept(playedIndex, instant: false);
        ClearCardEnergyPreview();
        HideCardHoverPreview(playedIndex);

        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
        {
            playedCard.HideHoverUi();
            playedCard.HoverHint.Visible = false;
        }

        SuppressHandHoverUntilMouseMove();
        UpdateProcessState();
    }

    private SkillCard DetachHandCardForPlay(int index)
    {
        if (!IsCardIndexValid(index))
            return null;

        SkillCard card = _cards[index];
        Control slot = _cardSlots[index];
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || slot == null
            || !GodotObject.IsInstanceValid(slot)
            || overlay == null
            || !GodotObject.IsInstanceValid(overlay)
        )
        {
            return null;
        }

        Vector2 globalPosition = card.GlobalPosition;
        Vector2 scale = card.Scale;
        float rotation = card.Rotation;
        Vector2 pivotOffset = card.PivotOffset;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;

        Node parent = card.GetParent();
        parent?.RemoveChild(card);
        overlay.AddChild(card);

        card.GlobalPosition = globalPosition;
        card.Scale = scale;
        card.Rotation = rotation;
        card.PivotOffset = pivotOffset;
        card.SetHandIndexBadge(0, visible: false);
        card.MouseFilter = MouseFilterEnum.Ignore;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        card.ZIndex = PlayedCardZIndex + _queuedCardPlays.Count + 1;
        card.StopBattleMotion();
        RemoveHandCardIdentity(card.CurrentSkill, card);
        _cards[index] = null;

        return card;
    }

    public Task QueueCarryCardAsync(Character actor, Skill skill)
    {
        if (
            actor == null
            || !GodotObject.IsInstanceValid(actor)
            || actor.State == Character.CharacterState.Dying
            || skill == null
        )
        {
            _freezeHandLayout = false;
            return Task.CompletedTask;
        }

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        bool hadStun = HasActiveStun(actor);
        SkillCard card = CreateTemporaryPlayCard(actor, skill, "连携", "CarryCard");
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var play = new QueuedCardPlay
        {
            Actor = actor,
            Index = -1,
            Skill = skill,
            SkillId = skill.SkillId,
            SkillType = skill.SkillType,
            HadStun = hadStun,
            Card = card,
            IsTemporaryCard = card != null,
            FlyToDiscardPileAfterUse = card != null,
            FreeEnergyCost = true,
            Completion = completion,
        };
        if (card != null)
            play.MoveToCenterTask = ShowTemporaryCardAtCenterAsync(play, "连携");

        bool shouldWaitForCompletion = !_isProcessingCardQueue;
        if (_isProcessingCardQueue)
            _queuedFollowUpCardPlays.Enqueue(play);
        else
            _queuedCardPlays.Enqueue(play);

        BattleNode?.DiscardBattleSkill(actor, skill);
        _ = ProcessCardQueueAsync();
        return shouldWaitForCompletion ? completion.Task : Task.CompletedTask;
    }

    public async Task PlayEchoCardAsync(Character actor, Skill skill)
    {
        if (
            actor == null
            || !GodotObject.IsInstanceValid(actor)
            || actor.State == Character.CharacterState.Dying
            || skill == null
        )
        {
            return;
        }

        SkillCard card = CreateTemporaryPlayCard(actor, skill, "回响", "EchoCard");
        if (card == null)
            return;

        var play = new QueuedCardPlay
        {
            Actor = actor,
            Skill = skill,
            Card = card,
            IsTemporaryCard = true,
        };

        await ShowTemporaryCardAtCenterAsync(play, "回响");
        await PlayCardVanishAfterExecutionAsync(play);
        QueueFreeTemporaryCard(card);
    }

    private async Task ProcessCardQueueAsync()
    {
        if (_isProcessingCardQueue)
            return;

        _isProcessingCardQueue = true;
        try
        {
            while (_queuedCardPlays.Count > 0 || _queuedFollowUpCardPlays.Count > 0)
            {
                QueuedCardPlay play = DequeueNextCardPlay();
                _activeExecutingCardPlay = play;
                _activeQueuedCardPlay = play?.IsHandCard == true ? play : null;
                RefreshQueuedPlayCardLayers();

                bool shouldStop;
                try
                {
                    shouldStop = await ExecuteQueuedCardAsync(play);
                }
                catch
                {
                    try
                    {
                        RecoverInterruptedQueuedPlay(play);
                    }
                    catch (Exception recoverEx)
                    {
                        GD.PushWarning(
                            $"CharacterControl: failed to recover interrupted queued play: {recoverEx.Message}"
                        );
                    }

                    if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
                        break;

                    throw;
                }
                finally
                {
                    if (_activeExecutingCardPlay == play)
                        _activeExecutingCardPlay = null;
                    if (_activeQueuedCardPlay == play)
                    {
                        _activeQueuedCardPlay = null;
                        RefreshQueuedPlayCardLayers();
                    }
                }

                if (shouldStop)
                {
                    ClearCardQueue(resetCards: true);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"CharacterControl: card queue aborted: {ex}");
            HideManualTargetPicker();
            ClearCardQueue(resetCards: true);
        }
        finally
        {
            _isProcessingCardQueue = false;
            if (GodotObject.IsInstanceValid(this))
            {
                if (_endTurnQueued)
                    await ExecuteQueuedEndTurnAsync();
                else
                {
                    RefreshTurnUi();
                    ScheduleCardHoverRefresh();
                }
            }
        }
    }

    private QueuedCardPlay DequeueNextCardPlay()
    {
        if (_queuedFollowUpCardPlays.Count > 0)
            return _queuedFollowUpCardPlays.Dequeue();

        return _queuedCardPlays.Dequeue();
    }

    private async Task<bool> ExecuteQueuedCardAsync(QueuedCardPlay play)
    {
        if (
            play == null
            || play.Actor == null
            || !GodotObject.IsInstanceValid(play.Actor)
            || play.Skill == null
            || (
                play.IsHandCard
                && (_activePlayer == null || !GodotObject.IsInstanceValid(_activePlayer))
            )
        )
        {
            play?.Skill?.RefundDisplayedEnergy();
            RestoreHandSlotForQueuedPlay(play);
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play?.IsHandCard == true;
        }

        if (play.IsHandCard)
            RefreshTurnUi();
        BattleNode?.RecordAutomationEvent(
            "card_execute_start",
            new Dictionary<string, object>
            {
                ["slot"] = play.Index + 1,
                ["actor"] = Battle.CharacterAutomationId(play.Actor),
                ["skillId"] = play.Skill.SkillId?.ToString(),
                ["skillName"] = play.Skill.SkillName,
                ["skillType"] = play.Skill.SkillType.ToString(),
            }
        );

        if (!await SelectManualFriendlyTargetIfNeededAsync(play))
        {
            play.Skill.RefundDisplayedEnergy();
            RestoreHandSlotForQueuedPlay(play);
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play.IsHandCard;
        }

        if (play.MoveToCenterTask != null)
            await play.MoveToCenterTask;
        else if (play.IsHandCard && play.MoveToCenterBeforeEffect)
            await MoveHandPlayCardToCenterAsync(play);

        play.CachedDrawEntryStartCenter = TryGetQueuedPlayCardGlobalCenter(play);

        if (play.FreeEnergyCost)
        {
            using (play.Skill.BeginEnergyCostWaiver())
            {
                await play.Skill.Effect();
            }
        }
        else
        {
            await play.Skill.Effect();
        }

        ResolveBattlePileAfterQueuedPlay(play);
        BattleNode?.RecordAutomationEvent(
            "card_execute_finished",
            new Dictionary<string, object>
            {
                ["slot"] = play.Index + 1,
                ["actor"] = Battle.CharacterAutomationId(play.Actor),
                ["skillId"] = play.Skill.SkillId?.ToString(),
                ["skillName"] = play.Skill.SkillName,
                ["skillType"] = play.Skill.SkillType.ToString(),
            }
        );

        if (BattleNode?.ShouldAbortSkillResolution() == true)
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: true);
            return true;
        }

        if (!GodotObject.IsInstanceValid(this))
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return true;
        }

        if (play.IsHandCard && !IsHandPlayStillValid(play))
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return true;
        }

        await PlayCardVanishAfterExecutionAsync(play);

        if (play.IsTemporaryCard)
        {
            CompleteQueuedPlay(play, succeeded: true);
            QueueFreeTemporaryCard(play.Card);
            return false;
        }

        QueueFreeQueuedPlayCard(play);

        if (play.Actor.State == Character.CharacterState.Dying)
        {
            if (play.Actor != _activePlayer)
            {
                CompleteQueuedPlay(play, succeeded: true);
                RefreshTurnUi();
                return false;
            }

            _isResolvingCard = false;
            play.Actor.EndAction();
            CompleteQueuedPlay(play, succeeded: true);
            return true;
        }

        CompleteQueuedPlay(play, succeeded: true);
        return false;
    }

    private void ResolveHandSlotAfterQueuedPlay(QueuedCardPlay play)
    {
        if (play?.IsHandCard != true)
            return;

        if (play.RemovedFromHand)
            return;

        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        BattleNode.RemovePlayerTeamBattleHandCardAt(play.Index);
        ResetCardDisplayTracking(play.Index);
        play.RemovedFromHand = true;
    }

    private void RestoreHandSlotForQueuedPlay(QueuedCardPlay play)
    {
        if (play?.IsHandCard != true || !play.RemovedFromHand || play.ResolvedToBattlePile)
        {
            return;
        }

        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        if (BattleNode.TryRestorePlayerTeamBattleHandCardAt(play.Index, play.Skill))
        {
            _queuedCardSkills.Remove(play.Skill);
            play.RemovedFromHand = false;
            RefreshTurnUi();
        }
    }

    private void ResolveBattlePileAfterQueuedPlay(QueuedCardPlay play)
    {
        if (
            play?.IsHandCard != true
            || play.ResolvedToBattlePile
            || play.Actor == null
            || !GodotObject.IsInstanceValid(play.Actor)
            || play.Skill == null
        )
        {
            return;
        }

        BattleNode?.DiscardBattleSkill(play.Actor, play.Skill);
        play.ResolvedToBattlePile = true;
    }

    private void RecoverInterruptedQueuedPlay(QueuedCardPlay play)
    {
        if (play == null)
            return;

        if (!play.ResolvedToBattlePile)
            RestoreHandSlotForQueuedPlay(play);

        if (
            play.IsHandCard
            && !play.ResolvedToBattlePile
            && GodotObject.IsInstanceValid(this)
            && IsInsideTree()
        )
        {
            ResetCardMotion(play.Index, instant: true);
        }

        QueueFreeQueuedPlayCard(play);
        CompleteQueuedPlay(play, succeeded: false);
    }

    private bool IsHandPlayStillValid(QueuedCardPlay play)
    {
        return play?.Actor != null
            && GodotObject.IsInstanceValid(play.Actor)
            && play.Actor.BattleNode != null
            && GodotObject.IsInstanceValid(play.Actor.BattleNode)
            && _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer);
    }

    private void CompleteQueuedPlay(QueuedCardPlay play, bool succeeded)
    {
        if (play?.IsHandCard == true)
            _queuedCardSkills.Remove(play.Skill);

        play?.Completion?.TrySetResult(succeeded);
    }

    private async Task PlayCardVanishAfterExecutionAsync(QueuedCardPlay play)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
            return;

        card.Button.Disabled = true;
        if (play?.Skill?.ExhaustsAfterUse == true)
            card.PlayExhaustEffect(CardPlayVanishDuration);
        else if (play?.IsHandCard == true || play?.FlyToDiscardPileAfterUse == true)
        {
            await PlayCardDiscardFlyAsync(card);
            return;
        }
        else
            card.PressEffect();
        await ToSignal(
            GetTree().CreateTimer(CardPlayVanishDuration),
            SceneTreeTimer.SignalName.Timeout
        );
    }

    public async Task PlayEndTurnHandDiscardAnimationsAsync(
        PlayerCharacter player,
        HashSet<int> handIndexes = null
    )
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        var animationTasks = new List<Task>();
        var discardEntries = new List<(int Index, SkillCard Card)>();
        var exhaustCards = new List<(int Index, SkillCard Card)>();

        for (int i = 0; i < _cards.Length; i++)
        {
            if (handIndexes != null && !handIndexes.Contains(i))
                continue;

            SkillCard sourceCard = _cards[i];
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
                continue;

            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;

            Skill skill = sourceCard.CurrentSkill;
            Skill[] hand = GetActiveHandSkills();
            if (skill == null && hand != null && i < hand.Length)
                skill = hand[i];

            if (skill?.ExhaustsAtTurnEndInHand == true)
            {
                SkillCard animationCard = CreateEndTurnHandDiscardAnimationCard(
                    i,
                    sourceCard,
                    skill,
                    PlayedCardZIndex + i + 1
                );
                if (animationCard != null)
                    exhaustCards.Add((i, animationCard));
                HideDiscardedHandCardAfterSelection(i, sourceCard);
                continue;
            }

            SkillCard discardCard = CreateEndTurnHandDiscardAnimationCard(
                i,
                sourceCard,
                skill,
                PlayedCardZIndex + i + 1
            );
            if (discardCard != null)
                discardEntries.Add((i, discardCard));
            HideDiscardedHandCardAfterSelection(i, sourceCard);
        }

        foreach (var entry in exhaustCards)
        {
            SkillCard card = entry.Card;
            card.PlayExhaustEffect(EndTurnCardVanishDuration);
            animationTasks.Add(FreeCardAfterDelayAsync(card, EndTurnCardVanishDuration));
        }

        for (int i = 0; i < discardEntries.Count; i++)
        {
            var entry = discardEntries[i];
            entry.Card.Button.Disabled = true;
            entry.Card.HoverHint.Visible = false;

            entry.Card.ZIndex = PlayedCardZIndex + i + 1;
            animationTasks.Add(PlayCardDiscardFlyAndFreeAsync(entry.Card));
        }

        if (animationTasks.Count > 0)
            await Task.WhenAll(animationTasks);
    }

    private SkillCard CreateEndTurnHandDiscardAnimationCard(
        int index,
        SkillCard sourceCard,
        Skill skill,
        int zIndex
    )
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (
            overlay == null
            || !GodotObject.IsInstanceValid(overlay)
            || sourceCard == null
            || !GodotObject.IsInstanceValid(sourceCard)
            || skill == null
        )
        {
            return null;
        }

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"EndTurnDiscardCard{index}";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.ConfigureDisplayScale(BattleCardScale);
        overlay.AddChild(card);
        card.ResetState();
        card.SetSkill(skill);
        card.CharacterName.Text = sourceCard.CharacterName.Text;
        card.Visible = true;
        card.GlobalPosition = sourceCard.GlobalPosition;
        card.Scale = sourceCard.Scale;
        card.Rotation = sourceCard.Rotation;
        card.PivotOffset = sourceCard.PivotOffset;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = zIndex;
        return card;
    }

    private async Task PlayCardDiscardFlyAndFreeAsync(SkillCard card)
    {
        try
        {
            await PlayCardDiscardFlyAsync(card);
        }
        finally
        {
            FreeAnimationCard(card);
        }
    }

    private async Task FreeCardAfterDelayAsync(SkillCard card, float delay)
    {
        try
        {
            if (delay > 0f && IsInsideTree())
            {
                await ToSignal(
                    GetTree().CreateTimer(delay),
                    SceneTreeTimer.SignalName.Timeout
                );
            }
        }
        finally
        {
            FreeAnimationCard(card);
        }
    }

    private static void FreeAnimationCard(SkillCard card)
    {
        if (card != null && GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    public async Task PlayTurnEndStatusTriggerAnimationAsync(
        PlayerCharacter player,
        int handIndex,
        Skill skill,
        Func<Task> triggerEffect
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || skill?.TriggersAtTurnEndInHand != true
        )
        {
            if (triggerEffect != null)
                await triggerEffect();
            return;
        }

        SkillCard card = null;
        bool isHandCard = false;
        if (CanAnimateHandCardsFor(player) && IsCardIndexValid(handIndex))
        {
            SkillCard sourceCard = _cards[handIndex];
            if (sourceCard != null && GodotObject.IsInstanceValid(sourceCard) && sourceCard.Visible)
            {
                card = sourceCard;
                isHandCard = true;
            }
        }

        if (isHandCard)
            _turnEndStatusTriggerCardIndexes.Add(handIndex);

        try
        {
            if (card != null)
            {
                var play = new QueuedCardPlay
                {
                    Actor = player,
                    Skill = skill,
                    Card = card,
                };
                card.SetEnergyCostText(I18n.Tr("ui.status.trigger", "触发"));
                card.CharacterName.Text = string.Empty;
                await MoveHandPlayCardToCenterAsync(play);
            }
            else
            {
                if (triggerEffect != null)
                    await triggerEffect();
                return;
            }

            if (triggerEffect != null)
                await triggerEffect();

            await PlayCardDiscardFlyAsync(card);
            if (isHandCard)
                HideTriggeredStatusHandCard(handIndex);
        }
        finally
        {
            if (isHandCard)
                _turnEndStatusTriggerCardIndexes.Remove(handIndex);
        }
    }

    private void HideTriggeredStatusHandCard(int handIndex)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        SkillCard card = _cards[handIndex];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.ResetState();
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        ResetCardMotion(handIndex, instant: true);
        card.Visible = false;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
    }

    private async Task PlayCardDiscardFlyAsync(SkillCard card)
    {
        await PlayCardFlyToPileAsync(card, _discardPileButton);
    }

    private async Task PlayCardFlyToPileAsync(SkillCard card, Button pileButton)
    {
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || pileButton == null
            || !GodotObject.IsInstanceValid(pileButton)
            || !pileButton.IsInsideTree()
        )
        {
            card?.PressEffect();
            await ToSignal(
                GetTree().CreateTimer(CardPlayVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
            return;
        }

        Rect2 startRect = card.GetGlobalRect();
        Vector2 startCenter = startRect.Position + startRect.Size * 0.5f;
        Vector2 endCenter = GetPileButtonVisualCenter(pileButton);
        if (startCenter.DistanceSquaredTo(endCenter) < 16f)
        {
            card.PressEffect();
            await ToSignal(
                GetTree().CreateTimer(CardPlayVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
            return;
        }

        card.PivotOffset = BattleCardBaseSize * 0.5f;
        Tween compressShaderTween = card.PressEffectPartial(
            centerVanish: 0.92f,
            glowMultiplier: 1.28f,
            duration: CardPlayDiscardCompressDuration
        );

        Tween compressTween = card.CreateTween();
        compressTween.SetParallel(true);
        compressTween
            .TweenProperty(
                card,
                "scale",
                Vector2.One * CardPlayDiscardCompressedScaleFactor,
                CardPlayDiscardCompressDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        compressTween
            .TweenMethod(
                Callable.From<float>(_ =>
                {
                    if (card == null || !GodotObject.IsInstanceValid(card))
                        return;

                    SetCardPivotCenterAt(card, startCenter);
                }),
                0f,
                1f,
                CardPlayDiscardCompressDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        compressTween.SetParallel(false);
        await Task.WhenAll(
            WaitForTweenFinishedAsync(compressTween),
            WaitForTweenFinishedAsync(compressShaderTween)
        );

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 flyStartCenter = startCenter;
        PrepareCardDiscardTrail(card, out Line trail, out GpuParticles2D particles);
        Vector2 control = GetRandomCardDiscardControlPoint(flyStartCenter, endCenter);
        Vector2 initialVelocity = GetQuadraticBezierVelocity(
            flyStartCenter,
            control,
            endCenter,
            0.01f
        );
        card.Rotation = GetRotationWithTopFacingVelocity(initialVelocity);
        UpdateTrailParticlesRotation(particles, initialVelocity);
        Tween flyShaderTween = card.PressEffectPartial(
            centerVanish: 0.9f,
            glowMultiplier: 1.18f,
            duration: CardPlayDiscardFlyDuration
        );

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(
                card,
                "scale",
                Vector2.One * CardPlayDiscardTargetScaleFactor,
                CardPlayDiscardFlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween
            .TweenMethod(
                Callable.From<float>(t =>
                {
                    if (card == null || !GodotObject.IsInstanceValid(card))
                        return;

                    Vector2 center = QuadraticBezier(flyStartCenter, control, endCenter, t);
                    Vector2 velocity = GetQuadraticBezierVelocity(
                        flyStartCenter,
                        control,
                        endCenter,
                        t
                    );
                    SetCardPivotCenterAt(card, center);
                    card.Rotation = GetRotationWithTopFacingVelocity(velocity);
                    UpdateTrailParticlesRotation(particles, velocity);
                }),
                0f,
                1f,
                CardPlayDiscardFlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.SetParallel(false);

        await Task.WhenAll(
            WaitForTweenFinishedAsync(tween),
            WaitForTweenFinishedAsync(flyShaderTween)
        );
        if (card != null && GodotObject.IsInstanceValid(card))
        {
            card.SetCardVisualVisible(false);
            card.Button.Disabled = true;
            card.HoverHint.Visible = false;
        }

        PulsePileButtonReceive(pileButton);
        await FadeAndHideCardDiscardTrailAsync(trail, particles);
    }

    private static void SetCardPivotCenterAt(SkillCard card, Vector2 center)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 currentPivotCenter = card.GetGlobalTransformWithCanvas() * card.PivotOffset;
        card.GlobalPosition += center - currentPivotCenter;
    }

    private static void PrepareCardDiscardTrail(
        SkillCard card,
        out Line trail,
        out GpuParticles2D particles
    )
    {
        trail = null;
        particles = null;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Node2D target = card.DiscardTrailTarget;
        trail = card.DiscardTrail;
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
        trail.ClearPoints();

        particles = card.DiscardTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        particles.Emitting = true;
    }

    private static void UpdateTrailParticlesRotation(GpuParticles2D particles, Vector2 velocity)
    {
        if (
            particles == null
            || !GodotObject.IsInstanceValid(particles)
            || velocity.LengthSquared() < 0.001f
        )
        {
            return;
        }

        particles.GlobalRotation = velocity.Angle() + Mathf.Pi;
    }

    private async Task FadeAndHideCardDiscardTrailAsync(Line trail, GpuParticles2D particles)
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
            CardPlayDiscardTrailFadeDuration
        );
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (trail != null && GodotObject.IsInstanceValid(trail))
                {
                    trail.Visible = false;
                    trail.ClearPoints();
                    trail.Modulate = Colors.White;
                    trail.Width = startWidth;
                    if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                        trail.Target.Visible = false;
                }

                HideCardTrailParticles(particles);
            })
        );

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private static void HideCardTrailParticles(GpuParticles2D particles)
    {
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Emitting = false;
        particles.Visible = false;
        particles.Modulate = Colors.White;
    }

    private static Vector2 GetRandomCardDiscardControlPoint(Vector2 start, Vector2 end)
    {
        Vector2 mid = (start + end) * 0.5f;
        float distance = start.DistanceTo(end);
        float lift =
            Math.Min(330f, Math.Max(120f, distance * 0.28f)) + (float)GD.RandRange(-28f, 52f);
        float side = end.X >= start.X ? 1f : -1f;
        float sideOffset = side * Math.Min(190f, distance * 0.16f) + (float)GD.RandRange(-90f, 90f);
        return mid + new Vector2(sideOffset, -lift);
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        Vector2 a = start.Lerp(control, t);
        Vector2 b = control.Lerp(end, t);
        return a.Lerp(b, t);
    }

    private static Vector2 GetQuadraticBezierVelocity(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return 2f * ((1f - t) * (control - start) + t * (end - control));
    }

    private static float GetRotationWithTopFacingVelocity(Vector2 velocity)
    {
        if (velocity.LengthSquared() < 0.001f)
            return 0f;

        return velocity.Angle() + Mathf.Pi * 0.5f;
    }

    private SkillCard CreateTemporaryPlayCard(
        Character actor,
        Skill skill,
        string tag,
        string nodeName
    )
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return null;

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = nodeName;
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.ConfigureDisplayScale(PlayedCardScale);
        overlay.AddChild(card);
        card.Visible = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.SetSkill(skill);
        card.SetEnergyCostCostText("0");
        card.CharacterName.Text = GetCharacterEnergyDisplayName(actor, tag);
        card.SetHandIndexBadge(0, visible: false);
        return card;
    }

    private CanvasLayer EnsureCardPlayOverlay()
    {
        Node parent = BattleNode ?? FindBattleNode() ?? GetParent();
        if (parent == null)
            return null;

        const string overlayName = "CardPlayOverlay";
        CanvasLayer overlay = parent.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null)
            return overlay;

        var root = GetTree()?.Root;
        overlay = root?.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null && overlay.GetParent() != parent)
        {
            overlay.Reparent(parent);
            return overlay;
        }

        overlay = new CanvasLayer { Name = overlayName };
        parent.AddChild(overlay);
        return overlay;
    }

    private CanvasLayer EnsureLiftedCardOverlay()
    {
        Node parent = BattleNode ?? FindBattleNode() ?? GetParent();
        if (parent == null)
            return null;

        const string overlayName = "LiftedCardOverlay";
        CanvasLayer overlay = parent.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null)
        {
            overlay.Layer = LiftedCardOverlayLayer;
            return overlay;
        }

        var root = GetTree()?.Root;
        overlay = root?.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null && overlay.GetParent() != parent)
        {
            overlay.Reparent(parent);
            overlay.Layer = LiftedCardOverlayLayer;
            return overlay;
        }

        overlay = new CanvasLayer
        {
            Name = overlayName,
            Layer = LiftedCardOverlayLayer,
        };
        parent.AddChild(overlay);
        return overlay;
    }

    private async Task ShowTemporaryCardAtCenterAsync(QueuedCardPlay play, string tag)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Visible = true;
        card.ResetState();
        card.SetSkill(play.Skill);
        card.SetEnergyCostCostText("0");
        card.CharacterName.Text = GetCharacterEnergyDisplayName(play.Actor, tag);
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = TemporaryCardZIndex;
        card.Scale = PlayedCardScale * TemporaryCardSpawnScaleMultiplier;
        card.GlobalPosition = GetScreenCenterCardPosition(card.Scale);
        card.StartAnimation();

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "scale", PlayedCardScale, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                card,
                "global_position",
                GetScreenCenterCardPosition(PlayedCardScale),
                CardPlayMoveDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task MoveHandPlayCardToCenterAsync(QueuedCardPlay play)
    {
        await MoveHandPlayCardToLayerAsync(play, 0);
    }

    private void RefreshQueuedPlayCardLayers()
    {
        var plays = new List<QueuedCardPlay>();
        if (_activeQueuedCardPlay?.IsHandCard == true)
            plays.Add(_activeQueuedCardPlay);

        plays.AddRange(_queuedCardPlays.Where(play => play?.IsHandCard == true));
        plays.AddRange(_queuedFollowUpCardPlays.Where(play => play?.IsHandCard == true));

        int layerIndex = 0;
        foreach (QueuedCardPlay play in plays)
        {
            if (
                play == null
                || play.Card == null
                || !GodotObject.IsInstanceValid(play.Card)
                || play.ResolvedToBattlePile
            )
            {
                continue;
            }

            RefreshQueuedPlayCardLayer(play, layerIndex++);
        }
    }

    private void RefreshQueuedPlayCardLayer(QueuedCardPlay play, int layerIndex)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        layerIndex = Math.Max(0, layerIndex);
        Vector2 targetScale = GetQueuedPlayCardLayerScale(layerIndex);
        Vector2 targetPosition = GetQueuedPlayCardLayerPosition(targetScale, layerIndex);
        bool sameTarget =
            play.MoveToCenterLayerIndex == layerIndex
            && play.MoveToCenterTargetPosition.HasValue
            && play.MoveToCenterTargetScale.HasValue
            && play.MoveToCenterTargetPosition.Value.DistanceSquaredTo(targetPosition) < 0.25f
            && play.MoveToCenterTargetScale.Value.DistanceSquaredTo(targetScale) < 0.0001f;

        if (sameTarget)
        {
            if (play.MoveToCenterTask != null && !play.MoveToCenterTask.IsCompleted)
                return;

            if (
                card.GlobalPosition.DistanceSquaredTo(targetPosition) < 0.25f
                && card.Scale.DistanceSquaredTo(targetScale) < 0.0001f
            )
            {
                play.MoveToCenterTask = Task.CompletedTask;
                return;
            }
        }

        play.MoveToCenterTask = MoveHandPlayCardToLayerAsync(play, layerIndex);
    }

    private async Task MoveHandPlayCardToLayerAsync(QueuedCardPlay play, int layerIndex)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        layerIndex = Math.Max(0, layerIndex);
        Vector2 targetScale = GetQueuedPlayCardLayerScale(layerIndex);
        Vector2 targetPosition = GetQueuedPlayCardLayerPosition(targetScale, layerIndex);
        play.MoveToCenterLayerIndex = layerIndex;
        play.MoveToCenterTargetPosition = targetPosition;
        play.MoveToCenterTargetScale = targetScale;

        card.Visible = true;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = GetQueuedPlayCardLayerZIndex(layerIndex);
        card.Modulate = GetQueuedPlayCardLayerModulate(layerIndex);
        card.StopBattleMotion();

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "scale", targetScale, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "global_position", targetPosition, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private static Vector2 GetQueuedPlayCardLayerScale(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        float scaleMultiplier = Math.Max(
            0.62f,
            1f - visibleLayerIndex * QueuedPlayedCardLayerScaleStep
        );
        return PlayedCardScale * scaleMultiplier;
    }

    private Vector2 GetQueuedPlayCardLayerPosition(Vector2 scale, int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        return GetScreenCenterCardPosition(scale)
            + new Vector2(0f, QueuedPlayedCardVerticalOffset)
            + QueuedPlayedCardLayerOffset * visibleLayerIndex;
    }

    private static int GetQueuedPlayCardLayerZIndex(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        return PlayedCardZIndex + QueuedPlayedCardVisibleLayers - visibleLayerIndex;
    }

    private static Color GetQueuedPlayCardLayerModulate(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        float alpha = Mathf.Clamp(1f - visibleLayerIndex * 0.1f, 0.68f, 1f);
        return new Color(1f, 1f, 1f, alpha);
    }

    private Vector2 GetScreenCenterCardPosition(Vector2 scale)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scaledSize = GetBattleCardScaledSize(scale);
        return viewportSize / 2f - scaledSize / 2f;
    }

    private static Vector2 GetBattleCardScaledSize(Vector2 scale)
    {
        return new Vector2(BattleCardBaseSize.X * scale.X, BattleCardBaseSize.Y * scale.Y);
    }

    private static Vector2 GetBattleCardCenterOffset(Vector2 scale)
    {
        return GetBattleCardScaledSize(scale) * 0.5f;
    }

    private static Vector2 GetSkillCardCenterOffset(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return Vector2.Zero;

        return GetBattleCardCenterOffset(card.Scale);
    }

    private static void QueueFreeTemporaryCard(SkillCard card)
    {
        if (card != null && GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    private void QueueFreeQueuedPlayCard(QueuedCardPlay play)
    {
        if (play?.IsHandCard == true)
            ReturnBattleCardToPool(play.Card);
        else if (play?.IsTemporaryCard == true)
            QueueFreeTemporaryCard(play.Card);
    }


}
