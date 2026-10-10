using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    public float PlayBattleDeckShuffleAnimation(int movedCardCount)
    {
        if (movedCardCount <= 0 || !IsInsideTree())
            return 0f;

        int previewCardCount = Math.Min(Math.Min(movedCardCount, ShufflePreviewMaxCardCount), 10);
        float totalDuration =
            ShufflePreviewCardFlyDuration
            + Math.Max(0, previewCardCount - 1) * ShufflePreviewCardStagger
            + ShufflePreviewDrawEntryDelayPadding;
        ulong delayUntil = Time.GetTicksMsec() + (ulong)Math.Ceiling(totalDuration * 1000f);
        if (delayUntil > _shuffleDrawEntryDelayUntilMsec)
            _shuffleDrawEntryDelayUntilMsec = delayUntil;

        PlayBattleDeckShuffleAnimationAsync(previewCardCount);
        return totalDuration;
    }

    private void PlayBattleDeckShuffleAnimationAsync(int previewCardCount)
    {
        if (
            previewCardCount <= 0
            || _drawPileButton == null
            || !GodotObject.IsInstanceValid(_drawPileButton)
            || !_drawPileButton.IsInsideTree()
            || _discardPileButton == null
            || !GodotObject.IsInstanceValid(_discardPileButton)
            || !_discardPileButton.IsInsideTree()
        )
        {
            return;
        }

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        Vector2 startCenter = GetPileButtonVisualCenter(_discardPileButton);
        Vector2 endCenter = GetPileButtonVisualCenter(_drawPileButton);
        if (startCenter.DistanceSquaredTo(endCenter) < 16f)
            return;

        for (int i = 0; i < previewCardCount; i++)
        {
            Vector2 startOffset = new(
                (float)GD.RandRange(-18f, 18f),
                (float)GD.RandRange(-14f, 14f)
            );
            Vector2 endOffset = new((float)GD.RandRange(-10f, 10f), (float)GD.RandRange(-8f, 8f));
            SkillCard card = CreateShufflePreviewCard(overlay, startCenter + startOffset, i);
            if (card == null)
                continue;

            _ = PlayShufflePreviewCardFlyAsync(
                card,
                startCenter + startOffset,
                endCenter + endOffset,
                i
            );
        }
    }

    private SkillCard CreateShufflePreviewCard(CanvasLayer overlay, Vector2 center, int index)
    {
        if (overlay == null || !GodotObject.IsInstanceValid(overlay))
            return null;

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "ShufflePreviewCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.ConfigureDisplayScale(Vector2.One);
        overlay.AddChild(card);
        card.ResetState();
        card.SetSkill(null);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Scale = Vector2.One * ShufflePreviewCardScale;
        card.Rotation = Mathf.DegToRad((float)GD.RandRange(-16f, 16f));
        card.Modulate = new Color(0.78f, 0.88f, 1f, 0.96f);
        card.ZIndex = PlayedCardZIndex + 40 + index;
        SetCardPivotCenterAt(card, center);
        return card;
    }

    private async Task PlayShufflePreviewCardFlyAsync(
        SkillCard card,
        Vector2 startCenter,
        Vector2 endCenter,
        int order
    )
    {
        try
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                return;

            float delay = Math.Max(0, order) * ShufflePreviewCardStagger;
            Vector2 control = GetShufflePreviewControlPoint(startCenter, endCenter, order);
            Vector2 initialVelocity = GetQuadraticBezierVelocity(
                startCenter,
                control,
                endCenter,
                0.01f
            );
            card.Rotation = GetRotationWithTopFacingVelocity(initialVelocity);
            PrepareCardDiscardTrail(card, out Line trail, out GpuParticles2D particles);
            UpdateTrailParticlesRotation(particles, initialVelocity);

            Tween tween = card.CreateTween();
            if (delay > 0f)
                tween.TweenInterval(delay);

            tween.SetParallel(true);
            tween
                .TweenMethod(
                    Callable.From<float>(t =>
                    {
                        if (card == null || !GodotObject.IsInstanceValid(card))
                            return;

                        Vector2 center = QuadraticBezier(startCenter, control, endCenter, t);
                        Vector2 velocity = GetQuadraticBezierVelocity(
                            startCenter,
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
                    ShufflePreviewCardFlyDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.InOut);
            tween
                .TweenProperty(
                    card,
                    "scale",
                    Vector2.One * ShufflePreviewCardScale * 0.72f,
                    ShufflePreviewCardFlyDuration
                )
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            tween
                .TweenProperty(
                    card,
                    "modulate",
                    new Color(0.94f, 0.98f, 1f, 0.72f),
                    ShufflePreviewCardFlyDuration
                )
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.InOut);
            tween.SetParallel(false);

            await ToSignal(tween, Tween.SignalName.Finished);
            await FadeAndHideCardDiscardTrailAsync(trail, particles);
        }
        finally
        {
            QueueFreeTemporaryCard(card);
        }
    }

    private static Vector2 GetShufflePreviewControlPoint(Vector2 start, Vector2 end, int order)
    {
        Vector2 mid = (start + end) * 0.5f;
        float distance = start.DistanceTo(end);
        float lift = Math.Min(360f, Math.Max(160f, distance * 0.34f));
        float wave = Mathf.Sin(order * 1.35f) * Math.Min(110f, distance * 0.1f);
        return mid + new Vector2(wave + (float)GD.RandRange(-36f, 36f), -lift);
    }

    public void PrepareHandCardDrawEntry(
        IReadOnlyList<int> handIndexes,
        HandCardEntryOrigin origin
    )
    {
        _preparedHandDrawEntrySlotCount = 0;
        if (handIndexes == null || handIndexes.Count == 0 || !CanAnimateHandCardsFor(_activePlayer))
            return;

        // Move existing card identities before attaching entry origins to the new slots.
        SyncHandSlotIdentities(GetActiveHandSkills());
        switch (origin)
        {
            case HandCardEntryOrigin.PlayedCard:
            {
                Vector2? startCenter = GetPlayedCardDrawEntryStartGlobalCenter();
                if (!startCenter.HasValue)
                    return;

                foreach (int index in handIndexes)
                {
                    if (!IsCardIndexValid(index))
                        continue;

                    _drawEntryFromPlayedCardOrigin.Add(index);
                    _drawEntryStartCenters[index] = startCenter.Value;
                    _customDrawEntryStartPositions.Remove(index);
                    AddPreparedHandDrawEntrySlot(index);
                }
                break;
            }
            case HandCardEntryOrigin.DrawPile:
            {
                foreach (int index in handIndexes)
                {
                    if (!IsCardIndexValid(index))
                        continue;

                    MarkHandDrawEntryFromDrawPile(index);
                    AddPreparedHandDrawEntrySlot(index);
                }
                break;
            }
        }
    }

    private void AddPreparedHandDrawEntrySlot(int index)
    {
        if (_preparedHandDrawEntrySlotCount >= _preparedHandDrawEntrySlots.Length)
            Array.Resize(ref _preparedHandDrawEntrySlots, _preparedHandDrawEntrySlots.Length * 2);

        _preparedHandDrawEntrySlots[_preparedHandDrawEntrySlotCount++] = index;
    }

    public async Task WaitForPreparedHandDrawEntryAsync()
    {
        if (_preparedHandDrawEntrySlotCount == 0)
            return;

        await WaitForHandDrawEntrySlotsAsync(
            _preparedHandDrawEntrySlots,
            _preparedHandDrawEntrySlotCount
        );
        _preparedHandDrawEntrySlotCount = 0;
    }

    public void MarkHandDrawEntryFromDrawPile(int handIndex)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        _drawEntryFromPlayedCardOrigin.Remove(handIndex);
        _drawEntryStartCenters.Remove(handIndex);
        SetHandEntryStartPositionFromPileKind(handIndex, BattlePileKind.Draw);
    }

    public bool CanAddCardsToHandDrawEntryFromPlayedCard(PlayerCharacter player, int count)
    {
        if (!CanAnimateAddCardsToHand(player, count))
            return false;

        return GetPlayedCardDrawEntryStartGlobalCenter().HasValue;
    }

    public bool CanAnimateAddCardsToHand(PlayerCharacter player, int count)
    {
        if (count <= 0 || !CanAnimateHandCardsFor(player))
            return false;

        return CountEmptyHandSlots(count) > 0;
    }

    private int CountEmptyHandSlots(int maxCount)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || maxCount <= 0)
            return 0;

        int count = 0;
        for (int i = 0; i < hand.Length && count < maxCount; i++)
        {
            if (hand[i] == null)
                count++;
        }

        return count;
    }

    public async Task WaitForHandDrawEntrySlotsAsync(IReadOnlyList<int> slotIndexes)
    {
        await WaitForHandDrawEntrySlotsAsync(slotIndexes, slotIndexes?.Count ?? 0);
    }

    private async Task WaitForHandDrawEntrySlotsAsync(IReadOnlyList<int> slotIndexes, int count)
    {
        if (slotIndexes == null || count <= 0 || !IsInsideTree())
            return;

        while (IsInsideTree())
        {
            bool anyBusy = false;
            for (int i = 0; i < count; i++)
            {
                int index = slotIndexes[i];
                if (IsCardDrawEntryBusy(index))
                {
                    anyBusy = true;
                    break;
                }
            }

            if (!anyBusy)
                break;

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    public Task PlayStatusCardInsertAnimationAsync(
        Character target,
        SkillID statusSkillId,
        int count,
        Character source = null
    )
    {
        return PlayStatusCardInsertAnimationAsync(
            new[] { new StatusCardInsertAnimationEntry(target, statusSkillId, count, source) }
        );
    }

    public Task PlayStatusCardInsertAnimationAsync(
        Character target,
        SkillID statusSkillId,
        int count,
        BattleCardPileTarget pileTarget,
        Character source = null
    )
    {
        return PlayStatusCardInsertAnimationAsync(
            new[]
            {
                new StatusCardInsertAnimationEntry(
                    target,
                    statusSkillId,
                    count,
                    source,
                    pileTarget
                ),
            }
        );
    }

    public async Task PlayStatusCardInsertAnimationAsync(
        IReadOnlyList<StatusCardInsertAnimationEntry> entries
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        while (_statusInsertAnimationBuffersInUse && IsInsideTree())
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (_statusInsertAnimationBuffersInUse || !IsInsideTree())
            return;

        _statusInsertAnimationBuffersInUse = true;
        var expandedEntries = _statusInsertExpandedEntries;
        var statusSkillCache = _statusInsertSkillCache;
        expandedEntries.Clear();
        statusSkillCache.Clear();
        try
        {
            foreach (StatusCardInsertAnimationEntry entry in entries)
            {
                if (entry.Count <= 0)
                    continue;

                if (!statusSkillCache.TryGetValue(entry.StatusSkillId, out Skill statusSkill))
                {
                    statusSkill = Skill.GetSkill(entry.StatusSkillId);
                    statusSkillCache[entry.StatusSkillId] = statusSkill;
                }

                if (statusSkill == null)
                    continue;

                for (int i = 0; i < entry.Count; i++)
                    expandedEntries.Add((entry.Target, entry.Source, statusSkill, entry.PileTarget));
            }

            if (expandedEntries.Count == 0)
                return;

            CanvasLayer overlay = EnsureCardPlayOverlay();
            if (overlay == null)
                return;

            var cards = _statusInsertPreviewCards;
            var flyTasks = _statusInsertFlyTasks;
            cards.Clear();
            flyTasks.Clear();
            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            Vector2 scale = GetStatusInsertScale(expandedEntries.Count, viewportSize);
            Vector2 cardSize = BattleCardBaseSize * scale;
            float gap = GetStatusInsertGap(cardSize);
            int columns = GetStatusInsertColumns(expandedEntries.Count, cardSize, gap, viewportSize);
            int rows = Mathf.CeilToInt(expandedEntries.Count / (float)columns);
            float rowGap = GetStatusInsertRowGap(cardSize);
            float totalHeight = rows * cardSize.Y + Math.Max(0, rows - 1) * rowGap;
            float startY = Mathf.Clamp(
                viewportSize.Y * 0.47f - totalHeight * 0.5f,
                42f,
                Math.Max(42f, viewportSize.Y - totalHeight - 42f)
            );
            Vector2 spawnPosition = new(viewportSize.X * 0.5f - cardSize.X * 0.5f, startY - 42f);
            float stagger = GetStatusInsertStagger(expandedEntries.Count);
            for (int i = 0; i < expandedEntries.Count; i++)
            {
                var entry = expandedEntries[i];
                SkillCard card = CreateStatusInsertPreviewCard(
                    entry.StatusSkill,
                    entry.Target,
                    entry.Source,
                    1,
                    scale
                );
                if (card == null)
                    continue;

                overlay.AddChild(card);
                card.RestoreDisplayState();
                card.GlobalPosition = spawnPosition;
                card.Scale = scale * 0.72f;
                card.Modulate = new Color(1f, 1f, 1f, 0f);
                card.ZIndex = TemporaryCardZIndex + i;
                cards.Add(
                    new StatusInsertPreviewCard
                    {
                        Card = card,
                        PileTarget = entry.PileTarget,
                        Scale = scale,
                        CardSize = cardSize,
                    }
                );

                Vector2 arrangedPosition = GetStatusInsertArrangedPosition(
                    i,
                    expandedEntries.Count,
                    columns,
                    cardSize,
                    gap,
                    rowGap,
                    viewportSize,
                    startY
                );
                Tween arrangeTween = card.CreateTween();
                arrangeTween.SetParallel(true);
                arrangeTween
                    .TweenProperty(card, "modulate:a", 1f, StatusInsertArrangeDuration)
                    .SetDelay(i * stagger);
                arrangeTween
                    .TweenProperty(card, "scale", scale, StatusInsertArrangeDuration)
                    .SetDelay(i * stagger)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);
                arrangeTween
                    .TweenProperty(
                        card,
                        "global_position",
                        arrangedPosition,
                        StatusInsertArrangeDuration
                    )
                    .SetDelay(i * stagger)
                    .SetTrans(Tween.TransitionType.Cubic)
                    .SetEase(Tween.EaseType.Out);

                if (
                    (i + 1) % StatusInsertCardsCreatedPerFrame == 0
                    && i + 1 < expandedEntries.Count
                )
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            if (cards.Count == 0)
                return;

            float arrangeWaitDuration =
                StatusInsertArrangeDuration + stagger * Math.Max(0, expandedEntries.Count - 1);
            await ToSignal(
                GetTree().CreateTimer(arrangeWaitDuration + StatusInsertHoldDuration),
                SceneTreeTimer.SignalName.Timeout
            );

            for (int i = 0; i < cards.Count; i++)
            {
                StatusInsertPreviewCard preview = cards[i];
                SkillCard card = preview.Card;
                if (card == null || !GodotObject.IsInstanceValid(card))
                    continue;

                float delay = i * stagger;
                Button pileButton = GetStatusInsertTargetPileButton(preview.PileTarget);
                flyTasks.Add(PlayStatusInsertCardIntoPileAsync(card, pileButton, delay));
            }

            for (int i = 0; i < flyTasks.Count; i++)
                await flyTasks[i];
        }
        finally
        {
            foreach (StatusInsertPreviewCard preview in _statusInsertPreviewCards)
                QueueFreeTemporaryCard(preview.Card);

            _statusInsertFlyTasks.Clear();
            _statusInsertPreviewCards.Clear();
            expandedEntries.Clear();
            statusSkillCache.Clear();
            _statusInsertAnimationBuffersInUse = false;
        }
    }

    private Vector2 GetStatusInsertScale(int count, Vector2 viewportSize)
    {
        float scaleFactor = StatusInsertCardScale;
        while (scaleFactor > 0.62f)
        {
            Vector2 scale = BattleCardScale * scaleFactor;
            Vector2 cardSize = BattleCardBaseSize * scale;
            float gap = GetStatusInsertGap(cardSize);
            int columns = GetStatusInsertColumns(count, cardSize, gap, viewportSize);
            int rows = Mathf.CeilToInt(count / (float)columns);
            float rowGap = GetStatusInsertRowGap(cardSize);
            float totalHeight = rows * cardSize.Y + Math.Max(0, rows - 1) * rowGap;
            if (totalHeight <= viewportSize.Y * 0.68f)
                return scale;

            scaleFactor -= 0.04f;
        }

        return BattleCardScale * Math.Max(0.62f, scaleFactor);
    }

    private static float GetStatusInsertGap(Vector2 cardSize)
    {
        return Math.Min(28f, Math.Max(10f, cardSize.X * 0.12f));
    }

    private static float GetStatusInsertRowGap(Vector2 cardSize)
    {
        return Math.Min(24f, Math.Max(12f, cardSize.Y * 0.08f));
    }

    private static float GetStatusInsertStagger(int count)
    {
        if (count <= 1)
            return 0f;

        return Math.Min(StatusInsertStagger, 0.18f / (count - 1));
    }

    private static int GetStatusInsertColumns(
        int count,
        Vector2 cardSize,
        float gap,
        Vector2 viewportSize
    )
    {
        float availableWidth = Math.Max(cardSize.X, viewportSize.X - 96f);
        int maxColumns = Math.Max(1, Mathf.FloorToInt((availableWidth + gap) / (cardSize.X + gap)));
        return Math.Max(1, Math.Min(count, maxColumns));
    }

    private static Vector2 GetStatusInsertArrangedPosition(
        int index,
        int count,
        int columns,
        Vector2 cardSize,
        float gap,
        float rowGap,
        Vector2 viewportSize,
        float startY
    )
    {
        int row = index / columns;
        int column = index % columns;
        int rowCount = Math.Min(columns, count - row * columns);
        float rowWidth = rowCount * cardSize.X + Math.Max(0, rowCount - 1) * gap;
        float startX = viewportSize.X * 0.5f - rowWidth * 0.5f;
        return new Vector2(
            startX + column * (cardSize.X + gap),
            startY + row * (cardSize.Y + rowGap)
        );
    }

    private SkillCard CreateStatusInsertPreviewCard(
        Skill statusSkill,
        Character target,
        Character source,
        int count,
        Vector2 scale
    )
    {
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "StatusInsertCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.ConfigureDisplayScale(scale);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.SetSkill(statusSkill);
        card.SetEnergyCostText(count > 1 ? $"状态 x{count}" : "状态");
        card.CharacterName.Text = string.Empty;
        card.HoverHint.Visible = false;
        return card;
    }

    private Button GetStatusInsertTargetPileButton(BattleCardPileTarget pileTarget)
    {
        return pileTarget switch
        {
            BattleCardPileTarget.DiscardPileCards => _discardPileButton,
            BattleCardPileTarget.DrawPileCards => _drawPileButton,
            _ => null,
        };
    }

    private async Task PlayStatusInsertCardIntoPileAsync(
        SkillCard card,
        Button pileButton,
        float delay
    )
    {
        if (delay > 0f)
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

        await PlayCardFlyToPileAsync(card, pileButton);
    }

    private static IEnumerable<SkillCard> EnumerateSkillCards(Node node)
    {
        if (node == null)
            yield break;

        if (node is SkillCard card)
            yield return card;

        foreach (Node child in node.GetChildren())
        {
            foreach (SkillCard childCard in EnumerateSkillCards(child))
                yield return childCard;
        }
    }


}
