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
    private static readonly Vector2 PileCardHolderPadding = new(14f, 18f);
    private Vector2 PileCardDisplaySize => BattleCardBaseSize * PileCardScale;
    private Vector2 PileCardHolderSize => PileCardDisplaySize + PileCardHolderPadding * 2f;
    private const float PileOverlayContentWidth = 1380f;
    private const int PileOverlayGridColumns = 5;
    private const int PileOverlayGridHSeparation = 18;
    private const int PileOverlayGridVSeparation = 34;
    private const int PileOverlaySectionSeparation = 48;
    private const int PileOverlayCardsCreatedPerFrame = 10;
    private const int PileOverlayVirtualizationThreshold = 24;
    private const int PileOverlayVirtualizationBufferRows = 2;
    private const int PileOverlayMaxPooledCardHolders = 96;
    private const float PileOverlayFadeInDuration = 0.12f;
    private const float PileOverlayFadeOutDuration = 0.10f;
    private const int PileOverlayAnimatedCardCount = 12;
    private const float PileOverlayCardEntryYOffset = 28f;
    private const float PileOverlayCardEntryDuration = 0.22f;
    private const float PileOverlayCardEntryStagger = 0.018f;

    private const float PileButtonReceivePulseDuration = 0.24f;

    private const int BattlePileOverlayLayer = 100;

    private const string PileButtonCountLabelName = "CountLabel";

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
    private ColorRect _pileOverlayMaskInputTarget;
    private Button _pileOverlayConfirmButton;
    private Button _pileOverlayConfirmButtonInputTarget;
    private Tween _pileOverlayFadeTween;
    private readonly Stack<Control> _pileCardHolderPool = new();
    private readonly List<PileOverlayVirtualGrid> _pileOverlayVirtualGrids = new();
    private readonly Dictionary<SkillCard, Action> _pileCardSelectionPressHandlers = new();

    private bool _isPileCardSelectionActive;
    private BattlePileKind _pileCardSelectionKind;
    private PileCardSelectionAction _pileCardSelectionAction;
    private int _pileCardSelectionTargetCount;
    private readonly HashSet<int> _pileCardSelectionIndexes = new();
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

    private sealed class PileOverlayBuildState
    {
        public int Version { get; init; }
        public int TotalCards { get; init; }
        public int CardsCreated { get; set; }
    }

    private sealed class PileOverlayVirtualGrid
    {
        public BattlePileKind Kind { get; init; }
        public PlayerCharacter FallbackPlayer { get; init; }
        public Control Grid { get; init; }
        public IndexedBattlePileEntry[] Entries { get; init; }
        public readonly List<Control> Holders = new();
        public bool EntryAnimationPlayed { get; set; }
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
        ShowPileOverlay(
            player,
            new[]
            {
                new BattlePileOverlaySection(
                    kind,
                    BuildPileCardSelectionTitle(kind, action, _pileCardSelectionTargetCount),
                    pile
                ),
            }
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
        EnsurePileButtonCountLabel(button);
        ConfigurePileButtonIcon(button);
        button.PivotOffset = button.Size / 2f;
        button.Resized += () =>
        {
            if (button == null || !GodotObject.IsInstanceValid(button))
                return;

            ConfigurePileButtonIcon(button);
            button.PivotOffset = button.Size / 2f;
            PositionPileButtonCountLabel(button);
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

    private static void ConfigurePileButtonIcon(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (button == null || icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        Vector2 size = button.Size;
        if (size == Vector2.Zero)
            size = button.CustomMinimumSize;
        if (size == Vector2.Zero)
            size = new Vector2(80f, 80f);

        icon.Position = Vector2.Zero;
        icon.Size = size;
        icon.CustomMinimumSize = size;
        icon.PivotOffset = size * 0.5f;
        icon.MouseFilter = MouseFilterEnum.Ignore;
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

    private static Label EnsurePileButtonCountLabel(Button button)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return null;

        Label label = button.GetNodeOrNull<Label>(PileButtonCountLabelName);
        if (label == null)
        {
            label = new Label
            {
                Name = PileButtonCountLabelName,
                MouseFilter = MouseFilterEnum.Ignore,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ZIndex = 12,
            };
            label.AddThemeFontSizeOverride("font_size", 18);
            label.AddThemeColorOverride("font_color", new Color(0.96f, 0.98f, 1f, 1f));
            label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.04f, 0.06f, 0.95f));
            label.AddThemeConstantOverride("outline_size", 3);
            button.AddChild(label);
        }

        PositionPileButtonCountLabel(button);
        return label;
    }

    private static void SetPileButtonCount(Button button, int count)
    {
        Label label = EnsurePileButtonCountLabel(button);
        if (label == null)
            return;

        label.Text = Math.Max(0, count).ToString();
        label.Modulate = button.Disabled
            ? new Color(0.70f, 0.78f, 0.84f, 0.62f)
            : Colors.White;
        label.Visible = true;
    }

    private static void PositionPileButtonCountLabel(Button button)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        Label label = button.GetNodeOrNull<Label>(PileButtonCountLabelName);
        if (label == null)
            return;

        Vector2 size = button.Size;
        if (size == Vector2.Zero)
            size = button.CustomMinimumSize;

        float width = Math.Max(48f, size.X);
        label.Size = new Vector2(width, 24f);
        label.Position = new Vector2((size.X - width) * 0.5f, size.Y - 14f);
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
            shader.SetShaderParameter("receive_amount", 0f);
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

    private void PulsePileButtonReceive(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (icon == null || !GodotObject.IsInstanceValid(icon))
            return;

        icon.PivotOffset = icon.Size / 2f;
        Tween scaleTween = icon.CreateTween();
        scaleTween.SetParallel(false);
        scaleTween
            .TweenProperty(icon, "scale", new Vector2(1.16f, 1.16f), PileButtonReceivePulseDuration * 0.42f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        scaleTween
            .TweenProperty(icon, "scale", Vector2.One, PileButtonReceivePulseDuration * 0.58f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.InOut);

        if (icon.Material is not ShaderMaterial shader)
            return;

        icon.CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                {
                    if (
                        GodotObject.IsInstanceValid(icon)
                        && icon.Material is ShaderMaterial liveShader
                    )
                        liveShader.SetShaderParameter("receive_amount", value);
                }),
                0f,
                1f,
                PileButtonReceivePulseDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
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

    private static Vector2 GetPileButtonVisualCenter(Button button)
    {
        Control icon = GetPileButtonIcon(button);
        if (
            icon != null
            && GodotObject.IsInstanceValid(icon)
            && icon.IsInsideTree()
            && icon.GetGlobalRect().Size.LengthSquared() > 1f
        )
        {
            return icon.GetGlobalRect().GetCenter();
        }

        return button.GetGlobalRect().GetCenter();
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


    private void UpdatePileButtons()
    {
        int drawCount = 0;
        int discardCount = 0;
        int exhaustedCount = 0;
        bool canReadPile =
            _activePlayer != null
            && GodotObject.IsInstanceValid(_activePlayer)
            && _activePlayer.State != Character.CharacterState.Dying
            && BattleNode != null
            && GodotObject.IsInstanceValid(BattleNode);
        bool canOpenPile =
            canReadPile
            && !IsPileLockedByCardResolution()
            && !IsManualTargetSelectionPending();

        if (canReadPile)
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
            SetPileButtonCount(_drawPileButton, drawCount);
            SyncPileButtonVisualState(_drawPileButton);
        }

        if (_discardPileButton != null)
        {
            _discardPileButton.Text = string.Empty;
            _discardPileButton.TooltipText = $"弃牌堆 {discardCount}";
            _discardPileButton.Disabled = !canOpenPile;
            SetPileButtonCount(_discardPileButton, discardCount);
            SyncPileButtonVisualState(_discardPileButton);
        }

        if (_exhaustedPileButton != null)
        {
            _exhaustedPileButton.Text = string.Empty;
            _exhaustedPileButton.TooltipText = $"消耗牌堆 {exhaustedCount}";
            _exhaustedPileButton.Disabled = !canOpenPile;
            SetPileButtonCount(_exhaustedPileButton, exhaustedCount);
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
            }
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

    private void ShowPileOverlay(PlayerCharacter player, IReadOnlyList<BattlePileOverlaySection> sections)
    {
        EnsurePileOverlayUi();
        int buildVersion = ++_pileOverlayBuildVersion;
        ClearPileOverlayCards();

        if (_pileOverlayRoot == null || _pileOverlaySections == null)
            return;

        _pileOverlayFadeTween?.Kill();
        bool wasVisible = _pileOverlayRoot.Visible && _pileOverlayRoot.Modulate.A > 0f;
        if (!wasVisible)
            SetPileOverlayRootAlpha(0f);
        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Stop;
        _pileOverlayRoot.MoveToFront();
        _pileOverlaySections.Visible = true;
        SyncPileOverlayConfirmButton();
        if (_pileOverlayScroll != null && GodotObject.IsInstanceValid(_pileOverlayScroll))
            _pileOverlayScroll.ScrollVertical = 0;

        TweenPileOverlayRootAlpha(1f, PileOverlayFadeInDuration);
        _ = PopulatePileOverlayAsync(player, sections, buildVersion);
    }

    private async Task PopulatePileOverlayAsync(
        PlayerCharacter player,
        IReadOnlyList<BattlePileOverlaySection> sections,
        int buildVersion
    )
    {
        var state = new PileOverlayBuildState
        {
            Version = buildVersion,
            TotalCards = sections?.Sum(section => section.Pile?.Length ?? 0) ?? 0,
        };
        foreach (BattlePileOverlaySection section in sections ?? Array.Empty<BattlePileOverlaySection>())
        {
            if (!IsPileOverlayBuildCurrent(state))
                return;

            await AddPileOverlaySection(_pileOverlaySections, player, section, state);
        }

        if (!IsPileOverlayBuildCurrent(state))
            return;

        _pileOverlaySections.QueueSort();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (IsPileOverlayBuildCurrent(state))
            RefreshPileOverlayVirtualGrids(playEntryAnimation: true);
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
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            groups.AddThemeConstantOverride("separation", 22);
            sectionRoot.AddChild(groups);
        }

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
                AddPileOverlayVirtualGrid(sectionRoot, player, section, indexedPile);
                return;
            }

            foreach (IndexedBattlePileEntry indexedEntry in indexedPile)
            {
                if (!IsPileOverlayBuildCurrent(state))
                    return;

                var holder = CreatePilePreviewCardHolder(
                    indexedEntry.Entry.Owner ?? player,
                    indexedEntry.Entry.SkillId,
                    out SkillCard card
                );
                if (holder == null || card == null)
                    continue;

                grid.AddChild(holder);
                ApplyPilePreviewCard(card, indexedEntry.Entry.Owner ?? player, indexedEntry.Entry.SkillId);
                card.ResetState();
                card.HoverHint.Visible = false;
                ConfigurePileSelectionCard(card, section.Kind, indexedEntry.Index);
                PlayPileOverlayCardEntryAnimation(card, state);
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
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            sectionRoot.AddChild(virtualGrid);
        }
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
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
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
                ApplyPilePreviewCard(card, entryOwner, indexedEntry.Entry.SkillId);
                card.ResetState();
                card.HoverHint.Visible = false;
                ConfigurePileSelectionCard(card, kind, indexedEntry.Index);
                PlayPileOverlayCardEntryAnimation(card, state);
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
        if (state.CardsCreated % PileOverlayCardsCreatedPerFrame != 0)
            return;

        SceneTree tree = GetTree();
        if (tree != null)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private void OnPileOverlayScrollChanged(double _)
    {
        RefreshPileOverlayVirtualGrids(playEntryAnimation: false);
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

            RefreshPileOverlayVirtualGrid(virtualGrid, visibleRect, playEntryAnimation);
            virtualGrid.EntryAnimationPlayed = true;
        }
    }

    private void RefreshPileOverlayVirtualGrid(
        PileOverlayVirtualGrid virtualGrid,
        Rect2 visibleRect,
        bool playEntryAnimation
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
        int firstRow = Mathf.Max(
            0,
            Mathf.FloorToInt(visibleTop / rowHeight) - PileOverlayVirtualizationBufferRows
        );
        int lastRow = Mathf.Min(
            Mathf.CeilToInt(totalCards / (float)PileOverlayGridColumns) - 1,
            Mathf.CeilToInt(visibleBottom / rowHeight) + PileOverlayVirtualizationBufferRows
        );
        if (lastRow < firstRow)
            lastRow = firstRow;

        int firstIndex = firstRow * PileOverlayGridColumns;
        int visibleSlotCount = Math.Min(
            totalCards - firstIndex,
            (lastRow - firstRow + 1) * PileOverlayGridColumns
        );
        EnsurePileOverlayVirtualHolderCount(virtualGrid, visibleSlotCount);

        for (int localIndex = 0; localIndex < virtualGrid.Holders.Count; localIndex++)
        {
            Control holder = virtualGrid.Holders[localIndex];
            if (holder == null || !GodotObject.IsInstanceValid(holder))
                continue;

            int pileEntryIndex = firstIndex + localIndex;
            if (localIndex >= visibleSlotCount || pileEntryIndex >= totalCards)
            {
                holder.Visible = false;
                continue;
            }

            IndexedBattlePileEntry indexedEntry = virtualGrid.Entries[pileEntryIndex];
            int row = pileEntryIndex / PileOverlayGridColumns;
            int column = pileEntryIndex % PileOverlayGridColumns;
            holder.Position = new Vector2(
                column * (PileCardHolderSize.X + PileOverlayGridHSeparation),
                row * rowHeight
            );
            holder.Visible = true;

            SkillCard card = holder.GetChildren().OfType<SkillCard>().FirstOrDefault();
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            PlayerCharacter owner = indexedEntry.Entry.Owner ?? virtualGrid.FallbackPlayer;
            ApplyPilePreviewCard(card, owner, indexedEntry.Entry.SkillId);
            card.ResetState();
            card.HoverHint.Visible = false;
            ConfigurePileSelectionCard(card, virtualGrid.Kind, indexedEntry.Index);

            if (playEntryAnimation && !virtualGrid.EntryAnimationPlayed)
                PlayPileOverlayCardEntryAnimation(card, localIndex, visibleSlotCount);
        }
    }

    private void EnsurePileOverlayVirtualHolderCount(
        PileOverlayVirtualGrid virtualGrid,
        int visibleSlotCount
    )
    {
        visibleSlotCount = Math.Max(0, visibleSlotCount);
        while (virtualGrid.Holders.Count < visibleSlotCount)
        {
            Control holder = CreateEmptyPilePreviewCardHolder(out _);
            if (holder == null)
                break;

            virtualGrid.Grid.AddChild(holder);
            virtualGrid.Holders.Add(holder);
        }
    }

    private static void PlayPileOverlayCardEntryAnimation(
        SkillCard card,
        PileOverlayBuildState state
    )
    {
        if (card == null || state == null)
            return;

        PlayPileOverlayCardEntryAnimation(card, state.CardsCreated, state.TotalCards);
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
                Layer = BattlePileOverlayLayer,
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
        if (_pileOverlayScroll != scroll)
        {
            if (
                _pileOverlayVScrollBar != null
                && GodotObject.IsInstanceValid(_pileOverlayVScrollBar)
            )
                _pileOverlayVScrollBar.ValueChanged -= OnPileOverlayScrollChanged;

            _pileOverlayScroll = scroll;
            _pileOverlayVScrollBar = _pileOverlayScroll.GetVScrollBar();
            if (_pileOverlayVScrollBar != null)
                _pileOverlayVScrollBar.ValueChanged += OnPileOverlayScrollChanged;
        }

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
        EnsurePileOverlayConfirmButton();
        SyncPileOverlayConfirmButton();
    }

    private void EnsurePileOverlayConfirmButton()
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayConfirmButton = _pileOverlayRoot.GetNodeOrNull<Button>("PileConfirmButton");
        if (_pileOverlayConfirmButton == null)
        {
            _pileOverlayConfirmButton = new Button { Name = "PileConfirmButton" };
            _pileOverlayRoot.AddChild(_pileOverlayConfirmButton);
        }

        if (_pileOverlayConfirmButtonInputTarget != _pileOverlayConfirmButton)
        {
            if (
                _pileOverlayConfirmButtonInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayConfirmButtonInputTarget)
            )
            {
                _pileOverlayConfirmButtonInputTarget.Pressed -= OnPileOverlayConfirmPressed;
            }

            _pileOverlayConfirmButton.Pressed += OnPileOverlayConfirmPressed;
            _pileOverlayConfirmButtonInputTarget = _pileOverlayConfirmButton;
        }

        _pileOverlayConfirmButton.CustomMinimumSize = new Vector2(170f, 58f);
        _pileOverlayConfirmButton.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _pileOverlayConfirmButton.OffsetLeft = -236f;
        _pileOverlayConfirmButton.OffsetTop = -86f;
        _pileOverlayConfirmButton.OffsetRight = -66f;
        _pileOverlayConfirmButton.OffsetBottom = -28f;
        _pileOverlayConfirmButton.MouseFilter = MouseFilterEnum.Stop;
        _pileOverlayConfirmButton.FocusMode = FocusModeEnum.None;
        _pileOverlayConfirmButton.ZIndex = 50;
        ConfigureEndTurnButton(_pileOverlayConfirmButton);
        _pileOverlayConfirmButton.Text = "确认";
        _pileOverlayConfirmButton.MoveToFront();
    }

    private void SyncPileOverlayConfirmButton()
    {
        if (
            _pileOverlayConfirmButton == null
            || !GodotObject.IsInstanceValid(_pileOverlayConfirmButton)
        )
        {
            return;
        }

        bool visible = _isPileCardSelectionActive && IsPileOverlayVisible();
        _pileOverlayConfirmButton.Visible = visible;
        _pileOverlayConfirmButton.Disabled =
            !visible || _pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount;
        if (visible)
            _pileOverlayConfirmButton.MoveToFront();
    }

    private void OnPileOverlayConfirmPressed()
    {
        if (!_isPileCardSelectionActive)
            return;

        _ = CompletePileCardSelectionAsync();
    }

    private void ConfigurePileOverlayRoot(Control root)
    {
        if (root == null)
            return;

        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.MouseFilter = MouseFilterEnum.Stop;
        if (_pileOverlayRootInputTarget != root)
        {
            if (
                _pileOverlayRootInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayRootInputTarget)
            )
                _pileOverlayRootInputTarget.GuiInput -= OnPileOverlayBackgroundGuiInput;

            root.GuiInput += OnPileOverlayBackgroundGuiInput;
            _pileOverlayRootInputTarget = root;
        }
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
        if (_pileOverlayMaskInputTarget != mask)
        {
            if (
                _pileOverlayMaskInputTarget != null
                && GodotObject.IsInstanceValid(_pileOverlayMaskInputTarget)
            )
                _pileOverlayMaskInputTarget.GuiInput -= OnPileOverlayBackgroundGuiInput;

            mask.GuiInput += OnPileOverlayBackgroundGuiInput;
            _pileOverlayMaskInputTarget = mask;
        }
    }

    private void OnPileOverlayBackgroundGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
        {
            if (_isPileCardSelectionActive)
            {
                GetViewport().SetInputAsHandled();
                return;
            }

            HidePileOverlay();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ConfigurePileSelectionCard(SkillCard card, BattlePileKind kind, int pileIndex)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        ClearPileSelectionCardBinding(card);
        bool selectable =
            _isPileCardSelectionActive
            && kind == _pileCardSelectionKind
            && pileIndex >= 0;
        if (!selectable)
            return;

        _pileCardSelectionCards[pileIndex] = card;
        card.Button.ToggleMode = true;
        bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
        card.Button.ButtonPressed = selected;
        card.Button.Disabled = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(selected);
        Action handler = () => _ = HandlePileCardSelectionPressedAsync(pileIndex);
        _pileCardSelectionPressHandlers[card] = handler;
        card.Button.Pressed += handler;
    }

    private void ClearPileSelectionCardBinding(SkillCard card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        if (_pileCardSelectionPressHandlers.TryGetValue(card, out Action handler))
        {
            card.Button.Pressed -= handler;
            _pileCardSelectionPressHandlers.Remove(card);
        }

        foreach (int key in _pileCardSelectionCards
            .Where(pair => pair.Value == card)
            .Select(pair => pair.Key)
            .ToArray())
        {
            _pileCardSelectionCards.Remove(key);
        }

        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Modulate = SkillButton.EnabledModulate;
        card.SetPlayableHighlight(false, instant: true);
    }

    private Task HandlePileCardSelectionPressedAsync(int pileIndex)
    {
        if (
            !_isPileCardSelectionActive
            || pileIndex < 0
            || BattleNode == null
            || !GodotObject.IsInstanceValid(BattleNode)
        )
        {
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Contains(pileIndex))
        {
            _pileCardSelectionIndexes.Remove(pileIndex);
            RefreshPileCardSelectionVisuals();
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        if (_pileCardSelectionIndexes.Count >= _pileCardSelectionTargetCount)
        {
            RefreshPileCardSelectionVisuals();
            RefreshTurnUi();
            return Task.CompletedTask;
        }

        _pileCardSelectionIndexes.Add(pileIndex);
        RefreshPileCardSelectionVisuals();
        RefreshTurnUi();
        return Task.CompletedTask;
    }

    private async Task CompletePileCardSelectionAsync()
    {
        if (!_isPileCardSelectionActive)
            return;

        if (_pileCardSelectionIndexes.Count < _pileCardSelectionTargetCount)
        {
            RefreshPileCardSelectionVisuals();
            RefreshTurnUi();
            return;
        }

        int selectedCount = _pileCardSelectionAction == PileCardSelectionAction.Exhaust
            ? ExhaustSelectedPileCards(_pileCardSelectionKind, _pileCardSelectionIndexes)
            : MoveSelectedPileCardsToHand(_pileCardSelectionKind, _pileCardSelectionIndexes);
        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        ClearAllPileSelectionCardBindings();
        HidePileOverlay();
        RefreshTurnUi();
        completion?.TrySetResult(selectedCount);
        await Task.CompletedTask;
    }

    private int ExhaustSelectedPileCards(
        BattlePileKind kind,
        IEnumerable<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        BattleCardPileTarget pileTarget = kind == BattlePileKind.Discard
            ? BattleCardPileTarget.DiscardPileCards
            : BattleCardPileTarget.DrawPileCards;
        int exhaustedCount = 0;
        int removedBefore = 0;
        foreach (int originalIndex in (selectedIndexes ?? Array.Empty<int>()).Distinct().OrderBy(x => x))
        {
            int currentIndex = originalIndex - removedBefore;
            if (!BattleNode.TryExhaustBattlePileCard(pileTarget, currentIndex, refreshUi: false))
                continue;

            exhaustedCount++;
            removedBefore++;
        }

        return exhaustedCount;
    }

    private int MoveSelectedPileCardsToHand(
        BattlePileKind kind,
        IEnumerable<int> selectedIndexes
    )
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return 0;

        int movedCount = 0;
        int removedBefore = 0;
        foreach (int originalIndex in (selectedIndexes ?? Array.Empty<int>()).Distinct().OrderBy(x => x))
        {
            int currentIndex = originalIndex - removedBefore;
            int handIndex = GetActiveHandFirstEmptyIndex();
            bool moved = kind switch
            {
                BattlePileKind.Draw => BattleNode.TryMoveDrawPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                BattlePileKind.Discard => BattleNode.TryMoveDiscardPileCardToTeamHand(
                    _activePlayer,
                    currentIndex,
                    refreshUi: false
                ),
                _ => false,
            };
            if (!moved)
                continue;

            SetHandEntryStartPositionFromPileKind(handIndex, kind);
            movedCount++;
            removedBefore++;
        }

        return movedCount;
    }

    private int GetActiveHandFirstEmptyIndex()
    {
        Skill[] hand = GetActiveHandSkills();
        return hand == null ? -1 : Array.FindIndex(hand, skill => skill == null);
    }

    private void SetHandEntryStartPositionFromPileKind(int handIndex, BattlePileKind kind)
    {
        if (!IsCardIndexValid(handIndex))
            return;

        Button sourceButton = kind switch
        {
            BattlePileKind.Draw => _drawPileButton,
            BattlePileKind.Discard => _discardPileButton,
            BattlePileKind.Exhausted => _exhaustedPileButton,
            _ => null,
        };
        Vector2? start = GetHandEntryStartPositionFromButton(sourceButton);
        if (start.HasValue)
            _customDrawEntryStartPositions[handIndex] = start.Value;
    }

    private Vector2? GetHandEntryStartPositionFromButton(Button button)
    {
        if (
            button == null
            || !GodotObject.IsInstanceValid(button)
            || !button.IsInsideTree()
            || _cardRow == null
            || !GodotObject.IsInstanceValid(_cardRow)
        )
        {
            return null;
        }

        Vector2 cardSize = BattleCardBaseSize * BattleCardScale;
        Vector2 sourceCenter = GetPileButtonVisualCenter(button);
        Vector2 cardRowOrigin = _cardRow.GetGlobalRect().Position;
        return sourceCenter - cardRowOrigin - cardSize * 0.5f;
    }

    private void RefreshPileCardSelectionVisuals()
    {
        foreach ((int pileIndex, SkillCard card) in _pileCardSelectionCards)
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool selected = _pileCardSelectionIndexes.Contains(pileIndex);
            card.Button.ButtonPressed = selected;
            card.Modulate = SkillButton.EnabledModulate;
            card.SetPlayableHighlight(selected);
        }
        SyncPileOverlayConfirmButton();
    }

    private void CancelPileCardSelection()
    {
        TaskCompletionSource<int> completion = _pileCardSelectionCompletion;
        _isPileCardSelectionActive = false;
        _pileCardSelectionAction = PileCardSelectionAction.MoveToHand;
        _pileCardSelectionTargetCount = 0;
        _pileCardSelectionIndexes.Clear();
        _pileCardSelectionCards.Clear();
        _pileCardSelectionCompletion = null;
        ClearAllPileSelectionCardBindings();
        SyncPileOverlayConfirmButton();
        completion?.TrySetResult(0);
    }

    private void ClearAllPileSelectionCardBindings()
    {
        foreach ((SkillCard card, Action handler) in _pileCardSelectionPressHandlers.ToArray())
        {
            if (card != null && GodotObject.IsInstanceValid(card))
            {
                card.Button.Pressed -= handler;
                card.Button.ToggleMode = false;
                card.Button.ButtonPressed = false;
                card.Modulate = SkillButton.EnabledModulate;
                card.SetPlayableHighlight(false, instant: true);
            }
        }

        _pileCardSelectionPressHandlers.Clear();
    }

    private Control CreatePilePreviewCardHolder(
        PlayerCharacter player,
        SkillID skillId,
        out SkillCard card
    )
    {
        if (Skill.GetSkill(skillId) == null)
        {
            card = null;
            return null;
        }

        return CreateEmptyPilePreviewCardHolder(out card);
    }

    private Control CreateEmptyPilePreviewCardHolder(out SkillCard card)
    {
        Control holder = RentPileCardHolder();
        card = holder.GetChildren().OfType<SkillCard>().FirstOrDefault();
        if (card == null)
        {
            card = SkillCardScene.Instantiate<SkillCard>();
            holder.AddChild(card);
        }

        holder.Name = "PileCardHolder";
        holder.CustomMinimumSize = PileCardHolderSize;
        holder.Size = PileCardHolderSize;
        holder.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        holder.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        holder.MouseFilter = MouseFilterEnum.Ignore;
        holder.ClipContents = false;
        holder.Visible = true;
        card.Position =
            PileCardHolderPadding - 0.5f * (Vector2.One - PileCardScale) * BattleCardBaseSize;
        card.PivotOffset = BattleCardBaseSize * 0.5f;
        return holder;
    }

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

    private static void PlayPileOverlayCardEntryAnimation(
        SkillCard card,
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
            return;
        }

        float delay = Math.Min(cardIndex, PileOverlayAnimatedCardCount - 1)
            * PileOverlayCardEntryStagger;

        card.Position = targetPosition + new Vector2(0f, PileOverlayCardEntryYOffset);
        Color targetModulate = SkillButton.EnabledModulate;
        card.Modulate = new Color(
            targetModulate.R,
            targetModulate.G,
            targetModulate.B,
            0f
        );
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
    }

    private static void ApplyPilePreviewCard(SkillCard card, PlayerCharacter player, SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        if (skill == null || card == null)
            return;

        bool isStatusCard = skill.IsStatusCard;
        if (!isStatusCard)
            skill.OwnerCharater = player;

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
    }

    private static bool IsStatusSkillId(SkillID skillId) => Skill.GetSkill(skillId)?.IsStatusCard == true;

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

        SkillCard card = holder.GetChildren().OfType<SkillCard>().FirstOrDefault();
        if (card == null || !GodotObject.IsInstanceValid(card) || card.Button.ToggleMode)
            return false;

        ClearPileSelectionCardBinding(card);
        card.HideHoverUi();
        card.RestoreDisplayState();
        card.Button.Disabled = true;
        card.Button.ToggleMode = false;
        card.Button.ButtonPressed = false;
        card.Modulate = SkillButton.EnabledModulate;
        holder.Visible = false;
        _pileCardHolderPool.Push(holder);
        return true;
    }

    private bool IsPileOverlayVisible()
    {
        return _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && _pileOverlayRoot.Modulate.A > 0.01f;
    }

    private void HidePileOverlay()
    {
        if (_isPileCardSelectionActive)
            CancelPileCardSelection();

        int hideVersion = ++_pileOverlayBuildVersion;
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayRoot.MouseFilter = MouseFilterEnum.Ignore;
        SyncPileOverlayConfirmButton();
        _pileOverlayFadeTween?.Kill();
        _pileOverlayFadeTween = _pileOverlayRoot.CreateTween();
        _pileOverlayFadeTween
            .TweenMethod(
                Callable.From<float>(SetPileOverlayRootAlpha),
                _pileOverlayRoot.Modulate.A,
                0f,
                PileOverlayFadeOutDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _pileOverlayFadeTween.Finished += () =>
        {
            if (
                hideVersion != _pileOverlayBuildVersion
                || _pileOverlayRoot == null
                || !GodotObject.IsInstanceValid(_pileOverlayRoot)
            )
            {
                return;
            }

            _pileOverlayRoot.Visible = false;
            SetPileOverlayRootAlpha(1f);
            ClearPileOverlayCards();
            RefreshTurnUi();
        };
    }

    private void TweenPileOverlayRootAlpha(float alpha, float duration)
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayFadeTween?.Kill();
        _pileOverlayFadeTween = _pileOverlayRoot.CreateTween();
        _pileOverlayFadeTween
            .TweenMethod(
                Callable.From<float>(SetPileOverlayRootAlpha),
                _pileOverlayRoot.Modulate.A,
                alpha,
                duration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void SetPileOverlayRootAlpha(float alpha)
    {
        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        Color modulate = _pileOverlayRoot.Modulate;
        modulate.A = alpha;
        _pileOverlayRoot.Modulate = modulate;
    }

}
