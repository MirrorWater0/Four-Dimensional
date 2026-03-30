using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EquipmentInterface : Control
{
    private const int SlotCount = 2;
    private const int MinInventoryCardPoolSize = 6;
    private const float SlotEquipExitMoveDistance = 72.0f;
    private const float SlotEquipEnterMoveDistance = 24.0f;
    private const float SlotExitDuration = 0.16f;
    private const float SlotEnterDuration = 0.2f;
    private const float InventoryMoveDistance = 72.0f;
    private const float InventoryExitDuration = 0.25f;
    private const float InventoryEnterDuration = 0.3f;
    private const float InventoryReflowDuration = 0.18f;
    private const float SlotBorderTweenDuration = 0.12f;
    private const float CharacterPortraitReplaceMoveDistance = 16.0f;
    private const float CharacterPortraitExitDuration = 0.14f;
    private const float CharacterPortraitEnterDuration = 0.18f;

    private static readonly string[] SlotLetters = ["A", "B"];
    private static readonly string[] CharacterButtonPaths =
    [
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/EchoButton",
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/KasiyaButton",
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/MariyaButton",
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot/CharacterSelectPanel/CharacterSelectList/NightingaleButton",
    ];
    private static readonly string[] SlotCardPaths =
    [
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA/Card",
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB/Card",
    ];
    private static readonly string[] SlotPanelPaths =
    [
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA",
        "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB",
    ];
    private static readonly Color SelectedTextColor = new(1f, 0.55f, 0.55f, 1f);
    private static readonly Color NormalTextColor = new(0.9f, 0.95f, 1f, 1f);
    private static readonly Color SlotDefaultBorderColor = new("#a7d6ff52");
    private static readonly Color SlotHoverBorderColor = new("#5cff8a");
    private static readonly Color SlotSelectedBorderColor = new("#ffd24a");

    private Button[] CharacterButtons => field ??= BuildNodes<Button>(CharacterButtonPaths);
    private Control CharacterSelectRoot =>
        field ??= GetNode<Control>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot"
        );
    private Control CharacterSelectorThumb =>
        field ??= GetNode<Control>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectRoot/CharacterSelectThumb"
        );

    private TextureRect CharacterPortrait =>
        field ??= GetNode<TextureRect>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterCard/CharacterRow/PortraitFrame/CharacterPortrait"
        );
    private Label CharacterNameLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterCard/CharacterRow/CharacterInfo/CharacterNameLabel"
        );
    private Label CharacterRoleLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterCard/CharacterRow/CharacterInfo/CharacterRoleLabel"
        );
    private Label CharacterHintLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterCard/CharacterRow/CharacterInfo/CharacterHintLabel"
        );

    private CardSlot[] SlotCards => field ??= BuildNodes<CardSlot>(SlotCardPaths);
    private PanelContainer[] SlotPanels => field ??= BuildNodes<PanelContainer>(SlotPanelPaths);
    private Label[] SlotCardLabels => field ??= [SlotCards[0].label, SlotCards[1].label];

    private InventoryGrid InventoryGridNode =>
        field ??= GetNode<InventoryGrid>(
            "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid"
        );
    private PackedScene InventoryCardScene =>
        field ??= ResourceLoader.Load<PackedScene>("res://Equipment/CardSlot.tscn");
    private CardSlot[] InventoryCards => _inventoryCards;

    private Label DetailNameLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/DetailCard/DetailVBox/DetailNameLabel"
        );
    private Label DetailTypeLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/DetailCard/DetailVBox/DetailTypeLabel"
        );
    private Label DetailDescLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/DetailCard/DetailVBox/DetailDescLabel"
        );

    private Label PowerValueLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/PreviewPanel/PreviewRows/PowerValue"
        );
    private Label SurviveValueLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/PreviewPanel/PreviewRows/SurviveValue"
        );
    private Label SpeedValueLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/PreviewPanel/PreviewRows/SpeedValue"
        );
    private Label LifeValueLabel =>
        field ??= GetNode<Label>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/PreviewPanel/PreviewRows/LifeValue"
        );

    private Button EquipButton =>
        field ??= GetNode<Button>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/ActionRow/EquipButton"
        );
    private Button UnequipButton =>
        field ??= GetNode<Button>(
            "RootMargin/MainVBox/ContentRow/RightPanel/RightVBox/ActionRow/UnequipButton"
        );

    private Equipment[] _catalog = Array.Empty<Equipment>();
    private int _selectedCharacterIndex;
    private int _selectedSlotIndex;
    private int _selectedInventoryCardIndex = -1;
    private Equipment _selectedEquipment;
    private bool _isEquipAnimating;
    private bool _isCharacterSwitchSlotInsertPending;
    private int _pendingSlotInsertIndex = -1;
    private Equipment.EquipmentName? _pendingInventoryInsertName;
    private readonly StyleBoxFlat[] _slotRuntimeStyleBoxes = new StyleBoxFlat[SlotCount];
    private readonly Tween[] _slotBorderTweens = new Tween[SlotCount];
    private readonly Color[] _slotCurrentBorderColors = new Color[SlotCount];
    private readonly bool[] _slotHovered = new bool[SlotCount];
    private readonly HashSet<CardSlot> _wiredInventoryCards = [];
    private CardSlot[] _inventoryCards = Array.Empty<CardSlot>();
    private Tween _characterSelectorTween;
    private bool _characterSelectorPositioned;
    public bool HasEquipmentChanges { get; private set; }

    public override void _Ready()
    {
        EnsureGameData();

        EnsureInventoryCardPool(MinInventoryCardPoolSize);
        InitializeSlotPanelStyles();
        WireUiEvents();
        RefreshAll();
        CharacterSelectRoot.Resized += RefreshCharacterSelectorLayout;
        foreach (var button in CharacterButtons)
            button.Resized += RefreshCharacterSelectorLayout;
        CallDeferred(nameof(RefreshCharacterSelectorLayout));
    }

    private void WireUiEvents()
    {
        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            int captured = i;
            CharacterButtons[i].Pressed += () => OnCharacterButtonPressed(captured);
            CharacterButtons[i].ToggleMode = true;
        }

        for (int i = 0; i < SlotCards.Length; i++)
        {
            int captured = i;
            SlotCards[i].Clicked += () => OnSlotCardPressed(captured);
            SlotPanels[i].GuiInput += @event => OnSlotPanelGuiInput(captured, @event);
            SlotPanels[i].MouseEntered += () => SetSlotHoverState(captured, true);
            SlotPanels[i].MouseExited += () => SetSlotHoverState(captured, false);
        }

        EquipButton.Pressed += OnEquipPressed;
        UnequipButton.Pressed += OnUnequipPressed;
    }

    private T[] BuildNodes<T>(IReadOnlyList<string> paths)
        where T : Node
    {
        var nodes = new T[paths.Count];
        for (int i = 0; i < paths.Count; i++)
            nodes[i] = GetNode<T>(paths[i]);
        return nodes;
    }

    private async void OnCharacterButtonPressed(int characterIndex)
    {
        if (
            GameInfo.PlayerCharacters == null
            || (uint)characterIndex >= (uint)GameInfo.PlayerCharacters.Length
        )
            return;

        if (_isEquipAnimating)
            return;
        if (characterIndex == _selectedCharacterIndex)
            return;

        await SwitchCharacterWithAnimationAsync(characterIndex);
    }

    private async Task SwitchCharacterWithAnimationAsync(int characterIndex)
    {
        SetEquipAnimating(true);
        try
        {
            int[] exitingSlotIndices = CollectVisibleSlotIndices();
            bool hasExitingEquipCards = exitingSlotIndices.Length > 0;

            // Portrait can exit first, equipment cards must fully exit before any insert starts.
            await AnimateCharacterPortraitExitAsync();
            if (hasExitingEquipCards)
            {
                await AnimateSlotsExitAsync(exitingSlotIndices);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            _selectedCharacterIndex = characterIndex;
            _selectedInventoryCardIndex = -1;
            _pendingSlotInsertIndex = -1;
            _pendingInventoryInsertName = null;
            _isCharacterSwitchSlotInsertPending = true;
            RefreshAll();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await AnimateCharacterPortraitEnterAsync();
            await AnimateSlotsEnterAsync();
            _isCharacterSwitchSlotInsertPending = false;
        }
        finally
        {
            if (_isCharacterSwitchSlotInsertPending)
            {
                _isCharacterSwitchSlotInsertPending = false;
                RefreshSlots();
            }
            SetEquipAnimating(false);
        }
    }

    private void RefreshCharacterSelectorLayout()
    {
        UpdateCharacterSelectorPosition(false);
    }

    private void UpdateCharacterSelectorPosition(bool animate)
    {
        if (
            CharacterButtons == null
            || CharacterButtons.Length == 0
            || _selectedCharacterIndex < 0
            || _selectedCharacterIndex >= CharacterButtons.Length
        )
            return;

        var button = CharacterButtons[_selectedCharacterIndex];
        if (button == null || !button.Visible || !GodotObject.IsInstanceValid(button))
            return;

        Rect2 selectorRect = CharacterSelectRoot.GetGlobalRect();
        Rect2 buttonRect = button.GetGlobalRect();
        if (selectorRect.Size.X <= 0f || buttonRect.Size.X <= 0f)
            return;

        Vector2 targetPosition = buttonRect.Position - selectorRect.Position;
        Vector2 targetSize = buttonRect.Size;

        _characterSelectorTween?.Kill();
        if (animate && _characterSelectorPositioned)
        {
            _characterSelectorTween = CreateTween();
            _characterSelectorTween.SetParallel(true);
            _characterSelectorTween.SetEase(Tween.EaseType.Out);
            _characterSelectorTween.SetTrans(Tween.TransitionType.Cubic);
            _characterSelectorTween.TweenProperty(
                CharacterSelectorThumb,
                "position",
                targetPosition,
                0.22f
            );
            _characterSelectorTween.TweenProperty(CharacterSelectorThumb, "size", targetSize, 0.22f);
        }
        else
        {
            CharacterSelectorThumb.Position = targetPosition;
            CharacterSelectorThumb.Size = targetSize;
        }

        _characterSelectorPositioned = true;
    }

    private int[] CollectVisibleSlotIndices()
    {
        var indices = new List<int>(SlotCount);
        for (int i = 0; i < SlotCards.Length; i++)
        {
            if (SlotCards[i].Visible)
                indices.Add(i);
        }
        return indices.ToArray();
    }

    private async Task AnimateSlotsExitAsync(IReadOnlyList<int> slotIndices)
    {
        if (slotIndices == null || slotIndices.Count == 0)
            return;

        var tasks = new List<Task>(slotIndices.Count);
        for (int i = 0; i < slotIndices.Count; i++)
            tasks.Add(AnimateSlotContentExitAsync(slotIndices[i]));
        await Task.WhenAll(tasks);
    }

    private async Task AnimateSlotsEnterAsync()
    {
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        var tasks = new List<Task>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            if (HasValidEquipment(info.Equipments[i]))
                tasks.Add(AnimateSlotContentEnterAsync(i));
        }

        if (tasks.Count == 0)
            return;

        await Task.WhenAll(tasks);
    }

    private async Task AnimateCharacterPortraitExitAsync()
    {
        var portrait = CharacterPortrait;
        if (portrait == null || !portrait.Visible)
            return;

        Vector2 basePos = portrait.Position;
        portrait.Position = basePos;
        SetCanvasItemAlpha(portrait, 1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(
            portrait,
            "position",
            basePos + new Vector2(CharacterPortraitReplaceMoveDistance, 0.0f),
            CharacterPortraitExitDuration
        );
        tween.TweenProperty(portrait, "modulate:a", 0.0f, CharacterPortraitExitDuration * 0.92f);
        await ToSignal(tween, Tween.SignalName.Finished);

        portrait.Position = basePos;
        SetCanvasItemAlpha(portrait, 0.0f);
    }

    private async Task AnimateCharacterPortraitEnterAsync()
    {
        var portrait = CharacterPortrait;
        if (portrait == null || !portrait.Visible)
            return;

        Vector2 basePos = portrait.Position;
        portrait.Position = basePos - new Vector2(CharacterPortraitReplaceMoveDistance, 0.0f);
        SetCanvasItemAlpha(portrait, 0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(portrait, "position", basePos, CharacterPortraitEnterDuration);
        tween.TweenProperty(portrait, "modulate:a", 1.0f, CharacterPortraitEnterDuration * 0.92f);
        await ToSignal(tween, Tween.SignalName.Finished);

        portrait.Position = basePos;
        SetCanvasItemAlpha(portrait, 1.0f);
    }

    private static void SetCanvasItemAlpha(CanvasItem item, float alpha)
    {
        if (item == null)
            return;
        item.Modulate = item.Modulate with { A = alpha };
    }

    private void OnSlotCardPressed(int slotIndex)
    {
        _selectedSlotIndex = Math.Clamp(slotIndex, 0, SlotCount - 1);
        _selectedInventoryCardIndex = -1;
        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            var equipped = info.Equipments[_selectedSlotIndex];
            _selectedEquipment = HasValidEquipment(equipped) ? Equipment.Clone(equipped) : null;
        }
        else
        {
            _selectedEquipment = null;
        }
        RefreshAll();
    }

    private void OnSlotPanelGuiInput(int slotIndex, InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;

        OnSlotCardPressed(slotIndex);
    }

    private void SetSlotHoverState(int slotIndex, bool hovered)
    {
        if ((uint)slotIndex >= (uint)SlotCount)
            return;
        _slotHovered[slotIndex] = hovered;
        ApplySlotPanelBorderState(slotIndex, animate: true);
    }

    private void OnInventoryCardPressed(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)_catalog.Length)
            return;

        _selectedInventoryCardIndex = cardIndex;
        _selectedEquipment = Equipment.Clone(_catalog[cardIndex]);
        RefreshAll();
    }

    private async void OnEquipPressed()
    {
        if (_isEquipAnimating)
            return;
        if ((uint)_selectedInventoryCardIndex >= (uint)_catalog.Length)
            return;
        int removedCardIndex = _selectedInventoryCardIndex;

        await EquipInventoryCardToSlotAsync(removedCardIndex, _selectedSlotIndex);
    }

    private async Task EquipInventoryCardToSlotAsync(int inventoryCardIndex, int slotIndex)
    {
        if (_isEquipAnimating)
            return;
        if ((uint)slotIndex >= (uint)SlotCount)
            return;
        if ((uint)inventoryCardIndex >= (uint)_catalog.Length)
            return;
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        SetEquipAnimating(true);
        try
        {
            _selectedSlotIndex = slotIndex;
            var selectedFromInventory = Equipment.Clone(_catalog[inventoryCardIndex]);
            bool hadEquip = HasValidEquipment(info.Equipments[slotIndex]);
            var inventoryPositionsBeforeRefresh = CaptureVisibleInventoryCardPositions();
            Equipment.EquipmentName? replacedEquipName = hadEquip
                ? info.Equipments[slotIndex].Name
                : null;

            Task slotExitTask = hadEquip
                ? AnimateSlotContentExitAsync(slotIndex)
                : Task.CompletedTask;
            Task inventoryExitTask = AnimateInventoryCardExitAsync(inventoryCardIndex);
            await Task.WhenAll(slotExitTask, inventoryExitTask);

            RemoveOwnedEquipmentByAvailableIndex(inventoryCardIndex);
            if (hadEquip && info.Equipments[slotIndex] != null)
                GameInfo.OwnedEquipments.Add(Equipment.Clone(info.Equipments[slotIndex]));

            info.Equipments[slotIndex] = Equipment.Clone(selectedFromInventory);
            SaveSelectedPlayerInfo(in info);
            HasEquipmentChanges = true;
            _selectedEquipment = Equipment.Clone(selectedFromInventory);
            _selectedInventoryCardIndex = -1;

            _pendingSlotInsertIndex = slotIndex;
            if (replacedEquipName.HasValue)
                _pendingInventoryInsertName = replacedEquipName.Value;
            RefreshAll();

            int replacedCardIndex = replacedEquipName.HasValue
                ? FindLastCatalogIndexByName(replacedEquipName.Value)
                : -1;

            Task inventoryReflowTask = AnimateInventoryReflowAsync(
                inventoryPositionsBeforeRefresh,
                replacedCardIndex
            );
            Task replacedEquipTask = Task.CompletedTask;
            if (replacedCardIndex >= 0)
                replacedEquipTask = AnimateInventoryCardEnterAsync(replacedCardIndex);

            // Keep slot motion order deterministic: exit fully completes before slot insert starts.
            await AnimateSlotContentEnterAsync(slotIndex);
            await Task.WhenAll(inventoryReflowTask, replacedEquipTask);
            if (
                replacedEquipName.HasValue
                && _pendingInventoryInsertName == replacedEquipName.Value
            )
                _pendingInventoryInsertName = null;
        }
        finally
        {
            if (_pendingSlotInsertIndex == slotIndex)
                _pendingSlotInsertIndex = -1;
            SetEquipAnimating(false);
        }
    }

    private async void OnUnequipPressed()
    {
        if (_isEquipAnimating)
            return;

        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        if (!HasValidEquipment(info.Equipments[_selectedSlotIndex]))
            return;

        SetEquipAnimating(true);
        try
        {
            Equipment unequipped = Equipment.Clone(info.Equipments[_selectedSlotIndex]);
            Equipment.EquipmentName unequippedName = unequipped.Name;
            await AnimateSlotContentExitAsync(_selectedSlotIndex);

            info.Equipments[_selectedSlotIndex] = null;
            SaveSelectedPlayerInfo(in info);
            GameInfo.OwnedEquipments.Add(unequipped);
            HasEquipmentChanges = true;

            _pendingInventoryInsertName = unequippedName;
            RefreshAll();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await AnimateInventoryCardEnterByNameAsync(unequippedName);
            if (_pendingInventoryInsertName == unequippedName)
                _pendingInventoryInsertName = null;
        }
        finally
        {
            SetEquipAnimating(false);
        }
    }

    private void SetEquipAnimating(bool isAnimating)
    {
        _isEquipAnimating = isAnimating;
        InventoryGridNode.SetInputBlocked(isAnimating);
    }

    private void RefreshAll()
    {
        RefreshAvailableCatalog();
        RefreshCharacterButtons();
        RefreshCharacterCard();
        RefreshSlots();
        RefreshInventoryCards();
        RefreshDetailCard();
        RefreshPreview();
        RefreshActionButtons();
    }

    private void RefreshCharacterButtons()
    {
        var players = GameInfo.PlayerCharacters;
        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            bool exists = players != null && i < players.Length;
            CharacterButtons[i].Visible = exists;
            if (!exists)
                continue;

            var info = players[i];
            string characterName = string.IsNullOrWhiteSpace(info.CharacterName)
                ? $"角色 {i + 1}"
                : info.CharacterName;
            int equippedCount = CountEquippedItems(info.Equipments);
            CharacterButtons[i].Text = $"{characterName}\n装备 {equippedCount}/{SlotCount}";

            bool selected = i == _selectedCharacterIndex;
            CharacterButtons[i].SetPressedNoSignal(selected);
            CharacterButtons[i].Modulate = selected
                ? Colors.White
                : new Color(0.84f, 0.88f, 0.94f, 0.82f);
        }

        UpdateCharacterSelectorPosition(true);
    }

    private void RefreshCharacterCard()
    {
        if (!TryGetSelectedPlayerInfo(out var info))
            return;

        CharacterNameLabel.Text = string.IsNullOrWhiteSpace(info.CharacterName)
            ? "未命名角色"
            : info.CharacterName;
        CharacterRoleLabel.Text = string.IsNullOrWhiteSpace(info.PassiveName)
            ? "无被动"
            : info.PassiveName;
        CharacterHintLabel.Text = BuildShortPassiveText(info.PassiveDescription);

        Texture2D portrait = null;
        if (!string.IsNullOrWhiteSpace(info.PortaitPath))
            portrait = ResourceLoader.Load<Texture2D>(info.PortaitPath);
        CharacterPortrait.Texture = portrait;
    }

    private void RefreshSlots()
    {
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        for (int i = 0; i < SlotCount; i++)
            RefreshSlotCard(i, info.Equipments[i]);
    }

    private void RefreshSlotCard(int slotIndex, Equipment equip)
    {
        var card = SlotCards[slotIndex];
        var label = SlotCardLabels[slotIndex];
        var validEquip = HasValidEquipment(equip) ? equip : null;
        bool hasEquip = validEquip != null;

        card.Visible = hasEquip;
        if (hasEquip)
        {
            bool prepareInsert = _isCharacterSwitchSlotInsertPending
                || slotIndex == _pendingSlotInsertIndex;
            if (prepareInsert)
                card.PrepareForInsertVisual();
            else
                card.ResetPanelVisualState();
        }

        if (label != null)
            label.Text = hasEquip
                ? $"{validEquip.DisplayName}\n{BuildEquipmentBonusInline(validEquip)}"
                : string.Empty;

        bool selected = slotIndex == _selectedSlotIndex;
        if (selected && hasEquip)
            card.Select();
        else
            card.Unselect();
        if (label != null)
            label.Modulate = selected ? SelectedTextColor : NormalTextColor;

        ApplySlotPanelBorderState(slotIndex, animate: false);
    }

    private void EnsureInventoryCardPool(int requiredCount)
    {
        int targetCount = Math.Max(MinInventoryCardPoolSize, requiredCount);
        var cards = InventoryGridNode.GetChildren().OfType<CardSlot>().ToList();
        if (cards.Count < targetCount)
        {
            if (InventoryCardScene == null)
            {
                GD.PushError("Inventory card scene is missing: res://Equipment/CardSlot.tscn");
            }
            else
            {
                for (int i = cards.Count; i < targetCount; i++)
                {
                    var card = InventoryCardScene.Instantiate<CardSlot>();
                    if (card == null)
                        break;

                    card.Name = $"InventoryCard{i + 1}";
                    InventoryGridNode.AddChild(card);
                    cards.Add(card);
                }
            }
        }

        _inventoryCards = cards.ToArray();
        WireInventoryCardEvents();
    }

    private void WireInventoryCardEvents()
    {
        for (int i = 0; i < _inventoryCards.Length; i++)
        {
            var card = _inventoryCards[i];
            if (card == null || _wiredInventoryCards.Contains(card))
                continue;

            int captured = i;
            card.Clicked += () => OnInventoryCardPressed(captured);
            _wiredInventoryCards.Add(card);
        }
    }

    private void RefreshInventoryCards()
    {
        EnsureInventoryCardPool(_catalog.Length);

        int pendingIndex = _pendingInventoryInsertName.HasValue
            ? FindLastCatalogIndexByName(_pendingInventoryInsertName.Value)
            : -1;

        for (int i = 0; i < InventoryCards.Length; i++)
            RefreshInventoryCard(i, pendingIndex);

        InventoryGridNode.LayoutCards();
    }

    private void RefreshInventoryCard(int cardIndex, int pendingIndex)
    {
        var card = InventoryCards[cardIndex];
        bool visible = cardIndex < _catalog.Length;
        card.Visible = visible;
        if (!visible)
        {
            card.Unselect();
            return;
        }

        if (cardIndex == pendingIndex)
            card.PrepareForInsertVisual();
        else
            card.ResetPanelVisualState();

        var equip = _catalog[cardIndex];
        var label = card.label;
        if (label != null)
        {
            label.Text = $"{equip.DisplayName}\n{BuildEquipmentBonusInline(equip)}";
            label.Modulate = NormalTextColor;
        }

        bool selected = cardIndex == _selectedInventoryCardIndex;
        if (selected)
        {
            card.Select();
            if (label != null)
                label.Modulate = SelectedTextColor;
            return;
        }

        card.Unselect();
    }

    private void RefreshDetailCard()
    {
        if (_selectedEquipment == null)
        {
            DetailNameLabel.Text = "未选择装备";
            DetailTypeLabel.Text = "类型：无";
            DetailDescLabel.Text = "请从中间列表选择一件装备。";
            return;
        }

        DetailNameLabel.Text = _selectedEquipment.DisplayName;
        DetailTypeLabel.Text = _selectedEquipment.TypeLabel;
        DetailDescLabel.Text = _selectedEquipment.Description;
    }

    private void RefreshPreview()
    {
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        Equipment[] current = info.Equipments;
        Equipment[] simulated = CloneEquipArray(current);
        if (_selectedEquipment != null)
            simulated[_selectedSlotIndex] = Equipment.Clone(_selectedEquipment);

        int currentPower = GetStatPreviewValue(info.Power, current, x => x.Power);
        int currentSurvive = GetStatPreviewValue(info.Survivability, current, x => x.Survivability);
        int currentSpeed = GetStatPreviewValue(info.Speed, current, x => x.Speed);
        int currentLife = GetStatPreviewValue(info.LifeMax, current, x => x.MaxLife);

        int nextPower = GetStatPreviewValue(info.Power, simulated, x => x.Power);
        int nextSurvive = GetStatPreviewValue(info.Survivability, simulated, x => x.Survivability);
        int nextSpeed = GetStatPreviewValue(info.Speed, simulated, x => x.Speed);
        int nextLife = GetStatPreviewValue(info.LifeMax, simulated, x => x.MaxLife);

        PowerValueLabel.Text = FormatPreview(currentPower, nextPower);
        SurviveValueLabel.Text = FormatPreview(currentSurvive, nextSurvive);
        SpeedValueLabel.Text = FormatPreview(currentSpeed, nextSpeed);
        LifeValueLabel.Text = FormatPreview(currentLife, nextLife);

    }

    private void RefreshActionButtons()
    {
        EquipButton.Text = $"装配到槽 {SlotLetters[_selectedSlotIndex]}";
        UnequipButton.Text = $"卸下槽 {SlotLetters[_selectedSlotIndex]}";

        EquipButton.Disabled = (uint)_selectedInventoryCardIndex >= (uint)_catalog.Length;

        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            UnequipButton.Disabled = !HasValidEquipment(info.Equipments[_selectedSlotIndex]);
        }
        else
        {
            UnequipButton.Disabled = true;
        }
    }

    private void InitializeSlotPanelStyles()
    {
        for (int i = 0; i < SlotPanels.Length; i++)
        {
            var style = SlotPanels[i].GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
            if (style == null)
                continue;

            _slotRuntimeStyleBoxes[i] = style;
            SlotPanels[i].AddThemeStyleboxOverride("panel", style);
            _slotCurrentBorderColors[i] = style.BorderColor;
            ApplySlotPanelBorderState(i, animate: false);
        }
    }

    private void ApplySlotPanelBorderState(int slotIndex, bool animate)
    {
        if ((uint)slotIndex >= (uint)_slotRuntimeStyleBoxes.Length)
            return;

        var style = _slotRuntimeStyleBoxes[slotIndex];
        if (style == null)
            return;

        Color targetColor =
            slotIndex == _selectedSlotIndex
                ? SlotSelectedBorderColor
                : (_slotHovered[slotIndex] ? SlotHoverBorderColor : SlotDefaultBorderColor);

        if (!animate || slotIndex == _selectedSlotIndex)
        {
            _slotBorderTweens[slotIndex]?.Kill();
            style.BorderColor = targetColor;
            _slotCurrentBorderColors[slotIndex] = targetColor;
            return;
        }

        _slotBorderTweens[slotIndex]?.Kill();
        Color startColor = _slotCurrentBorderColors[slotIndex];
        var tween = CreateTween();
        _slotBorderTweens[slotIndex] = tween;
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenMethod(
            Callable.From<float>(t =>
            {
                var c = startColor.Lerp(targetColor, t);
                _slotCurrentBorderColors[slotIndex] = c;
                style.BorderColor = c;
            }),
            0.0f,
            1.0f,
            SlotBorderTweenDuration
        );
    }

    private async Task AnimateSlotContentExitAsync(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)SlotCards.Length)
            return;

        await SlotCards[slotIndex]
            .PlayRemoveAnimation(SlotEquipExitMoveDistance, SlotExitDuration, keepHidden: true);
    }

    private async Task AnimateSlotContentEnterAsync(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)SlotCards.Length)
            return;
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;
        if (!HasValidEquipment(info.Equipments[slotIndex]))
            return;

        var card = SlotCards[slotIndex];
        if (!card.Visible)
            return;

        // Ensure the inner panel matches slot size before entering animation.
        card.SyncPanelSizeToCard();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        card.SyncPanelSizeToCard();

        await card.PlayInsertAnimation(SlotEquipEnterMoveDistance, SlotEnterDuration);
    }

    private async Task AnimateInventoryCardExitAsync(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (!card.Visible)
            return;

        card.SyncPanelSizeToCard();
        await card.PlayRemoveAnimation(
            InventoryMoveDistance,
            InventoryExitDuration,
            keepHidden: true
        );
    }

    private async Task AnimateInventoryCardEnterAsync(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (!card.Visible)
            return;

        card.SyncPanelSizeToCard();
        await card.PlayInsertAnimation(InventoryMoveDistance, InventoryEnterDuration);
    }

    private async Task AnimateInventoryCardEnterByNameAsync(Equipment.EquipmentName equipmentName)
    {
        int cardIndex = FindLastCatalogIndexByName(equipmentName);
        if (cardIndex < 0)
            return;

        await AnimateInventoryCardEnterAsync(cardIndex);
    }

    private async Task AnimateInventoryReflowAsync(
        Dictionary<Equipment.EquipmentName, Queue<Vector2>> previousPositions,
        int skipCardIndex = -1
    )
    {
        if (previousPositions == null || previousPositions.Count == 0)
            return;

        var cardsToAnimate = new List<(CardSlot card, Vector2 targetPos)>();
        int count = Math.Min(_catalog.Length, InventoryCards.Length);
        for (int i = 0; i < count; i++)
        {
            if (i == skipCardIndex)
                continue;

            var card = InventoryCards[i];
            if (!card.Visible)
                continue;

            var equipment = _catalog[i];
            if (
                !previousPositions.TryGetValue(equipment.Name, out var positions)
                || positions.Count == 0
            )
                continue;
            Vector2 previousPos = positions.Dequeue();

            Vector2 targetPos = card.Position;
            if (previousPos.DistanceSquaredTo(targetPos) < 0.25f)
                continue;

            card.Position = previousPos;
            cardsToAnimate.Add((card, targetPos));
        }

        if (cardsToAnimate.Count == 0)
            return;

        InventoryGridNode.SetLayoutSuppressed(true);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        for (int i = 0; i < cardsToAnimate.Count; i++)
        {
            var entry = cardsToAnimate[i];
            tween.TweenProperty(entry.card, "position", entry.targetPos, InventoryReflowDuration);
        }

        await ToSignal(tween, Tween.SignalName.Finished);

        InventoryGridNode.SetLayoutSuppressed(false);
        InventoryGridNode.LayoutCards(force: true);
    }

    private Dictionary<
        Equipment.EquipmentName,
        Queue<Vector2>
    > CaptureVisibleInventoryCardPositions()
    {
        var result = new Dictionary<Equipment.EquipmentName, Queue<Vector2>>();
        int count = Math.Min(_catalog.Length, InventoryCards.Length);
        for (int i = 0; i < count; i++)
        {
            if (!InventoryCards[i].Visible)
                continue;

            var equipmentName = _catalog[i].Name;
            if (!result.TryGetValue(equipmentName, out var positions))
            {
                positions = new Queue<Vector2>();
                result[equipmentName] = positions;
            }

            positions.Enqueue(InventoryCards[i].Position);
        }

        return result;
    }

}
