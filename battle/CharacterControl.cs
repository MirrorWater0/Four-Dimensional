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
    private static readonly Vector2 BattleCardBaseSize = new(240f, 370f);
    private static readonly Vector2 BattleCardScale = new(0.936f, 0.936f);
    private static readonly Vector2 PileCardScale = new(0.74f, 0.74f);
    private static readonly Vector2 PileCardDisplaySize = BattleCardBaseSize * PileCardScale;
    private const float PileCardEnterStagger = 0.025f;
    private const float EndTurnCardVanishDuration = 0.32f;
    private const float CardPlayVanishDuration = 0.5f;
    private const float CardPlayMoveDuration = 0.18f;
    private const float StatusInsertCardScale = 0.62f;
    private const float StatusInsertArrangeDuration = 0.22f;
    private const float StatusInsertHoldDuration = 0.22f;
    private const float StatusInsertFlyDuration = 0.34f;
    private const float StatusInsertImpactFadeDuration = 0.12f;
    private const float StatusInsertStagger = 0.055f;
    private const float CardHoverLiftY = -22f;
    private const float CardHoverScaleMultiplier = 1.03f;
    private const float CarryCardSpawnScaleMultiplier = 0.72f;
    private const float HandAreaPadding = 18f;
    private const float HandCardGap = 14f;
    private const float HandLayoutTweenDuration = 0.18f;
    private const int CardPlayOverlayLayer = 80;
    private const int PlayedCardZIndex = 100;
    private const int TemporaryCarryCardZIndex = 300;
    private const int ManualTargetPickerZIndex = 400;
    private const int HandCardCapacity = PlayerCharacter.MaxBattleHandSize;
    private static readonly Vector2 PlayedCardScale = new(1.104f, 1.104f);
    private static readonly Color QueuedCardModulate = new(1f, 1f, 1f, 0.82f);

    public Battle BattleNode => field ??= GetParent<Battle>();
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
    private GridContainer _pileOverlayGrid;
    private SkillCard[] _cards = new SkillCard[HandCardCapacity];
    private Control[] _cardSlots = new Control[HandCardCapacity];
    private Tween[] _cardSlotLayoutTweens = new Tween[HandCardCapacity];
    private Vector2?[] _cardSlotLayoutTargets = new Vector2?[HandCardCapacity];
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
            if (_manualTargetInfoOpen)
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
        card.EnergyCost.Text = "状态";
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
        int[] visibleSlotIndexes = GetVisibleHandSlotIndexes();
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

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            slot.Size = cardSize;
            slot.CustomMinimumSize = cardSize;
            Vector2 targetPosition = targetPositions.TryGetValue(i, out Vector2 visiblePosition)
                ? visiblePosition
                : hiddenPosition;
            MoveCardSlotTo(i, targetPosition, instant || !_layoutInitialized);
        }

        _layoutInitialized = true;
    }

    private void MoveCardSlotTo(int index, Vector2 targetPosition, bool instant)
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

        Tween tween = slot.CreateTween();
        _cardSlotLayoutTweens[index] = tween;
        _cardSlotLayoutTargets[index] = targetPosition;
        tween
            .TweenProperty(slot, "position", targetPosition, HandLayoutTweenDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        tween.TweenCallback(
            Callable.From(() =>
            {
                if (_cardSlotLayoutTweens[index] == tween)
                    _cardSlotLayoutTweens[index] = null;
            })
        );
    }

    private int[] GetVisibleHandSlotIndexes()
    {
        if (_activePlayer?.Skills == null)
            return Array.Empty<int>();

        int max = Math.Min(_activePlayer.Skills.Length, _cards.Length);
        var indexes = new List<int>(max);
        for (int i = 0; i < max; i++)
        {
            if (_activePlayer.Skills[i] != null)
                indexes.Add(i);
        }

        return indexes.ToArray();
    }

    private void PrepareNewHandCardSlotForRightEntry(int index)
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
        float rightMostX = -1f;
        float rowY = slot.Position.Y;
        bool foundVisibleSlot = false;

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            if (i == index || _cards[i] == null || !_cards[i].Visible)
                continue;

            Control otherSlot = _cardSlots[i];
            if (otherSlot == null || !GodotObject.IsInstanceValid(otherSlot))
                continue;

            if (!foundVisibleSlot)
            {
                rowY = otherSlot.Position.Y;
                foundVisibleSlot = true;
            }
            rightMostX = Mathf.Max(rightMostX, otherSlot.Position.X);
        }

        float startX = rightMostX >= 0f
            ? rightMostX + cardSize.X + HandCardGap
            : Math.Max(0f, (_cardRow.Size.X - cardSize.X) * 0.5f);
        slot.Position = new Vector2(startX, rowY);
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
                PrepareNewHandCardSlotForRightEntry(i);
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
                card.StartAnimation(0.05f * i);
            }

            _displayedSkills[i] = skill;
            _displayedSkillIds[i] = skill.SkillId;
            _cardDisplayInitialized[i] = true;

            bool canInteract =
                !_isResolvingCard
                && !_endTurnQueued
                && !_manualTargetInfoOpen
                && !IsManualTargetSelectionPending()
                && !isCommitted;
            bool canUse = canInteract && skill.CanUseCurrentEnergy();
            card.Button.Disabled = !canInteract;
            card.Modulate =
                isCommitted ? QueuedCardModulate
                : !skill.CanBePlayed ? SkillButton.EnabledModulate
                : canUse ? SkillButton.EnabledModulate
                : SkillButton.DisabledModulate;
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
    }

    private static bool WasSkillAlreadyDisplayed(Skill[] previousDisplayedSkills, Skill skill)
    {
        return skill != null
            && previousDisplayedSkills != null
            && previousDisplayedSkills.Any(previousSkill => ReferenceEquals(previousSkill, skill));
    }

    private Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (_suppressCardButtonPressUntilLeftRelease && !allowSuppressedPress)
            return Task.CompletedTask;

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
            return Task.CompletedTask;
        }

        Skill skill = _activePlayer.Skills[index];
        if (skill == null || !skill.CanUseCurrentEnergy())
            return Task.CompletedTask;

        if (_liftedCardIndex != -1 && _liftedCardIndex != index)
            return Task.CompletedTask;

        if (_liftedCardIndex == index)
        {
            if (!IsMouseOutsideHandArea())
            {
                ClearLiftedCard(instant: false);
                return Task.CompletedTask;
            }

            QueueCardPlay(index, skill);
            return Task.CompletedTask;
        }

        if (_liftedCardIndex != -1)
            ClearLiftedCard(instant: false);

        LiftCard(index);
        return Task.CompletedTask;
    }

    private void ClearCardButtonPressSuppression()
    {
        _suppressCardButtonPressUntilLeftRelease = false;
    }

    private void QueueCardPlay(int index, Skill skill)
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
        else
            card.PressEffect();
        await ToSignal(
            GetTree().CreateTimer(CardPlayVanishDuration),
            SceneTreeTimer.SignalName.Timeout
        );
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
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (overlay == null)
            return null;

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = "CarryCard";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.ConfigureDisplayScale(PlayedCardScale);
        overlay.AddChild(card);
        card.Visible = false;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.SetSkill(skill);
        card.EnergyCost.Text = "耗能:0";
        card.CharacterName.Text = $"{actor.CharacterName} | 连携";
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
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Visible = true;
        card.ResetState();
        card.SetSkill(play.Skill);
        card.EnergyCost.Text = "耗能:0";
        card.CharacterName.Text = $"{play.Actor.CharacterName} | 连携";
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
        _liftedCardIndex = -1;
        SetProcess(false);
        ResetCardMotion(index, instant);
    }

    private async Task<bool> SelectManualFriendlyTargetIfNeededAsync(QueuedCardPlay play)
    {
        if (play?.Skill?.RequiresManualFriendlyTarget() != true)
            return true;

        if (!HasManualFriendlyTargetCandidates(play.Skill))
            return true;

        Character target = await ShowManualTargetPickerAsync(play.Skill, play.Card);
        if (target == null || !GodotObject.IsInstanceValid(target))
            return false;

        play.Skill.SetManualFriendlyTarget(target);
        return play.Skill.HasManualFriendlyTarget();
    }

    private bool HasManualFriendlyTargetCandidates(Skill skill)
    {
        if (skill?.OwnerCharater == null || BattleNode == null)
            return false;

        bool excludeSelf = skill.ManualFriendlyTargetExcludesSelf();
        bool allowDying = skill.ManualFriendlyTargetAllowsDying();
        Character owner = skill.OwnerCharater;

        return BattleNode
            .GetTeamCharacters(skill.OwnerCharater.IsPlayer, includeSummons: true)
            .Any(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && (allowDying || character.State != Character.CharacterState.Dying)
                && (!excludeSelf || character != owner)
            );
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

        bool excludeSelf = skill.ManualFriendlyTargetExcludesSelf();
        bool allowDying = skill.ManualFriendlyTargetAllowsDying();
        Character owner = skill.OwnerCharater;
        Character[] targets = BattleNode
            .GetTeamCharacters(skill.OwnerCharater.IsPlayer, includeSummons: true)
            .Where(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && (allowDying || character.State != Character.CharacterState.Dying)
                && (!excludeSelf || character != owner)
            )
            .OrderBy(character => character.PositionIndex)
            .ToArray();

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
            _manualTargetPickerPlayedCard.Visible = !hidden;
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
        _manualTargetCompletion?.TrySetResult(null);
        _manualTargetCompletion = null;
        _manualTargetPickerTemporarilyHidden = false;
        if (
            _manualTargetPickerPlayedCard != null
            && GodotObject.IsInstanceValid(_manualTargetPickerPlayedCard)
        )
        {
            _manualTargetPickerPlayedCard.Visible = true;
        }
        _manualTargetPickerPlayedCard = null;

        if (
            _manualTargetPickerRoot == null
            || !GodotObject.IsInstanceValid(_manualTargetPickerRoot)
        )
        {
            if (wasInfoOpen || wasTargetSelectionPending)
                RefreshTurnUi();
            return;
        }

        _manualTargetPickerRoot.Visible = false;
        ApplyManualTargetPickerTemporaryHiddenState();
        ClearManualTargetCards();
        if (wasInfoOpen || wasTargetSelectionPending)
            RefreshTurnUi();
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
        ShowPileOverlay(_activePlayer, pile);
    }

    private void ShowPileOverlay(PlayerCharacter player, SkillID[] pile)
    {
        EnsurePileOverlayUi();
        ClearPileOverlayCards();

        if (_pileOverlayRoot == null || _pileOverlayGrid == null)
            return;

        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Stop;
        _pileOverlayRoot.MoveToFront();

        var cardsToAnimate = new List<SkillCard>();
        foreach (SkillID skillId in pile ?? Array.Empty<SkillID>())
        {
            var card = CreatePilePreviewCard(player, skillId);
            if (card == null)
                continue;

            var holder = CreatePileCardHolder(card);
            _pileOverlayGrid.AddChild(holder);
            card.ResetState();
            card.HoverHint.Visible = false;
            cardsToAnimate.Add(card);
        }

        _pileOverlayGrid.QueueSort();
        for (int i = 0; i < cardsToAnimate.Count; i++)
            cardsToAnimate[i]
                .CallDeferred(nameof(SkillCard.StartAnimation), PileCardEnterStagger * i);
    }

    private void EnsurePileOverlayUi()
    {
        if (_pileOverlayRoot != null && GodotObject.IsInstanceValid(_pileOverlayRoot))
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
        if (_pileOverlayRoot == null)
        {
            _pileOverlayRoot = new Control
            {
                Name = "PileOverlayRoot",
                Visible = false,
                MouseFilter = MouseFilterEnum.Stop,
            };
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
        {
            scroll = new ScrollContainer
            {
                Name = "Scroll",
                MouseFilter = MouseFilterEnum.Pass,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            scroll.SetAnchorsPreset(LayoutPreset.FullRect);
            scroll.OffsetLeft = 112f;
            scroll.OffsetTop = 80f;
            scroll.OffsetRight = -112f;
            scroll.OffsetBottom = -80f;
            _pileOverlayRoot.AddChild(scroll);

            var margin = new MarginContainer
            {
                Name = "Margin",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            margin.AddThemeConstantOverride("margin_left", 12);
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddThemeConstantOverride("margin_right", 12);
            margin.AddThemeConstantOverride("margin_bottom", 12);
            scroll.AddChild(margin);

            _pileOverlayGrid = new GridContainer
            {
                Name = "Grid",
                Columns = 6,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _pileOverlayGrid.AddThemeConstantOverride("h_separation", 18);
            _pileOverlayGrid.AddThemeConstantOverride("v_separation", 22);
            margin.AddChild(_pileOverlayGrid);
        }
        else
        {
            _pileOverlayGrid = scroll.GetNodeOrNull<GridContainer>("Margin/Grid");
        }
    }

    private void ConfigurePileOverlayRoot(Control root)
    {
        if (root == null)
            return;

        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.MouseFilter = MouseFilterEnum.Stop;
        root.GuiInput += OnPileOverlayBackgroundGuiInput;
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
            CustomMinimumSize = PileCardDisplaySize,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        card.Position = -0.5f * (Vector2.One - PileCardScale) * BattleCardBaseSize;
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
        if (_pileOverlayGrid == null || !GodotObject.IsInstanceValid(_pileOverlayGrid))
            return;

        for (int i = _pileOverlayGrid.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = _pileOverlayGrid.GetChild(i);
            _pileOverlayGrid.RemoveChild(child);
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

        foreach (SkillCard card in _cards)
        {
            if (card == null || !card.Visible)
                continue;

            card.Button.Disabled = true;
            if (card.CurrentSkill?.ExhaustsAtTurnEndInHand == true)
                card.PlayExhaustEffect(EndTurnCardVanishDuration);
            else
                card.Vanish();
        }

        await ToSignal(
            GetTree().CreateTimer(EndTurnCardVanishDuration),
            SceneTreeTimer.SignalName.Timeout
        );

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
