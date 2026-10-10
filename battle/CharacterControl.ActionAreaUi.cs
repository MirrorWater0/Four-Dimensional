using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    public void RefreshCurrentTurnUi()
    {
        RefreshTurnUi();
        if (BattleNode?.IsAutomationBattleLogActive == true)
            BattleNode.RecordAutomationSnapshot("ui_refreshed");
    }

    public Dictionary<string, object> GetAutomationUiState()
    {
        string selectionMode = "none";
        var selection = new Dictionary<string, object>();
        if (_isDiscardSelectionActive)
        {
            selectionMode = _discardSelectionExhaustMode ? "discard_selection_exhaust" : "discard_selection_discard";
            selection["targetCount"] = _discardSelectionTargetCount;
            selection["selectedCount"] = _discardSelectionSkills.Count;
            selection["remaining"] = Math.Max(0, _discardSelectionTargetCount - _discardSelectionSkills.Count);
            selection["selectedSlots"] = BuildDiscardSelectionAutomationSlots();
        }
        else if (_isPileCardSelectionActive)
        {
            selectionMode =
                _pileCardSelectionAction == PileCardSelectionAction.Exhaust
                    ? "pile_selection_exhaust"
                    : _pileCardSelectionAction == PileCardSelectionAction.FilterToDiscard
                        ? "pile_selection_filter"
                        : "pile_selection_to_hand";
            selection["pile"] = _pileCardSelectionKind.ToString();
            selection["targetCount"] = _pileCardSelectionTargetCount;
            selection["selectedCount"] = _pileCardSelectionIndexes.Count;
            selection["allowsFewer"] = _pileCardSelectionAllowsFewer;
            selection["remaining"] = Math.Max(
                0,
                _pileCardSelectionTargetCount - _pileCardSelectionIndexes.Count
            );
            selection["selectedPileIndexes"] = BuildPileSelectionAutomationIndexes();
        }
        else if (_manualTargetArrowSelectionActive)
        {
            selectionMode = "manual_friendly_target";
        }

        return new Dictionary<string, object>
        {
            ["statusText"] = _statusLabel?.Text,
            ["selectionMode"] = selectionMode,
            ["selection"] = selection,
            ["endTurnDisabled"] = _endTurnButton?.Disabled ?? true,
            ["isResolvingCard"] = _isResolvingCard,
            ["isProcessingCardQueue"] = _isProcessingCardQueue,
            ["queuedCardCount"] = _queuedCardPlays.Count + _queuedFollowUpCardPlays.Count,
        };
    }

    private bool TryBindActionAreaUiFromScene()
    {
        _root = GetNodeOrNull<Control>("ActionAreaRoot");
        _statusLabel = GetNodeOrNull<Label>("ActionAreaRoot/StatusLabel");
        _cardRow = GetNodeOrNull<Control>("ActionAreaRoot/CardRow");
        Control actionButtonsRoot = GetActionButtonsRootFromScene();
        _endTurnButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("EndTurnButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/EndTurnButton")
            ?? GetNodeOrNull<Button>(
                "../../CharacterControlLayer/BattleActionButtons/EndTurnButton"
            )
            ?? GetNodeOrNull<Button>("EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/CardRow/EndTurnButton");
        _drawPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("DrawPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/DrawPileButton")
            ?? GetNodeOrNull<Button>(
                "../../CharacterControlLayer/BattleActionButtons/DrawPileButton"
            );
        _discardPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("DiscardPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/DiscardPileButton")
            ?? GetNodeOrNull<Button>(
                "../../CharacterControlLayer/BattleActionButtons/DiscardPileButton"
            );
        _exhaustedPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("ExhaustedPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/ExhaustedPileButton")
            ?? GetNodeOrNull<Button>(
                "../../CharacterControlLayer/BattleActionButtons/ExhaustedPileButton"
            );

        if (_root == null || _statusLabel == null || _cardRow == null)
            throw new InvalidOperationException("Battle.tscn must contain ActionAreaRoot, StatusLabel and CardRow.");

        _cardRow.Resized += LayoutActionCards;

        for (int i = 0; i < _cards.Length; i++)
        {
            Control cardSlot = GetNodeOrNull<Control>($"ActionAreaRoot/CardRow/CardSlot{i}");
            if (cardSlot == null)
            {
                cardSlot = CreateCardSlot(i);
                _cardRow.AddChild(cardSlot);
            }

            SkillCard card =
                cardSlot.GetNodeOrNull<SkillCard>($"Card{i}")
                ?? FindChildSkillCard(cardSlot);
            if (card == null)
            {
                card = CreateBattleCard(i);
                cardSlot.AddChild(card);
            }

            _cardSlots[i] = cardSlot;
            // Editor preview may hide unused slots; gameplay controls card visibility.
            cardSlot.Visible = true;
            cardSlot.CustomMinimumSize = BattleCardBaseSize * BattleCardScale;
            cardSlot.MouseFilter = MouseFilterEnum.Ignore;
            cardSlot.ClipContents = false;
            WireBattleCard(card, i);
            _cards[i] = card;
        }

        EnsureHandInputBlocker();
        WireActionButtonsFromScene();
        ApplyMobileActionButtonLayout();
        LayoutActionCards(instant: true);
        return true;
    }

    private Control GetActionButtonsRootFromScene()
    {
        return BattleNode?.GetNodeOrNull<Control>("CharacterControlLayer/BattleActionButtons")
            ?? GetParent()?.GetNodeOrNull<Control>("BattleActionButtons");
    }

    private void WireActionButtonsFromScene()
    {
        if (_endTurnButton != null)
        {
            _endTurnButton.Text = "结束回合";
            _endTurnButton.Disabled = true;
            ConfigureEndTurnButton(_endTurnButton);
            _endTurnButton.Pressed += OnEndTurnPressed;
        }

        if (_drawPileButton != null)
        {
            ConfigurePileButton(_drawPileButton, "抽牌堆");
            _drawPileButton.Disabled = true;
            SyncPileButtonVisualState(_drawPileButton);
            _drawPileButton.Pressed += OnDrawPilePressed;
        }

        if (_discardPileButton != null)
        {
            ConfigurePileButton(_discardPileButton, "弃牌堆");
            _discardPileButton.Disabled = true;
            SyncPileButtonVisualState(_discardPileButton);
            _discardPileButton.Pressed += OnDiscardPilePressed;
        }

        if (_exhaustedPileButton != null)
        {
            ConfigurePileButton(_exhaustedPileButton, "消耗牌堆");
            _exhaustedPileButton.Disabled = true;
            SyncPileButtonVisualState(_exhaustedPileButton);
            _exhaustedPileButton.Pressed += OnExhaustedPilePressed;
        }
    }

    private void ApplyMobileActionButtonLayout()
    {
        if (!MobilePlatform.IsMobile)
            return;

        MobilePlatform.EnsureMinimumTouchTarget(_endTurnButton, 168f, 88f);
        MobilePlatform.EnsureMinimumTouchTarget(_drawPileButton, 80f, 80f);
        MobilePlatform.EnsureMinimumTouchTarget(_discardPileButton, 80f, 80f);
        MobilePlatform.EnsureMinimumTouchTarget(_exhaustedPileButton, 80f, 80f);
    }

    private void WireBattleCard(SkillCard card, int index)
    {
        if (card == null)
            return;

        if (card.ConfiguredDisplayScale.DistanceSquaredTo(BattleCardScale) > 0.0001f)
            card.ConfigureDisplayScale(BattleCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.Button.ActionMode = BaseButton.ActionModeEnum.Press;
        card.Button.SetMeta("suppress_ui_click_sfx", true);
        card.Button.SetMeta("suppress_ui_hover_sfx", true);
        SetCardButtonInputEnabled(card, false);
        card.SetMeta("hand_card_index", index);

        if (_wiredBattleCards.Contains(card))
            return;

        _wiredBattleCards.Add(card);

        card.Button.Pressed += () =>
        {
            int cardIndex = GetBattleCardSignalIndex(card);
            if (IsCardIndexValid(cardIndex))
                TryStartHandCardPress(cardIndex);
        };
        card.Button.MouseEntered += () =>
        {
            int cardIndex = GetBattleCardSignalIndex(card);
            if (
                IsCardIndexValid(cardIndex)
                && CanHoverHandCardAt(cardIndex)
                && SetCardHovered(cardIndex, true)
            )
            {
                SetCardHoverPreviewActive(cardIndex, true);
                return;
            }

            ScheduleCardHoverRefresh();
        };
        card.Button.MouseExited += () =>
        {
            int cardIndex = GetBattleCardSignalIndex(card);
            if (IsCardIndexValid(cardIndex) && cardIndex != _liftedCardIndex)
            {
                SetCardHovered(cardIndex, false);
                SetCardHoverPreviewActive(cardIndex, false);
            }

            ScheduleCardHoverRefresh();
        };
    }

    private static int GetBattleCardSignalIndex(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.HasMeta("hand_card_index"))
            return -1;

        Variant value = card.GetMeta("hand_card_index");
        return value.VariantType == Variant.Type.Int ? value.AsInt32() : -1;
    }

    private void PrewarmBattleCardPool()
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
            return;

        while (_battleCardPool.Count < BattleCardPoolPrewarmCount)
        {
            SkillCard card = CreateBattleCard(-1);
            PrepareBattleCardForPool(card);
            overlay.AddChild(card);
            _battleCardPool.Enqueue(card);
        }
    }

    private SkillCard TakeBattleCardFromPool(int index)
    {
        SkillCard card = null;
        while (_battleCardPool.Count > 0)
        {
            card = _battleCardPool.Dequeue();
            if (card != null && GodotObject.IsInstanceValid(card))
                break;

            card = null;
        }

        card ??= CreateBattleCard(index);
        card.Name = $"Card{index}";
        WireBattleCard(card, index);
        card.RestoreDisplayState();
        card.SetSkill(null);
        card.SetHoverUiEnabled(true);
        card.SetRelatedCardPreviewSuppressed(false);
        card.SetHandIndexBadge(0, visible: false);
        return card;
    }

    private void ReturnBattleCardToPool(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        RemoveHandCardIdentity(card.CurrentSkill, card);
        int cardIndex = FindCardSlotIndex(card);
        if (IsCardIndexValid(cardIndex))
            _cards[cardIndex] = null;

        if (_battleCardPool.Count >= BattleCardPoolMaxCount)
        {
            _wiredBattleCards.Remove(card);
            card.QueueFree();
            return;
        }

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
        {
            _wiredBattleCards.Remove(card);
            card.QueueFree();
            return;
        }

        Node parent = card.GetParent();
        if (parent != overlay)
        {
            parent?.RemoveChild(card);
            overlay.AddChild(card);
        }

        PrepareBattleCardForPool(card);
        _battleCardPool.Enqueue(card);
    }

    private void PrepareBattleCardForPool(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Name = $"PooledBattleCard{_battleCardPoolSerial++}";
        card.RestoreDisplayState();
        card.SetSkill(null);
        card.Visible = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.ZIndex = 0;
        card.SetMeta("hand_card_index", -1);
        card.HideHoverUi();
        card.SetHoverUiEnabled(false);
        card.SetRelatedCardPreviewSuppressed(true);
        card.SetHandIndexBadge(0, visible: false);
        SetCardButtonInputEnabled(card, false);
    }

    private void RefreshHandCardIndexBadges(Skill[] hand)
    {
        UserSettings.EnsureLoaded();
        bool showIndices = UserSettings.ShowHandCardIndices;
        Array.Fill(_handLayoutOrderBySlotIndex, 0);
        int visibleCount = FillVisibleHandSlotIndexes(
            _visibleHandSlotIndexesBuffer,
            excludeLiftedCard: false
        );
        for (int order = 0; order < visibleCount; order++)
            _handLayoutOrderBySlotIndex[_visibleHandSlotIndexesBuffer[order]] = order + 1;

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool hasVisibleSkill =
                hand != null
                && i < hand.Length
                && hand[i] != null
                && card.Visible;
            int order = hasVisibleSkill ? _handLayoutOrderBySlotIndex[i] : 0;
            card.SetHandIndexBadge(order, showIndices && order > 0);
        }
    }

    private int[] BuildDiscardSelectionAutomationSlots()
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || _discardSelectionSkills.Count == 0)
            return Array.Empty<int>();

        var slots = new int[_discardSelectionSkills.Count];
        int write = 0;
        for (int i = 0; i < _discardSelectionSkills.Count; i++)
        {
            int index = Array.IndexOf(hand, _discardSelectionSkills[i]);
            if (index >= 0)
                slots[write++] = index + 1;
        }

        if (write == slots.Length)
            return slots;

        if (write == 0)
            return Array.Empty<int>();

        Array.Resize(ref slots, write);
        return slots;
    }

    private int[] BuildPileSelectionAutomationIndexes()
    {
        if (_pileCardSelectionIndexes.Count == 0)
            return Array.Empty<int>();

        var indexes = new int[_pileCardSelectionIndexes.Count];
        for (int i = 0; i < _pileCardSelectionIndexes.Count; i++)
            indexes[i] = _pileCardSelectionIndexes[i];

        return indexes;
    }

    private static SkillCard FindChildSkillCard(Node parent)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.GetChildCount(); i++)
        {
            if (parent.GetChild(i) is SkillCard card)
                return card;
        }

        return null;
    }

    private void ClearHandCardIndexBadges()
    {
        if (_cards == null)
            return;

        foreach (SkillCard card in _cards)
        {
            if (card != null && GodotObject.IsInstanceValid(card))
                card.SetHandIndexBadge(0, visible: false);
        }
    }

    private void BuildActionAreaUi()
    {
        if (_uiBuilt)
            return;
        TryBindActionAreaUiFromScene();
        _uiBuilt = true;
        PrewarmBattleCardPool();
    }

    private Control CreateCardSlot(int index)
    {
        Control slot = HandSlotScene.Instantiate<Control>();
        slot.Name = $"CardSlot{index}";
        slot.GetNode<SkillCard>("Card").Name = $"Card{index}";
        return slot;
    }

    private SkillCard CreateBattleCard(int index)
    {
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"Card{index}";
        card.ConfigureDisplayScale(BattleCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        SetCardButtonInputEnabled(card, false);
        return card;
    }

    private void EnsureHandInputBlocker()
    {
        if (_cardRow != null && GodotObject.IsInstanceValid(_cardRow))
            _handInputBlocker = _cardRow.GetNode<Control>("HandInputBlocker");
    }

    private void SetHandInputBlockerVisible(bool visible)
    {
        EnsureHandInputBlocker();
        if (_handInputBlocker == null || !GodotObject.IsInstanceValid(_handInputBlocker))
            return;

        _handInputBlocker.Visible = visible;
        _handInputBlocker.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private static void SetCardButtonInputEnabled(SkillCard card, bool enabled)
    {
        if (card?.Button == null)
            return;

        bool disabled = !enabled;
        MouseFilterEnum mouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (card.Button.Disabled != disabled)
            card.Button.Disabled = disabled;
        if (card.Button.MouseFilter != mouseFilter)
            card.Button.MouseFilter = mouseFilter;
    }

    private static void ConfigureEndTurnButton(Button button)
    {
        if (button == null)
            return;

        button.FocusMode = FocusModeEnum.None;
        button.MouseFilter = MouseFilterEnum.Stop;
    }

}
