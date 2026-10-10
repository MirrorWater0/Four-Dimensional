using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

#nullable enable annotations

public readonly struct EventCardSelection(
    int playerIndex,
    SkillID skillId,
    SkillCard sourceCard,
    CardDeckOperationSnapshot? snapshot = null
)
{
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SkillId { get; } = skillId;
    public SkillCard SourceCard { get; } = sourceCard;
    public CardDeckOperationSnapshot? Snapshot { get; } =
        snapshot ?? CardDeckOperationSnapshot.FromCard(sourceCard);
}

public readonly struct EventCardSelectionEntry(
    int playerIndex,
    SkillID skillId,
    int count,
    string characterName,
    string characterKey,
    int power,
    int survivability
)
{
    public int PlayerIndex { get; } = playerIndex;
    public SkillID SkillId { get; } = skillId;
    public int Count { get; } = count;
    public string CharacterName { get; } = characterName;
    public string CharacterKey { get; } = characterKey;
    public int Power { get; } = power;
    public int Survivability { get; } = survivability;
}

public partial class EventCardSelectOverlay : Control
{
    private static readonly Vector2 SelectionCardScale = new(0.76f, 0.76f);
    private static readonly Vector2 SelectionCardPadding = new(9f, 12f);
    private static readonly Vector2 SelectionCardDisplaySize =
        CardPileOverlayUi.CardBaseSize * SelectionCardScale;
    private static readonly Vector2 SelectionCardHolderSize =
        SelectionCardDisplaySize + SelectionCardPadding * 2f;

    private const int WideSelectionGridColumns = 6;
    private const int MediumSelectionGridColumns = 4;
    private const int NarrowSelectionGridColumns = 3;
    private const int SelectionGridHSeparation = 14;
    private const int SelectionGridVSeparation = 20;
    private const float SelectionTopOffset = 108f;
    private const float SelectionBottomOffset = 136f;
    private const float SelectionActionWidth = 150f;
    private const float SelectionActionHeight = 46f;
    private const float SelectionActionGap = 12f;
    private const float SelectionActionBottomOffset = 18f;

    public event Action<EventCardSelection> CardSelected;
    public event Action<IReadOnlyList<EventCardSelection>> CardsConfirmed;
    public event Action SelectionCanceled;

    private Control _pileOverlayRoot;
    private ColorRect _mask;
    private ScrollContainer _scroll;
    private float _scrollBaseOffsetTop;
    private VBoxContainer _pileSections;
    private VBoxContainer _selectionSection;
    private Label _sectionTitle;
    private Label _emptyLabel;
    private GridContainer _cardGrid;
    private VBoxContainer _characterGroups;
    private Button _cancelButton;
    private Button _confirmButton;
    private Tween _fadeTween;
    private Tween _confirmRevealTween;
    private Tween _scrollBounceTween;
    private bool _isAnimating;
    private bool _uiReady;
    private bool _confirmRevealReady;
    private ScrollContainer _scrollInputTarget;
    private VScrollBar _vScrollBar;
    private bool _scrollBounceCheckPending;
    private float _pendingBounceDirection;
    private float _pendingBounceStrength;
    private bool _smoothScrollActive;
    private double _smoothScrollPosition;
    private double _smoothScrollTarget;
    private double _smoothScrollVelocity;
    private int _buildVersion;
    private int _selectionTargetCount = 1;
    private string _hintText = string.Empty;
    private readonly List<EventCardSelectionEntry> _entries = new();
    private readonly List<int> _selectedCounts = new();
    private readonly List<int> _selectionOrder = new();
    private readonly Dictionary<SkillCard, Action> _cardPressHandlers = new();
    private readonly Dictionary<SkillCard, int> _cardEntryIndexes = new();
    private readonly Dictionary<int, SkillCard> _entryCards = new();
    private ReadyButton _tacticsButton;
    private bool _tacticsButtonWasVisible;
    private bool _tacticsButtonWasDisabled;
    private bool _tacticsButtonSuspended;

    public override void _Ready()
    {
        SetProcessUnhandledInput(true);
        SetProcess(false);
        BuildUi();
        Visible = false;
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    public override void _Process(double delta)
    {
        UpdateSmoothScroll((float)delta);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!Visible || !MobilePlatform.IsCancelPress(inputEvent))
            return;

        OnCancelPressed();
        GetViewport()?.SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportSizeChanged;

        if (_scrollInputTarget != null && GodotObject.IsInstanceValid(_scrollInputTarget))
            _scrollInputTarget.GuiInput -= OnScrollGuiInput;
        if (_vScrollBar != null && GodotObject.IsInstanceValid(_vScrollBar))
            _vScrollBar.GuiInput -= OnScrollBarGuiInput;

        CancelSmoothScroll();
        CancelScrollBounce(resetOffsetTop: false);
        RestoreTacticsButton();
        base._ExitTree();
    }

    private void OnViewportSizeChanged()
    {
        if (Visible)
            UpdateFillParent();
    }

    public Task<EventCardSelection?> SelectOneAsync(
        IEnumerable<EventCardSelectionEntry> entries,
        string hintText
    )
    {
        var source = new TaskCompletionSource<EventCardSelection?>();
        Action<EventCardSelection> selected = null;
        Action canceled = null;
        selected = selection =>
        {
            CardSelected -= selected;
            SelectionCanceled -= canceled;
            source.TrySetResult(selection);
        };
        canceled = () =>
        {
            CardSelected -= selected;
            SelectionCanceled -= canceled;
            source.TrySetResult(null);
        };

        CardSelected += selected;
        SelectionCanceled += canceled;
        ShowSelection(entries, hintText);
        return source.Task;
    }

    public Task<IReadOnlyList<EventCardSelection>?> SelectManyAsync(
        IEnumerable<EventCardSelectionEntry> entries,
        string hintText,
        int count
    )
    {
        var source = new TaskCompletionSource<IReadOnlyList<EventCardSelection>?>();
        Action<IReadOnlyList<EventCardSelection>> confirmed = null;
        Action canceled = null;
        confirmed = selections =>
        {
            CardsConfirmed -= confirmed;
            SelectionCanceled -= canceled;
            source.TrySetResult(selections);
        };
        canceled = () =>
        {
            CardsConfirmed -= confirmed;
            SelectionCanceled -= canceled;
            source.TrySetResult(null);
        };

        CardsConfirmed += confirmed;
        SelectionCanceled += canceled;
        ShowSelection(entries, hintText, count);
        return source.Task;
    }

