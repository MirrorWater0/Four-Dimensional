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
    private bool _isAnimating;
    private bool _uiReady;
    private bool _confirmRevealReady;
    private int _buildVersion;
    private int _selectionTargetCount = 1;
    private string _hintText = string.Empty;
    private readonly List<EventCardSelectionEntry> _entries = new();
    private readonly List<int> _selectedCounts = new();
    private readonly List<int> _selectionOrder = new();
    private readonly Dictionary<SkillCard, Action> _cardPressHandlers = new();
    private readonly Dictionary<SkillCard, int> _cardEntryIndexes = new();
    private readonly Dictionary<int, SkillCard> _entryCards = new();

    public override void _Ready()
    {
        BuildUi();
        Visible = false;
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    public override void _ExitTree()
    {
        if (GetViewport() != null)
            GetViewport().SizeChanged -= OnViewportSizeChanged;
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

        int buildVersion = ++_buildVersion;
        ClearCards();
        _ = PopulateCardsAsync(buildVersion);

        bool wasVisible = Visible;
        ResetPresentation();
        Visible = true;
        _pileOverlayRoot.Visible = true;
        _pileOverlayRoot.Modulate = Colors.White;
        if (_scroll != null)
            _scroll.ScrollVertical = 0;

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
        CardPileOverlayUi.ConfigureOverlayRoot(_pileOverlayRoot);

        _mask = _pileOverlayRoot.GetNodeOrNull<ColorRect>("Mask");
        if (_mask == null)
        {
            _mask = new ColorRect { Name = "Mask" };
            _pileOverlayRoot.AddChild(_mask);
        }
        CardPileOverlayUi.ConfigureMask(_mask);
        _mask.GuiInput += OnMaskGuiInput;

        _scroll = _pileOverlayRoot.GetNodeOrNull<ScrollContainer>("Scroll");
        if (_scroll == null)
        {
            GD.PushError("EventCardSelectOverlay: BattlePileOverlay scene is missing Scroll node.");
            return;
        }

        _scroll.ZIndex = CardPileOverlayUi.ContentZIndex;
        CardPileOverlayUi.ConfigureCardScrollContainer(_scroll);
        _scrollBaseOffsetTop = _scroll.OffsetTop;

        var margin = _scroll.GetNodeOrNull<MarginContainer>("Margin");
        CardPileOverlayUi.ConfigureScrollContentMargin(margin);
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
            CardPileOverlayUi.ConfigureSectionLabel(_sectionTitle);
        if (_emptyLabel != null)
            CardPileOverlayUi.ConfigureSectionLabel(_emptyLabel);
        if (_cardGrid != null)
            CardPileOverlayUi.ConfigureCardGrid(_cardGrid);

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
    }

    private async System.Threading.Tasks.Task<int> PopulateFlatGridAsync(int buildVersion, int cardIndex)
    {
        if (_cardGrid == null)
            return cardIndex;

        foreach (var entry in _entries)
        {
            if (buildVersion != _buildVersion)
                return cardIndex;

            _cardGrid.AddChild(CreateCardHolder(entry, cardIndex));
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

        foreach (
            var group in _entries.GroupBy(entry => entry.CharacterName ?? string.Empty)
                .OrderBy(group => group.Key)
        )
        {
            if (buildVersion != _buildVersion)
                return cardIndex;

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
                Text = $"{group.Key}  {group.Count()}",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            CardPileOverlayUi.ConfigureSectionLabel(label);
            groupRoot.AddChild(label);

            var grid = new GridContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
            };
            CardPileOverlayUi.ConfigureCardGrid(grid);
            groupRoot.AddChild(grid);

            foreach (var entry in group)
            {
                if (buildVersion != _buildVersion)
                    return cardIndex;

                grid.AddChild(CreateCardHolder(entry, cardIndex));
                cardIndex++;
                if (cardIndex % 4 == 0)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            grid.QueueSort();
        }

        return cardIndex;
    }

    private Control CreateCardHolder(EventCardSelectionEntry entry, int cardIndex)
    {
        Control holder = CardPileOverlayUi.CreateCardHolder(out SkillCard card);
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
        card.CallDeferred(nameof(SkillCard.RestoreDisplayState));

        bool multiSelect = _selectionTargetCount > 1;
        card.Button.ToggleMode = multiSelect;
        card.Button.ButtonPressed = false;
        card.SetPlayableHighlight(false, instant: true);

        Action handler = () => OnCardPressed(cardIndex, card);
        _cardPressHandlers[card] = handler;
        card.Button.Pressed += handler;
        _cardEntryIndexes[card] = cardIndex;
        _entryCards[cardIndex] = card;

        if (entry.Count > 1)
            holder.AddChild(CardPileOverlayUi.CreateCountBadge(entry.Count));

        CardPileOverlayUi.PlayCardEntryAnimation(card, cardIndex);
        holder.Visible = true;
        return holder;
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

    private void ResetPresentation()
    {
        if (_mask != null)
        {
            Color maskColor = _mask.Color;
            maskColor.A = 0f;
            _mask.Color = maskColor;
        }

        if (_scroll != null)
        {
            _scroll.Modulate = new Color(1f, 1f, 1f, 0f);
            _scroll.OffsetTop = _scrollBaseOffsetTop + CardPileOverlayUi.ContentSlideOffset;
        }

        if (_cancelButton != null)
            _cancelButton.Modulate = new Color(1f, 1f, 1f, 0f);

        if (_confirmButton != null)
            _confirmButton.Modulate = new Color(1f, 1f, 1f, 0f);
    }

    private void SetPresentationFullyVisible()
    {
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
        _fadeTween?.Kill();
        _isAnimating = true;
        _fadeTween = _pileOverlayRoot.CreateTween();
        _fadeTween.SetParallel(true);

        if (_mask != null)
        {
            _fadeTween
                .TweenProperty(_mask, "color:a", CardPileOverlayUi.MaskMaxAlpha, CardPileOverlayUi.MaskFadeInDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.Out);
        }

        if (_scroll != null)
        {
            _fadeTween
                .TweenProperty(_scroll, "modulate:a", 1f, CardPileOverlayUi.ContentFadeInDuration)
                .SetDelay(CardPileOverlayUi.ContentIntroDelay)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _fadeTween
                .TweenProperty(_scroll, "offset_top", _scrollBaseOffsetTop, CardPileOverlayUi.ContentFadeInDuration)
                .SetDelay(CardPileOverlayUi.ContentIntroDelay)
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
        _fadeTween?.Kill();
        _isAnimating = true;
        _fadeTween = _pileOverlayRoot.CreateTween();
        _fadeTween.SetParallel(true);

        if (_mask != null)
        {
            _fadeTween
                .TweenProperty(_mask, "color:a", 0f, CardPileOverlayUi.MaskFadeOutDuration)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        if (_scroll != null)
        {
            _fadeTween
                .TweenProperty(_scroll, "modulate:a", 0f, CardPileOverlayUi.ContentFadeOutDuration)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.In);
            _fadeTween
                .TweenProperty(
                    _scroll,
                    "offset_top",
                    _scrollBaseOffsetTop + CardPileOverlayUi.ContentSlideOffset * 0.5f,
                    CardPileOverlayUi.ContentFadeOutDuration
                )
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
