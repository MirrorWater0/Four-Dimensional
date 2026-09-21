using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    public async Task<Dictionary<string, object>> RunHandPreviewDebugScenarioAsync()
    {
        var checks = new List<Dictionary<string, object>>();
        var failures = new List<string>();
        int index = FindHandPreviewDebugCardIndex();
        var result = new Dictionary<string, object>
        {
            ["ok"] = false,
            ["cardIndex"] = index,
            ["checks"] = checks,
            ["failures"] = failures,
        };

        if (!_uiBuilt || !Visible || index < 0)
        {
            failures.Add("no visible hand card is available for hand preview debug");
            return result;
        }

        if (IsManualTargetSelectionPending() || _manualTargetArrowSelectionActive)
        {
            failures.Add("manual target selection is already active");
            return result;
        }

        Skill skill = GetHandSkill(index);
        SkillCard card = _cards[index];
        if (skill == null || card == null || !GodotObject.IsInstanceValid(card))
        {
            failures.Add("selected hand card has no valid skill/card node");
            return result;
        }

        // The harness drives hover state directly. Stop the real-pointer validator so a
        // headless pointer parked at (0, 0) cannot clear that simulated hover mid-check.
        _handHoverValidationTimer?.Stop();
        _queuedHoverRefreshVersion = 0;
        _deferredHoverRefreshVersion++;

        try
        {
            PrepareHandPreviewDebugCard(index);

            HideAllCardHoverPreviews();
            _hoveredCardIndex = -1;
            SetCardHovered(index, true);
            ShowCardHoverPreviewNow(index);
            await WaitHandPreviewDebugFrameAsync();

            int activeCacheBuilds = GetHandPreviewDebugCacheBuilds(card);
            AddHandPreviewDebugCheck(checks, "hover_preview_open", IsCardPreviewDebugActive(index));
            AddHandPreviewDebugCheck(
                checks,
                "hover_preview_cache_attached",
                IsCardPreviewDebugCacheCurrent(card),
                GetHandPreviewDebugState(index)
            );

            int previewContextBeforeShuffle = BattleNode?.HandPreviewContextRevision ?? 0;
            int cacheBuildsBeforeShuffle = GetHandPreviewDebugCacheBuilds(card);
            int shuffledCards = BattleNode?.ShufflePlayerTeamBattleDeck(
                moveDiscardIntoDrawPile: false,
                playAnimation: false,
                triggerSearch: false,
                refreshUi: false
            ) ?? 0;
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "deck_shuffle_refreshes_active_preview",
                shuffledCards > 0
                    && (BattleNode?.HandPreviewContextRevision ?? 0) > previewContextBeforeShuffle
                    && IsCardPreviewDebugActive(index)
                    && IsCardPreviewDebugCacheCurrent(card)
                    && GetHandPreviewDebugCacheBuilds(card) > cacheBuildsBeforeShuffle,
                GetHandPreviewDebugState(index)
            );
            activeCacheBuilds = GetHandPreviewDebugCacheBuilds(card);

            LiftCard(index);
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(checks, "lift_keeps_preview", IsCardPreviewDebugActive(index));
            AddHandPreviewDebugCheck(
                checks,
                "lift_clears_hover_visual",
                !IsCardHoverHintDebugVisible(index),
                GetHandPreviewDebugState(index)
            );
            AddHandPreviewDebugCheck(
                checks,
                "lift_does_not_rebuild_cache",
                GetHandPreviewDebugCacheBuilds(card) == activeCacheBuilds,
                GetHandPreviewDebugState(index)
            );

            PrepareManualTargetArrowLiftedCard(index, skill);
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "prepare_manual_target_clears_preview",
                !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );
            AddHandPreviewDebugCheck(
                checks,
                "prepare_manual_target_clears_hover_visual",
                !IsCardHoverHintDebugVisible(index),
                GetHandPreviewDebugState(index)
            );

            _manualTargetArrowSelectionActive = true;
            _manualTargetArrowCardIndex = index;
            SuppressHandInteractionForManualTargetSelection();
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "manual_target_suppression_clears_lifted_preview",
                !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );
            AddHandPreviewDebugCheck(
                checks,
                "manual_target_suppression_does_not_rebuild_cache",
                GetHandPreviewDebugCacheBuilds(card) == activeCacheBuilds,
                GetHandPreviewDebugState(index)
            );

            card.ShowSkillPreview();
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "repeated_show_uses_active_preview",
                GetHandPreviewDebugCacheBuilds(card) == activeCacheBuilds,
                GetHandPreviewDebugState(index)
            );

            RefreshTurnUi();
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "refresh_turn_ui_keeps_cache",
                GetHandPreviewDebugCacheBuilds(card) == activeCacheBuilds,
                GetHandPreviewDebugState(index)
            );

            _manualTargetArrowSelectionActive = false;
            _manualTargetArrowCardIndex = -1;
            ClearLiftedCard(instant: true);
            await WaitHandPreviewDebugFrameAsync();
            PrepareHandPreviewDebugCard(index);

            HideCardHoverPreview(index);
            _hoveredCardIndex = -1;
            SetCardHovered(index, true);
            SetCardHoverPreviewActive(index, true);
            bool pendingBeforeLift = _pendingCardHoverPreviewIndex == index;
            LiftCard(index);
            PrepareManualTargetArrowLiftedCard(index, skill);
            _manualTargetArrowSelectionActive = true;
            _manualTargetArrowCardIndex = index;
            SuppressHandInteractionForManualTargetSelection();
            await WaitHandPreviewDebugDelayAsync();
            AddHandPreviewDebugCheck(
                checks,
                "pending_preview_canceled_for_manual_target",
                pendingBeforeLift && !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );
            AddHandPreviewDebugCheck(
                checks,
                "pending_preview_does_not_restore_hover_visual",
                !IsCardHoverHintDebugVisible(index),
                GetHandPreviewDebugState(index)
            );

            if (index != _liftedCardIndex)
            {
                SetCardHovered(index, false);
                SetCardHoverPreviewActive(index, false);
            }
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "mouse_exit_policy_keeps_manual_target_preview_closed",
                !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );

            _manualTargetArrowSelectionActive = false;
            _manualTargetArrowCardIndex = -1;
            ClearLiftedCard(instant: false);
            await WaitHandPreviewDebugFrameAsync();
            await WaitHandPreviewDebugFrameAsync();
            _suppressHandHoverUntilMouseMove = false;
            _handHoverSuppressionRequiresMouseMove = false;
            RestoreStableHandInputAfterQueuedPlay();
            RefreshTurnUi();
            await WaitHandPreviewDebugFrameAsync();

            card = _cards[index];
            bool canLiftAgain =
                card != null
                && GodotObject.IsInstanceValid(card)
                && card.Visible
                && !card.Button.Disabled;
            if (canLiftAgain)
                LiftCard(index);
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "drop_can_lift_again",
                canLiftAgain && _liftedCardIndex == index,
                GetHandPreviewDebugState(index)
            );

            ClearLiftedCard(instant: true);
            await WaitHandPreviewDebugFrameAsync();
            PrepareHandPreviewDebugCard(index);
            HideCardHoverPreview(index);
            _hoveredCardIndex = -1;
            SetCardHovered(index, true);
            ShowCardHoverPreviewNow(index);
            await WaitHandPreviewDebugFrameAsync();
            SetCardHovered(index, false);
            SetCardHoverPreviewActive(index, false);
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "mouse_exit_closes_unlifted_preview",
                !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );
            AddHandPreviewDebugCheck(
                checks,
                "mouse_exit_clears_unlifted_hover_visual",
                !IsCardHoverHintDebugVisible(index),
                GetHandPreviewDebugState(index)
            );

            PrepareHandPreviewDebugCard(index);
            SetCardHovered(index, true);
            ShowCardHoverPreviewNow(index);
            await WaitHandPreviewDebugFrameAsync();
            PrepareHandVisualsBeforeQueuedPlay(index, _cards[index]);
            await WaitHandPreviewDebugFrameAsync();
            AddHandPreviewDebugCheck(
                checks,
                "play_visuals_close_preview",
                !IsCardPreviewDebugActive(index),
                GetHandPreviewDebugState(index)
            );
        }
        finally
        {
            _manualTargetArrowSelectionActive = false;
            _manualTargetArrowCardIndex = -1;
            _manualTargetArrowUsesLiftedCard = false;
            if (_liftedCardIndex != -1)
                ClearLiftedCard(instant: true);
            HideCardHoverPreview(index);
            SetHandInputBlockerVisible(false);
            SetCardHoverUiEnabled(true);
            RestoreStableHandInputAfterQueuedPlay();
            _handHoverValidationTimer?.Start();
            RequestTurnUiRefresh(refreshHover: true);
        }

        foreach (var check in checks)
        {
            if (check.TryGetValue("ok", out object okValue) && okValue is bool ok && ok)
                continue;

            failures.Add(check.TryGetValue("name", out object name) ? name?.ToString() : "unknown");
        }

        result["ok"] = failures.Count == 0;
        result["finalState"] = GetDebugHandPreviewState();
        return result;
    }

    private int FindHandPreviewDebugCardIndex()
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return -1;

        int count = Math.Min(_cards.Length, hand.Length);
        for (int i = 0; i < count; i++)
        {
            SkillCard card = _cards[i];
            if (
                hand[i] != null
                && card != null
                && GodotObject.IsInstanceValid(card)
                && card.Visible
                && !IsCardCommitted(i)
                && !IsCardDrawEntryInputBlocked(i)
            )
            {
                return i;
            }
        }

        return -1;
    }

    private void PrepareHandPreviewDebugCard(int index)
    {
        _manualTargetArrowSelectionActive = false;
        _manualTargetArrowCardIndex = -1;
        _manualTargetArrowUsesLiftedCard = false;
        _suppressHandHoverUntilMouseMove = false;
        _handHoverSuppressionRequiresMouseMove = false;
        SetHandInputBlockerVisible(false);
        SetCardHoverUiEnabled(true);
        RestoreStableHandInputAfterQueuedPlay();

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
            SetCardButtonInputEnabled(card, true);
    }

    private bool IsCardPreviewDebugActive(int index)
    {
        if (!IsCardIndexValid(index) || !_cardHoverPreviewActive[index])
            return false;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return false;

        Dictionary<string, object> state = card.GetDebugSkillPreviewState();
        return state.TryGetValue("previewActive", out object active)
            && active is bool activeBool
            && activeBool
            && state.TryGetValue("activeSkillMatchesCurrent", out object matches)
            && matches is bool matchesBool
            && matchesBool;
    }

    private bool IsCardPreviewDebugCacheCurrent(SkillCard card)
    {
        Dictionary<string, object> state = card.GetDebugSkillPreviewState();
        return state.TryGetValue("cachedSkillMatchesCurrent", out object matches)
            && matches is bool matchesBool
            && matchesBool
            && state.TryGetValue("cachedPreviewRevision", out object cachedRevision)
            && state.TryGetValue("currentPreviewRevision", out object currentRevision)
            && cachedRevision is int cachedInt
            && currentRevision is int currentInt
            && cachedInt == currentInt;
    }

    private int GetHandPreviewDebugCacheBuilds(SkillCard card)
    {
        Dictionary<string, object> state = card.GetDebugSkillPreviewState();
        return state.TryGetValue("cacheBuilds", out object builds)
            && builds is int intBuilds
            ? intBuilds
            : 0;
    }

    private Dictionary<string, object> GetHandPreviewDebugState(int index)
    {
        var state = new Dictionary<string, object>
        {
            ["hoveredCardIndex"] = _hoveredCardIndex,
            ["liftedCardIndex"] = _liftedCardIndex,
            ["pendingPreviewIndex"] = _pendingCardHoverPreviewIndex,
            ["manualTargetArrowSelectionActive"] = _manualTargetArrowSelectionActive,
            ["manualTargetArrowCardIndex"] = _manualTargetArrowCardIndex,
            ["cardHoverPreviewActive"] =
                IsCardIndexValid(index) && _cardHoverPreviewActive[index],
        };

        SkillCard card = IsCardIndexValid(index) ? _cards[index] : null;
        if (card != null && GodotObject.IsInstanceValid(card))
            state["skillPreview"] = card.GetDebugSkillPreviewState();

        return state;
    }

    private bool IsCardHoverHintDebugVisible(int index)
    {
        SkillCard card = IsCardIndexValid(index) ? _cards[index] : null;
        return card != null
            && GodotObject.IsInstanceValid(card)
            && card.HoverHint != null
            && GodotObject.IsInstanceValid(card.HoverHint)
            && card.HoverHint.Visible;
    }

    private static void AddHandPreviewDebugCheck(
        List<Dictionary<string, object>> checks,
        string name,
        bool ok,
        Dictionary<string, object> state = null
    )
    {
        var check = new Dictionary<string, object>
        {
            ["name"] = name,
            ["ok"] = ok,
        };
        if (state != null)
            check["state"] = state;
        checks.Add(check);
    }

    private async Task WaitHandPreviewDebugFrameAsync()
    {
        SceneTree tree = GetTree();
        if (tree != null)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private async Task WaitHandPreviewDebugDelayAsync()
    {
        SceneTree tree = GetTree();
        if (tree == null)
            return;

        float delay = (HandHoverPreviewDelayMs + 35) / 1000.0f;
        await ToSignal(tree.CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
