using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public enum HandCardEntryOrigin
{
    Default,
    DrawPile,
    PlayedCard,
}

public partial class CharacterControl : Control
{
    public Battle BattleNode => field ??= FindBattleNode();
    public bool IsManualTargetArrowSelectionActive => _manualTargetArrowSelectionActive;
    public Frame CharaterFrame1 => field ??= GetNodeOrNull<Frame>("frame1");
    public Frame CharaterFrame2 => field ??= GetNodeOrNull<Frame>("frame2");
    public Frame CharaterFrame3 => field ??= GetNodeOrNull<Frame>("frame3");
    public Frame CharaterFrame4 => field ??= GetNodeOrNull<Frame>("frame4");
    public Frame[] CharactersControl =>
        new[] { CharaterFrame1, CharaterFrame2, CharaterFrame3, CharaterFrame4 };
    public Button EndTurnButton => _endTurnButton;
    public Control ActionCardContainer => _cardRow;

    private Battle FindBattleNode()
    {
        Node node = GetParent();
        while (node != null)
        {
            if (node is Battle battle)
                return battle;

            node = node.GetParent();
        }

        return null;
    }
    public override void _Ready()
    {
        SkillCard.PrewarmExhaustEffect();
        BuildActionAreaUi();
        SetProcess(false);
        SetProcessInput(true);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        float frameDelta = (float)delta;
        UpdateHandLayoutFollowers(frameDelta);
        UpdateLiftedCardPosition(frameDelta);
        UpdatePileOverlaySmoothScroll(frameDelta);
        UpdateProcessState();
    }

    public override void _Input(InputEvent @event)
    {
        if (
            @event is InputEventKey key
            && key.Pressed
            && !key.Echo
            && !key.AltPressed
            && !key.CtrlPressed
            && !key.MetaPressed
            && (key.Keycode == Key.E || key.PhysicalKeycode == Key.E)
            && CanUseEndTurnShortcut()
        )
        {
            OnEndTurnPressed();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            @event is InputEventKey handCardKey
            && handCardKey.Pressed
            && !handCardKey.Echo
            && TryHandleHandCardIndexShortcut(handCardKey)
        )
        {
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            IsPileOverlayVisible()
            && @event is InputEventKey closeKey
            && closeKey.Pressed
            && !closeKey.Echo
            && closeKey.Keycode == Key.Escape
        )
        {
            HidePileOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            _manualTargetArrowSelectionActive
            && @event is InputEventKey arrowCloseKey
            && arrowCloseKey.Pressed
            && !arrowCloseKey.Echo
            && arrowCloseKey.Keycode == Key.Escape
        )
        {
            HideManualTargetPicker();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            _manualTargetArrowSelectionActive
            && @event is InputEventKey arrowNavigationKey
            && arrowNavigationKey.Pressed
            && !arrowNavigationKey.Echo
        )
        {
            if (TryHandleManualTargetArrowKeyInput(arrowNavigationKey))
            {
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (
            @event is InputEventMouseButton leftMouseButton
            && leftMouseButton.Pressed
            && leftMouseButton.ButtonIndex == MouseButton.Left
            && _liftedCardIndex != -1
            && !_manualTargetArrowSelectionActive
        )
        {
            if (!IsMouseOutsideHandArea())
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_suppressCardButtonPressUntilLeftRelease)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            int liftedIndex = _liftedCardIndex;
            _suppressCardButtonPressUntilLeftRelease = true;
            _ = HandleCardPressedAsync(liftedIndex, allowSuppressedPress: true);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            @event is InputEventMouseButton leftMouseRelease
            && !leftMouseRelease.Pressed
            && leftMouseRelease.ButtonIndex == MouseButton.Left
        )
        {
            if (_suppressCardButtonPressUntilLeftRelease)
                CallDeferred(nameof(ClearCardButtonPressSuppression));

            if (_suppressHandHoverUntilMouseMove)
            {
                _suppressHandHoverUntilMouseMove = false;
                ScheduleCardHoverRefresh();
            }
        }

        if (_suppressHandHoverUntilMouseMove && @event is InputEventMouseMotion)
        {
            Vector2 mousePosition = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
            if (
                !Input.IsMouseButtonPressed(MouseButton.Left)
                && mousePosition.DistanceSquaredTo(_handHoverSuppressionMousePosition)
                    >= HandHoverResumeMouseMoveDistance * HandHoverResumeMouseMoveDistance
            )
            {
                _suppressHandHoverUntilMouseMove = false;
                ScheduleCardHoverRefresh();
            }
        }
        else if (@event is InputEventMouseMotion && _liftedCardIndex == -1)
        {
            ScheduleCardHoverRefresh();
        }

        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Right
        )
        {
            if (_manualTargetArrowSelectionActive)
            {
                HideManualTargetPicker();
            }
            else if (IsPileOverlayVisible())
            {
                if (!_isPileCardSelectionActive)
                    HidePileOverlay();
            }
            else if (_liftedCardIndex != -1)
            {
                ClearLiftedCard(instant: false);
                HideManualTargetPicker();
            }
            else if (IsCardQueueBusy() || IsManualTargetSelectionPending()) { }
            else
            {
                return;
            }

            GetViewport().SetInputAsHandled();
        }
    }

