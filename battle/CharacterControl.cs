using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl : Control
{
    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/Reward/SkillCard.tscn"
    );
    private static readonly PackedScene CharacterTargetCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/ManualTarget/CharacterTargetCard.tscn"
    );
    private static readonly PackedScene ManualTargetArrowScene = GD.Load<PackedScene>(
        "res://battle/UIScene/ManualTarget/ManualTargetArrowView.tscn"
    );
    private static readonly PackedScene BattlePileOverlayScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattlePileOverlay.tscn"
    );
    private static readonly Vector2 BattleCardBaseSize = new(240f, 370f);
    private static readonly Vector2 BattleCardScale = new(0.936f, 0.936f);
    private static readonly Vector2 PileCardScale = new(0.74f, 0.74f);
    private static readonly Vector2 PileCardDisplaySize = BattleCardBaseSize * PileCardScale;
    private static readonly Vector2 PileCardHolderPadding = new(14f, 18f);
    private static readonly Vector2 PileCardHolderSize = PileCardDisplaySize + PileCardHolderPadding * 2f;
    private const float PileOverlayContentWidth = 1000f;
    private const int PileOverlaySectionSeparation = 48;
    private const float PileCardEnterStagger = 0.025f;
    private const float EndTurnCardVanishDuration = 0.32f;
    private const float CardPlayVanishDuration = 0.5f;
    private const float CardPlayDiscardCompressDuration = 0.24f;
    private const float CardPlayDiscardCenterVanishBeforeFly = 0.92f;
    private const float CardPlayDiscardFlyDuration = 0.46f;
    private const float CardPlayDiscardTrailFadeDuration = 0.22f;
    private const float CardPlayDiscardSquareSize = 72f;
    private const float CardPlayDiscardTargetScaleMultiplier = 0.08f;
    private const float CardPlayMoveDuration = 0.18f;
    private const float StatusInsertCardScale = 0.62f;
    private const float StatusInsertArrangeDuration = 0.22f;
    private const float StatusInsertHoldDuration = 0.22f;
    private const float StatusInsertFlyDuration = 0.34f;
    private const float StatusInsertImpactFadeDuration = 0.12f;
    private const float StatusInsertStagger = 0.055f;
    private const int StatusInsertCardsCreatedPerFrame = 3;
    private const float CardHoverLiftY = -22f;
    private const float CardHoverScaleMultiplier = 1.03f;
    private const float CarryCardSpawnScaleMultiplier = 0.72f;
    private const float HandAreaPadding = 18f;
    private const float HandCardGap = 14f;
    private const float HandLayoutTweenDuration = 0.14f;
    private const float HandDrawEntryTweenDuration = 0.30f;
    private const float HandDrawTrailFadeDuration = 0.18f;
    private const float HandDrawEntryStagger = 0.055f;
    private const float HandDrawExistingShiftDelayRatio = 0.08f;
    private const float HandDrawExistingShiftStagger = 0.018f;
    private const float HandDrawRevealLeadTime = 0.035f;
    private const float HandDrawRevealDuration = 0.18f;
    private const int CardPlayOverlayLayer = 80;
    private const int PlayedCardZIndex = 100;
    private const int TemporaryCarryCardZIndex = 300;
    private const int ManualTargetPickerZIndex = 400;
    private const int HandCardCapacity = PlayerCharacter.MaxBattleHandSize;
    private static readonly Vector2 PlayedCardScale = new(1.104f, 1.104f);
    private static readonly Color QueuedCardModulate = new(1f, 1f, 1f, 0.82f);
    private static readonly Color ManualTargetCandidateColor = new(0.42f, 0.82f, 1f, 0.72f);
    private static readonly Color ManualTargetHoveredColor = new(1f, 0.95f, 0.62f, 1f);
    private static readonly Vector2 ManualTargetDamagePreviewLabelOffset = new(-50f, -130f);

    public Battle BattleNode => field ??= GetParent<Battle>();
    public bool IsManualTargetArrowSelectionActive => _manualTargetArrowSelectionActive;
    public Frame CharaterFrame1 => field ??= GetNodeOrNull<Frame>("frame1");
    public Frame CharaterFrame2 => field ??= GetNodeOrNull<Frame>("frame2");
    public Frame CharaterFrame3 => field ??= GetNodeOrNull<Frame>("frame3");
    public Frame CharaterFrame4 => field ??= GetNodeOrNull<Frame>("frame4");
    public Frame[] CharactersControl =>
        new[] { CharaterFrame1, CharaterFrame2, CharaterFrame3, CharaterFrame4 };
    public Button EndTurnButton => _endTurnButton;
    public Control ActionCardContainer => _cardRow;

    private VBoxContainer _root;
    private Label _statusLabel;
    private Control _cardRow;
    private Control _manualTargetPickerRoot;
    private ColorRect _manualTargetPickerMask;
    private HBoxContainer _manualTargetPickerRow;
    private Button _manualTargetPickerHideButton;
    private SkillCard _manualTargetPickerPlayedCard;
    private Control _manualTargetArrowRoot;
    private ColorRect _manualTargetArrowMask;
    private ManualTargetArrowView _manualTargetArrowLayer;
    private Label _manualTargetArrowHintLabel;
    private Character[] _manualTargetArrowTargets = Array.Empty<Character>();
    private Character _manualTargetArrowOwner;
    private Character _manualTargetArrowHoveredTarget;
    private Skill _manualTargetArrowSkill;
    private int _manualTargetArrowCardIndex = -1;
    private bool _manualTargetArrowSelectionActive;
    private readonly List<VBoxContainer> _manualTargetArrowDamagePanels = new();
    private Button _endTurnButton;
    private Button _drawPileButton;
    private Button _discardPileButton;
    private Button _exhaustedPileButton;
    private Button _manualTargetInfoButton;
    private Tween _drawPileHoverTween;
    private Tween _discardPileHoverTween;
    private Tween _exhaustedPileHoverTween;
    private Tween _drawPileShaderTween;
    private Tween _discardPileShaderTween;
    private Tween _exhaustedPileShaderTween;
    private CanvasLayer _pileOverlayLayer;
    private Control _pileOverlayRoot;
    private MarginContainer _pileOverlayMargin;
    private VBoxContainer _pileOverlaySections;
    private GridContainer _pileOverlayGrid;
    private SkillCard[] _cards = new SkillCard[HandCardCapacity];
    private Control[] _cardSlots = new Control[HandCardCapacity];
    private Tween[] _cardSlotLayoutTweens = new Tween[HandCardCapacity];
    private Vector2?[] _cardSlotLayoutTargets = new Vector2?[HandCardCapacity];
    private readonly HashSet<int> _drawEntrySlotIndexes = new();
    private readonly Dictionary<int, int> _drawEntrySlotOrders = new();
    private readonly Queue<QueuedCardPlay> _queuedCardPlays = new();
    private readonly Queue<QueuedCardPlay> _queuedFollowUpCardPlays = new();
    private readonly HashSet<int> _queuedCardIndices = new();
    private Skill[] _displayedSkills = new Skill[HandCardCapacity];
    private SkillID?[] _displayedSkillIds = new SkillID?[HandCardCapacity];
    private bool[] _cardDisplayInitialized = new bool[HandCardCapacity];
    private PlayerCharacter _activePlayer;
    private bool _isResolvingCard;
    private bool _isProcessingCardQueue;
    private bool _uiBuilt;
    private int _hoveredCardIndex = -1;
    private int _liftedCardIndex = -1;
    private Vector2 _liftedCardMouseOffset;
    private bool[] _cardHoverPreviewActive = new bool[HandCardCapacity];
    private bool _suppressCardButtonPressUntilLeftRelease;
    private bool _layoutInitialized;
    private bool _suppressNextRefreshLayout;
    private bool _endTurnQueued;
    private bool _manualTargetInfoOpen;
    private bool _manualTargetPickerTemporarilyHidden;
    private TaskCompletionSource<Character> _manualTargetCompletion;

    public readonly struct StatusCardInsertAnimationEntry
    {
        public StatusCardInsertAnimationEntry(
            Character target,
            SkillID statusSkillId,
            int count,
            Character source = null
        )
        {
            Target = target;
            StatusSkillId = statusSkillId;
            Count = count;
            Source = source;
        }

        public Character Target { get; }
        public SkillID StatusSkillId { get; }
        public int Count { get; }
        public Character Source { get; }
    }

    public readonly struct StatusCardExhaustAnimationEntry
    {
        public StatusCardExhaustAnimationEntry(PlayerCharacter player, SkillID statusSkillId)
        {
            Player = player;
            StatusSkillId = statusSkillId;
        }

        public PlayerCharacter Player { get; }
        public SkillID StatusSkillId { get; }
    }

    private sealed class QueuedCardPlay
    {
        public Character Actor { get; init; }
        public int Index { get; init; }
        public Skill Skill { get; init; }
        public SkillID? SkillId { get; init; }
        public Skill.SkillTypes SkillType { get; init; }
        public bool HadStun { get; init; }
        public SkillCard Card { get; init; }
        public bool IsHandCard { get; init; }
        public bool IsTemporaryCard { get; init; }
        public bool QueueFreeCardAfterVanish { get; init; }
        public bool FreeEnergyCost { get; init; }
        public bool ForceManualTargetCardPicker { get; init; }
        public TaskCompletionSource<bool> Completion { get; init; }
    }

    private sealed class StatusInsertPreviewCard
    {
        public SkillCard Card { get; init; }
        public Character Target { get; init; }
        public Vector2 Scale { get; init; }
        public Vector2 CardSize { get; init; }
        public int TargetIndex { get; set; }
        public int TargetCount { get; set; }
    }

    private readonly struct BattlePileOverlaySection
    {
        public BattlePileOverlaySection(
            BattlePileKind kind,
            string title,
            SkillID[] pile
        )
        {
            Kind = kind;
            Title = title;
            Pile = pile ?? Array.Empty<SkillID>();
        }

        public BattlePileKind Kind { get; }
        public string Title { get; }
        public SkillID[] Pile { get; }
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
        UpdateLiftedCardPosition();
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
            _ = QueueEndTurnAsync();
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
            _manualTargetInfoOpen
            && @event is InputEventKey infoCloseKey
            && infoCloseKey.Pressed
            && !infoCloseKey.Echo
            && infoCloseKey.Keycode == Key.Escape
        )
        {
            HideManualTargetPicker();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            @event is InputEventMouseButton leftMouseButton
            && leftMouseButton.Pressed
            && leftMouseButton.ButtonIndex == MouseButton.Left
            && _liftedCardIndex != -1
        )
        {
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
            && _suppressCardButtonPressUntilLeftRelease
        )
        {
            CallDeferred(nameof(ClearCardButtonPressSuppression));
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
            else if (_manualTargetInfoOpen)
            {
                HideManualTargetPicker();
            }
            else if (IsPileOverlayVisible())
            {
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
        _activePlayer = player;
        _isResolvingCard = false;
        _isProcessingCardQueue = false;
        _endTurnQueued = false;
        HidePileOverlay();
        HideManualTargetPicker();
        ClearCardQueue(resetCards: true);
        ClearLiftedCard(instant: true);
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

        _isResolvingCard = false;
        _isProcessingCardQueue = false;
        _endTurnQueued = false;
        HidePileOverlay();
        ClearCardQueue(resetCards: true);
        ClearLiftedCard(instant: true);
        _activePlayer = null;
        Visible = keepPanelVisible;
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

    public void RefreshTextSizeFromSettings()
    {
        if (!_uiBuilt)
            return;

        foreach (SkillCard card in _cards)
            card?.RefreshTextSizeFromSettings();

        if (_pileOverlayGrid != null && GodotObject.IsInstanceValid(_pileOverlayGrid))
        {
            foreach (SkillCard card in EnumerateSkillCards(_pileOverlayGrid))
                card.RefreshTextSizeFromSettings();
        }

        RefreshTurnUi();
    }

    public void RefreshManualTargetCardVisibilityFromSettings()
    {
        ApplyManualTargetPickerTemporaryHiddenState();
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

    public async Task PlayStatusCardInsertAnimationAsync(
        IReadOnlyList<StatusCardInsertAnimationEntry> entries
    )
    {
        if (entries == null || entries.Count == 0 || !IsInsideTree())
            return;

        var expandedEntries = new List<(Character Target, Character Source, Skill StatusSkill)>();
        var statusSkillCache = new Dictionary<SkillID, Skill>();
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
                expandedEntries.Add((entry.Target, entry.Source, statusSkill));
        }

        if (expandedEntries.Count == 0)
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        var cards = new List<StatusInsertPreviewCard>(expandedEntries.Count);
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
        Vector2 spawnPosition = new(
            viewportSize.X * 0.5f - cardSize.X * 0.5f,
            startY - 42f
        );
        float stagger = GetStatusInsertStagger(expandedEntries.Count);
        var arrangeTasks = new List<Task>();

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
            card.ZIndex = TemporaryCarryCardZIndex + i;
            cards.Add(
                new StatusInsertPreviewCard
                {
                    Card = card,
                    Target = entry.Target,
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
                .SetTrans(Tween.TransitionType.Back)
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
            arrangeTasks.Add(WaitForTweenFinishedAsync(arrangeTween));

            if ((i + 1) % StatusInsertCardsCreatedPerFrame == 0 && i + 1 < expandedEntries.Count)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        if (cards.Count == 0)
            return;

        AssignStatusInsertTargetOffsets(cards);

        await Task.WhenAll(arrangeTasks);
        await ToSignal(GetTree().CreateTimer(StatusInsertHoldDuration), SceneTreeTimer.SignalName.Timeout);

        var flyTasks = new List<Task>();
        for (int i = 0; i < cards.Count; i++)
        {
            StatusInsertPreviewCard preview = cards[i];
            SkillCard card = preview.Card;
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            float delay = i * stagger;
            Vector2 targetPosition = GetStatusInsertTargetPosition(preview.Target, preview.CardSize);
            Vector2 hitPosition =
                targetPosition
                + new Vector2(
                    (preview.TargetIndex - (preview.TargetCount - 1) * 0.5f) * 8f,
                    0f
                );
            Tween flyTween = card.CreateTween();
            flyTween.SetParallel(true);
            flyTween
                .TweenProperty(card, "global_position", hitPosition, StatusInsertFlyDuration)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            flyTween
                .TweenProperty(card, "scale", preview.Scale, StatusInsertFlyDuration)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            flyTween
                .TweenProperty(card, "modulate:a", 0.45f, StatusInsertFlyDuration / 3f)
                .SetDelay(delay + StatusInsertFlyDuration * 2f / 3f)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            flyTasks.Add(WaitForTweenFinishedAsync(flyTween));
        }

        await Task.WhenAll(flyTasks);

        var fadeTasks = new List<Task>();
        foreach (StatusInsertPreviewCard preview in cards)
        {
            SkillCard card = preview.Card;
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            Tween fadeTween = card.CreateTween();
            fadeTween.SetParallel(true);
            fadeTween.TweenProperty(card, "modulate:a", 0f, StatusInsertImpactFadeDuration);
            fadeTween
                .TweenProperty(card, "scale", preview.Scale * 0.08f, StatusInsertImpactFadeDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            fadeTasks.Add(WaitForTweenFinishedAsync(fadeTween));
        }

        await Task.WhenAll(fadeTasks);

        foreach (StatusInsertPreviewCard preview in cards)
            QueueFreeTemporaryCard(preview.Card);
    }

    private async Task WaitForTweenFinishedAsync(Tween tween)
    {
        if (tween == null || !GodotObject.IsInstanceValid(tween))
            return;

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private static Vector2 GetStatusInsertScale(int count, Vector2 viewportSize)
    {
        float scaleFactor = StatusInsertCardScale;
        while (scaleFactor > 0.46f)
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

        return BattleCardScale * Math.Max(0.46f, scaleFactor);
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
        return new Vector2(startX + column * (cardSize.X + gap), startY + row * (cardSize.Y + rowGap));
    }

    private static void AssignStatusInsertTargetOffsets(List<StatusInsertPreviewCard> cards)
    {
        foreach (var group in cards.GroupBy(card => card.Target))
        {
            int count = group.Count();
            int index = 0;
            foreach (StatusInsertPreviewCard card in group)
            {
                card.TargetIndex = index++;
                card.TargetCount = count;
            }
        }
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
        card.CharacterName.Text = target?.CharacterName ?? source?.CharacterName ?? "状态牌";
        card.HoverHint.Visible = false;
        return card;
    }

    private Vector2 GetStatusInsertTargetPosition(Character target, Vector2 cardSize)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (target == null || !GodotObject.IsInstanceValid(target))
            return viewportSize * 0.5f - cardSize * 0.5f;

        Vector2 targetScreenPosition = GetStatusInsertTargetScreenPosition(target);
        return targetScreenPosition - cardSize * 0.5f + new Vector2(0f, -24f);
    }

    private static Vector2 GetStatusInsertTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        Node2D anchor = target.Sprite != null && GodotObject.IsInstanceValid(target.Sprite)
            ? target.Sprite
            : target;
        return anchor.GetGlobalTransformWithCanvas().Origin;
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
            RefreshTurnUi();
            return;
        }

        _isResolvingCard = !enabled;
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
        return player != null
            && player == _activePlayer
            && _uiBuilt
            && IsInsideTree();
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

        bool playedAny = false;
        foreach (int index in indexes)
        {
            if (!IsCardIndexValid(index))
                continue;

            SkillCard card = _cards[index];
            if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
                continue;

            playedAny = true;
            card.Button.Disabled = true;
            card.PlayExhaustEffect(duration);
        }

        if (!playedAny)
            return;

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

        var cards = new List<SkillCard>(entries.Count);
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scale = GetStatusExhaustPreviewScale(entries.Count, viewportSize);
        Vector2 cardSize = BattleCardBaseSize * scale;
        float gap = Math.Max(10f, cardSize.X * 0.12f);
        int columns = Math.Min(entries.Count, Math.Max(1, Mathf.FloorToInt(viewportSize.X * 0.62f / (cardSize.X + gap))));
        int rows = Mathf.CeilToInt(entries.Count / (float)columns);
        float rowGap = Math.Max(10f, cardSize.Y * 0.08f);
        Vector2 start = viewportSize * 0.5f
            - new Vector2(
                columns * cardSize.X + Math.Max(0, columns - 1) * gap,
                rows * cardSize.Y + Math.Max(0, rows - 1) * rowGap
            ) * 0.5f;

        for (int i = 0; i < entries.Count; i++)
        {
            Skill statusSkill = Skill.GetSkill(entries[i].StatusSkillId);
            if (statusSkill == null)
                continue;

            SkillCard card = CreateStatusInsertPreviewCard(statusSkill, entries[i].Player, entries[i].Player, 1, scale);
            if (card == null)
                continue;

            overlay.AddChild(card);
            card.RestoreDisplayState();
            card.Name = "StatusExhaustCard";
            card.ZIndex = TemporaryCarryCardZIndex + i;
            card.Modulate = new Color(1f, 1f, 1f, 0f);
            int row = i / columns;
            int col = i % columns;
            card.GlobalPosition = start + new Vector2(col * (cardSize.X + gap), row * (cardSize.Y + rowGap));
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
            exhaustTween.TweenCallback(Callable.From(() => card.PlayExhaustEffect(duration))).SetDelay(delay + 0.08f);
        }

        await ToSignal(
            GetTree().CreateTimer(duration + 0.12f + stagger * Math.Max(0, cards.Count - 1)),
            SceneTreeTimer.SignalName.Timeout
        );

        foreach (SkillCard card in cards)
        {
            if (card != null && GodotObject.IsInstanceValid(card))
                card.QueueFree();
        }
    }

    private static Vector2 GetStatusExhaustPreviewScale(int count, Vector2 viewportSize)
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

    public void RefreshCurrentTurnUi()
    {
        RefreshTurnUi();
    }

    private bool TryBindActionAreaUiFromScene()
    {
        _root = GetNodeOrNull<VBoxContainer>("ActionAreaRoot");
        _statusLabel = GetNodeOrNull<Label>("ActionAreaRoot/StatusLabel");
        _cardRow = GetNodeOrNull<Control>("ActionAreaRoot/CardRow");
        Control actionButtonsRoot = GetActionButtonsRootFromScene();
        _endTurnButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("EndTurnButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/EndTurnButton")
            ?? GetNodeOrNull<Button>("EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/CardRow/EndTurnButton");
        _drawPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("DrawPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/DrawPileButton");
        _discardPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("DiscardPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/DiscardPileButton");
        _exhaustedPileButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("ExhaustedPileButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/ExhaustedPileButton");
        _manualTargetInfoButton =
            actionButtonsRoot?.GetNodeOrNull<Button>("ManualTargetInfoButton")
            ?? GetNodeOrNull<Button>("../BattleActionButtons/ManualTargetInfoButton");

        if (_root == null || _statusLabel == null)
            return false;

        if (_cardRow == null)
        {
            _cardRow = new Control
            {
                Name = "CardRow",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _root.AddChild(_cardRow);
        }

        _root.MouseFilter = MouseFilterEnum.Ignore;
        _statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        _cardRow.MouseFilter = MouseFilterEnum.Ignore;
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
                ?? cardSlot.GetChildren().OfType<SkillCard>().FirstOrDefault();
            if (card == null)
            {
                card = CreateBattleCard(i);
                cardSlot.AddChild(card);
            }

            _cardSlots[i] = cardSlot;
            cardSlot.CustomMinimumSize = BattleCardBaseSize * BattleCardScale;
            cardSlot.MouseFilter = MouseFilterEnum.Ignore;
            WireBattleCard(card, i);
            _cards[i] = card;
        }

        WireActionButtonsFromScene();
        LayoutActionCards(instant: true);
        return true;
    }

    private Control GetActionButtonsRootFromScene()
    {
        Node parent = GetParent();
        return parent?.GetNodeOrNull<Control>("BattleActionButtons");
    }

    private void WireActionButtonsFromScene()
    {
        if (_endTurnButton != null)
        {
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

        if (_manualTargetInfoButton != null)
        {
            ConfigureManualTargetInfoButton(_manualTargetInfoButton);
            _manualTargetInfoButton.Disabled = true;
            _manualTargetInfoButton.Pressed += OnManualTargetInfoPressed;
        }
    }

    private void WireBattleCard(SkillCard card, int index)
    {
        if (card == null)
            return;

        card.ConfigureDisplayScale(BattleCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.Button.Disabled = true;

        int capturedIndex = index;
        card.Button.Pressed += () => _ = HandleCardPressedAsync(capturedIndex);
        card.Button.MouseEntered += () =>
        {
            if (SetCardHovered(capturedIndex, true))
                SetCardHoverPreviewActive(capturedIndex, true);
        };
        card.Button.MouseExited += () =>
        {
            if (SetCardHovered(capturedIndex, false))
                SetCardHoverPreviewActive(capturedIndex, false);
        };
    }

    private void BuildActionAreaUi()
    {
        if (_uiBuilt)
            return;

        if (TryBindActionAreaUiFromScene())
        {
            _uiBuilt = true;
            return;
        }

        _root = new VBoxContainer
        {
            Name = "ActionAreaRoot",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _root.AddThemeConstantOverride("separation", 12);
        AddChild(_root);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = "等待行动",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 24);
        _root.AddChild(_statusLabel);

        _cardRow = new Control
        {
            Name = "CardRow",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _cardRow.Resized += LayoutActionCards;
        _root.AddChild(_cardRow);
        for (int i = 0; i < _cards.Length; i++)
        {
            Control cardSlot = CreateCardSlot(i);
            _cardRow.AddChild(cardSlot);
            _cardSlots[i] = cardSlot;

            SkillCard card = CreateBattleCard(i);
            cardSlot.AddChild(card);

            WireBattleCard(card, i);
            _cards[i] = card;
        }

        LayoutActionCards(instant: true);

        _uiBuilt = true;
    }

    private static Control CreateCardSlot(int index)
    {
        return new Control
        {
            Name = $"CardSlot{index}",
            CustomMinimumSize = BattleCardBaseSize * BattleCardScale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
    }

    private static SkillCard CreateBattleCard(int index)
    {
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"Card{index}";
        card.ConfigureDisplayScale(BattleCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.Button.Disabled = true;
        return card;
    }

    private static void ConfigureEndTurnButton(Button button)
    {
        if (button == null)
            return;

        button.Text = "结束回合";
        button.FocusMode = FocusModeEnum.None;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.AddThemeFontSizeOverride("font_size", 22);
        button.AddThemeColorOverride("font_color", new Color(0.96f, 0.98f, 1f, 1f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.95f, 0.78f, 1f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.68f, 0.74f, 0.78f, 0.62f));
        button.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.05f, 0.08f, 0.9f));
        button.AddThemeConstantOverride("outline_size", 3);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateEndTurnStyle(
                new Color(0.07f, 0.16f, 0.22f, 0.9f),
                new Color(0.35f, 0.82f, 0.92f, 0.88f)
            )
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateEndTurnStyle(
                new Color(0.10f, 0.24f, 0.30f, 0.95f),
                new Color(0.65f, 0.94f, 1f, 1f),
                3
            )
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateEndTurnStyle(
                new Color(0.04f, 0.11f, 0.16f, 0.98f),
                new Color(1f, 0.78f, 0.32f, 1f),
                3
            )
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateEndTurnStyle(
                new Color(0.05f, 0.08f, 0.1f, 0.58f),
                new Color(0.25f, 0.36f, 0.42f, 0.62f)
            )
        );
        button.AddThemeStyleboxOverride(
            "focus",
            CreateEndTurnStyle(
                new Color(0.10f, 0.24f, 0.30f, 0.95f),
                new Color(0.65f, 0.94f, 1f, 1f),
                3
            )
        );
    }

    private void ConfigurePileButton(Button button, string text)
    {
        if (button == null)
            return;

        button.Text = string.Empty;
        button.TooltipText = text;
        button.Flat = true;
        button.FocusMode = FocusModeEnum.None;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        button.AddThemeStyleboxOverride("normal", CreatePileButtonStyleBox());
        button.AddThemeStyleboxOverride("hover", CreatePileButtonStyleBox());
        button.AddThemeStyleboxOverride("pressed", CreatePileButtonStyleBox());
        button.AddThemeStyleboxOverride("disabled", CreatePileButtonStyleBox());
        button.AddThemeStyleboxOverride("focus", CreatePileButtonStyleBox());
        button.PivotOffset = button.Size / 2f;
        button.Resized += () =>
        {
            if (button == null || !GodotObject.IsInstanceValid(button))
                return;

            button.PivotOffset = button.Size / 2f;
        };
        button.MouseEntered += () => AnimatePileButtonHover(button, true);
        button.MouseExited += () => AnimatePileButtonHover(button, false);
        button.FocusEntered += () => AnimatePileButtonHover(button, true);
        button.FocusExited += () => AnimatePileButtonHover(button, false);
        button.ButtonDown += () => SetPileButtonPressedAmount(button, 1f, 0.08f);
        button.ButtonUp += () => SetPileButtonPressedAmount(button, 0f, 0.12f);
        button.VisibilityChanged += () =>
        {
            if (button == null || !GodotObject.IsInstanceValid(button))
                return;

            if (!button.Visible)
                AnimatePileButtonHover(button, false);
        };
        SyncPileButtonVisualState(button);
    }

    private static void ConfigureManualTargetInfoButton(Button button)
    {
        if (button == null)
            return;

        button.Text = string.Empty;
        button.TooltipText = "查看己方角色信息";
        button.FocusMode = FocusModeEnum.None;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;

        var transparent = new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0f) };
        var hover = new StyleBoxFlat
        {
            BgColor = new Color(0.35f, 0.82f, 0.92f, 0.08f),
            BorderColor = new Color(0.65f, 0.94f, 1f, 0.24f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
        };

        button.AddThemeStyleboxOverride("normal", transparent);
        button.AddThemeStyleboxOverride("pressed", transparent);
        button.AddThemeStyleboxOverride("disabled", transparent);
        button.AddThemeStyleboxOverride("focus", transparent);
        button.AddThemeStyleboxOverride("hover", hover);
    }

    private static StyleBoxFlat CreateEndTurnStyle(
        Color background,
        Color border,
        int borderWidth = 2
    )
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 18,
            ContentMarginTop = 12,
            ContentMarginRight = 18,
            ContentMarginBottom = 12,
        };
    }

    private static StyleBoxFlat CreatePileButtonStyleBox()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0f),
            BorderColor = new Color(1f, 1f, 1f, 0f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 0,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusBottomLeft = 8,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
            ExpandMarginLeft = 1,
            ExpandMarginTop = 1,
            ExpandMarginRight = 1,
            ExpandMarginBottom = 1,
        };
    }

    private void SyncPileButtonVisualState(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon) || icon.Material is not ShaderMaterial shader)
            return;

        shader.SetShaderParameter("disabled_amount", button.Disabled ? 1f : 0f);
        if (button.Disabled)
        {
            shader.SetShaderParameter("hover_amount", 0f);
            shader.SetShaderParameter("pressed_amount", 0f);
            icon.Scale = Vector2.One;
        }
    }

    private void AnimatePileButtonHover(Button button, bool hovered)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        if (button.Disabled)
            hovered = false;

        Tween hoverTween = GetPileButtonHoverTween(button);
        if (hoverTween != null && GodotObject.IsInstanceValid(hoverTween))
            hoverTween.Kill();

        Tween shaderTween = GetPileButtonShaderTween(button);
        if (shaderTween != null && GodotObject.IsInstanceValid(shaderTween))
            shaderTween.Kill();

        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        icon.PivotOffset = icon.Size / 2f;
        Vector2 targetScale = hovered ? new Vector2(1.10f, 1.10f) : Vector2.One;
        Tween newHoverTween = icon.CreateTween();
        newHoverTween
            .TweenProperty(icon, "scale", targetScale, hovered ? 0.18f : 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        SetPileButtonHoverTween(button, newHoverTween);

        if (icon.Material is not ShaderMaterial shader)
            return;

        float from = GetShaderParameterFloat(shader, "hover_amount");
        float to = hovered ? 1f : 0f;
        Tween newShaderTween = icon.CreateTween();
        newShaderTween
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("hover_amount", value);
                }),
                from,
                to,
                hovered ? 0.20f : 0.12f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(hovered ? Tween.EaseType.Out : Tween.EaseType.InOut);
        SetPileButtonShaderTween(button, newShaderTween);
    }

    private void SetPileButtonPressedAmount(Button button, float amount, float duration)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon) || icon.Material is not ShaderMaterial shader)
            return;

        float from = GetShaderParameterFloat(shader, "pressed_amount");
        icon.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("pressed_amount", value);
                }),
                from,
                amount,
                duration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private static float GetShaderParameterFloat(ShaderMaterial shader, string parameterName)
    {
        if (shader == null)
            return 0f;

        Variant value = shader.GetShaderParameter(parameterName);
        return value.VariantType switch
        {
            Variant.Type.Float => (float)value.AsDouble(),
            Variant.Type.Int => value.AsInt64(),
            _ => 0f,
        };
    }

    private static Control GetPileButtonIcon(Button button)
    {
        return button?.GetNodeOrNull<Control>("PileIcon");
    }

    private Tween GetPileButtonHoverTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileHoverTween;
        if (button == _discardPileButton)
            return _discardPileHoverTween;
        return _exhaustedPileHoverTween;
    }

    private void SetPileButtonHoverTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileHoverTween = tween;
        else if (button == _discardPileButton)
            _discardPileHoverTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileHoverTween = tween;
    }

    private Tween GetPileButtonShaderTween(Button button)
    {
        if (button == _drawPileButton)
            return _drawPileShaderTween;
        if (button == _discardPileButton)
            return _discardPileShaderTween;
        return _exhaustedPileShaderTween;
    }

    private void SetPileButtonShaderTween(Button button, Tween tween)
    {
        if (button == _drawPileButton)
            _drawPileShaderTween = tween;
        else if (button == _discardPileButton)
            _discardPileShaderTween = tween;
        else if (button == _exhaustedPileButton)
            _exhaustedPileShaderTween = tween;
    }

    private void LayoutActionCards()
    {
        LayoutActionCards(instant: false);
    }

    private void LayoutActionCards(bool instant)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        int[] visibleSlotIndexes = GetVisibleHandSlotIndexes(excludeLiftedCard: true);
        int handCount = visibleSlotIndexes.Length;
        float rowWidth = Math.Max(_cardRow.Size.X, 0f);
        float rowHeight = Math.Max(_cardRow.Size.Y, cardSize.Y);
        float cardGap = HandCardGap;
        float totalWidth = handCount * cardSize.X + Math.Max(0, handCount - 1) * cardGap;

        if (handCount > 1 && totalWidth > rowWidth)
        {
            float fixedWidth = handCount * cardSize.X;
            cardGap = Math.Max(4f, (rowWidth - fixedWidth) / (handCount - 1));
            totalWidth = fixedWidth + Math.Max(0, handCount - 1) * cardGap;
        }

        float x = Math.Max(0f, (rowWidth - totalWidth) / 2f);
        float cardY = Math.Max(0f, (rowHeight - cardSize.Y) / 2f);
        Vector2 hiddenPosition = new(-cardSize.X * 2f, cardY);
        var targetPositions = new Dictionary<int, Vector2>();
        foreach (int slotIndex in visibleSlotIndexes)
        {
            targetPositions[slotIndex] = new Vector2(x, cardY);
            x += cardSize.X + cardGap;
        }

        bool hasPendingDrawEntry =
            !instant && _layoutInitialized && _drawEntrySlotIndexes.Count > 0;
        float existingDrawShiftDelay = hasPendingDrawEntry
            ? GetLatestDrawEntryDelay() + HandDrawEntryTweenDuration * HandDrawExistingShiftDelayRatio
            : 0f;

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            slot.Size = cardSize;
            slot.CustomMinimumSize = cardSize;
            if (i == _liftedCardIndex)
            {
                _cardSlotLayoutTweens[i]?.Kill();
                _cardSlotLayoutTweens[i] = null;
                _cardSlotLayoutTargets[i] = null;
                continue;
            }

            Vector2 targetPosition = targetPositions.TryGetValue(i, out Vector2 visiblePosition)
                ? visiblePosition
                : hiddenPosition;
            bool drawEntry = _drawEntrySlotIndexes.Contains(i);
            MoveCardSlotTo(
                i,
                targetPosition,
                instant || !_layoutInitialized,
                drawEntry,
                !drawEntry
                    ? existingDrawShiftDelay
                        + GetExistingHandCardDrawShiftStagger(i, visibleSlotIndexes)
                    : 0f
            );
        }

        _layoutInitialized = true;
    }

    private void MoveCardSlotTo(
        int index,
        Vector2 targetPosition,
        bool instant,
        bool drawEntry = false,
        float layoutDelay = 0f
    )
    {
        if (index < 0 || index >= _cardSlots.Length)
            return;

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        if (instant || slot.Position.DistanceSquaredTo(targetPosition) < 0.25f)
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = targetPosition;
            _drawEntrySlotIndexes.Remove(index);
            _drawEntrySlotOrders.Remove(index);
            slot.Position = targetPosition;
            return;
        }

        if (
            _cardSlotLayoutTargets[index].HasValue
            && _cardSlotLayoutTargets[index].Value.DistanceSquaredTo(targetPosition) < 0.25f
            && _cardSlotLayoutTweens[index] != null
        )
        {
            return;
        }

        _cardSlotLayoutTweens[index]?.Kill();

        float duration = drawEntry ? HandDrawEntryTweenDuration : HandLayoutTweenDuration;
        float delay = drawEntry ? GetDrawEntryDelay(index) : 0f;
        delay = Math.Max(delay, layoutDelay);

        Tween tween = slot.CreateTween();
        _cardSlotLayoutTweens[index] = tween;
        _cardSlotLayoutTargets[index] = targetPosition;
        if (delay > 0f)
            tween.TweenInterval(delay);
        if (drawEntry)
        {
            tween.TweenCallback(
                Callable.From(() => StartDrawEntryTrail(index, targetPosition - slot.Position))
            );
        }
        tween
            .TweenProperty(slot, "position", targetPosition, duration)
            .SetTrans(drawEntry ? Tween.TransitionType.Cubic : Tween.TransitionType.Sine)
            .SetEase(drawEntry ? Tween.EaseType.Out : Tween.EaseType.InOut);
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (_cardSlotLayoutTweens[index] == tween)
                    _cardSlotLayoutTweens[index] = null;
                if (drawEntry && index >= 0 && index < _cards.Length)
                {
                    _cards[index]?.PlayDrawSettleEffect();
                    _ = FadeAndHideCardDrawEntryTrailAsync(_cards[index]);
                }
                _drawEntrySlotIndexes.Remove(index);
                _drawEntrySlotOrders.Remove(index);
            })
        );
    }

    private float GetDrawEntryDelay(int index)
    {
        return _drawEntrySlotOrders.TryGetValue(index, out int order)
            ? Math.Max(0, order) * HandDrawEntryStagger
            : 0f;
    }

    private float GetLatestDrawEntryDelay()
    {
        if (_drawEntrySlotOrders.Count == 0)
            return 0f;

        return Math.Max(0, _drawEntrySlotOrders.Values.Max()) * HandDrawEntryStagger;
    }

    private float GetExistingHandCardDrawShiftStagger(int index, int[] visibleSlotIndexes)
    {
        if (visibleSlotIndexes == null || visibleSlotIndexes.Length <= 1)
            return 0f;

        int rightToLeftOrder = 0;
        for (int i = visibleSlotIndexes.Length - 1; i >= 0; i--)
        {
            int slotIndex = visibleSlotIndexes[i];
            if (_drawEntrySlotIndexes.Contains(slotIndex))
                continue;

            if (slotIndex == index)
                return rightToLeftOrder * HandDrawExistingShiftStagger;

            rightToLeftOrder++;
        }

        return 0f;
    }

    private int[] GetVisibleHandSlotIndexes(bool excludeLiftedCard = false)
    {
        if (_activePlayer?.Skills == null)
            return Array.Empty<int>();

        int max = Math.Min(_activePlayer.Skills.Length, _cards.Length);
        var indexes = new List<int>(max);
        for (int i = 0; i < max; i++)
        {
            if (excludeLiftedCard && i == _liftedCardIndex)
                continue;
            if (_activePlayer.Skills[i] != null)
                indexes.Add(i);
        }

        return indexes.ToArray();
    }

    private void PrepareNewHandCardSlotForDrawEntry(int index)
    {
        if (
            index < 0
            || index >= _cardSlots.Length
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return;
        }

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        slot.Position = GetDrawPileCardStartPosition(cardSize, slot.Position.Y);
        _cardSlotLayoutTargets[index] = null;
        _drawEntrySlotIndexes.Add(index);
        _drawEntrySlotOrders[index] = _drawEntrySlotOrders.Count;
    }

    private Vector2 GetDrawPileCardStartPosition(Vector2 cardSize, float fallbackY)
    {
        if (
            _drawPileButton != null
            && GodotObject.IsInstanceValid(_drawPileButton)
            && _drawPileButton.IsInsideTree()
            && _cardRow != null
            && GodotObject.IsInstanceValid(_cardRow)
        )
        {
            Rect2 drawPileRect = _drawPileButton.GetGlobalRect();
            Vector2 drawPileCenter = drawPileRect.Position + drawPileRect.Size * 0.5f;
            Vector2 cardRowOrigin = _cardRow.GetGlobalRect().Position;
            return drawPileCenter - cardRowOrigin - cardSize * 0.5f;
        }

        return new Vector2(-cardSize.X * 0.85f, fallbackY);
    }

    private void StartDrawEntryTrail(int index, Vector2 velocity)
    {
        if (index < 0 || index >= _cards.Length)
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Node2D target = card.DrawTrailTarget;
        Line trail = card.DrawTrail;
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || trail == null
            || !GodotObject.IsInstanceValid(trail)
        )
        {
            return;
        }

        target.Visible = true;
        target.Position = BattleCardBaseSize * 0.5f;
        trail.Target = target;
        trail.ManualPreviewMode = false;
        trail.Visible = true;
        trail.GlobalPosition = Vector2.Zero;
        trail.Modulate = Colors.White;
        trail.ClearPoints();

        GpuParticles2D particles = card.DrawTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        UpdateTrailParticlesRotation(particles, velocity);
        particles.Emitting = true;
    }

    private async Task FadeAndHideCardDrawEntryTrailAsync(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Line trail = card.DrawTrail;
        GpuParticles2D particles = card.DrawTrailParticles;
        if (particles != null && GodotObject.IsInstanceValid(particles))
            particles.Emitting = false;

        if (trail == null || !GodotObject.IsInstanceValid(trail))
        {
            HideCardTrailParticles(particles);
            return;
        }

        trail.ManualPreviewMode = true;
        float startWidth = trail.Width;
        Tween tween = trail.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(fade =>
            {
                if (trail == null || !GodotObject.IsInstanceValid(trail))
                    return;

                trail.Modulate = new Color(1f, 1f, 1f, 1f - fade);
                trail.Width = Mathf.Lerp(startWidth, 0.5f, fade);
            }),
            0f,
            1f,
            HandDrawTrailFadeDuration
        );
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (trail != null && GodotObject.IsInstanceValid(trail))
                {
                    trail.Visible = false;
                    trail.ClearPoints();
                    trail.Modulate = Colors.White;
                    trail.Width = startWidth;
                    if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                        trail.Target.Visible = false;
                }

                HideCardTrailParticles(particles);
            })
        );

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void RefreshTurnUi()
    {
        if (!_uiBuilt)
            return;

        bool updateLayout = !_suppressNextRefreshLayout;
        _suppressNextRefreshLayout = false;
        Skill[] previousDisplayedSkills = _displayedSkills.ToArray();
        SkillID?[] previousDisplayedSkillIds = _displayedSkillIds.ToArray();
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || _activePlayer.State == Character.CharacterState.Dying
        )
        {
            _statusLabel.Text = "等待行动";
            ClearLiftedCard(instant: true);
            ClearCardQueue(resetCards: false);
            ResetCardDisplayTracking();
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null)
                    continue;

                ResetCardMotion(i, instant: true);
                _cards[i].Visible = false;
                _cards[i].Button.Disabled = true;
            }

            if (_endTurnButton != null)
                _endTurnButton.Disabled = true;
            if (_drawPileButton != null)
            {
                _drawPileButton.Text = string.Empty;
                _drawPileButton.TooltipText = "抽牌堆";
                _drawPileButton.Disabled = true;
                SyncPileButtonVisualState(_drawPileButton);
            }
            if (_discardPileButton != null)
            {
                _discardPileButton.Text = string.Empty;
                _discardPileButton.TooltipText = "弃牌堆";
                _discardPileButton.Disabled = true;
                SyncPileButtonVisualState(_discardPileButton);
            }
            if (_exhaustedPileButton != null)
            {
                _exhaustedPileButton.Text = string.Empty;
                _exhaustedPileButton.TooltipText = "消耗牌堆";
                _exhaustedPileButton.Disabled = true;
                SyncPileButtonVisualState(_exhaustedPileButton);
            }
            if (_manualTargetInfoButton != null)
                _manualTargetInfoButton.Disabled = true;
            return;
        }

        _statusLabel.Text =
            $"{_activePlayer.CharacterName} 回合中 | 当前能量 {_activePlayer.Energy}";

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null)
                continue;

            Skill skill = i < _activePlayer.Skills.Length ? _activePlayer.Skills[i] : null;
            if (skill == null)
            {
                card.Visible = false;
                card.Button.Disabled = true;
                ResetCardMotion(i, instant: true);
                ResetCardDisplayTracking(i);
                continue;
            }

            skill.OwnerCharater = _activePlayer;
            skill.UpdateDescription();

            bool isNewCardForHand =
                !_cardDisplayInitialized[i] || !ReferenceEquals(_displayedSkills[i], skill);
            bool movedFromAnotherSlot =
                isNewCardForHand
                && WasSkillAlreadyDisplayed(previousDisplayedSkills, skill);
            bool shouldAnimate = isNewCardForHand && !movedFromAnotherSlot;
            bool shouldResetDisplayState = !card.Visible || isNewCardForHand;

            card.Visible = true;
            if (shouldAnimate)
                PrepareNewHandCardSlotForDrawEntry(i);
            if (shouldResetDisplayState)
            {
                if (shouldAnimate)
                    card.ResetState();
                else
                    card.RestoreDisplayState();
            }

            card.SetSkill(skill);
            card.CharacterName.Text = _activePlayer.CharacterName;

            bool isCommitted = IsCardCommitted(i);
            if (isCommitted)
            {
                card.Visible = false;
                card.Button.Disabled = true;
                card.HoverHint.Visible = false;
                continue;
            }

            if (shouldAnimate)
            {
                float revealDelay = Math.Max(0f, GetDrawEntryDelay(i) - HandDrawRevealLeadTime);
                card.StartAnimationWithDuration(revealDelay, HandDrawRevealDuration);
            }

            _displayedSkills[i] = skill;
            _displayedSkillIds[i] = skill.SkillId;
            _cardDisplayInitialized[i] = true;

            bool isArrowSelectedCard =
                _manualTargetArrowSelectionActive && i == _manualTargetArrowCardIndex;
            bool canInteract =
                !_isResolvingCard
                && !_endTurnQueued
                && !_manualTargetInfoOpen
                && !IsManualTargetSelectionPending()
                && !isCommitted;
            bool canUse = canInteract && skill.CanUseCurrentEnergy();
            card.Button.Disabled = !canInteract || isArrowSelectedCard;
            card.Modulate =
                isCommitted ? QueuedCardModulate
                : _manualTargetArrowSelectionActive ? SkillButton.EnabledModulate
                : isArrowSelectedCard ? SkillButton.EnabledModulate
                : !skill.CanBePlayed ? SkillButton.EnabledModulate
                : canUse ? SkillButton.EnabledModulate
                : SkillButton.DisabledModulate;
            if (isArrowSelectedCard)
                ApplyManualTargetArrowCardVisual(card);
        }

        if (_endTurnButton != null)
            _endTurnButton.Disabled =
                _isResolvingCard
                || _endTurnQueued
                || _manualTargetInfoOpen
                || IsManualTargetSelectionPending();

        UpdatePileButtons();

        if (_manualTargetInfoButton != null)
            _manualTargetInfoButton.Disabled =
                _isResolvingCard
                || _endTurnQueued
                || _manualTargetInfoOpen
                || IsManualTargetSelectionPending();

        if (updateLayout)
            LayoutActionCards();
        if (
            _manualTargetArrowSelectionActive
            && IsCardIndexValid(_manualTargetArrowCardIndex)
        )
        {
            ApplyManualTargetArrowCardVisual(_cards[_manualTargetArrowCardIndex]);
        }
    }

    private static bool WasSkillAlreadyDisplayed(Skill[] previousDisplayedSkills, Skill skill)
    {
        return skill != null
            && previousDisplayedSkills != null
            && previousDisplayedSkills.Any(previousSkill => ReferenceEquals(previousSkill, skill));
    }

    private async Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (_suppressCardButtonPressUntilLeftRelease && !allowSuppressedPress)
            return;

        if (
            _isResolvingCard
            || _endTurnQueued
            || _manualTargetInfoOpen
            || IsManualTargetSelectionPending()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= _activePlayer.Skills.Length
        )
        {
            return;
        }

        Skill skill = _activePlayer.Skills[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return;

        if (_liftedCardIndex != -1 && _liftedCardIndex != index)
            return;

        if (_liftedCardIndex == index)
        {
            if (!IsMouseOutsideHandArea())
            {
                ClearLiftedCard(instant: false);
                return;
            }

            QueueCardPlay(index, skill);
            return;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: false);

        if (ShouldUseManualTargetArrowSelection(skill))
        {
            await SelectManualTargetFromHandAndQueueAsync(index, skill);
            return;
        }

        LiftCard(index);
    }

    private async Task SelectManualTargetFromHandAndQueueAsync(int index, Skill skill)
    {
        if (skill == null || !IsCardIndexValid(index) || IsCardCommitted(index))
            return;

        if (!HasManualFriendlyTargetCandidates(skill))
        {
            QueueCardPlay(index, skill);
            return;
        }

        skill.ClearManualFriendlyTarget();
        SkillCard sourceCard = _cards[index];
        Character target = await ShowManualTargetArrowPickerAsync(skill, sourceCard, index);
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            skill.ClearManualFriendlyTarget();
            RefreshTurnUi();
            return;
        }

        skill.SetManualFriendlyTarget(target);
        if (!skill.HasManualFriendlyTarget())
        {
            skill.ClearManualFriendlyTarget();
            RefreshTurnUi();
            return;
        }

        QueueCardPlay(index, skill, keepManualFriendlyTarget: true);
    }

    private void ClearCardButtonPressSuppression()
    {
        _suppressCardButtonPressUntilLeftRelease = false;
    }

    private void QueueCardPlay(int index, Skill skill, bool keepManualFriendlyTarget = false)
    {
        if (
            skill == null
            || _endTurnQueued
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
        )
        {
            return;
        }

        if (!keepManualFriendlyTarget)
            skill.ClearManualFriendlyTarget();

        bool hadStun = HasActiveStun(_activePlayer);
        if (!hadStun && !skill.TrySpendDisplayedEnergy())
        {
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        SkillCard card = _cards[index];
        SkillCard playedCard = CreateHandPlayCard(_activePlayer, skill, card);
        if (playedCard == null)
        {
            skill.RefundDisplayedEnergy();
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        if (_liftedCardIndex == index)
        {
            _liftedCardIndex = -1;
            SetProcess(false);
        }
        _hoveredCardIndex = -1;

        if (card != null)
        {
            card.Visible = false;
            card.Button.Disabled = true;
            card.HoverHint.Visible = false;
            ResetCardMotion(index, instant: true);
        }

        var play = new QueuedCardPlay
        {
            Actor = _activePlayer,
            Index = index,
            Skill = skill,
            SkillId = skill.SkillId,
            SkillType = skill.SkillType,
            HadStun = hadStun,
            Card = playedCard,
            IsHandCard = true,
            QueueFreeCardAfterVanish = true,
        };

        _queuedCardIndices.Add(index);
        _queuedCardPlays.Enqueue(play);
        BattleNode?.DiscardBattleSkill(_activePlayer, skill);
        ResolveHandSlotAfterQueuedPlay(play);

        if (playedCard != null)
        {
            playedCard.Button.Disabled = true;
            playedCard.HoverHint.Visible = false;
            playedCard.ZIndex = 50 + _queuedCardPlays.Count;
            playedCard.Modulate = QueuedCardModulate;
        }

        RefreshTurnUi();
        _ = ProcessCardQueueAsync();
    }

    public Task QueueCarryCardAsync(Character actor, Skill skill)
    {
        if (
            actor == null
            || !GodotObject.IsInstanceValid(actor)
            || actor.State == Character.CharacterState.Dying
            || skill == null
        )
        {
            return Task.CompletedTask;
        }

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        bool hadStun = HasActiveStun(actor);
        SkillCard card = CreateTemporaryCarryCard(actor, skill);
        if (card == null)
            return Task.CompletedTask;

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var play = new QueuedCardPlay
        {
            Actor = actor,
            Index = -1,
            Skill = skill,
            SkillId = skill.SkillId,
            SkillType = skill.SkillType,
            HadStun = hadStun,
            Card = card,
            IsTemporaryCard = true,
            FreeEnergyCost = true,
            ForceManualTargetCardPicker = true,
            Completion = completion,
        };

        bool shouldWaitForCompletion = !_isProcessingCardQueue;
        if (_isProcessingCardQueue)
            _queuedFollowUpCardPlays.Enqueue(play);
        else
            _queuedCardPlays.Enqueue(play);

        BattleNode?.DiscardBattleSkill(actor, skill);
        _ = ProcessCardQueueAsync();
        return shouldWaitForCompletion ? completion.Task : Task.CompletedTask;
    }

    public async Task PlayEchoCardAsync(Character actor, Skill skill)
    {
        if (
            actor == null
            || !GodotObject.IsInstanceValid(actor)
            || actor.State == Character.CharacterState.Dying
            || skill == null
        )
        {
            return;
        }

        SkillCard card = CreateTemporaryPlayCard(actor, skill, "回响", "EchoCard");
        if (card == null)
            return;

        var play = new QueuedCardPlay
        {
            Actor = actor,
            Skill = skill,
            Card = card,
            IsTemporaryCard = true,
            QueueFreeCardAfterVanish = true,
        };

        await ShowTemporaryCardAtCenterAsync(play, "回响");
        await PlayCardVanishAfterExecutionAsync(play);
        QueueFreeTemporaryCard(card);
    }

    private async Task ProcessCardQueueAsync()
    {
        if (_isProcessingCardQueue)
            return;

        _isProcessingCardQueue = true;
        try
        {
            while (_queuedCardPlays.Count > 0 || _queuedFollowUpCardPlays.Count > 0)
            {
                QueuedCardPlay play = DequeueNextCardPlay();
                if (play.IsHandCard)
                    _queuedCardIndices.Remove(play.Index);

                bool shouldStop = await ExecuteQueuedCardAsync(play);
                if (shouldStop)
                {
                    ClearCardQueue(resetCards: true);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"CharacterControl: card queue aborted: {ex}");
            HideManualTargetPicker();
            ClearCardQueue(resetCards: true);
        }
        finally
        {
            _isProcessingCardQueue = false;
            if (GodotObject.IsInstanceValid(this))
            {
                if (_endTurnQueued)
                    await ExecuteQueuedEndTurnAsync();
                else
                    RefreshTurnUi();
            }
        }
    }

    private QueuedCardPlay DequeueNextCardPlay()
    {
        if (_queuedFollowUpCardPlays.Count > 0)
            return _queuedFollowUpCardPlays.Dequeue();

        return _queuedCardPlays.Dequeue();
    }

    private async Task<bool> ExecuteQueuedCardAsync(QueuedCardPlay play)
    {
        if (
            play == null
            || play.Actor == null
            || !GodotObject.IsInstanceValid(play.Actor)
            || play.Skill == null
            || (play.IsHandCard && play.Actor != _activePlayer)
        )
        {
            play?.Skill?.RefundDisplayedEnergy();
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play?.IsHandCard == true;
        }

        if (play.IsTemporaryCard)
            await ShowTemporaryCarryCardAtCenterAsync(play);

        if (play.IsHandCard)
            RefreshTurnUi();

        if (!await SelectManualFriendlyTargetIfNeededAsync(play))
        {
            play.Skill.RefundDisplayedEnergy();
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play.IsHandCard;
        }

        if (play.FreeEnergyCost)
        {
            using (play.Skill.BeginEnergyCostWaiver())
            {
                await play.Skill.Effect();
            }
        }
        else
        {
            await play.Skill.Effect();
        }

        if (BattleNode?.ShouldAbortSkillResolution() == true)
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: true);
            return true;
        }

        if (!GodotObject.IsInstanceValid(this))
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return true;
        }

        if (play.IsHandCard && !IsHandPlayStillValid(play))
        {
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return true;
        }

        await PlayCardVanishAfterExecutionAsync(play);

        if (play.IsTemporaryCard)
        {
            CompleteQueuedPlay(play, succeeded: true);
            QueueFreeTemporaryCard(play.Card);
            return false;
        }

        QueueFreeQueuedPlayCard(play);

        if (play.Actor.State == Character.CharacterState.Dying)
        {
            _isResolvingCard = false;
            play.Actor.EndAction();
            CompleteQueuedPlay(play, succeeded: true);
            return true;
        }

        CompleteQueuedPlay(play, succeeded: true);
        return false;
    }

    private void ResolveHandSlotAfterQueuedPlay(QueuedCardPlay play)
    {
        if (play?.IsHandCard != true || play.Actor is not PlayerCharacter player)
            return;

        _queuedCardIndices.Remove(play.Index);
        player.RemoveBattleHandCardAt(play.Index);
        ResetCardDisplayTracking(play.Index);
    }

    private bool IsHandPlayStillValid(QueuedCardPlay play)
    {
        return play?.Actor != null
            && GodotObject.IsInstanceValid(play.Actor)
            && play.Actor.BattleNode != null
            && GodotObject.IsInstanceValid(play.Actor.BattleNode)
            && play.Actor == _activePlayer;
    }

    private void CompleteQueuedPlay(QueuedCardPlay play, bool succeeded)
    {
        play?.Completion?.TrySetResult(succeeded);
    }

    private async Task PlayCardVanishAfterExecutionAsync(QueuedCardPlay play)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
            return;

        card.Button.Disabled = true;
        if (play?.Skill?.ExhaustsAfterUse == true)
            card.PlayExhaustEffect(CardPlayVanishDuration);
        else if (play?.IsHandCard == true)
        {
            await PlayCardDiscardFlyAsync(card);
            return;
        }
        else
            card.PressEffect();
        await ToSignal(
            GetTree().CreateTimer(CardPlayVanishDuration),
            SceneTreeTimer.SignalName.Timeout
        );
    }

    private async Task PlayEndTurnHandDiscardAnimationsAsync(PlayerCharacter player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        var flyTasks = new List<Task>();
        bool playedFallbackVanish = false;

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard sourceCard = _cards[i];
            if (sourceCard == null || !GodotObject.IsInstanceValid(sourceCard) || !sourceCard.Visible)
                continue;

            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;

            Skill skill = sourceCard.CurrentSkill;
            if (skill == null && player.Skills != null && i < player.Skills.Length)
                skill = player.Skills[i];

            if (skill?.ExhaustsAtTurnEndInHand == true)
            {
                sourceCard.PlayExhaustEffect(EndTurnCardVanishDuration);
                playedFallbackVanish = true;
                continue;
            }

            SkillCard discardCard = CreateHandPlayCard(player, skill, sourceCard);
            if (discardCard == null)
            {
                sourceCard.Vanish();
                playedFallbackVanish = true;
                continue;
            }

            sourceCard.Visible = false;
            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;
            ResetCardMotion(i, instant: true);

            discardCard.ZIndex = PlayedCardZIndex + flyTasks.Count + 1;
            flyTasks.Add(PlayEndTurnDiscardCardAsync(discardCard));
        }

        if (flyTasks.Count > 0)
        {
            await Task.WhenAll(flyTasks);
            return;
        }

        if (playedFallbackVanish)
        {
            await ToSignal(
                GetTree().CreateTimer(EndTurnCardVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
        }
    }

    private async Task PlayEndTurnDiscardCardAsync(SkillCard card)
    {
        try
        {
            await PlayCardDiscardFlyAsync(card);
        }
        finally
        {
            QueueFreeTemporaryCard(card);
        }
    }

    private async Task PlayCardDiscardFlyAsync(SkillCard card)
    {
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || _discardPileButton == null
            || !GodotObject.IsInstanceValid(_discardPileButton)
            || !_discardPileButton.IsInsideTree()
        )
        {
            card?.PressEffect();
            await ToSignal(
                GetTree().CreateTimer(CardPlayVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
            return;
        }

        Rect2 startRect = card.GetGlobalRect();
        Rect2 discardRect = _discardPileButton.GetGlobalRect();
        Vector2 startCenter = startRect.Position + startRect.Size * 0.5f;
        Vector2 endCenter = discardRect.Position + discardRect.Size * 0.5f;
        if (startCenter.DistanceSquaredTo(endCenter) < 16f)
        {
            card.PressEffect();
            await ToSignal(
                GetTree().CreateTimer(CardPlayVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
            return;
        }

        Vector2 squareScale = GetCardDiscardSquareScale();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.PressEffectPartial(
            centerVanish: CardPlayDiscardCenterVanishBeforeFly,
            glowMultiplier: 1.35f,
            duration: CardPlayDiscardCompressDuration
        );

        Tween compressTween = card.CreateTween();
        compressTween.SetParallel(true);
        compressTween
            .TweenProperty(card, "scale", squareScale, CardPlayDiscardCompressDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        compressTween
            .TweenMethod(
                Callable.From<float>(_ =>
                {
                    if (card == null || !GodotObject.IsInstanceValid(card))
                        return;

                    SetCardPivotCenterAt(card, startCenter);
                }),
                0f,
                1f,
                CardPlayDiscardCompressDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        compressTween.SetParallel(false);
        await ToSignal(compressTween, Tween.SignalName.Finished);

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 flyStartCenter = startCenter;
        PrepareCardDiscardTrail(card, out Line trail, out GpuParticles2D particles);
        Vector2 targetScale = squareScale * CardPlayDiscardTargetScaleMultiplier;
        Vector2 control = GetRandomCardDiscardControlPoint(flyStartCenter, endCenter);
        Vector2 initialVelocity = GetQuadraticBezierVelocity(flyStartCenter, control, endCenter, 0.01f);
        card.Rotation = GetRotationWithTopFacingVelocity(initialVelocity);
        UpdateTrailParticlesRotation(particles, initialVelocity);

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenMethod(
                Callable.From<float>(t =>
                {
                    if (card == null || !GodotObject.IsInstanceValid(card))
                        return;

                    Vector2 center = QuadraticBezier(flyStartCenter, control, endCenter, t);
                    Vector2 velocity = GetQuadraticBezierVelocity(
                        flyStartCenter,
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
                CardPlayDiscardFlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween
            .TweenProperty(card, "scale", targetScale, CardPlayDiscardFlyDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.SetParallel(false);

        await ToSignal(tween, Tween.SignalName.Finished);
        await FadeAndHideCardDiscardTrailAsync(trail, particles);
    }

    private static void SetCardPivotCenterAt(SkillCard card, Vector2 center)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.GlobalPosition = center - card.PivotOffset;
    }

    private static Vector2 GetCardDiscardSquareScale()
    {
        return new Vector2(
            CardPlayDiscardSquareSize / BattleCardBaseSize.X,
            CardPlayDiscardSquareSize / BattleCardBaseSize.Y
        );
    }

    private static void PrepareCardDiscardTrail(
        SkillCard card,
        out Line trail,
        out GpuParticles2D particles
    )
    {
        trail = null;
        particles = null;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Node2D target = card.DiscardTrailTarget;
        trail = card.DiscardTrail;
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || trail == null
            || !GodotObject.IsInstanceValid(trail)
        )
        {
            return;
        }

        target.Visible = true;
        target.Position = card.PivotOffset;
        trail.Target = target;
        trail.ManualPreviewMode = false;
        trail.Visible = true;
        trail.GlobalPosition = Vector2.Zero;
        trail.Modulate = Colors.White;
        trail.ClearPoints();

        particles = card.DiscardTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        particles.Emitting = true;
    }

    private static void UpdateTrailParticlesRotation(GpuParticles2D particles, Vector2 velocity)
    {
        if (
            particles == null
            || !GodotObject.IsInstanceValid(particles)
            || velocity.LengthSquared() < 0.001f
        )
        {
            return;
        }

        particles.GlobalRotation = velocity.Angle() + Mathf.Pi;
    }

    private async Task FadeAndHideCardDiscardTrailAsync(Line trail, GpuParticles2D particles)
    {
        if (particles != null && GodotObject.IsInstanceValid(particles))
            particles.Emitting = false;

        if (trail == null || !GodotObject.IsInstanceValid(trail))
        {
            HideCardTrailParticles(particles);
            return;
        }

        trail.ManualPreviewMode = true;
        float startWidth = trail.Width;
        Tween tween = trail.CreateTween();
        tween.TweenMethod(
            Callable.From<float>(fade =>
            {
                if (trail == null || !GodotObject.IsInstanceValid(trail))
                    return;

                trail.Modulate = new Color(1f, 1f, 1f, 1f - fade);
                trail.Width = Mathf.Lerp(startWidth, 0.5f, fade);
            }),
            0f,
            1f,
            CardPlayDiscardTrailFadeDuration
        );
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (trail != null && GodotObject.IsInstanceValid(trail))
                {
                    trail.Visible = false;
                    trail.ClearPoints();
                    trail.Modulate = Colors.White;
                    trail.Width = startWidth;
                    if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                        trail.Target.Visible = false;
                }

                HideCardTrailParticles(particles);
            })
        );

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private static void HideCardTrailParticles(GpuParticles2D particles)
    {
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Emitting = false;
        particles.Visible = false;
        particles.Modulate = Colors.White;
    }

    private static Vector2 GetRandomCardDiscardControlPoint(Vector2 start, Vector2 end)
    {
        Vector2 mid = (start + end) * 0.5f;
        float distance = start.DistanceTo(end);
        float lift =
            Math.Min(330f, Math.Max(120f, distance * 0.28f))
            + (float)GD.RandRange(-28f, 52f);
        float side = end.X >= start.X ? 1f : -1f;
        float sideOffset =
            side * Math.Min(190f, distance * 0.16f)
            + (float)GD.RandRange(-90f, 90f);
        return mid + new Vector2(sideOffset, -lift);
    }

    private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        Vector2 a = start.Lerp(control, t);
        Vector2 b = control.Lerp(end, t);
        return a.Lerp(b, t);
    }

    private static Vector2 GetQuadraticBezierVelocity(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return 2f * ((1f - t) * (control - start) + t * (end - control));
    }

    private static float GetRotationWithTopFacingVelocity(Vector2 velocity)
    {
        if (velocity.LengthSquared() < 0.001f)
            return 0f;

        return velocity.Angle() + Mathf.Pi * 0.5f;
    }

    private SkillCard CreateHandPlayCard(Character actor, Skill skill, SkillCard sourceCard)
    {
        if (
            actor == null
            || skill == null
            || sourceCard == null
            || !GodotObject.IsInstanceValid(sourceCard)
        )
        {
            return null;
        }

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return null;

        Vector2 sourceGlobalPosition = sourceCard.GlobalPosition;
        Vector2 sourceScale = sourceCard.Scale;

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "PlayedHandCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.ConfigureDisplayScale(BattleCardScale);
        overlay.AddChild(card);
        card.RestoreDisplayState();
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.SetSkill(skill);
        card.CharacterName.Text = actor.CharacterName;
        card.HoverHint.Visible = false;
        card.Scale = sourceScale;
        card.GlobalPosition = sourceGlobalPosition;
        card.ZIndex = PlayedCardZIndex;
        return card;
    }

    private SkillCard CreateTemporaryCarryCard(Character actor, Skill skill)
    {
        return CreateTemporaryPlayCard(actor, skill, "连携", "CarryCard");
    }

    private SkillCard CreateTemporaryPlayCard(
        Character actor,
        Skill skill,
        string tag,
        string nodeName
    )
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return null;

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = nodeName;
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.ConfigureDisplayScale(PlayedCardScale);
        overlay.AddChild(card);
        card.Visible = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.SetSkill(skill);
        card.SetEnergyCostCostText("0");
        card.CharacterName.Text = $"{actor.CharacterName} | {tag}";
        return card;
    }

    private CanvasLayer EnsureCardPlayOverlay()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        const string overlayName = "CardPlayOverlay";
        CanvasLayer overlay = root.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null)
        {
            overlay.Layer = CardPlayOverlayLayer;
            return overlay;
        }

        overlay = new CanvasLayer { Name = overlayName, Layer = CardPlayOverlayLayer };
        root.AddChild(overlay);
        return overlay;
    }

    private async Task ShowTemporaryCarryCardAtCenterAsync(QueuedCardPlay play)
    {
        await ShowTemporaryCardAtCenterAsync(play, "连携");
    }

    private async Task ShowTemporaryCardAtCenterAsync(QueuedCardPlay play, string tag)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Visible = true;
        card.ResetState();
        card.SetSkill(play.Skill);
        card.SetEnergyCostCostText("0");
        card.CharacterName.Text = $"{play.Actor.CharacterName} | {tag}";
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = TemporaryCarryCardZIndex;
        card.Scale = PlayedCardScale * CarryCardSpawnScaleMultiplier;
        card.GlobalPosition = GetScreenCenterCardPosition(card.Scale);
        card.StartAnimation();

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "scale", PlayedCardScale, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                card,
                "global_position",
                GetScreenCenterCardPosition(PlayedCardScale),
                CardPlayMoveDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private Vector2 GetScreenCenterCardPosition(Vector2 scale)
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scaledSize = new(BattleCardBaseSize.X * scale.X, BattleCardBaseSize.Y * scale.Y);
        return viewportSize / 2f - scaledSize / 2f;
    }

    private static void QueueFreeTemporaryCard(SkillCard card)
    {
        if (card != null && GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    private static void QueueFreeQueuedPlayCard(QueuedCardPlay play)
    {
        if (play?.IsTemporaryCard == true || play?.QueueFreeCardAfterVanish == true)
            QueueFreeTemporaryCard(play.Card);
    }

    private void LiftCard(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || card.Button.Disabled)
            return;

        _liftedCardIndex = index;
        _liftedCardMouseOffset = GetViewport().GetMousePosition() - card.GlobalPosition;

        if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            ResetCardMotion(_hoveredCardIndex, instant: false);
        _hoveredCardIndex = -1;

        card.StopBattleMotion();
        card.HoverHint.Visible = true;
        card.ZIndex = PlayedCardZIndex;
        SetProcess(true);
        LayoutActionCards(instant: false);
        UpdateLiftedCardPosition();
    }

    private void ClearLiftedCard(bool instant)
    {
        if (_liftedCardIndex == -1)
        {
            SetProcess(false);
            return;
        }

        int index = _liftedCardIndex;
        Skill skill =
            _activePlayer?.Skills != null && index < _activePlayer.Skills.Length
                ? _activePlayer.Skills[index]
                : null;
        skill?.ClearManualFriendlyTarget();
        SyncLiftedSlotPositionToCard(index);
        _liftedCardIndex = -1;
        SetProcess(false);
        ResetCardMotion(index, instant);
        LayoutActionCards(instant);
    }

    private void SyncLiftedSlotPositionToCard(int index)
    {
        if (!IsCardIndexValid(index) || _cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        Control slot = _cardSlots[index];
        SkillCard card = _cards[index];
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
        card.Position = Vector2.Zero;
    }

    private async Task<bool> SelectManualFriendlyTargetIfNeededAsync(QueuedCardPlay play)
    {
        if (play?.Skill?.RequiresManualFriendlyTarget() != true)
            return true;

        if (play.Skill.HasManualFriendlyTarget())
            return true;

        if (!HasManualFriendlyTargetCandidates(play.Skill))
            return true;

        Character target = !play.ForceManualTargetCardPicker
            && ShouldUseManualTargetArrowSelection(play.Skill)
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
            && skill?.RequiresManualFriendlyTarget() == true
            && skill?.ManualFriendlyTargetAllowsDying() == false;
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
        _manualTargetPickerMask.GuiInput += OnManualTargetPickerMaskGuiInput;
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

    private void OnManualTargetInfoPressed()
    {
        if (!CanOpenManualTargetInfoPicker())
            return;

        _ = ShowManualTargetInfoPickerAsync();
    }

    private bool CanOpenManualTargetInfoPicker()
    {
        return _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying
            && !_isResolvingCard
            && !_endTurnQueued
            && !_manualTargetInfoOpen
            && !IsManualTargetSelectionPending()
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode);
    }

    private async Task ShowManualTargetInfoPickerAsync()
    {
        if (!CanOpenManualTargetInfoPicker())
            return;

        EnsureManualTargetPickerUi();
        if (_manualTargetPickerRoot == null || _manualTargetPickerRow == null)
            return;

        ClearManualTargetCards();
        ClearLiftedCard(instant: false);
        _manualTargetPickerTemporarilyHidden = false;
        _manualTargetInfoOpen = true;
        RefreshTurnUi();
        _statusLabel.Text = "查看己方角色信息";

        Character[] targets = BattleNode
            .GetTeamCharacters(_activePlayer.IsPlayer, includeSummons: true)
            .Where(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && character.State != Character.CharacterState.Dying
            )
            .OrderBy(character => character.PositionIndex)
            .ToArray();

        AddManualTargetCards(targets, selectable: false, onSelected: null);
        ShowManualTargetPickerRoot(targets.Length);
        if (targets.Length == 0)
        {
            HideManualTargetPicker();
            return;
        }

        try
        {
            while (_manualTargetInfoOpen && IsManualTargetInfoContextValid())
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        finally
        {
            HideManualTargetPicker();
        }
    }

    private void EnsureManualTargetArrowUi()
    {
        if (_manualTargetArrowRoot != null && GodotObject.IsInstanceValid(_manualTargetArrowRoot))
            return;

        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return;

        _manualTargetArrowRoot = new Control
        {
            Name = "ManualTargetArrowPicker",
            Visible = false,
            ZIndex = ManualTargetPickerZIndex + 1,
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

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        _manualTargetArrowTargets = targets;
        _manualTargetArrowOwner = owner;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = skill;
        _manualTargetArrowCardIndex = sourceCardIndex;
        _manualTargetArrowSelectionActive = true;
        _manualTargetPickerPlayedCard = playedCard;
        _manualTargetArrowRoot.Visible = true;
        if (_manualTargetArrowMask != null)
            _manualTargetArrowMask.MouseFilter = MouseFilterEnum.Ignore;
        _statusLabel.Text = "选择一名己方角色";
        RefreshTurnUi();
        if (sourceCardIndex >= 0 && IsCardIndexValid(sourceCardIndex))
            ApplyManualTargetArrowCardVisual(_cards[sourceCardIndex]);
        RefreshManualTargetArrowPreviews(null);

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

        Character owner = skill.OwnerCharater;
        Character[] targets = GetManualFriendlyTargetCandidates(skill);

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
        bool canToggle = rootVisible && IsManualTargetSelectionPending() && !_manualTargetInfoOpen;
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
        return IsInsideTree()
            && _manualTargetArrowRoot != null
            && GodotObject.IsInstanceValid(_manualTargetArrowRoot)
            && _manualTargetArrowRoot.Visible
            && _manualTargetArrowSelectionActive
            && skill != null
            && owner != null
            && GodotObject.IsInstanceValid(owner)
            && owner.State != Character.CharacterState.Dying
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
        _manualTargetArrowLayer.SetEndpoints(
            GetManualTargetArrowStartPosition(owner, playedCard),
            mousePosition
        );
    }

    public void NotifyManualTargetHover(Character target, bool hovered)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return;

        Character nextHoveredTarget = hovered ? target : null;
        if (!hovered && _manualTargetArrowHoveredTarget != target)
            return;

        if (_manualTargetArrowHoveredTarget == nextHoveredTarget)
            return;

        _manualTargetArrowHoveredTarget = nextHoveredTarget;
        RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
        RefreshManualTargetArrowEffectPreview(_manualTargetArrowHoveredTarget);
    }

    public bool TrySelectManualTargetFromCharacter(Character target)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return false;

        _manualTargetArrowHoveredTarget = target;
        RefreshManualTargetArrowPreviews(target);
        RefreshManualTargetArrowEffectPreview(target);
        _manualTargetCompletion?.TrySetResult(target);
        return true;
    }

    private bool IsManualTargetArrowCandidate(Character target)
    {
        return target != null
            && GodotObject.IsInstanceValid(target)
            && (_manualTargetArrowTargets ?? Array.Empty<Character>()).Contains(target);
    }

    private static void ApplyManualTargetArrowCardVisual(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.StopBattleMotion();
        card.HoverHint.Visible = true;
        card.ZIndex = 12;
        card.Modulate = SkillButton.EnabledModulate;
        card.TweenBattleMotion(
            new Vector2(0f, CardHoverLiftY),
            BattleCardScale * CardHoverScaleMultiplier,
            0.08f
        );
    }

    private static Vector2 GetManualTargetArrowStartPosition(Character owner, SkillCard playedCard)
    {
        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
            return playedCard.GetGlobalRect().GetCenter();

        if (owner != null && GodotObject.IsInstanceValid(owner))
            return owner.GetGlobalTransformWithCanvas().Origin;

        return Vector2.Zero;
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
        if (
            !_manualTargetArrowSelectionActive
            || _manualTargetArrowSkill == null
            || hoveredTarget == null
            || !GodotObject.IsInstanceValid(hoveredTarget)
        )
        {
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
            return;
        }

        _manualTargetArrowSkill.SetManualFriendlyTarget(hoveredTarget);
        if (!_manualTargetArrowSkill.HasManualFriendlyTarget())
            return;

        var entries = _manualTargetArrowSkill.GetPreviewEffectEntries();
        if (entries == null || entries.Length == 0)
            return;

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

        Node2D anchor = target.Sprite != null && GodotObject.IsInstanceValid(target.Sprite)
            ? target.Sprite
            : target;
        return anchor.GetGlobalTransformWithCanvas().Origin;
    }

    private bool IsManualTargetInfoContextValid()
    {
        return IsInsideTree()
            && _manualTargetPickerRoot != null
            && GodotObject.IsInstanceValid(_manualTargetPickerRoot)
            && _manualTargetPickerRoot.Visible
            && _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode)
            && BattleNode.ShouldAbortSkillResolution() != true;
    }

    private void OnManualTargetPickerMaskGuiInput(InputEvent @event)
    {
        if (!_manualTargetInfoOpen)
            return;

        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
        {
            HideManualTargetPicker();
            GetViewport().SetInputAsHandled();
        }
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

    private void HideManualTargetPicker()
    {
        bool wasInfoOpen = _manualTargetInfoOpen;
        bool wasTargetSelectionPending = IsManualTargetSelectionPending();
        _manualTargetInfoOpen = false;
        bool wasArrowSelectionActive = _manualTargetArrowSelectionActive;
        int arrowCardIndex = _manualTargetArrowCardIndex;
        _manualTargetArrowSelectionActive = false;
        _manualTargetCompletion?.TrySetResult(null);
        _manualTargetCompletion = null;
        _manualTargetPickerTemporarilyHidden = false;
        HideManualTargetArrowPreviews();
        HideManualTargetArrowEffectPreview();
        if (wasArrowSelectionActive)
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
        _manualTargetArrowTargets = Array.Empty<Character>();
        _manualTargetArrowOwner = null;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = null;
        _manualTargetArrowCardIndex = -1;
        if (
            _manualTargetArrowRoot != null
            && GodotObject.IsInstanceValid(_manualTargetArrowRoot)
        )
        {
            _manualTargetArrowRoot.Visible = false;
        }
        if (
            _manualTargetArrowLayer != null
            && GodotObject.IsInstanceValid(_manualTargetArrowLayer)
        )
        {
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
        if (wasArrowSelectionActive)
            ResetManualTargetArrowCardVisual(arrowCardIndex);

        if (
            _manualTargetPickerRoot == null
            || !GodotObject.IsInstanceValid(_manualTargetPickerRoot)
        )
        {
            if (wasInfoOpen || wasTargetSelectionPending || wasArrowSelectionActive)
                RefreshTurnUi();
            return;
        }

        _manualTargetPickerRoot.Visible = false;
        ApplyManualTargetPickerTemporaryHiddenState();
        ClearManualTargetCards();
        if (wasInfoOpen || wasTargetSelectionPending || wasArrowSelectionActive)
            RefreshTurnUi();
    }

    private void ResetManualTargetArrowCardVisual(int index)
    {
        if (!IsCardIndexValid(index) || IsCardCommitted(index))
            return;

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

    private void UpdateLiftedCardPosition()
    {
        if (_liftedCardIndex == -1)
        {
            SetProcess(false);
            return;
        }

        if (!IsCardIndexValid(_liftedCardIndex))
        {
            ClearLiftedCard(instant: true);
            return;
        }

        SkillCard card = _cards[_liftedCardIndex];
        if (card == null || !card.Visible || card.Button.Disabled)
        {
            ClearLiftedCard(instant: true);
            return;
        }

        card.GlobalPosition = GetViewport().GetMousePosition() - _liftedCardMouseOffset;
        card.ZIndex = PlayedCardZIndex;
    }

    private bool SetCardHovered(int index, bool hovered)
    {
        if (!IsCardIndexValid(index) || _liftedCardIndex != -1)
            return false;

        SkillCard card = _cards[index];
        if (card == null || !card.Visible)
            return false;

        if (_manualTargetArrowSelectionActive && index == _manualTargetArrowCardIndex)
        {
            ApplyManualTargetArrowCardVisual(card);
            return false;
        }

        if (hovered)
        {
            if (_isResolvingCard || IsCardCommitted(index))
                return false;

            if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
                ResetCardMotion(_hoveredCardIndex, instant: false);

            _hoveredCardIndex = index;
            card.HoverHint.Visible = true;
            card.ZIndex = 10;
            card.TweenBattleMotion(
                new Vector2(0f, CardHoverLiftY),
                BattleCardScale * CardHoverScaleMultiplier
            );
            return true;
        }

        if (_hoveredCardIndex != index)
            return false;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ResetCardMotion(index, instant: false);
        return true;
    }

    private void SetCardHoverPreviewActive(int index, bool active)
    {
        if (!IsCardIndexValid(index) || _cardHoverPreviewActive[index] == active)
            return;

        _cardHoverPreviewActive[index] = active;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (active)
            card.ShowSkillPreview();
        else
            card.HideSkillPreview();
    }

    private void ResetCardMotion(int index, bool instant)
    {
        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null)
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;
        if (_liftedCardIndex == index)
            _liftedCardIndex = -1;

        SetCardHoverPreviewActive(index, false);
        card.HoverHint.Visible = false;
        card.ZIndex = 0;
        card.TweenBattleMotion(Vector2.Zero, BattleCardScale, instant ? 0f : 0.16f, instant);
    }

    private void ClearCardQueue(bool resetCards)
    {
        HideManualTargetPicker();
        if (
            _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
            && _queuedCardIndices.Count == 0
        )
            return;

        foreach (QueuedCardPlay play in _queuedCardPlays.ToArray())
        {
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            if (resetCards && play != null && play.IsHandCard)
                ResetCardMotion(play.Index, instant: true);
        }

        _queuedCardPlays.Clear();
        foreach (QueuedCardPlay play in _queuedFollowUpCardPlays.ToArray())
        {
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
        }
        _queuedFollowUpCardPlays.Clear();

        foreach (int index in _queuedCardIndices.ToArray())
        {
            if (resetCards)
                ResetCardMotion(index, instant: true);
        }
        _queuedCardIndices.Clear();
    }

    private bool IsCardCommitted(int index)
    {
        return _queuedCardIndices.Contains(index);
    }

    private bool IsMouseOutsideHandArea()
    {
        Rect2? handRect = null;
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            Rect2 slotRect = slot.GetGlobalRect();
            handRect = handRect.HasValue ? MergeRect(handRect.Value, slotRect) : slotRect;
        }

        if (!handRect.HasValue)
        {
            if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
                return true;

            handRect = _cardRow.GetGlobalRect();
        }

        return !handRect.Value.Grow(HandAreaPadding).HasPoint(GetViewport().GetMousePosition());
    }

    private static Rect2 MergeRect(Rect2 a, Rect2 b)
    {
        Vector2 start = new(
            Mathf.Min(a.Position.X, b.Position.X),
            Mathf.Min(a.Position.Y, b.Position.Y)
        );
        Vector2 end = new(Mathf.Max(a.End.X, b.End.X), Mathf.Max(a.End.Y, b.End.Y));
        return new Rect2(start, end - start);
    }

    private bool IsCardIndexValid(int index)
    {
        return index >= 0 && index < _cards.Length;
    }

    private void RefreshSkillOwners(PlayerCharacter player)
    {
        if (player?.Skills == null)
            return;

        for (int i = 0; i < player.Skills.Length; i++)
        {
            if (player.Skills[i] == null)
                continue;

            player.Skills[i].OwnerCharater = player;
            player.Skills[i].UpdateDescription();
        }
    }

    private static bool HasActiveStun(Character character)
    {
        return character?.SkillBuffs?.Any(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Stun && x.Stack > 0
            ) == true;
    }

    private void ResetCardDisplayTracking()
    {
        for (int i = 0; i < _displayedSkillIds.Length; i++)
        {
            _displayedSkills[i] = null;
            _displayedSkillIds[i] = null;
            _cardDisplayInitialized[i] = false;
        }
    }

    private void ResetCardDisplayTracking(int index)
    {
        if (index < 0 || index >= _displayedSkillIds.Length)
            return;

        _displayedSkills[index] = null;
        _displayedSkillIds[index] = null;
        _cardDisplayInitialized[index] = false;
    }

    private void UpdatePileButtons()
    {
        int drawCount = 0;
        int discardCount = 0;
        int exhaustedCount = 0;
        bool canOpenPile =
            _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying
            && !_isResolvingCard
            && !_endTurnQueued
            && !_manualTargetInfoOpen
            && !IsManualTargetSelectionPending()
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode);

        if (canOpenPile)
        {
            drawCount = BattleNode.GetDrawBattleCardPile(_activePlayer).Length;
            discardCount = BattleNode.GetDiscardBattleCardPile(_activePlayer).Length;
            exhaustedCount = BattleNode.GetExhaustedBattleCardPile(_activePlayer).Length;
        }

        if (_drawPileButton != null)
        {
            _drawPileButton.Text = string.Empty;
            _drawPileButton.TooltipText = $"抽牌堆 {drawCount}";
            _drawPileButton.Disabled = !canOpenPile;
            SyncPileButtonVisualState(_drawPileButton);
        }

        if (_discardPileButton != null)
        {
            _discardPileButton.Text = string.Empty;
            _discardPileButton.TooltipText = $"弃牌堆 {discardCount}";
            _discardPileButton.Disabled = !canOpenPile;
            SyncPileButtonVisualState(_discardPileButton);
        }

        if (_exhaustedPileButton != null)
        {
            _exhaustedPileButton.Text = string.Empty;
            _exhaustedPileButton.TooltipText = $"消耗牌堆 {exhaustedCount}";
            _exhaustedPileButton.Disabled = !canOpenPile;
            SyncPileButtonVisualState(_exhaustedPileButton);
        }
    }

    private void OnDrawPilePressed()
    {
        ShowCurrentPlayerPile(BattlePileKind.Draw);
    }

    private void OnDiscardPilePressed()
    {
        ShowCurrentPlayerPile(BattlePileKind.Discard);
    }

    private void OnExhaustedPilePressed()
    {
        ShowCurrentPlayerPile(BattlePileKind.Exhausted);
    }

    private enum BattlePileKind
    {
        Draw,
        Discard,
        Exhausted,
    }

    private void ShowCurrentPlayerPile(BattlePileKind kind)
    {
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || _activePlayer.State == Character.CharacterState.Dying
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || _isResolvingCard
            || _endTurnQueued
            || _manualTargetInfoOpen
            || IsManualTargetSelectionPending()
        )
        {
            return;
        }

        SkillID[] pile = kind switch
        {
            BattlePileKind.Draw => BattleNode.GetDrawBattleCardPile(_activePlayer),
            BattlePileKind.Discard => BattleNode.GetDiscardBattleCardPile(_activePlayer),
            BattlePileKind.Exhausted => BattleNode.GetExhaustedBattleCardPile(_activePlayer),
            _ => Array.Empty<SkillID>(),
        };
        ShowPileOverlay(
            _activePlayer,
            new[]
            {
                new BattlePileOverlaySection(
                    kind,
                    GetPileTitle(kind),
                    pile
                ),
            }
        );
    }

    public bool ShowPlayerBattleCardPiles(PlayerCharacter player)
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || player.State == Character.CharacterState.Dying
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || _manualTargetInfoOpen
            || IsManualTargetSelectionPending()
        )
        {
            return false;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: false);

        ShowPileOverlay(
            player,
            new[]
            {
                new BattlePileOverlaySection(
                    BattlePileKind.Draw,
                    GetPileTitle(BattlePileKind.Draw),
                    BattleNode.GetDrawBattleCardPile(player)
                ),
                new BattlePileOverlaySection(
                    BattlePileKind.Discard,
                    GetPileTitle(BattlePileKind.Discard),
                    BattleNode.GetDiscardBattleCardPile(player)
                ),
                new BattlePileOverlaySection(
                    BattlePileKind.Exhausted,
                    GetPileTitle(BattlePileKind.Exhausted),
                    BattleNode.GetExhaustedBattleCardPile(player)
                ),
            }
        );
        return true;
    }

    private static string GetPileTitle(BattlePileKind kind)
    {
        return kind switch
        {
            BattlePileKind.Draw => "抽牌堆",
            BattlePileKind.Discard => "弃牌堆",
            BattlePileKind.Exhausted => "消耗牌堆",
            _ => "牌堆",
        };
    }

    private void ShowPileOverlay(PlayerCharacter player, IReadOnlyList<BattlePileOverlaySection> sections)
    {
        EnsurePileOverlayUi();
        ClearPileOverlayCards();

        if (_pileOverlayRoot == null || _pileOverlaySections == null)
            return;

        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Stop;
        _pileOverlayRoot.MoveToFront();

        var cardsToAnimate = new List<SkillCard>();
        foreach (BattlePileOverlaySection section in sections ?? Array.Empty<BattlePileOverlaySection>())
        {
            AddPileOverlaySection(_pileOverlaySections, player, section, cardsToAnimate);
        }

        _pileOverlaySections.QueueSort();
        for (int i = 0; i < cardsToAnimate.Count; i++)
            cardsToAnimate[i]
                .CallDeferred(nameof(SkillCard.StartAnimation), PileCardEnterStagger * i);
    }

    private void AddPileOverlaySection(
        VBoxContainer stack,
        PlayerCharacter player,
        BattlePileOverlaySection section,
        List<SkillCard> cardsToAnimate
    )
    {
        if (stack == null)
            return;

        var sectionRoot =
            GetPileOverlaySectionRoot(section.Kind) ?? CreatePileOverlaySectionRoot(section.Kind);
        if (sectionRoot.GetParent() == null)
            stack.AddChild(sectionRoot);
        sectionRoot.Visible = true;

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
        grid.CustomMinimumSize = new Vector2(PileOverlayContentWidth, 0f);

        ClearGridChildren(grid);

        if (section.Pile.Length == 0)
        {
            emptyLabel.Visible = true;
            grid.Visible = false;
            return;
        }

        emptyLabel.Visible = false;
        grid.Visible = true;
        _pileOverlayGrid = grid;

        foreach (SkillID skillId in section.Pile)
        {
            var card = CreatePilePreviewCard(player, skillId);
            if (card == null)
                continue;

            card.ResetState();
            card.HoverHint.Visible = false;

            var holder = CreatePileCardHolder(card);
            grid.AddChild(holder);
            cardsToAnimate?.Add(card);
        }

        grid.QueueSort();
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

    private VBoxContainer CreatePileOverlaySectionRoot(BattlePileKind kind)
    {
        return new VBoxContainer
        {
            Name = GetPileOverlaySectionName(kind),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
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

    private void EnsurePileOverlayUi()
    {
        if (
            _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlaySections != null
            && GodotObject.IsInstanceValid(_pileOverlaySections)
        )
            return;

        Node parent = BattleNode ?? GetParent();
        if (parent == null)
            return;

        _pileOverlayLayer = parent.GetNodeOrNull<CanvasLayer>("BattlePileOverlayLayer");
        if (_pileOverlayLayer == null)
        {
            _pileOverlayLayer = new CanvasLayer
            {
                Name = "BattlePileOverlayLayer",
                Layer = CardPlayOverlayLayer + 20,
            };
            parent.AddChild(_pileOverlayLayer);
        }

        _pileOverlayRoot = _pileOverlayLayer.GetNodeOrNull<Control>("PileOverlayRoot");
        if (
            _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.GetNodeOrNull<VBoxContainer>("Scroll/Margin/PileSections") == null
        )
        {
            _pileOverlayRoot.Name = "OldPileOverlayRoot";
            _pileOverlayRoot.QueueFree();
            _pileOverlayRoot = null;
        }

        if (_pileOverlayRoot == null)
        {
            _pileOverlayRoot =
                BattlePileOverlayScene?.Instantiate<Control>() ?? CreateFallbackPileOverlayRoot();
            _pileOverlayLayer.AddChild(_pileOverlayRoot);
        }
        ConfigurePileOverlayRoot(_pileOverlayRoot);

        var mask = _pileOverlayRoot.GetNodeOrNull<ColorRect>("Mask");
        if (mask == null)
        {
            mask = new ColorRect
            {
                Name = "Mask",
                Color = new Color(0f, 0f, 0f, 0.68f),
                MouseFilter = MouseFilterEnum.Stop,
            };
            _pileOverlayRoot.AddChild(mask);
        }
        ConfigurePileOverlayMask(mask);

        var scroll = _pileOverlayRoot.GetNodeOrNull<ScrollContainer>("Scroll");
        if (scroll == null)
            scroll = CreateFallbackPileOverlayScroll(_pileOverlayRoot);

        _pileOverlayMargin = scroll.GetNodeOrNull<MarginContainer>("Margin");
        if (_pileOverlayMargin == null)
            _pileOverlayMargin = CreateFallbackPileOverlayMargin(scroll);

        _pileOverlaySections = _pileOverlayMargin.GetNodeOrNull<VBoxContainer>("PileSections");
        if (_pileOverlaySections == null)
        {
            _pileOverlaySections = new VBoxContainer
            {
                Name = "PileSections",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _pileOverlayMargin.AddChild(_pileOverlaySections);
        }
        _pileOverlaySections.AddThemeConstantOverride("separation", PileOverlaySectionSeparation);
    }

    private void ConfigurePileOverlayRoot(Control root)
    {
        if (root == null)
            return;

        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.MouseFilter = MouseFilterEnum.Stop;
        root.GuiInput += OnPileOverlayBackgroundGuiInput;
    }

    private static Control CreateFallbackPileOverlayRoot()
    {
        return new Control
        {
            Name = "PileOverlayRoot",
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
    }

    private static ScrollContainer CreateFallbackPileOverlayScroll(Control root)
    {
        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            MouseFilter = MouseFilterEnum.Pass,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        scroll.OffsetLeft = 160f;
        scroll.OffsetTop = 86f;
        scroll.OffsetRight = -160f;
        scroll.OffsetBottom = -92f;
        root.AddChild(scroll);
        return scroll;
    }

    private static MarginContainer CreateFallbackPileOverlayMargin(ScrollContainer scroll)
    {
        var margin = new MarginContainer
        {
            Name = "Margin",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        scroll.AddChild(margin);
        return margin;
    }

    private void ConfigurePileOverlayMask(ColorRect mask)
    {
        if (mask == null)
            return;

        mask.SetAnchorsPreset(LayoutPreset.FullRect);
        mask.Color = new Color(0f, 0f, 0f, 0.68f);
        mask.MouseFilter = MouseFilterEnum.Stop;
        mask.GuiInput += OnPileOverlayBackgroundGuiInput;
    }

    private void OnPileOverlayBackgroundGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
        {
            HidePileOverlay();
            GetViewport().SetInputAsHandled();
        }
    }

    private static Control CreatePileCardHolder(SkillCard card)
    {
        var holder = new Control
        {
            CustomMinimumSize = PileCardHolderSize,
            Size = PileCardHolderSize,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = false,
        };

        card.Position =
            PileCardHolderPadding - 0.5f * (Vector2.One - PileCardScale) * BattleCardBaseSize;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        holder.AddChild(card);
        return holder;
    }

    private static SkillCard CreatePilePreviewCard(PlayerCharacter player, SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        if (skill == null)
            return null;

        skill.OwnerCharater = player;
        skill.UpdateDescription();

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"PileCard_{skillId}";
        card.ConfigureDisplayScale(PileCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.PreviewCharacterName = player?.CharacterName;
        card.PreviewCharacterKey = player?.CharacterKey;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Button.FocusMode = FocusModeEnum.None;
        card.SetSkill(skill);
        card.CharacterName.Text = player?.CharacterName ?? string.Empty;
        return card;
    }

    private void ClearPileOverlayCards()
    {
        if (_pileOverlaySections != null && GodotObject.IsInstanceValid(_pileOverlaySections))
        {
            foreach (Node child in _pileOverlaySections.GetChildren())
            {
                if (child is not Control section)
                    continue;

                section.Visible = false;
                if (section.GetNodeOrNull<Label>("Empty") is Label emptyLabel)
                    emptyLabel.Visible = false;
                if (section.GetNodeOrNull<GridContainer>("Grid") is GridContainer grid)
                {
                    grid.Visible = false;
                    ClearGridChildren(grid);
                }
            }
            _pileOverlayGrid = null;
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
                child.QueueFree();
            }
            return;
        }

        for (int i = _pileOverlayMargin.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = _pileOverlayMargin.GetChild(i);
            _pileOverlayMargin.RemoveChild(child);
            child.QueueFree();
        }
        _pileOverlayGrid = null;
    }

    private static void ClearGridChildren(GridContainer grid)
    {
        if (grid == null || !GodotObject.IsInstanceValid(grid))
            return;

        for (int i = grid.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = grid.GetChild(i);
            grid.RemoveChild(child);
            child.QueueFree();
        }
    }

    private bool IsPileOverlayVisible()
    {
        return _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible;
    }

    private void HidePileOverlay()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayRoot.Visible = false;
        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Ignore;
        ClearPileOverlayCards();
    }

    private void OnEndTurnPressed()
    {
        _ = QueueEndTurnAsync();
    }

    private Color GetCardEnergyStateModulate(int index)
    {
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || !IsCardIndexValid(index)
            || index >= _activePlayer.Skills.Length
        )
        {
            return SkillButton.DisabledModulate;
        }

        Skill skill = _activePlayer.Skills[index];
        bool canInteract =
            skill != null
            && !_isResolvingCard
            && !_endTurnQueued
            && !_manualTargetInfoOpen
            && !IsManualTargetSelectionPending()
            && !IsCardCommitted(index);
        if (canInteract && !skill.CanBePlayed)
            return SkillButton.EnabledModulate;

        return canInteract && skill.CanUseCurrentEnergy()
            ? SkillButton.EnabledModulate
            : SkillButton.DisabledModulate;
    }

    private bool CanUseEndTurnShortcut()
    {
        return Visible
            && _uiBuilt
            && _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _endTurnButton != null
            && !_endTurnButton.Disabled;
    }

    private async Task QueueEndTurnAsync()
    {
        if (!CanQueueEndTurn())
            return;

        _endTurnQueued = true;
        ClearLiftedCard(instant: false);
        RefreshTurnUi();

        if (
            !_isProcessingCardQueue
            && _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
        )
            await ExecuteQueuedEndTurnAsync();
    }

    private bool CanQueueEndTurn()
    {
        return _activePlayer != null
            && !_isResolvingCard
            && !_endTurnQueued
            && !IsManualTargetSelectionPending()
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }

    private async Task ExecuteQueuedEndTurnAsync()
    {
        if (!_endTurnQueued || !CanExecuteQueuedEndTurn())
        {
            _endTurnQueued = false;
            RefreshTurnUi();
            return;
        }

        _endTurnQueued = false;
        PlayerCharacter endingPlayer = _activePlayer;
        ClearLiftedCard(instant: false);
        _isResolvingCard = true;
        _suppressNextRefreshLayout = true;
        RefreshTurnUi();

        await PlayEndTurnHandDiscardAnimationsAsync(endingPlayer);

        if (
            !GodotObject.IsInstanceValid(this)
            || endingPlayer == null
            || !GodotObject.IsInstanceValid(endingPlayer)
            || _activePlayer != endingPlayer
        )
        {
            return;
        }

        endingPlayer.EndAction();
    }

    private bool CanExecuteQueuedEndTurn()
    {
        return _activePlayer != null
            && !_isResolvingCard
            && !_isProcessingCardQueue
            && _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }
}
