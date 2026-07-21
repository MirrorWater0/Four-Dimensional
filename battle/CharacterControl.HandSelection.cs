using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    public void SetPlayerInputsEnabled(PlayerCharacter player, bool enabled)
    {
        if (player == null || player != _activePlayer || !_uiBuilt)
            return;

        if (
            !enabled
            && (
                _isProcessingCardQueue
                || _queuedCardPlays.Count > 0
                || _queuedFollowUpCardPlays.Count > 0
            )
        )
        {
            RequestTurnUiRefresh();
            return;
        }

        _isResolvingCard = !enabled;
        if (enabled)
            _isResolvingEndTurn = false;
        if (!enabled)
            ClearLiftedCard(instant: false);
        RefreshTurnUi();
    }

    public SkillCard GetCardSlot(int index)
    {
        if (index < 0 || index >= _cards.Length)
            return null;

        return _cards[index];
    }

    public bool CanAnimateHandCardsFor(PlayerCharacter player)
    {
        return player != null && player == _activePlayer && _uiBuilt && IsInsideTree();
    }

    public async Task<int> SelectAndDiscardHandCardsAsync(PlayerCharacter player, int count)
    {
        return await SelectHandCardsForDiscardOrExhaustAsync(player, count, exhaustMode: false);
    }

    public async Task<int> SelectAndExhaustHandCardsAsync(PlayerCharacter player, int count)
    {
        return await SelectHandCardsForDiscardOrExhaustAsync(player, count, exhaustMode: true);
    }

    public Task<int> SelectHandCardsForKeywordAsync(
        PlayerCharacter player,
        int count,
        BattleCardKeyword keyword
    ) => SelectHandCardsForKeywordApplyAsync(player, count, keyword);

    public Task<int> SelectHandCardsToTransformAsync(
        PlayerCharacter player,
        int count,
        SkillID replacementSkillId = SkillID.None
    ) => SelectHandCardsForTransformAsync(player, count, replacementSkillId);

    private async Task<int> SelectHandCardsForTransformAsync(
        PlayerCharacter player,
        int count,
        SkillID replacementSkillId
    )
    {
        if (count <= 0)
            return 0;

        BuildActionAreaUi();
        if (
            !_uiBuilt
            || !IsInsideTree()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || player == null
            || !GodotObject.IsInstanceValid(player)
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
        )
        {
            return 0;
        }

        Skill[] hand = GetActiveHandSkills();
        int availableCount = 0;
        if (hand != null)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i]?.SkillId.HasValue == true)
                    availableCount++;
            }
        }
        if (availableCount <= 0)
            return 0;

        CancelDiscardSelection();
        _discardSelectionTargetCount = Math.Min(count, availableCount);
        _discardSelectionCompletion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _isDiscardSelectionActive = true;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionKeywordMode = false;
        _discardSelectionTransformMode = true;
        _discardSelectionTransformSkillId = replacementSkillId;
        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionSelectedCards();
        ResetDiscardSelectionTemporaryHideState();
        EnsureDiscardSelectionScreenMask();
        EnsureDiscardSelectionHideButton();
        ClearLiftedCard(instant: false);
        HideManualTargetPicker();
        HidePileOverlay();
        RefreshTurnUi();

        int selectedCount = await _discardSelectionCompletion.Task;
        return Math.Max(0, selectedCount);
    }

    private async Task<int> SelectHandCardsForKeywordApplyAsync(
        PlayerCharacter player,
        int count,
        BattleCardKeyword keyword
    )
    {
        if (count <= 0)
            return 0;

        BuildActionAreaUi();
        if (
            !_uiBuilt
            || !IsInsideTree()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || player == null
            || !GodotObject.IsInstanceValid(player)
        )
        {
            return 0;
        }

        Skill[] hand = GetActiveHandSkills();
        int availableCount = 0;
        if (hand != null)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] != null)
                    availableCount++;
            }
        }
        if (availableCount <= 0)
            return 0;

        CancelDiscardSelection();
        _discardSelectionTargetCount = Math.Min(count, availableCount);
        _discardSelectionCompletion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _isDiscardSelectionActive = true;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionKeywordMode = true;
        _discardSelectionTransformMode = false;
        _discardSelectionTransformSkillId = SkillID.None;
        _discardSelectionKeyword = keyword;
        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionSelectedCards();
        ResetDiscardSelectionTemporaryHideState();
        EnsureDiscardSelectionScreenMask();
        EnsureDiscardSelectionHideButton();
        ClearLiftedCard(instant: false);
        HideManualTargetPicker();
        HidePileOverlay();
        RefreshTurnUi();

        int selectedCount = await _discardSelectionCompletion.Task;
        return Math.Max(0, selectedCount);
    }

    private async Task<int> SelectHandCardsForDiscardOrExhaustAsync(
        PlayerCharacter player,
        int count,
        bool exhaustMode
    )
    {
        if (count <= 0)
            return 0;

        BuildActionAreaUi();
        if (
            !_uiBuilt
            || !IsInsideTree()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || player == null
            || !GodotObject.IsInstanceValid(player)
        )
        {
            return 0;
        }

        Skill[] hand = GetActiveHandSkills();
        int availableCount = 0;
        if (hand != null)
        {
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i] != null)
                    availableCount++;
            }
        }
        if (availableCount <= 0)
            return 0;

        CancelDiscardSelection();
        _discardSelectionTargetCount = Math.Min(count, availableCount);
        _discardSelectionCompletion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _isDiscardSelectionActive = true;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = exhaustMode;
        _discardSelectionKeywordMode = false;
        _discardSelectionTransformMode = false;
        _discardSelectionTransformSkillId = SkillID.None;
        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionSelectedCards();
        ResetDiscardSelectionTemporaryHideState();
        EnsureDiscardSelectionScreenMask();
        EnsureDiscardSelectionHideButton();
        ClearLiftedCard(instant: false);
        HideManualTargetPicker();
        HidePileOverlay();
        RefreshTurnUi();

        int selectedCount = await _discardSelectionCompletion.Task;
        return Math.Max(0, selectedCount);
    }

    public async Task PlayHandCardExhaustAnimationAsync(
        PlayerCharacter player,
        IReadOnlyCollection<int> indexes,
        float duration = CardPlayVanishDuration
    )
    {
        if (
            player == null
            || player != _activePlayer
            || indexes == null
            || indexes.Count == 0
            || !IsInsideTree()
        )
        {
            return;
        }

        if (!PlayHandCardExhaustEffectsAtIndexes(indexes, duration))
            return;

        await WaitForHandCardExhaustAnimationAsync(duration);
    }

    private bool TryPlayHandCardExhaustEffectAt(int index, float duration)
    {
        if (!_uiBuilt || !IsCardIndexValid(index))
            return false;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
            return false;

        card.Button.Disabled = true;
        card.PlayExhaustEffect(duration);
        return true;
    }

    private bool PlayHandCardExhaustEffectsAtIndexes(
        IReadOnlyCollection<int> indexes,
        float duration
    )
    {
        if (indexes == null || indexes.Count == 0)
            return false;

        bool playedAny = false;
        foreach (int index in indexes)
        {
            if (TryPlayHandCardExhaustEffectAt(index, duration))
                playedAny = true;
        }

        return playedAny;
    }

    private async Task WaitForHandCardExhaustAnimationAsync(float duration)
    {
        await ToSignal(GetTree().CreateTimer(duration), SceneTreeTimer.SignalName.Timeout);
    }

    public async Task PlayStatusCardExhaustPreviewAnimationAsync(
        IReadOnlyList<StatusCardExhaustAnimationEntry> entries,
        float duration = CardPlayVanishDuration
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        var cards = _statusExhaustPreviewCards;
        cards.Clear();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scale = GetStatusExhaustPreviewScale(entries.Count, viewportSize);
        Vector2 cardSize = BattleCardBaseSize * scale;
        float gap = Math.Max(10f, cardSize.X * 0.12f);
        int columns = Math.Min(
            entries.Count,
            Math.Max(1, Mathf.FloorToInt(viewportSize.X * 0.62f / (cardSize.X + gap)))
        );
        int rows = Mathf.CeilToInt(entries.Count / (float)columns);
        float rowGap = Math.Max(10f, cardSize.Y * 0.08f);
        Vector2 start =
            viewportSize * 0.5f
            - new Vector2(
                columns * cardSize.X + Math.Max(0, columns - 1) * gap,
                rows * cardSize.Y + Math.Max(0, rows - 1) * rowGap
            ) * 0.5f;

        try
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Skill statusSkill = Skill.GetSkill(entries[i].StatusSkillId);
                if (statusSkill == null)
                    continue;

                SkillCard card = CreateStatusInsertPreviewCard(
                    statusSkill,
                    entries[i].Player,
                    entries[i].Player,
                    1,
                    scale
                );
                if (card == null)
                    continue;

                overlay.AddChild(card);
                card.RestoreDisplayState();
                card.Name = "StatusExhaustCard";
                card.ZIndex = TemporaryCardZIndex + i;
                card.Modulate = new Color(1f, 1f, 1f, 0f);
                int row = i / columns;
                int col = i % columns;
                card.GlobalPosition =
                    start + new Vector2(col * (cardSize.X + gap), row * (cardSize.Y + rowGap));
                cards.Add(card);
            }

            if (cards.Count == 0)
                return;

            float stagger = Math.Min(0.035f, 0.16f / Math.Max(1, cards.Count - 1));
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                float delay = stagger * i;
                Tween appearTween = card.CreateTween();
                appearTween.SetParallel(true);
                appearTween.TweenProperty(card, "modulate:a", 1f, 0.08f).SetDelay(delay);
                appearTween.TweenProperty(card, "scale", scale * 1.035f, 0.1f).SetDelay(delay);

                Tween exhaustTween = card.CreateTween();
                exhaustTween
                    .TweenCallback(Callable.From(() => card.PlayExhaustEffect(duration)))
                    .SetDelay(delay + 0.08f);
            }

            await ToSignal(
                GetTree().CreateTimer(duration + 0.12f + stagger * Math.Max(0, cards.Count - 1)),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.QueueFree();
            }
            cards.Clear();
        }
    }

    public async Task PlayOwnedCardExhaustPreviewAnimationAsync(
        IReadOnlyList<StatusCardExhaustAnimationEntry> entries,
        float duration = CardPlayVanishDuration
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        var cards = _ownedExhaustPreviewCards;
        cards.Clear();
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scale = GetOwnedCardExhaustPreviewScale(entries.Count, viewportSize);
        Vector2 cardSize = BattleCardBaseSize * scale;
        float gap = Math.Max(12f, cardSize.X * 0.1f);
        int columns = Math.Min(
            entries.Count,
            Math.Max(1, Mathf.FloorToInt(viewportSize.X * 0.78f / (cardSize.X + gap)))
        );
        int rows = Mathf.CeilToInt(entries.Count / (float)columns);
        float rowGap = Math.Max(12f, cardSize.Y * 0.08f);
        Vector2 start =
            viewportSize * 0.5f
            - new Vector2(
                columns * cardSize.X + Math.Max(0, columns - 1) * gap,
                rows * cardSize.Y + Math.Max(0, rows - 1) * rowGap
            ) * 0.5f;

        try
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Skill skill = Skill.GetSkill(entries[i].StatusSkillId);
                if (skill == null)
                    continue;

                SkillCard card = CreateOwnedCardExhaustPreviewCard(skill, entries[i].Player, scale);
                if (card == null)
                    continue;

                overlay.AddChild(card);
                card.RestoreDisplayState();
                card.Name = "PileSelectionExhaustCard";
                card.ZIndex = TemporaryCardZIndex + i;
                card.Modulate = new Color(1f, 1f, 1f, 0f);
                int row = i / columns;
                int col = i % columns;
                card.GlobalPosition =
                    start + new Vector2(col * (cardSize.X + gap), row * (cardSize.Y + rowGap));
                cards.Add(card);
            }

            if (cards.Count == 0)
                return;

            float stagger = Math.Min(0.04f, 0.18f / Math.Max(1, cards.Count - 1));
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                float delay = stagger * i;
                Tween appearTween = card.CreateTween();
                appearTween.SetParallel(true);
                appearTween.TweenProperty(card, "modulate:a", 1f, 0.1f).SetDelay(delay);
                appearTween
                    .TweenProperty(card, "scale", scale * 1.04f, 0.12f)
                    .From(scale * 0.92f)
                    .SetDelay(delay);

                Tween exhaustTween = card.CreateTween();
                exhaustTween
                    .TweenCallback(Callable.From(() => card.PlayExhaustEffect(duration)))
                    .SetDelay(delay + 0.12f);
            }

            await ToSignal(
                GetTree().CreateTimer(duration + 0.14f + stagger * Math.Max(0, cards.Count - 1)),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.QueueFree();
            }

            cards.Clear();
        }
    }

    public void MarkHandStatusExhaustPending(IReadOnlyCollection<int> handIndexes)
    {
        if (handIndexes == null || handIndexes.Count == 0)
            return;

        foreach (int index in handIndexes)
        {
            if (index == _liftedCardIndex)
                ClearLiftedCard(instant: true);

            _pendingHandStatusExhaustIndexes.Add(index);
        }

        RequestTurnUiRefresh();
    }

    public void ClearHandStatusExhaustPending(IReadOnlyCollection<int> handIndexes)
    {
        if (handIndexes == null || handIndexes.Count == 0)
            return;

        foreach (int index in handIndexes)
            _pendingHandStatusExhaustIndexes.Remove(index);
    }

    public void HideHandCardsForDyingRemoval(IReadOnlyCollection<int> indexes)
    {
        if (indexes == null || indexes.Count == 0)
            return;

        if (_liftedCardIndex >= 0)
        {
            foreach (int index in indexes)
            {
                if (index != _liftedCardIndex)
                    continue;

                ClearLiftedCard(instant: true);
                break;
            }
        }

        foreach (int index in indexes)
        {
            if (!IsCardIndexValid(index))
                continue;

            HideDiscardedHandCardAfterSelection(index, _cards[index]);
        }
    }

    public async Task PlayDyingOwnedCardExhaustAnimationAsync(
        IReadOnlyList<StatusCardExhaustAnimationEntry> entries,
        IReadOnlyCollection<int> handIndexesToHide = null,
        float duration = CardPlayVanishDuration
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        IReadOnlyCollection<int> handIndexes = handIndexesToHide ?? Array.Empty<int>();
        MarkHandStatusExhaustPending(handIndexes);
        int handEntryCount = handIndexes.Count;
        int pileEntryCount = Math.Max(0, entries.Count - handEntryCount);
        var overlayEntries = _dyingOwnedExhaustOverlayEntries;
        overlayEntries.Clear();

        for (int i = 0; i < pileEntryCount; i++)
            overlayEntries.Add(entries[i]);

        bool playedHandAnimation = false;
        int handOrder = 0;
        foreach (int handIndex in handIndexes)
        {
            int entryIndex = pileEntryCount + handOrder;
            if (TryPlayHandCardExhaustEffectAt(handIndex, duration))
            {
                playedHandAnimation = true;
                handOrder++;
                continue;
            }

            if (entryIndex < entries.Count)
                overlayEntries.Add(entries[entryIndex]);
            handOrder++;
        }

        Task handAnimationTask = Task.CompletedTask;
        Task overlayAnimationTask = Task.CompletedTask;
        if (playedHandAnimation)
            handAnimationTask = WaitForHandCardExhaustAnimationAsync(duration);
        if (overlayEntries.Count > 0)
            overlayAnimationTask = PlayDyingOwnedCardExhaustOverlayAnimationAsync(
                overlayEntries,
                duration
            );

        await handAnimationTask;
        await overlayAnimationTask;
        overlayEntries.Clear();
    }

    private async Task PlayDyingOwnedCardExhaustOverlayAnimationAsync(
        IReadOnlyList<StatusCardExhaustAnimationEntry> entries,
        float duration = CardPlayVanishDuration
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        int displayCount = Math.Min(entries.Count, DyingOwnedCardExhaustDisplayMax);
        Vector2 scale = BattleCardScale;
        Vector2 cardSize = BattleCardBaseSize * scale;
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 stackCenter = viewportSize * 0.5f;
        Vector2 stackBias = DyingOwnedCardStackOffset * (displayCount - 1) * 0.5f;
        var cards = _dyingOwnedExhaustPreviewCards;
        cards.Clear();

        try
        {
            for (int i = 0; i < displayCount; i++)
            {
                StatusCardExhaustAnimationEntry entry = entries[i];
                Skill skill = Skill.GetSkill(entry.StatusSkillId);
                if (skill == null)
                    continue;

                SkillCard card = CreateDyingOwnedCardExhaustPreviewCard(skill, entry.Player, scale);
                if (card == null)
                    continue;

                overlay.AddChild(card);
                card.RestoreDisplayState();
                card.Name = $"DyingOwnedExhaustCard{i}";
                card.ZIndex = TemporaryCardZIndex + i;
                card.GlobalPosition =
                    stackCenter
                    - cardSize * 0.5f
                    + DyingOwnedCardStackOffset * i
                    - stackBias;
                cards.Add(card);
            }

            if (cards.Count == 0)
                return;

            float stagger = Math.Min(0.04f, 0.2f / Math.Max(1, cards.Count - 1));
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                float delay = stagger * i;
                Tween exhaustTween = card.CreateTween();
                exhaustTween
                    .TweenCallback(Callable.From(() => card.PlayExhaustEffect(duration)))
                    .SetDelay(delay);
            }

            await ToSignal(
                GetTree().CreateTimer(duration + 0.08f + stagger * Math.Max(0, cards.Count - 1)),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.QueueFree();
            }

            cards.Clear();
        }
    }

    private SkillCard CreateDyingOwnedCardExhaustPreviewCard(
        Skill skill,
        PlayerCharacter player,
        Vector2 scale
    )
    {
        return CreateOwnedCardExhaustPreviewCard(skill, player, scale);
    }

    private SkillCard CreateOwnedCardExhaustPreviewCard(
        Skill skill,
        PlayerCharacter player,
        Vector2 scale
    )
    {
        if (skill == null)
            return null;

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "OwnedExhaustCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.ConfigureDisplayScale(scale);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        bool isStatusCard = skill.IsStatusCard;
        if (!isStatusCard)
            skill.OwnerCharater = player;
        card.SetSkill(skill);
        card.CharacterName.Text = isStatusCard
            ? string.Empty
            : GetCharacterEnergyDisplayName(player);
        card.HoverHint.Visible = false;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        return card;
    }

    private Vector2 GetOwnedCardExhaustPreviewScale(int count, Vector2 viewportSize)
    {
        float scaleFactor = BattleCardScaleFactor;
        if (count > 4)
            scaleFactor *= 0.92f;
        if (count > 7)
            scaleFactor *= 0.84f;

        Vector2 scale = Vector2.One * scaleFactor;
        Vector2 cardSize = BattleCardBaseSize * scale;
        if (cardSize.Y > viewportSize.Y * 0.42f)
            scale *= (viewportSize.Y * 0.42f) / cardSize.Y;
        return scale;
    }

    private Vector2 GetStatusExhaustPreviewScale(int count, Vector2 viewportSize)
    {
        float scaleFactor = 0.42f;
        if (count > 4)
            scaleFactor = 0.36f;
        if (count > 8)
            scaleFactor = 0.3f;

        Vector2 scale = Vector2.One * scaleFactor;
        Vector2 cardSize = BattleCardBaseSize * scale;
        if (cardSize.Y > viewportSize.Y * 0.34f)
            scale *= (viewportSize.Y * 0.34f) / cardSize.Y;
        return scale;
    }


}