    public void ShowSelection(
        IEnumerable<EventCardSelectionEntry> entries,
        string hintText,
        int selectionCount = 1
    )
    {
        BuildUi();
        if (!EnsureUiReady())
            return;

        _entries.Clear();
        if (entries != null)
            _entries.AddRange(entries);

        _hintText = string.IsNullOrWhiteSpace(hintText) ? "请选择一张卡牌" : hintText;
        _selectionTargetCount = Math.Max(1, selectionCount);
        _selectedCounts.Clear();
        for (int i = 0; i < _entries.Count; i++)
            _selectedCounts.Add(0);
        _selectionOrder.Clear();
        _confirmRevealReady = false;
        UpdateSectionTitle();
        _cancelButton.Disabled = false;
        _cancelButton.Visible = true;
        SyncConfirmButton();

        UpdateFillParent();
        MoveToFront();
        ZIndex = 20;
        SuspendTacticsButton();

        int buildVersion = ++_buildVersion;
        _fadeTween?.Kill();
        ClearCards();
        _ = PopulateCardsAsync(buildVersion);

        bool wasVisible = Visible;
        ResetPresentation();
        Visible = true;
        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.Modulate = Colors.White;
        ResetScrollPosition();

        if (!wasVisible)
            PlayIntroAnimation();
        else
            SetPresentationFullyVisible();
    }

    public void HideSelection()
    {
        if (!Visible && !_isAnimating)
            return;

        PlayOutroAnimation();
    }

    public void DetachSelectionCardsForDeckOperations(
        Node caller,
        IReadOnlyList<EventCardSelection> selections
    )
    {
        if (selections == null || selections.Count == 0)
            return;

        var detachedCards = new HashSet<SkillCard>();
        foreach (EventCardSelection selection in selections)
        {
            SkillCard card = selection.SourceCard;
            if (card == null || !GodotObject.IsInstanceValid(card) || detachedCards.Contains(card))
                continue;

            BattleReady.TryDetachDeckOperationCard(caller, card);
            detachedCards.Add(card);
            _cardPressHandlers.Remove(card);
            _cardEntryIndexes.Remove(card);
            foreach ((int entryIndex, SkillCard entryCard) in _entryCards.ToArray())
            {
                if (entryCard == card)
                    _entryCards.Remove(entryIndex);
            }
        }
    }

    private void BuildUi()
    {
        if (_uiReady)
            return;

        SetAnchorsPreset(LayoutPreset.FullRect);
        ClipContents = false;
        MouseFilter = MouseFilterEnum.Stop;

        _pileOverlayRoot =
            CardPileOverlayUi.OverlayScene?.Instantiate<Control>()
            ?? new Control { Name = "PileOverlayRoot" };
        AddChild(_pileOverlayRoot);

        _mask = _pileOverlayRoot.GetNodeOrNull<ColorRect>("Mask");
        if (_mask == null)
        {
            _mask = new ColorRect { Name = "Mask" };
            _pileOverlayRoot.AddChild(_mask);
        }
        _mask.GuiInput += OnMaskGuiInput;

        _scroll = _pileOverlayRoot.GetNodeOrNull<ScrollContainer>("Scroll");
        if (_scroll == null)
        {
            GD.PushError("EventCardSelectOverlay: BattlePileOverlay scene is missing Scroll node.");
            return;
        }

        _scroll.ZIndex = CardPileOverlayUi.ContentZIndex;
        ConfigureScrollInput();

        var margin = _scroll.GetNodeOrNull<MarginContainer>("Margin");
        _pileSections = margin?.GetNodeOrNull<VBoxContainer>("PileSections");
        if (_pileSections == null)
        {
            GD.PushError("EventCardSelectOverlay: BattlePileOverlay scene is missing PileSections node.");
            return;
        }

        _pileSections.AddThemeConstantOverride("separation", CardPileOverlayUi.SectionSeparation);

        _selectionSection = _pileSections.GetNodeOrNull<VBoxContainer>("DrawSection");
        if (_selectionSection == null)
        {
            GD.PushError("EventCardSelectOverlay: BattlePileOverlay scene is missing DrawSection node.");
            return;
        }

        _pileSections
            .GetNodeOrNull<VBoxContainer>("DiscardSection")
            ?.SetDeferred(Control.PropertyName.Visible, false);
        _pileSections
            .GetNodeOrNull<VBoxContainer>("ExhaustedSection")
            ?.SetDeferred(Control.PropertyName.Visible, false);

        _sectionTitle = _selectionSection.GetNodeOrNull<Label>("Title");
        _emptyLabel = _selectionSection.GetNodeOrNull<Label>("Empty");
        _cardGrid = _selectionSection.GetNodeOrNull<GridContainer>("Grid");
        _characterGroups = _selectionSection.GetNodeOrNull<VBoxContainer>("Groups");
        if (_characterGroups == null)
        {
            _characterGroups = new VBoxContainer
            {
                Name = "Groups",
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            _characterGroups.AddThemeConstantOverride("separation", 22);
            _selectionSection.AddChild(_characterGroups);
        }

        if (_sectionTitle != null)
            ConfigureSelectionLabel(_sectionTitle);
        if (_emptyLabel != null)
            ConfigureSelectionLabel(_emptyLabel);
        if (_cardGrid != null)
            ConfigureSelectionGrid(_cardGrid);

        _cancelButton =
            _pileOverlayRoot.GetNodeOrNull<Button>("PileHideButton")
            ?? _pileOverlayRoot.GetNodeOrNull<Button>("PileCancelButton");
        if (_cancelButton == null)
        {
            _cancelButton = new Button { Name = "PileCancelButton" };
            _pileOverlayRoot.AddChild(_cancelButton);
        }
        CardPileOverlayUi.ConfigureCancelButton(_cancelButton);
        if (!_cancelButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnCancelPressed)))
            _cancelButton.Pressed += OnCancelPressed;

