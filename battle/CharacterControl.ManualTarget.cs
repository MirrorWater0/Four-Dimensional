using System;
using System.Collections.Generic;
using System.Linq;
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
        if (card == null || card.Button.Disabled)
            return;

        DetachDrawEntryStateForImmediateInteraction(index);

        _suppressCardButtonPressUntilLeftRelease = Input.IsMouseButtonPressed(MouseButton.Left);
        _liftedCardIndex = index;
        _liftedCardSkill = skill;
        _liftedCard = card;
        _liftedCardMouseOffset = GetSkillCardCenterOffset(card);
        SetHandInputBlockerVisible(true);
        BlockOtherHandCardInputWhileLifted(index);

        if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            ResetCardMotion(_hoveredCardIndex, instant: false);
        _hoveredCardIndex = -1;

        card.StopBattleMotion();
        card.SetRelatedCardPreviewSuppressed(true);
        card.SetHoverUiEnabled(false);

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
            Vector2 globalPosition = card.GlobalPosition;
            card.GetParent()?.RemoveChild(card);
            overlay.AddChild(card);
            card.GlobalPosition = globalPosition;
        }

        card.HoverHint.Visible = true;
        SetCardHoverPreviewActive(index, true);
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
        _suppressCardButtonPressUntilLeftRelease = false;
        SetHandInputBlockerVisible(false);
        if (!_manualTargetArrowSelectionActive)
            SetCardHoverUiEnabled(true);
        _cardSlotLayoutPixelsPerSecondOverrides[index] = instant
            ? 0f
            : Math.Max(HandLayoutPixelsPerSecond, HandDroppedCardReturnPixelsPerSecond);
        UpdateProcessState();
        ResetCardMotion(index, instant, HandDroppedCardReturnMotionDuration);
        LayoutActionCards(instant);
        RefreshTurnUi();
    }

    private void ReleaseLiftedCardForManualTargetSelection(int index)
    {
        if (_liftedCardIndex != index)
            return;

        _manualTargetArrowUsesLiftedCard = true;
        _suppressCardButtonPressUntilLeftRelease = false;
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
        _suppressCardButtonPressUntilLeftRelease = false;
        SetHandInputBlockerVisible(true);
        SuppressHandInteractionForManualTargetSelection();
        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        card.HoverHint.Visible = true;
        card.ZIndex = LiftedCardZIndex;
        HideCardHoverPreview(index);
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
        if (_manualTargetArrowMouseHidden)
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

        slot.GlobalPosition = card.GlobalPosition;
        if (card.GetParent() != slot)
        {
            card.GetParent()?.RemoveChild(card);
            slot.AddChild(card);
        }

        card.Position = Vector2.Zero;
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
                Character[] drawableCandidates = candidates
                    .Where(candidate =>
                        candidate is PlayerCharacter player
                        && BattleNode?.HasDrawablePlayerCarrySkill(player, carrySkillType) == true
                    )
                    .ToArray();
                if (drawableCandidates.Length > 0)
                    candidates = drawableCandidates;
            }

            Random rng = BattleNode?.BattleIntentionRandom ?? new Random();
            Character randomTarget = candidates[rng.Next(candidates.Length)];
            play.Skill.SetManualFriendlyTarget(randomTarget);
            return play.Skill.HasManualFriendlyTarget();
        }

        bool shouldUseArrowSelection =
            !play.ForceManualTargetCardPicker && ShouldUseManualTargetArrowSelection(play.Skill);

        Character target = shouldUseArrowSelection
            ? await ShowManualTargetArrowPickerAsync(play.Skill, play.Card)
            : await ShowManualTargetPickerAsync(play.Skill, play.Card);
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        play.Skill.SetManualFriendlyTarget(target);
        return play.Skill.HasManualFriendlyTarget();
    }

    private bool HasManualFriendlyTargetCandidates(Skill skill)
    {
        return GetManualFriendlyTargetCandidates(skill).Length > 0;
    }

    private Character[] GetManualFriendlyTargetCandidates(Skill skill)
    {
        if (skill?.OwnerCharater == null || BattleNode == null)
            return Array.Empty<Character>();

        bool excludeSelf = skill.ManualFriendlyTargetExcludesSelf();
        bool allowDying = skill.ManualFriendlyTargetAllowsDying();
        Character owner = skill.OwnerCharater;

        return BattleNode
            .GetTeamCharacters(skill.OwnerCharater.IsPlayer, includeSummons: true)
            .Where(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && (allowDying || character.State != Character.CharacterState.Dying)
                && (!excludeSelf || character != owner)
            )
            .OrderBy(character => character.PositionIndex)
            .ToArray();
    }

    private static bool ShouldUseManualTargetArrowSelection(Skill skill)
    {
        UserSettings.EnsureLoaded();
        return UserSettings.UseArrowManualTargetSelection
            && skill?.RequiresManualFriendlyTarget() == true;
    }

    private void EnsureManualTargetPickerUi()
    {
        if (_manualTargetPickerRoot != null && GodotObject.IsInstanceValid(_manualTargetPickerRoot))
        {
            EnsureManualTargetPickerToggleButtons();
            return;
        }

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        _manualTargetPickerRoot = new Control
        {
            Name = "ManualTargetPicker",
            Visible = false,
            ZIndex = ManualTargetPickerZIndex,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _manualTargetPickerRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.AddChild(_manualTargetPickerRoot);

        _manualTargetPickerMask = new ColorRect
        {
            Name = "Mask",
            Color = new Color(0f, 0f, 0f, 0.42f),
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 0,
        };
        _manualTargetPickerMask.SetAnchorsPreset(LayoutPreset.FullRect);
        _manualTargetPickerRoot.AddChild(_manualTargetPickerMask);

        _manualTargetPickerRow = new HBoxContainer
        {
            Name = "Cards",
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 1,
        };
        _manualTargetPickerRow.AnchorLeft = 0.5f;
        _manualTargetPickerRow.AnchorRight = 0.5f;
        _manualTargetPickerRow.AnchorTop = 0.5f;
        _manualTargetPickerRow.AnchorBottom = 0.5f;
        _manualTargetPickerRow.OffsetLeft = -452f;
        _manualTargetPickerRow.OffsetTop = -130f;
        _manualTargetPickerRow.OffsetRight = 452f;
        _manualTargetPickerRow.OffsetBottom = 130f;
        _manualTargetPickerRow.AddThemeConstantOverride("separation", 32);
        _manualTargetPickerRoot.AddChild(_manualTargetPickerRow);

        EnsureManualTargetPickerToggleButtons();
    }

    private void EnsureManualTargetPickerToggleButtons()
    {
        if (
            _manualTargetPickerRoot == null
            || !GodotObject.IsInstanceValid(_manualTargetPickerRoot)
        )
            return;

        if (
            _manualTargetPickerHideButton == null
            || !GodotObject.IsInstanceValid(_manualTargetPickerHideButton)
        )
        {
            _manualTargetPickerHideButton =
                _manualTargetPickerRoot.GetNodeOrNull<Button>("HideButton")
                ?? new Button { Name = "HideButton" };
            if (_manualTargetPickerHideButton.GetParent() == null)
                _manualTargetPickerRoot.AddChild(_manualTargetPickerHideButton);
            _manualTargetPickerHideButton.Pressed += ToggleManualTargetPickerTemporaryHidden;
        }

        _manualTargetPickerRoot.GetNodeOrNull<Button>("RestoreButton")?.QueueFree();

        _manualTargetPickerHideButton.MoveToFront();
        ApplyManualTargetPickerTemporaryHiddenState();
    }

    private static void ConfigureManualTargetPickerToggleButton(Button button, bool hidden)
    {
        if (button == null)
            return;

        button.Text = hidden ? "继续选择目标" : "隐藏";
        button.FocusMode = FocusModeEnum.None;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.ZIndex = 1000;
        button.AnchorLeft = 1f;
        button.AnchorRight = 1f;
        button.AnchorTop = 0.5f;
        button.AnchorBottom = 0.5f;
        button.OffsetLeft = -380f;
        button.OffsetRight = -206f;
        button.OffsetTop = -16f;
        button.OffsetBottom = 28f;
    }

    private static void ConfigureManualTargetArrowScale(ManualTargetArrowView arrow)
    {
        if (arrow == null)
            return;

        float segmentScale = (ManualTargetArrowSegmentScaleStart + ManualTargetArrowSegmentScaleEnd)
            * 0.5f;
        arrow.ArrowWidth = 30f * segmentScale;
        arrow.ShadowWidth = arrow.ArrowWidth * 1.75f;
        arrow.CurveLift = 118f;
        arrow.PointCount = 19;
        arrow.HeadSize = new Vector2(48f, 42f);
        arrow.TailSize = new Vector2(30f, 20f);
        arrow.HeadShaftInset = 29f;
        arrow.TailShaftInset = 16f;
        arrow.TipOutlineWidth = 0.12f;
        arrow.HeadDefaultScale = ManualTargetArrowHeadDefaultScale;
        arrow.HeadHoverScale = ManualTargetArrowHeadHoverScale;
        arrow.SetHeadHighlighted(false);
    }

    private void EnsureManualTargetArrowUi()
    {
        if (_manualTargetArrowRoot != null && GodotObject.IsInstanceValid(_manualTargetArrowRoot))
            return;

        CanvasLayer overlay = EnsureLiftedCardOverlay();
        if (overlay == null)
            return;

        _manualTargetArrowRoot = new Control
        {
            Name = "ManualTargetArrowPicker",
            Visible = false,
            ZIndex = LiftedCardZIndex + 10,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _manualTargetArrowRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.AddChild(_manualTargetArrowRoot);

        _manualTargetArrowMask = new ColorRect
        {
            Name = "InputMask",
            Color = new Color(0f, 0f, 0f, 0.10f),
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 0,
        };
        _manualTargetArrowMask.SetAnchorsPreset(LayoutPreset.FullRect);
        _manualTargetArrowMask.GuiInput += OnManualTargetArrowMaskGuiInput;
        _manualTargetArrowRoot.AddChild(_manualTargetArrowMask);

        _manualTargetArrowLayer =
            ManualTargetArrowScene?.Instantiate<ManualTargetArrowView>()
            ?? new ManualTargetArrowView();
        _manualTargetArrowLayer.Name = "Arrow";
        _manualTargetArrowLayer.MouseFilter = MouseFilterEnum.Ignore;
        _manualTargetArrowLayer.ZIndex = 1;
        ConfigureManualTargetArrowScale(_manualTargetArrowLayer);
        _manualTargetArrowLayer.SetAnchorsPreset(LayoutPreset.FullRect);
        _manualTargetArrowRoot.AddChild(_manualTargetArrowLayer);

        _manualTargetArrowHintLabel = new Label
        {
            Name = "Hint",
            Text = "选择目标",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            ZIndex = 2,
        };
        _manualTargetArrowHintLabel.AddThemeColorOverride(
            "font_color",
            new Color(0.86f, 0.97f, 1f, 0.96f)
        );
        _manualTargetArrowHintLabel.AddThemeColorOverride(
            "font_shadow_color",
            new Color(0f, 0f, 0f, 0.72f)
        );
        _manualTargetArrowHintLabel.AddThemeConstantOverride("shadow_offset_x", 2);
        _manualTargetArrowHintLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        _manualTargetArrowHintLabel.AddThemeFontSizeOverride("font_size", 26);
        _manualTargetArrowHintLabel.AnchorLeft = 0.5f;
        _manualTargetArrowHintLabel.AnchorRight = 0.5f;
        _manualTargetArrowHintLabel.AnchorTop = 0f;
        _manualTargetArrowHintLabel.AnchorBottom = 0f;
        _manualTargetArrowHintLabel.OffsetLeft = -140f;
        _manualTargetArrowHintLabel.OffsetRight = 140f;
        _manualTargetArrowHintLabel.OffsetTop = 92f;
        _manualTargetArrowHintLabel.OffsetBottom = 132f;
        _manualTargetArrowRoot.AddChild(_manualTargetArrowHintLabel);
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
        _manualTargetPickerPlayedCard = playedCard;
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

        Character[] targets = BattleNode
            .GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Concat(BattleNode.GetTeamCharacters(isPlayer: false, includeSummons: true))
            .Where(character => character != null && GodotObject.IsInstanceValid(character))
            .OrderBy(character => character.IsPlayer ? 0 : 1)
            .ThenBy(character => character.PositionIndex)
            .ToArray();

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
        _manualTargetPickerPlayedCard = null;
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

    private async Task<Character> ShowManualTargetPickerAsync(
        Skill skill,
        SkillCard playedCard = null
    )
    {
        if (skill?.OwnerCharater == null || BattleNode == null)
            return null;

        EnsureManualTargetPickerUi();
        if (_manualTargetPickerRoot == null || _manualTargetPickerRow == null)
            return null;

        ClearManualTargetCards();
        _manualTargetPickerTemporarilyHidden = false;
        _manualTargetPickerPlayedCard = playedCard;
        _statusLabel.Text = "选择一名己方角色";

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        RefreshTurnUi();
        ShowCardEnergyPreview(skill);

        Character owner = skill.OwnerCharater;
        Character[] targets = GetManualFriendlyTargetCandidates(skill);
        ShowManualRebirthTargetPreviews(skill, targets);

        AddManualTargetCards(
            targets,
            selectable: true,
            onSelected: selected => completion.TrySetResult(selected)
        );
        ShowManualTargetPickerRoot(targets.Length);
        if (targets.Length == 0)
        {
            HideManualTargetPicker();
            return null;
        }

        try
        {
            while (!completion.Task.IsCompleted && IsManualTargetPickerContextValid(skill, owner))
            {
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

    private void AddManualTargetCards(
        Character[] targets,
        bool selectable,
        Action<Character> onSelected
    )
    {
        for (int i = 0; i < (targets?.Length ?? 0); i++)
        {
            if (CharacterTargetCardScene?.Instantiate() is not CharacterTargetCard card)
                continue;

            Character target = targets[i];
            card.SetTarget(target);
            card.SetSelectable(selectable);
            card.SetTooltipOnHover(!selectable);
            if (selectable && onSelected != null)
                card.Selected += selected => onSelected(selected);

            var slot = new Control
            {
                Name = "TargetCardSlot",
                CustomMinimumSize = card.CustomMinimumSize,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _manualTargetPickerRow.AddChild(slot);
            slot.AddChild(card);
            AnimateManualTargetCard(card, i, targets.Length);
        }
    }

    private void ShowManualTargetPickerRoot(int targetCount)
    {
        _manualTargetPickerRoot.Visible = targetCount > 0;
        if (_manualTargetPickerMask != null)
        {
            _manualTargetPickerMask.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        ApplyManualTargetPickerTemporaryHiddenState();
        if (targetCount == 0 || _manualTargetPickerMask == null)
            return;

        if (_manualTargetPickerTemporarilyHidden)
            return;

        _manualTargetPickerMask
            .CreateTween()
            .TweenProperty(_manualTargetPickerMask, "modulate:a", 1f, 0.12f);
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

    private void SetManualTargetPickerTemporarilyHidden(bool hidden)
    {
        if (!IsManualTargetSelectionPending())
            hidden = false;

        _manualTargetPickerTemporarilyHidden = hidden;
        ApplyManualTargetPickerTemporaryHiddenState();

        if (_statusLabel != null && IsManualTargetSelectionPending())
            _statusLabel.Text = hidden ? "选择一名己方角色（界面已隐藏）" : "选择一名己方角色";
    }

    private void ToggleManualTargetPickerTemporaryHidden()
    {
        SetManualTargetPickerTemporarilyHidden(!_manualTargetPickerTemporarilyHidden);
    }

    private void ApplyManualTargetPickerTemporaryHiddenState()
    {
        bool rootVisible =
            _manualTargetPickerRoot != null
            && GodotObject.IsInstanceValid(_manualTargetPickerRoot)
            && _manualTargetPickerRoot.Visible;
        bool canToggle = rootVisible && IsManualTargetSelectionPending();
        bool hidden = canToggle && _manualTargetPickerTemporarilyHidden;

        if (_manualTargetPickerMask != null && GodotObject.IsInstanceValid(_manualTargetPickerMask))
        {
            _manualTargetPickerMask.Visible = rootVisible && !hidden;
            _manualTargetPickerMask.MouseFilter = hidden
                ? MouseFilterEnum.Ignore
                : MouseFilterEnum.Stop;
        }

        if (_manualTargetPickerRow != null && GodotObject.IsInstanceValid(_manualTargetPickerRow))
            _manualTargetPickerRow.Visible = rootVisible && !hidden;

        if (
            _manualTargetPickerHideButton != null
            && GodotObject.IsInstanceValid(_manualTargetPickerHideButton)
        )
        {
            ConfigureManualTargetPickerToggleButton(_manualTargetPickerHideButton, hidden);
            _manualTargetPickerHideButton.Visible = canToggle;
            _manualTargetPickerHideButton.Disabled = !canToggle;
            if (_manualTargetPickerHideButton.Visible)
                _manualTargetPickerHideButton.MoveToFront();
        }

        if (
            _manualTargetPickerPlayedCard != null
            && GodotObject.IsInstanceValid(_manualTargetPickerPlayedCard)
        )
        {
            UserSettings.EnsureLoaded();
            _manualTargetPickerPlayedCard.Visible =
                !hidden || UserSettings.KeepManualTargetCardVisibleWhenHidden;
        }
    }

    private bool IsManualTargetPickerContextValid(Skill skill, Character owner)
    {
        return IsInsideTree()
            && _manualTargetPickerRoot != null
            && GodotObject.IsInstanceValid(_manualTargetPickerRoot)
            && _manualTargetPickerRoot.Visible
            && skill != null
            && owner != null
            && GodotObject.IsInstanceValid(owner)
            && owner.State != Character.CharacterState.Dying
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode)
            && BattleNode.ShouldAbortSkillResolution() != true;
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
        UpdateManualTargetArrowVisual(_manualTargetArrowOwner, _manualTargetPickerPlayedCard);
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
        Character[] candidates = (_manualTargetArrowTargets ?? Array.Empty<Character>())
            .Where(target => target != null && GodotObject.IsInstanceValid(target))
            .OrderBy(target => target.PositionIndex)
            .ToArray();
        if (candidates.Length == 0)
            return null;

        Character current = _manualTargetArrowHoveredTarget;
        int currentIndex = Array.IndexOf(candidates, current);
        if (currentIndex < 0)
            return GetDefaultManualTargetArrowTarget(_manualTargetArrowOwner, candidates);

        int nextIndex = (currentIndex + direction + candidates.Length) % candidates.Length;
        return candidates[nextIndex];
    }

    private static Character GetDefaultManualTargetArrowTarget(Character owner, Character[] targets)
    {
        Character[] candidates = (targets ?? Array.Empty<Character>())
            .Where(target => target != null && GodotObject.IsInstanceValid(target))
            .OrderBy(target => target.PositionIndex)
            .ToArray();
        if (candidates.Length == 0)
            return null;

        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return candidates[0];

        return candidates
            .OrderBy(target => Math.Abs(target.PositionIndex - owner.PositionIndex))
            .ThenBy(target => target.PositionIndex)
            .FirstOrDefault();
    }

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
            && (_manualTargetArrowTargets ?? Array.Empty<Character>()).Contains(target);
    }

    private void ApplyManualTargetArrowCardVisual(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 targetScale = BattleCardScale * ManualTargetCenteredCardScaleMultiplier;
        int index = GetBattleCardSignalIndex(card);
        card.HoverHint.Visible = true;
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

        var entries = _manualTargetArrowSkill.GetPreviewEffectEntries();
        if (entries == null || entries.Length == 0)
            return;

        ShowManualTargetArrowEffectTargetPreviews(entries);

        CanvasLayer layer = EnsureCardPlayOverlay();
        if (layer == null)
            return;

        int panelIndex = 0;
        foreach (
            var group in entries
                .Where(entry => entry.Target != null && GodotObject.IsInstanceValid(entry.Target))
                .GroupBy(entry => entry.Target)
        )
        {
            VBoxContainer panel = GetOrCreateManualTargetArrowDamagePanel(layer, panelIndex++);
            PreviewEffectDisplay.ShowPanel(
                panel,
                group.ToArray(),
                GetTargetScreenPosition(group.Key),
                ManualTargetDamagePreviewLabelOffset
            );
        }

        for (int i = panelIndex; i < _manualTargetArrowDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_manualTargetArrowDamagePanels[i]))
                _manualTargetArrowDamagePanels[i].Visible = false;
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
        _manualTargetArrowEffectHostileTargets = entries
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.IsPlayer != owner.IsPlayer
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();
        _manualTargetArrowEffectFriendlyTargets = entries
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.IsPlayer == owner.IsPlayer
                && !manualCandidates.Contains(entry.Target)
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();

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

        _manualTargetArrowEffectHostileTargets = Array.Empty<Character>();
        _manualTargetArrowEffectFriendlyTargets = Array.Empty<Character>();

        if (_manualTargetArrowSelectionActive)
            RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
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
        foreach (var entry in _manualRebirthTargetPreviewStates.ToArray())
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

    private void AnimateManualTargetCard(Control card, int index, int count)
    {
        if (card == null)
            return;

        const float cardWidth = 190f;
        const float cardSeparation = 32f;
        float centerOffsetX = (count - 1) * (cardWidth + cardSeparation) * 0.5f;
        float startOffsetX = centerOffsetX - index * (cardWidth + cardSeparation);
        float normalizedIndex =
            count <= 1 ? 0f : (index - (count - 1) * 0.5f) / ((count - 1) * 0.5f);
        float finalRotation = Mathf.DegToRad(normalizedIndex * 4f);

        card.Modulate = new Color(1f, 1f, 1f, 0f);
        card.Position = new Vector2(startOffsetX, 72f);
        card.PivotOffset = card.CustomMinimumSize * 0.5f;
        card.Rotation = Mathf.DegToRad(normalizedIndex * -8f);
        card.Scale = new Vector2(0.72f, 0.72f);

        Tween tween = card.CreateTween();
        if (index > 0)
            tween.TweenInterval(index * 0.025f);
        tween.SetParallel(true);
        tween.TweenProperty(card, "modulate:a", 1f, 0.14f);
        tween
            .TweenProperty(card, "position", Vector2.Zero, 0.24f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "rotation", finalRotation, 0.28f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "scale", Vector2.One, 0.22f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
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
        _manualTargetPickerTemporarilyHidden = false;
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
            _manualTargetPickerPlayedCard != null
            && GodotObject.IsInstanceValid(_manualTargetPickerPlayedCard)
        )
        {
            _manualTargetPickerPlayedCard.Visible = true;
        }
        _manualTargetPickerPlayedCard = null;
        if (wasArrowSelectionActive && resetArrowCardVisual)
            ResetManualTargetArrowCardVisual(arrowCardIndex);

        bool shouldRefreshTurnUiAfterHide =
            (wasTargetSelectionPending || wasArrowSelectionActive)
            && !(wasArrowSelectionActive && !resetArrowCardVisual);

        if (
            _manualTargetPickerRoot == null
            || !GodotObject.IsInstanceValid(_manualTargetPickerRoot)
        )
        {
            if (shouldRefreshTurnUiAfterHide)
                RefreshTurnUi();
            return;
        }

        _manualTargetPickerRoot.Visible = false;
        ApplyManualTargetPickerTemporaryHiddenState();
        ClearManualTargetCards();
        if (shouldRefreshTurnUiAfterHide)
            RefreshTurnUi();
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

    private void ClearManualTargetCards()
    {
        if (_manualTargetPickerRow == null || !GodotObject.IsInstanceValid(_manualTargetPickerRow))
            return;

        foreach (Node child in _manualTargetPickerRow.GetChildren())
        {
            if (child is Control slot)
            {
                foreach (Node slotChild in slot.GetChildren())
                {
                    if (slotChild is CharacterTargetCard card)
                        card.HideTargetTooltip();
                }
            }

            child.QueueFree();
        }
    }


}
