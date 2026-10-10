using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private static readonly NodePath BlockingMenuNodePath = "/root/Map/MenuLayer/Menu";
    private static readonly PackedScene SkillCardScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattleHandCard.tscn"
    );
    private static readonly Vector2 BattleCardBaseSize = new(240f, 370f);

    public float BattleCardScaleFactor => HandLayout.CardScale;

    public float HandCardYOffset => HandLayout.CardYOffset;

    [Export(PropertyHint.Range, "-160,160,1")]
    public float HandDrawEntryLandingYOffset = 0f;

    [Export(PropertyHint.Range, "0.02,0.2,0.005")]
    public float HandDroppedCardReturnMotionDuration = 0.06f;

    [Export(PropertyHint.Range, "900,9000,100")]
    public float HandDroppedCardReturnPixelsPerSecond = 2200f;
    private Vector2 BattleCardScale => Vector2.One * BattleCardScaleFactor;
    private const float EndTurnCardVanishDuration = 0.32f;
    private const float CardPlayVanishDuration = 0.36f;
    private const float CardPlayDiscardCompressDuration = 0.16f;
    private const float CardPlayDiscardFlyDuration = 0.24f;
    private const float CardPlayDiscardTrailFadeDuration = 0.1f;
    private const float CardPlayDiscardCompressedScaleFactor = 0.22f;
    private const float CardPlayDiscardTargetScaleFactor = 0.28f;
    private const float CardPlayMoveDuration = 0.32f;
    private const float CardPlayArcLiftMin = 56f;
    private const float CardPlayArcLiftMax = 132f;
    private const float CardPlayArcSidePull = 34f;
    private const float QueuedCardLayerMinMoveDuration = 0.075f;
    private const float QueuedCardLayerPixelsPerSecond = 3100f;
    private const float DiscardSelectionReturnDuration = 0.15f;
    private const float StatusInsertCardScale = 1.0f;
    private const float StatusInsertArrangeDuration = 0.22f;
    private const float StatusInsertHoldDuration = 0.22f;
    private const float StatusInsertFlyDuration = 0.34f;
    private const float StatusInsertStagger = 0.055f;
    private const int StatusInsertCardsCreatedPerFrame = 3;
    // Make the hovered card stand out clearly above the rest of the hand.
    private float CardHoverLiftY => HandLayout.HoverLift;
    private float CardHoverScaleMultiplier => HandLayout.HoverScale;
    private const float HandHoverResumeMouseMoveDistance = 8f;
    private const double HandHoverValidationIntervalSeconds = 0.1;
    private const float TemporaryCardSpawnScaleMultiplier = 0.72f;
    [ExportGroup("Selection Layout")]
    [Export] public float DiscardSelectionSelectedVerticalOffset = -80f;
    [Export] public float DiscardSelectionSelectedMaxStep = 280f;
    [Export] public float DiscardSelectionSelectedGap = 24f;
    private float HandAreaPadding => HandLayout.AreaPadding;
    private float HandCardHoverSpreadRatio => HandLayout.HoverSpreadRatio;
    private float HandCardHoverSpreadBase => HandLayout.HoverSpreadBase;
    private float HandCardMaxHoverSpread => HandLayout.MaxHoverSpread;
    private const float HandCardResetMotionDuration = 0.16f;
    private const float LiftedCardMouseFollowSharpness = 34f;
    private const float LiftedCardMouseFollowSnapDistance = 1.2f;
    private const float LiftedCardRotationFollowSharpness = 22f;
    private const float LiftedCardRotationSnapDistance = 0.002f;
    private const float HandLayoutMinTweenDuration = 0.24f;
    private const float HandLayoutMaxTweenDuration = 0.9f;
    private const float HandLayoutPixelsPerSecond = 900f;
    private const float HandLayoutFollowSharpness = 8f;
    private const float HandLayoutFollowSnapDistance = 0.45f;
    private const float HandLayoutFollowSnapRotation = 0.002f;
    private const float HandDrawEntryMinTweenDuration = 0.15f;
    private const float HandDrawEntryMaxTweenDuration = 0.45f;
    private const float HandDrawEntryPixelsPerSecond = 2500f;
    private const float HandDrawTrailFadeDuration = 0.1f;
    private const float HandDrawTrailWidth = 10f;
    private const float HandDrawEntryStagger = 0.035f;
    private const float HandDrawEntryMinStagger = 0.014f;
    private const float HandDrawEntryMaxTotalStaggerDuration = 0.34f;
    private const int HandDrawEntryCompactDurationThreshold = 6;
    private const float HandDrawEntryManyCardDurationScale = 0.72f;
    private const int ShufflePreviewMaxCardCount = 9;
    private const float ShufflePreviewCardScale = 0.18f;
    private const float ShufflePreviewCardFlyDuration = 0.78f;
    private const float ShufflePreviewCardStagger = 0.045f;
    private const float ShufflePreviewDrawEntryDelayPadding = 0.08f;
    private const int HandCardHoverZIndex = 90;
    private const int PlayedCardZIndex = 100;
    private const int DiscardSelectionSelectedCardZIndex = PlayedCardZIndex + 20;
    private const int TemporaryCardZIndex = 300;
    private const int DyingOwnedCardExhaustDisplayMax = 8;
    private const int BattleCardPoolPrewarmCount = HandCardCapacity;
    private const int BattleCardPoolMaxCount = HandCardCapacity * 2;
    private static readonly Vector2 DyingOwnedCardStackOffset = new(10f, 6f);
    private const int LiftedCardOverlayLayer = 80;
    private const int LiftedCardZIndex = 0;
    private const int HandCardCapacity = PlayerCharacter.MaxBattleHandSize;
    private const int QueuedPlayedCardVisibleLayers = 4;
    private const float QueuedPlayedCardLayerScaleStep = 0.075f;
    private const float QueuedPlayedCardVerticalOffset = -100f;
    private float ManualTargetCenteredCardScaleMultiplier => CardHoverScaleMultiplier;
    private const float ManualTargetCenteredCardMoveDuration = 0.32f;
    private const float ManualTargetArrowSegmentScaleStart = 0.28f;
    private const float ManualTargetArrowSegmentScaleEnd = 0.42f;
    private const float ManualTargetArrowHeadDefaultScale = 0.95f;
    private const float ManualTargetArrowHeadHoverScale = 1.05f;
    private static readonly Vector2 QueuedPlayedCardLayerOffset = new(18f, -10f);
    private static readonly Vector2 PlayedCardScale = new(0.74f, 0.74f);
    private static readonly Color QueuedCardModulate = new(1f, 1f, 1f, 0.82f);
    private static readonly Color ManualTargetCandidateColor = new(0.42f, 0.82f, 1f, 0.72f);
    private static readonly Color ManualTargetHoveredColor = new(1f, 0.95f, 0.62f, 1f);
    private static readonly Color ManualTargetEffectHostileColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color ManualTargetEffectFriendlyColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Color DiscardSelectionCardModulate = new(1f, 0.82f, 0.42f, 1f);
    private Control _root;
    private Label _statusLabel;
    private Control _cardRow;
    private BattleHandLayout HandLayout => (BattleHandLayout)(_cardRow ?? GetNode<Control>("ActionAreaRoot/CardRow"));
    private static readonly PackedScene HandSlotScene = GD.Load<PackedScene>("res://battle/UIScene/BattleHandSlot.tscn");
    private Control _handInputBlocker;
    private SkillCard _manualTargetArrowPlayedCard;
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
    private readonly List<Character> _manualTargetArrowEffectHostileTargets = new();
    private readonly List<Character> _manualTargetArrowEffectFriendlyTargets = new();
    private readonly List<Character> _manualTargetArrowDamageTargetsBuffer = new();
    private readonly Dictionary<Character, List<Skill.PreviewEffectEntry>>
        _manualTargetArrowDamageEntriesByTarget = new();
    private readonly Dictionary<Character, RebirthPreviewState> _manualRebirthTargetPreviewStates =
        new();
    private readonly HashSet<int> _turnEndStatusTriggerCardIndexes = new();
    private readonly HashSet<int> _pendingHandStatusExhaustIndexes = new();
    private Button _endTurnButton;
    private SkillCard[] _cards = new SkillCard[HandCardCapacity];
    private Control[] _cardSlots = new Control[HandCardCapacity];
    private readonly Queue<SkillCard> _battleCardPool = new();
    private readonly HashSet<SkillCard> _wiredBattleCards = new();
    private readonly Dictionary<Skill, SkillCard> _handCardsBySkill = new();
    private readonly Dictionary<Skill, Control> _handSlotsBySkill = new();
    private readonly HashSet<Skill> _activeHandSkillsBuffer = new();
    private readonly List<Skill> _skillRemovalBuffer = new();
    private readonly List<SkillCard> _handCardReturnBuffer = new();
    private readonly List<int> _handCardInputPriorityOrderBuffer = new(HandCardCapacity);
    private readonly List<Task> _endTurnHandDiscardAnimationTasks = new(HandCardCapacity);
    private readonly List<(int Index, SkillCard Card)> _endTurnHandDiscardEntries =
        new(HandCardCapacity);
    private readonly List<(int Index, SkillCard Card)> _endTurnHandExhaustEntries =
        new(HandCardCapacity);
    private readonly List<(Skill Skill, PlayerCharacter Owner, SkillCard Card)> _discardSelectionEntryBuffer =
        new(HandCardCapacity);
    private readonly List<Task> _discardSelectionFlyTasks = new(HandCardCapacity);
    private readonly Dictionary<SkillCard, Tween> _discardSelectionArrangeTweens = new();
    private readonly Dictionary<SkillCard, (Vector2 Position, Vector2 Scale, bool UsesOriginalHandVisual)> _discardSelectionArrangeTargets =
        new();
    private readonly List<(
        Character Target,
        Character Source,
        Skill StatusSkill,
        BattleCardPileTarget PileTarget
    )> _statusInsertExpandedEntries = new();
    private readonly Dictionary<SkillID, Skill> _statusInsertSkillCache = new();
    private readonly List<StatusInsertPreviewCard> _statusInsertPreviewCards = new();
    private readonly List<Task> _statusInsertFlyTasks = new();
    private bool _statusInsertAnimationBuffersInUse;
    private readonly List<SkillCard> _statusExhaustPreviewCards = new();
    private readonly List<SkillCard> _ownedExhaustPreviewCards = new();
    private readonly List<SkillCard> _dyingOwnedExhaustPreviewCards = new();
    private readonly List<StatusCardExhaustAnimationEntry> _dyingOwnedExhaustOverlayEntries = new();
    private int _battleCardPoolSerial;
    private Tween[] _cardSlotLayoutTweens = new Tween[HandCardCapacity];
    private Vector2?[] _cardSlotLayoutTargets = new Vector2?[HandCardCapacity];
    private float?[] _cardSlotLayoutRotationTargets = new float?[HandCardCapacity];
    private bool[] _cardSlotLayoutFollowActive = new bool[HandCardCapacity];
    private float[] _cardSlotLayoutPixelsPerSecondOverrides = new float[HandCardCapacity];
    private readonly bool[] _drawEntryArcActive = new bool[HandCardCapacity];
    private readonly Vector2[] _drawEntryArcStartPositions = new Vector2[HandCardCapacity];
    private readonly float[] _drawEntryArcStartRotations = new float[HandCardCapacity];
    private readonly float[] _drawEntryArcDurations = new float[HandCardCapacity];
    private readonly float[] _drawEntryArcElapsed = new float[HandCardCapacity];
    private readonly Vector2[] _drawEntryArcVelocities = new Vector2[HandCardCapacity];
    private readonly Vector2[] _drawEntryArcOvershootPositions = new Vector2[HandCardCapacity];
    private readonly Vector2[] _drawEntryArcTargetPositions = new Vector2[HandCardCapacity];
    private readonly int[] _visibleHandSlotIndexesBuffer = new int[HandCardCapacity];
    private readonly bool[] _handLayoutTargetPositionValid = new bool[HandCardCapacity];
    private readonly Vector2[] _handLayoutTargetPositions = new Vector2[HandCardCapacity];
    private readonly float[] _handLayoutTargetRotations = new float[HandCardCapacity];
    private readonly int[] _handLayoutOrderBySlotIndex = new int[HandCardCapacity];
    private readonly HashSet<int> _drawEntrySlotIndexes = new();
    private readonly Dictionary<int, int> _drawEntrySlotOrders = new();
    private readonly HashSet<int> _hiddenPendingDrawEntrySlotIndexes = new();
    private readonly Dictionary<int, Vector2> _customDrawEntryStartPositions = new();
    private readonly Dictionary<int, Vector2> _drawEntryStartCenters = new();
    private readonly HashSet<int> _drawEntryFromPlayedCardOrigin = new();
    private readonly Dictionary<int, SkillCard> _drawEntryPreviewCards = new();
    private readonly HashSet<int> _pendingDrawEntryAnimations = new();
    private readonly int[] _drawEntryAnimationVersions = new int[HandCardCapacity];
    private readonly Dictionary<
        Skill,
        (Vector2 Position, float Rotation, float PixelsPerSecond)
    > _pendingHandReorderStarts = new();
    private readonly Queue<QueuedCardPlay> _queuedCardPlays = new();
    private readonly Queue<QueuedCardPlay> _queuedFollowUpCardPlays = new();
    private readonly List<QueuedCardPlay> _pendingBattlePileQueuedPlays = new();
    private readonly HashSet<Skill> _queuedCardSkills = new();
    private readonly List<QueuedCardPlay> _queuedPlayLayerBuffer = new();
    private QueuedCardPlay _activeQueuedCardPlay;
    private QueuedCardPlay _activeExecutingCardPlay;
    private int _retiringQueuedPlayVisualCount;
    private int[] _preparedHandDrawEntrySlots = new int[HandCardCapacity];
    private int _preparedHandDrawEntrySlotCount;
    private Skill[] _displayedSkills = new Skill[HandCardCapacity];
    private readonly Skill[] _previousDisplayedSkillsSnapshot = new Skill[HandCardCapacity];
    private readonly Vector2?[] _previousSlotPositionsSnapshot = new Vector2?[HandCardCapacity];
    private readonly float?[] _previousSlotRotationsSnapshot = new float?[HandCardCapacity];
    private SkillID?[] _displayedSkillIds = new SkillID?[HandCardCapacity];
    private bool[] _cardDisplayInitialized = new bool[HandCardCapacity];
    private PlayerCharacter _activePlayer;
    private bool _isResolvingCard;
    private bool _isProcessingCardQueue;
    private bool _handLayoutSyncPending;
    private int _deferredHoverRefreshVersion;
    private int _queuedHoverRefreshVersion;
    private Timer _handHoverValidationTimer;
    private bool _handHoverValidationPointerInitialized;
    private Vector2 _lastHandHoverValidationPointerPosition;
    private Menu _blockingMenu;
    private bool _blockingMenuLookupCompleted;
    private int _liftedCardDropHoverRefreshVersion;
    private bool _uiBuilt;
    private bool _turnUiRefreshQueued;
    private bool _turnUiRefreshHoverQueued;
    private int _turnUiRefreshVersion;
    private int _hoveredCardIndex = -1;
    private int _liftedCardIndex = -1;
    private Skill _liftedCardSkill;
    private SkillCard _liftedCard;
    private Vector2 _liftedCardMouseOffset;
    private bool _manualTargetArrowUsesLiftedCard;
    private Input.MouseModeEnum _manualTargetArrowPreviousMouseMode = Input.MouseModeEnum.Visible;
    private bool _manualTargetArrowMouseHidden;
    private bool[] _cardHoverPreviewActive = new bool[HandCardCapacity];
    private int _pendingCardHoverPreviewIndex = -1;
    private int _cardHoverPreviewRequestVersion;
    private readonly HashSet<Buff> _cardPreviewHighlightedBuffs = new();
    private int _cardPreviewHighlightedBuffsIndex = -1;
    private PlayerCharacter _cardCharacterShadowHoverPlayer;
    private int _cardCharacterShadowHoverIndex = -1;
    private bool _suppressCardButtonPressUntilLeftRelease;
    private int _suppressCardButtonPressSerial = -1;
    private int _leftMouseButtonPressSerial;
    private bool _leftMouseButtonPressed;
    private int _handledHandCardPressSerial = -1;
    private bool _suppressHandHoverUntilMouseMove;
    private bool _handHoverSuppressionRequiresMouseMove;
    private Vector2 _handHoverSuppressionMousePosition;
    private bool _layoutInitialized;
    private bool _suppressNextRefreshLayout;
    private bool _freezeHandLayout;
    private bool _endTurnQueued;
    private bool _isResolvingEndTurn;
    private ulong _shuffleDrawEntryDelayUntilMsec;
    private int _shuffleDelayedLayoutRefreshVersion;
    private TaskCompletionSource<Character> _manualTargetCompletion;
    private bool _isDiscardSelectionActive;
    private bool _isDiscardSelectionCompleting;
    private bool _discardSelectionExhaustMode;
    private bool _discardSelectionKeywordMode;
    private bool _discardSelectionTransformMode;
    private BattleCardKeyword _discardSelectionKeyword;
    private SkillID _discardSelectionTransformSkillId = SkillID.None;
    private int _discardSelectionTargetCount;
    private readonly List<Skill> _discardSelectionSkills = new();
    private readonly Dictionary<Skill, SkillCard> _discardSelectionCards = new();
    private readonly Dictionary<Skill, int> _discardSelectionOriginalHandIndexes = new();
    private readonly HashSet<int> _discardSelectionOriginalVisualHandIndexes = new();
    private readonly HashSet<int> _discardSelectionReturningHandIndexes = new();
    private readonly HashSet<int> _discardSelectionFlyingReturnHandIndexes = new();
    private readonly HashSet<int> _discardSelectionFlyingToDiscardHandIndexes = new();
    private readonly Dictionary<int, SkillCard> _discardSelectionReturningPreviewCards = new();
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
        public bool MoveToCenterBeforeEffect { get; init; }
        public bool RemovedFromHand { get; set; }
        public bool PendingBattlePileResolution { get; set; }
        public bool ResolvedToBattlePile { get; set; }
        public bool AbilityVisualConsumed { get; set; }
        public bool AbilityActivationVisualInProgress { get; set; }
        public Task MoveToCenterTask { get; set; }
        public Tween MoveToCenterTween { get; set; }
        public TaskCompletionSource<bool> MoveToCenterCompletion { get; set; }
        public int MoveToCenterMotionSerial { get; set; }
        public int MoveToCenterLayerIndex { get; set; } = -1;
        public Vector2? MoveToCenterTargetPosition { get; set; }
        public Vector2? MoveToCenterTargetScale { get; set; }
        public Vector2? CachedDrawEntryStartCenter { get; set; }
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

}