    private bool TryHandleHandCardIndexShortcut(InputEventKey key)
    {
        if (!CanUseHandCardIndexShortcut(key))
            return false;

        int shortcutNumber = GetHandCardShortcutNumber(key);
        if (shortcutNumber <= 0)
            return false;

        int[] visibleSlotIndexes = GetVisibleHandSlotIndexes(excludeLiftedCard: false);
        int orderIndex = shortcutNumber - 1;
        if (orderIndex < 0 || orderIndex >= visibleSlotIndexes.Length)
            return false;

        int slotIndex = visibleSlotIndexes[orderIndex];
        _ = HandleHandCardIndexShortcutAsync(slotIndex);
        return true;
    }

    private bool CanUseHandCardIndexShortcut(InputEventKey key)
    {
        UserSettings.EnsureLoaded();
        return UserSettings.ShowHandCardIndices
            && key != null
            && !key.AltPressed
            && !key.CtrlPressed
            && !key.MetaPressed
            && !key.ShiftPressed
            && Visible
            && _uiBuilt
            && !IsBlockingMenuOpen();
    }

    private static int GetHandCardShortcutNumber(InputEventKey key)
    {
        int digit = GetHandCardShortcutDigit(key.Keycode);
        if (digit < 0)
            digit = GetHandCardShortcutDigit(key.PhysicalKeycode);

        if (digit < 0)
            return -1;

        return digit == 0 ? 10 : digit;
    }

    private static int GetHandCardShortcutDigit(Key keycode)
    {
        long value = (long)keycode;
        if (value >= '0' && value <= '9')
            return (int)(value - '0');

        return keycode switch
        {
            Key.Kp0 => 0,
            Key.Kp1 => 1,
            Key.Kp2 => 2,
            Key.Kp3 => 3,
            Key.Kp4 => 4,
            Key.Kp5 => 5,
            Key.Kp6 => 6,
            Key.Kp7 => 7,
            Key.Kp8 => 8,
            Key.Kp9 => 9,
            _ => -1,
        };
    }

    private bool IsBlockingMenuOpen()
    {
        Node root = GetTree()?.Root;
        return IsBlockingMenuOpen(root);
    }

    private static bool IsBlockingMenuOpen(Node node)
    {
        if (node == null)
            return false;

        if (node is Menu menu && menu.Visible)
            return true;

        foreach (Node child in node.GetChildren())
        {
            if (IsBlockingMenuOpen(child))
                return true;
        }

        return false;
    }

    public void Connect()
    {
        BuildActionAreaUi();
        for (int i = 0; i < BattleNode.PlayersList.Count; i++)
        {
            RefreshSkillOwners(BattleNode.PlayersList[i]);
        }
    }

    public void ShowPlayerTurn(PlayerCharacter player)
    {
        if (player == null)
            return;

        BuildActionAreaUi();
        bool preserveHandDisplay =
            _activePlayer == player && GetActiveHandSkills()?.Any(skill => skill != null) == true;
        _activePlayer = player;
        _isResolvingCard = false;
        _isProcessingCardQueue = false;
        _endTurnQueued = false;
        _isResolvingEndTurn = false;
        _freezeHandLayout = false;
        CancelDiscardSelection();
        CancelPileCardSelection();
        HidePileOverlay();
        HideManualTargetPicker();
        ClearCardQueue(resetCards: true);
        ClearLiftedCard(instant: true);
        if (!preserveHandDisplay)
            ResetCardDisplayTracking();
        RefreshSkillOwners(player);
        Visible = true;
        RefreshTurnUi();
    }

