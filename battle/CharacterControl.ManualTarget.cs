using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void LiftCard(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        Skill skill = GetHandSkill(index);
        if (skill == null)
            return;

        SkillCard card = _cards[index];
        if (card == null || card.Button.Disabled || IsCardDrawEntryInputBlocked(index))
            return;

        DetachDrawEntryForInteraction(index, card);
        SuppressCardButtonPressForCurrentLeftPress();
        _liftedCardIndex = index;
        _liftedCardSkill = skill;
        _liftedCard = card;
        AudioManager.PlayCardPickup(card);
        _liftedCardMouseOffset = GetSkillCardCenterOffset(card);
        SetHandInputBlockerVisible(true);
        BlockOtherHandCardInputWhileLifted(index);

        if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            ResetCardMotion(_hoveredCardIndex, instant: false);
        _hoveredCardIndex = -1;

        card.StopBattleMotion();
        card.SetRelatedCardPreviewSuppressed(true);
        card.SetHoverUiEnabled(false);
        Vector2 liftedGlobalPosition = card.GlobalPosition;
        float liftedGlobalRotation = GetCanvasRotation(card);
        Vector2 liftedPivotCenter = card.GetGlobalTransformWithCanvas() * card.PivotOffset;
        Vector2 liftedScale = BattleCardScale * CardHoverScaleMultiplier;

        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = null;
            _cardSlotLayoutRotationTargets[index] = null;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
            slot.Rotation = 0f;
        }

        CanvasLayer overlay = EnsureLiftedCardOverlay();
        if (overlay != null && GodotObject.IsInstanceValid(overlay))
        {
            card.PreserveSkillPreviewOnNextReparent();
            card.GetParent()?.RemoveChild(card);
            overlay.AddChild(card);
            card.GlobalPosition = liftedGlobalPosition;
            card.Rotation = liftedGlobalRotation;
            card.Scale = liftedScale;
            SetCardPivotCenterAt(card, liftedPivotCenter);
            _liftedCardMouseOffset = GetBattleCardCenterOffset(liftedScale);
        }

        card.HoverHint.Visible = false;
        card.ZIndex = LiftedCardZIndex;
        SetProcess(true);
        LayoutActionCards(instant: false);
    }

    private void ClearLiftedCard(bool instant)
    {
        if (_liftedCardIndex == -1)
        {
            UpdateProcessState();
            return;
        }

        int index = _liftedCardIndex;
        Skill[] hand = GetActiveHandSkills();
        Skill skill = _liftedCardSkill;
        if (skill == null && hand != null && index < hand.Length)
            skill = hand[index];
        skill?.ClearManualFriendlyTarget();
        SyncLiftedSlotPositionToCard(index);
        _liftedCardIndex = -1;
        _liftedCardSkill = null;
        SkillCard liftedCard = _liftedCard;
        _liftedCard = null;
        _manualTargetArrowUsesLiftedCard = false;
        if (liftedCard != null && GodotObject.IsInstanceValid(liftedCard))
            liftedCard.SetRelatedCardPreviewSuppressed(false);
        ClearCardButtonPressSuppression();
        SuppressHandHoverUntilMouseMove(requireMouseMove: true);
        SetHandInputBlockerVisible(false);
        if (!_manualTargetArrowSelectionActive)
            SetCardHoverUiEnabled(true);
        _cardSlotLayoutPixelsPerSecondOverrides[index] = instant
            ? 0f
            : Math.Max(HandLayoutPixelsPerSecond, HandDroppedCardReturnPixelsPerSecond);
        UpdateProcessState();
        ResetCardMotion(index, instant, HandDroppedCardReturnMotionDuration);
        LayoutActionCards(instant);
        RequestTurnUiRefresh(refreshHover: true);
        ScheduleLiftedCardDropHoverRefresh(instant);
    }

    private void ScheduleLiftedCardDropHoverRefresh(bool instant)
    {
        int version = ++_liftedCardDropHoverRefreshVersion;
        _ = RefreshHoverAfterLiftedCardDropAsync(version, instant);
    }

    private async Task RefreshHoverAfterLiftedCardDropAsync(int version, bool instant)
    {
        SceneTree tree = GetTree();
        if (tree == null)
            return;

        if (!instant && HandDroppedCardReturnMotionDuration > 0f)
            await ToSignal(
                tree.CreateTimer(HandDroppedCardReturnMotionDuration),
                SceneTreeTimer.SignalName.Timeout
            );
        else
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        if (
            version != _liftedCardDropHoverRefreshVersion
            || !GodotObject.IsInstanceValid(this)
            || !_uiBuilt
            || !Visible
            || _endTurnQueued
            || _liftedCardIndex != -1
            || _manualTargetArrowSelectionActive
            || IsManualTargetSelectionPending()
        )
        {
            return;
        }

        _suppressHandHoverUntilMouseMove = false;
        _handHoverSuppressionRequiresMouseMove = false;
        ScheduleCardHoverRefresh();
    }

    private void ReleaseLiftedCardForManualTargetSelection(int index)
    {
        if (_liftedCardIndex != index)
            return;

        _manualTargetArrowUsesLiftedCard = true;
        ClearCardButtonPressSuppression();
        SetHandInputBlockerVisible(false);
        SetCardHoverUiEnabled(false);
        UpdateProcessState();
    }

    private void PrepareManualTargetArrowLiftedCard(int index, Skill skill)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_liftedCardIndex != index)
            LiftCard(index);

        if (_liftedCardIndex != index)
            return;

        SkillCard card = _liftedCard ?? _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        _liftedCardSkill = skill;
        _liftedCard = card;
        _manualTargetArrowUsesLiftedCard = true;
        _manualTargetArrowCardIndex = index;
        _liftedCardMouseOffset = GetSkillCardCenterOffset(card);
        ClearCardButtonPressSuppression();
        SetHandInputBlockerVisible(true);
        SuppressHandInteractionForManualTargetSelection();
        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        card.HoverHint.Visible = false;
        card.ZIndex = LiftedCardZIndex;
        SetProcess(true);
    }

    private void SuppressHandInteractionForManualTargetSelection()
    {
        SetCardHoverUiEnabled(false);
        HideAllCardHoverPreviews();

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            card.HideHoverUi();
            SetCardButtonInputEnabled(card, false);
        }

        if (_endTurnButton != null)
            _endTurnButton.Disabled = true;
    }

    private void HideMouseForManualTargetArrowSelection()
    {
        if (MobilePlatform.IsTouchPrimary || _manualTargetArrowMouseHidden)
            return;

        _manualTargetArrowPreviousMouseMode = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        _manualTargetArrowMouseHidden = true;
    }

    private void RestoreMouseAfterManualTargetArrowSelection()
    {
        if (!_manualTargetArrowMouseHidden)
            return;

        Input.MouseMode = _manualTargetArrowPreviousMouseMode;
        _manualTargetArrowMouseHidden = false;
    }

    private void BlockOtherHandCardInputWhileLifted(int liftedIndex)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == liftedIndex)
                continue;

            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            ClearHandCardHoverMotion(i, instant: true);
            card.SetHoverUiEnabled(false);
            SetCardButtonInputEnabled(card, false);
        }
    }

    private void SyncLiftedSlotPositionToCard(int index)
    {
        if (!IsCardIndexValid(index) || _cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        Control slot = _cardSlots[index];
        SkillCard card = _liftedCard ?? _cards[index];
        if (
            slot == null
            || !GodotObject.IsInstanceValid(slot)
            || card == null
            || !GodotObject.IsInstanceValid(card)
        )
        {
            return;
        }

        Vector2 pivotCenter = card.GetGlobalTransformWithCanvas() * card.PivotOffset;
        slot.GlobalPosition = pivotCenter - card.PivotOffset;
        if (card.GetParent() != slot)
        {
            card.PreserveSkillPreviewOnNextReparent();
            card.GetParent()?.RemoveChild(card);
            slot.AddChild(card);
        }

        card.Position = Vector2.Zero;
        card.Rotation = 0f;
        SetCardPivotCenterAt(card, pivotCenter);
        _cards[index] = card;
        WireBattleCard(card, index);
    }

    private async Task<bool> SelectManualFriendlyTargetIfNeededAsync(QueuedCardPlay play)
    {
        if (play?.Skill?.RequiresManualFriendlyTarget() != true)
            return true;

        if (play.Skill.HasManualFriendlyTarget())
            return true;

        Character[] candidates = GetManualFriendlyTargetCandidates(play.Skill);
        if (candidates.Length == 0)
            return true;

        if (play.IsTemporaryCard)
        {
            if (play.Skill.TryGetManualFriendlyCarrySkillType(out Skill.SkillTypes carrySkillType))
            {
                Character[] drawableCandidates = GetDrawableManualFriendlyTargetCandidates(
                    candidates,
                    carrySkillType
                );
                if (drawableCandidates.Length > 0)
                    candidates = drawableCandidates;
            }

            Random rng = BattleNode?.BattleIntentionRandom ?? new Random();
            Character randomTarget = candidates[rng.Next(candidates.Length)];
            play.Skill.SetManualFriendlyTarget(randomTarget);
            return play.Skill.HasManualFriendlyTarget();
        }

        Character target = await ShowManualTargetArrowPickerAsync(play.Skill, play.Card);
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        play.Skill.SetManualFriendlyTarget(target);
        return play.Skill.HasManualFriendlyTarget();
    }

    private bool HasManualFriendlyTargetCandidates(Skill skill)
    {
        return GetManualFriendlyTargetCandidates(skill).Length > 0;
    }

    private static bool ShouldUseManualTargetArrowSelection(Skill skill)
    {
        return skill?.RequiresManualFriendlyTarget() == true;
    }

    private Character[] GetManualFriendlyTargetCandidates(Skill skill)
    {
        if (skill?.OwnerCharater == null || BattleNode == null)
            return Array.Empty<Character>();

        bool excludeSelf = skill.ManualFriendlyTargetExcludesSelf();
        bool allowDying = skill.ManualFriendlyTargetAllowsDying();
        Character owner = skill.OwnerCharater;
        var team = BattleNode.GetTeamCharacters(skill.OwnerCharater.IsPlayer, includeSummons: true);
        if (team == null)
            return Array.Empty<Character>();

        int count = 0;
        foreach (Character character in team)
        {
            if (IsValidManualFriendlyTargetCandidate(character, owner, excludeSelf, allowDying))
                count++;
        }
        if (count == 0)
            return Array.Empty<Character>();

        var candidates = new Character[count];
        int write = 0;
        foreach (Character character in team)
        {
            if (IsValidManualFriendlyTargetCandidate(character, owner, excludeSelf, allowDying))
                candidates[write++] = character;
        }

        if (skill.TryGetManualFriendlyCarrySkillType(out Skill.SkillTypes carrySkillType))
            candidates = GetDrawableManualFriendlyTargetCandidates(candidates, carrySkillType);

        Array.Sort(candidates, CompareTargetPosition);
        return candidates;
    }

    private static bool IsValidManualFriendlyTargetCandidate(
        Character character,
        Character owner,
        bool excludeSelf,
        bool allowDying
    )
    {
        return character != null
            && GodotObject.IsInstanceValid(character)
            && (allowDying || character.State != Character.CharacterState.Dying)
            && (!excludeSelf || !ReferenceEquals(character, owner));
    }

    private Character[] GetDrawableManualFriendlyTargetCandidates(
        Character[] candidates,
        Skill.SkillTypes carrySkillType
    )
    {
        if (candidates == null || candidates.Length == 0)
            return Array.Empty<Character>();

        int count = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (
                candidates[i] is PlayerCharacter player
                && BattleNode?.HasDrawablePlayerCarrySkill(player, carrySkillType) == true
            )
            {
                count++;
            }
        }
        if (count == 0)
            return Array.Empty<Character>();

        var drawableCandidates = new Character[count];
        int write = 0;
        for (int i = 0; i < candidates.Length; i++)
        {
            if (
                candidates[i] is PlayerCharacter player
                && BattleNode?.HasDrawablePlayerCarrySkill(player, carrySkillType) == true
            )
            {
                drawableCandidates[write++] = candidates[i];
            }
        }

        return drawableCandidates;
    }

    private void EnsureManualTargetArrowUi()
    {
        if (_manualTargetArrowRoot != null && GodotObject.IsInstanceValid(_manualTargetArrowRoot))
            return;
        CanvasLayer overlay = EnsureLiftedCardOverlay();
        if (overlay == null)
            return;
        _manualTargetArrowRoot = overlay.GetNode<Control>("ManualTargetArrowPicker");
        _manualTargetArrowMask = _manualTargetArrowRoot.GetNode<ColorRect>("InputMask");
        _manualTargetArrowLayer = _manualTargetArrowRoot.GetNode<ManualTargetArrowView>("Arrow");
        _manualTargetArrowHintLabel = _manualTargetArrowRoot.GetNode<Label>("Hint");
        _manualTargetArrowMask.GuiInput += OnManualTargetArrowMaskGuiInput;
        var cancelButton = _manualTargetArrowRoot.GetNode<Button>("CancelButton");
        cancelButton.Visible = MobilePlatform.IsMobile;
        cancelButton.Text = I18n.Tr("ui.common.cancel", "取消");
        cancelButton.Pressed += () => HideManualTargetPicker();
    }

    private async Task<Character> ShowManualTargetArrowPickerAsync(
        Skill skill,
        SkillCard playedCard = null,
        int sourceCardIndex = -1
    )
    {
        if (skill?.OwnerCharater == null || BattleNode == null)
            return null;

        EnsureManualTargetArrowUi();
        if (_manualTargetArrowRoot == null || _manualTargetArrowLayer == null)
            return null;

        Character owner = skill.OwnerCharater;
        Character[] targets = GetManualFriendlyTargetCandidates(skill);
        if (targets.Length == 0)
            return null;
        ShowManualRebirthTargetPreviews(skill, targets);

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        _manualTargetArrowTargets = targets;
        _manualTargetArrowOwner = owner;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = skill;
        _manualTargetArrowCardIndex = sourceCardIndex;
        _manualTargetArrowSourcePosition = null;
        _manualTargetArrowSourceTangent = null;
        _manualTargetArrowStatusText = "选择一名己方角色";
        _manualTargetArrowSelectionActive = true;
        _manualTargetArrowPlayedCard = playedCard;
        SuppressHandInteractionForManualTargetSelection();
        _hoveredCardIndex = -1;
        ShowCardEnergyPreview(skill);
        _manualTargetArrowRoot.Visible = true;
        HideMouseForManualTargetArrowSelection();
        if (_manualTargetArrowMask != null)
            _manualTargetArrowMask.MouseFilter = MouseFilterEnum.Ignore;
        if (_manualTargetArrowHintLabel != null)
            _manualTargetArrowHintLabel.Text = _manualTargetArrowStatusText;
        _statusLabel.Text = "选择一名己方角色";
        SetManualTargetArrowHoveredTarget(GetDefaultManualTargetArrowTarget(owner, targets));
        ShowCardEnergyPreview(skill);
        if (sourceCardIndex >= 0 && IsCardIndexValid(sourceCardIndex))
            ApplyManualTargetArrowCardVisual(playedCard ?? _cards[sourceCardIndex]);
        UpdateManualTargetArrowVisual(owner, playedCard);

        try
        {
            while (!completion.Task.IsCompleted && IsManualTargetArrowContextValid(skill, owner))
            {
                UpdateManualTargetArrowVisual(owner, playedCard);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            return completion.Task.IsCompleted ? await completion.Task : null;
        }
        finally
        {
            if (_manualTargetCompletion == completion)
                _manualTargetCompletion = null;
            bool confirmedTarget =
                completion.Task.IsCompletedSuccessfully
                && completion.Task.Result != null
                && GodotObject.IsInstanceValid(completion.Task.Result);
            HideManualTargetPicker(resetArrowCardVisual: !confirmedTarget);
        }
    }

    public async Task<Character> PickItemTargetAsync(Control sourceControl = null)
    {
        if (BattleNode == null)
            return null;

        Character[] targets = GetItemTargetCandidates();

        if (targets.Length == 0)
            return null;

        return await ShowItemTargetArrowPickerAsync(
            targets,
            GetManualTargetArrowSourcePosition(sourceControl),
            GetManualTargetArrowSourceTangent(sourceControl),
            "选择道具目标"
        );
    }

    private async Task<Character> ShowItemTargetArrowPickerAsync(
        Character[] targets,
        Vector2 sourcePosition,
        Vector2 sourceTangent,
        string statusText
    )
    {
        if (targets == null || targets.Length == 0 || BattleNode == null)
            return null;

        EnsureManualTargetArrowUi();
        if (_manualTargetArrowRoot == null || _manualTargetArrowLayer == null)
            return null;

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        _manualTargetArrowTargets = targets;
        _manualTargetArrowOwner = null;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = null;
        _manualTargetArrowCardIndex = -1;
        _manualTargetArrowSourcePosition = sourcePosition;
        _manualTargetArrowSourceTangent = sourceTangent;
        _manualTargetArrowStatusText = statusText;
        _manualTargetArrowSelectionActive = true;
        _manualTargetArrowPlayedCard = null;
        SuppressHandInteractionForManualTargetSelection();
        _manualTargetArrowRoot.Visible = true;
        HideMouseForManualTargetArrowSelection();
        if (_manualTargetArrowMask != null)
            _manualTargetArrowMask.MouseFilter = MouseFilterEnum.Ignore;
        if (_manualTargetArrowHintLabel != null)
            _manualTargetArrowHintLabel.Text = statusText;
        _statusLabel.Text = statusText;
        SetManualTargetArrowHoveredTarget(GetDefaultManualTargetArrowTarget(null, targets));
        UpdateManualTargetArrowVisual();

        try
        {
            while (!completion.Task.IsCompleted && IsManualTargetArrowContextValid())
            {
                UpdateManualTargetArrowVisual();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            return completion.Task.IsCompleted ? await completion.Task : null;
        }
        finally
        {
            if (_manualTargetCompletion == completion)
                _manualTargetCompletion = null;
            HideManualTargetPicker();
        }
    }

    private bool IsManualTargetSelectionPending()
    {
        return _manualTargetCompletion != null && !_manualTargetCompletion.Task.IsCompleted;
    }

    private bool IsCardQueueBusy()
    {
        return _isProcessingCardQueue
            || _queuedCardPlays.Count > 0
            || _queuedFollowUpCardPlays.Count > 0;
    }

    private bool IsManualTargetArrowContextValid(Skill skill, Character owner)
    {
        return skill != null && IsManualTargetArrowContextValid(owner, requireLivingOwner: true);
    }

    private bool IsManualTargetArrowContextValid(
        Character owner = null,
        bool requireLivingOwner = false
    )
    {
        return IsInsideTree()
            && _manualTargetArrowRoot != null
            && GodotObject.IsInstanceValid(_manualTargetArrowRoot)
            && _manualTargetArrowRoot.Visible
            && _manualTargetArrowSelectionActive
            && (
                !requireLivingOwner
                || (
                    owner != null
                    && GodotObject.IsInstanceValid(owner)
                    && owner.State != Character.CharacterState.Dying
                )
            )
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode)
            && BattleNode.ShouldAbortSkillResolution() != true;
    }

    private void UpdateManualTargetArrowVisual()
    {
        UpdateManualTargetArrowVisual(_manualTargetArrowOwner, _manualTargetArrowPlayedCard);
    }

    private void UpdateManualTargetArrowVisual(Character owner, SkillCard playedCard)
    {
        if (
            !_manualTargetArrowSelectionActive
            || _manualTargetArrowLayer == null
            || !GodotObject.IsInstanceValid(_manualTargetArrowLayer)
        )
        {
            return;
        }

        Vector2 mousePosition = GetViewport().GetMousePosition();
        if (_statusLabel != null && _manualTargetArrowSelectionActive)
            _statusLabel.Text = _manualTargetArrowStatusText;
        Vector2 startPosition = GetManualTargetArrowStartPosition(
            _manualTargetArrowSourcePosition,
            owner,
            playedCard
        );
        _manualTargetArrowLayer.SetEndpoints(
            startPosition,
            mousePosition,
            GetManualTargetArrowStartTangent(startPosition, mousePosition, owner, playedCard),
            GetManualTargetArrowEndTangent(startPosition, mousePosition)
        );
        Character hoveredTarget = ResolveManualTargetArrowHoveredTarget(mousePosition);
        if (hoveredTarget != null)
            SetManualTargetArrowHoveredTarget(hoveredTarget);
    }

    public void NotifyManualTargetHover(Character target, bool hovered)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return;

        Character nextHoveredTarget = hovered ? target : null;
        if (!hovered && _manualTargetArrowHoveredTarget != target)
            return;

        SetManualTargetArrowHoveredTarget(nextHoveredTarget);
    }

    public bool TrySelectManualTargetFromCharacter(Character target)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return false;

        SetManualTargetArrowHoveredTarget(target);
        target.PlayTargetLockPulse(ManualTargetHoveredColor, 1.1f);
        _manualTargetCompletion?.TrySetResult(target);
        return true;
    }

    private void SetManualTargetArrowHoveredTarget(Character target)
    {
        if (target != null && !IsManualTargetArrowCandidate(target))
            target = null;

        if (_manualTargetArrowHoveredTarget == target)
            return;

        _manualTargetArrowHoveredTarget = target;
        if (_manualTargetArrowLayer != null && GodotObject.IsInstanceValid(_manualTargetArrowLayer))
            _manualTargetArrowLayer.SetHeadHighlighted(_manualTargetArrowHoveredTarget != null);
        RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
        RefreshManualTargetArrowEffectPreview(_manualTargetArrowHoveredTarget);
    }

    private bool TryHandleManualTargetArrowKeyInput(InputEventKey key)
    {
        if (!_manualTargetArrowSelectionActive)
            return false;

        Key keycode = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (keycode == Key.Enter || keycode == Key.KpEnter || keycode == Key.Space)
        {
            if (_manualTargetArrowHoveredTarget == null)
                SetManualTargetArrowHoveredTarget(
                    GetDefaultManualTargetArrowTarget(
                        _manualTargetArrowOwner,
                        _manualTargetArrowTargets
                    )
                );

            if (_manualTargetArrowHoveredTarget != null)
            {
                _manualTargetArrowHoveredTarget.PlayTargetLockPulse(ManualTargetHoveredColor, 1.1f);
                _manualTargetCompletion?.TrySetResult(_manualTargetArrowHoveredTarget);
                return true;
            }

            return false;
        }

        int direction = keycode switch
        {
            Key.Left or Key.Up => -1,
            Key.Right or Key.Down => 1,
            _ => 0,
        };
        if (direction == 0)
            return false;

        Character nextTarget = GetNextManualTargetArrowTarget(direction);
        if (nextTarget == null)
            return false;

        SetManualTargetArrowHoveredTarget(nextTarget);
        UpdateManualTargetArrowVisual();
        return true;
    }

    private Character GetNextManualTargetArrowTarget(int direction)
    {
        Character[] candidates = _manualTargetArrowTargets ?? Array.Empty<Character>();
        if (!HasValidTargetCandidate(candidates))
            return null;

        Character current = _manualTargetArrowHoveredTarget;
        int currentIndex = Array.IndexOf(candidates, current);
        if (currentIndex < 0)
            return GetDefaultManualTargetArrowTarget(_manualTargetArrowOwner, candidates);

        for (int offset = 1; offset <= candidates.Length; offset++)
        {
            int nextIndex =
                (currentIndex + direction * offset + candidates.Length) % candidates.Length;
            Character target = candidates[nextIndex];
            if (target != null && GodotObject.IsInstanceValid(target))
                return target;
        }

        return null;
    }

    private static Character GetDefaultManualTargetArrowTarget(Character owner, Character[] targets)
    {
        if (targets == null || targets.Length == 0)
            return null;

        Character best = null;
        bool hasValidOwner = owner != null && GodotObject.IsInstanceValid(owner);
        int bestDistance = int.MaxValue;
        int bestPosition = int.MaxValue;
        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            int distance = hasValidOwner
                ? Math.Abs(target.PositionIndex - owner.PositionIndex)
                : 0;
            int position = target.PositionIndex;
            if (best == null || distance < bestDistance || (distance == bestDistance && position < bestPosition))
            {
                best = target;
                bestDistance = distance;
                bestPosition = position;
            }
        }

        return best;
    }

    private static bool HasValidTargetCandidate(Character[] candidates)
    {
        if (candidates == null)
            return false;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && GodotObject.IsInstanceValid(candidates[i]))
                return true;
        }

        return false;
    }

    private Character[] GetItemTargetCandidates()
    {
        if (BattleNode == null)
            return Array.Empty<Character>();

        var playerTeam = BattleNode.GetTeamCharacters(isPlayer: true, includeSummons: true);
        var enemyTeam = BattleNode.GetTeamCharacters(isPlayer: false, includeSummons: true);
        int count = CountValidCharacters(playerTeam) + CountValidCharacters(enemyTeam);
        if (count == 0)
            return Array.Empty<Character>();

        var targets = new Character[count];
        int write = 0;
        AppendValidCharacters(playerTeam, targets, ref write);
        AppendValidCharacters(enemyTeam, targets, ref write);
        Array.Sort(targets, CompareItemTargetOrder);
        return targets;
    }

    private static int CountValidCharacters(IEnumerable<Character> characters)
    {
        if (characters == null)
            return 0;

        int count = 0;
        foreach (Character character in characters)
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                count++;
        }

        return count;
    }

    private static void AppendValidCharacters(
        IEnumerable<Character> source,
        Character[] target,
        ref int write
    )
    {
        if (source == null || target == null)
            return;

        foreach (Character character in source)
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                target[write++] = character;
        }
    }

    private static int CompareTargetPosition(Character a, Character b)
    {
        int aPosition = a?.PositionIndex ?? int.MaxValue;
        int bPosition = b?.PositionIndex ?? int.MaxValue;
        return aPosition.CompareTo(bPosition);
    }

    private static int CompareItemTargetOrder(Character a, Character b)
    {
        int teamCompare = GetItemTargetTeamOrder(a).CompareTo(GetItemTargetTeamOrder(b));
        if (teamCompare != 0)
            return teamCompare;

        return CompareTargetPosition(a, b);
    }

    private static int GetItemTargetTeamOrder(Character character) =>
        character?.IsPlayer == true ? 0 : 1;

    private Character ResolveManualTargetArrowHoveredTarget(Vector2 mousePosition)
    {
        Character[] candidates = _manualTargetArrowTargets ?? Array.Empty<Character>();
        Character bestTarget = null;
        float bestDistanceSquared = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Character target = candidates[i];
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || !IsMouseOverManualTargetCandidate(target, mousePosition)
            )
            {
                continue;
            }

            Vector2 center = GetManualTargetCandidateScreenCenter(target);
            float distanceSquared = center.DistanceSquaredTo(mousePosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestTarget = target;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestTarget;
    }

    private static bool IsMouseOverManualTargetCandidate(Character target, Vector2 mousePosition)
    {
        if (target?.Hoverframe != null && GodotObject.IsInstanceValid(target.Hoverframe))
        {
            Rect2 rect = target.Hoverframe.GetGlobalRect().Grow(10f);
            if (rect.HasPoint(mousePosition))
                return true;
        }

        return GetManualTargetCandidateScreenCenter(target).DistanceSquaredTo(mousePosition)
            <= 140f * 140f;
    }

    private static Vector2 GetManualTargetCandidateScreenCenter(Character target)
    {
        if (target?.Hoverframe != null && GodotObject.IsInstanceValid(target.Hoverframe))
            return target.Hoverframe.GetGlobalRect().GetCenter();

        return target?.GetGlobalTransformWithCanvas().Origin ?? Vector2.Zero;
    }

    private bool IsManualTargetArrowCandidate(Character target)
    {
        return target != null
            && GodotObject.IsInstanceValid(target)
            && ContainsCharacterReference(_manualTargetArrowTargets, target);
    }

    private void ApplyManualTargetArrowCardVisual(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 targetScale = BattleCardScale * ManualTargetCenteredCardScaleMultiplier;
        int index = GetBattleCardSignalIndex(card);
        card.HoverHint.Visible = false;
        card.ZIndex =
            _manualTargetArrowUsesLiftedCard
            && index == _manualTargetArrowCardIndex
            && card.GetParent() is CanvasLayer
                ? LiftedCardZIndex
                : 12;
        card.Modulate = SkillButton.EnabledModulate;
        card.TweenBattleMotion(
            GetManualTargetCardCenteredMotionPosition(card, targetScale),
            targetScale,
            ManualTargetCenteredCardMoveDuration
        );
    }

    private Vector2 GetManualTargetCardCenteredMotionPosition(SkillCard card, Vector2 targetScale)
    {
        int index = card != null ? GetBattleCardSignalIndex(card) : -1;
        if (
            _manualTargetArrowUsesLiftedCard
            && IsCardIndexValid(index)
            && index == _manualTargetArrowCardIndex
            && card.GetParent() is CanvasLayer
        )
        {
            return GetManualTargetLiftedCardPosition(targetScale);
        }

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return new Vector2(0f, CardHoverLiftY);

        Vector2 targetGlobalPosition = GetHandAreaCenterCardPosition(targetScale);
        return targetGlobalPosition - slot.GlobalPosition;
    }

    private Vector2 GetManualTargetLiftedCardPosition(Vector2 scale)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scaledSize = GetBattleCardScaledSize(scale);

        float x = viewportSize.X * 0.5f - scaledSize.X * 0.5f;
        float y = viewportSize.Y - scaledSize.Y;
        return new Vector2(x, y);
    }

    private Vector2 GetHandAreaCenterCardPosition(Vector2 scale)
    {
        Rect2 handRect =
            _cardRow != null && GodotObject.IsInstanceValid(_cardRow)
                ? _cardRow.GetGlobalRect()
                : GetViewport().GetVisibleRect();

        return handRect.GetCenter() - GetBattleCardCenterOffset(scale);
    }

    private static Vector2 GetManualTargetArrowStartPosition(Character owner, SkillCard playedCard)
    {
        return GetManualTargetArrowStartPosition(null, owner, playedCard);
    }

    private static Vector2 GetManualTargetArrowStartPosition(
        Vector2? sourcePosition,
        Character owner,
        SkillCard playedCard
    )
    {
        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
            return playedCard.GetGlobalRect().GetCenter();

        if (sourcePosition.HasValue)
            return sourcePosition.Value;

        if (owner != null && GodotObject.IsInstanceValid(owner))
            return owner.GetGlobalTransformWithCanvas().Origin;

        return Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowSourcePosition(Control sourceControl)
    {
        if (sourceControl != null && GodotObject.IsInstanceValid(sourceControl))
            return sourceControl.GetGlobalRect().GetCenter();

        return GetViewport()?.GetMousePosition() ?? Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowSourceTangent(Control sourceControl)
    {
        if (sourceControl != null && GodotObject.IsInstanceValid(sourceControl))
            return GetPointToViewportCenterTangent(
                sourceControl.GetGlobalRect().GetCenter(),
                Vector2.Up
            );

        return Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowStartTangent(
        Vector2 startPosition,
        Vector2 endPosition,
        Character owner,
        SkillCard playedCard
    )
    {
        if (_manualTargetArrowSourceTangent.HasValue)
            return GetSafeDirection(
                _manualTargetArrowSourceTangent.Value,
                endPosition - startPosition
            );

        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
            return GetManualTargetArrowSourceTangent(playedCard);

        if (owner != null && GodotObject.IsInstanceValid(owner))
            return GetPointToViewportCenterTangent(startPosition, endPosition - startPosition);

        return GetSafeDirection(endPosition - startPosition, Vector2.Right);
    }

    private static Vector2 GetManualTargetArrowEndTangent(
        Vector2 startPosition,
        Vector2 endPosition
    )
    {
        return GetSafeDirection(endPosition - startPosition, Vector2.Right);
    }

    private Vector2 GetPointToViewportCenterTangent(Vector2 point, Vector2 fallback)
    {
        Viewport viewport = GetViewport();
        if (viewport == null)
            return GetSafeDirection(fallback, Vector2.Up);

        Vector2 viewportCenter = viewport.GetVisibleRect().Size * 0.5f;
        return GetSafeDirection(viewportCenter - point, fallback);
    }

    private static Vector2 GetSafeDirection(Vector2 value, Vector2 fallback)
    {
        if (value.LengthSquared() >= 0.01f)
            return value.Normalized();

        if (fallback.LengthSquared() >= 0.01f)
            return fallback.Normalized();

        return Vector2.Right;
    }

    private void RefreshManualTargetArrowPreviews(Character hoveredTarget)
    {
        foreach (Character target in _manualTargetArrowTargets ?? Array.Empty<Character>())
        {
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            target.ShowTargetPreview(
                target == hoveredTarget ? ManualTargetHoveredColor : ManualTargetCandidateColor,
                animate: false
            );
        }
    }

    private void HideManualTargetArrowPreviews()
    {
        foreach (Character target in _manualTargetArrowTargets ?? Array.Empty<Character>())
        {
            if (target != null && GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }
    }

    private void RefreshManualTargetArrowEffectPreview(Character hoveredTarget)
    {
        HideManualTargetArrowEffectPreview();
        if (!_manualTargetArrowSelectionActive || _manualTargetArrowSkill == null)
        {
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
            return;
        }

        if (hoveredTarget != null && GodotObject.IsInstanceValid(hoveredTarget))
            _manualTargetArrowSkill.SetManualFriendlyTarget(hoveredTarget);
        else
            _manualTargetArrowSkill.ClearManualFriendlyTarget();

        if (hoveredTarget != null && !_manualTargetArrowSkill.HasManualFriendlyTarget())
            return;

        CardEffectPreviewSnapshot snapshot = CardEffectPreviewEcs.GetSnapshot(
            _manualTargetArrowSkill
        );
        var entries = snapshot.EffectEntries;
        if (entries == null || entries.Length == 0)
            return;

        ShowManualTargetArrowEffectTargetPreviews(entries);

        CanvasLayer layer = EnsureCardPlayOverlay();
        if (layer == null)
            return;

        int panelIndex = 0;
        CardPreviewTargetEffectGroup[] groups =
            snapshot.EffectGroupsByTarget ?? Array.Empty<CardPreviewTargetEffectGroup>();
        for (int i = 0; i < groups.Length; i++)
        {
            CardPreviewTargetEffectGroup group = groups[i];
            Character target = group.Target;
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || group.Entries == null
                || group.Entries.Length == 0
            )
            {
                continue;
            }

            VBoxContainer panel = GetOrCreateManualTargetArrowDamagePanel(layer, panelIndex++);
            PreviewEffectDisplay.ShowTargetEffectPanel(
                panel,
                group.Entries,
                GetTargetScreenPosition(target)
            );
        }

        for (int i = panelIndex; i < _manualTargetArrowDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_manualTargetArrowDamagePanels[i]))
                _manualTargetArrowDamagePanels[i].Visible = false;
        }
    }

    private void BuildManualTargetArrowDamageEntryGroups(Skill.PreviewEffectEntry[] entries)
    {
        _manualTargetArrowDamageTargetsBuffer.Clear();
        foreach (
            List<Skill.PreviewEffectEntry> targetEntries in _manualTargetArrowDamageEntriesByTarget
                .Values
        )
        {
            targetEntries.Clear();
        }

        for (int i = 0; i < entries.Length; i++)
        {
            Skill.PreviewEffectEntry entry = entries[i];
            Character target = entry.Target;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            if (!_manualTargetArrowDamageEntriesByTarget.TryGetValue(target, out var targetEntries))
            {
                targetEntries = new List<Skill.PreviewEffectEntry>(4);
                _manualTargetArrowDamageEntriesByTarget[target] = targetEntries;
            }
            if (targetEntries.Count == 0)
                _manualTargetArrowDamageTargetsBuffer.Add(target);

            targetEntries.Add(entry);
        }
    }

    private void HideManualTargetArrowEffectPreview()
    {
        for (int i = 0; i < _manualTargetArrowDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_manualTargetArrowDamagePanels[i]))
                _manualTargetArrowDamagePanels[i].Visible = false;
        }

        HideManualTargetArrowEffectTargetPreviews();
    }

    private void ShowManualTargetArrowEffectTargetPreviews(
        IReadOnlyList<Skill.PreviewEffectEntry> entries
    )
    {
        Character owner = _manualTargetArrowSkill?.OwnerCharater;
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        Character[] manualCandidates = _manualTargetArrowTargets ?? Array.Empty<Character>();
        _manualTargetArrowEffectHostileTargets.Clear();
        _manualTargetArrowEffectFriendlyTargets.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            Character target = entries[i].Target;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            if (target.IsPlayer != owner.IsPlayer)
            {
                if (!_manualTargetArrowEffectHostileTargets.Contains(target))
                    _manualTargetArrowEffectHostileTargets.Add(target);
                continue;
            }

            if (
                !ContainsCharacterReference(manualCandidates, target)
                && !_manualTargetArrowEffectFriendlyTargets.Contains(target)
            )
            {
                _manualTargetArrowEffectFriendlyTargets.Add(target);
            }
        }

        foreach (Character target in _manualTargetArrowEffectHostileTargets)
            target.ShowTargetPreview(ManualTargetEffectHostileColor, animate: false);

        foreach (Character target in _manualTargetArrowEffectFriendlyTargets)
            target.ShowTargetPreview(ManualTargetEffectFriendlyColor, animate: false);
    }

    private void HideManualTargetArrowEffectTargetPreviews()
    {
        foreach (Character target in _manualTargetArrowEffectHostileTargets)
        {
            if (target != null && GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        foreach (Character target in _manualTargetArrowEffectFriendlyTargets)
        {
            if (target != null && GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        _manualTargetArrowEffectHostileTargets.Clear();
        _manualTargetArrowEffectFriendlyTargets.Clear();

        if (_manualTargetArrowSelectionActive)
            RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
    }

    private static bool ContainsCharacterReference(Character[] candidates, Character target)
    {
        if (candidates == null || target == null)
            return false;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (ReferenceEquals(candidates[i], target))
                return true;
        }

        return false;
    }

    private VBoxContainer GetOrCreateManualTargetArrowDamagePanel(CanvasLayer layer, int index)
    {
        while (_manualTargetArrowDamagePanels.Count <= index)
        {
            VBoxContainer panel = PreviewEffectDisplay.CreatePanel();
            layer.AddChild(panel);
            _manualTargetArrowDamagePanels.Add(panel);
        }

        VBoxContainer pooledPanel = _manualTargetArrowDamagePanels[index];
        if (GodotObject.IsInstanceValid(pooledPanel))
            return pooledPanel;

        pooledPanel = PreviewEffectDisplay.CreatePanel();
        layer.AddChild(pooledPanel);
        _manualTargetArrowDamagePanels[index] = pooledPanel;
        return pooledPanel;
    }

    private static Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        Node2D anchor =
            target.Sprite != null && GodotObject.IsInstanceValid(target.Sprite)
                ? target.Sprite
                : target;
        return anchor.GetGlobalTransformWithCanvas().Origin;
    }

    private void ShowManualRebirthTargetPreviews(Skill skill, Character[] targets)
    {
        HideManualRebirthTargetPreviews();
        if (skill?.ManualFriendlyTargetAllowsDying() != true || targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || target.State != Character.CharacterState.Dying
            )
            {
                continue;
            }

            if (_manualRebirthTargetPreviewStates.ContainsKey(target))
                continue;

            Color originalModulate = target.Modulate;
            if (
                TryCreateRebirthPreviewState(
                    target,
                    originalModulate,
                    out RebirthPreviewState state
                )
            )
            {
                _manualRebirthTargetPreviewStates[target] = state;
                target.Modulate = new Color(
                    originalModulate.R,
                    originalModulate.G,
                    originalModulate.B,
                    1f
                );
            }
            else
            {
                _manualRebirthTargetPreviewStates[target] = new RebirthPreviewState
                {
                    TargetModulate = originalModulate,
                };
                target.Modulate = new Color(
                    originalModulate.R,
                    originalModulate.G,
                    originalModulate.B,
                    0.42f
                );
            }
        }
    }

    private void HideManualRebirthTargetPreviews()
    {
        foreach (KeyValuePair<Character, RebirthPreviewState> entry in _manualRebirthTargetPreviewStates)
        {
            Character target = entry.Key;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            RestoreRebirthPreviewSprite(target, entry.Value);
            if (target.State == Character.CharacterState.Dying)
                target.Modulate = entry.Value.TargetModulate;
        }

        _manualRebirthTargetPreviewStates.Clear();
    }

    private static bool TryCreateRebirthPreviewState(
        Character target,
        Color targetModulate,
        out RebirthPreviewState state
    )
    {
        state = null;
        if (target?.Sprite == null || !GodotObject.IsInstanceValid(target.Sprite))
        {
            return false;
        }

        Node2D sourceSprite = target.Sprite;
        Node parent = sourceSprite.GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return false;

        Node duplicatedNode = sourceSprite.Duplicate();
        if (duplicatedNode is not Node2D renderSprite)
        {
            duplicatedNode?.QueueFree();
            return false;
        }

        Vector2I viewportSize = new(1024, 1024);
        Vector2 viewportCenter = viewportSize / 2;
        LocalizeRebirthPreviewMaterials(renderSprite);
        renderSprite.SetMeta("skip_spawn_shader", true);

        SubViewport viewport = new()
        {
            Name = "RebirthPreviewViewport",
            Size = viewportSize,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };

        renderSprite.Name = "RebirthPreviewRenderSprite";
        renderSprite.Visible = true;
        renderSprite.Position = viewportCenter;
        renderSprite.Rotation = sourceSprite.Rotation;
        renderSprite.Scale = sourceSprite.Scale;
        renderSprite.ZIndex = 0;
        renderSprite.ZAsRelative = true;

        Sprite2D previewSprite = new()
        {
            Name = "RebirthPreviewSprite",
            Texture = viewport.GetTexture(),
            Centered = true,
            Position = sourceSprite.Position,
            ZIndex = sourceSprite.ZIndex,
            ZAsRelative = sourceSprite.ZAsRelative,
            Modulate = new Color(1f, 1f, 1f, 0.42f),
        };

        state = new RebirthPreviewState
        {
            TargetModulate = targetModulate,
            SpriteVisible = sourceSprite.Visible,
            Viewport = viewport,
            PreviewSprite = previewSprite,
        };

        parent.AddChild(viewport);
        viewport.AddChild(renderSprite);
        parent.AddChild(previewSprite);
        parent.MoveChild(
            previewSprite,
            Math.Clamp(sourceSprite.GetIndex(), 0, parent.GetChildCount() - 1)
        );
        sourceSprite.Visible = false;
        return true;
    }

    private static void RestoreRebirthPreviewSprite(Character target, RebirthPreviewState state)
    {
        if (state == null || target?.Sprite == null || !GodotObject.IsInstanceValid(target.Sprite))
        {
            return;
        }

        target.Sprite.Visible = state.SpriteVisible;

        if (state.PreviewSprite != null && GodotObject.IsInstanceValid(state.PreviewSprite))
            state.PreviewSprite.QueueFree();

        if (state.Viewport != null && GodotObject.IsInstanceValid(state.Viewport))
            state.Viewport.QueueFree();
    }

    private static void LocalizeRebirthPreviewMaterials(Node2D renderSprite)
    {
        if (renderSprite == null || !GodotObject.IsInstanceValid(renderSprite))
            return;

        if (renderSprite is CanvasItem canvas && canvas.Material is Material material)
        {
            Material localMaterial = (Material)material.Duplicate();
            localMaterial.ResourceLocalToScene = true;
            canvas.Material = localMaterial;
            if (localMaterial is ShaderMaterial shaderMaterial)
                shaderMaterial.SetShaderParameter("progress", 0f);
        }

        if (renderSprite.GetClass() != "SpineSprite")
            return;

        Variant normalMaterialVariant = renderSprite.Get("normal_material");
        if (normalMaterialVariant.VariantType != Variant.Type.Object)
            return;

        if (normalMaterialVariant.As<Material>() is not Material normalMaterial)
            return;

        Material localNormalMaterial = (Material)normalMaterial.Duplicate();
        localNormalMaterial.ResourceLocalToScene = true;
        renderSprite.Set("normal_material", localNormalMaterial);
        if (localNormalMaterial is ShaderMaterial localShaderMaterial)
            localShaderMaterial.SetShaderParameter("progress", 0f);
    }

    private void OnManualTargetArrowMaskGuiInput(InputEvent @event)
    {
        if (!_manualTargetArrowSelectionActive)
            return;

        if (@event is InputEventMouseMotion)
        {
            UpdateManualTargetArrowVisual();
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            UpdateManualTargetArrowVisual();
            if (
                _manualTargetArrowHoveredTarget != null
                && GodotObject.IsInstanceValid(_manualTargetArrowHoveredTarget)
            )
            {
                _manualTargetArrowHoveredTarget.PlayTargetLockPulse(ManualTargetHoveredColor, 1.1f);
                _manualTargetCompletion?.TrySetResult(_manualTargetArrowHoveredTarget);
            }

            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            HideManualTargetPicker();
            GetViewport().SetInputAsHandled();
        }
    }

    private void HideManualTargetPicker(bool resetArrowCardVisual = true)
    {
        ClearCardEnergyPreview();
        bool wasTargetSelectionPending = IsManualTargetSelectionPending();
        bool wasArrowSelectionActive = _manualTargetArrowSelectionActive;
        int arrowCardIndex = _manualTargetArrowCardIndex;
        _manualTargetArrowSelectionActive = false;
        _manualTargetCompletion?.TrySetResult(null);
        _manualTargetCompletion = null;
        HideManualRebirthTargetPreviews();
        HideManualTargetArrowPreviews();
        HideManualTargetArrowEffectPreview();
        if (wasArrowSelectionActive)
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
        if (wasArrowSelectionActive)
            RestoreMouseAfterManualTargetArrowSelection();
        if (wasArrowSelectionActive)
            SetCardHoverUiEnabled(true);
        _manualTargetArrowTargets = Array.Empty<Character>();
        _manualTargetArrowOwner = null;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = null;
        _manualTargetArrowCardIndex = -1;
        _manualTargetArrowSourcePosition = null;
        _manualTargetArrowSourceTangent = null;
        _manualTargetArrowStatusText = "选择一名己方角色";
        if (_manualTargetArrowRoot != null && GodotObject.IsInstanceValid(_manualTargetArrowRoot))
        {
            _manualTargetArrowRoot.Visible = false;
        }
        if (_manualTargetArrowLayer != null && GodotObject.IsInstanceValid(_manualTargetArrowLayer))
        {
            _manualTargetArrowLayer.SetHeadHighlighted(false);
            _manualTargetArrowLayer.SetEndpoints(Vector2.Zero, Vector2.Zero);
            _manualTargetArrowLayer.QueueRedraw();
        }
        if (
            _manualTargetArrowPlayedCard != null
            && GodotObject.IsInstanceValid(_manualTargetArrowPlayedCard)
        )
        {
            _manualTargetArrowPlayedCard.Visible = true;
        }
        _manualTargetArrowPlayedCard = null;
        if (wasArrowSelectionActive && resetArrowCardVisual)
            ResetManualTargetArrowCardVisual(arrowCardIndex);

        bool shouldRefreshTurnUiAfterHide =
            (wasTargetSelectionPending || wasArrowSelectionActive)
            && !(wasArrowSelectionActive && !resetArrowCardVisual);

        if (shouldRefreshTurnUiAfterHide)
            RequestTurnUiRefresh(refreshHover: true);
    }

    private void ResetManualTargetArrowCardVisual(int index)
    {
        if (!IsCardIndexValid(index) || IsCardCommitted(index))
            return;

        if (_manualTargetArrowUsesLiftedCard && _liftedCardIndex == index)
        {
            ClearLiftedCard(instant: false);
            return;
        }

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
            return;

        ResetCardMotion(index, instant: false);
    }

}
