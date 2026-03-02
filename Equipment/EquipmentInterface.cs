using System;
using System.Collections.Generic;
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

    private static readonly string[] SlotLetters = ["A", "B"];
    private static readonly Color SelectedFrameColor = new(0.93f, 0.26f, 0.24f, 0.95f);
    private static readonly Color UnselectedSlotFrameColor = new(0.654902f, 0.839216f, 1f, 0.34f);
    private static readonly Color SelectedTextColor = new(1f, 0.55f, 0.55f, 1f);
    private static readonly Color NormalTextColor = new(0.9f, 0.95f, 1f, 1f);

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

    private PanelContainer[] SlotPanels =>
        field ??= [
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB"
            ),
        ];
    private VBoxContainer[] SlotContents =>
        field ??= [
            GetNode<VBoxContainer>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA/SlotAContent"
            ),
            GetNode<VBoxContainer>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB/SlotBContent"
            ),
        ];
    private Label[] SlotNameLabels =>
        field ??= [
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA/SlotAContent/SlotAName"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB/SlotBContent/SlotBName"
            ),
        ];
    private Label[] SlotBonusLabels =>
        field ??= [
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotA/SlotAContent/SlotABonus"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/LeftPanel/LeftVBox/EquippedSlots/SlotB/SlotBContent/SlotBBonus"
            ),
        ];

    private PanelContainer[] InventoryCards =>
        field ??= [
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card1"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card2"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card3"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card4"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card5"
            ),
            GetNode<PanelContainer>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card6"
            ),
        ];
    private Label[] InventoryCardLabels =>
        field ??= [
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card1/Card1Label"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card2/Card2Label"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card3/Card3Label"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card4/Card4Label"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card5/Card5Label"
            ),
            GetNode<Label>(
                "RootMargin/MainVBox/ContentRow/CenterPanel/CenterVBox/InventoryScroll/InventoryGrid/Card6/Card6Label"
            ),
        ];

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
    private StyleBoxFlat _slotNormalStyle;
    private StyleBoxFlat _slotSelectedStyle;
    private StyleBoxFlat _cardNormalStyle;
    private StyleBoxFlat _cardSelectedStyle;
    private bool _isEquipAnimating;

    public override void _Ready()
    {
        EnsureGameData();

        _catalog = Array.Empty<Equipment>();
        _selectedCharacterIndex = 0;
        _selectedSlotIndex = 0;
        _selectedEquipment = null;

        for (int i = 0; i < SlotPanels.Length; i++)
        {
            SlotContents[i].MouseFilter = MouseFilterEnum.Ignore;
            SlotNameLabels[i].MouseFilter = MouseFilterEnum.Ignore;
            SlotBonusLabels[i].MouseFilter = MouseFilterEnum.Ignore;
        }
        for (int i = 0; i < InventoryCardLabels.Length; i++)
            InventoryCardLabels[i].MouseFilter = MouseFilterEnum.Ignore;

        WireUiEvents();
        EnsureSelectionStyles();
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

        for (int i = 0; i < SlotPanels.Length; i++)
        {
            int captured = i;
            SlotPanels[i].GuiInput += ev => OnSlotGuiInput(captured, ev);
        }

        for (int i = 0; i < InventoryCards.Length; i++)
        {
            int captured = i;
            InventoryCards[i].GuiInput += ev => OnInventoryCardGuiInput(captured, ev);
        }

        EquipButton.Pressed += OnEquipPressed;
        UnequipButton.Pressed += OnUnequipPressed;
    }

    private void OnCharacterButtonPressed(int characterIndex)
    {
        if (GameInfo.PlayerCharacters == null || characterIndex >= GameInfo.PlayerCharacters.Length)
            return;

        _selectedCharacterIndex = characterIndex;
        RefreshAll();
    }

    private void OnSlotGuiInput(int slotIndex, InputEvent ev)
    {
        if (ev is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left || !mb.Pressed)
            return;

        _selectedSlotIndex = Math.Clamp(slotIndex, 0, SlotCount - 1);
        RefreshAll();
    }

    private void OnInventoryCardGuiInput(int cardIndex, InputEvent ev)
    {
        if (ev is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Left || !mb.Pressed)
            return;

        if (cardIndex < 0 || cardIndex >= _catalog.Length)
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

        if (!TryGetSelectedPlayerInfo(out var info))
            return;

        _isEquipAnimating = true;
        try
        {
            EnsureEquipmentArray(ref info);
            bool hadEquip = info.Equipments[_selectedSlotIndex] != null;
            var inventoryPositionsBeforeRefresh = CaptureVisibleInventoryCardPositions();
            Equipment.EquipmentName? replacedEquipName = hadEquip
                ? info.Equipments[_selectedSlotIndex].Name
                : null;
            int removedCardIndex = FindCatalogIndexByName(_selectedEquipment.Name);

            Task slotExitTask = hadEquip
                ? AnimateSlotContentExitAsync(_selectedSlotIndex)
                : Task.CompletedTask;
            Task inventoryExitTask = removedCardIndex >= 0
                ? AnimateInventoryCardExitAsync(removedCardIndex)
                : Task.CompletedTask;
            await Task.WhenAll(slotExitTask, inventoryExitTask);

            RemoveOwnedEquipmentFirstByName(_selectedEquipment.Name);
            if (hadEquip && info.Equipments[_selectedSlotIndex] != null)
                GameInfo.OwnedEquipments.Add(Equipment.Clone(info.Equipments[_selectedSlotIndex]));

            info.Equipments[_selectedSlotIndex] = Equipment.Clone(_selectedEquipment);
            GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;

            RefreshAll();

            Task slotEnterTask = AnimateSlotContentEnterAsync(_selectedSlotIndex);
            Task inventoryReflowTask = AnimateInventoryReflowAsync(inventoryPositionsBeforeRefresh);
            Task inventoryEnterTask = replacedEquipName.HasValue
                ? AnimateInventoryCardEnterByNameAsync(replacedEquipName.Value)
                : Task.CompletedTask;
            await Task.WhenAll(slotEnterTask, inventoryEnterTask, inventoryReflowTask);
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

        if (!TryGetSelectedPlayerInfo(out var info))
            return;

        EnsureEquipmentArray(ref info);
        if (info.Equipments[_selectedSlotIndex] == null)
            return;

        _isEquipAnimating = true;
        try
        {
            var inventoryPositionsBeforeRefresh = CaptureVisibleInventoryCardPositions();
            Equipment unequipped = Equipment.Clone(info.Equipments[_selectedSlotIndex]);
            Equipment.EquipmentName unequippedName = unequipped.Name;
            await AnimateSlotContentExitAsync(_selectedSlotIndex);

            info.Equipments[_selectedSlotIndex] = null;
            GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
            GameInfo.OwnedEquipments.Add(unequipped);

            RefreshAll();

            Task slotEnterTask = AnimateSlotContentEnterAsync(_selectedSlotIndex);
            Task inventoryReflowTask = AnimateInventoryReflowAsync(inventoryPositionsBeforeRefresh);
            Task inventoryEnterTask = AnimateInventoryCardEnterByNameAsync(unequippedName);
            await Task.WhenAll(slotEnterTask, inventoryEnterTask, inventoryReflowTask);
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
        for (int i = 0; i < CharacterButtons.Length; i++)
        {
            bool exists = GameInfo.PlayerCharacters != null && i < GameInfo.PlayerCharacters.Length;
            CharacterButtons[i].Visible = exists;
            if (!exists)
                continue;

            var info = GameInfo.PlayerCharacters[i];
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
        if (!TryGetSelectedPlayerInfo(out var info))
            return;

        EnsureEquipmentArray(ref info);

        for (int i = 0; i < SlotCount; i++)
        {
            var equip = info.Equipments[i];
            SlotNameLabels[i].Text =
                equip == null ? $"装备槽 {SlotLetters[i]}（空）" : equip.DisplayName;
            SlotBonusLabels[i].Text = BuildEquipmentBonusInline(equip);

            bool selected = i == _selectedSlotIndex;
            if (_slotSelectedStyle != null && _slotNormalStyle != null)
                SlotPanels[i].AddThemeStyleboxOverride(
                    "panel",
                    selected ? _slotSelectedStyle : _slotNormalStyle
                );

            SlotPanels[i].Modulate = selected
                ? new Color(1.08f, 1.08f, 1.08f, 1f)
                : new Color(0.88f, 0.92f, 0.98f, 1f);
            SlotNameLabels[i].Modulate = selected ? SelectedTextColor : NormalTextColor;
            SlotBonusLabels[i].Modulate = selected
                ? new Color(1f, 0.78f, 0.78f, 1f)
                : new Color(0.86f, 0.92f, 1f, 1f);
        }

        GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
    }

    private void RefreshInventoryCards()
    {
        for (int i = 0; i < InventoryCards.Length; i++)
        {
            bool visible = i < _catalog.Length;
            InventoryCards[i].Visible = visible;
            if (!visible)
                continue;

            var eq = _catalog[i];
            InventoryCardLabels[i].Text = $"{eq.DisplayName}\n{BuildEquipmentBonusInline(eq)}";

            bool selected = _selectedEquipment != null && _selectedEquipment.Name == eq.Name;
            if (_cardSelectedStyle != null && _cardNormalStyle != null)
                InventoryCards[i].AddThemeStyleboxOverride(
                    "panel",
                    selected ? _cardSelectedStyle : _cardNormalStyle
                );

            InventoryCards[i].Modulate = selected
                ? new Color(1.08f, 1.08f, 1.08f, 1f)
                : new Color(0.9f, 0.94f, 1f, 1f);
            InventoryCardLabels[i].Modulate = selected ? SelectedTextColor : NormalTextColor;
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
        if (!TryGetSelectedPlayerInfo(out var info))
            return;

        EnsureEquipmentArray(ref info);

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

        GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
    }

    private void RefreshActionButtons()
    {
        EquipButton.Text = $"装配到槽 {SlotLetters[_selectedSlotIndex]}";
        UnequipButton.Text = $"卸下槽 {SlotLetters[_selectedSlotIndex]}";

        EquipButton.Disabled = _selectedEquipment == null;

        if (TryGetSelectedPlayerInfo(out var info))
        {
            EnsureEquipmentArray(ref info);
            UnequipButton.Disabled = info.Equipments[_selectedSlotIndex] == null;
            GameInfo.PlayerCharacters[_selectedCharacterIndex] = info;
        }
        else
        {
            UnequipButton.Disabled = true;
        }
    }

    private bool TryGetSelectedPlayerInfo(out PlayerInfoStructure info)
    {
        info = default;
        if (
            GameInfo.PlayerCharacters == null
            || _selectedCharacterIndex < 0
            || _selectedCharacterIndex >= GameInfo.PlayerCharacters.Length
        )
            return false;

        info = GameInfo.PlayerCharacters[_selectedCharacterIndex];
        return true;
    }

    private static void EnsureEquipmentArray(ref PlayerInfoStructure info)
    {
        if (info.Equipments != null && info.Equipments.Length >= SlotCount)
            return;

        Equipment[] result = new Equipment[SlotCount];
        if (info.Equipments != null)
        {
            int count = Math.Min(info.Equipments.Length, SlotCount);
            for (int i = 0; i < count; i++)
                result[i] = info.Equipments[i];
        }
        info.Equipments = result;
    }

    private static Equipment[] CloneEquipArray(Equipment[] source)
    {
        Equipment[] result = new Equipment[SlotCount];
        if (source == null)
            return result;

        int count = Math.Min(source.Length, SlotCount);
        for (int i = 0; i < count; i++)
            result[i] = Equipment.Clone(source[i]);
        return result;
    }

    private static int SumEquipment(Equipment[] equips, Func<Equipment, int> selector)
    {
        if (equips == null)
            return 0;

        int sum = 0;
        for (int i = 0; i < equips.Length; i++)
        {
            if (equips[i] == null)
                continue;
            sum += selector(equips[i]);
        }
        return sum;
    }

    private static string BuildShortPassiveText(string passive)
    {
        if (string.IsNullOrWhiteSpace(passive))
            return "暂无被动描述。";

        int newline = passive.IndexOf('\n');
        if (newline <= 0)
            return passive;

        return passive.Substring(0, newline);
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

    private async Task AnimateSlotContentExitAsync(int slotIndex)
    {
        var content = GetSlotContent(slotIndex);
        if (content == null)
            return;

        Vector2 baseGlobal = content.GlobalPosition;
        bool originalTopLevel = content.TopLevel;
        SetControlTopLevel(content, true);

        content.GlobalPosition = baseGlobal;
        SetControlAlpha(content, 1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(
            content,
            "global_position",
            baseGlobal + new Vector2(SlotEquipMoveDistance, 0),
            SlotExitDuration
        );
        tween.TweenProperty(content, "modulate:a", 0.0f, SlotExitDuration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetControlTopLevel(content, originalTopLevel);
        content.GlobalPosition = baseGlobal;
    }

    private async Task AnimateSlotContentEnterAsync(int slotIndex)
    {
        var content = GetSlotContent(slotIndex);
        if (content == null)
            return;

        Vector2 baseGlobal = content.GlobalPosition;
        bool originalTopLevel = content.TopLevel;
        SetControlTopLevel(content, true);

        content.GlobalPosition = baseGlobal - new Vector2(SlotEquipMoveDistance, 0);
        SetControlAlpha(content, 0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(content, "global_position", baseGlobal, SlotEnterDuration);
        tween.TweenProperty(content, "modulate:a", 1.0f, SlotEnterDuration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetControlTopLevel(content, originalTopLevel);
        content.GlobalPosition = baseGlobal;
        SetControlAlpha(content, 1.0f);
    }

    private async Task AnimateInventoryCardExitAsync(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (card == null || !card.Visible)
            return;

        Vector2 baseGlobal = card.GlobalPosition;
        bool originalTopLevel = card.TopLevel;
        SetControlTopLevel(card, true);

        card.GlobalPosition = baseGlobal;
        SetControlAlpha(card, 1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(
            card,
            "global_position",
            baseGlobal + new Vector2(InventoryMoveDistance, 0),
            InventoryExitDuration
        );
        tween.TweenProperty(card, "modulate:a", 0.0f, InventoryExitDuration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetControlTopLevel(card, originalTopLevel);
        card.GlobalPosition = baseGlobal;
    }

    private async Task AnimateInventoryCardEnterAsync(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= InventoryCards.Length)
            return;

        var card = InventoryCards[cardIndex];
        if (card == null || !card.Visible)
            return;

        Vector2 baseGlobal = card.GlobalPosition;
        bool originalTopLevel = card.TopLevel;
        SetControlTopLevel(card, true);

        card.GlobalPosition = baseGlobal - new Vector2(InventoryMoveDistance, 0);
        SetControlAlpha(card, 0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(card, "global_position", baseGlobal, InventoryEnterDuration);
        tween.TweenProperty(card, "modulate:a", 1.0f, InventoryEnterDuration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetControlTopLevel(card, originalTopLevel);
        card.GlobalPosition = baseGlobal;
        SetControlAlpha(card, 1.0f);
    }

    private async Task AnimateInventoryCardEnterByNameAsync(Equipment.EquipmentName equipmentName)
    {
        int cardIndex = FindCatalogIndexByName(equipmentName);
        if (cardIndex < 0)
            return;

        await AnimateInventoryCardEnterAsync(cardIndex);
    }

    private async Task AnimateInventoryReflowAsync(
        Dictionary<Equipment.EquipmentName, Vector2> previousPositions
    )
    {
        if (previousPositions == null || previousPositions.Count == 0)
            return;

        var cardsToRestore = new List<(PanelContainer card, bool originalTopLevel, Vector2 targetPos)>();
        int count = Math.Min(_catalog.Length, InventoryCards.Length);
        for (int i = 0; i < count; i++)
        {
            var card = InventoryCards[i];
            if (card == null || !card.Visible)
                continue;

            var equipment = _catalog[i];
            if (equipment == null || !previousPositions.TryGetValue(equipment.Name, out var previousPos))
                continue;

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
            tween.TweenProperty(entry.card, "global_position", entry.targetPos, InventoryReflowDuration);
        }

        await ToSignal(tween, Tween.SignalName.Finished);

        for (int i = 0; i < cardsToRestore.Count; i++)
        {
            var entry = cardsToRestore[i];
            SetControlTopLevel(entry.card, entry.originalTopLevel);
            entry.card.GlobalPosition = entry.targetPos;
        }
    }

    private Dictionary<Equipment.EquipmentName, Vector2> CaptureVisibleInventoryCardPositions()
    {
        var result = new Dictionary<Equipment.EquipmentName, Vector2>();
        int count = Math.Min(_catalog.Length, InventoryCards.Length);
        for (int i = 0; i < count; i++)
        {
            if (_catalog[i] == null || InventoryCards[i] == null || !InventoryCards[i].Visible)
                continue;

            result[_catalog[i].Name] = InventoryCards[i].GlobalPosition;
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

    private Control GetSlotContent(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotContents.Length)
            return null;
        return SlotContents[slotIndex];
    }

    private static void SetControlTopLevel(Control control, bool topLevel)
    {
        if (control == null || control.TopLevel == topLevel)
            return;

        Vector2 globalPos = control.GlobalPosition;
        control.TopLevel = topLevel;
        control.GlobalPosition = globalPos;
    }

    private static void SetControlAlpha(Control control, float alpha)
    {
        if (control == null)
            return;

        Color modulate = control.Modulate;
        control.Modulate = new Color(modulate.R, modulate.G, modulate.B, alpha);
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

        // Keep an empty selection when current selected card is removed from available list.
        _selectedEquipment = null;
    }

    private Equipment[] BuildAvailableCatalog()
    {
        EnsureOwnedEquipmentsInitialized();
        var available = new List<Equipment>(GameInfo.OwnedEquipments.Count);
        for (int i = 0; i < GameInfo.OwnedEquipments.Count; i++)
        {
            if (GameInfo.OwnedEquipments[i] == null)
                continue;

            available.Add(Equipment.Clone(GameInfo.OwnedEquipments[i]));
        }
        return available.ToArray();
    }

    private static void EnsureOwnedEquipmentsInitialized()
    {
        if (GameInfo.OwnedEquipments == null)
            GameInfo.OwnedEquipments = new List<Equipment>();

        if (GameInfo.OwnedEquipments.Count > 0)
            return;

        // Keep current behavior for old saves that have no inventory yet.
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.RiftBlade));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.PhaseShoulder));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.EchoCore));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.LumenBadge));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.SilentPendant));
        GameInfo.OwnedEquipments.Add(Equipment.Create(Equipment.EquipmentName.FoldedBulwark));
        GameInfo.OwnedEquipments.RemoveAll(x => x == null);
    }

    private static void RemoveOwnedEquipmentFirstByName(Equipment.EquipmentName equipmentName)
    {
        EnsureOwnedEquipmentsInitialized();
        for (int i = 0; i < GameInfo.OwnedEquipments.Count; i++)
        {
            if (GameInfo.OwnedEquipments[i] == null)
                continue;
            if (GameInfo.OwnedEquipments[i].Name != equipmentName)
                continue;

            GameInfo.OwnedEquipments.RemoveAt(i);
            return;
        }
    }

    private void EnsureSelectionStyles()
    {
        _slotNormalStyle = ClonePanelStyle(SlotPanels[0], UnselectedSlotFrameColor, 2);
        _slotSelectedStyle = ClonePanelStyle(SlotPanels[0], SelectedFrameColor, 3);
        _cardNormalStyle = ClonePanelStyle(InventoryCards[0], UnselectedSlotFrameColor, 2);
        _cardSelectedStyle = ClonePanelStyle(InventoryCards[0], SelectedFrameColor, 3);
    }

    private static StyleBoxFlat ClonePanelStyle(PanelContainer panel, Color borderColor, int borderWidth)
    {
        var source = panel.GetThemeStylebox("panel") as StyleBoxFlat;
        if (source == null)
            return null;

        var clone = source.Duplicate() as StyleBoxFlat;
        if (clone == null)
            return null;

        clone.BorderColor = borderColor;
        clone.BorderWidthLeft = borderWidth;
        clone.BorderWidthTop = borderWidth;
        clone.BorderWidthRight = borderWidth;
        clone.BorderWidthBottom = borderWidth;
        return clone;
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