    public void DisablePlayerActions(PlayerCharacter player = null)
    {
        BuildActionAreaUi();
        if (player != null && _activePlayer != player)
            return;

        bool keepPanelVisible = player != null && _activePlayer == player;

        _isResolvingCard = keepPanelVisible;
        _isProcessingCardQueue = false;
        _endTurnQueued = false;
        _isResolvingEndTurn = keepPanelVisible;
        _freezeHandLayout = false;
        CancelDiscardSelection();
        CancelPileCardSelection();
        HidePileOverlay();
        ClearCardQueue(resetCards: true);
        ClearLiftedCard(instant: true);
        _activePlayer = keepPanelVisible ? player : null;
        Visible = keepPanelVisible;
        if (!keepPanelVisible)
            ResetCardDisplayTracking();
        RefreshTurnUi();
    }

    public void DisableAll()
    {
        DisablePlayerActions();
        BattleNode?.MapNode?.PlayerResourceState?.SetItemsEnabled(false);
    }

    public void RefreshDisplayedSkillDescriptions()
    {
        if (!_uiBuilt)
            return;

        if (_activePlayer != null)
            RefreshTurnUi();
    }

    public void InvalidateHandCardHoverPreviewCaches()
    {
        if (!_uiBuilt)
            return;

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            card.InvalidateHoverPreviewCache();
        }
    }

    public void RefreshTextSizeFromSettings()
    {
        if (!_uiBuilt)
            return;

        foreach (SkillCard card in _cards)
            card?.RefreshTextSizeFromSettings();

        Node pileOverlayCardRoot =
            _pileOverlaySections != null ? _pileOverlaySections : _pileOverlayGrid;
        if (pileOverlayCardRoot != null && GodotObject.IsInstanceValid(pileOverlayCardRoot))
        {
            foreach (SkillCard card in EnumerateSkillCards(pileOverlayCardRoot))
                card.RefreshTextSizeFromSettings();
        }

        RefreshTurnUi();
    }

    public void RefreshManualTargetCardVisibilityFromSettings()
    {
        ApplyManualTargetPickerTemporaryHiddenState();
    }

    public void QueueHandReorderAnimation(Skill[] oldHand, Skill[] newHand)
    {
        if (oldHand == null || newHand == null || _cardSlots == null)
            return;

        int max = Math.Min(Math.Min(oldHand.Length, newHand.Length), _cardSlots.Length);
        for (int newIndex = 0; newIndex < max; newIndex++)
        {
            Skill skill = newHand[newIndex];
            if (skill == null)
                continue;

            int oldIndex = Array.FindIndex(oldHand, oldSkill => ReferenceEquals(oldSkill, skill));
            if (oldIndex < 0 || oldIndex == newIndex || oldIndex >= _cardSlots.Length)
                continue;

            Control oldSlot = _cardSlots[oldIndex];
            if (oldSlot == null || !GodotObject.IsInstanceValid(oldSlot))
                continue;

            if (TryDetachDrawEntryStateForHandReorder(oldIndex, out Vector2 drawEntryStart))
            {
                _pendingHandReorderStarts[skill] = (
                    drawEntryStart,
                    0f,
                    HandDrawEntryPixelsPerSecond
                );
                continue;
            }

            _pendingHandReorderStarts[skill] = (
                NormalizeHandReorderStartPosition(oldSlot.Position),
                0f,
                0f
            );
        }
    }

    public void QueueBatchDrawMakeRoomAnimation(Skill[] oldHand, Skill[] newHand)
    {
        if (oldHand == null || newHand == null)
            return;

        int length = Math.Min(oldHand.Length, newHand.Length);
        for (int i = 0; i < length; i++)
        {
            if (newHand[i] != null && oldHand[i] == null)
                MarkHandDrawEntryFromDrawPile(i);
        }
    }

    private static bool ContainsSkillReference(Skill[] hand, Skill skill)
    {
        if (hand == null || skill == null)
            return false;

        return hand.Any(oldSkill => ReferenceEquals(oldSkill, skill));
    }

    private Skill[] GetActiveHandSkills()
    {
        if (_activePlayer == null || !GodotObject.IsInstanceValid(_activePlayer))
            return null;

        Skill[] teamHand = BattleNode?.GetPlayerTeamBattleHand();
        if (
            BattleNode?.IsResolvingPlayerTeamActionPhase == true
            || teamHand?.Any(skill => skill != null) == true
        )
            return teamHand;

        return _activePlayer.Skills;
    }

}
