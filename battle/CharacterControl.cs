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

    private static readonly Vector2 BattleCardBaseSize = new(240f, 370f);
    private static readonly Vector2 BattleCardScale = new(0.936f, 0.936f);
    private const float EndTurnCardVanishDuration = 0.32f;
    private const float CardPlayVanishDuration = 0.42f;
    private const float CardPlayMoveDuration = 0.18f;
    private const float CardHoverLiftY = -22f;
    private const float CardHoverScaleMultiplier = 1.03f;
    private const float CarryCardSpawnScaleMultiplier = 0.72f;
    private const float HandAreaPadding = 18f;
    private const float HandCardGap = 14f;
    private const float HandLayoutTweenDuration = 0.18f;
    private const int CardPlayOverlayLayer = 80;
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
    private Button _endTurnButton;
    private SkillCard[] _cards = new SkillCard[HandCardCapacity];
    private Control[] _cardSlots = new Control[HandCardCapacity];
    private Tween[] _cardSlotLayoutTweens = new Tween[HandCardCapacity];
    private Vector2?[] _cardSlotLayoutTargets = new Vector2?[HandCardCapacity];
    private readonly Queue<QueuedCardPlay> _queuedCardPlays = new();
    private readonly Queue<QueuedCardPlay> _queuedFollowUpCardPlays = new();
    private readonly HashSet<int> _queuedCardIndices = new();
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

    public override void _Ready()
    {
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
            && _liftedCardIndex != -1
        )
        {
            ClearLiftedCard(instant: false);
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

    private bool TryBindActionAreaUiFromScene()
    {
        _root = GetNodeOrNull<VBoxContainer>("ActionAreaRoot");
        _statusLabel = GetNodeOrNull<Label>("ActionAreaRoot/StatusLabel");
        _cardRow = GetNodeOrNull<Control>("ActionAreaRoot/CardRow");
        _endTurnButton =
            GetNodeOrNull<Button>("EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/EndTurnButton")
            ?? GetNodeOrNull<Button>("ActionAreaRoot/CardRow/EndTurnButton");

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

        if (_endTurnButton == null)
        {
            _endTurnButton = new Button { Name = "EndTurnButton" };
            AddChild(_endTurnButton);
        }

        _endTurnButton.Disabled = true;
        ConfigureEndTurnButton(_endTurnButton);
        _endTurnButton.Pressed += OnEndTurnPressed;
        LayoutActionCards(instant: true);
        return true;
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

        _endTurnButton = new Button
        {
            Name = "EndTurnButton",
            Text = "结束回合",
            Disabled = true,
        };
        ConfigureEndTurnButton(_endTurnButton);
        _endTurnButton.Pressed += OnEndTurnPressed;
        AddChild(_endTurnButton);
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

        button.Text = "\u7ed3\u675f\u56de\u5408";
        button.CustomMinimumSize = new Vector2(164f, 72f);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
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

    private void RefreshTurnUi()
    {
        if (!_uiBuilt)
            return;

        bool updateLayout = !_suppressNextRefreshLayout;
        _suppressNextRefreshLayout = false;
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
            return;
        }

        _statusLabel.Text =
            $"{_activePlayer.CharacterName} 回合中 | 当前能量 {_activePlayer.Energy}";

        for (int i = 0; i < _cards.Length; i++)
        {
            SkillCard card = _cards[i];
            if (card == null)
                continue;

            Skill skill =
                i < _activePlayer.Skills.Length ? _activePlayer.Skills[i] : null;
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
                !_cardDisplayInitialized[i] || _displayedSkillIds[i] != skill.SkillId;
            bool movedFromAnotherSlot =
                isNewCardForHand && WasSkillAlreadyDisplayed(previousDisplayedSkillIds, skill.SkillId);
            bool shouldAnimate = isNewCardForHand && !movedFromAnotherSlot;

            card.Visible = true;
            if (shouldAnimate)
                card.ResetState();

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

            _displayedSkillIds[i] = skill.SkillId;
            _cardDisplayInitialized[i] = true;

            bool canInteract = !_isResolvingCard && !_endTurnQueued && !isCommitted;
            bool canUse = canInteract && skill.CanUseCurrentEnergy();
            card.Button.Disabled = !canInteract;
            card.Modulate = isCommitted
                ? QueuedCardModulate
                : canUse
                    ? SkillButton.EnabledModulate
                    : SkillButton.DisabledModulate;
        }

        if (_endTurnButton != null)
            _endTurnButton.Disabled = _isResolvingCard || _endTurnQueued;

        if (updateLayout)
            LayoutActionCards();
    }

    private static bool WasSkillAlreadyDisplayed(SkillID?[] previousDisplayedSkillIds, SkillID? skillId)
    {
        return skillId.HasValue
            && previousDisplayedSkillIds != null
            && previousDisplayedSkillIds.Any(id => id == skillId);
    }

    private Task HandleCardPressedAsync(int index, bool allowSuppressedPress = false)
    {
        if (_suppressCardButtonPressUntilLeftRelease && !allowSuppressedPress)
            return Task.CompletedTask;

        if (
            _isResolvingCard
            || _endTurnQueued
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

        await PlayCardVanishAfterExecutionAsync(play.Card);

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
        return
            play?.Actor != null
            && GodotObject.IsInstanceValid(play.Actor)
            && play.Actor.BattleNode != null
            && GodotObject.IsInstanceValid(play.Actor.BattleNode)
            && play.Actor == _activePlayer;
    }

    private void CompleteQueuedPlay(QueuedCardPlay play, bool succeeded)
    {
        play?.Completion?.TrySetResult(succeeded);
    }

    private async Task PlayCardVanishAfterExecutionAsync(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
            return;

        card.Button.Disabled = true;
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
        card.ZIndex = 100;
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
        card.EnergyCost.Text = "耗能：0";
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
        card.EnergyCost.Text = "耗能：0";
        card.CharacterName.Text = $"{play.Actor.CharacterName} | 连携";
        card.Button.Disabled = true;
        card.HoverHint.Visible = false;
        card.ZIndex = 300;
        card.Scale = PlayedCardScale * CarryCardSpawnScaleMultiplier;
        card.GlobalPosition = GetScreenCenterCardPosition(card.Scale);
        card.StartAnimation();

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(card, "scale", PlayedCardScale, CardPlayMoveDuration)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(
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
        card.ZIndex = 100;
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
        _liftedCardIndex = -1;
        SetProcess(false);
        ResetCardMotion(index, instant);
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
        card.ZIndex = 100;
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
            _displayedSkillIds[i] = null;
            _cardDisplayInitialized[i] = false;
        }
    }

    private void ResetCardDisplayTracking(int index)
    {
        if (index < 0 || index >= _displayedSkillIds.Length)
            return;

        _displayedSkillIds[index] = null;
        _cardDisplayInitialized[index] = false;
    }

    private void OnEndTurnPressed()
    {
        _ = QueueEndTurnAsync();
    }

    private bool CanUseEndTurnShortcut()
    {
        return
            Visible
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

        if (!_isProcessingCardQueue && _queuedCardPlays.Count == 0 && _queuedFollowUpCardPlays.Count == 0)
            await ExecuteQueuedEndTurnAsync();
    }

    private bool CanQueueEndTurn()
    {
        return
            _activePlayer != null
            && !_isResolvingCard
            && !_endTurnQueued
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
            card.Vanish();
        }

        await ToSignal(GetTree().CreateTimer(EndTurnCardVanishDuration), SceneTreeTimer.SignalName.Timeout);

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
        return
            _activePlayer != null
            && !_isResolvingCard
            && !_isProcessingCardQueue
            && _queuedCardPlays.Count == 0
            && _queuedFollowUpCardPlays.Count == 0
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying;
    }
}