        _confirmButton = _pileOverlayRoot.GetNodeOrNull<Button>("PileConfirmButton");
        if (_confirmButton == null)
        {
            _confirmButton = new Button { Name = "PileConfirmButton", Visible = false };
            _pileOverlayRoot.AddChild(_confirmButton);
        }
        CardPileOverlayUi.ConfigureConfirmButton(_confirmButton);
        if (!_confirmButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnConfirmPressed)))
            _confirmButton.Pressed += OnConfirmPressed;

        EnsureDrawOrder();
        _uiReady = true;
        UpdateFillParent();
    }

    private bool EnsureUiReady() =>
        _uiReady
        && _pileOverlayRoot != null
        && GodotObject.IsInstanceValid(_pileOverlayRoot)
        && _sectionTitle != null
        && _cancelButton != null;

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationResized || what == NotificationParented)
            UpdateFillParent();
    }

    private void UpdateFillParent()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        if (_pileOverlayRoot == null || !GodotObject.IsInstanceValid(_pileOverlayRoot))
            return;

        _pileOverlayRoot.SetAnchorsPreset(LayoutPreset.FullRect);
        _pileOverlayRoot.SetOffsetsPreset(LayoutPreset.FullRect);
        ConfigureSelectionLayout();
    }

    private void ConfigureSelectionLayout()
    {
        if (_scroll == null || !GodotObject.IsInstanceValid(_scroll))
            return;

        Vector2 viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        float horizontalInset = Mathf.Clamp(viewportSize.X * 0.06f, 28f, 120f);
        _scroll.OffsetLeft = horizontalInset;
        _scroll.OffsetTop = SelectionTopOffset;
        _scroll.OffsetRight = -horizontalInset;
        _scroll.OffsetBottom = -SelectionBottomOffset;
        _scrollBaseOffsetTop = _scroll.OffsetTop;

        float availableWidth = Mathf.Max(1f, viewportSize.X - horizontalInset * 2f - 24f);
        int columns = GetSelectionGridColumns(availableWidth);
        float contentWidth = GetSelectionContentWidth(columns);

        if (_pileSections != null)
            _pileSections.CustomMinimumSize = new Vector2(contentWidth, 0f);
        if (_selectionSection != null)
            _selectionSection.CustomMinimumSize = new Vector2(contentWidth, 0f);
        if (_sectionTitle != null)
            ConfigureSelectionLabel(_sectionTitle, contentWidth);
        if (_emptyLabel != null)
            ConfigureSelectionLabel(_emptyLabel, contentWidth);
        if (_cardGrid != null)
            ConfigureSelectionGrid(_cardGrid, columns, contentWidth);

        ConfigureSelectionActionButton(_cancelButton, confirm: false);
        ConfigureSelectionActionButton(_confirmButton, confirm: true);
    }

    private static int GetSelectionGridColumns(float availableWidth)
    {
        float wideWidth = GetSelectionContentWidth(WideSelectionGridColumns);
        float mediumWidth = GetSelectionContentWidth(MediumSelectionGridColumns);
        return availableWidth >= wideWidth
            ? WideSelectionGridColumns
            : availableWidth >= mediumWidth ? MediumSelectionGridColumns : NarrowSelectionGridColumns;
    }

    private static float GetSelectionContentWidth(int columns)
    {
        return SelectionCardHolderSize.X * columns
            + SelectionGridHSeparation * Mathf.Max(0, columns - 1);
    }

    private static void ConfigureSelectionLabel(Label label, float contentWidth = 0f)
    {
        if (label == null)
            return;

        float width = contentWidth > 0f
            ? contentWidth
            : GetSelectionContentWidth(WideSelectionGridColumns);
        label.CustomMinimumSize = new Vector2(width, 32f);
        label.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeColorOverride("font_color", new Color(0.9f, 0.91f, 0.95f, 0.96f));
        label.AddThemeColorOverride("font_outline_color", Colors.Transparent);
        label.AddThemeConstantOverride("outline_size", 0);
        label.MouseFilter = MouseFilterEnum.Ignore;
    }

    private static void ConfigureSelectionGrid(
        GridContainer grid,
        int columns = WideSelectionGridColumns,
        float contentWidth = 0f
    )
    {
        if (grid == null)
            return;

        float width = contentWidth > 0f ? contentWidth : GetSelectionContentWidth(columns);
        grid.CustomMinimumSize = new Vector2(width, 0f);
        grid.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        grid.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        grid.MouseFilter = MouseFilterEnum.Ignore;
        grid.ClipContents = false;
        grid.Columns = columns;
        grid.AddThemeConstantOverride("h_separation", SelectionGridHSeparation);
        grid.AddThemeConstantOverride("v_separation", SelectionGridVSeparation);
    }

    private static void ConfigureSelectionActionButton(Button button, bool confirm)
    {
        if (button == null)
            return;

        float verticalOffset = confirm ? 0f : SelectionActionHeight + SelectionActionGap;
        button.CustomMinimumSize = new Vector2(SelectionActionWidth, SelectionActionHeight);
        button.SetAnchorsPreset(LayoutPreset.BottomWide);
        button.AnchorLeft = 0.5f;
        button.AnchorRight = 0.5f;
        button.OffsetLeft = -SelectionActionWidth * 0.5f;
        button.OffsetTop = -SelectionActionBottomOffset - SelectionActionHeight - verticalOffset;
        button.OffsetRight = SelectionActionWidth * 0.5f;
        button.OffsetBottom = -SelectionActionBottomOffset - verticalOffset;
        button.AddThemeFontSizeOverride("font_size", 19);
        button.AddThemeColorOverride("font_color", new Color(0.9f, 0.91f, 0.95f, 1f));
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(0.8f, 0.82f, 0.88f, 1f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.52f, 0.58f, 0.64f));
        button.AddThemeColorOverride("font_outline_color", Colors.Transparent);
        button.AddThemeConstantOverride("outline_size", 0);
        button.AddThemeStyleboxOverride(
            "normal",
            CreateSelectionButtonStyle(
                new Color(0.045f, 0.05f, 0.06f, 0.9f),
                new Color(0.64f, 0.66f, 0.72f, 0.5f)
            )
        );
        button.AddThemeStyleboxOverride(
            "hover",
            CreateSelectionButtonStyle(
                new Color(0.1f, 0.11f, 0.13f, 0.96f),
                new Color(0.92f, 0.93f, 0.97f, 0.82f)
            )
        );
        button.AddThemeStyleboxOverride(
            "pressed",
            CreateSelectionButtonStyle(
                new Color(0.13f, 0.14f, 0.16f, 1f),
                new Color(0.97f, 0.98f, 1f, 0.94f)
            )
        );
        button.AddThemeStyleboxOverride(
            "disabled",
            CreateSelectionButtonStyle(
                new Color(0.03f, 0.035f, 0.045f, 0.55f),
                new Color(0.42f, 0.44f, 0.5f, 0.28f)
            )
        );
    }

    private static StyleBoxFlat CreateSelectionButtonStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
        };
    }

    private void SuspendTacticsButton()
    {
        if (_tacticsButtonSuspended)
            return;

        Node root = GetTree()?.Root;
        _tacticsButton = root?.GetNodeOrNull<ReadyButton>("Map/UI/ReadyButton")
            ?? root?.GetNodeOrNull<ReadyButton>("/root/Map/UI/ReadyButton");
        if (_tacticsButton == null || !GodotObject.IsInstanceValid(_tacticsButton))
            return;

        _tacticsButtonWasVisible = _tacticsButton.Visible;
        _tacticsButtonWasDisabled = _tacticsButton.Disabled;
        _tacticsButton.Visible = false;
        _tacticsButton.Disabled = true;
        _tacticsButtonSuspended = true;
    }

    private void RestoreTacticsButton()
    {
        if (!_tacticsButtonSuspended)
            return;

        if (_tacticsButton != null && GodotObject.IsInstanceValid(_tacticsButton))
        {
            _tacticsButton.Visible = _tacticsButtonWasVisible;
            _tacticsButton.Disabled = _tacticsButtonWasDisabled;
        }

        _tacticsButton = null;
        _tacticsButtonSuspended = false;
    }

    private void EnsureDrawOrder()
    {
        if (_mask != null && GodotObject.IsInstanceValid(_mask))
            _pileOverlayRoot.MoveChild(_mask, 0);

        if (_cancelButton != null && GodotObject.IsInstanceValid(_cancelButton))
            _pileOverlayRoot.MoveChild(_cancelButton, _pileOverlayRoot.GetChildCount() - 1);

        if (_confirmButton != null && GodotObject.IsInstanceValid(_confirmButton))
            _pileOverlayRoot.MoveChild(_confirmButton, _pileOverlayRoot.GetChildCount() - 1);
    }

    private async System.Threading.Tasks.Task PopulateCardsAsync(int buildVersion)
    {
        if (_selectionSection == null)
            return;

        _selectionSection.Visible = true;

        if (_entries.Count == 0)
        {
            if (_emptyLabel != null)
                _emptyLabel.Visible = true;
            if (_cardGrid != null)
                _cardGrid.Visible = false;
            if (_characterGroups != null)
            {
                _characterGroups.Visible = false;
                ClearChildren(_characterGroups);
            }
            return;
        }

        if (_emptyLabel != null)
            _emptyLabel.Visible = false;

        UserSettings.EnsureLoaded();
        bool groupByCharacter = UserSettings.GroupBattlePilesByCharacter;
        if (_cardGrid != null)
            _cardGrid.Visible = !groupByCharacter;
        if (_characterGroups != null)
            _characterGroups.Visible = groupByCharacter;

        ClearCards();
        int cardIndex = 0;
        if (groupByCharacter)
            cardIndex = await PopulateCharacterGroupsAsync(buildVersion, cardIndex);
        else
            cardIndex = await PopulateFlatGridAsync(buildVersion, cardIndex);

        if (buildVersion != _buildVersion)
            return;

        _cardGrid?.QueueSort();
        _characterGroups?.QueueSort();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (buildVersion == _buildVersion)
            ResetScrollPosition();
    }

    private void ConfigureScrollInput()
    {
        if (_scroll == null || !GodotObject.IsInstanceValid(_scroll))
            return;

        if (_scrollInputTarget != _scroll)
        {
            if (_scrollInputTarget != null && GodotObject.IsInstanceValid(_scrollInputTarget))
                _scrollInputTarget.GuiInput -= OnScrollGuiInput;
            if (_vScrollBar != null && GodotObject.IsInstanceValid(_vScrollBar))
                _vScrollBar.GuiInput -= OnScrollBarGuiInput;

            _scrollInputTarget = _scroll;
            _scroll.GuiInput += OnScrollGuiInput;
            _vScrollBar = _scroll.GetVScrollBar();
            if (_vScrollBar != null)
                _vScrollBar.GuiInput += OnScrollBarGuiInput;
        }
    }

    private async System.Threading.Tasks.Task<int> PopulateFlatGridAsync(int buildVersion, int cardIndex)
    {
        if (_cardGrid == null)
            return cardIndex;

        for (int entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
        {
            if (buildVersion != _buildVersion)
                return cardIndex;

            _cardGrid.AddChild(CreateCardHolder(_entries[entryIndex], entryIndex, cardIndex));
            cardIndex++;
            if (cardIndex % 4 == 0)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        return cardIndex;
    }

    private async System.Threading.Tasks.Task<int> PopulateCharacterGroupsAsync(
        int buildVersion,
        int cardIndex
    )
    {
        if (_characterGroups == null)
            return cardIndex;

        // Keep the grouping display in party order. More importantly, retain each entry's
        // original index: the grouped layout does not have the same order as _entries.
        foreach (var group in _entries.Select((entry, entryIndex) => (entry, entryIndex))
            .GroupBy(item => item.entry.PlayerIndex)
            .OrderBy(group => group.Key))
        {
            if (buildVersion != _buildVersion)
                return cardIndex;

            string characterName = group.First().entry.CharacterName ?? string.Empty;

            var groupRoot = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            groupRoot.AddThemeConstantOverride("separation", 8);
            _characterGroups.AddChild(groupRoot);

            var label = new Label
            {
                Text = $"{characterName}  {group.Count()}",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            ConfigureSelectionLabel(label, GetCurrentSelectionContentWidth());
            groupRoot.AddChild(label);

            var grid = new GridContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            ConfigureSelectionGrid(
                grid,
                GetCurrentSelectionGridColumns(),
                GetCurrentSelectionContentWidth()
            );
            groupRoot.AddChild(grid);

            foreach (var item in group)
            {
                if (buildVersion != _buildVersion)
                    return cardIndex;

                grid.AddChild(CreateCardHolder(item.entry, item.entryIndex, cardIndex));
                cardIndex++;
                if (cardIndex % 4 == 0)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            grid.QueueSort();
        }

        return cardIndex;
    }

    private Control CreateCardHolder(
        EventCardSelectionEntry entry,
        int entryIndex,
        int cardIndex
    )
    {
        var holder = new Control
        {
            Name = "EventSelectionCardHolder",
            CustomMinimumSize = SelectionCardHolderSize,
            Size = SelectionCardHolderSize,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
            ClipContents = false,
            Visible = false,
        };
        SkillCard card = CardPileOverlayUi.SkillCardScene?.Instantiate<SkillCard>();
        if (card == null)
            return holder;

        holder.AddChild(card);
        card.Visible = true;
        card.Modulate = Colors.Transparent;
        card.PivotOffset = CardPileOverlayUi.CardBaseSize * 0.5f;
        CardPileOverlayUi.ApplyPreviewCard(
            card,
            entry.SkillId,
            entry.CharacterName,
            entry.CharacterKey,
            entry.Power,
            entry.Survivability,
            entry.PlayerIndex
        );
        card.ResetState();
        card.HoverHint.Visible = false;
        card.ConfigureDisplayScale(SelectionCardScale);
        card.PointerHoverScaleMultiplier = 1.05f;
        card.Position = SelectionCardPadding
            - 0.5f * (Vector2.One - SelectionCardScale) * CardPileOverlayUi.CardBaseSize;

        bool multiSelect = _selectionTargetCount > 1;
        card.Button.ToggleMode = multiSelect;
        card.Button.ButtonPressed = false;
        card.SetPlayableHighlight(false, instant: true);

        Action handler = () => OnCardPressed(entryIndex, card);
        _cardPressHandlers[card] = handler;
        card.Button.Pressed += handler;
        _cardEntryIndexes[card] = entryIndex;
        _entryCards[entryIndex] = card;

        if (entry.Count > 1)
            holder.AddChild(CreateSelectionCountBadge(entry.Count));

        PlaySelectionCardEntryAnimation(card, cardIndex);
        holder.Visible = true;
        return holder;
    }

    private int GetCurrentSelectionGridColumns()
    {
        Vector2 viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);
        float horizontalInset = Mathf.Clamp(viewportSize.X * 0.06f, 28f, 120f);
        float availableWidth = Mathf.Max(1f, viewportSize.X - horizontalInset * 2f - 24f);
        return GetSelectionGridColumns(availableWidth);
    }

    private float GetCurrentSelectionContentWidth() =>
        GetSelectionContentWidth(GetCurrentSelectionGridColumns());

    private static Label CreateSelectionCountBadge(int count)
    {
        var badge = new Label
        {
            Text = $"x{count}",
            OffsetLeft = SelectionCardHolderSize.X - 44f,
            OffsetTop = SelectionCardHolderSize.Y - 38f,
            OffsetRight = SelectionCardHolderSize.X - 4f,
            OffsetBottom = SelectionCardHolderSize.Y - 10f,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        badge.AddThemeFontSizeOverride("font_size", 18);
        badge.AddThemeColorOverride("font_color", new Color(0.92f, 0.93f, 0.97f, 0.96f));
        badge.AddThemeColorOverride("font_outline_color", Colors.Transparent);
        badge.AddThemeConstantOverride("outline_size", 0);
        return badge;
    }

    private static void PlaySelectionCardEntryAnimation(SkillCard card, int cardIndex)
    {
        if (card == null || !GodotObject.IsInstanceValid(card))
            return;

        Vector2 targetPosition = card.Position;
        Vector2 targetScale = SelectionCardScale;
        if (cardIndex >= CardPileOverlayUi.AnimatedCardCount)
        {
            card.Modulate = SkillButton.EnabledModulate;
            card.Scale = targetScale;
            return;
        }

        float delay = CardPileOverlayUi.CardEntryBaseDelay
            + Math.Min(cardIndex, CardPileOverlayUi.AnimatedCardCount - 1)
                * CardPileOverlayUi.CardEntryStagger;
        card.Position = targetPosition + new Vector2(0f, 20f);
        Color targetModulate = SkillButton.EnabledModulate;
        card.Modulate = new Color(targetModulate.R, targetModulate.G, targetModulate.B, 0f);
        card.Scale = targetScale * 0.9f;

        Tween tween = card.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(card, "position", targetPosition, CardPileOverlayUi.CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "modulate", targetModulate, CardPileOverlayUi.CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(card, "scale", targetScale, CardPileOverlayUi.CardEntryDuration)
            .SetDelay(delay)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnCardPressed(int entryIndex, SkillCard card)
    {
        if (!Visible || _isAnimating)
            return;

        if (_selectionTargetCount <= 1)
        {
            EventCardSelectionEntry entry = _entries[entryIndex];
            DisableSelectionInput();
            CardSelected?.Invoke(new EventCardSelection(entry.PlayerIndex, entry.SkillId, card));
            return;
        }

        HandleMultiSelectPress(entryIndex);
    }

    private void HandleMultiSelectPress(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= _entries.Count)
            return;

        EventCardSelectionEntry entry = _entries[entryIndex];
        int totalSelected = GetTotalSelectedCount();

        if (_selectedCounts[entryIndex] > 0)
        {
            _selectedCounts[entryIndex]--;
            RemoveOneSelectionOrder(entryIndex);
        }
        else if (totalSelected >= _selectionTargetCount)
        {
            RemoveLastSelection();
            if (_selectedCounts[entryIndex] < entry.Count)
            {
                _selectedCounts[entryIndex]++;
                _selectionOrder.Add(entryIndex);
            }
        }
        else if (_selectedCounts[entryIndex] < entry.Count)
        {
            _selectedCounts[entryIndex]++;
            _selectionOrder.Add(entryIndex);
        }

        RefreshSelectionVisuals();
        UpdateSectionTitle();
        SyncConfirmButton();
    }

    private void OnConfirmPressed()
    {
        if (!Visible || _isAnimating || _selectionTargetCount <= 1)
            return;

        if (GetTotalSelectedCount() < _selectionTargetCount)
        {
            SyncConfirmButton();
            return;
        }

        IReadOnlyList<EventCardSelection> selections = BuildConfirmedSelections();
        DisableSelectionInput();
        CardsConfirmed?.Invoke(selections);
    }

    private void OnCancelPressed()
    {
        if (!Visible || _isAnimating)
            return;

        HideSelection();
        SelectionCanceled?.Invoke();
    }

    private void OnMaskGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseButton
            && mouseButton.Pressed
            && mouseButton.ButtonIndex == MouseButton.Left
        )
            GetViewport().SetInputAsHandled();
    }

    private void DisableSelectionInput()
    {
        _cancelButton.Disabled = true;
        if (_confirmButton != null)
            _confirmButton.Disabled = true;
        foreach ((SkillCard card, _) in _cardPressHandlers.ToArray())
        {
            if (card != null && GodotObject.IsInstanceValid(card))
                card.Button.Disabled = true;
        }
    }

    private void ClearCards()
    {
        foreach ((SkillCard card, Action handler) in _cardPressHandlers.ToArray())
        {
            if (card != null && GodotObject.IsInstanceValid(card))
            {
                card.Button.Pressed -= handler;
                card.Button.ToggleMode = false;
                card.Button.ButtonPressed = false;
                card.SetPlayableHighlight(false, instant: true);
            }
        }
        _cardPressHandlers.Clear();
        _cardEntryIndexes.Clear();
        _entryCards.Clear();

        if (_cardGrid != null)
            ClearChildren(_cardGrid);
        if (_characterGroups != null)
            ClearChildren(_characterGroups);
    }

    private int GetTotalSelectedCount() => _selectedCounts.Sum();

    private void RemoveOneSelectionOrder(int entryIndex)
    {
        for (int i = _selectionOrder.Count - 1; i >= 0; i--)
        {
            if (_selectionOrder[i] != entryIndex)
                continue;

            _selectionOrder.RemoveAt(i);
            return;
        }
    }

    private void RemoveLastSelection()
    {
        if (_selectionOrder.Count == 0)
            return;

        int entryIndex = _selectionOrder[^1];
        _selectionOrder.RemoveAt(_selectionOrder.Count - 1);
        if (entryIndex >= 0 && entryIndex < _selectedCounts.Count && _selectedCounts[entryIndex] > 0)
            _selectedCounts[entryIndex]--;
    }

    private void RefreshSelectionVisuals()
    {
        foreach ((SkillCard card, int entryIndex) in _cardEntryIndexes.ToArray())
        {
            if (card == null || !GodotObject.IsInstanceValid(card))
                continue;

            bool selected = entryIndex >= 0
                && entryIndex < _selectedCounts.Count
                && _selectedCounts[entryIndex] > 0;
            card.Button.ButtonPressed = selected;
            card.Modulate = SkillButton.EnabledModulate;
            card.SetPlayableHighlight(selected);
        }
    }

    private void UpdateSectionTitle()
    {
        if (_sectionTitle == null)
            return;

        if (_selectionTargetCount > 1)
        {
            _sectionTitle.Text =
                $"{_hintText}  {GetTotalSelectedCount()}/{_selectionTargetCount}";
            return;
        }

        _sectionTitle.Text = $"{_hintText}  {_entries.Count}";
    }

    private void SyncConfirmButton()
    {
        if (_confirmButton == null || !GodotObject.IsInstanceValid(_confirmButton))
            return;

        bool multiSelect = _selectionTargetCount > 1;
        bool selectionReady = GetTotalSelectedCount() >= _selectionTargetCount;
        bool visible = multiSelect && Visible && selectionReady;
        _confirmButton.Visible = visible;
        _confirmButton.Disabled = !visible;

        if (!multiSelect || !Visible)
        {
            _confirmRevealReady = false;
            return;
        }

        if (!visible)
        {
            _confirmRevealReady = false;
            _confirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
            return;
        }

        if (!_confirmRevealReady)
        {
            _confirmRevealReady = true;
            PlayConfirmRevealAnimation();
            return;
        }

        if (_confirmButton.Modulate.A < 0.05f)
            _confirmButton.Modulate = Colors.White;
    }

    private void PlayConfirmRevealAnimation()
    {
        if (_confirmButton == null || !GodotObject.IsInstanceValid(_confirmButton))
            return;

        _confirmRevealTween?.Kill();
        _confirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
        _confirmRevealTween = _confirmButton.CreateTween();
        _confirmRevealTween
            .TweenProperty(_confirmButton, "modulate:a", 1f, CardPileOverlayUi.ActionButtonFadeDuration)
            .SetDelay(CardPileOverlayUi.ActionButtonFadeDelay)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _confirmRevealTween.Finished += () => _confirmRevealTween = null;
    }

    private IReadOnlyList<EventCardSelection> BuildConfirmedSelections()
    {
        var selections = new List<EventCardSelection>();
        for (int entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
        {
            int count = _selectedCounts[entryIndex];
            if (count <= 0)
                continue;

            EventCardSelectionEntry entry = _entries[entryIndex];
            if (!_entryCards.TryGetValue(entryIndex, out SkillCard card))
                continue;

            for (int i = 0; i < count; i++)
            {
                selections.Add(
                    new EventCardSelection(entry.PlayerIndex, entry.SkillId, card)
                );
            }
        }

        return selections;
    }

    private static void ClearChildren(Node parent)
    {
        if (parent == null)
            return;

        foreach (Node child in parent.GetChildren())
            child.QueueFree();
    }

    private void OnScrollGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (
                !mouseButton.Pressed
                || mouseButton.ButtonIndex is not MouseButton.WheelUp and not MouseButton.WheelDown
            )
            {
                return;
            }

            float visualDirection = mouseButton.ButtonIndex == MouseButton.WheelUp ? 1f : -1f;
            float scrollDirection = mouseButton.ButtonIndex == MouseButton.WheelUp ? -1f : 1f;
            float wheelFactor = Math.Max(0.35f, Math.Abs(mouseButton.Factor));
            if (
                QueueSmoothScrollByDelta(
                    scrollDirection * CardPileOverlayUi.SmoothWheelStep * wheelFactor,
                    visualDirection,
                    wheelFactor
                )
            )
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (@event is InputEventPanGesture panGesture)
        {
            float scrollDelta = panGesture.Delta.Y * CardPileOverlayUi.PanGestureMultiplier;
            if (Mathf.Abs(scrollDelta) <= 0.01f)
                return;

            float visualDirection = scrollDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(scrollDelta) / CardPileOverlayUi.SmoothWheelStep,
                0.5f,
                1.4f
            );
            if (QueueSmoothScrollByDelta(scrollDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
            return;
        }

        if (MobilePlatform.TryGetTouchScrollDelta(@event, out float touchDelta, 1.15f))
        {
            float visualDirection = touchDelta < 0f ? 1f : -1f;
            float bounceStrength = Mathf.Clamp(
                Mathf.Abs(touchDelta) / CardPileOverlayUi.SmoothWheelStep,
                0.35f,
                1.1f
            );
            if (QueueSmoothScrollByDelta(touchDelta, visualDirection, bounceStrength))
                GetViewport().SetInputAsHandled();
        }
    }

    private void OnScrollBarGuiInput(InputEvent @event)
    {
        if (
            _smoothScrollActive
            && @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
        )
        {
            CancelSmoothScroll();
        }
    }

    private bool QueueSmoothScrollByDelta(
        float scrollDelta,
        float visualDirection,
        float bounceStrength
    )
    {
        if (!CanReceiveSmoothScroll())
            return false;

        double minValue = _vScrollBar.MinValue;
        double maxValue = GetScrollMaxValue();
        double currentValue = _vScrollBar.Value;
        if (!_smoothScrollActive)
        {
            _smoothScrollPosition = currentValue;
            _smoothScrollTarget = currentValue;
            _smoothScrollVelocity = 0d;
        }

        double requestedTarget = _smoothScrollTarget + scrollDelta;
        double clampedTarget = Math.Clamp(requestedTarget, minValue, maxValue);
        bool hitEdge = !Mathf.IsEqualApprox((float)requestedTarget, (float)clampedTarget);

        _smoothScrollTarget = clampedTarget;
        _smoothScrollActive =
            Math.Abs(_smoothScrollTarget - _smoothScrollPosition)
                > CardPileOverlayUi.SmoothScrollSnapDistance
            || Math.Abs(_smoothScrollVelocity) > CardPileOverlayUi.SmoothScrollStopSpeed;
        if (hitEdge)
            QueueScrollBounceCheck(visualDirection, bounceStrength);

        SetProcess(_smoothScrollActive);
        return true;
    }

    private void UpdateSmoothScroll(float delta)
    {
        if (!_smoothScrollActive)
            return;

        if (!CanReceiveSmoothScroll())
        {
            CancelSmoothScroll();
            return;
        }

        float frameDelta = Mathf.Clamp(delta, 0f, 0.05f);
        if (frameDelta <= 0f)
            return;

        double minValue = _vScrollBar.MinValue;
        double maxValue = GetScrollMaxValue();
        _smoothScrollTarget = Math.Clamp(_smoothScrollTarget, minValue, maxValue);
        _smoothScrollPosition = Math.Clamp(_smoothScrollPosition, minValue, maxValue);

        double distance = _smoothScrollTarget - _smoothScrollPosition;
        if (
            Math.Abs(distance) <= CardPileOverlayUi.SmoothScrollSnapDistance
            && Math.Abs(_smoothScrollVelocity) <= CardPileOverlayUi.SmoothScrollStopSpeed
        )
        {
            SetScrollValue(_smoothScrollTarget);
            CancelSmoothScroll();
            return;
        }

        _smoothScrollVelocity += distance * CardPileOverlayUi.SmoothScrollSpring * frameDelta;
        _smoothScrollVelocity *= Math.Exp(-CardPileOverlayUi.SmoothScrollDamping * frameDelta);
        _smoothScrollVelocity = Math.Clamp(
            _smoothScrollVelocity,
            -CardPileOverlayUi.SmoothScrollMaxVelocity,
            CardPileOverlayUi.SmoothScrollMaxVelocity
        );

        double nextValue = _smoothScrollPosition + _smoothScrollVelocity * frameDelta;
        if (nextValue <= minValue || nextValue >= maxValue)
        {
            nextValue = Math.Clamp(nextValue, minValue, maxValue);
            _smoothScrollTarget = Math.Clamp(_smoothScrollTarget, minValue, maxValue);
            _smoothScrollVelocity = 0d;
        }

        _smoothScrollPosition = nextValue;
        SetScrollValue(_smoothScrollPosition);
    }

    private bool CanReceiveSmoothScroll()
    {
        return Visible
            && _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && _scroll != null
            && GodotObject.IsInstanceValid(_scroll)
            && _scroll.Modulate.A > 0.05f
            && _vScrollBar != null
            && GodotObject.IsInstanceValid(_vScrollBar)
            && GetScrollMaxValue() > _vScrollBar.MinValue + 0.5d;
    }

    private void SetScrollValue(double value)
    {
        if (_scroll == null || !GodotObject.IsInstanceValid(_scroll))
            return;

        _scroll.ScrollVertical = Mathf.RoundToInt((float)value);
    }

    private void CancelSmoothScroll()
    {
        _smoothScrollActive = false;
        _smoothScrollPosition =
            _vScrollBar != null && GodotObject.IsInstanceValid(_vScrollBar)
                ? _vScrollBar.Value
                : 0d;
        _smoothScrollTarget = _smoothScrollPosition;
        _smoothScrollVelocity = 0d;
        SetProcess(false);
    }

    private void ResetScrollPosition()
    {
        CancelSmoothScroll();
        bool resetBounceOffset =
            _scroll != null
            && GodotObject.IsInstanceValid(_scroll)
            && _scroll.Modulate.A > 0.95f;
        CancelScrollBounce(resetBounceOffset);
        if (_scroll == null || !GodotObject.IsInstanceValid(_scroll))
            return;

        _scroll.ScrollVertical = 0;
        if (_vScrollBar != null && GodotObject.IsInstanceValid(_vScrollBar))
            _vScrollBar.Value = _vScrollBar.MinValue;
    }

    private void QueueScrollBounceCheck(float visualDirection, float strength = 1f)
    {
        if (!CanReceiveScrollBounce())
            return;

        _pendingBounceDirection = visualDirection >= 0f ? 1f : -1f;
        _pendingBounceStrength = Math.Max(
            _pendingBounceStrength,
            Math.Max(0.5f, strength)
        );
        if (_scrollBounceCheckPending)
            return;

        _scrollBounceCheckPending = true;
        CallDeferred(MethodName.DeferredApplyScrollBounce);
    }

    private void DeferredApplyScrollBounce()
    {
        _scrollBounceCheckPending = false;
        float visualDirection = _pendingBounceDirection;
        float strength = _pendingBounceStrength;
        _pendingBounceDirection = 0f;
        _pendingBounceStrength = 0f;

        if (
            visualDirection == 0f
            || !CanReceiveScrollBounce()
            || !IsScrollAtEdge(visualDirection)
        )
        {
            return;
        }

        PlayScrollBounce(visualDirection, strength);
    }

    private bool CanReceiveScrollBounce()
    {
        return Visible
            && _pileOverlayRoot != null
            && GodotObject.IsInstanceValid(_pileOverlayRoot)
            && _pileOverlayRoot.Visible
            && _scroll != null
            && GodotObject.IsInstanceValid(_scroll)
            && _scroll.Modulate.A > 0.05f
            && _vScrollBar != null
            && GodotObject.IsInstanceValid(_vScrollBar)
            && GetScrollMaxValue() > _vScrollBar.MinValue + 0.5d;
    }

    private bool IsScrollAtEdge(float visualDirection)
    {
        if (_vScrollBar == null || !GodotObject.IsInstanceValid(_vScrollBar))
            return false;

        double value = _vScrollBar.Value;
        double minValue = _vScrollBar.MinValue;
        double maxValue = GetScrollMaxValue();
        return visualDirection > 0f
            ? value <= minValue + 0.5d
            : value >= maxValue - 0.5d;
    }

    private double GetScrollMaxValue()
    {
        if (_vScrollBar == null || !GodotObject.IsInstanceValid(_vScrollBar))
            return 0d;

        return Math.Max(_vScrollBar.MinValue, _vScrollBar.MaxValue - _vScrollBar.Page);
    }

    private void PlayScrollBounce(float visualDirection, float strength)
    {
        if (_scroll == null || !GodotObject.IsInstanceValid(_scroll))
            return;

        float direction = visualDirection >= 0f ? 1f : -1f;
        float currentOffset = _scroll.OffsetTop - _scrollBaseOffsetTop;
        if (Math.Sign(currentOffset) != Math.Sign(direction))
            currentOffset = 0f;

        float targetOffset = Mathf.Clamp(
            currentOffset
                + direction
                    * CardPileOverlayUi.ScrollBounceStep
                    * Mathf.Clamp(strength, 0.5f, 2f),
            -CardPileOverlayUi.ScrollBounceMaxOffset,
            CardPileOverlayUi.ScrollBounceMaxOffset
        );

        _scrollBounceTween?.Kill();
        _scrollBounceTween = _scroll.CreateTween();
        _scrollBounceTween.SetParallel(false);
        _scrollBounceTween
            .TweenProperty(
                _scroll,
                "offset_top",
                _scrollBaseOffsetTop + targetOffset,
                CardPileOverlayUi.ScrollBounceOutDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _scrollBounceTween
            .TweenProperty(
                _scroll,
                "offset_top",
                _scrollBaseOffsetTop,
                CardPileOverlayUi.ScrollBounceBackDuration
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _scrollBounceTween.TweenCallback(Callable.From(() => _scrollBounceTween = null));
    }

    private void CancelScrollBounce(bool resetOffsetTop)
    {
        _scrollBounceCheckPending = false;
        _pendingBounceDirection = 0f;
        _pendingBounceStrength = 0f;
        _scrollBounceTween?.Kill();
        _scrollBounceTween = null;

        if (
            resetOffsetTop
            && _scroll != null
            && GodotObject.IsInstanceValid(_scroll)
        )
        {
            _scroll.OffsetTop = _scrollBaseOffsetTop;
        }
    }

    private void ResetPresentation()
    {
        CancelSmoothScroll();
        CancelScrollBounce(resetOffsetTop: false);

        if (_mask != null)
        {
            Color maskColor = _mask.Color;
            maskColor.A = 0f;
            _mask.Color = maskColor;
        }

        if (_scroll != null)
        {
            _scroll.Modulate = Colors.White;
            _scroll.OffsetTop = _scrollBaseOffsetTop + CardPileOverlayUi.ContentSlideOffset;
        }

        if (_cancelButton != null)
            _cancelButton.Modulate = new Color(1f, 1f, 1f, 0f);

        if (_confirmButton != null)
            _confirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
    }

    private void SetPresentationFullyVisible()
    {
        CancelScrollBounce(resetOffsetTop: false);

        if (_mask != null)
        {
            Color maskColor = _mask.Color;
            maskColor.A = CardPileOverlayUi.MaskMaxAlpha;
            _mask.Color = maskColor;
        }

        if (_scroll != null)
        {
            _scroll.Modulate = Colors.White;
            _scroll.OffsetTop = _scrollBaseOffsetTop;
        }

        if (_cancelButton != null)
            _cancelButton.Modulate = Colors.White;

        SyncConfirmButton();
    }

    private void PlayIntroAnimation()
    {
        CancelScrollBounce(resetOffsetTop: false);
        _fadeTween?.Kill();
        _isAnimating = true;
        _fadeTween = _pileOverlayRoot.CreateTween();
        _fadeTween.SetParallel(true);

        if (_mask != null)
        {
            Color maskColor = _mask.Color;
            maskColor.A = CardPileOverlayUi.MaskMaxAlpha;
            _mask.Color = maskColor;
        }

        if (_scroll != null)
        {
            _scroll.Modulate = Colors.White;
            _scroll.OffsetTop = _scrollBaseOffsetTop + CardPileOverlayUi.ContentSlideOffset;
            _fadeTween
                .TweenProperty(
                    _scroll,
                    "offset_top",
                    _scrollBaseOffsetTop,
                    CardPileOverlayUi.ContentMoveDuration
                )
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        if (_cancelButton != null)
        {
            _fadeTween
                .TweenProperty(_cancelButton, "modulate:a", 1f, CardPileOverlayUi.ActionButtonFadeDuration)
                .SetDelay(CardPileOverlayUi.ActionButtonFadeDelay)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }

        _fadeTween.Finished += OnIntroFinished;
    }

    private void PlayOutroAnimation()
    {
        int hideVersion = ++_buildVersion;
        CancelSmoothScroll();
        CancelScrollBounce(resetOffsetTop: false);
        _fadeTween?.Kill();
        _isAnimating = true;
        _fadeTween = _pileOverlayRoot.CreateTween();
        _fadeTween.SetParallel(true);

        if (_mask != null)
        {
            _fadeTween
                .TweenProperty(_mask, "color:a", 0f, CardPileOverlayUi.ContentMoveDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_scroll != null)
        {
            _fadeTween
                .TweenProperty(_scroll, "modulate:a", 0f, CardPileOverlayUi.ContentMoveDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
        }

        if (_cancelButton != null)
        {
            _fadeTween
                .TweenProperty(_cancelButton, "modulate:a", 0f, 0.12f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_confirmButton != null)
        {
            _fadeTween
                .TweenProperty(_confirmButton, "modulate:a", 0f, 0.12f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        _fadeTween.SetParallel(false);
        _fadeTween.TweenCallback(
            Callable.From(() =>
            {
                if (hideVersion != _buildVersion)
                    return;

                OnOutroFinished();
            })
        );
    }

    private void OnIntroFinished()
    {
        _fadeTween = null;
        _isAnimating = false;
        SetPresentationFullyVisible();
    }

    private void OnOutroFinished()
    {
        _fadeTween = null;
        _isAnimating = false;
        RestoreTacticsButton();
        Visible = false;
        _pileOverlayRoot.Visible = false;
        _selectionTargetCount = 1;
        _confirmRevealReady = false;
        if (_confirmButton != null)
            _confirmButton.Visible = false;
        ClearCards();
        ResetPresentation();
    }
}
