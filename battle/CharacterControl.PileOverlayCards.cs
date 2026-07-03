using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private Control CreatePilePreviewCardHolder(
        PlayerCharacter player,
        SkillID skillId,
        out SkillCard card
    )
    {
        if (GetPilePreviewSkill(player, skillId) == null)
        {
            card = null;
            return null;
        }

        return CreateEmptyPilePreviewCardHolder(out card);
    }

    private Control CreateEmptyPilePreviewCardHolder(out SkillCard card)
    {
        Control holder = RentPileCardHolder();
        card = GetPileHolderCard(holder);
        if (card == null)
        {
            card = SkillCardScene.Instantiate<SkillCard>();
            holder.AddChild(card);
        }

        holder.Name = "PileCardHolder";
        holder.Position = Vector2.Zero;
        holder.CustomMinimumSize = PileCardHolderSize;
        holder.Size = PileCardHolderSize;
        holder.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        holder.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        holder.MouseFilter = MouseFilterEnum.Ignore;
        holder.ClipContents = false;
        holder.Visible = false;
        card.Visible = true;
        card.Modulate = Colors.Transparent;
        card.Scale = Vector2.One;
        card.Position = GetPilePreviewCardRestPosition();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        return holder;
    }

    private static Vector2 GetPilePreviewCardRestPosition() =>
        PileCardHolderPadding - 0.5f * (Vector2.One - PileCardScale) * BattleCardBaseSize;

    private Control RentPileCardHolder()
    {
        while (_pileCardHolderPool.Count > 0)
        {
            Control holder = _pileCardHolderPool.Pop();
            if (holder != null && GodotObject.IsInstanceValid(holder))
                return holder;
        }

        return new Control();
    }

    private void PlayPileOverlayCardEntryAnimation(
        SkillCard card,
        Control holder,
        BattlePileKind kind,
        int cardIndex,
        int totalCards
    )
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 targetPosition = card.Position;
        if (cardIndex >= PileOverlayAnimatedCardCount)
        {
            card.Position = targetPosition;
            card.Modulate = SkillButton.EnabledModulate;
            card.Scale = Vector2.One;
            card.SetTransientPointerInputDisabled(false, refreshHoverWhenEnabled: true);
            return;
        }

        float delay = PileOverlayCardEntryBaseDelay
            + Math.Min(cardIndex, PileOverlayAnimatedCardCount - 1) * PileOverlayCardEntryStagger;

        Vector2? startPosition = GetPileOverlayCardEntryStartPosition(
            holder,
            GetPileButtonForKind(kind)
        );
        card.SetTransientPointerInputDisabled(true);
        card.Position = startPosition ?? targetPosition + new Vector2(0f, PileOverlayCardEntryYOffset);
        Color targetModulate = SkillButton.EnabledModulate;
        card.Modulate = new Color(targetModulate.R, targetModulate.G, targetModulate.B, 0f);
        card.Scale = Vector2.One;
        Tween tween = card.CreateTween();
        tween.SetParallel(true);

        tween
            .TweenProperty(card, "position", targetPosition, PileOverlayCardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "modulate", targetModulate, PileOverlayCardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(card))
                    card.SetTransientPointerInputDisabled(
                        false,
                        refreshHoverWhenEnabled: true
                    );
            })
        );
    }

    private static int CountShatterSlashCardsInSections(
        IReadOnlyList<BattlePileOverlaySection> sections
    )
    {
        if (sections == null || sections.Count == 0)
            return 0;

        return sections.Sum(section =>
            section.Pile?.Count(entry => entry.SkillId == SkillID.ShatterSlash) ?? 0
        );
    }

    private SkillID? GetPileSelectionTriggerSkillId()
    {
        if (_activeQueuedCardPlay?.SkillId is SkillID queuedSkillId)
            return queuedSkillId;

        return _activeQueuedCardPlay?.Skill?.SkillId;
    }

    private string GetPileSelectionTriggerSkillName()
    {
        Skill skill = _activeQueuedCardPlay?.Skill;
        if (skill == null)
            return "unknown";

        return string.IsNullOrWhiteSpace(skill.SkillName) ? skill.GetType().Name : skill.SkillName;
    }

    private static string FormatPilePerfTriggerSkill(PileOverlayOpenContext context)
    {
        if (context.Source != PileOverlayOpenSource.CardSelection)
            return "-";

        string skillName = string.IsNullOrWhiteSpace(context.TriggerSkillName)
            ? "unknown"
            : context.TriggerSkillName;
        if (context.TriggerSkillId is SkillID skillId)
            return $"{skillName}({skillId})";

        return skillName;
    }

    private static string FormatPilePerfOpenSource(PileOverlayOpenContext context)
    {
        return context.Source switch
        {
            PileOverlayOpenSource.CardSelection =>
                $"card-selection trigger={FormatPilePerfTriggerSkill(context)}",
            PileOverlayOpenSource.ShowAllPiles => "show-all-piles",
            _ => "manual-view",
        };
    }

    private void LogPileOverlayBuildPerf(
        PlayerCharacter player,
        IReadOnlyList<BattlePileOverlaySection> sections,
        PileOverlayBuildState state,
        ulong buildStartUsec
    )
    {
        Battle battle = BattleNode;
        if (battle == null || state == null)
            return;

        double populateMs = (Time.GetTicksUsec() - buildStartUsec) / 1000.0;
        PileOverlayApplyPerfTracker perf = state.ApplyPerf;
        PileOverlayOpenContext context = state.OpenContext;
        string sectionSummary = string.Join(
            ", ",
            (sections ?? Array.Empty<BattlePileOverlaySection>()).Select(section =>
                $"{section.Title}:{section.Pile.Length}"
            )
        );
        string playerName = string.IsNullOrWhiteSpace(player?.CharacterName)
            ? player?.Name ?? "<player>"
            : player.CharacterName;
        double shatterAvg = perf.ShatterSlashCount > 0
            ? perf.ShatterSlashApplyMs / perf.ShatterSlashCount
            : 0d;
        double otherAvg = perf.OtherCount > 0 ? perf.OtherApplyMs / perf.OtherCount : 0d;
        string sourceLabel = FormatPilePerfOpenSource(context);

        battle.LogPilePerfSummary(
            $"open [{sourceLabel}] [{sectionSummary}] player={playerName}, total={state.TotalCards}, "
            + $"prep={context.PrepMs:F2}ms, sync={context.SyncSetupMs:F2}ms, populate={populateMs:F2}ms, "
            + $"applied ShatterSlash={perf.ShatterSlashCount} ({perf.ShatterSlashApplyMs:F2}ms, avg {shatterAvg:F2}ms), "
            + $"applied other={perf.OtherCount} ({perf.OtherApplyMs:F2}ms, avg {otherAvg:F2}ms), "
            + $"virtual={state.UsesVirtualization}"
        );

        if (
            context.Source == PileOverlayOpenSource.CardSelection
            && context.FlowStartUsec > 0
        )
        {
            double flowMs = (Time.GetTicksUsec() - context.FlowStartUsec) / 1000.0;
            battle.LogPilePerfSummary(
                $"card-selection ready in {flowMs:F2}ms trigger={FormatPilePerfTriggerSkill(context)}, "
                + $"prep={context.PrepMs:F2}ms, sync={context.SyncSetupMs:F2}ms, populate={populateMs:F2}ms, "
                + $"pile={state.TotalCards}, virtual={state.UsesVirtualization}"
            );
            battle.MarkPilePerfEvent(
                $"card-selection trigger={FormatPilePerfTriggerSkill(context)}, pile={state.TotalCards}, flow={flowMs:F0}ms"
            );
            return;
        }

        battle.MarkPilePerfEvent(
            $"pile-open source={context.Source}, total={state.TotalCards}, populate={populateMs:F0}ms"
        );
    }

    private void ApplyPilePreviewCardForOverlay(
        SkillCard card,
        PlayerCharacter player,
        SkillID skillId
    )
    {
        ulong startUsec = Time.GetTicksUsec();
        ApplyPilePreviewCard(card, player, skillId);
        if (_pileOverlayActiveApplyPerfTracker == null)
            return;

        double elapsedMs = (Time.GetTicksUsec() - startUsec) / 1000.0;
        _pileOverlayActiveApplyPerfTracker.Record(skillId, elapsedMs);
    }

    private void ApplyPilePreviewCard(SkillCard card, PlayerCharacter player, SkillID skillId)
    {
        Skill skill = GetPilePreviewSkill(player, skillId);
        if (skill == null || card == null)
            return;

        bool isStatusCard = skill.IsStatusCard;

        card.Name = $"PileCard_{skillId}";
        card.ConfigureDisplayScale(PileCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.AutoAdjustDescriptionTextSize = false;
        card.PreviewCharacterName = isStatusCard ? null : player?.CharacterName;
        card.PreviewCharacterKey = isStatusCard ? null : player?.CharacterKey;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Button.Disabled = false;
        card.Button.FocusMode = FocusModeEnum.None;
        card.SetSkill(skill);
        card.CharacterName.Text = isStatusCard ? string.Empty : GetCharacterEnergyDisplayName(player);
        card.ConfigurePilePreviewVisuals();
    }

    private Skill GetPilePreviewSkill(PlayerCharacter player, SkillID skillId)
    {
        bool isStatusCard = IsStatusSkillId(skillId);
        ulong ownerId = isStatusCard ? 0UL : player?.GetInstanceId() ?? 0UL;
        var key = new PileOverlayPreviewSkillKey(skillId, ownerId);
        if (_pileOverlayPreviewSkillCache.TryGetValue(key, out Skill cachedSkill))
            return cachedSkill;

        Skill skill = Skill.GetSkill(skillId);
        if (skill == null)
            return null;

        if (!isStatusCard)
            skill.OwnerCharater = player;

        _pileOverlayPreviewSkillCache[key] = skill;
        return skill;
    }

    private bool IsStatusSkillId(SkillID skillId)
    {
        if (_pileOverlayStatusSkillIdCache.TryGetValue(skillId, out bool cached))
            return cached;

        bool isStatusCard = Skill.GetSkill(skillId)?.IsStatusCard == true;
        _pileOverlayStatusSkillIdCache[skillId] = isStatusCard;
        return isStatusCard;
    }

    private static string GetPileGroupDisplayName(PlayerCharacter owner) =>
        owner?.CharacterName ?? I18n.Tr("ui.encyclopedia.skill_type.status", "状态");

    private void ClearPileOverlayCards()
    {
        _pileOverlayVirtualGrids.Clear();
        ClearAllPileSelectionCardBindings();
        if (_pileOverlaySections != null && GodotObject.IsInstanceValid(_pileOverlaySections))
        {
            foreach (Node child in _pileOverlaySections.GetChildren())
            {
                if (child is not Control section)
                    continue;

                section.Visible = false;
                if (section.GetNodeOrNull<Label>("Empty") is Label emptyLabel)
                    emptyLabel.Visible = false;
                if (section.GetNodeOrNull<VBoxContainer>("List") is VBoxContainer list)
                {
                    list.Visible = false;
                    ClearGridChildren(list);
                }
                if (section.GetNodeOrNull<GridContainer>("Grid") is GridContainer grid)
                {
                    grid.Visible = false;
                    ClearGridChildren(grid);
                }
                if (section.GetNodeOrNull<VBoxContainer>("Groups") is VBoxContainer groups)
                {
                    groups.Visible = false;
                    ClearGridChildren(groups);
                }
                if (section.GetNodeOrNull<Control>("VirtualGrid") is Control virtualGrid)
                {
                    virtualGrid.Visible = false;
                    ClearGridChildren(virtualGrid);
                }
            }
            _pileOverlayGrid = null;
            _pileCardSelectionCards.Clear();
            return;
        }

        if (_pileOverlayMargin == null || !GodotObject.IsInstanceValid(_pileOverlayMargin))
        {
            if (_pileOverlayGrid == null || !GodotObject.IsInstanceValid(_pileOverlayGrid))
                return;

            for (int i = _pileOverlayGrid.GetChildCount() - 1; i >= 0; i--)
            {
                Node child = _pileOverlayGrid.GetChild(i);
                _pileOverlayGrid.RemoveChild(child);
                ReleasePileOverlayChild(child);
            }
            _pileCardSelectionCards.Clear();
            return;
        }

        for (int i = _pileOverlayMargin.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = _pileOverlayMargin.GetChild(i);
            _pileOverlayMargin.RemoveChild(child);
            ReleasePileOverlayChild(child);
        }
        _pileOverlayGrid = null;
        _pileCardSelectionCards.Clear();
    }

    private void ClearGridChildren(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;

        for (int i = node.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = node.GetChild(i);
            node.RemoveChild(child);
            ReleasePileOverlayChild(child);
        }
    }

    private void ReleasePileOverlayChild(Node child)
    {
        if (child == null || !GodotObject.IsInstanceValid(child))
            return;

        if (TryPoolPileCardHolder(child))
            return;

        for (int i = child.GetChildCount() - 1; i >= 0; i--)
        {
            Node grandChild = child.GetChild(i);
            child.RemoveChild(grandChild);
            ReleasePileOverlayChild(grandChild);
        }

        child.QueueFree();
    }

    private bool TryPoolPileCardHolder(Node node)
    {
        if (
            node is not Control holder
            || holder.Name != "PileCardHolder"
            || _pileCardHolderPool.Count >= PileOverlayMaxPooledCardHolders
        )
        {
            return false;
        }

        SkillCard card = GetPileHolderCard(holder);
        if (card == null || !GodotObject.IsInstanceValid(card) || card.Button.ToggleMode)
            return false;

        ClearPileSelectionCardBinding(card);
        ClearPileHolderPreviewMeta(holder);
        card.SetTransientPointerInputDisabled(false);
        card.HideHoverUi();
        card.RestoreDisplayState();
        card.Button.Disabled = true;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Position = GetPilePreviewCardRestPosition();
        card.Modulate = SkillButton.EnabledModulate;
        card.Scale = Vector2.One;
        holder.Position = Vector2.Zero;
        holder.Visible = false;
        _pileCardHolderPool.Push(holder);
        return true;
    }

    private static SkillCard GetPileHolderCard(Control holder)
    {
        if (holder == null || !GodotObject.IsInstanceValid(holder))
            return null;

        for (int i = 0; i < holder.GetChildCount(); i++)
        {
            if (holder.GetChild(i) is SkillCard card)
                return card;
        }

        return null;
    }

}
