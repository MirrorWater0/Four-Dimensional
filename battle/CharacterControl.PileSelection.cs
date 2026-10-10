using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private readonly struct PileCardDiscardAnimationEntry
    {
        public PileCardDiscardAnimationEntry(SkillCard sourceCard, Skill skill)
        {
            Skill = skill;
            CharacterName = sourceCard.CharacterName.Text;
            GlobalPosition = sourceCard.GlobalPosition;
            Scale = sourceCard.Scale;
            Rotation = GetCanvasRotation(sourceCard);
            PivotOffset = sourceCard.PivotOffset;
            Modulate = sourceCard.Modulate;
        }

        public Skill Skill { get; }
        public string CharacterName { get; }
        public Vector2 GlobalPosition { get; }
        public Vector2 Scale { get; }
        public float Rotation { get; }
        public Vector2 PivotOffset { get; }
        public Color Modulate { get; }
    }

    private readonly List<PileCardDiscardAnimationEntry> _pileCardDiscardAnimationEntries =
        new();
    private readonly List<Task> _pileCardDiscardAnimationTasks = new();

    private void ConfigurePileSelectionCard(SkillCard card, BattlePileKind kind, int pileIndex)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        ClearPileSelectionCardBinding(card);
        bool selectable =
            _isPileCardSelectionActive
            && !_pileOverlayContentTemporarilyHidden
            && kind == _pileCardSelectionKind
            && pileIndex >= 0;
        if (!selectable)
            return;

        _pileCardSelectionCards[pileIndex] = card;
        card.Button.ToggleMode = true;
        bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
        card.Button.ButtonPressed = selected;
        SetPileSelectionCardPointerInputEnabled(card, enabled: true);
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(selected);
        Action handler = () => _ = HandlePileCardSelectionPressedAsync(pileIndex);
        _pileCardSelectionPressHandlers[card] = handler;
        card.Button.Pressed += handler;
    }

    private void ClearPileSelectionCardBinding(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (_pileCardSelectionPressHandlers.TryGetValue(card, out Action handler))
        {
            card.Button.Pressed -= handler;
            _pileCardSelectionPressHandlers.Remove(card);
        }

        _pileCardSelectionRemovalBuffer.Clear();
        foreach (KeyValuePair<int, SkillCard> pair in _pileCardSelectionCards)
        {
            if (ReferenceEquals(pair.Value, card))
                _pileCardSelectionRemovalBuffer.Add(pair.Key);
        }
        for (int i = 0; i < _pileCardSelectionRemovalBuffer.Count; i++)
            _pileCardSelectionCards.Remove(_pileCardSelectionRemovalBuffer[i]);
        _pileCardSelectionRemovalBuffer.Clear();

        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        SetPileSelectionCardPointerInputEnabled(card, enabled: true);
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(false, instant: true);
    }

    private void SetPileSelectionCardPointerInputEnabled(SkillCard card, bool enabled)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        MouseFilterEnum mouseFilter = enabled
            ? MouseFilterEnum.Stop
            : MouseFilterEnum.Ignore;
        card.MouseFilter = mouseFilter;
        if (card.Button != null && GodotObject.IsInstanceValid(card.Button))
            card.Button.MouseFilter = mouseFilter;

        card.SetTransientPointerInputDisabled(!enabled, refreshHoverWhenEnabled: enabled);
        if (!enabled)
            card.HideHoverUi();
    }

    private void SetPileSelectionCardPointerInputEnabled(bool enabled)
    {
        foreach (SkillCard card in _pileCardSelectionPressHandlers.Keys)
            SetPileSelectionCardPointerInputEnabled(card, enabled);

        foreach (SkillCard card in _pileCardSelectionCards.Values)
            SetPileSelectionCardPointerInputEnabled(card, enabled);
    }

    private Task HandlePileCardSelectionPressedAsync(int pileIndex)
    {
        if (
            !_isPileCardSelectionActive
            || pileIndex < 0
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
        )
        {
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Contains(pileIndex))
        {
            _pileCardSelectionIndexes.Remove(pileIndex);
            RefreshPileCardSelectionVisuals();
            RequestTurnUiRefresh();
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Count >= _pileCardSelectionTargetCount)
        {
            _pileCardSelectionIndexes.RemoveAt(_pileCardSelectionIndexes.Count - 1);
        }

        _pileCardSelectionIndexes.Add(pileIndex);
        RefreshPileCardSelectionVisuals();
        RequestTurnUiRefresh();
        return Task.CompletedTask;
    }

    private async Task CompletePileCardSelectionAsync()
    {
        if (!_isPileCardSelectionActive)
            return;

        if (
            !_pileCardSelectionAllowsFewer
            && _pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount
        )
        {
            RefreshPileCardSelectionVisuals();
            RequestTurnUiRefresh();
            return;
        }

        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        bool exhaustSelected = _pileCardSelectionAction == PileCardSelectionAction.Exhaust;
        bool applyKeywordSelected = _pileCardSelectionAction == PileCardSelectionAction.ApplyKeyword;
        bool transformSelected = _pileCardSelectionAction == PileCardSelectionAction.Transform;
        bool filterSelected =
            _pileCardSelectionAction == PileCardSelectionAction.FilterToDiscard;
        BattleCardKeyword selectedKeyword = _pileCardSelectionKeyword;
        SkillID transformSkillId = _pileCardSelectionTransformSkillId;
        List<int> selectedIndexes = CopySortedUniquePileSelectionIndexes();
        BattlePileKind selectedKind = _pileCardSelectionKind;
        PlayerCharacter selectingPlayer = _activePlayer;
        if (filterSelected)
            CaptureSelectedDrawPileDiscardAnimationEntries(selectedIndexes);

        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionKeyword = BattleCardKeyword.Retain;
        _pileCardSelectionTransformSkillId = SkillID.None;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionAllowsFewer = false;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        ClearAllPileSelectionCardBindings();
        ClearPileCardSelectionOverlaySnapshot();
        HidePileOverlay();
        RequestTurnUiRefresh(refreshHover: true);

        int selectedCount = filterSelected
            ? await MoveSelectedDrawPileCardsToDiscardWithAnimationAsync(selectedIndexes)
            : transformSelected
                ? TransformSelectedPileCards(
                    selectingPlayer,
                    selectedKind,
                    selectedIndexes,
                    transformSkillId
                )
                : applyKeywordSelected
                    ? ApplyKeywordToSelectedPileCards(selectingPlayer, selectedKind, selectedIndexes, selectedKeyword)
                    : exhaustSelected
                        ? await ExhaustSelectedPileCardsWithAnimationAsync(
                            selectingPlayer,
                            selectedKind,
                            selectedIndexes
                        )
                        : MoveSelectedPileCardsToHand(selectedKind, selectedIndexes);
        selectedIndexes.Clear();
        completion?.TrySetResult(selectedCount);
    }

    private List<int> CopySortedUniquePileSelectionIndexes()
    {
        _pileCardSelectionIndexBuffer.Clear();
        for (int i = 0; i < _pileCardSelectionIndexes.Count; i++)
            _pileCardSelectionIndexBuffer.Add(_pileCardSelectionIndexes[i]);

        _pileCardSelectionIndexBuffer.Sort();
        int write = 0;
        int previous = int.MinValue;
        for (int read = 0; read < _pileCardSelectionIndexBuffer.Count; read++)
        {
            int value = _pileCardSelectionIndexBuffer[read];
            if (read > 0 && value == previous)
                continue;

            _pileCardSelectionIndexBuffer[write++] = value;
            previous = value;
        }
        if (write < _pileCardSelectionIndexBuffer.Count)
            _pileCardSelectionIndexBuffer.RemoveRange(write, _pileCardSelectionIndexBuffer.Count - write);

        return _pileCardSelectionIndexBuffer;
    }

    private int ApplyKeywordToSelectedPileCards(
        PlayerCharacter player,
        BattlePileKind kind,
        IReadOnlyList<int> selectedIndexes,
        BattleCardKeyword keyword
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || selectedIndexes == null
            || selectedIndexes.Count == 0
        )
        {
            return 0;
        }

        Battle.BattleCardPileEntry[] pile = GetPileEntriesForSelection(player, kind);
        int applied = 0;
        for (int i = 0; i < selectedIndexes.Count; i++)
        {
            int index = selectedIndexes[i];
            if (index < 0 || index >= pile.Length)
                continue;

            Battle.BattleCardPileEntry entry = pile[index];
            PlayerCharacter owner = entry.Owner ?? player;
            if (owner == null)
                continue;

            BattleNode.AddBattleCardKeyword(entry.InstanceId, keyword);
            owner.InvalidateSkillTooltipCache();
            applied++;
        }

        player?.InvalidateSkillTooltipCache();

        return applied;
    }

    private int TransformSelectedPileCards(
        PlayerCharacter player,
        BattlePileKind kind,
        IReadOnlyList<int> selectedIndexes,
        SkillID replacementSkillId
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || selectedIndexes == null
            || selectedIndexes.Count == 0
        )
        {
            return 0;
        }

        BattleCardPileTarget pileTarget = kind == BattlePileKind.Discard
            ? BattleCardPileTarget.DiscardPileCards
            : BattleCardPileTarget.DrawPileCards;
        int transformedCount = 0;
        for (int i = 0; i < selectedIndexes.Count; i++)
        {
            if (
                BattleNode.TryTransformBattlePileCard(
                    pileTarget,
                    selectedIndexes[i],
                    replacementSkillId,
                    player,
                    refreshUi: false
                )
            )
            {
                transformedCount++;
            }
        }

        if (transformedCount > 0)
            RequestTurnUiRefresh(refreshHover: true);
        return transformedCount;
    }

    private async Task<int> ExhaustSelectedPileCardsWithAnimationAsync(
        PlayerCharacter player,
        BattlePileKind kind,
        IReadOnlyList<int> selectedIndexes
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || selectedIndexes == null
            || selectedIndexes.Count == 0
        )
        {
            return 0;
        }

        Battle.BattleCardPileEntry[] pile = GetPileEntriesForSelection(player, kind);
        List<StatusCardExhaustAnimationEntry> animationEntries =
            _pileCardSelectionExhaustAnimationEntries;
        animationEntries.Clear();
        for (int i = 0; i < selectedIndexes.Count; i++)
        {
            int index = selectedIndexes[i];
            if (index < 0 || index >= pile.Length)
                continue;

            Battle.BattleCardPileEntry entry = pile[index];
            PlayerCharacter owner = IsStatusSkillId(entry.SkillId)
                ? player
                : entry.Owner ?? player;
            animationEntries.Add(new StatusCardExhaustAnimationEntry(owner, entry.SkillId));
        }

        if (animationEntries.Count == 0)
            return 0;

        await PlayOwnedCardExhaustPreviewAnimationAsync(animationEntries);
        animationEntries.Clear();
        return ExhaustSelectedPileCards(kind, selectedIndexes);
    }

    private int ExhaustSelectedPileCards(
        BattlePileKind kind,
        IReadOnlyList<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        BattleCardPileTarget pileTarget = kind == BattlePileKind.Discard
            ? BattleCardPileTarget.DiscardPileCards
            : BattleCardPileTarget.DrawPileCards;
        int exhaustedCount = 0;
        int removedBefore = 0;
        int count = selectedIndexes?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            int originalIndex = selectedIndexes[i];
            int currentIndex = originalIndex - removedBefore;
            if (!BattleNode.TryExhaustBattlePileCard(pileTarget, currentIndex, refreshUi: false))
                continue;

            exhaustedCount++;
            removedBefore++;
        }

        return exhaustedCount;
    }

    private int MoveSelectedPileCardsToHand(
        BattlePileKind kind,
        IReadOnlyList<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        Skill[] oldHand = (Skill[])GetActiveHandSkills().Clone();
        int movedCount = 0;
        int removedBefore = 0;
        int count = selectedIndexes?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            int originalIndex = selectedIndexes[i];
            int currentIndex = originalIndex - removedBefore;
            bool moved = kind switch
            {
                BattlePileKind.Draw => BattleNode.TryMoveDrawPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                BattlePileKind.Discard => BattleNode.TryMoveDiscardPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                _ => false,
            };
            if (!moved)
                continue;

            movedCount++;
            removedBefore++;
        }

        if (movedCount > 0)
        {
            Skill[] hand = GetActiveHandSkills();
            SyncHandSlotIdentities(hand);
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] != null && !ContainsSkillReference(oldHand, hand[i]))
                    SetHandEntryStartPositionFromPileKind(i, kind);
            }
        }

        return movedCount;
    }

    private void CaptureSelectedDrawPileDiscardAnimationEntries(
        IReadOnlyList<int> selectedIndexes
    )
    {
        _pileCardDiscardAnimationEntries.Clear();
        if (selectedIndexes == null)
            return;

        for (int i = 0; i < selectedIndexes.Count; i++)
        {
            if (
                !_pileCardSelectionCards.TryGetValue(selectedIndexes[i], out SkillCard sourceCard)
                || sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
                || sourceCard.CurrentSkill == null
            )
            {
                continue;
            }

            _pileCardDiscardAnimationEntries.Add(
                new PileCardDiscardAnimationEntry(sourceCard, sourceCard.CurrentSkill)
            );
        }
    }

    private async Task<int> MoveSelectedDrawPileCardsToDiscardWithAnimationAsync(
        IReadOnlyList<int> selectedIndexes
    )
    {
        int movedCount = MoveSelectedDrawPileCardsToDiscard(selectedIndexes);
        List<PileCardDiscardAnimationEntry> animationEntries =
            _pileCardDiscardAnimationEntries;
        if (movedCount <= 0 || animationEntries.Count == 0 || !IsInsideTree())
        {
            animationEntries.Clear();
            return movedCount;
        }

        try
        {
            await ToSignal(
                GetTree().CreateTimer(PileOverlayContentMoveDuration),
                SceneTreeTimer.SignalName.Timeout
            );

            List<Task> animationTasks = _pileCardDiscardAnimationTasks;
            animationTasks.Clear();
            for (int i = 0; i < animationEntries.Count; i++)
            {
                SkillCard card = CreatePileCardDiscardAnimationCard(
                    animationEntries[i],
                    PlayedCardZIndex + i + 1
                );
                if (card != null)
                    animationTasks.Add(PlayCardDiscardFlyAndFreeAsync(card));
            }

            for (int i = 0; i < animationTasks.Count; i++)
                await animationTasks[i];
            animationTasks.Clear();
        }
        finally
        {
            _pileCardDiscardAnimationTasks.Clear();
            animationEntries.Clear();
        }

        return movedCount;
    }

    private SkillCard CreatePileCardDiscardAnimationCard(
        PileCardDiscardAnimationEntry entry,
        int zIndex
    )
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null || !GodotObject.IsInstanceValid(overlay) || entry.Skill == null)
            return null;

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "PileDiscardCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.ConfigureDisplayScale(PileCardScale);
        overlay.AddChild(card);
        card.ResetState();
        card.SetSkill(entry.Skill);
        card.CharacterName.Text = entry.CharacterName ?? string.Empty;
        card.ConfigurePilePreviewVisuals();
        card.Visible = true;
        card.GlobalPosition = entry.GlobalPosition;
        card.Scale = entry.Scale;
        card.Rotation = entry.Rotation;
        card.PivotOffset = entry.PivotOffset;
        card.Modulate = entry.Modulate;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = zIndex;
        return card;
    }

    private int MoveSelectedDrawPileCardsToDiscard(IReadOnlyList<int> selectedIndexes)
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        int movedCount = 0;
        int removedBefore = 0;
        int count = selectedIndexes?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            int currentIndex = selectedIndexes[i] - removedBefore;
            if (!BattleNode.TryMoveDrawPileCardToDiscard(currentIndex, refreshUi: false))
                continue;

            movedCount++;
            removedBefore++;
        }

        if (movedCount > 0)
            RequestTurnUiRefresh(refreshHover: true);
        return movedCount;
    }

    private Vector2? GetPlayedCardDrawEntryStartGlobalCenter()
    {
        Vector2? liveCenter = TryGetQueuedPlayCardGlobalCenter(_activeExecutingCardPlay);
        if (liveCenter.HasValue)
            return liveCenter;

        liveCenter = TryGetQueuedPlayCardGlobalCenter(_activeQueuedCardPlay);
        if (liveCenter.HasValue)
            return liveCenter;

        return _activeExecutingCardPlay?.CachedDrawEntryStartCenter
            ?? _activeQueuedCardPlay?.CachedDrawEntryStartCenter;
    }

    private static Vector2? TryGetQueuedPlayCardGlobalCenter(QueuedCardPlay play)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.IsInsideTree())
            return null;

        return card.GetGlobalRect().GetCenter();
    }

    private void SetHandEntryStartPositionFromPileKind(int handIndex, BattlePileKind kind)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        Button sourceButton = kind switch
        {
            BattlePileKind.Draw => _drawPileButton,
            BattlePileKind.Discard => _discardPileButton,
            BattlePileKind.Exhausted => _exhaustedPileButton,
            _ => null,
        };
        Vector2? start = GetHandEntryStartPositionFromButton(sourceButton);
        if (start.HasValue)
            _customDrawEntryStartPositions[handIndex] = start.Value;
    }

    private Vector2? GetHandEntryStartPositionFromButton(Button button)
    {
        if (
            button == null
            || !GodotObject.IsInstanceValid(button)
            || !button.IsInsideTree()
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return null;
        }

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        Vector2 sourceCenter = GetPileButtonVisualCenter(button);
        Vector2 cardRowOrigin = _cardRow.GetGlobalRect().Position;
        return sourceCenter - cardRowOrigin - cardSize * 0.5f;
    }

    private void RefreshPileCardSelectionVisuals()
    {
        foreach ((int pileIndex, SkillCard card) in _pileCardSelectionCards)
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
            card.Button.ButtonPressed = selected;
            card.Modulate = SkillButton.EnabledModulate;
            card.SetPlayableHighlight(selected);
        }
        SyncPileOverlaySelectionButtons();
    }

    private void CancelPileCardSelection()
    {
        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionKeyword = BattleCardKeyword.Retain;
        _pileCardSelectionTransformSkillId = SkillID.None;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionAllowsFewer = false;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        _pileOverlayContentTemporarilyHidden = false;
        ClearAllPileSelectionCardBindings();
        ClearPileCardSelectionOverlaySnapshot();
        SyncPileOverlaySelectionButtons();
        completion?.TrySetResult(0);
    }

    private void ClearAllPileSelectionCardBindings()
    {
        foreach ((SkillCard card, Action handler) in _pileCardSelectionPressHandlers)
        {
            if (card != null && GodotObject.IsInstanceValid(card))
            {
                card.Button.Pressed -= handler;
                card.Button.ToggleMode = false;
                card.Button.ButtonPressed = false;
                SetPileSelectionCardPointerInputEnabled(card, enabled: true);
                card.Modulate = SkillButton.EnabledModulate;
                card.SetPlayableHighlight(false, instant: true);
            }
        }

        _pileCardSelectionPressHandlers.Clear();
    }

}
