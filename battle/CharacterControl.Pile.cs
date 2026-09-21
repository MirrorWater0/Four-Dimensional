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
    private const float PileOverlayContentMoveDuration = CardPileOverlayUi.ContentMoveDuration;
    private const float PileOverlayContentSlideOffset = CardPileOverlayUi.ContentSlideOffset;
    private const float PileOverlayConfirmFadeDuration = 0.18f;
    private const float PileOverlayConfirmFadeDelay = 0.12f;
    private const float PileOverlayScrollBounceStep = CardPileOverlayUi.ScrollBounceStep;
    private const float PileOverlayScrollBounceMaxOffset = CardPileOverlayUi.ScrollBounceMaxOffset;
    private const float PileOverlayScrollBounceOutDuration = CardPileOverlayUi.ScrollBounceOutDuration;
    private const float PileOverlayScrollBounceBackDuration = CardPileOverlayUi.ScrollBounceBackDuration;
    private const float PileOverlaySmoothWheelStep = CardPileOverlayUi.SmoothWheelStep;
    private const float PileOverlayPanGestureMultiplier = CardPileOverlayUi.PanGestureMultiplier;
    private const float PileOverlaySmoothScrollSpring = CardPileOverlayUi.SmoothScrollSpring;
    private const float PileOverlaySmoothScrollDamping = CardPileOverlayUi.SmoothScrollDamping;
    private const float PileOverlaySmoothScrollMaxVelocity = CardPileOverlayUi.SmoothScrollMaxVelocity;
    private const float PileOverlaySmoothScrollSnapDistance = CardPileOverlayUi.SmoothScrollSnapDistance;
    private const float PileOverlaySmoothScrollStopSpeed = CardPileOverlayUi.SmoothScrollStopSpeed;
    private static readonly bool PileOverlayLayoutTraceEnabled = false;

    private const float PileButtonReceivePulseDuration = 0.20f;
    private const ulong PileButtonReceivePulseMinIntervalMsec = 170;

    private const int BattlePileOverlayLayer = 100;

    private const string PileButtonCountLabelName = "CountLabel";
    private const string PileButtonCountBackgroundName = "CountBackground";
    private const string PileHolderPreviewSkillIdMeta = "pile_preview_skill_id";
    private const string PileHolderPreviewInstanceIdMeta = "pile_preview_instance_id";
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
    private Tween _drawPileReceiveScaleTween;
    private Tween _discardPileReceiveScaleTween;
    private Tween _exhaustedPileReceiveScaleTween;
    private Tween _drawPileReceiveShaderTween;
    private Tween _discardPileReceiveShaderTween;
    private Tween _exhaustedPileReceiveShaderTween;
    private ulong _drawPileLastReceivePulseMsec;
    private ulong _discardPileLastReceivePulseMsec;
    private ulong _exhaustedPileLastReceivePulseMsec;
    private CanvasLayer _pileOverlayLayer;
    private Control _pileOverlayRoot;
    private ScrollContainer _pileOverlayScroll;
    private Control.MouseFilterEnum _pileOverlayScrollMouseFilter = MouseFilterEnum.Stop;
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
    private readonly List<int> _pileCardSelectionIndexBuffer = new();
    private readonly List<int> _pileCardSelectionRemovalBuffer = new();
    private readonly List<StatusCardExhaustAnimationEntry> _pileCardSelectionExhaustAnimationEntries =
        new();
    private readonly List<PileOverlayCharacterGroup> _pileOverlayCharacterGroups = new();

    private bool _isPileCardSelectionActive;
    private BattlePileKind _pileCardSelectionKind;
    private PileCardSelectionAction _pileCardSelectionAction;
    private BattleCardKeyword _pileCardSelectionKeyword;
    private SkillID _pileCardSelectionTransformSkillId = SkillID.None;
    private int _pileCardSelectionTargetCount;
    private bool _pileCardSelectionAllowsFewer;
    private readonly List<int> _pileCardSelectionIndexes = new();
    private readonly Dictionary<int, SkillCard> _pileCardSelectionCards = new();
    private TaskCompletionSource<int> _pileCardSelectionCompletion;
    private PlayerCharacter _pileCardSelectionOverlayPlayer;
    private Battle.BattleCardPileEntry[] _pileCardSelectionOverlayPile =
        Array.Empty<Battle.BattleCardPileEntry>();
    private PileOverlayOpenContext _pileCardSelectionOverlayOpenContext;
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

    private sealed class PileOverlayCharacterGroup
    {
        public PlayerCharacter Owner { get; set; }
        public readonly List<IndexedBattlePileEntry> Entries = new();
    }

    private readonly struct PileOverlayPreviewSkillKey : IEquatable<PileOverlayPreviewSkillKey>
    {
        public PileOverlayPreviewSkillKey(SkillID skillId, ulong instanceId, ulong ownerId)
        {
            SkillId = skillId;
            InstanceId = instanceId;
            OwnerId = ownerId;
        }

        public SkillID SkillId { get; }
        public ulong InstanceId { get; }
        public ulong OwnerId { get; }

        public bool Equals(PileOverlayPreviewSkillKey other) =>
            SkillId == other.SkillId
            && InstanceId == other.InstanceId
            && OwnerId == other.OwnerId;

        public override bool Equals(object obj) =>
            obj is PileOverlayPreviewSkillKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SkillId, InstanceId, OwnerId);
    }

    private enum PileCardSelectionAction
    {
        MoveToHand,
        Exhaust,
        ApplyKeyword,
        Transform,
        FilterToDiscard,
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

    public Task<int> SelectPileCardsForKeywordAsync(
        PlayerCharacter player,
        BattleCardPileTarget pileTarget,
        int count,
        BattleCardKeyword keyword
    )
    {
        BattlePileKind kind = pileTarget switch
        {
            BattleCardPileTarget.DiscardPileCards => BattlePileKind.Discard,
            BattleCardPileTarget.DrawPileCards => BattlePileKind.Draw,
            _ => BattlePileKind.Draw,
        };
        return SelectPileCardsAsync(
            player,
            kind,
            count,
            PileCardSelectionAction.ApplyKeyword,
            keyword
        );
    }

    public Task<int> SelectPileCardsToTransformAsync(
        PlayerCharacter player,
        BattleCardPileTarget pileTarget,
        int count,
        SkillID replacementSkillId = SkillID.None
    )
    {
        BattlePileKind kind = pileTarget == BattleCardPileTarget.DiscardPileCards
            ? BattlePileKind.Discard
            : BattlePileKind.Draw;
        return SelectPileCardsAsync(
            player,
            kind,
            count,
            PileCardSelectionAction.Transform,
            replacementSkillId: replacementSkillId
        );
    }

    public Task<int> FilterTopDrawPileCardsAsync(PlayerCharacter player, int viewCount) =>
        SelectPileCardsAsync(
            player,
            BattlePileKind.Draw,
            viewCount,
            PileCardSelectionAction.FilterToDiscard,
            allowFewer: true,
            visibleCardCount: viewCount
        );

    private async Task<int> SelectPileCardsAsync(
        PlayerCharacter player,
        BattlePileKind kind,
        int count,
        PileCardSelectionAction action,
        BattleCardKeyword keyword = BattleCardKeyword.Retain,
        SkillID replacementSkillId = SkillID.None,
        bool allowFewer = false,
        int visibleCardCount = 0
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
        if (visibleCardCount > 0 && pile.Length > visibleCardCount)
            pile = pile.Take(visibleCardCount).ToArray();
        int availableCount = action == PileCardSelectionAction.MoveToHand
            ? Math.Min(pile.Length, BattleNode.GetPlayerTeamBattleHandEmptySlotCount())
            : pile.Length;
        if (availableCount <= 0)
            return 0;

        CancelDiscardSelection();
        CancelPileCardSelection();
        _pileCardSelectionKind = kind;
        _pileCardSelectionAction = action;
        _pileCardSelectionKeyword = keyword;
        _pileCardSelectionTransformSkillId = replacementSkillId;
        _pileCardSelectionTargetCount = Math.Min(count, availableCount);
        _pileCardSelectionAllowsFewer = allowFewer;
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
        CapturePileCardSelectionOverlaySnapshot(player, pile, openContext);
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
                    BuildPileCardSelectionTitle(
                        kind,
                        action,
                        _pileCardSelectionTargetCount,
                        keyword
                    ),
                    pile
                ),
            },
            openContext
        );
        RequestTurnUiRefresh();

        int movedCount = await _pileCardSelectionCompletion.Task;
        return Math.Max(0, movedCount);
    }

    private static string BuildPileCardSelectionTitle(
        BattlePileKind kind,
        PileCardSelectionAction action,
        int count,
        BattleCardKeyword keyword = BattleCardKeyword.Retain
    )
    {
        return action switch
        {
            PileCardSelectionAction.Exhaust => $"选择{count}张{GetPileTitle(kind)}消耗",
            PileCardSelectionAction.ApplyKeyword =>
                $"选择{count}张{GetPileTitle(kind)}为其添加{keyword.GetDisplayName()}",
            PileCardSelectionAction.Transform => $"选择{count}张{GetPileTitle(kind)}变化",
            PileCardSelectionAction.FilterToDiscard =>
                $"查看{GetPileTitle(kind)}顶部{count}张牌，选择任意张放入弃牌堆",
            _ => $"选择{count}张{GetPileTitle(kind)}加入手牌",
        };
    }

    private void CapturePileCardSelectionOverlaySnapshot(
        PlayerCharacter player,
        Battle.BattleCardPileEntry[] pile,
        PileOverlayOpenContext openContext
    )
    {
        _pileCardSelectionOverlayPlayer = player;
        _pileCardSelectionOverlayPile = pile?.ToArray()
            ?? Array.Empty<Battle.BattleCardPileEntry>();
        _pileCardSelectionOverlayOpenContext = openContext;
    }

    private void RestorePileCardSelectionOverlay()
    {
        if (!_isPileCardSelectionActive)
            return;

        PlayerCharacter player = _pileCardSelectionOverlayPlayer;
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            CancelPileCardSelection();
            HidePileOverlay();
            return;
        }

        var restoreContext = new PileOverlayOpenContext(
            PileOverlayOpenSource.CardSelection,
            _pileCardSelectionOverlayOpenContext.TriggerSkillId,
            _pileCardSelectionOverlayOpenContext.TriggerSkillName,
            flowStartUsec: Time.GetTicksUsec()
        );
        ShowPileOverlay(
            player,
            new[]
            {
                new BattlePileOverlaySection(
                    _pileCardSelectionKind,
                    BuildPileCardSelectionTitle(
                        _pileCardSelectionKind,
                        _pileCardSelectionAction,
                        _pileCardSelectionTargetCount,
                        _pileCardSelectionKeyword
                    ),
                    _pileCardSelectionOverlayPile
                ),
            },
            restoreContext
        );
        RequestTurnUiRefresh();
    }

    private void ClearPileCardSelectionOverlaySnapshot()
    {
        _pileCardSelectionOverlayPlayer = null;
        _pileCardSelectionOverlayPile = Array.Empty<Battle.BattleCardPileEntry>();
        _pileCardSelectionOverlayOpenContext = default;
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
            || (IsPileLockedByCardResolution() && !CanReadPileWhileCardSelectionIsHidden())
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

    private bool CanReadPileWhileCardSelectionIsHidden() =>
        _isPileCardSelectionActive && _pileOverlayContentTemporarilyHidden;

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
