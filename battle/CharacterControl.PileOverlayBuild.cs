using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private void ShowPileOverlay(
        PlayerCharacter player,
        IReadOnlyList<BattlePileOverlaySection> sections,
        PileOverlayOpenContext openContext = default
    )
    {
        ulong syncStartUsec = Time.GetTicksUsec();
        EnsurePileOverlayUi();
        int buildVersion = ++_pileOverlayBuildVersion;
        _pileOverlayContentTemporarilyHidden = false;
        _pileOverlayLayoutTraceLastGlobalX.Clear();
        ClearPileOverlayCards();

        if (_pileOverlayRoot == null || _pileOverlaySections == null)
            return;

        _pileOverlayFadeTween?.Kill();
        bool wasVisible = IsPileOverlayVisible();
        if (!wasVisible)
            ResetPileOverlayPresentation();
        else
            SetPileOverlayPresentationFullyVisible();
        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.Modulate = Colors.White;
        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Ignore;
        _pileOverlayRoot.MoveToFront();
        _pileOverlaySections.Visible = true;
        EnsurePileOverlayDrawOrder();
        SyncPileOverlaySelectionButtons();
        ResetPileOverlayScroll();
        LogPileOverlayLayoutTrace($"show-after-reset wasVisible={wasVisible} total={sections?.Sum(section => section.Pile?.Length ?? 0) ?? 0}");

        if (!wasVisible)
            PlayPileOverlayIntroAnimation();

        double syncSetupMs = (Time.GetTicksUsec() - syncStartUsec) / 1000.0;
        openContext = new PileOverlayOpenContext(
            openContext.Source,
            openContext.TriggerSkillId,
            openContext.TriggerSkillName,
            openContext.PrepMs,
            openContext.FlowStartUsec,
            syncSetupMs
        );
        _ = PopulatePileOverlayAsync(player, sections, buildVersion, openContext);
    }

    private async Task PopulatePileOverlayAsync(
        PlayerCharacter player,
        IReadOnlyList<BattlePileOverlaySection> sections,
        int buildVersion,
        PileOverlayOpenContext openContext
    )
    {
        ulong buildStartUsec = Time.GetTicksUsec();
        var state = new PileOverlayBuildState
        {
            Version = buildVersion,
            TotalCards = sections?.Sum(section => section.Pile?.Length ?? 0) ?? 0,
            ShatterSlashInPile = CountShatterSlashCardsInSections(sections),
            OpenContext = openContext,
        };
        _pileOverlayActiveApplyPerfTracker = state.ApplyPerf;
        try
        {
            SceneTree tree = GetTree();
            if (tree != null)
            {
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                if (!IsPileOverlayBuildCurrent(state))
                    return;
            }
            state.LastYieldUsec = Time.GetTicksUsec();

            foreach (
                BattlePileOverlaySection section in sections ?? Array.Empty<BattlePileOverlaySection>()
            )
            {
                if (!IsPileOverlayBuildCurrent(state))
                    return;

                await AddPileOverlaySection(_pileOverlaySections, player, section, state);
            }

            if (!IsPileOverlayBuildCurrent(state))
                return;

            _pileOverlaySections.QueueSort();
            LogPileOverlayLayoutTrace("populate-after-sections-before-frame");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            LogPileOverlayLayoutTrace("populate-after-sections-frame1");
            if (IsPileOverlayBuildCurrent(state))
            {
                ResetPileOverlayScroll();
                LogPileOverlayLayoutTrace("populate-after-reset-scroll");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                LogPileOverlayLayoutTrace("populate-after-reset-scroll-frame1");
            }

            if (IsPileOverlayBuildCurrent(state))
            {
                ulong refreshStartUsec = Time.GetTicksUsec();
                LogPileOverlayLayoutTrace("populate-before-virtual-refresh");
                await RefreshPileOverlayVirtualGridsAsync(state, playEntryAnimation: true);
                LogPileOverlayLayoutTrace("populate-after-virtual-refresh");
                BattleNode?.LogPilePerfWork("pile-virtual-refresh-open", refreshStartUsec);
            }

            if (IsPileOverlayBuildCurrent(state))
                LogPileOverlayBuildPerf(player, sections, state, buildStartUsec);
        }
        finally
        {
            _pileOverlayActiveApplyPerfTracker = null;
        }
    }

    private async Task AddPileOverlaySection(
        VBoxContainer stack,
        PlayerCharacter player,
        BattlePileOverlaySection section,
        PileOverlayBuildState state
    )
    {
        if (stack == null)
            return;

        var sectionRoot =
            GetPileOverlaySectionRoot(section.Kind) ?? CreatePileOverlaySectionRoot(section.Kind);
        if (sectionRoot.GetParent() == null)
            stack.AddChild(sectionRoot);
        sectionRoot.Visible = true;
        ConfigurePileOverlayFixedWidthContainer(sectionRoot, SizeFlags.ShrinkCenter);

        var title = sectionRoot.GetNodeOrNull<Label>("Title");
        if (title == null)
        {
            title = new Label
            {
                Name = "Title",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sectionRoot.AddChild(title);
        }
        ConfigurePileOverlayLabel(title);
        title.Text = $"{section.Title}  {section.Pile.Length}";

        var emptyLabel = sectionRoot.GetNodeOrNull<Label>("Empty");
        if (emptyLabel == null)
        {
            emptyLabel = new Label
            {
                Name = "Empty",
                Text = "空",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sectionRoot.AddChild(emptyLabel);
        }
        ConfigurePileOverlayLabel(emptyLabel);

        var grid = sectionRoot.GetNodeOrNull<GridContainer>("Grid");
        if (grid == null)
        {
            grid = new GridContainer
            {
                Name = "Grid",
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            sectionRoot.AddChild(grid);
        }
        ConfigurePileOverlayGrid(grid);

        var groups = sectionRoot.GetNodeOrNull<VBoxContainer>("Groups");
        if (groups == null)
        {
            groups = new VBoxContainer
            {
                Name = "Groups",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            groups.AddThemeConstantOverride("separation", 22);
            sectionRoot.AddChild(groups);
        }
        ConfigurePileOverlayFixedWidthContainer(groups, SizeFlags.ShrinkBegin);

        ClearGridChildren(grid);
        ClearGridChildren(groups);

        if (section.Pile.Length == 0)
        {
            emptyLabel.Visible = true;
            grid.Visible = false;
            groups.Visible = false;
            return;
        }

        emptyLabel.Visible = false;
        UserSettings.EnsureLoaded();
        bool groupByCharacter = UserSettings.GroupBattlePilesByCharacter;
        grid.Visible = !groupByCharacter;
        groups.Visible = groupByCharacter;
        IndexedBattlePileEntry[] indexedPile = section.Pile
            .Select((entry, index) => new IndexedBattlePileEntry(index, entry))
            .ToArray();

        if (groupByCharacter)
        {
            await AddPileOverlayCharacterGroups(
                groups,
                player,
                section.Kind,
                indexedPile,
                state
            );
        }
        else
        {
            _pileOverlayGrid = grid;
            if (section.Pile.Length >= PileOverlayVirtualizationThreshold)
            {
                grid.Visible = false;
                state.UsesVirtualization = true;
                AddPileOverlayVirtualGrid(sectionRoot, player, section, indexedPile);
                return;
            }

            foreach (IndexedBattlePileEntry indexedEntry in indexedPile)
            {
                if (!IsPileOverlayBuildCurrent(state))
                    return;

                PlayerCharacter entryOwner = IsStatusSkillId(indexedEntry.Entry.SkillId)
                    ? null
                    : indexedEntry.Entry.Owner ?? player;
                var holder = CreatePilePreviewCardHolder(
                    entryOwner,
                    indexedEntry.Entry.SkillId,
                    out SkillCard card
                );
                if (holder == null || card == null)
                    continue;

                grid.AddChild(holder);
                ApplyPilePreviewCardForOverlay(
                    card,
                    entryOwner,
                    indexedEntry.Entry.SkillId
                );
                card.ResetState();
                card.HoverHint.Visible = false;
                ConfigurePileSelectionCard(card, section.Kind, indexedEntry.Index);
                PlayPileOverlayCardEntryAnimation(card, holder, section.Kind, state);
                holder.Visible = true;
                await YieldPileOverlayBuildIfNeeded(state);
            }

            grid.QueueSort();
        }
    }

    private void AddPileOverlayVirtualGrid(
        VBoxContainer sectionRoot,
        PlayerCharacter player,
        BattlePileOverlaySection section,
        IndexedBattlePileEntry[] indexedPile
    )
    {
        AddPileOverlayVirtualGrid(sectionRoot, player, section.Kind, indexedPile);
    }

    private void AddPileOverlayVirtualGrid(
        VBoxContainer sectionRoot,
        PlayerCharacter player,
        BattlePileKind kind,
        IndexedBattlePileEntry[] indexedPile
    )
    {
        if (sectionRoot == null || indexedPile == null)
            return;

        var virtualGrid = sectionRoot.GetNodeOrNull<Control>("VirtualGrid");
        if (virtualGrid == null)
        {
            virtualGrid = new Control
            {
                Name = "VirtualGrid",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            sectionRoot.AddChild(virtualGrid);
        }
        virtualGrid.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        virtualGrid.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        ClearGridChildren(virtualGrid);

        int rows = Mathf.CeilToInt(indexedPile.Length / (float)PileOverlayGridColumns);
        float rowHeight = PileCardHolderSize.Y + PileOverlayGridVSeparation;
        float contentHeight = Math.Max(
            PileCardHolderSize.Y,
            rows * PileCardHolderSize.Y + Math.Max(0, rows - 1) * PileOverlayGridVSeparation
        );
        virtualGrid.CustomMinimumSize = new Vector2(PileOverlayContentWidth, contentHeight);
        virtualGrid.Size = virtualGrid.CustomMinimumSize;
        virtualGrid.Visible = true;

        _pileOverlayVirtualGrids.Add(
            new PileOverlayVirtualGrid
            {
                Kind = kind,
                FallbackPlayer = player,
                Grid = virtualGrid,
                Entries = indexedPile,
            }
        );
    }

    private async Task AddPileOverlayCharacterGroups(
        VBoxContainer groups,
        PlayerCharacter fallbackPlayer,
        BattlePileKind kind,
        IReadOnlyList<IndexedBattlePileEntry> pile,
        PileOverlayBuildState state
    )
    {
        if (groups == null || pile == null)
            return;

        bool forceVirtualizedGroups = pile.Count >= PileOverlayVirtualizationThreshold;
        foreach (
            var ownerGroup in pile.GroupBy(indexedEntry =>
                IsStatusSkillId(indexedEntry.Entry.SkillId)
                    ? null
                    : indexedEntry.Entry.Owner ?? fallbackPlayer
            )
        )
        {
            if (!IsPileOverlayBuildCurrent(state))
                return;

            PlayerCharacter owner = ownerGroup.Key;
            IndexedBattlePileEntry[] entries = ownerGroup.ToArray();
            if (entries.Length == 0)
                continue;

            var groupRoot = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            ConfigurePileOverlayFixedWidthContainer(groupRoot, SizeFlags.ShrinkBegin);
            groupRoot.AddThemeConstantOverride("separation", 8);
            groups.AddChild(groupRoot);

            var label = new Label
            {
                Text = $"{GetPileGroupDisplayName(owner)}  {entries.Length}",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            ConfigurePileOverlayLabel(label);
            groupRoot.AddChild(label);

            var grid = new GridContainer
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            ConfigurePileOverlayGrid(grid);
            groupRoot.AddChild(grid);
            _pileOverlayGrid = grid;

            if (forceVirtualizedGroups || entries.Length >= PileOverlayVirtualizationThreshold)
            {
                grid.Visible = false;
                state.UsesVirtualization = true;
                AddPileOverlayVirtualGrid(groupRoot, owner ?? fallbackPlayer, kind, entries);
                continue;
            }

            foreach (IndexedBattlePileEntry indexedEntry in entries)
            {
                if (!IsPileOverlayBuildCurrent(state))
                    return;

                PlayerCharacter entryOwner = IsStatusSkillId(indexedEntry.Entry.SkillId)
                    ? null
                    : owner ?? fallbackPlayer;
                var holder = CreatePilePreviewCardHolder(
                    entryOwner,
                    indexedEntry.Entry.SkillId,
                    out SkillCard card
                );
                if (holder == null || card == null)
                    continue;

                grid.AddChild(holder);
                ApplyPilePreviewCardForOverlay(card, entryOwner, indexedEntry.Entry.SkillId);
                card.ResetState();
                card.HoverHint.Visible = false;
                ConfigurePileSelectionCard(card, kind, indexedEntry.Index);
                PlayPileOverlayCardEntryAnimation(card, holder, kind, state);
                holder.Visible = true;
                await YieldPileOverlayBuildIfNeeded(state);
            }

            grid.QueueSort();
        }
    }

    private bool IsPileOverlayBuildCurrent(PileOverlayBuildState state)
    {
        return state != null
            && state.Version == _pileOverlayBuildVersion
            && _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlaySections != null
            && GodotObject.IsInstanceValid(_pileOverlaySections);
    }

    private async Task YieldPileOverlayBuildIfNeeded(PileOverlayBuildState state)
    {
        if (state == null)
            return;

        state.CardsCreated++;
        ulong nowUsec = Time.GetTicksUsec();
        bool countBudgetReached =
            state.CardsCreated % PileOverlayCardsCreatedPerFrame == 0;
        bool timeBudgetReached =
            state.LastYieldUsec == 0UL
            || (nowUsec - state.LastYieldUsec) / 1000.0 >= PileOverlayBuildFrameBudgetMs;
        if (!countBudgetReached && !timeBudgetReached)
            return;

        SceneTree tree = GetTree();
        if (tree != null)
        {
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            state.LastYieldUsec = Time.GetTicksUsec();
        }
    }

    private void OnPileOverlayScrollChanged(double _)
    {
        if (_pileOverlayScrollRefreshPending)
            return;

        _pileOverlayScrollRefreshPending = true;
        CallDeferred(MethodName.DeferredRefreshPileOverlayVirtualGrids);
    }

    private void DeferredRefreshPileOverlayVirtualGrids()
    {
        _pileOverlayScrollRefreshPending = false;
        if (
            _pileOverlayRoot == null
            || !GodotObject.IsInstanceValid(_pileOverlayRoot)
            || !_pileOverlayRoot.Visible
            || _pileOverlayVirtualGrids.Count == 0
        )
        {
            return;
        }

        var tracker = new PileOverlayApplyPerfTracker();
        _pileOverlayActiveApplyPerfTracker = tracker;
        ulong startUsec = Time.GetTicksUsec();
        LogPileOverlayLayoutTrace($"scroll-refresh-before scroll={_pileOverlayScroll?.ScrollVertical ?? -1}");
        try
        {
            RefreshPileOverlayVirtualGrids(playEntryAnimation: false);
        }
        finally
        {
            _pileOverlayActiveApplyPerfTracker = null;
        }
        LogPileOverlayLayoutTrace($"scroll-refresh-after scroll={_pileOverlayScroll?.ScrollVertical ?? -1}");

        double elapsedMs = (Time.GetTicksUsec() - startUsec) / 1000.0;
        Battle battle = BattleNode;
        if (battle == null || !battle.PilePerfLogEnabled)
            return;

        battle.MarkPilePerfEvent("pile-scroll-refresh");
        string summary =
            $"scroll refresh {elapsedMs:F2}ms, applied ShatterSlash={tracker.ShatterSlashCount} "
            + $"({tracker.ShatterSlashApplyMs:F2}ms), other={tracker.OtherCount} "
            + $"({tracker.OtherApplyMs:F2}ms)";
        if (
            elapsedMs >= battle.PilePerfWorkSpikeMs
            || tracker.ShatterSlashApplyMs >= battle.PilePerfWorkSpikeMs
        )
        {
            battle.LogPilePerf(summary);
        }
    }

    private void RefreshPileOverlayVirtualGrids(bool playEntryAnimation)
    {
        if (
            _pileOverlayVirtualGrids.Count == 0
            || _pileOverlayScroll == null
            || !GodotObject.IsInstanceValid(_pileOverlayScroll)
        )
        {
            return;
        }

        Rect2 visibleRect = _pileOverlayScroll.GetGlobalRect();
        foreach (PileOverlayVirtualGrid virtualGrid in _pileOverlayVirtualGrids.ToArray())
        {
            if (
                virtualGrid?.Grid == null
                || !GodotObject.IsInstanceValid(virtualGrid.Grid)
                || virtualGrid.Entries == null
            )
            {
                continue;
            }

            RefreshPileOverlayVirtualGrid(virtualGrid, visibleRect, playEntryAnimation, state: null);
            virtualGrid.EntryAnimationPlayed = true;
        }
    }

    private async Task RefreshPileOverlayVirtualGridsAsync(
        PileOverlayBuildState state,
        bool playEntryAnimation
    )
    {
        if (
            _pileOverlayVirtualGrids.Count == 0
            || _pileOverlayScroll == null
            || !GodotObject.IsInstanceValid(_pileOverlayScroll)
        )
        {
            return;
        }

        Rect2 visibleRect = _pileOverlayScroll.GetGlobalRect();
        foreach (PileOverlayVirtualGrid virtualGrid in _pileOverlayVirtualGrids.ToArray())
        {
            if (
                virtualGrid?.Grid == null
                || !GodotObject.IsInstanceValid(virtualGrid.Grid)
                || virtualGrid.Entries == null
            )
            {
                continue;
            }

            if (!IsPileOverlayBuildCurrent(state))
                return;

            await RefreshPileOverlayVirtualGridAsync(
                virtualGrid,
                visibleRect,
                playEntryAnimation,
                state
            );
            virtualGrid.EntryAnimationPlayed = true;
        }
    }

    private void RefreshPileOverlayVirtualGrid(
        PileOverlayVirtualGrid virtualGrid,
        Rect2 visibleRect,
        bool playEntryAnimation,
        PileOverlayBuildState state
    )
    {
        int totalCards = virtualGrid.Entries.Length;
        if (totalCards == 0)
            return;

        float rowHeight = PileCardHolderSize.Y + PileOverlayGridVSeparation;
        float gridTop = virtualGrid.Grid.GetGlobalRect().Position.Y;
        float visibleTop = Mathf.Max(0f, visibleRect.Position.Y - gridTop);
        float visibleBottom = Mathf.Min(
            virtualGrid.Grid.CustomMinimumSize.Y,
            visibleRect.End.Y - gridTop
        );
        CalculatePileOverlayVirtualWindow(
            totalCards,
            rowHeight,
            visibleTop,
            visibleBottom,
            out int firstIndex,
            out int visibleSlotCount
        );
        EnsurePileOverlayVirtualHolderCount(virtualGrid, visibleSlotCount);
        if (
            !playEntryAnimation
            && virtualGrid.FirstIndex == firstIndex
            && virtualGrid.VisibleSlotCount == visibleSlotCount
        )
        {
            return;
        }

        virtualGrid.FirstIndex = firstIndex;
        virtualGrid.VisibleSlotCount = visibleSlotCount;

        for (int localIndex = 0; localIndex < virtualGrid.Holders.Count; localIndex++)
        {
            if (!TryConfigurePileOverlayVirtualHolder(
                    virtualGrid,
                    firstIndex,
                    visibleSlotCount,
                    totalCards,
                    rowHeight,
                    localIndex,
                    playEntryAnimation,
                    out _
                ))
            {
                continue;
            }
        }
    }

    private async Task RefreshPileOverlayVirtualGridAsync(
        PileOverlayVirtualGrid virtualGrid,
        Rect2 visibleRect,
        bool playEntryAnimation,
        PileOverlayBuildState state
    )
    {
        int totalCards = virtualGrid.Entries.Length;
        if (totalCards == 0)
            return;

        float rowHeight = PileCardHolderSize.Y + PileOverlayGridVSeparation;
        float gridTop = virtualGrid.Grid.GetGlobalRect().Position.Y;
        float visibleTop = Mathf.Max(0f, visibleRect.Position.Y - gridTop);
        float visibleBottom = Mathf.Min(
            virtualGrid.Grid.CustomMinimumSize.Y,
            visibleRect.End.Y - gridTop
        );
        CalculatePileOverlayVirtualWindow(
            totalCards,
            rowHeight,
            visibleTop,
            visibleBottom,
            out int firstIndex,
            out int visibleSlotCount
        );
        await EnsurePileOverlayVirtualHolderCountAsync(virtualGrid, visibleSlotCount, state);
        virtualGrid.FirstIndex = firstIndex;
        virtualGrid.VisibleSlotCount = visibleSlotCount;

        for (int localIndex = 0; localIndex < virtualGrid.Holders.Count; localIndex++)
        {
            if (!IsPileOverlayBuildCurrent(state))
                return;

            if (!TryConfigurePileOverlayVirtualHolder(
                    virtualGrid,
                    firstIndex,
                    visibleSlotCount,
                    totalCards,
                    rowHeight,
                    localIndex,
                    playEntryAnimation,
                    out bool appliedCard
                ))
            {
                continue;
            }

            if (appliedCard)
                await YieldPileOverlayBuildIfNeeded(state);
        }
    }

    private bool TryConfigurePileOverlayVirtualHolder(
        PileOverlayVirtualGrid virtualGrid,
        int firstIndex,
        int visibleSlotCount,
        int totalCards,
        float rowHeight,
        int localIndex,
        bool playEntryAnimation,
        out bool appliedCard
    )
    {
        appliedCard = false;
        Control holder = virtualGrid.Holders[localIndex];
        if (holder == null || !GodotObject.IsInstanceValid(holder))
            return false;

        int pileEntryIndex = firstIndex + localIndex;
        if (localIndex >= visibleSlotCount || pileEntryIndex >= totalCards)
        {
            holder.Visible = false;
            return true;
        }

        IndexedBattlePileEntry indexedEntry = virtualGrid.Entries[pileEntryIndex];
        int row = pileEntryIndex / PileOverlayGridColumns;
        int column = pileEntryIndex % PileOverlayGridColumns;
        holder.Position = new Vector2(
            column * (PileCardHolderSize.X + PileOverlayGridHSeparation),
            row * rowHeight
        );

        SkillCard card = GetPileHolderCard(holder);
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            holder.Visible = false;
            return true;
        }

        PlayerCharacter owner = indexedEntry.Entry.Owner ?? virtualGrid.FallbackPlayer;
        bool previewChanged = ApplyPilePreviewCardForOverlayIfChanged(
            holder,
            card,
            owner,
            indexedEntry.Entry.SkillId,
            indexedEntry.Index
        );
        if (previewChanged)
        {
            card.ResetState();
            card.HoverHint.Visible = false;
        }

        ConfigurePileSelectionCard(card, virtualGrid.Kind, indexedEntry.Index);

        if (previewChanged && playEntryAnimation && !virtualGrid.EntryAnimationPlayed)
            PlayPileOverlayCardEntryAnimation(
                card,
                holder,
                virtualGrid.Kind,
                pileEntryIndex,
                totalCards
            );
        else
        {
            card.Position = GetPilePreviewCardRestPosition();
            card.Modulate = SkillButton.EnabledModulate;
            card.Scale = Vector2.One;
        }

        holder.Visible = true;
        appliedCard = previewChanged;
        return true;
    }

    private bool ApplyPilePreviewCardForOverlayIfChanged(
        Control holder,
        SkillCard card,
        PlayerCharacter player,
        SkillID skillId,
        int pileIndex
    )
    {
        ulong ownerId = player?.GetInstanceId() ?? 0;
        if (
            holder.HasMeta(PileHolderPreviewSkillIdMeta)
            && holder.GetMeta(PileHolderPreviewSkillIdMeta).AsInt32() == (int)skillId
            && holder.GetMeta(PileHolderPreviewOwnerIdMeta).AsUInt64() == ownerId
            && holder.GetMeta(PileHolderPreviewPileIndexMeta).AsInt32() == pileIndex
        )
        {
            return false;
        }

        ApplyPilePreviewCardForOverlay(card, player, skillId);
        holder.SetMeta(PileHolderPreviewSkillIdMeta, (int)skillId);
        holder.SetMeta(PileHolderPreviewOwnerIdMeta, ownerId);
        holder.SetMeta(PileHolderPreviewPileIndexMeta, pileIndex);
        return true;
    }

    private static void ClearPileHolderPreviewMeta(Control holder)
    {
        if (holder == null || !GodotObject.IsInstanceValid(holder))
            return;

        if (holder.HasMeta(PileHolderPreviewSkillIdMeta))
            holder.RemoveMeta(PileHolderPreviewSkillIdMeta);
        if (holder.HasMeta(PileHolderPreviewOwnerIdMeta))
            holder.RemoveMeta(PileHolderPreviewOwnerIdMeta);
        if (holder.HasMeta(PileHolderPreviewPileIndexMeta))
            holder.RemoveMeta(PileHolderPreviewPileIndexMeta);
    }

    private void EnsurePileOverlayVirtualHolderCount(
        PileOverlayVirtualGrid virtualGrid,
        int visibleSlotCount
    )
    {
        EnsurePileOverlayVirtualHolderCountCore(virtualGrid, visibleSlotCount);
    }

    private async Task EnsurePileOverlayVirtualHolderCountAsync(
        PileOverlayVirtualGrid virtualGrid,
        int visibleSlotCount,
        PileOverlayBuildState state
    )
    {
        visibleSlotCount = Math.Max(0, visibleSlotCount);
        while (virtualGrid.Holders.Count < visibleSlotCount)
        {
            if (state != null && !IsPileOverlayBuildCurrent(state))
                return;

            AddPileOverlayVirtualHolder(virtualGrid);
            if (state != null)
                await YieldPileOverlayBuildIfNeeded(state);
        }
    }

    private void EnsurePileOverlayVirtualHolderCountCore(
        PileOverlayVirtualGrid virtualGrid,
        int visibleSlotCount
    )
    {
        visibleSlotCount = Math.Max(0, visibleSlotCount);
        while (virtualGrid.Holders.Count < visibleSlotCount)
        {
            if (!AddPileOverlayVirtualHolder(virtualGrid))
                break;
        }
    }

    private bool AddPileOverlayVirtualHolder(PileOverlayVirtualGrid virtualGrid)
    {
        Control holder = CreateEmptyPilePreviewCardHolder(out _);
        if (holder == null)
            return false;

        virtualGrid.Grid.AddChild(holder);
        virtualGrid.Holders.Add(holder);
        return true;
    }

    private static void CalculatePileOverlayVirtualWindow(
        int totalCards,
        float rowHeight,
        float visibleTop,
        float visibleBottom,
        out int firstIndex,
        out int visibleSlotCount
    )
    {
        int totalRows = Mathf.CeilToInt(totalCards / (float)PileOverlayGridColumns);
        int firstRow = Mathf.Max(
            0,
            Mathf.FloorToInt(visibleTop / rowHeight) - PileOverlayVirtualizationBufferRows
        );
        int lastVisibleRow = Mathf.FloorToInt(Mathf.Max(0f, visibleBottom - 1f) / rowHeight);
        int lastRow = Mathf.Min(
            totalRows - 1,
            lastVisibleRow + PileOverlayVirtualizationBufferRows
        );
        if (lastRow < firstRow)
            lastRow = firstRow;

        firstIndex = firstRow * PileOverlayGridColumns;
        visibleSlotCount = Math.Min(
            Math.Max(0, totalCards - firstIndex),
            (lastRow - firstRow + 1) * PileOverlayGridColumns
        );
    }

    private void PlayPileOverlayCardEntryAnimation(
        SkillCard card,
        Control holder,
        BattlePileKind kind,
        PileOverlayBuildState state
    )
    {
        if (card == null || state == null)
            return;

        PlayPileOverlayCardEntryAnimation(
            card,
            holder,
            kind,
            state.CardsCreated,
            state.TotalCards
        );
    }

    private VBoxContainer GetPileOverlaySectionRoot(BattlePileKind kind)
    {
        if (_pileOverlaySections == null || !GodotObject.IsInstanceValid(_pileOverlaySections))
            return null;

        return _pileOverlaySections.GetNodeOrNull<VBoxContainer>(GetPileOverlaySectionName(kind));
    }

    private static void ConfigurePileOverlayLabel(Label label)
    {
        if (label == null)
            return;

        label.CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.MouseFilter = MouseFilterEnum.Ignore;
    }

    private static void ConfigurePileOverlayGrid(GridContainer grid)
    {
        if (grid == null)
            return;

        grid.CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        grid.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        grid.MouseFilter = MouseFilterEnum.Ignore;
        grid.Columns = PileOverlayGridColumns;
        grid.AddThemeConstantOverride("h_separation", PileOverlayGridHSeparation);
        grid.AddThemeConstantOverride("v_separation", PileOverlayGridVSeparation);
    }

    private VBoxContainer CreatePileOverlaySectionRoot(BattlePileKind kind)
    {
        return new VBoxContainer
        {
            Name = GetPileOverlaySectionName(kind),
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
    }

    private static void ConfigurePileOverlayFixedWidthContainer(
        Control control,
        SizeFlags horizontalFlags
    )
    {
        if (control == null)
            return;

        control.CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f);
        control.SizeFlagsHorizontal = horizontalFlags;
        control.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        control.MouseFilter = MouseFilterEnum.Ignore;
    }

    private static string GetPileOverlaySectionName(BattlePileKind kind)
    {
        return kind switch
        {
            BattlePileKind.Draw => "DrawSection",
            BattlePileKind.Discard => "DiscardSection",
            BattlePileKind.Exhausted => "ExhaustedSection",
            _ => "PileSection",
        };
    }

}
