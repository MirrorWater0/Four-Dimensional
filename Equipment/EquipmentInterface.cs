using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EquipmentInterface : Control
{
    private const int SlotCount = 2;
    private const float SlotEquipMoveDistance = 72.0f;
    private const float SlotExitDuration = 0.16f;
    private const float SlotEnterDuration = 0.2f;
    private const float InventoryMoveDistance = 72.0f;
    private const float InventoryExitDuration = 0.16f;
    private const float InventoryEnterDuration = 0.2f;
    private const float InventoryReflowDuration = 0.18f;
    private const float SlotBorderTweenDuration = 0.12f;

    private static readonly string[] SlotLetters = ["A", "B"];
    private static readonly Color SelectedTextColor = new(1f, 0.55f, 0.55f, 1f);
    private static readonly Color NormalTextColor = new(0.9f, 0.95f, 1f, 1f);
    private static readonly Color SlotDefaultBorderColor = new("#a7d6ff52");
    private static readonly Color SlotHoverBorderColor = new("#5cff8a");
    private static readonly Color SlotSelectedBorderColor = new("#ffd24a");

    private Button CloseButton =>
        field ??= GetNode<Button>("RootMargin/MainVBox/TopBar/CloseButton");

    private Button[] CharacterButtons =>
        field ??= [
            GetNode<Button>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectPanel/CharacterSelectList/EchoButton"
            ),
            GetNode<Button>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectPanel/CharacterSelectList/KasiyaButton"
            ),
            GetNode<Button>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectPanel/CharacterSelectList/MariyaButton"
            ),
            GetNode<Button>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/CharacterSelectPanel/CharacterSelectList/NightingaleButton"
            ),
        ];

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

    private CardSlot[] SlotCards =>
        field ??= [
            GetNode<CardSlot>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA/Card"
            ),
            GetNode<CardSlot>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB/Card"
            ),
        ];
    private SlotDropPanel[] SlotPanels =>
        field ??= [
            GetNode<SlotDropPanel>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA"
            ),
            GetNode<SlotDropPanel>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB"
            ),
        ];
    private Label[] SlotCardLabels =>
        field ??= [
            SlotCards[0].label,
            SlotCards[1].label,
        ];

    private VBoxContainer InventoryGrid =>
        field ??= GetNode<VBoxContainer>(
            "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid"
        );
    private CardSlot[] InventoryCards => field ??= BuildInventoryCards();
    private Label[] InventoryCardLabels => field ??= BuildInventoryCardLabels();

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
    private Equipment _selectedEquipment;
    private bool _isEquipAnimating;
    private readonly StyleBoxFlat[] _slotRuntimeStyleBoxes = new StyleBoxFlat[SlotCount];
    private readonly Tween[] _slotBorderTweens = new Tween[SlotCount];
    private readonly Color[] _slotCurrentBorderColors = new Color[SlotCount];
    private readonly bool[] _slotHovered = new bool[SlotCount];
    public bool HasEquipmentChanges { get; private set; }

    public override void _Ready()
    {
        EnsureGameData();

        _catalog = Array.Empty<Equipment>();
        _selectedCharacterIndex = 0;
        _selectedSlotIndex = 0;
        _selectedEquipment = null;

        InitializeSlotPanelStyles();
        WireUiEvents();
        RefreshAll();
    }

    private void WireUiEvents()
    {
        CloseButton.Pressed += QueueFree;

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
            SlotPanels[i].MouseEntered += () => OnSlotPanelMouseEntered(captured);
            SlotPanels[i].MouseExited += () => OnSlotPanelMouseExited(captured);
            SlotPanels[i].InventoryCardDropped += OnSlotPanelInventoryCardDropped;
        }

        for (int i = 0; i < InventoryCards.Length; i++)
        {
            int captured = i;
            InventoryCards[i].Clicked += () => OnInventoryCardPressed(captured);
        }

        UnequipButton.Pressed += OnUnequipPressed;
    }

    private void OnCharacterButtonPressed(int characterIndex)
    {
        if (
            GameInfo.PlayerCharacters == null
            || (uint)characterIndex >= (uint)GameInfo.PlayerCharacters.Length
        )
            return;

        _selectedCharacterIndex = characterIndex;
        RefreshAll();
    }

    private void OnSlotCardPressed(int slotIndex)
    {
        _selectedSlotIndex = Math.Clamp(slotIndex, 0, SlotCount - 1);
        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            var equipped = info.Equipments[_selectedSlotIndex];
            _selectedEquipment = HasValidEquipment(equipped) ? Equipment.Clone(equipped) : null;
            SaveSelectedPlayerInfo(in info);
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

    private async void OnSlotPanelInventoryCardDropped(int slotIndex, NodePath sourceCardPath)
    {
        if (_isEquipAnimating)
            return;
        if ((uint)slotIndex >= (uint)SlotCount)
            return;

        _selectedSlotIndex = slotIndex;
        int cardIndex = FindInventoryCardIndexByPath(sourceCardPath);
        if (cardIndex < 0 || cardIndex >= _catalog.Length)
        {
            RefreshAll();
            return;
        }

        await EquipInventoryCardToSlotAsync(cardIndex, slotIndex, animateReplacedToInventory: true);
    }

    private void OnSlotPanelMouseEntered(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)SlotCount)
            return;
        _slotHovered[slotIndex] = true;
        ApplySlotPanelBorderState(slotIndex, animate: true);
    }

    private void OnSlotPanelMouseExited(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)SlotCount)
            return;
        _slotHovered[slotIndex] = false;
        ApplySlotPanelBorderState(slotIndex, animate: true);
    }

    private void OnInventoryCardPressed(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)_catalog.Length)
            return;

        _selectedEquipment = Equipment.Clone(_catalog[cardIndex]);
        RefreshAll();
    }

    private async void OnEquipPressed()
    {
        if (_isEquipAnimating)
            return;
        if (_selectedEquipment == null)
            return;
        int removedCardIndex = FindCatalogIndexByName(_selectedEquipment.Name);
        if (removedCardIndex < 0)
            return;

        await EquipInventoryCardToSlotAsync(
            removedCardIndex,
            _selectedSlotIndex,
            animateReplacedToInventory: false
        );
    }

    private async Task EquipInventoryCardToSlotAsync(
        int inventoryCardIndex,
        int slotIndex,
        bool animateReplacedToInventory
    )
    {
        if (_isEquipAnimating)
            return;
        if ((uint)slotIndex >= (uint)SlotCount)
            return;
        if ((uint)inventoryCardIndex >= (uint)_catalog.Length)
            return;
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;

        _isEquipAnimating = true;
        try
        {
            _selectedSlotIndex = slotIndex;
            var selectedFromInventory = Equipment.Clone(_catalog[inventoryCardIndex]);
            bool hadEquip = HasValidEquipment(info.Equipments[slotIndex]);
            Vector2 replacedEquipFromGlobalPos = hadEquip
                ? SlotCards[slotIndex].GlobalPosition
                : Vector2.Zero;
            var inventoryPositionsBeforeRefresh = CaptureVisibleInventoryCardPositions();
            Equipment.EquipmentName? replacedEquipName = hadEquip ? info.Equipments[slotIndex].Name : null;

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

            RefreshAll();

            int replacedCardIndex = replacedEquipName.HasValue
                ? FindLastCatalogIndexByName(replacedEquipName.Value)
                : -1;

            Task slotEnterTask = AnimateSlotContentEnterAsync(slotIndex);
            Task inventoryReflowTask = AnimateInventoryReflowAsync(
                inventoryPositionsBeforeRefresh,
                replacedCardIndex
            );
            Task replacedEquipTask = Task.CompletedTask;
            if (replacedCardIndex >= 0)
            {
                replacedEquipTask = animateReplacedToInventory
                    ? AnimateReplacedEquipmentToInventoryEndAsync(
                        replacedCardIndex,
                        replacedEquipFromGlobalPos
                    )
                    : AnimateInventoryCardEnterAsync(replacedCardIndex);
            }

            await Task.WhenAll(slotEnterTask, inventoryReflowTask, replacedEquipTask);
        }
        finally
        {
            _isEquipAnimating = false;
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

        _isEquipAnimating = true;
        try
        {
            Equipment unequipped = Equipment.Clone(info.Equipments[_selectedSlotIndex]);
            Equipment.EquipmentName unequippedName = unequipped.Name;
            await AnimateSlotContentExitAsync(_selectedSlotIndex);

            info.Equipments[_selectedSlotIndex] = null;
            SaveSelectedPlayerInfo(in info);
            GameInfo.OwnedEquipments.Add(unequipped);
            HasEquipmentChanges = true;

            RefreshAll();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await AnimateInventoryCardEnterByNameAsync(unequippedName);
        }
        finally
        {
            _isEquipAnimating = false;
        }
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
            CharacterButtons[i].Text = string.IsNullOrWhiteSpace(info.CharacterName)
                ? $"角色 {i + 1}"
                : info.CharacterName;
            CharacterButtons[i].ButtonPressed = (i == _selectedCharacterIndex);
        }
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
        {
            var equip = HasValidEquipment(info.Equipments[i]) ? info.Equipments[i] : null;
            bool hasEquip = equip != null;
            SlotCards[i].Visible = hasEquip;
            if (SlotCardLabels[i] != null)
            {
                SlotCardLabels[i].Text = hasEquip
                    ? $"{equip.DisplayName}\n{BuildEquipmentBonusInline(equip)}"
                    : string.Empty;
            }

            bool selected = i == _selectedSlotIndex;
            if (selected && hasEquip)
                SlotCards[i].Select();
            else
                SlotCards[i].Unselect();
            if (SlotCardLabels[i] != null)
                SlotCardLabels[i].Modulate = selected ? SelectedTextColor : NormalTextColor;
            ApplySlotPanelBorderState(i, animate: false);
        }

        SaveSelectedPlayerInfo(in info);
    }

    private void RefreshInventoryCards()
    {
        for (int i = 0; i < InventoryCards.Length; i++)
        {
            bool visible = i < _catalog.Length;
            InventoryCards[i].Visible = visible;
            if (!visible)
            {
                InventoryCards[i].Unselect();
                continue;
            }

            var eq = _catalog[i];
            if (InventoryCardLabels[i] != null)
            {
                InventoryCardLabels[i].Text = $"{eq.DisplayName}\n{BuildEquipmentBonusInline(eq)}";
                InventoryCardLabels[i].Modulate = NormalTextColor;
            }

            bool selected = _selectedEquipment != null && _selectedEquipment.Name == eq.Name;
            if (selected)
            {
                InventoryCards[i].Select();
                if (InventoryCardLabels[i] != null)
                    InventoryCardLabels[i].Modulate = SelectedTextColor;
            }
            else
                InventoryCards[i].Unselect();
        }
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

        int currentPower = info.Power + SumEquipment(current, x => x.Power);
        int currentSurvive = info.Survivability + SumEquipment(current, x => x.Survivability);
        int currentSpeed = info.Speed + SumEquipment(current, x => x.Speed);
        int currentLife = info.LifeMax + SumEquipment(current, x => x.MaxLife);

        int nextPower = info.Power + SumEquipment(simulated, x => x.Power);
        int nextSurvive = info.Survivability + SumEquipment(simulated, x => x.Survivability);
        int nextSpeed = info.Speed + SumEquipment(simulated, x => x.Speed);
        int nextLife = info.LifeMax + SumEquipment(simulated, x => x.MaxLife);

        PowerValueLabel.Text = FormatPreview(currentPower, nextPower);
        SurviveValueLabel.Text = FormatPreview(currentSurvive, nextSurvive);
        SpeedValueLabel.Text = FormatPreview(currentSpeed, nextSpeed);
        LifeValueLabel.Text = FormatPreview(currentLife, nextLife);

        SaveSelectedPlayerInfo(in info);
    }

    private void RefreshActionButtons()
    {
        EquipButton.Text = $"拖拽到槽 {SlotLetters[_selectedSlotIndex]} 装配";
        UnequipButton.Text = $"卸下槽 {SlotLetters[_selectedSlotIndex]}";

        EquipButton.Disabled = true;

        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            UnequipButton.Disabled = !HasValidEquipment(info.Equipments[_selectedSlotIndex]);
            SaveSelectedPlayerInfo(in info);
        }
        else
        {
            UnequipButton.Disabled = true;
        }
    }

    private bool TryGetSelectedPlayerInfo(out PlayerInfoStructure info)
    {
        var players = GameInfo.PlayerCharacters;
        if (players == null || (uint)_selectedCharacterIndex >= (uint)players.Length)
        {
            info = default;
            return false;
        }

        info = players[_selectedCharacterIndex];
        return true;
    }

    private bool TryGetSelectedPlayerWithEquipments(out PlayerInfoStructure info)
    {
        if (!TryGetSelectedPlayerInfo(out info))
            return false;

        EnsureEquipmentArray(ref info);
        return true;
    }

    private void SaveSelectedPlayerInfo(in PlayerInfoStructure info)
    {
        GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
    }

    private static void EnsureEquipmentArray(ref PlayerInfoStructure info)
    {
        var source = info.Equipments ?? Array.Empty<Equipment>();
        if (source.Length >= SlotCount)
        {
            for (int i = 0; i < SlotCount; i++)
                if (!HasValidEquipment(source[i]))
                    source[i] = null;
            return;
        }

        Equipment[] result = new Equipment[SlotCount];
        Array.Copy(source, result, Math.Min(source.Length, SlotCount));
        for (int i = 0; i < result.Length; i++)
            if (!HasValidEquipment(result[i]))
                result[i] = null;
        info.Equipments = result;
    }

    private static Equipment[] CloneEquipArray(Equipment[] source)
    {
        var result = new Equipment[SlotCount];
        if (source == null)
            return result;

        Array.Copy(source, result, Math.Min(source.Length, SlotCount));
        for (int i = 0; i < result.Length; i++)
            result[i] = Equipment.Clone(result[i]);
        return result;
    }

    private static int SumEquipment(Equipment[] equips, Func<Equipment, int> selector)
    {
        if (equips == null)
            return 0;

        int sum = 0;
        foreach (var equip in equips)
            if (HasValidEquipment(equip))
                sum += selector(equip);
        return sum;
    }

    private static string BuildShortPassiveText(string passive)
    {
        if (string.IsNullOrWhiteSpace(passive))
            return "暂无被动描述。";

        int newline = passive.IndexOf('\n');
        return newline > 0 ? passive[..newline] : passive;
    }

    private static string BuildEquipmentBonusInline(Equipment equip)
    {
        if (equip == null)
            return "力量 +0  生存 +0  速度 +0  生命 +0";

        return $"力量 +{equip.Power}  生存 +{equip.Survivability}  速度 +{equip.Speed}  生命 +{equip.MaxLife}";
    }

    private static string FormatPreview(int current, int next)
    {
        return current == next ? current.ToString() : $"{current} → {next}";
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

        Color targetColor = slotIndex == _selectedSlotIndex
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

        await SlotCards[slotIndex].PlayRemoveAnimation(SlotEquipMoveDistance, SlotExitDuration);
    }

    private async Task AnimateSlotContentEnterAsync(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)SlotCards.Length)
            return;
        if (!TryGetSelectedPlayerWithEquipments(out var info))
            return;
        if (!HasValidEquipment(info.Equipments[slotIndex]))
            return;

        // When inserting into an empty slot, the card just became visible and may need a frame
        // for container layout to finalize its size before playing move/alpha animation.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (SlotCards[slotIndex].Size.X <= 1f || SlotCards[slotIndex].Size.Y <= 1f)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        await SlotCards[slotIndex].PlayInsertAnimation(SlotEquipMoveDistance, SlotEnterDuration);
    }

    private async Task AnimateInventoryCardExitAsync(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (!card.Visible)
            return;

        await card.PlayRemoveAnimation(InventoryMoveDistance, InventoryExitDuration);
    }

    private async Task AnimateInventoryCardEnterAsync(int cardIndex)
    {
        if ((uint)cardIndex >= (uint)InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (!card.Visible)
            return;

        await card.PlayInsertAnimation(InventoryMoveDistance, InventoryEnterDuration);
    }

    private async Task AnimateInventoryCardEnterByNameAsync(Equipment.EquipmentName equipmentName)
    {
        int cardIndex = FindLastCatalogIndexByName(equipmentName);
        if (cardIndex < 0)
            return;

        await AnimateInventoryCardEnterAsync(cardIndex);
    }

    private async Task AnimateReplacedEquipmentToInventoryEndAsync(
        int cardIndex,
        Vector2 fromGlobalPosition
    )
    {
        if ((uint)cardIndex >= (uint)InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (!card.Visible)
            return;

        Vector2 targetGlobal = card.GlobalPosition;
        bool originalTopLevel = card.TopLevel;
        SetControlTopLevel(card, true);
        card.GlobalPosition = fromGlobalPosition;
        SetControlAlpha(card, 1.0f);

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Quint);
        tween.TweenProperty(card, "global_position", targetGlobal, InventoryEnterDuration);
        await ToSignal(tween, Tween.SignalName.Finished);

        SetControlTopLevel(card, originalTopLevel);
        card.GlobalPosition = targetGlobal;
        SetControlAlpha(card, 1.0f);
    }

    private async Task AnimateInventoryReflowAsync(
        Dictionary<Equipment.EquipmentName, Queue<Vector2>> previousPositions,
        int skipCardIndex = -1
    )
    {
        if (previousPositions == null || previousPositions.Count == 0)
            return;

        var cardsToRestore =
            new List<(PanelContainer card, bool originalTopLevel, Vector2 targetPos)>();
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

            Vector2 targetPos = card.GlobalPosition;
            if (previousPos.DistanceSquaredTo(targetPos) < 0.25f)
                continue;

            bool originalTopLevel = card.TopLevel;
            SetControlTopLevel(card, true);
            card.GlobalPosition = previousPos;
            SetControlAlpha(card, 1.0f);
            cardsToRestore.Add((card, originalTopLevel, targetPos));
        }

        if (cardsToRestore.Count == 0)
            return;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        for (int i = 0; i < cardsToRestore.Count; i++)
        {
            var entry = cardsToRestore[i];
            tween.TweenProperty(
                entry.card,
                "global_position",
                entry.targetPos,
                InventoryReflowDuration
            );
        }

        await ToSignal(tween, Tween.SignalName.Finished);

        for (int i = 0; i < cardsToRestore.Count; i++)
        {
            var entry = cardsToRestore[i];
            SetControlTopLevel(entry.card, entry.originalTopLevel);
            entry.card.GlobalPosition = entry.targetPos;
        }
    }

    private Dictionary<Equipment.EquipmentName, Queue<Vector2>> CaptureVisibleInventoryCardPositions()
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

            positions.Enqueue(InventoryCards[i].GlobalPosition);
        }

        return result;
    }

    private int FindCatalogIndexByName(Equipment.EquipmentName equipmentName)
    {
        for (int i = 0; i < _catalog.Length; i++)
        {
            if (_catalog[i].Name == equipmentName)
                return i;
        }
        return -1;
    }

    private int FindInventoryCardIndexByPath(NodePath sourceCardPath)
    {
        if (sourceCardPath.IsEmpty)
            return -1;

        var sourceCard = GetNodeOrNull<CardSlot>(sourceCardPath);
        if (sourceCard == null)
            return -1;

        for (int i = 0; i < InventoryCards.Length; i++)
        {
            if (InventoryCards[i] == sourceCard)
                return i;
        }
        return -1;
    }

    private int FindLastCatalogIndexByName(Equipment.EquipmentName equipmentName)
    {
        for (int i = _catalog.Length - 1; i >= 0; i--)
        {
            if (_catalog[i].Name == equipmentName)
                return i;
        }
        return -1;
    }

    private CardSlot[] BuildInventoryCards()
    {
        return InventoryGrid.GetChildren().OfType<CardSlot>().ToArray();
    }

    private Label[] BuildInventoryCardLabels()
    {
        return InventoryCards.Select(card => card.label).ToArray();
    }

    private static void SetControlTopLevel(Control control, bool topLevel)
    {
        if (control.TopLevel == topLevel)
            return;

        Vector2 globalPos = control.GlobalPosition;
        control.TopLevel = topLevel;
        control.GlobalPosition = globalPos;
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        control.Modulate = control.Modulate with { A = alpha };
    }

    private void RefreshAvailableCatalog()
    {
        _catalog = BuildAvailableCatalog();

        if (_catalog.Length == 0)
        {
            _selectedEquipment = null;
            return;
        }

        if (_selectedEquipment == null)
            return;

        for (int i = 0; i < _catalog.Length; i++)
        {
            if (_catalog[i].Name != _selectedEquipment.Name)
                continue;

            _selectedEquipment = Equipment.Clone(_catalog[i]);
            return;
        }

        if (TryGetSelectedPlayerWithEquipments(out var info))
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var equipped = info.Equipments[i];
                if (!HasValidEquipment(equipped))
                    continue;
                if (equipped.Name != _selectedEquipment.Name)
                    continue;

                _selectedEquipment = Equipment.Clone(equipped);
                SaveSelectedPlayerInfo(in info);
                return;
            }
            SaveSelectedPlayerInfo(in info);
        }

        // Keep an empty selection when current selected card is removed from available list.
        _selectedEquipment = null;
    }

    private Equipment[] BuildAvailableCatalog()
    {
        EnsureOwnedEquipmentsInitialized();
        var owned = GameInfo.OwnedEquipments;
        var available = new List<Equipment>(owned.Count);
        foreach (var equip in owned)
        {
            if (!HasValidEquipment(equip))
                continue;

            available.Add(Equipment.Clone(equip));
        }
        return available.ToArray();
    }

    private static void EnsureOwnedEquipmentsInitialized()
    {
        if (GameInfo.OwnedEquipments == null)
            GameInfo.OwnedEquipments = new List<Equipment>();

        if (GameInfo.OwnedEquipments.Count > 0)
            return;

        // Only seed starter inventory for fresh/legacy data (no characters yet).
        if (GameInfo.PlayerCharacters != null && GameInfo.PlayerCharacters.Length > 0)
            return;

        // Keep current behavior for old saves that have no inventory yet.
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.RiftBlade));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.PhaseShoulder));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.EchoCore));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.LumenBadge));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.SilentPendant));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.FoldedBulwark));
        GameInfo.OwnedEquipments.RemoveAll(x => !HasValidEquipment(x));
    }

    private static void RemoveOwnedEquipmentByAvailableIndex(int availableIndex)
    {
        if (availableIndex < 0)
            return;

        EnsureOwnedEquipmentsInitialized();
        int visibleIndex = 0;
        for (int i = 0; i < GameInfo.OwnedEquipments.Count; i++)
        {
            if (!HasValidEquipment(GameInfo.OwnedEquipments[i]))
                continue;
            if (visibleIndex == availableIndex)
            {
                GameInfo.OwnedEquipments.RemoveAt(i);
                return;
            }
            visibleIndex++;
        }
    }

    private static bool HasValidEquipment(Equipment equip)
    {
        return equip != null && !string.IsNullOrWhiteSpace(equip.DisplayName);
    }

    private static void EnsureGameData()
    {
        EnsureOwnedEquipmentsInitialized();

        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
        {
            GameInfo.PlayerCharacters =
            [
                new PlayerCharacterRegistry().Echo,
                new PlayerCharacterRegistry().Kasiya,
                new PlayerCharacterRegistry().Mariya,
                new PlayerCharacterRegistry().Nightingale,
            ];

            for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
            {
                var info = GameInfo.PlayerCharacters[i];
                info.PositionIndex = i + 1;
                GameInfo.PlayerCharacters[i] = info;
            }
        }

        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var info = GameInfo.PlayerCharacters[i];
            EnsureEquipmentArray(ref info);
            GameInfo.PlayerCharacters[i] = info;
        }
    }
}
