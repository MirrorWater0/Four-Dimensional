using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterControl
{
    private static readonly PackedScene BattlePileOverlayScene = GD.Load<PackedScene>(
        "res://battle/UIScene/BattlePileOverlay.tscn"
    );
    private static readonly Vector2 PileCardScale = new(1f, 1f);
    private static Vector2 PileCardHolderPadding => CardPileOverlayUi.CardHolderPadding;
    private Vector2 PileCardDisplaySize => BattleCardBaseSize * PileCardScale;
    private Vector2 PileCardHolderSize => PileCardDisplaySize + PileCardHolderPadding * 2f;
    private const float PileOverlayContentWidth = 1412f;
    private const int PileOverlayGridColumns = 5;
    private const int PileOverlayGridHSeparation = 18;
    private const int PileOverlayGridVSeparation = 34;
    private const int PileOverlaySectionSeparation = 48;
    private const int PileOverlayCardsCreatedPerFrame = 3;
    private const double PileOverlayBuildFrameBudgetMs = 4.5d;
    private const int PileOverlayVirtualizationThreshold = 36;
    private const int PileOverlayVirtualizationBufferRows = 1;
    private const int PileOverlayMaxPooledCardHolders = 96;
    private const int PileOverlayMaskZIndex = 0;
    private const int PileOverlayContentZIndex = 1;
    private const int PileOverlayConfirmZIndex = 2;
    private const float PileOverlayMaskMaxAlpha = 0.68f;
    private const float PileOverlayMaskFadeInDuration = 0.24f;
    private const float PileOverlayMaskFadeOutDuration = 0.16f;
    private const float PileOverlayContentFadeInDuration = 0.30f;
    private const float PileOverlayContentFadeOutDuration = 0.18f;
    private const float PileOverlayContentSlideOffset = 22f;
    private const float PileOverlayContentIntroDelay = 0.05f;
    private const float PileOverlayConfirmFadeDuration = 0.18f;
    private const float PileOverlayConfirmFadeDelay = 0.12f;
    private const float PileOverlayScrollBounceStep = 18f;
    private const float PileOverlayScrollBounceMaxOffset = 46f;
    private const float PileOverlayScrollBounceOutDuration = 0.06f;
    private const float PileOverlayScrollBounceBackDuration = 0.28f;
    private const float PileOverlaySmoothWheelStep = 360f;
    private const float PileOverlayPanGestureMultiplier = 18f;
    private const float PileOverlaySmoothScrollSpring = 210f;
    private const float PileOverlaySmoothScrollDamping = 24f;
    private const float PileOverlaySmoothScrollMaxVelocity = 5200f;
    private const float PileOverlaySmoothScrollSnapDistance = 0.6f;
    private const float PileOverlaySmoothScrollStopSpeed = 12f;
    private static readonly bool PileOverlayLayoutTraceEnabled = false;
    private const int PileOverlayAnimatedCardCount = 10;
    private const float PileOverlayCardEntryYOffset = 28f;
    private const float PileOverlayCardEntryDuration = 0.20f;
    private const float PileOverlayCardEntryStagger = 0.010f;
    private const float PileOverlayCardEntryBaseDelay = 0.02f;

    private const float PileButtonReceivePulseDuration = 0.24f;

    private const int BattlePileOverlayLayer = 100;

    private const string PileButtonCountLabelName = "CountLabel";
    private const string PileHolderPreviewSkillIdMeta = "pile_preview_skill_id";
    private const string PileHolderPreviewOwnerIdMeta = "pile_preview_owner_id";
    private const string PileHolderPreviewPileIndexMeta = "pile_preview_pile_index";

    private bool _pileOverlayScrollRefreshPending;

    private Button _drawPileButton;
    private Button _discardPileButton;
    private Button _exhaustedPileButton;
    private Character _energyPreviewCharacter;
    private Tween _drawPileHoverTween;
    private Tween _discardPileHoverTween;
    private Tween _exhaustedPileHoverTween;
    private Tween _drawPileShaderTween;
    private Tween _discardPileShaderTween;
    private Tween _exhaustedPileShaderTween;
    private CanvasLayer _pileOverlayLayer;
    private Control _pileOverlayRoot;
    private ScrollContainer _pileOverlayScroll;
    private VScrollBar _pileOverlayVScrollBar;
    private MarginContainer _pileOverlayMargin;
    private VBoxContainer _pileOverlaySections;
    private GridContainer _pileOverlayGrid;
    private Control _pileOverlayRootInputTarget;
    private ColorRect _pileOverlayMask;
    private ColorRect _pileOverlayMaskInputTarget;
    private float _pileOverlayScrollBaseOffsetTop;
    private ScrollContainer _pileOverlayScrollInputTarget;
    private Tween _pileOverlayScrollBounceTween;
    private bool _pileOverlayScrollBounceCheckPending;
    private float _pileOverlayPendingBounceDirection;
    private float _pileOverlayPendingBounceStrength;
    private bool _pileOverlaySmoothScrollActive;
    private double _pileOverlaySmoothScrollPosition;
    private double _pileOverlaySmoothScrollTarget;
    private double _pileOverlaySmoothScrollVelocity;
    private bool _pileOverlaySmoothScrollTracePending;
    private readonly Dictionary<string, float> _pileOverlayLayoutTraceLastGlobalX = new();
    private Button _pileOverlayConfirmButton;
    private Button _pileOverlayConfirmButtonInputTarget;
    private Button _pileOverlayHideButton;
    private Button _pileOverlayHideButtonInputTarget;
    private bool _pileOverlayConfirmSelectionReady;
    private bool _pileOverlayContentTemporarilyHidden;
    private Tween _pileOverlayFadeTween;
    private readonly Stack<Control> _pileCardHolderPool = new();
    private readonly Dictionary<PileOverlayPreviewSkillKey, Skill> _pileOverlayPreviewSkillCache = new();
    private readonly Dictionary<SkillID, bool> _pileOverlayStatusSkillIdCache = new();
    private readonly List<PileOverlayVirtualGrid> _pileOverlayVirtualGrids = new();
    private readonly Dictionary<SkillCard, Action> _pileCardSelectionPressHandlers = new();

    private bool _isPileCardSelectionActive;
    private BattlePileKind _pileCardSelectionKind;
    private PileCardSelectionAction _pileCardSelectionAction;
    private int _pileCardSelectionTargetCount;
    private readonly List<int> _pileCardSelectionIndexes = new();
    private readonly Dictionary<int, SkillCard> _pileCardSelectionCards = new();
    private TaskCompletionSource<int> _pileCardSelectionCompletion;
    private int _pileOverlayBuildVersion;

    private readonly struct BattlePileOverlaySection
    {
        public BattlePileOverlaySection(
            BattlePileKind kind,
            string title,
            Battle.BattleCardPileEntry[] pile
        )
        {
            Kind = kind;
            Title = title;
            Pile = pile ?? Array.Empty<Battle.BattleCardPileEntry>();
        }

        public BattlePileKind Kind { get; }
        public string Title { get; }
        public Battle.BattleCardPileEntry[] Pile { get; }
    }

    private sealed class PileOverlayApplyPerfTracker
    {
        public int ShatterSlashCount { get; private set; }
        public double ShatterSlashApplyMs { get; private set; }
        public int OtherCount { get; private set; }
        public double OtherApplyMs { get; private set; }

        public void Record(SkillID skillId, double elapsedMs)
        {
            if (skillId == SkillID.ShatterSlash)
            {
                ShatterSlashCount++;
                ShatterSlashApplyMs += elapsedMs;
            }
            else
            {
                OtherCount++;
                OtherApplyMs += elapsedMs;
            }
        }
    }

    private sealed class PileOverlayBuildState
    {
        public int Version { get; init; }
        public int TotalCards { get; init; }
        public int CardsCreated { get; set; }
        public int ShatterSlashInPile { get; set; }
        public bool UsesVirtualization { get; set; }
        public ulong LastYieldUsec { get; set; }
        public PileOverlayOpenContext OpenContext { get; init; }
        public PileOverlayApplyPerfTracker ApplyPerf { get; } = new();
    }

    private PileOverlayApplyPerfTracker _pileOverlayActiveApplyPerfTracker;

    private sealed class PileOverlayVirtualGrid
    {
        public BattlePileKind Kind { get; init; }
        public PlayerCharacter FallbackPlayer { get; init; }
        public Control Grid { get; init; }
        public IndexedBattlePileEntry[] Entries { get; init; }
        public readonly List<Control> Holders = new();
        public int FirstIndex { get; set; } = -1;
        public int VisibleSlotCount { get; set; } = -1;
        public bool EntryAnimationPlayed { get; set; }
    }

    private readonly struct PileOverlayPreviewSkillKey : IEquatable<PileOverlayPreviewSkillKey>
    {
        public PileOverlayPreviewSkillKey(SkillID skillId, ulong ownerId)
        {
            SkillId = skillId;
            OwnerId = ownerId;
        }

        public SkillID SkillId { get; }
        public ulong OwnerId { get; }

        public bool Equals(PileOverlayPreviewSkillKey other) =>
            SkillId == other.SkillId && OwnerId == other.OwnerId;

        public override bool Equals(object obj) =>
            obj is PileOverlayPreviewSkillKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SkillId, OwnerId);
    }

    private enum PileCardSelectionAction
    {
        MoveToHand,
        Exhaust,
    }

    private readonly struct IndexedBattlePileEntry
    {
        public IndexedBattlePileEntry(int index, Battle.BattleCardPileEntry entry)
        {
            Index = index;
            Entry = entry;
        }

        public int Index { get; }
        public Battle.BattleCardPileEntry Entry { get; }
    }

    public Task<int> SelectDrawPileCardsToHandAsync(PlayerCharacter player, int count) =>
        SelectPileCardsAsync(player, BattlePileKind.Draw, count, PileCardSelectionAction.MoveToHand);

    public Task<int> SelectDiscardPileCardsToHandAsync(PlayerCharacter player, int count) =>
        SelectPileCardsAsync(player, BattlePileKind.Discard, count, PileCardSelectionAction.MoveToHand);

    public Task<int> SelectPileCardsToExhaustAsync(
        PlayerCharacter player,
        BattleCardPileTarget pileTarget,
        int count
    )
    {
        BattlePileKind kind = pileTarget == BattleCardPileTarget.DiscardPileCards
            ? BattlePileKind.Discard
            : BattlePileKind.Draw;
        return SelectPileCardsAsync(player, kind, count, PileCardSelectionAction.Exhaust);
    }

    private async Task<int> SelectPileCardsAsync(
        PlayerCharacter player,
        BattlePileKind kind,
        int count,
        PileCardSelectionAction action
    )
    {
        if (count <= 0)
            return 0;

        ulong flowStartUsec = Time.GetTicksUsec();
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

        Battle.BattleCardPileEntry[] pile = GetPileEntriesForSelection(player, kind);
        int availableCount = action == PileCardSelectionAction.MoveToHand
            ? Math.Min(pile.Length, BattleNode.GetPlayerTeamBattleHandEmptySlotCount())
            : pile.Length;
        if (availableCount <= 0)
            return 0;

        CancelDiscardSelection();
        CancelPileCardSelection();
        _pileCardSelectionKind = kind;
        _pileCardSelectionAction = action;
        _pileCardSelectionTargetCount = Math.Min(count, availableCount);
        _pileCardSelectionCompletion = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _isPileCardSelectionActive = true;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        ClearLiftedCard(instant: false);
        HideManualTargetPicker();

        SkillID? triggerSkillId = GetPileSelectionTriggerSkillId();
        string triggerSkillName = GetPileSelectionTriggerSkillName();
        double prepMs = (Time.GetTicksUsec() - flowStartUsec) / 1000.0;
        var openContext = new PileOverlayOpenContext(
            PileOverlayOpenSource.CardSelection,
            triggerSkillId,
            triggerSkillName,
            prepMs,
            flowStartUsec
        );
        BattleNode?.LogPilePerf(
            $"card-selection start trigger={FormatPilePerfTriggerSkill(openContext)}, "
            + $"pile={pile.Length}, select={_pileCardSelectionTargetCount}, "
            + $"kind={kind}, prep={prepMs:F2}ms"
        );

        ShowPileOverlay(
            player,
            new[]
            {
                new BattlePileOverlaySection(
                    kind,
                    BuildPileCardSelectionTitle(kind, action, _pileCardSelectionTargetCount),
                    pile
                ),
            },
            openContext
        );
        RefreshTurnUi();

        int movedCount = await _pileCardSelectionCompletion.Task;
        return Math.Max(0, movedCount);
    }

    private static string BuildPileCardSelectionTitle(
        BattlePileKind kind,
        PileCardSelectionAction action,
        int count
    )
    {
        string actionText = action == PileCardSelectionAction.Exhaust ? "消耗" : "加入手牌";
        return $"选择{count}张{GetPileTitle(kind)}{actionText}";
    }

    private Battle.BattleCardPileEntry[] GetPileEntriesForSelection(
        PlayerCharacter player,
        BattlePileKind kind
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return Array.Empty<Battle.BattleCardPileEntry>();

        return kind switch
        {
            BattlePileKind.Draw => BattleNode.GetDrawBattleCardPileEntries(player),
            BattlePileKind.Discard => BattleNode.GetDiscardBattleCardPileEntries(player),
            _ => Array.Empty<Battle.BattleCardPileEntry>(),
        };
    }

    private enum BattlePileKind
    {
        Draw,
        Discard,
        Exhausted,
    }

    private enum PileOverlayOpenSource
    {
        ManualView,
        CardSelection,
        ShowAllPiles,
    }

    private readonly struct PileOverlayOpenContext
    {
        public PileOverlayOpenContext(
            PileOverlayOpenSource source,
            SkillID? triggerSkillId = null,
            string triggerSkillName = null,
            double prepMs = 0d,
            ulong flowStartUsec = 0,
            double syncSetupMs = 0d
        )
        {
            Source = source;
            TriggerSkillId = triggerSkillId;
            TriggerSkillName = triggerSkillName;
            PrepMs = prepMs;
            FlowStartUsec = flowStartUsec;
            SyncSetupMs = syncSetupMs;
        }

        public PileOverlayOpenSource Source { get; }
        public SkillID? TriggerSkillId { get; }
        public string TriggerSkillName { get; }
        public double PrepMs { get; }
        public ulong FlowStartUsec { get; }
        public double SyncSetupMs { get; }

        public static PileOverlayOpenContext ManualView() =>
            new(PileOverlayOpenSource.ManualView);

        public static PileOverlayOpenContext ShowAllPiles() =>
            new(PileOverlayOpenSource.ShowAllPiles);
    }

    private void ShowCurrentPlayerPile(BattlePileKind kind)
    {
        if (
            _activePlayer == null
            || !GodotObject.IsInstanceValid(_activePlayer)
            || _activePlayer.State == Character.CharacterState.Dying
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
            || IsPileLockedByCardResolution()
            || IsManualTargetSelectionPending()
        )
        {
            return;
        }

        Battle.BattleCardPileEntry[] pile = kind switch
        {
            BattlePileKind.Draw => BattleNode.GetDrawBattleCardPileEntries(_activePlayer),
            BattlePileKind.Discard => BattleNode.GetDiscardBattleCardPileEntries(_activePlayer),
            BattlePileKind.Exhausted => BattleNode.GetExhaustedBattleCardPileEntries(_activePlayer),
            _ => Array.Empty<Battle.BattleCardPileEntry>(),
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
            },
            PileOverlayOpenContext.ManualView()
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
                    BattleNode.GetOwnedDrawBattleCardPileEntries(player)
                ),
                new BattlePileOverlaySection(
                    BattlePileKind.Discard,
                    GetPileTitle(BattlePileKind.Discard),
                    BattleNode.GetOwnedDiscardBattleCardPileEntries(player)
                ),
                new BattlePileOverlaySection(
                    BattlePileKind.Exhausted,
                    GetPileTitle(BattlePileKind.Exhausted),
                    BattleNode.GetOwnedExhaustedBattleCardPileEntries(player)
                ),
            },
            PileOverlayOpenContext.ShowAllPiles()
        );
        return true;
    }

    private bool IsPileLockedByCardResolution()
    {
        return _isResolvingCard && !_isResolvingEndTurn;
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
}
