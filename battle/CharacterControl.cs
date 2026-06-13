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
    private static readonly Vector2 BattleCardBaseSize = new(240f, 370f);

    [Export(PropertyHint.Range, "0.5,1.5,0.01")]
    public float BattleCardScaleFactor = 1f;

    [Export(PropertyHint.Range, "-160,160,1")]
    public float HandCardYOffset = 40f;

    [Export(PropertyHint.Range, "-160,160,1")]
    public float HandDrawEntryLandingYOffset = 0f;

    [Export(PropertyHint.Range, "0.02,0.2,0.005")]
    public float HandDroppedCardReturnMotionDuration = 0.06f;

    [Export(PropertyHint.Range, "900,9000,100")]
    public float HandDroppedCardReturnPixelsPerSecond = 2200f;
    private Vector2 BattleCardScale => Vector2.One * BattleCardScaleFactor;
    private const float EndTurnCardVanishDuration = 0.32f;
    private const float CardPlayVanishDuration = 0.5f;
    private const float CardPlayDiscardCompressDuration = 0.25f;
    private const float CardPlayDiscardFlyDuration = 0.32f;
    private const float CardPlayDiscardTrailFadeDuration = 0.14f;
    private const float CardPlayDiscardCompressedScaleFactor = 0.22f;
    private const float CardPlayDiscardTargetScaleFactor = 0.28f;
    private const float CardPlayMoveDuration = 0.25f;
    private const float StatusInsertCardScale = 1.0f;
    private const float StatusInsertArrangeDuration = 0.22f;
    private const float StatusInsertHoldDuration = 0.22f;
    private const float StatusInsertFlyDuration = 0.34f;
    private const float StatusInsertStagger = 0.055f;
    private const int StatusInsertCardsCreatedPerFrame = 3;
    private const float CardHoverLiftY = -58f;
    private const float CardHoverScaleMultiplier = 1.15f;
    private const float HandHoverResumeMouseMoveDistance = 8f;
    private const float TemporaryCardSpawnScaleMultiplier = 0.72f;
    private const float DiscardSelectionSelectedVerticalOffset = -80f;
    private const float DiscardSelectionSelectedMaxStep = 280f;
    private const float DiscardSelectionSelectedGap = 24f;
    private const float HandAreaPadding = 18f;
    private const float HandCardGap = 14f;
    private const float HandCardMinStepRatio = 0.42f;
    private const float HandCardOverlapStepRatio = 5f / 6f;
    private const float HandCardHoverSpreadRatio = 0.55f;
    private const float HandCardMaxHoverSpread = 36f;
    private const float HandCardResetMotionDuration = 0.16f;
    private const float HandLayoutMinTweenDuration = 0.24f;
    private const float HandLayoutMaxTweenDuration = 0.9f;
    private const float HandLayoutPixelsPerSecond = 900f;
    private const float HandLayoutFollowSharpness = 8f;
    private const float HandLayoutFollowSnapDistance = 0.45f;
    private const float HandLayoutFollowSnapRotation = 0.002f;
    private const float HandDrawEntryMinTweenDuration = 0.2f;
    private const float HandDrawEntryMaxTweenDuration = 0.60f;
    private const float HandDrawEntryPixelsPerSecond = 1850f;
    private const float HandDrawTrailFadeDuration = 0.1f;
    private const float HandDrawTrailWidth = 10f;
    private const float HandDrawEntryStagger = 0.05f;
    private const float HandDrawEntryMinStagger = 0.018f;
    private const float HandDrawEntryMaxTotalStaggerDuration = 0.48f;
    private const int HandDrawEntryCompactDurationThreshold = 6;
    private const float HandDrawEntryManyCardDurationScale = 0.72f;
    private const int ShufflePreviewMaxCardCount = 9;
    private const float ShufflePreviewCardScale = 0.18f;
    private const float ShufflePreviewCardFlyDuration = 0.78f;
    private const float ShufflePreviewCardStagger = 0.045f;
    private const float ShufflePreviewDrawEntryDelayPadding = 0.08f;
    private const int HandCardHoverZIndex = 90;
    private const int StatusLabelZIndex = HandCardHoverZIndex + 8;
    private const float StatusLabelLiftY = 62f;
    private const int PlayedCardZIndex = 100;
    private const int DiscardSelectionOverlayZIndex = PlayedCardZIndex + 8;
    private const int DiscardSelectionSelectedCardZIndex = PlayedCardZIndex + 20;
    private const int TemporaryCardZIndex = 300;
    private const int ManualTargetPickerZIndex = 400;
    private const int HandCardCapacity = PlayerCharacter.MaxBattleHandSize;
    private const int QueuedPlayedCardVisibleLayers = 4;
    private const float QueuedPlayedCardLayerScaleStep = 0.075f;
    private const float QueuedPlayedCardVerticalOffset = -100f;
    private static readonly Vector2 QueuedPlayedCardLayerOffset = new(18f, -10f);
    private static readonly Vector2 PlayedCardScale = new(0.74f, 0.74f);
    private static readonly Color QueuedCardModulate = new(1f, 1f, 1f, 0.82f);
    private static readonly Color ManualTargetCandidateColor = new(0.42f, 0.82f, 1f, 0.72f);
    private static readonly Color ManualTargetHoveredColor = new(1f, 0.95f, 0.62f, 1f);
    private static readonly Color ManualTargetEffectHostileColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color ManualTargetEffectFriendlyColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Color DiscardSelectionCardModulate = new(1f, 0.82f, 0.42f, 1f);
    private static readonly Vector2 ManualTargetDamagePreviewLabelOffset = new(-50f, -130f);

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

    private VBoxContainer _root;
    private Label _statusLabel;
    private Control _cardRow;
    private Control _handInputBlocker;
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
    private Vector2? _manualTargetArrowSourcePosition;
    private Vector2? _manualTargetArrowSourceTangent;
    private string _manualTargetArrowStatusText = "选择一名己方角色";
    private readonly List<VBoxContainer> _manualTargetArrowDamagePanels = new();
    private Character[] _manualTargetArrowEffectHostileTargets = Array.Empty<Character>();
    private Character[] _manualTargetArrowEffectFriendlyTargets = Array.Empty<Character>();
    private readonly Dictionary<Character, RebirthPreviewState> _manualRebirthTargetPreviewStates =
        new();
    private readonly HashSet<int> _turnEndStatusTriggerCardIndexes = new();
    private Button _endTurnButton;
    private SkillCard[] _cards = new SkillCard[HandCardCapacity];
    private Control[] _cardSlots = new Control[HandCardCapacity];
    private Tween[] _cardSlotLayoutTweens = new Tween[HandCardCapacity];
    private Vector2?[] _cardSlotLayoutTargets = new Vector2?[HandCardCapacity];
    private float?[] _cardSlotLayoutRotationTargets = new float?[HandCardCapacity];
    private bool[] _cardSlotLayoutFollowActive = new bool[HandCardCapacity];
    private float[] _cardSlotLayoutPixelsPerSecondOverrides = new float[HandCardCapacity];
    private readonly HashSet<int> _drawEntrySlotIndexes = new();
    private readonly Dictionary<int, int> _drawEntrySlotOrders = new();
    private readonly HashSet<int> _hiddenPendingDrawEntrySlotIndexes = new();
    private readonly Dictionary<int, Vector2> _customDrawEntryStartPositions = new();
    private readonly Dictionary<int, Vector2> _drawEntryStartCenters = new();
    private readonly Dictionary<int, SkillCard> _drawEntryPreviewCards = new();
    private readonly HashSet<int> _pendingDrawEntryAnimations = new();
    private readonly int[] _drawEntryAnimationVersions = new int[HandCardCapacity];
    private readonly Dictionary<
        Skill,
        (Vector2 Position, float Rotation)
    > _pendingHandReorderStarts = new();
    private readonly Queue<QueuedCardPlay> _queuedCardPlays = new();
    private readonly Queue<QueuedCardPlay> _queuedFollowUpCardPlays = new();
    private readonly HashSet<int> _queuedCardIndices = new();
    private QueuedCardPlay _activeQueuedCardPlay;
    private Skill[] _displayedSkills = new Skill[HandCardCapacity];
    private SkillID?[] _displayedSkillIds = new SkillID?[HandCardCapacity];
    private bool[] _cardDisplayInitialized = new bool[HandCardCapacity];
    private PlayerCharacter _activePlayer;
    private bool _isResolvingCard;
    private bool _isProcessingCardQueue;
    private int _deferredHoverRefreshVersion;
    private bool _uiBuilt;
    private int _hoveredCardIndex = -1;
    private int _liftedCardIndex = -1;
    private Vector2 _liftedCardMouseOffset;
    private bool[] _cardHoverPreviewActive = new bool[HandCardCapacity];
    private bool _suppressCardButtonPressUntilLeftRelease;
    private bool _suppressHandHoverUntilMouseMove;
    private Vector2 _handHoverSuppressionMousePosition;
    private bool _layoutInitialized;
    private bool _suppressNextRefreshLayout;
    private bool _freezeHandLayout;
    private bool _endTurnQueued;
    private bool _isResolvingEndTurn;
    private ulong _shuffleDrawEntryDelayUntilMsec;
    private int _shuffleDelayedLayoutRefreshVersion;
    private bool _manualTargetPickerTemporarilyHidden;
    private TaskCompletionSource<Character> _manualTargetCompletion;
    private bool _isDiscardSelectionActive;
    private bool _isDiscardSelectionCompleting;
    private bool _discardSelectionExhaustMode;
    private int _discardSelectionTargetCount;
    private readonly List<Skill> _discardSelectionSkills = new();
    private readonly Dictionary<Skill, SkillCard> _discardSelectionCards = new();
    private Control _discardSelectionOverlay;
    private ColorRect _discardSelectionScreenMask;
    private TaskCompletionSource<int> _discardSelectionCompletion;
    private int _lastDiscardSelectionInputIndex = -1;
    private ulong _lastDiscardSelectionInputMsec;

    public readonly struct StatusCardInsertAnimationEntry
    {
        public StatusCardInsertAnimationEntry(
            Character target,
            SkillID statusSkillId,
            int count,
            Character source = null,
            BattleCardPileTarget pileTarget = BattleCardPileTarget.DrawPileCards
        )
        {
            Target = target;
            StatusSkillId = statusSkillId;
            Count = count;
            Source = source;
            PileTarget = pileTarget;
        }

        public Character Target { get; }
        public SkillID StatusSkillId { get; }
        public int Count { get; }
        public Character Source { get; }
        public BattleCardPileTarget PileTarget { get; }
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
        public bool FlyToDiscardPileAfterUse { get; init; }
        public bool FreeEnergyCost { get; init; }
        public bool ForceManualTargetCardPicker { get; init; }
        public bool MoveToCenterBeforeEffect { get; init; }
        public bool RemovedFromHand { get; set; }
        public bool ResolvedToBattlePile { get; set; }
        public Task MoveToCenterTask { get; set; }
        public TaskCompletionSource<bool> Completion { get; init; }
    }

    private sealed class RebirthPreviewState
    {
        public Color TargetModulate { get; init; }
        public bool SpriteVisible { get; init; }
        public SubViewport Viewport { get; init; }
        public Sprite2D PreviewSprite { get; init; }
    }

    private sealed class StatusInsertPreviewCard
    {
        public SkillCard Card { get; init; }
        public BattleCardPileTarget PileTarget { get; init; }
        public Vector2 Scale { get; init; }
        public Vector2 CardSize { get; init; }
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
        UpdateHandLayoutFollowers((float)delta);
        UpdateLiftedCardPosition();
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
        )
        {
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
            && _suppressCardButtonPressUntilLeftRelease
        )
        {
            if (_suppressHandHoverUntilMouseMove)
                _handHoverSuppressionMousePosition =
                    GetViewport()?.GetMousePosition() ?? _handHoverSuppressionMousePosition;
            CallDeferred(nameof(ClearCardButtonPressSuppression));
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

            _pendingHandReorderStarts[skill] = (
                NormalizeHandReorderStartPosition(oldSlot.Position),
                0f
            );
        }
    }

    public void QueueBatchDrawMakeRoomAnimation(Skill[] oldHand, Skill[] newHand)
    {
        // Drawn cards now fly directly into their final hand slots. Keeping this
        // hook as a state reset avoids the old compressed mid-point settle pass.
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

    public float PlayBattleDeckShuffleAnimation(int movedCardCount)
    {
        if (movedCardCount <= 0 || !IsInsideTree())
            return 0f;

        int previewCardCount = Math.Min(Math.Min(movedCardCount, ShufflePreviewMaxCardCount), 2);
        float totalDuration =
            ShufflePreviewCardFlyDuration
            + Math.Max(0, previewCardCount - 1) * ShufflePreviewCardStagger
            + ShufflePreviewDrawEntryDelayPadding;
        ulong delayUntil = Time.GetTicksMsec() + (ulong)Math.Ceiling(totalDuration * 1000f);
        if (delayUntil > _shuffleDrawEntryDelayUntilMsec)
            _shuffleDrawEntryDelayUntilMsec = delayUntil;

        _ = PlayBattleDeckShuffleAnimationAsync(previewCardCount);
        return totalDuration;
    }

    private async Task PlayBattleDeckShuffleAnimationAsync(int previewCardCount)
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

        var flyTasks = new List<Task>(previewCardCount);
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

            flyTasks.Add(
                PlayShufflePreviewCardFlyAsync(
                    card,
                    startCenter + startOffset,
                    endCenter + endOffset,
                    i
                )
            );
        }

        if (flyTasks.Count > 0)
            await Task.WhenAll(flyTasks);
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

        var expandedEntries =
            new List<(
                Character Target,
                Character Source,
                Skill StatusSkill,
                BattleCardPileTarget PileTarget
            )>();
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
                expandedEntries.Add((entry.Target, entry.Source, statusSkill, entry.PileTarget));
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
        Vector2 spawnPosition = new(viewportSize.X * 0.5f - cardSize.X * 0.5f, startY - 42f);
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

        await Task.WhenAll(arrangeTasks);
        await ToSignal(
            GetTree().CreateTimer(StatusInsertHoldDuration),
            SceneTreeTimer.SignalName.Timeout
        );

        var flyTasks = new List<Task>();
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

        await Task.WhenAll(flyTasks);

        foreach (StatusInsertPreviewCard preview in cards)
            QueueFreeTemporaryCard(preview.Card);
    }

    private async Task WaitForTweenFinishedAsync(Tween tween)
    {
        if (tween == null || !GodotObject.IsInstanceValid(tween))
            return;

        await ToSignal(tween, Tween.SignalName.Finished);
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
        int availableCount = hand?.Count(skill => skill != null) ?? 0;
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
        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionSelectedCards();
        EnsureDiscardSelectionScreenMask();
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

        foreach (SkillCard card in cards)
        {
            if (card != null && GodotObject.IsInstanceValid(card))
                card.QueueFree();
        }
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
                ClipContents = false,
            };
            _root.AddChild(_cardRow);
        }

        _root.MouseFilter = MouseFilterEnum.Ignore;
        _statusLabel.MouseFilter = MouseFilterEnum.Ignore;
        ConfigureStatusLabel(_statusLabel);
        _cardRow.MouseFilter = MouseFilterEnum.Ignore;
        _cardRow.ClipContents = false;
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
            cardSlot.ClipContents = false;
            WireBattleCard(card, i);
            _cards[i] = card;
        }

        EnsureHandInputBlocker();
        WireActionButtonsFromScene();
        LayoutActionCards(instant: true);
        CallDeferred(nameof(PositionStatusLabel));
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

    private void WireBattleCard(SkillCard card, int index)
    {
        if (card == null)
            return;

        card.ConfigureDisplayScale(BattleCardScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        SetCardButtonInputEnabled(card, false);

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
        ConfigureStatusLabel(_statusLabel);
        _root.AddChild(_statusLabel);

        _cardRow = new Control
        {
            Name = "CardRow",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = false,
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

        EnsureHandInputBlocker();
        LayoutActionCards(instant: true);
        CallDeferred(nameof(PositionStatusLabel));

        _uiBuilt = true;
    }

    private Control CreateCardSlot(int index)
    {
        return new Control
        {
            Name = $"CardSlot{index}",
            CustomMinimumSize = BattleCardBaseSize * BattleCardScale,
            PivotOffset = BattleCardBaseSize * BattleCardScale * 0.5f,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = false,
        };
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
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        if (_handInputBlocker == null || !GodotObject.IsInstanceValid(_handInputBlocker))
            _handInputBlocker = _cardRow.GetNodeOrNull<Control>("HandInputBlocker");

        if (_handInputBlocker == null || !GodotObject.IsInstanceValid(_handInputBlocker))
        {
            _handInputBlocker = new Control
            {
                Name = "HandInputBlocker",
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
                ZIndex = PlayedCardZIndex - 1,
            };
            _cardRow.AddChild(_handInputBlocker);
        }

        _handInputBlocker.ZIndex = PlayedCardZIndex - 1;
        SyncHandInputBlockerRect();
    }

    private void SyncHandInputBlockerRect()
    {
        if (
            _handInputBlocker == null
            || !GodotObject.IsInstanceValid(_handInputBlocker)
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return;
        }

        _handInputBlocker.Position = Vector2.Zero;
        _handInputBlocker.Size = _cardRow.Size;
    }

    private void SetHandInputBlockerVisible(bool visible)
    {
        EnsureHandInputBlocker();
        if (_handInputBlocker == null || !GodotObject.IsInstanceValid(_handInputBlocker))
            return;

        SyncHandInputBlockerRect();
        _handInputBlocker.Visible = visible;
        _handInputBlocker.MouseFilter = visible ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private static void SetCardButtonInputEnabled(SkillCard card, bool enabled)
    {
        if (card?.Button == null)
            return;

        card.Button.Disabled = !enabled;
        card.Button.MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
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

    private static void ConfigureStatusLabel(Label label)
    {
        if (label == null)
            return;

        label.ZIndex = StatusLabelZIndex;
        label.TopLevel = true;
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.03f, 0.06f, 0.95f));
        label.AddThemeConstantOverride("outline_size", 4);
    }

    public void PositionStatusLabel()
    {
        if (
            _statusLabel == null
            || !GodotObject.IsInstanceValid(_statusLabel)
            || _root == null
            || !GodotObject.IsInstanceValid(_root)
            || !_root.IsInsideTree()
        )
        {
            return;
        }

        Rect2 rootRect = _root.GetGlobalRect();
        float width = Math.Min(rootRect.Size.X, 760f);
        _statusLabel.Size = new Vector2(width, Math.Max(_statusLabel.Size.Y, 34f));
        _statusLabel.GlobalPosition =
            rootRect.Position + new Vector2((rootRect.Size.X - width) * 0.5f, -StatusLabelLiftY);
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

    private void LayoutActionCards()
    {
        LayoutActionCards(instant: false);
    }

    private void LayoutActionCards(bool instant)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return;

        SyncHandInputBlockerRect();
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        int[] visibleSlotIndexes = GetVisibleHandSlotIndexes(excludeLiftedCard: true);
        int handCount = visibleSlotIndexes.Length;
        float rowWidth = Math.Max(_cardRow.Size.X, 0f);
        float rowHeight = Math.Max(_cardRow.Size.Y, cardSize.Y);
        float cardY = GetHandCardY(rowHeight, cardSize);
        Vector2 hiddenPosition = new(-cardSize.X * 2f, cardY);
        Dictionary<int, Vector2> targetPositions = BuildHandLayoutTargetPositions(
            visibleSlotIndexes,
            cardSize,
            rowWidth,
            rowHeight
        );
        var handOrderBySlotIndex = new Dictionary<int, int>();
        for (int order = 0; order < visibleSlotIndexes.Length; order++)
            handOrderBySlotIndex[visibleSlotIndexes[order]] = order;

        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            if (_turnEndStatusTriggerCardIndexes.Contains(i))
                continue;

            slot.Size = cardSize;
            slot.CustomMinimumSize = cardSize;
            slot.PivotOffset = cardSize * 0.5f;
            if (handOrderBySlotIndex.TryGetValue(i, out int handOrder))
                ApplyHandCardLayer(i, handOrder);
            else
                ApplyHandCardLayer(i, 0);

            if (i == _liftedCardIndex || IsCardCommitted(i))
            {
                _cardSlotLayoutTweens[i]?.Kill();
                _cardSlotLayoutTweens[i] = null;
                _cardSlotLayoutTargets[i] = null;
                _cardSlotLayoutRotationTargets[i] = null;
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                ClearDrawEntryState(i, revealCard: true);
                continue;
            }

            Vector2 targetPosition = targetPositions.TryGetValue(i, out Vector2 visiblePosition)
                ? visiblePosition
                : hiddenPosition;
            bool drawEntry = _drawEntrySlotIndexes.Contains(i);
            MoveCardSlotTo(i, targetPosition, 0f, instant || !_layoutInitialized, drawEntry);
        }

        _layoutInitialized = true;
    }

    private Dictionary<int, Vector2> BuildHandLayoutTargetPositions(
        int[] visibleSlotIndexes,
        Vector2 cardSize,
        float rowWidth,
        float rowHeight
    )
    {
        visibleSlotIndexes ??= Array.Empty<int>();
        int handCount = visibleSlotIndexes?.Length ?? 0;
        float preferredStep =
            handCount > 1 ? cardSize.X * HandCardOverlapStepRatio : cardSize.X + HandCardGap;
        float minStep = cardSize.X * HandCardMinStepRatio;
        float cardStep =
            handCount > 1
                ? Mathf.Clamp((rowWidth - cardSize.X) / (handCount - 1), minStep, preferredStep)
                : 0f;
        float totalWidth =
            handCount > 1 ? cardSize.X + cardStep * (handCount - 1) : handCount * cardSize.X;
        float x = (rowWidth - totalWidth) / 2f;
        float cardY = GetHandCardY(rowHeight, cardSize);
        int hoveredOrder = Array.IndexOf(visibleSlotIndexes, _hoveredCardIndex);
        float overlapWidth = Math.Max(0f, cardSize.X - cardStep);
        float hoverSpread = Mathf.Clamp(
            overlapWidth * HandCardHoverSpreadRatio,
            0f,
            HandCardMaxHoverSpread
        );
        var targetPositions = new Dictionary<int, Vector2>();
        for (int order = 0; order < handCount; order++)
        {
            int slotIndex = visibleSlotIndexes[order];
            float targetX = x + cardStep * order;
            if (hoveredOrder != -1 && order != hoveredOrder)
            {
                int distance = Math.Abs(order - hoveredOrder);
                float distanceFalloff = 1f / distance;
                targetX += Math.Sign(order - hoveredOrder) * hoverSpread * distanceFalloff;
            }

            targetPositions[slotIndex] = new Vector2(targetX, cardY);
        }

        return targetPositions;
    }

    private float GetHandCardY(float rowHeight, Vector2 cardSize)
    {
        return Math.Max(0f, rowHeight - cardSize.Y) + HandCardYOffset;
    }

    private void UpdateHandLayoutFollowers(float delta)
    {
        if (delta <= 0f || _cardSlots == null)
            return;

        bool hasActiveFollower = false;
        float followRatio = 1f - Mathf.Exp(-HandLayoutFollowSharpness * delta);
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            if (
                !_cardSlotLayoutFollowActive[i]
                || !_cardSlotLayoutTargets[i].HasValue
                || !_cardSlotLayoutRotationTargets[i].HasValue
            )
            {
                continue;
            }

            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
            {
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                continue;
            }

            Vector2 targetPosition = _cardSlotLayoutTargets[i].Value;
            float targetRotation = _cardSlotLayoutRotationTargets[i].Value;
            float distance = slot.Position.DistanceTo(targetPosition);
            float rotationDistance = Math.Abs(slot.Rotation - targetRotation);
            if (
                distance <= HandLayoutFollowSnapDistance
                && rotationDistance <= HandLayoutFollowSnapRotation
            )
            {
                slot.Position = targetPosition;
                slot.Rotation = targetRotation;
                _cardSlotLayoutFollowActive[i] = false;
                _cardSlotLayoutPixelsPerSecondOverrides[i] = 0f;
                continue;
            }

            bool hasSpeedOverride = _cardSlotLayoutPixelsPerSecondOverrides[i] > 0f;
            float pixelsPerSecond = hasSpeedOverride
                ? _cardSlotLayoutPixelsPerSecondOverrides[i]
                : HandLayoutPixelsPerSecond;
            float maxMove = pixelsPerSecond * delta;
            float move = Math.Min(
                maxMove,
                hasSpeedOverride
                    ? distance
                    : Math.Max(HandLayoutFollowSnapDistance, distance * followRatio)
            );
            bool drawEntryFollower =
                _drawEntryPreviewCards.ContainsKey(i) || _drawEntrySlotIndexes.Contains(i);
            slot.Position = drawEntryFollower
                ? slot.Position.Lerp(targetPosition, followRatio)
                : slot.Position.MoveToward(targetPosition, move);
            slot.Rotation = Mathf.Lerp(slot.Rotation, targetRotation, followRatio);
            hasActiveFollower = true;
        }

        if (!hasActiveFollower)
            UpdateProcessState();
    }

    private void UpdateProcessState()
    {
        SetProcess(_liftedCardIndex != -1 || IsAnyHandLayoutFollowerActive());
    }

    private bool IsAnyHandLayoutFollowerActive()
    {
        return _cardSlotLayoutFollowActive?.Any(active => active) == true;
    }

    private void ApplyHandCardLayer(int index, int handOrder)
    {
        if (!IsCardIndexValid(index))
            return;

        Control slot = _cardSlots[index];
        SkillCard card = _cards[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        int layer = handOrder + 1;
        if (index == _hoveredCardIndex)
            layer = HandCardHoverZIndex;
        else if (index == _liftedCardIndex || IsCardCommitted(index))
            layer = Math.Max(slot.ZIndex, PlayedCardZIndex);

        slot.ZIndex = layer;
        if (card != null && GodotObject.IsInstanceValid(card))
            card.ZIndex = layer;
    }

    private int GetHandOrderForSlotIndex(int index)
    {
        int[] visibleSlotIndexes = GetVisibleHandSlotIndexes(excludeLiftedCard: true);
        int order = Array.IndexOf(visibleSlotIndexes, index);
        return order >= 0 ? order : Math.Max(0, index);
    }

    private void MoveCardSlotTo(
        int index,
        Vector2 targetPosition,
        float targetRotation,
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

        if (drawEntry)
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = targetPosition;
            _cardSlotLayoutRotationTargets[index] = targetRotation;
            slot.Scale = Vector2.One;

            if (_drawEntryPreviewCards.ContainsKey(index))
            {
                _cardSlotLayoutFollowActive[index] = true;
                UpdateProcessState();
            }
            else if (!_pendingDrawEntryAnimations.Contains(index))
            {
                StartDrawEntryPreviewAnimation(index, targetPosition, targetPosition, layoutDelay);
            }

            return;
        }

        if (
            instant
            || (
                slot.Position.DistanceSquaredTo(targetPosition) < 0.25f
                && Math.Abs(slot.Rotation - targetRotation) < 0.002f
            )
        )
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = targetPosition;
            _cardSlotLayoutRotationTargets[index] = targetRotation;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
            ClearDrawEntryState(index, revealCard: true);
            CancelDrawEntryPreview(index);
            slot.Position = targetPosition;
            slot.Rotation = targetRotation;
            slot.Scale = Vector2.One;
            return;
        }

        if (
            _cardSlotLayoutTargets[index].HasValue
            && _cardSlotLayoutTargets[index].Value.DistanceSquaredTo(targetPosition) < 0.25f
            && _cardSlotLayoutRotationTargets[index].HasValue
            && Math.Abs(_cardSlotLayoutRotationTargets[index].Value - targetRotation) < 0.002f
            && (_cardSlotLayoutTweens[index] != null || _cardSlotLayoutFollowActive[index])
        )
        {
            return;
        }

        if (_cardSlotLayoutTweens[index] != null)
        {
            _cardSlotLayoutTweens[index]?.Kill();
            HideCardDrawEntryTrail(index);
        }

        CancelDrawEntryPreview(index);
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = targetPosition;
        _cardSlotLayoutRotationTargets[index] = targetRotation;
        _cardSlotLayoutFollowActive[index] = true;
        slot.Scale = Vector2.One;
        UpdateProcessState();
    }

    private static float GetHandLayoutTweenDuration(Vector2 startPosition, Vector2 targetPosition)
    {
        float distance = startPosition.DistanceTo(targetPosition);
        return Mathf.Clamp(
            distance / HandLayoutPixelsPerSecond,
            HandLayoutMinTweenDuration,
            HandLayoutMaxTweenDuration
        );
    }

    private float GetDrawEntryDelay(int index)
    {
        return _drawEntrySlotOrders.TryGetValue(index, out int order)
            ? GetDrawEntryDelayForOrder(order, GetDrawEntryBatchCount())
            : 0f;
    }

    private int GetDrawEntryBatchCount() => Math.Max(1, _drawEntrySlotOrders.Count);

    private static float GetDrawEntryTweenDuration(
        Vector2 startPosition,
        Vector2 targetPosition,
        int batchCount
    )
    {
        float distance = startPosition.DistanceTo(targetPosition);
        float duration = Mathf.Clamp(
            distance / HandDrawEntryPixelsPerSecond,
            HandDrawEntryMinTweenDuration,
            HandDrawEntryMaxTweenDuration
        );

        if (batchCount >= HandDrawEntryCompactDurationThreshold)
            duration *= HandDrawEntryManyCardDurationScale;

        return Mathf.Max(0.16f, duration);
    }

    private static float GetDrawEntryDelayForOrder(int order, int batchCount)
    {
        order = Math.Max(0, order);
        batchCount = Math.Max(1, batchCount);
        if (order == 0 || batchCount <= 1)
            return 0f;

        float dynamicStagger = Math.Min(
            HandDrawEntryStagger,
            HandDrawEntryMaxTotalStaggerDuration / Math.Max(1, batchCount - 1)
        );
        dynamicStagger = Mathf.Clamp(dynamicStagger, HandDrawEntryMinStagger, HandDrawEntryStagger);
        return order * dynamicStagger;
    }

    private void StartDrawEntryPreviewAnimation(
        int index,
        Vector2 targetPosition,
        Vector2 entryTarget,
        float layoutDelay
    )
    {
        if (!IsCardIndexValid(index))
            return;

        int version = ++_drawEntryAnimationVersions[index];
        _pendingDrawEntryAnimations.Add(index);
        _ = PlayDrawEntryPreviewAnimationAsync(
            index,
            targetPosition,
            entryTarget,
            layoutDelay,
            version
        );
    }

    private async Task PlayDrawEntryPreviewAnimationAsync(
        int index,
        Vector2 targetPosition,
        Vector2 entryTarget,
        float layoutDelay,
        int version
    )
    {
        float delay = GetDrawEntryDelay(index) + GetPendingShuffleDrawEntryDelay();
        delay = Math.Max(delay, layoutDelay);
        if (delay > 0f && IsInsideTree())
            await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

        if (!CanStartDrawEntryAnimation(index, version))
            return;

        Vector2 currentTargetPosition = GetDrawEntryTargetSlotPosition(index, targetPosition);
        SkillCard movingCard = ActivateDrawEntryCardInHandSlot(index, currentTargetPosition);
        if (movingCard == null)
        {
            _pendingDrawEntryAnimations.Remove(index);
            FinishDrawEntryAnimation(index, version, revealCard: true);
            return;
        }

        _pendingDrawEntryAnimations.Remove(index);

        if (!CanContinueDrawEntryAnimation(index, version, movingCard))
            return;

        PulsePileButtonReceive(_drawPileButton);
        movingCard.Visible = true;
        movingCard.Modulate = Colors.White;

        Vector2 startCenter = GetDrawEntryStartCenter(index);
        Vector2 targetCenter = GetDrawEntryTargetCenter(index, currentTargetPosition);

        PrepareDrawEntryPreviewTrail(movingCard, out Line trail, out GpuParticles2D particles);
        UpdateTrailParticlesRotation(particles, targetCenter - startCenter);

        await WaitForDrawEntrySlotArrivalAsync(index, version, movingCard, particles);
        if (!IsInsideTree() || !CanContinueDrawEntryAnimation(index, version, movingCard))
            return;

        FinishDrawEntrySlotArrival(index, movingCard);
        _ = FadeAndHideDrawEntryPreviewTrailAsync(trail, particles);
        if (
            _drawEntryPreviewCards.TryGetValue(index, out SkillCard currentPreview)
            && currentPreview == movingCard
        )
        {
            _drawEntryPreviewCards.Remove(index);
        }
        FinishDrawEntryAnimation(index, version, revealCard: false);
    }

    private SkillCard ActivateDrawEntryCardInHandSlot(int index, Vector2 targetPosition)
    {
        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && index < hand.Length ? hand[index] : null;
        if (skill == null)
            return null;

        CancelDrawEntryPreview(index, invalidateAnimation: false);

        SkillCard card = _cards[index];
        Control slot = _cardSlots[index];
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || slot == null
            || !GodotObject.IsInstanceValid(slot)
        )
        {
            return null;
        }

        Node oldParent = card.GetParent();
        if (oldParent != slot)
        {
            oldParent?.RemoveChild(card);
            slot.AddChild(card);
        }

        slot.ClipContents = false;
        slot.Position = GetDrawEntryStartSlotPosition(index);
        slot.Rotation = 0f;
        slot.Scale = Vector2.One;

        card.RestoreDisplayState();
        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Stop;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Scale = BattleCardScale;
        card.Rotation = 0f;
        card.Position = Vector2.Zero;
        ApplyHandCardLayer(index, GetHandOrderForSlotIndex(index));
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _cardSlotLayoutTargets[index] = targetPosition;
        _cardSlotLayoutRotationTargets[index] = 0f;
        _cardSlotLayoutFollowActive[index] = true;
        UpdateProcessState();
        _drawEntryPreviewCards[index] = card;
        return card;
    }

    private async Task WaitForDrawEntrySlotArrivalAsync(
        int index,
        int version,
        SkillCard movingCard,
        GpuParticles2D particles
    )
    {
        while (
            CanContinueDrawEntryAnimation(index, version, movingCard)
            && _cardSlotLayoutFollowActive[index]
            && IsInsideTree()
        )
        {
            Vector2 currentCenter =
                movingCard.GetGlobalTransformWithCanvas() * movingCard.PivotOffset;
            Vector2 targetCenter = GetDrawEntryTargetCenter(
                index,
                GetDrawEntryTargetSlotPosition(index, Vector2.Zero)
            );
            UpdateTrailParticlesRotation(particles, targetCenter - currentCenter);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void FinishDrawEntrySlotArrival(int index, SkillCard card)
    {
        if (!IsCardIndexValid(index) || card == null || !GodotObject.IsInstanceValid(card))
            return;

        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
        {
            slot.Position = GetDrawEntryTargetSlotPosition(index, slot.Position);
            slot.Rotation = 0f;
            slot.Scale = Vector2.One;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        }

        card.Position = Vector2.Zero;
        card.Rotation = 0f;
        card.Scale = BattleCardScale;
    }

    private async Task ReturnDrawEntryCardToSlotSmoothAsync(int index, SkillCard card)
    {
        if (!ReturnDrawEntryCardToSlot(index, card, preserveGlobalCenter: true))
            return;

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        float distance = card.Position.DistanceTo(Vector2.Zero);
        if (distance <= 0.5f || !card.IsInsideTree())
        {
            card.Position = Vector2.Zero;
            return;
        }

        float duration = Mathf.Clamp(distance / HandDrawEntryPixelsPerSecond, 0.045f, 0.12f);
        Tween settleTween = card.CreateTween();
        settleTween
            .TweenProperty(card, "position", Vector2.Zero, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(settleTween, Tween.SignalName.Finished);

        if (card != null && GodotObject.IsInstanceValid(card))
            card.Position = Vector2.Zero;
    }

    private bool ReturnDrawEntryCardToSlot(
        int index,
        SkillCard card,
        bool preserveGlobalCenter = false
    )
    {
        if (
            !IsCardIndexValid(index)
            || card == null
            || !GodotObject.IsInstanceValid(card)
            || _cardSlots[index] == null
            || !GodotObject.IsInstanceValid(_cardSlots[index])
        )
        {
            return false;
        }

        Vector2 globalCenter = preserveGlobalCenter
            ? card.GetGlobalTransformWithCanvas() * card.PivotOffset
            : Vector2.Zero;
        Node parent = card.GetParent();
        parent?.RemoveChild(card);
        _cardSlots[index].AddChild(card);
        card.Position = Vector2.Zero;
        card.Rotation = 0f;
        card.Scale = BattleCardScale;
        if (preserveGlobalCenter)
            SetCardPivotCenterAt(card, globalCenter);
        card.Visible = true;
        card.MouseFilter = MouseFilterEnum.Stop;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        _cards[index] = card;
        return true;
    }

    private int GetDrawEntryOrder(int index) =>
        _drawEntrySlotOrders.TryGetValue(index, out int order) ? order : 0;

    private Vector2 GetDrawEntryStartCenter()
    {
        if (
            _drawPileButton != null
            && GodotObject.IsInstanceValid(_drawPileButton)
            && _drawPileButton.IsInsideTree()
        )
        {
            return GetPileButtonVisualCenter(_drawPileButton);
        }

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        return GetCardRowGlobalPosition(new Vector2(-cardSize.X * 0.85f, 0f)) + cardSize * 0.5f;
    }

    private Vector2 GetDrawEntryStartCenter(int index)
    {
        return _drawEntryStartCenters.TryGetValue(index, out Vector2 center)
            ? center
            : GetDrawEntryStartCenter();
    }

    private Vector2 GetDrawEntryStartSlotPosition(int index)
    {
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        return GetCardRowLocalPosition(GetDrawEntryStartCenter(index)) - cardSize * 0.5f;
    }

    private Vector2 GetDrawEntryTargetSlotPosition(int index, Vector2 fallbackTargetPosition)
    {
        return IsCardIndexValid(index) && _cardSlotLayoutTargets[index].HasValue
            ? _cardSlotLayoutTargets[index].Value
            : fallbackTargetPosition;
    }

    private Vector2 GetDrawEntryTargetCenter(int index, Vector2 fallbackTargetPosition)
    {
        Vector2 landingOffset = new(0f, HandDrawEntryLandingYOffset);
        Vector2 targetPosition = GetDrawEntryTargetSlotPosition(index, fallbackTargetPosition);
        return GetCardRowGlobalPosition(targetPosition)
            + BattleCardBaseSize * BattleCardScale * 0.5f
            + landingOffset;
    }

    private Vector2 GetCardRowGlobalPosition(Vector2 localPosition)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return localPosition;

        return _cardRow.GetGlobalTransformWithCanvas() * localPosition;
    }

    private Vector2 GetCardRowLocalPosition(Vector2 globalPosition)
    {
        if (_cardRow == null || !GodotObject.IsInstanceValid(_cardRow))
            return globalPosition;

        return _cardRow.GetGlobalTransformWithCanvas().AffineInverse() * globalPosition;
    }

    private void FinishDrawEntryAnimation(int index, int version, bool revealCard)
    {
        if (!IsCardIndexValid(index) || _drawEntryAnimationVersions[index] != version)
            return;

        ClearDrawEntryState(index, revealCard, hideTrail: true);
        RefreshTurnUi();
    }

    private bool CanContinueDrawEntryAnimation(int index, int version, SkillCard previewCard)
    {
        return IsCardIndexValid(index)
            && _drawEntryAnimationVersions[index] == version
            && _drawEntrySlotIndexes.Contains(index)
            && previewCard != null
            && GodotObject.IsInstanceValid(previewCard);
    }

    private bool CanStartDrawEntryAnimation(int index, int version)
    {
        return IsCardIndexValid(index)
            && _drawEntryAnimationVersions[index] == version
            && _drawEntrySlotIndexes.Contains(index);
    }

    private void CancelDrawEntryPreview(int index, bool invalidateAnimation = true)
    {
        if (!IsCardIndexValid(index))
            return;

        if (invalidateAnimation)
            _drawEntryAnimationVersions[index]++;
        _pendingDrawEntryAnimations.Remove(index);
        if (!_drawEntryPreviewCards.TryGetValue(index, out SkillCard previewCard))
            return;

        _drawEntryPreviewCards.Remove(index);
        if (previewCard != null && GodotObject.IsInstanceValid(previewCard))
            ReturnDrawEntryCardToSlot(index, previewCard);
    }

    private float GetPendingShuffleDrawEntryDelay()
    {
        return GetRemainingShuffleDrawEntryDelay();
    }

    private float GetRemainingShuffleDrawEntryDelay()
    {
        if (_shuffleDrawEntryDelayUntilMsec == 0)
            return 0f;

        ulong now = Time.GetTicksMsec();
        if (now >= _shuffleDrawEntryDelayUntilMsec)
        {
            _shuffleDrawEntryDelayUntilMsec = 0;
            return 0f;
        }

        return (_shuffleDrawEntryDelayUntilMsec - now) / 1000f;
    }

    private bool ShouldDelayHandLayoutForShuffleDraw()
    {
        return _hiddenPendingDrawEntrySlotIndexes.Count > 0
            && GetRemainingShuffleDrawEntryDelay() > 0f;
    }

    private void ScheduleHandLayoutAfterShuffleDrawDelay()
    {
        float delay = GetRemainingShuffleDrawEntryDelay();
        if (delay <= 0f || !IsInsideTree())
            return;

        int version = ++_shuffleDelayedLayoutRefreshVersion;
        _ = RefreshTurnUiAfterShuffleDrawDelayAsync(version, delay);
    }

    private async Task RefreshTurnUiAfterShuffleDrawDelayAsync(int version, float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
        if (version != _shuffleDelayedLayoutRefreshVersion || !IsInsideTree())
            return;

        RefreshTurnUi();
    }

    private int[] GetVisibleHandSlotIndexes(bool excludeLiftedCard = false)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return Array.Empty<int>();

        int max = Math.Min(hand.Length, _cards.Length);
        var indexes = new List<int>(max);
        for (int i = 0; i < max; i++)
        {
            if (excludeLiftedCard && i == _liftedCardIndex)
                continue;
            if (_turnEndStatusTriggerCardIndexes.Contains(i))
                continue;
            if (hand[i] != null)
                indexes.Add(i);
        }

        return indexes.ToArray();
    }

    private Vector2?[] CaptureCardSlotPositions()
    {
        var positions = new Vector2?[_cardSlots.Length];
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            positions[i] = slot.Position;
        }

        return positions;
    }

    private float?[] CaptureCardSlotRotations()
    {
        var rotations = new float?[_cardSlots.Length];
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            rotations[i] = slot.Rotation;
        }

        return rotations;
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
        int drawOrder = _drawEntrySlotOrders.Count;
        if (_customDrawEntryStartPositions.TryGetValue(index, out Vector2 customStartPosition))
        {
            _drawEntryStartCenters[index] =
                GetCardRowGlobalPosition(customStartPosition) + cardSize * 0.5f;
            _customDrawEntryStartPositions.Remove(index);
        }
        else
        {
            _drawEntryStartCenters[index] = GetDrawEntryStartCenter();
        }
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        _drawEntrySlotIndexes.Add(index);
        _drawEntrySlotOrders[index] = drawOrder;
        _hiddenPendingDrawEntrySlotIndexes.Add(index);

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
        {
            card.Visible = false;
            SetCardButtonInputEnabled(card, false);
            card.HoverHint.Visible = false;
        }
    }

    private void PrepareMovedHandCardSlotFromPreviousPosition(
        int index,
        int previousIndex,
        Vector2?[] previousSlotPositions,
        float?[] previousSlotRotations
    )
    {
        if (
            index < 0
            || index >= _cardSlots.Length
            || previousIndex < 0
            || previousSlotPositions == null
            || previousIndex >= previousSlotPositions.Length
            || !previousSlotPositions[previousIndex].HasValue
        )
        {
            return;
        }

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        CancelDrawEntryPreview(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _drawEntryStartCenters.Remove(index);
        slot.Scale = Vector2.One;
        slot.Position = NormalizeHandReorderStartPosition(
            previousSlotPositions[previousIndex].Value
        );
        slot.Rotation = 0f;
    }

    private bool PreparePendingHandReorderMove(int index, Skill skill)
    {
        if (
            skill == null
            || !_pendingHandReorderStarts.TryGetValue(skill, out var start)
            || index < 0
            || index >= _cardSlots.Length
        )
        {
            return false;
        }

        _pendingHandReorderStarts.Remove(skill);

        Control slot = _cardSlots[index];
        if (slot == null || !GodotObject.IsInstanceValid(slot))
            return false;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
        CancelDrawEntryPreview(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _drawEntryStartCenters.Remove(index);
        slot.Scale = Vector2.One;
        slot.Position = start.Position;
        slot.Rotation = 0f;
        return true;
    }

    private Vector2 NormalizeHandReorderStartPosition(Vector2 position)
    {
        position.Y = GetCurrentHandCardY();
        return position;
    }

    private float GetCurrentHandCardY()
    {
        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        float rowHeight = Math.Max(
            _cardRow != null && GodotObject.IsInstanceValid(_cardRow)
                ? _cardRow.Size.Y
                : cardSize.Y,
            cardSize.Y
        );
        return GetHandCardY(rowHeight, cardSize);
    }

    private void ClearDrawEntryState(int index, bool revealCard, bool hideTrail = true)
    {
        if (!IsCardIndexValid(index))
            return;

        if (hideTrail)
            HideCardDrawEntryTrail(index);

        bool wasHidden = _hiddenPendingDrawEntrySlotIndexes.Remove(index);
        _pendingDrawEntryAnimations.Remove(index);
        _drawEntrySlotIndexes.Remove(index);
        _drawEntrySlotOrders.Remove(index);
        _drawEntryStartCenters.Remove(index);
        if (_cardSlots[index] != null && GodotObject.IsInstanceValid(_cardSlots[index]))
            _cardSlots[index].Scale = Vector2.One;

        if (!revealCard || !wasHidden)
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.Visible = true;
    }

    private void HideCardDrawEntryTrail(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        HideCardDrawEntryTrail(_cards[index]);
    }

    private static void HideCardDrawEntryTrail(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Line trail = card.DrawTrail;
        if (trail != null && GodotObject.IsInstanceValid(trail))
        {
            trail.Visible = false;
            trail.ClearPoints();
            trail.Modulate = Colors.White;
            trail.Width = HandDrawTrailWidth;
            trail.ManualPreviewMode = false;
            if (trail.Target != null && GodotObject.IsInstanceValid(trail.Target))
                trail.Target.Visible = false;
        }

        HideCardTrailParticles(card.DrawTrailParticles);
    }

    private static void PrepareDrawEntryPreviewTrail(
        SkillCard card,
        out Line trail,
        out GpuParticles2D particles
    )
    {
        trail = null;
        particles = null;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Node2D target = card.DrawTrailTarget;
        trail = card.DrawTrail;
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
        trail.Width = HandDrawTrailWidth;
        trail.ClearPoints();

        particles = card.DrawTrailParticles;
        if (particles == null || !GodotObject.IsInstanceValid(particles))
            return;

        particles.Visible = true;
        particles.Modulate = Colors.White;
        particles.Emitting = false;
        particles.Restart();
        particles.Emitting = true;
    }

    private async Task FadeAndHideDrawEntryPreviewTrailAsync(Line trail, GpuParticles2D particles)
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
                    trail.Width = HandDrawTrailWidth;
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
        Vector2?[] previousSlotPositions = CaptureCardSlotPositions();
        float?[] previousSlotRotations = CaptureCardSlotRotations();
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || _activePlayer.State == Character.CharacterState.Dying
        )
        {
            ClearCardEnergyPreview();
            _statusLabel.Text = "等待行动";
            PositionStatusLabel();
            ClearLiftedCard(instant: true);
            ClearCardQueue(resetCards: false);
            ResetCardDisplayTracking();
            for (int i = 0; i < _cards.Length; i++)
            {
                if (_cards[i] == null)
                    continue;

                ResetCardMotion(i, instant: true);
                _cards[i].Visible = false;
                SetCardButtonInputEnabled(_cards[i], false);
            }

            if (_endTurnButton != null)
                _endTurnButton.Disabled = true;
            if (_drawPileButton != null)
            {
                _drawPileButton.Text = string.Empty;
                _drawPileButton.TooltipText = "抽牌堆";
                _drawPileButton.Disabled = true;
                SetPileButtonCount(_drawPileButton, 0);
                SyncPileButtonVisualState(_drawPileButton);
            }
            if (_discardPileButton != null)
            {
                _discardPileButton.Text = string.Empty;
                _discardPileButton.TooltipText = "弃牌堆";
                _discardPileButton.Disabled = true;
                SetPileButtonCount(_discardPileButton, 0);
                SyncPileButtonVisualState(_discardPileButton);
            }
            if (_exhaustedPileButton != null)
            {
                _exhaustedPileButton.Text = string.Empty;
                _exhaustedPileButton.TooltipText = "消耗牌堆";
                _exhaustedPileButton.Disabled = true;
                SetPileButtonCount(_exhaustedPileButton, 0);
                SyncPileButtonVisualState(_exhaustedPileButton);
            }
            return;
        }

        _statusLabel.Text = BuildPlayerTeamTurnStatusText();
        PositionStatusLabel();

        Skill[] hand = GetActiveHandSkills();
        bool hasAnyDrawEntryBusy = IsAnyCardDrawEntryBusy();
        bool manualTargetSelectionPending = IsManualTargetSelectionPending();
        bool canInteractWithHandCards =
            !_isResolvingCard
            && !_endTurnQueued
            && !_isPileCardSelectionActive
            && !manualTargetSelectionPending;

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null)
                continue;

            if (_turnEndStatusTriggerCardIndexes.Contains(i))
            {
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            if (IsCardCommitted(i))
            {
                card.Visible = true;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            Skill skill = hand != null && i < hand.Length ? hand[i] : null;
            if (skill == null)
            {
                _hiddenPendingDrawEntrySlotIndexes.Remove(i);
                card.Visible = false;
                SetCardButtonInputEnabled(card, false);
                card.SetPlayableHighlight(false, instant: true);
                ResetCardMotion(i, instant: true);
                ResetCardDisplayTracking(i);
                continue;
            }

            if (skill.OwnerCharater == null || !GodotObject.IsInstanceValid(skill.OwnerCharater))
                skill.OwnerCharater = _activePlayer;

            bool isNewCardForHand =
                !_cardDisplayInitialized[i] || !ReferenceEquals(_displayedSkills[i], skill);
            int previousSlotIndex = FindPreviousDisplayedSkillIndex(previousDisplayedSkills, skill);
            bool movedFromPendingReorder =
                isNewCardForHand && PreparePendingHandReorderMove(i, skill);
            bool movedFromAnotherSlot =
                isNewCardForHand && !movedFromPendingReorder && previousSlotIndex >= 0;
            bool shouldAnimate =
                isNewCardForHand && !movedFromPendingReorder && !movedFromAnotherSlot;
            bool shouldResetDisplayState = !card.Visible || isNewCardForHand;

            if (shouldAnimate)
                PrepareNewHandCardSlotForDrawEntry(i);
            else if (movedFromAnotherSlot)
                PrepareMovedHandCardSlotFromPreviousPosition(
                    i,
                    previousSlotIndex,
                    previousSlotPositions,
                    previousSlotRotations
                );
            bool hideForDrawEntry =
                _hiddenPendingDrawEntrySlotIndexes.Contains(i)
                || _pendingDrawEntryAnimations.Contains(i);
            if (shouldResetDisplayState)
            {
                card.RestoreDisplayState();
                SnapHandCardToBaseVisual(i, card);
            }
            card.Visible = !hideForDrawEntry;

            card.SetSkill(skill);
            string ownerDisplayName = GetSkillOwnerDisplayName(skill);
            if (card.CharacterName.Text != ownerDisplayName)
                card.CharacterName.Text = ownerDisplayName;

            _displayedSkills[i] = skill;
            _displayedSkillIds[i] = skill.SkillId;
            _cardDisplayInitialized[i] = true;

            if (hideForDrawEntry)
            {
                card.Visible = false;
                SetCardButtonInputEnabled(card, false);
                card.HoverHint.Visible = false;
                card.SetPlayableHighlight(false, instant: true);
                continue;
            }

            bool isArrowSelectedCard =
                _manualTargetArrowSelectionActive && i == _manualTargetArrowCardIndex;
            bool isDrawEntryBusy = IsCardDrawEntryBusy(i);
            bool canUseCurrentEnergy = skill.CanUseCurrentEnergy();
            bool isDiscardSelectionCandidate =
                _isDiscardSelectionActive
                && !_isDiscardSelectionCompleting
                && skill != null
                && !hasAnyDrawEntryBusy;
            bool canInteract =
                _liftedCardIndex != -1
                    ? i == _liftedCardIndex && canInteractWithHandCards
                    : isDiscardSelectionCandidate || canInteractWithHandCards;
            canInteract = canInteract && !isDrawEntryBusy;
            card.SetHoverUiEnabled(
                i == _liftedCardIndex
                || (_liftedCardIndex == -1 && !_manualTargetArrowSelectionActive)
            );
            bool shouldShowPlayableHighlight =
                !_isDiscardSelectionActive
                && !isArrowSelectedCard
                && canInteract
                && skill.CanBePlayed
                && canUseCurrentEnergy;
            SetCardButtonInputEnabled(card, canInteract && !isArrowSelectedCard);
            card.Modulate = SkillButton.EnabledModulate;
            card.SetEnergyCostAffordable(canUseCurrentEnergy);
            card.SetPlayableHighlight(shouldShowPlayableHighlight);
            if (isArrowSelectedCard)
                ApplyManualTargetArrowCardVisual(card);
        }

        if (_endTurnButton != null)
        {
            if (_isDiscardSelectionActive)
            {
                _endTurnButton.Text = _isDiscardSelectionCompleting
                    ? _discardSelectionExhaustMode
                        ? "消耗中"
                        : "丢弃中"
                    : "确认";
                _endTurnButton.Disabled =
                    _isDiscardSelectionCompleting
                    || _discardSelectionSkills.Count < _discardSelectionTargetCount;
            }
            else if (_isPileCardSelectionActive)
            {
                _endTurnButton.Text = "确认";
                _endTurnButton.Disabled =
                    _pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount;
            }
            else
            {
                _endTurnButton.Text = "结束回合";
                _endTurnButton.Disabled =
                    _isResolvingCard
                    || _endTurnQueued
                    || IsManualTargetSelectionPending();
            }
        }

        UpdatePileButtons();

        bool delayHandLayoutForShuffleDraw =
            updateLayout && !_freezeHandLayout && ShouldDelayHandLayoutForShuffleDraw();
        if (delayHandLayoutForShuffleDraw)
            ScheduleHandLayoutAfterShuffleDrawDelay();
        else if (updateLayout && !_freezeHandLayout)
            LayoutActionCards();
        if (
            _isDiscardSelectionActive
            && !_isDiscardSelectionCompleting
            && _discardSelectionSkills.Count > 0
        )
        {
            ArrangeDiscardSelectionSelectedCards();
        }
        if (_manualTargetArrowSelectionActive && IsCardIndexValid(_manualTargetArrowCardIndex))
        {
            ApplyManualTargetArrowCardVisual(_cards[_manualTargetArrowCardIndex]);
        }
    }

    private void SnapHandCardToBaseVisual(int index, SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.StopBattleMotion();
        card.HideHoverUi();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Position = Vector2.Zero;
        card.Scale = BattleCardScale;
        card.Rotation = 0f;
        card.HoverHint.Visible = false;

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot))
            slot.Scale = Vector2.One;
    }

    private int FindPreviousDisplayedSkillIndex(Skill[] previousDisplayedSkills, Skill skill)
    {
        if (skill == null || previousDisplayedSkills == null)
            return -1;

        for (int i = 0; i < previousDisplayedSkills.Length; i++)
        {
            if (ReferenceEquals(previousDisplayedSkills[i], skill))
                return i;
        }

        return -1;
    }

    private string BuildPlayerTeamTurnStatusText()
    {
        if (_isDiscardSelectionActive)
        {
            if (_isDiscardSelectionCompleting)
                return _discardSelectionExhaustMode ? "正在消耗所选牌" : "正在丢弃所选牌";

            int remaining = Math.Max(
                0,
                _discardSelectionTargetCount - _discardSelectionSkills.Count
            );
            if (_discardSelectionExhaustMode)
                return remaining > 0 ? $"选择{remaining}张牌消耗" : "确认消耗所选牌";

            return remaining > 0 ? $"选择{remaining}张牌丢弃" : "确认丢弃所选牌";
        }

        if (_isPileCardSelectionActive)
        {
            int remaining = Math.Max(
                0,
                _pileCardSelectionTargetCount - _pileCardSelectionIndexes.Count
            );
            string pileName = GetPileTitle(_pileCardSelectionKind);
            if (_pileCardSelectionAction == PileCardSelectionAction.Exhaust)
                return remaining > 0 ? $"从{pileName}选择{remaining}张牌消耗" : "正在消耗所选牌";

            return remaining > 0 ? $"从{pileName}选择{remaining}张牌加入手牌" : "正在加入手牌";
        }

        if (_isResolvingEndTurn)
            return "等待行动";

        int playerEnergy = BattleNode?.PlayerEnergy ?? 0;
        return $"玩家回合 | 能量 {playerEnergy}";
    }

    private static string GetSkillOwnerDisplayName(Skill skill)
    {
        if (skill?.IsStatusCard == true)
            return string.Empty;

        Character owner = skill?.OwnerCharater;
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return string.Empty;

        return GetCharacterEnergyDisplayName(owner);
    }

    private static string GetCharacterEnergyDisplayName(Character character, string suffix = null)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return suffix ?? string.Empty;

        string text = character.CharacterName;
        return string.IsNullOrWhiteSpace(suffix) ? text : $"{text} | {suffix}";
    }

    private async Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (_suppressCardButtonPressUntilLeftRelease && !allowSuppressedPress)
            return;

        if (_isDiscardSelectionActive)
        {
            await HandleDiscardSelectionCardPressedAsync(index);
            return;
        }

        if (
            _isResolvingCard
            || _endTurnQueued
            || _isPileCardSelectionActive
            || IsManualTargetSelectionPending()
            || _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || index < 0
            || index >= (GetActiveHandSkills()?.Length ?? 0)
            || IsCardDrawEntryBusy(index)
        )
        {
            return;
        }

        Skill skill = GetActiveHandSkills()?[index];
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

    private async Task HandleDiscardSelectionCardPressedAsync(int index)
    {
        if (
            !_isDiscardSelectionActive
            || _isDiscardSelectionCompleting
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
            || IsAnyCardDrawEntryBusy()
        )
        {
            return;
        }

        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && index < hand.Length ? hand[index] : null;
        if (skill == null)
            return;

        if (ConsumeDuplicateDiscardSelectionInput(index))
            return;

        ClearLiftedCard(instant: false);

        if (_discardSelectionSkills.Count >= _discardSelectionTargetCount)
            return;

        AddDiscardSelectionSelectedCard(index, skill);
        ArrangeDiscardSelectionSelectedCards();
        RefreshTurnUi();
        await Task.CompletedTask;
    }

    private bool ConsumeDuplicateDiscardSelectionInput(int index)
    {
        ulong now = Time.GetTicksMsec();
        if (
            _lastDiscardSelectionInputIndex == index
            && now >= _lastDiscardSelectionInputMsec
            && now - _lastDiscardSelectionInputMsec <= 50UL
        )
        {
            return true;
        }

        _lastDiscardSelectionInputIndex = index;
        _lastDiscardSelectionInputMsec = now;
        return false;
    }

    private void ResetDiscardSelectionInputGuard()
    {
        _lastDiscardSelectionInputIndex = -1;
        _lastDiscardSelectionInputMsec = 0UL;
    }

    private async Task CompleteDiscardSelectionAsync()
    {
        if (!_isDiscardSelectionActive || _isDiscardSelectionCompleting)
            return;

        _isDiscardSelectionCompleting = true;
        HideDiscardSelectionScreenMask();
        RefreshTurnUi();

        int selectedCount = _discardSelectionExhaustMode
            ? await ExhaustSelectedHandCardsAsync()
            : await DiscardSelectedHandCardsAsync();
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        _isDiscardSelectionActive = false;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionTargetCount = 0;
        _discardSelectionCompletion = null;
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionVisualState(returnCards: false);
        RefreshTurnUi();
        completion?.TrySetResult(selectedCount);
    }

    private void CancelDiscardSelection()
    {
        TaskCompletionSource<int> completion = _discardSelectionCompletion;
        _isDiscardSelectionActive = false;
        _isDiscardSelectionCompleting = false;
        _discardSelectionExhaustMode = false;
        _discardSelectionTargetCount = 0;
        _discardSelectionCompletion = null;
        ResetDiscardSelectionInputGuard();
        ClearDiscardSelectionVisualState(returnCards: true);
        HideDiscardSelectionScreenMask();
        completion?.TrySetResult(0);
    }

    private void AddDiscardSelectionSelectedCard(int index, Skill skill)
    {
        if (skill == null || !IsCardIndexValid(index))
            return;

        if (!_discardSelectionSkills.Contains(skill))
            _discardSelectionSkills.Add(skill);

        if (!_discardSelectionCards.ContainsKey(skill))
        {
            SkillCard previewCard = CreateDiscardSelectionPreviewCard(index, skill);
            if (previewCard == null)
                return;

            _discardSelectionCards[skill] = previewCard;
        }

        ClearDiscardSelectionHandSlotPreview(index);
        ClearActiveHandSkillForDiscardSelection(index, skill);

        SkillCard card = _discardSelectionCards[skill];
        card.StopBattleMotion();
        card.HoverHint.Visible = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.ZIndex = DiscardSelectionSelectedCardZIndex + _discardSelectionSkills.Count;
    }

    private void ClearDiscardSelectionHandSlotPreview(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        SkillCard handCard = _cards[index];
        if (handCard == null || !GodotObject.IsInstanceValid(handCard))
            return;

        handCard.HideHoverUi();
        handCard.HoverHint.Visible = false;
        handCard.StopBattleMotion();
        handCard.Modulate = SkillButton.EnabledModulate;
        handCard.Visible = false;
        SetCardButtonInputEnabled(handCard, false);
    }

    private SkillCard CreateDiscardSelectionPreviewCard(int index, Skill skill)
    {
        Control overlay = EnsureDiscardSelectionOverlay();
        if (skill == null || overlay == null || !GodotObject.IsInstanceValid(overlay))
            return null;

        Vector2 previewScale = GetDiscardSelectionCardScale();
        Vector2 globalPosition = GetDiscardSelectionPreviewStartPosition(index);
        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"DiscardSelectionCard{index}";
        card.ConfigureDisplayScale(previewScale);
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = true;
        card.PreviewCharacterName = skill.OwnerCharater?.CharacterName;
        card.PreviewCharacterKey = (skill.OwnerCharater as PlayerCharacter)?.CharacterKey;
        overlay.AddChild(card);

        card.SetSkill(skill);
        card.CharacterName.Text = GetSkillOwnerDisplayName(skill);
        card.Visible = true;
        card.GlobalPosition = globalPosition;
        card.Scale = previewScale;
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.MouseFilter = MouseFilterEnum.Stop;
        card.Button.MouseFilter = MouseFilterEnum.Stop;
        card.Button.Disabled = false;
        card.HoverHint.Visible = false;
        card.Button.Pressed += () =>
        {
            if (
                _isDiscardSelectionActive
                && !_isDiscardSelectionCompleting
                && _discardSelectionSkills.Contains(skill)
            )
            {
                MoveDiscardSelectionCardBack(skill, animateBack: true);
                ArrangeDiscardSelectionSelectedCards();
                RefreshTurnUi();
            }
        };
        return card;
    }

    private Vector2 GetDiscardSelectionPreviewStartPosition(int index)
    {
        SkillCard sourceCard = IsCardIndexValid(index) ? _cards[index] : null;
        if (sourceCard != null && GodotObject.IsInstanceValid(sourceCard) && sourceCard.Visible)
            return sourceCard.GlobalPosition;

        Control slot = IsCardIndexValid(index) ? _cardSlots[index] : null;
        if (slot != null && GodotObject.IsInstanceValid(slot))
            return slot.GlobalPosition;

        return GetDiscardSelectionSelectedPosition(GetDiscardSelectionCardScale(), 0, 1);
    }

    private void ArrangeDiscardSelectionSelectedCards()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        int count = selectedSkills.Length;
        Vector2 selectedScale = GetDiscardSelectionCardScale();
        for (int order = 0; order < count; order++)
        {
            Skill skill = selectedSkills[order];
            if (skill == null || !_discardSelectionCards.TryGetValue(skill, out SkillCard card))
                continue;
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            Vector2 targetScale = selectedScale;
            Vector2 targetPosition = GetDiscardSelectionSelectedPosition(
                selectedScale,
                order,
                count
            );
            card.ZIndex = DiscardSelectionSelectedCardZIndex + order;
            card.Button.Disabled = false;
            card.HoverHint.Visible = false;
            card.Modulate = SkillButton.EnabledModulate;

            Tween tween = card.CreateTween();
            tween.SetParallel(true);
            tween
                .TweenProperty(card, "global_position", targetPosition, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            tween
                .TweenProperty(card, "scale", targetScale, CardPlayMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }
    }

    private Vector2 GetDiscardSelectionSelectedPosition(Vector2 scale, int order, int count)
    {
        count = Math.Max(1, count);
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        Vector2 scaledSize = BattleCardBaseSize * scale;
        float availableWidth = Math.Max(0f, viewportSize.X - scaledSize.X - 96f);
        float step =
            count <= 1
                ? 0f
                : Math.Min(
                    scaledSize.X + DiscardSelectionSelectedGap,
                    Math.Min(DiscardSelectionSelectedMaxStep, availableWidth / (count - 1))
                );
        float x = viewportSize.X * 0.5f - scaledSize.X * 0.5f + (order - (count - 1) * 0.5f) * step;
        float y =
            viewportSize.Y * 0.5f - scaledSize.Y * 0.5f + DiscardSelectionSelectedVerticalOffset;
        return new Vector2(x, y);
    }

    private Vector2 GetDiscardSelectionCardScale()
    {
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            Control slot = _cardSlots[i];
            if (slot == null || !GodotObject.IsInstanceValid(slot))
                continue;

            Rect2 rect = slot.GetGlobalRect();
            if (rect.Size.X <= 1f || rect.Size.Y <= 1f)
                continue;

            return new Vector2(
                rect.Size.X / BattleCardBaseSize.X,
                rect.Size.Y / BattleCardBaseSize.Y
            );
        }

        return BattleCardScale;
    }

    private void MoveDiscardSelectionCardBack(Skill skill, bool animateBack)
    {
        if (skill == null)
            return;

        _discardSelectionCards.TryGetValue(skill, out SkillCard card);
        Vector2? returnStartPosition = GetDiscardSelectionReturnEntryStartPosition(card);
        if (card != null && GodotObject.IsInstanceValid(card))
            card.QueueFree();

        _discardSelectionCards.Remove(skill);
        int restoredIndex = -1;
        if (_discardSelectionSkills.Remove(skill))
            restoredIndex = RestoreActiveHandSkillFromDiscardSelection(skill);
        if (returnStartPosition.HasValue && IsCardIndexValid(restoredIndex))
            _customDrawEntryStartPositions[restoredIndex] = returnStartPosition.Value;
    }

    private Vector2? GetDiscardSelectionReturnEntryStartPosition(SkillCard previewCard)
    {
        if (
            previewCard == null
            || !GodotObject.IsInstanceValid(previewCard)
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return null;
        }

        return _cardRow.GetGlobalTransformWithCanvas().AffineInverse() * previewCard.GlobalPosition;
    }

    private void ClearActiveHandSkillForDiscardSelection(int index, Skill skill)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || index < 0 || index >= hand.Length)
            return;

        if (!ReferenceEquals(hand[index], skill))
            return;

        hand[index] = null;
        ResetCardDisplayTracking(index);
        InvalidateDiscardSelectionSkillOwner(skill);
    }

    private int RestoreActiveHandSkillFromDiscardSelection(Skill skill)
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null || skill == null)
            return -1;

        int restoreIndex = CompactHandInPlace(hand);
        if (restoreIndex < 0)
            return -1;

        if (skill.OwnerCharater == null || !GodotObject.IsInstanceValid(skill.OwnerCharater))
            skill.OwnerCharater = _activePlayer;
        skill.UpdateDescription();
        hand[restoreIndex] = skill;
        InvalidateDiscardSelectionSkillOwner(skill);
        return restoreIndex;
    }

    private static int CompactHandInPlace(Skill[] hand)
    {
        if (hand == null)
            return -1;

        int next = 0;
        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill == null)
                continue;

            if (next != i)
            {
                hand[next] = skill;
                hand[i] = null;
            }
            next++;
        }

        for (int i = next; i < hand.Length; i++)
            hand[i] = null;

        return next < hand.Length ? next : -1;
    }

    private void InvalidateDiscardSelectionSkillOwner(Skill skill)
    {
        if (skill?.OwnerCharater is PlayerCharacter owner)
            owner.InvalidateSkillTooltipCache();
        if (_activePlayer != null && GodotObject.IsInstanceValid(_activePlayer))
            _activePlayer.InvalidateSkillTooltipCache();
    }

    private void ClearDiscardSelectionSelectedCards()
    {
        ClearDiscardSelectionVisualState(returnCards: true);
    }

    private void ClearDiscardSelectionVisualState(bool returnCards)
    {
        if (returnCards)
        {
            foreach (Skill skill in _discardSelectionSkills.ToArray())
                MoveDiscardSelectionCardBack(skill, animateBack: false);
        }
        else
        {
            foreach (SkillCard card in _discardSelectionCards.Values.ToArray())
            {
                if (card != null && GodotObject.IsInstanceValid(card))
                    card.QueueFree();
            }
        }

        _discardSelectionCards.Clear();
        _discardSelectionSkills.Clear();
    }

    private void HideDiscardedHandCardAfterSelection(int index, SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.ResetState();
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.Position = Vector2.Zero;
        card.Scale = BattleCardScale;
        card.Modulate = SkillButton.EnabledModulate;
        card.Visible = false;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        card.ZIndex = 0;
        ResetCardDisplayTracking(index);
    }

    private Control EnsureDiscardSelectionOverlay()
    {
        Node parent = GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        const string overlayName = "DiscardSelectionOverlay";
        Control overlay =
            _discardSelectionOverlay != null
            && GodotObject.IsInstanceValid(_discardSelectionOverlay)
                ? _discardSelectionOverlay
                : parent.GetNodeOrNull<Control>(overlayName);

        if (overlay == null)
        {
            overlay = new Control
            {
                Name = overlayName,
                MouseFilter = MouseFilterEnum.Ignore,
                ZIndex = DiscardSelectionOverlayZIndex,
                TopLevel = false,
            };
            overlay.SetAnchorsPreset(LayoutPreset.FullRect);
            if (parent is Control parentControl)
                overlay.Size = parentControl.Size;
            else
                overlay.Size = GetViewport().GetVisibleRect().Size;
            parent.AddChild(overlay);
        }

        overlay.Visible = true;
        overlay.ZIndex = DiscardSelectionOverlayZIndex;
        overlay.MouseFilter = MouseFilterEnum.Ignore;
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.Size = GetViewport().GetVisibleRect().Size;
        if (
            _endTurnButton != null
            && GodotObject.IsInstanceValid(_endTurnButton)
            && _endTurnButton.GetParent() is Node actionButtonsRoot
            && actionButtonsRoot.GetParent() == parent
        )
        {
            int buttonIndex = actionButtonsRoot.GetIndex();
            parent.MoveChild(overlay, Math.Max(0, buttonIndex));
        }
        _discardSelectionOverlay = overlay;
        return overlay;
    }

    private void EnsureDiscardSelectionScreenMask()
    {
        if (
            _discardSelectionScreenMask != null
            && GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            _discardSelectionScreenMask.Visible = true;
            return;
        }

        Node parent = EnsureCardPlayOverlay();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return;

        _discardSelectionScreenMask = new ColorRect
        {
            Name = "DiscardSelectionScreenMask",
            Color = new Color(0f, 0f, 0f, 0.46f),
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = DiscardSelectionOverlayZIndex - 1,
        };
        _discardSelectionScreenMask.SetAnchorsPreset(LayoutPreset.FullRect);
        _discardSelectionScreenMask.Size = GetViewport().GetVisibleRect().Size;
        parent.AddChild(_discardSelectionScreenMask);
    }

    private void HideDiscardSelectionScreenMask()
    {
        if (
            _discardSelectionScreenMask != null
            && GodotObject.IsInstanceValid(_discardSelectionScreenMask)
        )
        {
            _discardSelectionScreenMask.Visible = false;
            _discardSelectionScreenMask.QueueFree();
        }

        _discardSelectionScreenMask = null;
    }

    private async Task<int> DiscardSelectedHandCardsAsync()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        if (selectedSkills.Length == 0)
            return 0;

        var entries = new List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)>();
        foreach (Skill skill in selectedSkills)
        {
            if (skill == null)
                continue;

            _discardSelectionCards.TryGetValue(skill, out SkillCard sourceCard);
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
            {
                continue;
            }

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter ?? _activePlayer;
            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;
            sourceCard.Modulate = SkillButton.EnabledModulate;
            sourceCard.ZIndex = PlayedCardZIndex + entries.Count + 1;
            entries.Add((skill, owner, sourceCard));
        }

        if (entries.Count == 0)
        {
            foreach (Skill skill in selectedSkills)
                RestoreActiveHandSkillFromDiscardSelection(skill);
            return 0;
        }

        foreach (var entry in entries)
        {
            entry.Card.Button.Disabled = true;
            entry.Card.HoverHint.Visible = false;
        }

        var flyTasks = new List<Task>();
        bool previousFreezeHandLayout = _freezeHandLayout;
        _freezeHandLayout = true;
        try
        {
            foreach (var entry in entries)
            {
                if (entry.Card == null)
                {
                    entry.Card?.Vanish();
                    continue;
                }

                entry.Card.ZIndex = PlayedCardZIndex + flyTasks.Count + 1;
                flyTasks.Add(PlayCardDiscardFlyAsync(entry.Card));
            }

            if (flyTasks.Count > 0)
                await Task.WhenAll(flyTasks);

            foreach (var entry in entries)
            {
                BattleNode?.DiscardBattleSkill(entry.Owner, entry.Skill, forceDiscard: true);
            }
            CompactActiveHandAfterDiscardSelection();

            foreach (var entry in entries)
            {
                if (entry.Card != null && GodotObject.IsInstanceValid(entry.Card))
                    entry.Card.QueueFree();
            }
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
        }

        return entries.Count;
    }

    private async Task<int> ExhaustSelectedHandCardsAsync()
    {
        Skill[] selectedSkills = _discardSelectionSkills.ToArray();
        if (selectedSkills.Length == 0)
            return 0;

        var entries = new List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)>();
        foreach (Skill skill in selectedSkills)
        {
            if (skill == null)
                continue;

            _discardSelectionCards.TryGetValue(skill, out SkillCard sourceCard);
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
            {
                continue;
            }

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter ?? _activePlayer;
            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;
            sourceCard.Modulate = SkillButton.EnabledModulate;
            sourceCard.ZIndex = PlayedCardZIndex + entries.Count + 1;
            entries.Add((skill, owner, sourceCard));
        }

        if (entries.Count == 0)
        {
            foreach (Skill skill in selectedSkills)
                RestoreActiveHandSkillFromDiscardSelection(skill);
            return 0;
        }

        bool previousFreezeHandLayout = _freezeHandLayout;
        _freezeHandLayout = true;
        try
        {
            bool playedAny = false;
            int order = 0;
            foreach (var entry in entries)
            {
                if (entry.Card == null)
                    continue;

                entry.Card.ZIndex = PlayedCardZIndex + order + 1;
                entry.Card.PlayExhaustEffect(CardPlayVanishDuration);
                playedAny = true;
                order++;
            }

            if (playedAny)
            {
                await ToSignal(
                    GetTree().CreateTimer(CardPlayVanishDuration),
                    SceneTreeTimer.SignalName.Timeout
                );
            }

            BattleNode?.ExhaustPlayerTeamBattleSkills(
                entries.Select(entry => entry.Skill),
                _activePlayer
            );
            CompactActiveHandAfterDiscardSelection();

            foreach (var entry in entries)
            {
                if (entry.Card != null && GodotObject.IsInstanceValid(entry.Card))
                    entry.Card.QueueFree();
            }
        }
        finally
        {
            _freezeHandLayout = previousFreezeHandLayout;
        }

        return entries.Count;
    }

    private void CompactActiveHandAfterDiscardSelection()
    {
        Skill[] hand = GetActiveHandSkills();
        if (hand == null)
            return;

        CompactHandInPlace(hand);

        foreach (
            PlayerCharacter owner in hand.Select(skill => skill?.OwnerCharater)
                .OfType<PlayerCharacter>()
                .Distinct()
        )
        {
            owner.InvalidateSkillTooltipCache();
        }
        _activePlayer?.InvalidateSkillTooltipCache();
    }

    private async Task SelectManualTargetFromHandAndQueueAsync(int index, Skill skill)
    {
        if (
            skill == null
            || !IsCardIndexValid(index)
            || IsCardCommitted(index)
            || IsCardDrawEntryBusy(index)
        )
        {
            return;
        }

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

    private void SuppressHandHoverUntilMouseMove()
    {
        _suppressHandHoverUntilMouseMove = true;
        _handHoverSuppressionMousePosition = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
        _deferredHoverRefreshVersion++;
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
            || IsCardDrawEntryBusy(index)
        )
        {
            return;
        }

        if (!keepManualFriendlyTarget)
            skill.ClearManualFriendlyTarget();

        Character actor = skill.OwnerCharater ?? _activePlayer;
        bool hadStun = HasActiveStun(actor);
        if (!hadStun && !skill.TrySpendDisplayedEnergy())
        {
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        SkillCard sourceCard = _cards[index];
        PrepareHandVisualsBeforeQueuedPlay(index, sourceCard);

        SkillCard card = DetachHandCardForPlay(index);
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            skill.RefundDisplayedEnergy();
            ResetCardMotion(index, instant: false);
            RefreshTurnUi();
            return;
        }

        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.Modulate = QueuedCardModulate;
        card.ZIndex = PlayedCardZIndex + _queuedCardPlays.Count + 1;
        card.StopBattleMotion();

        var play = new QueuedCardPlay
        {
            Actor = actor,
            Index = index,
            Skill = skill,
            SkillId = skill.SkillId,
            SkillType = skill.SkillType,
            HadStun = hadStun,
            Card = card,
            IsHandCard = true,
            MoveToCenterBeforeEffect = true,
        };

        _queuedCardPlays.Enqueue(play);

        ResolveHandSlotAfterQueuedPlay(play);
        RefreshQueuedPlayCardLayers();
        RefreshTurnUi();
        _ = ProcessCardQueueAsync();
    }

    private void PrepareHandVisualsBeforeQueuedPlay(int playedIndex, SkillCard playedCard)
    {
        if (_liftedCardIndex == playedIndex)
            _liftedCardIndex = -1;

        _suppressCardButtonPressUntilLeftRelease = false;
        SetHandInputBlockerVisible(false);
        if (!_manualTargetArrowSelectionActive)
            SetCardHoverUiEnabled(true);

        if (_hoveredCardIndex == playedIndex)
            _hoveredCardIndex = -1;
        else if (_hoveredCardIndex != -1)
            ClearHandCardHoverMotion(_hoveredCardIndex, instant: false);

        ClearAllHandCardHoverMotionExcept(playedIndex, instant: false);
        ClearCardEnergyPreview();
        HideCardHoverPreview(playedIndex);

        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
        {
            playedCard.HideHoverUi();
            playedCard.HoverHint.Visible = false;
        }

        SuppressHandHoverUntilMouseMove();
        UpdateProcessState();
    }

    private SkillCard DetachHandCardForPlay(int index)
    {
        if (!IsCardIndexValid(index))
            return null;

        SkillCard card = _cards[index];
        Control slot = _cardSlots[index];
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || slot == null
            || !GodotObject.IsInstanceValid(slot)
            || overlay == null
            || !GodotObject.IsInstanceValid(overlay)
        )
        {
            return null;
        }

        Vector2 globalPosition = card.GlobalPosition;
        Vector2 scale = card.Scale;
        float rotation = card.Rotation;
        Vector2 pivotOffset = card.PivotOffset;

        _cardSlotLayoutTweens[index]?.Kill();
        _cardSlotLayoutTweens[index] = null;
        _cardSlotLayoutTargets[index] = null;
        _cardSlotLayoutRotationTargets[index] = null;
        _cardSlotLayoutFollowActive[index] = false;
        _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;

        Node parent = card.GetParent();
        parent?.RemoveChild(card);
        overlay.AddChild(card);

        card.GlobalPosition = globalPosition;
        card.Scale = scale;
        card.Rotation = rotation;
        card.PivotOffset = pivotOffset;
        card.MouseFilter = MouseFilterEnum.Ignore;
        SetCardButtonInputEnabled(card, false);
        card.HoverHint.Visible = false;
        card.ZIndex = PlayedCardZIndex + _queuedCardPlays.Count + 1;
        card.StopBattleMotion();

        SkillCard replacement = CreateBattleCard(index);
        replacement.RestoreDisplayState();
        replacement.PivotOffset = BattleCardBaseSize * 0.5f;
        replacement.Position = Vector2.Zero;
        replacement.Scale = BattleCardScale;
        replacement.Rotation = 0f;
        replacement.Visible = false;
        slot.AddChild(replacement);
        WireBattleCard(replacement, index);
        _cards[index] = replacement;

        return card;
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
            _freezeHandLayout = false;
            return Task.CompletedTask;
        }

        skill.OwnerCharater = actor;
        skill.UpdateDescription();

        bool hadStun = HasActiveStun(actor);
        SkillCard card = CreateTemporaryPlayCard(actor, skill, "连携", "CarryCard");
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
            IsTemporaryCard = card != null,
            FlyToDiscardPileAfterUse = card != null,
            FreeEnergyCost = true,
            Completion = completion,
        };
        if (card != null)
            play.MoveToCenterTask = ShowTemporaryCardAtCenterAsync(play, "连携");

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
                _activeQueuedCardPlay = play?.IsHandCard == true ? play : null;
                RefreshQueuedPlayCardLayers();

                bool shouldStop;
                try
                {
                    shouldStop = await ExecuteQueuedCardAsync(play);
                }
                catch
                {
                    RecoverInterruptedQueuedPlay(play);
                    throw;
                }
                finally
                {
                    if (_activeQueuedCardPlay == play)
                    {
                        _activeQueuedCardPlay = null;
                        RefreshQueuedPlayCardLayers();
                    }
                }

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
                {
                    RefreshTurnUi();
                    ScheduleCardHoverRefresh();
                }
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
            || (
                play.IsHandCard
                && (_activePlayer == null || !GodotObject.IsInstanceValid(_activePlayer))
            )
        )
        {
            play?.Skill?.RefundDisplayedEnergy();
            RestoreHandSlotForQueuedPlay(play);
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play?.IsHandCard == true;
        }

        if (play.IsHandCard)
            RefreshTurnUi();

        if (!await SelectManualFriendlyTargetIfNeededAsync(play))
        {
            play.Skill.RefundDisplayedEnergy();
            RestoreHandSlotForQueuedPlay(play);
            QueueFreeQueuedPlayCard(play);
            CompleteQueuedPlay(play, succeeded: false);
            return play.IsHandCard;
        }

        if (play.MoveToCenterTask != null)
            await play.MoveToCenterTask;
        else if (play.IsHandCard && play.MoveToCenterBeforeEffect)
            await MoveHandPlayCardToCenterAsync(play);

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

        ResolveBattlePileAfterQueuedPlay(play);

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
            if (play.Actor != _activePlayer)
            {
                CompleteQueuedPlay(play, succeeded: true);
                RefreshTurnUi();
                return false;
            }

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
        if (play?.IsHandCard != true)
            return;

        if (play.RemovedFromHand)
            return;

        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        BattleNode.RemovePlayerTeamBattleHandCardAt(play.Index);
        ResetCardDisplayTracking(play.Index);
        play.RemovedFromHand = true;
    }

    private void RestoreHandSlotForQueuedPlay(QueuedCardPlay play)
    {
        if (play?.IsHandCard != true || !play.RemovedFromHand || play.ResolvedToBattlePile)
        {
            return;
        }

        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        if (BattleNode.TryRestorePlayerTeamBattleHandCardAt(play.Index, play.Skill))
        {
            _queuedCardIndices.Remove(play.Index);
            play.RemovedFromHand = false;
            RefreshTurnUi();
        }
    }

    private void ResolveBattlePileAfterQueuedPlay(QueuedCardPlay play)
    {
        if (
            play?.IsHandCard != true
            || play.ResolvedToBattlePile
            || play.Actor == null
            || !GodotObject.IsInstanceValid(play.Actor)
            || play.Skill == null
        )
        {
            return;
        }

        BattleNode?.DiscardBattleSkill(play.Actor, play.Skill);
        play.ResolvedToBattlePile = true;
    }

    private void RecoverInterruptedQueuedPlay(QueuedCardPlay play)
    {
        if (play == null)
            return;

        if (!play.ResolvedToBattlePile)
            RestoreHandSlotForQueuedPlay(play);

        if (play.IsHandCard)
        {
            if (!play.ResolvedToBattlePile)
                ResetCardMotion(play.Index, instant: true);
        }

        QueueFreeQueuedPlayCard(play);
        CompleteQueuedPlay(play, succeeded: false);
    }

    private bool IsHandPlayStillValid(QueuedCardPlay play)
    {
        return play?.Actor != null
            && GodotObject.IsInstanceValid(play.Actor)
            && play.Actor.BattleNode != null
            && GodotObject.IsInstanceValid(play.Actor.BattleNode)
            && _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer);
    }

    private void CompleteQueuedPlay(QueuedCardPlay play, bool succeeded)
    {
        if (play?.IsHandCard == true)
            _queuedCardIndices.Remove(play.Index);

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
        else if (play?.IsHandCard == true || play?.FlyToDiscardPileAfterUse == true)
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

    public async Task PlayEndTurnHandDiscardAnimationsAsync(
        PlayerCharacter player,
        HashSet<int> handIndexes = null
    )
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        var animationTasks = new List<Task>();
        var discardEntries = new List<(int Index, SkillCard Card)>();
        var exhaustCards = new List<(int Index, SkillCard Card)>();

        for (int i = 0; i < _cards.Length; i++)
        {
            if (handIndexes != null && !handIndexes.Contains(i))
                continue;

            SkillCard sourceCard = _cards[i];
            if (
                sourceCard == null
                || !GodotObject.IsInstanceValid(sourceCard)
                || !sourceCard.Visible
            )
                continue;

            sourceCard.Button.Disabled = true;
            sourceCard.HoverHint.Visible = false;

            Skill skill = sourceCard.CurrentSkill;
            Skill[] hand = GetActiveHandSkills();
            if (skill == null && hand != null && i < hand.Length)
                skill = hand[i];

            if (skill?.ExhaustsAtTurnEndInHand == true)
            {
                SkillCard animationCard = CreateEndTurnHandDiscardAnimationCard(
                    i,
                    sourceCard,
                    skill,
                    PlayedCardZIndex + i + 1
                );
                if (animationCard != null)
                    exhaustCards.Add((i, animationCard));
                HideDiscardedHandCardAfterSelection(i, sourceCard);
                continue;
            }

            SkillCard discardCard = CreateEndTurnHandDiscardAnimationCard(
                i,
                sourceCard,
                skill,
                PlayedCardZIndex + i + 1
            );
            if (discardCard != null)
                discardEntries.Add((i, discardCard));
            HideDiscardedHandCardAfterSelection(i, sourceCard);
        }

        foreach (var entry in exhaustCards)
        {
            SkillCard card = entry.Card;
            card.PlayExhaustEffect(EndTurnCardVanishDuration);
            animationTasks.Add(FreeCardAfterDelayAsync(card, EndTurnCardVanishDuration));
        }

        for (int i = 0; i < discardEntries.Count; i++)
        {
            var entry = discardEntries[i];
            entry.Card.Button.Disabled = true;
            entry.Card.HoverHint.Visible = false;

            entry.Card.ZIndex = PlayedCardZIndex + i + 1;
            animationTasks.Add(PlayCardDiscardFlyAndFreeAsync(entry.Card));
        }

        if (animationTasks.Count > 0)
            await Task.WhenAll(animationTasks);
    }

    private SkillCard CreateEndTurnHandDiscardAnimationCard(
        int index,
        SkillCard sourceCard,
        Skill skill,
        int zIndex
    )
    {
        CanvasLayer overlay = EnsureCardPlayOverlay();
        if (
            overlay == null
            || !GodotObject.IsInstanceValid(overlay)
            || sourceCard == null
            || !GodotObject.IsInstanceValid(sourceCard)
            || skill == null
        )
        {
            return null;
        }

        SkillCard card = SkillCardScene.Instantiate<SkillCard>();
        card.Name = $"EndTurnDiscardCard{index}";
        card.AutoPressEffect = false;
        card.UseDefaultHoverEffect = false;
        card.HoverUiEnabled = false;
        card.ConfigureDisplayScale(BattleCardScale);
        overlay.AddChild(card);
        card.ResetState();
        card.SetSkill(skill);
        card.CharacterName.Text = sourceCard.CharacterName.Text;
        card.Visible = true;
        card.GlobalPosition = sourceCard.GlobalPosition;
        card.Scale = sourceCard.Scale;
        card.Rotation = sourceCard.Rotation;
        card.PivotOffset = sourceCard.PivotOffset;
        card.MouseFilter = MouseFilterEnum.Ignore;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = zIndex;
        return card;
    }

    private async Task PlayCardDiscardFlyAndFreeAsync(SkillCard card)
    {
        try
        {
            await PlayCardDiscardFlyAsync(card);
        }
        finally
        {
            FreeAnimationCard(card);
        }
    }

    private async Task FreeCardAfterDelayAsync(SkillCard card, float delay)
    {
        try
        {
            if (delay > 0f && IsInsideTree())
            {
                await ToSignal(
                    GetTree().CreateTimer(delay),
                    SceneTreeTimer.SignalName.Timeout
                );
            }
        }
        finally
        {
            FreeAnimationCard(card);
        }
    }

    private static void FreeAnimationCard(SkillCard card)
    {
        if (card != null && GodotObject.IsInstanceValid(card))
            card.QueueFree();
    }

    public async Task PlayTurnEndStatusTriggerAnimationAsync(
        PlayerCharacter player,
        int handIndex,
        Skill skill,
        Func<Task> triggerEffect
    )
    {
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || skill?.TriggersAtTurnEndInHand != true
        )
        {
            if (triggerEffect != null)
                await triggerEffect();
            return;
        }

        SkillCard card = null;
        bool isHandCard = false;
        if (CanAnimateHandCardsFor(player) && IsCardIndexValid(handIndex))
        {
            SkillCard sourceCard = _cards[handIndex];
            if (sourceCard != null && GodotObject.IsInstanceValid(sourceCard) && sourceCard.Visible)
            {
                card = sourceCard;
                isHandCard = true;
            }
        }

        if (isHandCard)
            _turnEndStatusTriggerCardIndexes.Add(handIndex);

        try
        {
            if (card != null)
            {
                var play = new QueuedCardPlay
                {
                    Actor = player,
                    Skill = skill,
                    Card = card,
                };
                card.SetEnergyCostText(I18n.Tr("ui.status.trigger", "触发"));
                card.CharacterName.Text = string.Empty;
                await MoveHandPlayCardToCenterAsync(play);
            }
            else
            {
                if (triggerEffect != null)
                    await triggerEffect();
                return;
            }

            if (triggerEffect != null)
                await triggerEffect();

            await PlayCardDiscardFlyAsync(card);
            if (isHandCard)
                HideTriggeredStatusHandCard(handIndex);
        }
        finally
        {
            if (isHandCard)
                _turnEndStatusTriggerCardIndexes.Remove(handIndex);
        }
    }

    private void HideTriggeredStatusHandCard(int handIndex)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        SkillCard card = _cards[handIndex];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.ResetState();
        card.Rotation = 0f;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        ResetCardMotion(handIndex, instant: true);
        card.Visible = false;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
    }

    private async Task PlayCardDiscardFlyAsync(SkillCard card)
    {
        await PlayCardFlyToPileAsync(card, _discardPileButton);
    }

    private async Task PlayCardFlyToPileAsync(SkillCard card, Button pileButton)
    {
        if (
            card == null
            || !GodotObject.IsInstanceValid(card)
            || pileButton == null
            || !GodotObject.IsInstanceValid(pileButton)
            || !pileButton.IsInsideTree()
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
        Vector2 startCenter = startRect.Position + startRect.Size * 0.5f;
        Vector2 endCenter = GetPileButtonVisualCenter(pileButton);
        if (startCenter.DistanceSquaredTo(endCenter) < 16f)
        {
            card.PressEffect();
            await ToSignal(
                GetTree().CreateTimer(CardPlayVanishDuration),
                SceneTreeTimer.SignalName.Timeout
            );
            return;
        }

        card.PivotOffset = BattleCardBaseSize * 0.5f;
        Tween compressShaderTween = card.PressEffectPartial(
            centerVanish: 0.92f,
            glowMultiplier: 1.28f,
            duration: CardPlayDiscardCompressDuration
        );

        Tween compressTween = card.CreateTween();
        compressTween.SetParallel(true);
        compressTween
            .TweenProperty(
                card,
                "scale",
                Vector2.One * CardPlayDiscardCompressedScaleFactor,
                CardPlayDiscardCompressDuration
            )
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
        await Task.WhenAll(
            WaitForTweenFinishedAsync(compressTween),
            WaitForTweenFinishedAsync(compressShaderTween)
        );

        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 flyStartCenter = startCenter;
        PrepareCardDiscardTrail(card, out Line trail, out GpuParticles2D particles);
        Vector2 control = GetRandomCardDiscardControlPoint(flyStartCenter, endCenter);
        Vector2 initialVelocity = GetQuadraticBezierVelocity(
            flyStartCenter,
            control,
            endCenter,
            0.01f
        );
        card.Rotation = GetRotationWithTopFacingVelocity(initialVelocity);
        UpdateTrailParticlesRotation(particles, initialVelocity);
        Tween flyShaderTween = card.PressEffectPartial(
            centerVanish: 0.9f,
            glowMultiplier: 1.18f,
            duration: CardPlayDiscardFlyDuration
        );

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(
                card,
                "scale",
                Vector2.One * CardPlayDiscardTargetScaleFactor,
                CardPlayDiscardFlyDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
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
        tween.SetParallel(false);

        await Task.WhenAll(
            WaitForTweenFinishedAsync(tween),
            WaitForTweenFinishedAsync(flyShaderTween)
        );
        if (card != null && GodotObject.IsInstanceValid(card))
        {
            card.SetCardVisualVisible(false);
            card.Button.Disabled = true;
            card.HoverHint.Visible = false;
        }

        PulsePileButtonReceive(pileButton);
        await FadeAndHideCardDiscardTrailAsync(trail, particles);
    }

    private static void SetCardPivotCenterAt(SkillCard card, Vector2 center)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 currentPivotCenter = card.GetGlobalTransformWithCanvas() * card.PivotOffset;
        card.GlobalPosition += center - currentPivotCenter;
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
            Math.Min(330f, Math.Max(120f, distance * 0.28f)) + (float)GD.RandRange(-28f, 52f);
        float side = end.X >= start.X ? 1f : -1f;
        float sideOffset = side * Math.Min(190f, distance * 0.16f) + (float)GD.RandRange(-90f, 90f);
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
        card.CharacterName.Text = GetCharacterEnergyDisplayName(actor, tag);
        return card;
    }

    private CanvasLayer EnsureCardPlayOverlay()
    {
        Node parent = BattleNode ?? FindBattleNode() ?? GetParent();
        if (parent == null)
            return null;

        const string overlayName = "CardPlayOverlay";
        CanvasLayer overlay = parent.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null)
            return overlay;

        var root = GetTree()?.Root;
        overlay = root?.GetNodeOrNull<CanvasLayer>(overlayName);
        if (overlay != null && overlay.GetParent() != parent)
        {
            overlay.Reparent(parent);
            return overlay;
        }

        overlay = new CanvasLayer { Name = overlayName };
        parent.AddChild(overlay);
        return overlay;
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
        card.CharacterName.Text = GetCharacterEnergyDisplayName(play.Actor, tag);
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = TemporaryCardZIndex;
        card.Scale = PlayedCardScale * TemporaryCardSpawnScaleMultiplier;
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

    private async Task MoveHandPlayCardToCenterAsync(QueuedCardPlay play)
    {
        await MoveHandPlayCardToLayerAsync(play, 0);
    }

    private void RefreshQueuedPlayCardLayers()
    {
        var plays = new List<QueuedCardPlay>();
        if (_activeQueuedCardPlay?.IsHandCard == true)
            plays.Add(_activeQueuedCardPlay);

        plays.AddRange(_queuedCardPlays.Where(play => play?.IsHandCard == true));
        plays.AddRange(_queuedFollowUpCardPlays.Where(play => play?.IsHandCard == true));

        int layerIndex = 0;
        foreach (QueuedCardPlay play in plays)
        {
            if (
                play == null
                || play.Card == null
                || !GodotObject.IsInstanceValid(play.Card)
                || play.ResolvedToBattlePile
            )
            {
                continue;
            }

            play.MoveToCenterTask = MoveHandPlayCardToLayerAsync(play, layerIndex++);
        }
    }

    private async Task MoveHandPlayCardToLayerAsync(QueuedCardPlay play, int layerIndex)
    {
        SkillCard card = play?.Card;
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        layerIndex = Math.Max(0, layerIndex);
        Vector2 targetScale = GetQueuedPlayCardLayerScale(layerIndex);
        Vector2 targetPosition = GetQueuedPlayCardLayerPosition(targetScale, layerIndex);

        card.Visible = true;
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = GetQueuedPlayCardLayerZIndex(layerIndex);
        card.Modulate = GetQueuedPlayCardLayerModulate(layerIndex);
        card.StopBattleMotion();

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "scale", targetScale, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "global_position", targetPosition, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private static Vector2 GetQueuedPlayCardLayerScale(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        float scaleMultiplier = Math.Max(
            0.62f,
            1f - visibleLayerIndex * QueuedPlayedCardLayerScaleStep
        );
        return PlayedCardScale * scaleMultiplier;
    }

    private Vector2 GetQueuedPlayCardLayerPosition(Vector2 scale, int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        return GetScreenCenterCardPosition(scale)
            + new Vector2(0f, QueuedPlayedCardVerticalOffset)
            + QueuedPlayedCardLayerOffset * visibleLayerIndex;
    }

    private static int GetQueuedPlayCardLayerZIndex(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        return PlayedCardZIndex + QueuedPlayedCardVisibleLayers - visibleLayerIndex;
    }

    private static Color GetQueuedPlayCardLayerModulate(int layerIndex)
    {
        int visibleLayerIndex = Math.Min(
            Math.Max(layerIndex, 0),
            QueuedPlayedCardVisibleLayers - 1
        );
        float alpha = Mathf.Clamp(1f - visibleLayerIndex * 0.1f, 0.68f, 1f);
        return new Color(1f, 1f, 1f, alpha);
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
        if (play?.IsTemporaryCard == true || play?.IsHandCard == true)
            QueueFreeTemporaryCard(play.Card);
    }

    private void LiftCard(int index)
    {
        if (!IsCardIndexValid(index) || IsCardDrawEntryBusy(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || card.Button.Disabled)
            return;

        _suppressCardButtonPressUntilLeftRelease = Input.IsMouseButtonPressed(MouseButton.Left);
        _liftedCardIndex = index;
        _liftedCardMouseOffset = GetViewport().GetMousePosition() - card.GlobalPosition;
        SetHandInputBlockerVisible(true);
        BlockOtherHandCardInputWhileLifted(index);

        if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            ResetCardMotion(_hoveredCardIndex, instant: false);
        _hoveredCardIndex = -1;

        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
        {
            _cardSlotLayoutTweens[index]?.Kill();
            _cardSlotLayoutTweens[index] = null;
            _cardSlotLayoutTargets[index] = null;
            _cardSlotLayoutRotationTargets[index] = null;
            _cardSlotLayoutFollowActive[index] = false;
            _cardSlotLayoutPixelsPerSecondOverrides[index] = 0f;
            slot.Rotation = 0f;
        }

        card.StopBattleMotion();
        card.HoverHint.Visible = true;
        SetCardHoverPreviewActive(index, true);
        card.ZIndex = PlayedCardZIndex;
        SetProcess(true);
        LayoutActionCards(instant: false);
        UpdateLiftedCardPosition();
    }

    private void ClearLiftedCard(bool instant)
    {
        if (_liftedCardIndex == -1)
        {
            UpdateProcessState();
            return;
        }

        int index = _liftedCardIndex;
        Skill[] hand = GetActiveHandSkills();
        Skill skill = hand != null && index < hand.Length ? hand[index] : null;
        skill?.ClearManualFriendlyTarget();
        SyncLiftedSlotPositionToCard(index);
        _liftedCardIndex = -1;
        _suppressCardButtonPressUntilLeftRelease = false;
        SetHandInputBlockerVisible(false);
        if (!_manualTargetArrowSelectionActive)
            SetCardHoverUiEnabled(true);
        _cardSlotLayoutPixelsPerSecondOverrides[index] = instant
            ? 0f
            : Math.Max(HandLayoutPixelsPerSecond, HandDroppedCardReturnPixelsPerSecond);
        UpdateProcessState();
        ResetCardMotion(index, instant, HandDroppedCardReturnMotionDuration);
        LayoutActionCards(instant);
        RefreshTurnUi();
    }

    private void BlockOtherHandCardInputWhileLifted(int liftedIndex)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == liftedIndex)
                continue;

            SkillCard card = _cards[i];
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            ClearHandCardHoverMotion(i, instant: true);
            card.SetHoverUiEnabled(false);
            SetCardButtonInputEnabled(card, false);
        }
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

        Character[] candidates = GetManualFriendlyTargetCandidates(play.Skill);
        if (candidates.Length == 0)
            return true;

        if (play.IsTemporaryCard)
        {
            if (play.Skill.TryGetManualFriendlyCarrySkillType(out Skill.SkillTypes carrySkillType))
            {
                Character[] drawableCandidates = candidates
                    .Where(candidate =>
                        candidate is PlayerCharacter player
                        && BattleNode?.HasDrawablePlayerCarrySkill(player, carrySkillType) == true
                    )
                    .ToArray();
                if (drawableCandidates.Length > 0)
                    candidates = drawableCandidates;
            }

            Random rng = BattleNode?.BattleIntentionRandom ?? new Random();
            Character randomTarget = candidates[rng.Next(candidates.Length)];
            play.Skill.SetManualFriendlyTarget(randomTarget);
            return play.Skill.HasManualFriendlyTarget();
        }

        bool shouldUseArrowSelection =
            !play.ForceManualTargetCardPicker && ShouldUseManualTargetArrowSelection(play.Skill);

        Character target = shouldUseArrowSelection
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
            && skill?.RequiresManualFriendlyTarget() == true;
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
        ShowManualRebirthTargetPreviews(skill, targets);

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        _manualTargetArrowTargets = targets;
        _manualTargetArrowOwner = owner;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = skill;
        _manualTargetArrowCardIndex = sourceCardIndex;
        _manualTargetArrowSourcePosition = null;
        _manualTargetArrowSourceTangent = null;
        _manualTargetArrowStatusText = "选择一名己方角色";
        _manualTargetArrowSelectionActive = true;
        _manualTargetPickerPlayedCard = playedCard;
        SetCardHoverUiEnabled(false);
        HideAllCardHoverPreviews();
        ShowCardEnergyPreview(skill);
        _manualTargetArrowRoot.Visible = true;
        if (_manualTargetArrowMask != null)
            _manualTargetArrowMask.MouseFilter = MouseFilterEnum.Ignore;
        if (_manualTargetArrowHintLabel != null)
            _manualTargetArrowHintLabel.Text = _manualTargetArrowStatusText;
        _statusLabel.Text = "选择一名己方角色";
        SetManualTargetArrowHoveredTarget(GetDefaultManualTargetArrowTarget(owner, targets));
        RefreshTurnUi();
        ShowCardEnergyPreview(skill);
        if (sourceCardIndex >= 0 && IsCardIndexValid(sourceCardIndex))
            ApplyManualTargetArrowCardVisual(_cards[sourceCardIndex]);

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

    public async Task<Character> PickItemTargetAsync(Control sourceControl = null)
    {
        if (BattleNode == null)
            return null;

        Character[] targets = BattleNode
            .GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Concat(BattleNode.GetTeamCharacters(isPlayer: false, includeSummons: true))
            .Where(character => character != null && GodotObject.IsInstanceValid(character))
            .OrderBy(character => character.IsPlayer ? 0 : 1)
            .ThenBy(character => character.PositionIndex)
            .ToArray();

        if (targets.Length == 0)
            return null;

        return await ShowItemTargetArrowPickerAsync(
            targets,
            GetManualTargetArrowSourcePosition(sourceControl),
            GetManualTargetArrowSourceTangent(sourceControl),
            "选择道具目标"
        );
    }

    private async Task<Character> ShowItemTargetArrowPickerAsync(
        Character[] targets,
        Vector2 sourcePosition,
        Vector2 sourceTangent,
        string statusText
    )
    {
        if (targets == null || targets.Length == 0 || BattleNode == null)
            return null;

        EnsureManualTargetArrowUi();
        if (_manualTargetArrowRoot == null || _manualTargetArrowLayer == null)
            return null;

        var completion = new TaskCompletionSource<Character>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _manualTargetCompletion = completion;
        _manualTargetArrowTargets = targets;
        _manualTargetArrowOwner = null;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = null;
        _manualTargetArrowCardIndex = -1;
        _manualTargetArrowSourcePosition = sourcePosition;
        _manualTargetArrowSourceTangent = sourceTangent;
        _manualTargetArrowStatusText = statusText;
        _manualTargetArrowSelectionActive = true;
        _manualTargetPickerPlayedCard = null;
        SetCardHoverUiEnabled(false);
        HideAllCardHoverPreviews();
        _manualTargetArrowRoot.Visible = true;
        if (_manualTargetArrowMask != null)
            _manualTargetArrowMask.MouseFilter = MouseFilterEnum.Ignore;
        if (_manualTargetArrowHintLabel != null)
            _manualTargetArrowHintLabel.Text = statusText;
        _statusLabel.Text = statusText;
        SetManualTargetArrowHoveredTarget(GetDefaultManualTargetArrowTarget(null, targets));
        RefreshTurnUi();

        try
        {
            while (!completion.Task.IsCompleted && IsManualTargetArrowContextValid())
            {
                UpdateManualTargetArrowVisual();
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
        ShowCardEnergyPreview(skill);

        Character owner = skill.OwnerCharater;
        Character[] targets = GetManualFriendlyTargetCandidates(skill);
        ShowManualRebirthTargetPreviews(skill, targets);

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
        bool canToggle = rootVisible && IsManualTargetSelectionPending();
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
        return skill != null && IsManualTargetArrowContextValid(owner, requireLivingOwner: true);
    }

    private bool IsManualTargetArrowContextValid(
        Character owner = null,
        bool requireLivingOwner = false
    )
    {
        return IsInsideTree()
            && _manualTargetArrowRoot != null
            && GodotObject.IsInstanceValid(_manualTargetArrowRoot)
            && _manualTargetArrowRoot.Visible
            && _manualTargetArrowSelectionActive
            && (
                !requireLivingOwner
                || (
                    owner != null
                    && GodotObject.IsInstanceValid(owner)
                    && owner.State != Character.CharacterState.Dying
                )
            )
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
        if (_statusLabel != null && _manualTargetArrowSelectionActive)
            _statusLabel.Text = _manualTargetArrowStatusText;
        Vector2 startPosition = GetManualTargetArrowStartPosition(
            _manualTargetArrowSourcePosition,
            owner,
            playedCard
        );
        _manualTargetArrowLayer.SetEndpoints(
            startPosition,
            mousePosition,
            GetManualTargetArrowStartTangent(startPosition, mousePosition, owner, playedCard),
            GetManualTargetArrowEndTangent(startPosition, mousePosition)
        );
        Character hoveredTarget = ResolveManualTargetArrowHoveredTarget(mousePosition);
        if (hoveredTarget != null)
            SetManualTargetArrowHoveredTarget(hoveredTarget);
    }

    public void NotifyManualTargetHover(Character target, bool hovered)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return;

        Character nextHoveredTarget = hovered ? target : null;
        if (!hovered && _manualTargetArrowHoveredTarget != target)
            return;

        SetManualTargetArrowHoveredTarget(nextHoveredTarget);
    }

    public bool TrySelectManualTargetFromCharacter(Character target)
    {
        if (!_manualTargetArrowSelectionActive || !IsManualTargetArrowCandidate(target))
            return false;

        SetManualTargetArrowHoveredTarget(target);
        _manualTargetCompletion?.TrySetResult(target);
        return true;
    }

    private void SetManualTargetArrowHoveredTarget(Character target)
    {
        if (target != null && !IsManualTargetArrowCandidate(target))
            target = null;

        if (_manualTargetArrowHoveredTarget == target)
            return;

        _manualTargetArrowHoveredTarget = target;
        RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
        RefreshManualTargetArrowEffectPreview(_manualTargetArrowHoveredTarget);
    }

    private bool TryHandleManualTargetArrowKeyInput(InputEventKey key)
    {
        if (!_manualTargetArrowSelectionActive)
            return false;

        Key keycode = key.Keycode != Key.None ? key.Keycode : key.PhysicalKeycode;
        if (keycode == Key.Enter || keycode == Key.KpEnter || keycode == Key.Space)
        {
            if (_manualTargetArrowHoveredTarget == null)
                SetManualTargetArrowHoveredTarget(
                    GetDefaultManualTargetArrowTarget(
                        _manualTargetArrowOwner,
                        _manualTargetArrowTargets
                    )
                );

            if (_manualTargetArrowHoveredTarget != null)
            {
                _manualTargetCompletion?.TrySetResult(_manualTargetArrowHoveredTarget);
                return true;
            }

            return false;
        }

        int direction = keycode switch
        {
            Key.Left or Key.Up => -1,
            Key.Right or Key.Down => 1,
            _ => 0,
        };
        if (direction == 0)
            return false;

        Character nextTarget = GetNextManualTargetArrowTarget(direction);
        if (nextTarget == null)
            return false;

        SetManualTargetArrowHoveredTarget(nextTarget);
        UpdateManualTargetArrowVisual();
        return true;
    }

    private Character GetNextManualTargetArrowTarget(int direction)
    {
        Character[] candidates = (_manualTargetArrowTargets ?? Array.Empty<Character>())
            .Where(target => target != null && GodotObject.IsInstanceValid(target))
            .OrderBy(target => target.PositionIndex)
            .ToArray();
        if (candidates.Length == 0)
            return null;

        Character current = _manualTargetArrowHoveredTarget;
        int currentIndex = Array.IndexOf(candidates, current);
        if (currentIndex < 0)
            return GetDefaultManualTargetArrowTarget(_manualTargetArrowOwner, candidates);

        int nextIndex = (currentIndex + direction + candidates.Length) % candidates.Length;
        return candidates[nextIndex];
    }

    private static Character GetDefaultManualTargetArrowTarget(Character owner, Character[] targets)
    {
        Character[] candidates = (targets ?? Array.Empty<Character>())
            .Where(target => target != null && GodotObject.IsInstanceValid(target))
            .OrderBy(target => target.PositionIndex)
            .ToArray();
        if (candidates.Length == 0)
            return null;

        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return candidates[0];

        return candidates
            .OrderBy(target => Math.Abs(target.PositionIndex - owner.PositionIndex))
            .ThenBy(target => target.PositionIndex)
            .FirstOrDefault();
    }

    private Character ResolveManualTargetArrowHoveredTarget(Vector2 mousePosition)
    {
        Character[] candidates = _manualTargetArrowTargets ?? Array.Empty<Character>();
        Character bestTarget = null;
        float bestDistanceSquared = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Character target = candidates[i];
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || !IsMouseOverManualTargetCandidate(target, mousePosition)
            )
            {
                continue;
            }

            Vector2 center = GetManualTargetCandidateScreenCenter(target);
            float distanceSquared = center.DistanceSquaredTo(mousePosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestTarget = target;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestTarget;
    }

    private static bool IsMouseOverManualTargetCandidate(Character target, Vector2 mousePosition)
    {
        if (target?.Hoverframe != null && GodotObject.IsInstanceValid(target.Hoverframe))
        {
            Rect2 rect = target.Hoverframe.GetGlobalRect().Grow(10f);
            if (rect.HasPoint(mousePosition))
                return true;
        }

        return GetManualTargetCandidateScreenCenter(target).DistanceSquaredTo(mousePosition)
            <= 140f * 140f;
    }

    private static Vector2 GetManualTargetCandidateScreenCenter(Character target)
    {
        if (target?.Hoverframe != null && GodotObject.IsInstanceValid(target.Hoverframe))
            return target.Hoverframe.GetGlobalRect().GetCenter();

        return target?.GetGlobalTransformWithCanvas().Origin ?? Vector2.Zero;
    }

    private bool IsManualTargetArrowCandidate(Character target)
    {
        return target != null
            && GodotObject.IsInstanceValid(target)
            && (_manualTargetArrowTargets ?? Array.Empty<Character>()).Contains(target);
    }

    private void ApplyManualTargetArrowCardVisual(SkillCard card)
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
        return GetManualTargetArrowStartPosition(null, owner, playedCard);
    }

    private static Vector2 GetManualTargetArrowStartPosition(
        Vector2? sourcePosition,
        Character owner,
        SkillCard playedCard
    )
    {
        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
            return playedCard.GetGlobalRect().GetCenter();

        if (sourcePosition.HasValue)
            return sourcePosition.Value;

        if (owner != null && GodotObject.IsInstanceValid(owner))
            return owner.GetGlobalTransformWithCanvas().Origin;

        return Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowSourcePosition(Control sourceControl)
    {
        if (sourceControl != null && GodotObject.IsInstanceValid(sourceControl))
            return sourceControl.GetGlobalRect().GetCenter();

        return GetViewport()?.GetMousePosition() ?? Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowSourceTangent(Control sourceControl)
    {
        if (sourceControl != null && GodotObject.IsInstanceValid(sourceControl))
            return GetPointToViewportCenterTangent(
                sourceControl.GetGlobalRect().GetCenter(),
                Vector2.Up
            );

        return Vector2.Zero;
    }

    private Vector2 GetManualTargetArrowStartTangent(
        Vector2 startPosition,
        Vector2 endPosition,
        Character owner,
        SkillCard playedCard
    )
    {
        if (_manualTargetArrowSourceTangent.HasValue)
            return GetSafeDirection(
                _manualTargetArrowSourceTangent.Value,
                endPosition - startPosition
            );

        if (playedCard != null && GodotObject.IsInstanceValid(playedCard))
            return GetManualTargetArrowSourceTangent(playedCard);

        if (owner != null && GodotObject.IsInstanceValid(owner))
            return GetPointToViewportCenterTangent(startPosition, endPosition - startPosition);

        return GetSafeDirection(endPosition - startPosition, Vector2.Right);
    }

    private static Vector2 GetManualTargetArrowEndTangent(
        Vector2 startPosition,
        Vector2 endPosition
    )
    {
        return GetSafeDirection(endPosition - startPosition, Vector2.Right);
    }

    private Vector2 GetPointToViewportCenterTangent(Vector2 point, Vector2 fallback)
    {
        Viewport viewport = GetViewport();
        if (viewport == null)
            return GetSafeDirection(fallback, Vector2.Up);

        Vector2 viewportCenter = viewport.GetVisibleRect().Size * 0.5f;
        return GetSafeDirection(viewportCenter - point, fallback);
    }

    private static Vector2 GetSafeDirection(Vector2 value, Vector2 fallback)
    {
        if (value.LengthSquared() >= 0.01f)
            return value.Normalized();

        if (fallback.LengthSquared() >= 0.01f)
            return fallback.Normalized();

        return Vector2.Right;
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
        if (!_manualTargetArrowSelectionActive || _manualTargetArrowSkill == null)
        {
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
            return;
        }

        if (hoveredTarget != null && GodotObject.IsInstanceValid(hoveredTarget))
            _manualTargetArrowSkill.SetManualFriendlyTarget(hoveredTarget);
        else
            _manualTargetArrowSkill.ClearManualFriendlyTarget();

        if (hoveredTarget != null && !_manualTargetArrowSkill.HasManualFriendlyTarget())
            return;

        var entries = _manualTargetArrowSkill.GetPreviewEffectEntries();
        if (entries == null || entries.Length == 0)
            return;

        ShowManualTargetArrowEffectTargetPreviews(entries);

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

        HideManualTargetArrowEffectTargetPreviews();
    }

    private void ShowManualTargetArrowEffectTargetPreviews(
        IReadOnlyList<Skill.PreviewEffectEntry> entries
    )
    {
        Character owner = _manualTargetArrowSkill?.OwnerCharater;
        if (owner == null || !GodotObject.IsInstanceValid(owner))
            return;

        Character[] manualCandidates = _manualTargetArrowTargets ?? Array.Empty<Character>();
        _manualTargetArrowEffectHostileTargets = entries
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.IsPlayer != owner.IsPlayer
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();
        _manualTargetArrowEffectFriendlyTargets = entries
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.IsPlayer == owner.IsPlayer
                && !manualCandidates.Contains(entry.Target)
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();

        foreach (Character target in _manualTargetArrowEffectHostileTargets)
            target.ShowTargetPreview(ManualTargetEffectHostileColor, animate: false);

        foreach (Character target in _manualTargetArrowEffectFriendlyTargets)
            target.ShowTargetPreview(ManualTargetEffectFriendlyColor, animate: false);
    }

    private void HideManualTargetArrowEffectTargetPreviews()
    {
        foreach (Character target in _manualTargetArrowEffectHostileTargets)
        {
            if (target != null && GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        foreach (Character target in _manualTargetArrowEffectFriendlyTargets)
        {
            if (target != null && GodotObject.IsInstanceValid(target))
                target.HideTargetPreview();
        }

        _manualTargetArrowEffectHostileTargets = Array.Empty<Character>();
        _manualTargetArrowEffectFriendlyTargets = Array.Empty<Character>();

        if (_manualTargetArrowSelectionActive)
            RefreshManualTargetArrowPreviews(_manualTargetArrowHoveredTarget);
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

        Node2D anchor =
            target.Sprite != null && GodotObject.IsInstanceValid(target.Sprite)
                ? target.Sprite
                : target;
        return anchor.GetGlobalTransformWithCanvas().Origin;
    }

    private void ShowManualRebirthTargetPreviews(Skill skill, Character[] targets)
    {
        HideManualRebirthTargetPreviews();
        if (skill?.ManualFriendlyTargetAllowsDying() != true || targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || target.State != Character.CharacterState.Dying
            )
            {
                continue;
            }

            if (_manualRebirthTargetPreviewStates.ContainsKey(target))
                continue;

            Color originalModulate = target.Modulate;
            if (
                TryCreateRebirthPreviewState(
                    target,
                    originalModulate,
                    out RebirthPreviewState state
                )
            )
            {
                _manualRebirthTargetPreviewStates[target] = state;
                target.Modulate = new Color(
                    originalModulate.R,
                    originalModulate.G,
                    originalModulate.B,
                    1f
                );
            }
            else
            {
                _manualRebirthTargetPreviewStates[target] = new RebirthPreviewState
                {
                    TargetModulate = originalModulate,
                };
                target.Modulate = new Color(
                    originalModulate.R,
                    originalModulate.G,
                    originalModulate.B,
                    0.42f
                );
            }
        }
    }

    private void HideManualRebirthTargetPreviews()
    {
        foreach (var entry in _manualRebirthTargetPreviewStates.ToArray())
        {
            Character target = entry.Key;
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            RestoreRebirthPreviewSprite(target, entry.Value);
            if (target.State == Character.CharacterState.Dying)
                target.Modulate = entry.Value.TargetModulate;
        }

        _manualRebirthTargetPreviewStates.Clear();
    }

    private static bool TryCreateRebirthPreviewState(
        Character target,
        Color targetModulate,
        out RebirthPreviewState state
    )
    {
        state = null;
        if (target?.Sprite == null || !GodotObject.IsInstanceValid(target.Sprite))
        {
            return false;
        }

        Node2D sourceSprite = target.Sprite;
        Node parent = sourceSprite.GetParent();
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return false;

        Node duplicatedNode = sourceSprite.Duplicate();
        if (duplicatedNode is not Node2D renderSprite)
        {
            duplicatedNode?.QueueFree();
            return false;
        }

        Vector2I viewportSize = new(1024, 1024);
        Vector2 viewportCenter = viewportSize / 2;
        LocalizeRebirthPreviewMaterials(renderSprite);
        renderSprite.SetMeta("skip_spawn_shader", true);

        SubViewport viewport = new()
        {
            Name = "RebirthPreviewViewport",
            Size = viewportSize,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };

        renderSprite.Name = "RebirthPreviewRenderSprite";
        renderSprite.Visible = true;
        renderSprite.Position = viewportCenter;
        renderSprite.Rotation = sourceSprite.Rotation;
        renderSprite.Scale = sourceSprite.Scale;
        renderSprite.ZIndex = 0;
        renderSprite.ZAsRelative = true;

        Sprite2D previewSprite = new()
        {
            Name = "RebirthPreviewSprite",
            Texture = viewport.GetTexture(),
            Centered = true,
            Position = sourceSprite.Position,
            ZIndex = sourceSprite.ZIndex,
            ZAsRelative = sourceSprite.ZAsRelative,
            Modulate = new Color(1f, 1f, 1f, 0.42f),
        };

        state = new RebirthPreviewState
        {
            TargetModulate = targetModulate,
            SpriteVisible = sourceSprite.Visible,
            Viewport = viewport,
            PreviewSprite = previewSprite,
        };

        parent.AddChild(viewport);
        viewport.AddChild(renderSprite);
        parent.AddChild(previewSprite);
        parent.MoveChild(
            previewSprite,
            Math.Clamp(sourceSprite.GetIndex(), 0, parent.GetChildCount() - 1)
        );
        sourceSprite.Visible = false;
        return true;
    }

    private static void RestoreRebirthPreviewSprite(Character target, RebirthPreviewState state)
    {
        if (state == null || target?.Sprite == null || !GodotObject.IsInstanceValid(target.Sprite))
        {
            return;
        }

        target.Sprite.Visible = state.SpriteVisible;

        if (state.PreviewSprite != null && GodotObject.IsInstanceValid(state.PreviewSprite))
            state.PreviewSprite.QueueFree();

        if (state.Viewport != null && GodotObject.IsInstanceValid(state.Viewport))
            state.Viewport.QueueFree();
    }

    private static void LocalizeRebirthPreviewMaterials(Node2D renderSprite)
    {
        if (renderSprite == null || !GodotObject.IsInstanceValid(renderSprite))
            return;

        if (renderSprite is CanvasItem canvas && canvas.Material is Material material)
        {
            Material localMaterial = (Material)material.Duplicate();
            localMaterial.ResourceLocalToScene = true;
            canvas.Material = localMaterial;
            if (localMaterial is ShaderMaterial shaderMaterial)
                shaderMaterial.SetShaderParameter("progress", 0f);
        }

        if (renderSprite.GetClass() != "SpineSprite")
            return;

        Variant normalMaterialVariant = renderSprite.Get("normal_material");
        if (normalMaterialVariant.VariantType != Variant.Type.Object)
            return;

        if (normalMaterialVariant.As<Material>() is not Material normalMaterial)
            return;

        Material localNormalMaterial = (Material)normalMaterial.Duplicate();
        localNormalMaterial.ResourceLocalToScene = true;
        renderSprite.Set("normal_material", localNormalMaterial);
        if (localNormalMaterial is ShaderMaterial localShaderMaterial)
            localShaderMaterial.SetShaderParameter("progress", 0f);
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
        ClearCardEnergyPreview();
        bool wasTargetSelectionPending = IsManualTargetSelectionPending();
        bool wasArrowSelectionActive = _manualTargetArrowSelectionActive;
        int arrowCardIndex = _manualTargetArrowCardIndex;
        _manualTargetArrowSelectionActive = false;
        _manualTargetCompletion?.TrySetResult(null);
        _manualTargetCompletion = null;
        _manualTargetPickerTemporarilyHidden = false;
        HideManualRebirthTargetPreviews();
        HideManualTargetArrowPreviews();
        HideManualTargetArrowEffectPreview();
        if (wasArrowSelectionActive)
            _manualTargetArrowSkill?.ClearManualFriendlyTarget();
        if (wasArrowSelectionActive)
            SetCardHoverUiEnabled(true);
        _manualTargetArrowTargets = Array.Empty<Character>();
        _manualTargetArrowOwner = null;
        _manualTargetArrowHoveredTarget = null;
        _manualTargetArrowSkill = null;
        _manualTargetArrowCardIndex = -1;
        _manualTargetArrowSourcePosition = null;
        _manualTargetArrowSourceTangent = null;
        _manualTargetArrowStatusText = "选择一名己方角色";
        if (_manualTargetArrowRoot != null && GodotObject.IsInstanceValid(_manualTargetArrowRoot))
        {
            _manualTargetArrowRoot.Visible = false;
        }
        if (_manualTargetArrowLayer != null && GodotObject.IsInstanceValid(_manualTargetArrowLayer))
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
            if (wasTargetSelectionPending || wasArrowSelectionActive)
                RefreshTurnUi();
            return;
        }

        _manualTargetPickerRoot.Visible = false;
        ApplyManualTargetPickerTemporaryHiddenState();
        ClearManualTargetCards();
        if (wasTargetSelectionPending || wasArrowSelectionActive)
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
            UpdateProcessState();
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
        if (!IsCardIndexValid(index))
            return false;

        if (_suppressHandHoverUntilMouseMove && hovered)
            return false;

        if (IsCardDrawEntryBusy(index) || _liftedCardIndex != -1)
        {
            if (!hovered)
                ClearHandCardHoverUiOnly(index);
            return false;
        }

        if (_isDiscardSelectionActive)
        {
            SkillCard handCard = _cards[index];
            if (handCard == null || !handCard.Visible)
                return false;

            if (hovered)
            {
                _hoveredCardIndex = index;
                handCard.HoverHint.Visible = true;
                handCard.ZIndex = HandCardHoverZIndex;
                handCard.TweenBattleMotion(
                    new Vector2(0f, CardHoverLiftY),
                    BattleCardScale * CardHoverScaleMultiplier
                );
                LayoutActionCards(instant: false);
                return true;
            }

            if (_hoveredCardIndex == index)
                _hoveredCardIndex = -1;

            ResetCardMotion(index, instant: false);
            LayoutActionCards(instant: false);
            return true;
        }

        SkillCard card = _cards[index];
        if (card == null || !card.Visible)
            return false;

        if (_manualTargetArrowSelectionActive)
        {
            ShowCardEnergyPreview(_manualTargetArrowSkill);
            if (index == _manualTargetArrowCardIndex)
                ApplyManualTargetArrowCardVisual(card);
            else
            {
                HideCardHoverPreview(index);
                card.HideHoverUi();
            }
            return false;
        }

        if (hovered)
        {
            if (_isResolvingCard || IsCardCommitted(index))
                return false;

            if (_hoveredCardIndex != -1 && _hoveredCardIndex != index)
            {
                ClearCardEnergyPreview();
                ResetCardMotion(_hoveredCardIndex, instant: false);
            }

            _hoveredCardIndex = index;
            card.HoverHint.Visible = true;
            card.ZIndex = HandCardHoverZIndex;
            ShowCardEnergyPreview(GetHandSkill(index));
            card.TweenBattleMotion(
                new Vector2(0f, CardHoverLiftY),
                BattleCardScale * CardHoverScaleMultiplier
            );
            LayoutActionCards(instant: false);
            return true;
        }

        if (_hoveredCardIndex != index)
            return false;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        ResetCardMotion(index, instant: false);
        LayoutActionCards(instant: false);
        return true;
    }

    private void ScheduleCardHoverRefresh()
    {
        if (
            !_uiBuilt
            || !Visible
            || _suppressHandHoverUntilMouseMove
            || _isResolvingCard
            || _endTurnQueued
            || IsCardQueueBusy()
        )
            return;

        int version = ++_deferredHoverRefreshVersion;
        CallDeferred(nameof(RefreshCardHoverUnderMouse), version);
    }

    private void RefreshCardHoverUnderMouse(int version)
    {
        if (
            version != _deferredHoverRefreshVersion
            || !_uiBuilt
            || !Visible
            || _suppressHandHoverUntilMouseMove
            || _isResolvingCard
            || _endTurnQueued
            || IsCardQueueBusy()
            || _isPileCardSelectionActive
            || IsManualTargetSelectionPending()
            || _liftedCardIndex != -1
        )
        {
            return;
        }

        Vector2 mousePosition = GetViewport()?.GetMousePosition() ?? Vector2.Zero;
        for (int i = _cards.Length - 1; i >= 0; i--)
        {
            SkillCard card = _cards[i];
            if (
                card == null
                || !GodotObject.IsInstanceValid(card)
                || !card.Visible
                || card.Button == null
                || card.Button.Disabled
                || IsCardCommitted(i)
                || IsCardDrawEntryBusy(i)
                || !card.Button.GetGlobalRect().HasPoint(mousePosition)
            )
            {
                continue;
            }

            if (SetCardHovered(i, true))
                SetCardHoverPreviewActive(i, true);
            return;
        }
    }

    private Skill GetHandSkill(int index)
    {
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || GetActiveHandSkills() == null
            || index < 0
            || index >= GetActiveHandSkills().Length
        )
        {
            return null;
        }

        return GetActiveHandSkills()[index];
    }

    private void ShowCardEnergyPreview(Skill skill)
    {
        ClearCardEnergyPreview();
    }

    private void ClearCardEnergyPreview()
    {
        if (_energyPreviewCharacter != null && GodotObject.IsInstanceValid(_energyPreviewCharacter))
            _energyPreviewCharacter.SetEnergyUsePreviewVisible(false);

        _energyPreviewCharacter = null;
    }

    private void SetCardHoverPreviewActive(int index, bool active)
    {
        if (active && _manualTargetArrowSelectionActive)
        {
            HideCardHoverPreview(index);
            return;
        }

        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (active)
        {
            _cardHoverPreviewActive[index] = true;
            card.ShowSkillPreview();
        }
        else
        {
            if (!_cardHoverPreviewActive[index])
                return;

            _cardHoverPreviewActive[index] = false;
            card.HideSkillPreview();
        }
    }

    private void HideAllCardHoverPreviews()
    {
        ClearCardEnergyPreview();
        for (int i = 0; i < _cards.Length; i++)
            HideCardHoverPreview(i);
    }

    private void SetCardHoverUiEnabled(bool enabled)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card != null && GodotObject.IsInstanceValid(card))
                card.SetHoverUiEnabled(enabled);
        }
    }

    private void HideCardHoverPreview(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        _cardHoverPreviewActive[index] = false;

        SkillCard card = _cards[index];
        if (card != null && GodotObject.IsInstanceValid(card))
            card.HideSkillPreview();
    }

    private void ResetCardMotion(
        int index,
        bool instant,
        float motionDuration = HandCardResetMotionDuration
    )
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

        HideCardHoverPreview(index);
        ClearDrawEntryState(index, revealCard: false);
        Control slot = _cardSlots[index];
        if (slot != null && GodotObject.IsInstanceValid(slot))
            slot.Scale = Vector2.One;
        card.HoverHint.Visible = false;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.ZIndex = 0;
        card.TweenBattleMotion(
            Vector2.Zero,
            BattleCardScale,
            instant ? 0f : Math.Max(0f, motionDuration),
            instant
        );
    }

    private void ClearHandCardHoverMotion(int index, bool instant)
    {
        if (!IsCardIndexValid(index))
            return;

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (index == _liftedCardIndex)
        {
            ClearHandCardHoverUiOnly(index);
            return;
        }

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        HideCardHoverPreview(index);
        card.HideHoverUi();
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        card.ZIndex = 0;
        card.TweenBattleMotion(Vector2.Zero, BattleCardScale, instant ? 0f : 0.16f, instant);
    }

    private void ClearHandCardHoverUiOnly(int index)
    {
        if (!IsCardIndexValid(index))
            return;

        if (_hoveredCardIndex == index)
            _hoveredCardIndex = -1;

        ClearCardEnergyPreview();
        if (index != _liftedCardIndex)
            HideCardHoverPreview(index);

        SkillCard card = _cards[index];
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        card.HideHoverUi();
        card.HoverHint.Visible = false;
    }

    private void ClearAllHandCardHoverMotionExcept(int excludedIndex, bool instant)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i == excludedIndex || i == _liftedCardIndex || IsCardCommitted(i))
                continue;

            ClearHandCardHoverMotion(i, instant);
        }
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
            RestoreHandSlotForQueuedPlay(play);
            if (resetCards && play != null && play.IsHandCard)
                ResetCardMotion(play.Index, instant: true);
        }

        _queuedCardPlays.Clear();
        foreach (QueuedCardPlay play in _queuedFollowUpCardPlays.ToArray())
        {
            play?.Skill?.RefundDisplayedEnergy();
            CompleteQueuedPlay(play, succeeded: false);
            QueueFreeQueuedPlayCard(play);
            RestoreHandSlotForQueuedPlay(play);
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

    private bool IsAnyCardDrawEntryBusy()
    {
        return _hiddenPendingDrawEntrySlotIndexes.Count > 0
            || _pendingDrawEntryAnimations.Count > 0;
    }

    private bool IsCardDrawEntryBusy(int index)
    {
        return IsCardIndexValid(index)
            && (
                _hiddenPendingDrawEntrySlotIndexes.Contains(index)
                || _pendingDrawEntryAnimations.Contains(index)
                || _drawEntryPreviewCards.ContainsKey(index)
            );
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
        Skill[] hand = player == _activePlayer ? GetActiveHandSkills() : player?.Skills;
        if (hand == null)
            return;

        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
                continue;

            if (
                hand[i].OwnerCharater == null
                || !GodotObject.IsInstanceValid(hand[i].OwnerCharater)
            )
                hand[i].OwnerCharater = player;
            hand[i].UpdateDescription();
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
        for (int i = 0; i < _cards.Length; i++)
            ClearDrawEntryState(i, revealCard: false);

        _customDrawEntryStartPositions.Clear();
        _drawEntryStartCenters.Clear();
        _pendingDrawEntryAnimations.Clear();
        foreach (int index in _drawEntryPreviewCards.Keys.ToArray())
            CancelDrawEntryPreview(index);
        _pendingHandReorderStarts.Clear();
        for (int i = 0; i < _displayedSkillIds.Length; i++)
        {
            if (_cardSlots[i] != null && GodotObject.IsInstanceValid(_cardSlots[i]))
                _cardSlots[i].Scale = Vector2.One;
            _displayedSkills[i] = null;
            _displayedSkillIds[i] = null;
            _cardDisplayInitialized[i] = false;
        }
    }

    private void ResetCardDisplayTracking(int index)
    {
        if (index < 0 || index >= _displayedSkillIds.Length)
            return;

        ClearDrawEntryState(index, revealCard: false);
        _customDrawEntryStartPositions.Remove(index);
        _drawEntryStartCenters.Remove(index);
        _pendingDrawEntryAnimations.Remove(index);
        CancelDrawEntryPreview(index);
        if (_cardSlots[index] != null && GodotObject.IsInstanceValid(_cardSlots[index]))
            _cardSlots[index].Scale = Vector2.One;
        _displayedSkills[index] = null;
        _displayedSkillIds[index] = null;
        _cardDisplayInitialized[index] = false;
    }

    private void OnEndTurnPressed()
    {
        if (_isDiscardSelectionActive)
        {
            if (_isDiscardSelectionCompleting)
                return;

            _ = CompleteDiscardSelectionAsync();
            return;
        }

        if (_isPileCardSelectionActive)
        {
            _ = CompletePileCardSelectionAsync();
            return;
        }

        _ = QueueEndTurnAsync();
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
            && !_isPileCardSelectionActive
            && !IsManualTargetSelectionPending()
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }

    private Task ExecuteQueuedEndTurnAsync()
    {
        if (!_endTurnQueued || !CanExecuteQueuedEndTurn())
        {
            _endTurnQueued = false;
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        _endTurnQueued = false;
        PlayerCharacter endingPlayer = _activePlayer;
        ClearLiftedCard(instant: false);
        _isResolvingCard = true;
        _isResolvingEndTurn = true;
        _freezeHandLayout = true;
        _suppressNextRefreshLayout = true;
        RefreshTurnUi();

        if (
            !GodotObject.IsInstanceValid(this)
            || endingPlayer == null
            || !GodotObject.IsInstanceValid(endingPlayer)
            || _activePlayer != endingPlayer
        )
        {
            _isResolvingEndTurn = false;
            return Task.CompletedTask;
        }

        endingPlayer.EndAction();
        return Task.CompletedTask;
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
