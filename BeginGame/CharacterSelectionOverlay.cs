using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

public partial class CharacterSelectionOverlay : Control
{
    private const int DefaultRequiredSelectionCount = GameInfo.DefaultPlayerPartySize;
    private const int MinDifficulty = GameInfo.MinDifficulty;
    private const int MaxDifficulty = GameInfo.MaxDifficulty;
    private const int MaxSeedDigits = 9;
    private const int MaxSeedValue = 999_999_999;
    private const float EnterAnimationDuration = 0.24f;
    private const float ExitAnimationDuration = 0.18f;
    private const float MaskTransitionDuration = 0.22f;
    private const float PanelEnterSlideOffset = 28f;
    private const float PanelExitSlideOffset = 18f;
    private const string CharacterCardScenePath = "res://BeginGame/CharacterSelectButton.tscn";
    private const int SpaceSm = 6;
    private const int SpaceMd = 12;
    private sealed class CharacterOption
    {
        public PlayerInfoStructure Info;
        public Texture2D Portrait;
        public Texture2D Hero;
        public Texture2D Icon;
        public CharacterSelectButton CardButton;
        public bool Hovered;
    }

    private readonly List<CharacterOption> _displayedFormation = new();
    private readonly List<CharacterOption> _options = new();
    private readonly List<CharacterOption> _selectedOptions = new();
    private CharacterOption _focusedOption;
    private static readonly Dictionary<string, string> CharacterHeroPaths = new()
    {
        ["Echo"] = "res://asset/UI/CharacterSelect/EchoHero.png",
        ["Kasiya"] = "res://asset/UI/CharacterSelect/KasiyaHero.png",
        ["Mariya"] = "res://asset/UI/CharacterSelect/MariyaHero.png",
        ["Nightingale"] = "res://asset/UI/CharacterSelect/NightingaleHero.png",
    };
    private static readonly Dictionary<string, string> CharacterIconPaths = new()
    {
        ["Echo"] = "res://asset/svg/CharacterIcon/Echo.svg",
        ["Kasiya"] = "res://asset/svg/CharacterIcon/Kasiya.svg",
        ["Mariya"] = "res://asset/svg/CharacterIcon/Mariya.svg",
        ["Nightingale"] = "res://asset/svg/CharacterIcon/Nightingale.svg",
    };

    private Action<PlayerInfoStructure[], int, int> _confirmAction;
    private int _requiredSelectionCount = DefaultRequiredSelectionCount;
    private int _selectedDifficulty = MinDifficulty;
    private bool _openRequested;
    private bool _isAnimating;
    private bool _isClosing;
    private bool _normalizingSeedInput;
    private bool _difficultyTooltipVisible;
    private Tween _overlayTween;
    private Tween _previewTween;
    private Tip _difficultyTooltip;
    private PackedScene _characterCardScene;
    private const string FormationPath = "SafeArea/Center/Panel/Margin/VBox/TeamBar";
    private ColorRect Shade => field ??= GetNodeOrNull<ColorRect>("Shade");
    private TextureRect HeroImage => field ??= GetNode<TextureRect>(DossierPath + "/Plate/HeroImage");
    private Label Folio => field ??= GetNode<Label>(DossierPath + "/Plate/Folio");
    private Control DossierText => field ??= GetNode<Control>(DossierStackPath);
    private Label Callsign => field ??= GetNode<Label>(DossierStackPath + "/Callsign");
    private Label PassiveTitle => field ??= GetNode<Label>(DossierStackPath + "/DossierScroll/DossierContent/PassiveTitle");
    private Label TeamCount => field ??= GetNode<Label>(FormationPath + "/TeamCount");
    private Control Panel => field ??= GetNodeOrNull<Control>("SafeArea/Center/Panel");
    private const string DossierPath = "SafeArea/Center/Panel/Margin/VBox/Body/DossierColumn";
    private const string DossierStackPath = DossierPath + "/CaptionArea/Stack";

    private Label TitleLabel => field ??= GetNodeOrNull<Label>(DossierStackPath + "/Title");
    private Label EyebrowLabel =>
        field ??= GetNodeOrNull<Label>("SafeArea/Center/Panel/Margin/VBox/Header/Eyebrow");
    private Label ScreenTitleLabel =>
        field ??= GetNodeOrNull<Label>("SafeArea/Center/Panel/Margin/VBox/Header/TitleRow/ScreenTitle");
    private Label RosterLabel =>
        field ??= GetNodeOrNull<Label>(
            "SafeArea/Center/Panel/Margin/VBox/Body/RosterColumn/RosterLabel"
        );
    private Label DossierLabel =>
        field ??= GetNodeOrNull<Label>(DossierStackPath + "/DossierLabel");
    private Control DossierColumn => field ??= GetNodeOrNull<Control>(DossierPath);
    private Label HintLabel =>
        field ??= GetNodeOrNull<Label>(DossierStackPath + "/DossierScroll/DossierContent/HintLabel")
            ?? GetNodeOrNull<Label>(DossierStackPath + "/HintLabel");
    private Label TeamLabel =>
        field ??= GetNodeOrNull<Label>(FormationPath + "/TeamLabel");
    private HBoxContainer TeamSlotsContainer =>
        field ??= GetNodeOrNull<HBoxContainer>(FormationPath + "/TeamSlots");
    private VBoxContainer CardGrid =>
        field ??= GetNodeOrNull<VBoxContainer>(
            "SafeArea/Center/Panel/Margin/VBox/Body/RosterColumn/CardGrid"
        );
    private Button ConfirmButton =>
        field ??= GetNodeOrNull<Button>("SafeArea/Center/Panel/Margin/VBox/Footer/ConfirmButton");
    private Button RandomStartButton =>
        field ??= GetNodeOrNull<Button>(
            "SafeArea/Center/Panel/Margin/VBox/Footer/RandomStartButton"
        );
    private Button CancelButton =>
        field ??= GetNodeOrNull<Button>("SafeArea/Center/Panel/Margin/VBox/Footer/CancelButton");
    private Control DifficultyBox =>
        field ??= GetNodeOrNull<Control>("SafeArea/Center/Panel/Margin/VBox/Footer/DifficultyBox");
    private Button DifficultyMinusButton =>
        field ??= GetNodeOrNull<Button>(
            "SafeArea/Center/Panel/Margin/VBox/Footer/DifficultyBox/DifficultyMinusButton"
        );
    private Button DifficultyPlusButton =>
        field ??= GetNodeOrNull<Button>(
            "SafeArea/Center/Panel/Margin/VBox/Footer/DifficultyBox/DifficultyPlusButton"
        );
    private Label DifficultyValueLabel =>
        field ??= GetNodeOrNull<Label>(
            "SafeArea/Center/Panel/Margin/VBox/Footer/DifficultyBox/DifficultyValueLabel"
        );
    private Label DifficultyLabel =>
        field ??= GetNodeOrNull<Label>(
            "SafeArea/Center/Panel/Margin/VBox/Footer/DifficultyBox/DifficultyLabel"
        );
    private Label SeedLabel =>
        field ??= GetNodeOrNull<Label>("SafeArea/Center/Panel/Margin/VBox/Footer/SeedBox/SeedLabel");
    private Tip DifficultyTooltip => _difficultyTooltip ??= EnsureDifficultyTooltip();
    private LineEdit SeedInput =>
        field ??= GetNodeOrNull<LineEdit>("SafeArea/Center/Panel/Margin/VBox/Footer/SeedBox/SeedInput");
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ProcessMode = ProcessModeEnum.Always;
        LocalizeStaticTexts();
        ApplyMobileTouchLayout();

        if (CancelButton != null)
        {
            CancelButton.ActionMode = BaseButton.ActionModeEnum.Press;
            CancelButton.Pressed += Close;
        }
        if (ConfirmButton != null)
        {
            ConfirmButton.ActionMode = BaseButton.ActionModeEnum.Press;
            ConfirmButton.Pressed += ConfirmSelection;
        }
        if (RandomStartButton != null)
        {
            RandomStartButton.ActionMode = BaseButton.ActionModeEnum.Press;
            RandomStartButton.Pressed += RandomizeSelectionAndStart;
        }
        SetupDifficultyButton(DifficultyMinusButton, -1);
        SetupDifficultyButton(DifficultyPlusButton, 1);
        WireDifficultyTooltipTarget(DifficultyBox);
        WireDifficultyTooltipTarget(DifficultyLabel);
        WireDifficultyTooltipTarget(DifficultyValueLabel);
        WireDifficultyTooltipTarget(DifficultyMinusButton);
        WireDifficultyTooltipTarget(DifficultyPlusButton);
        if (SeedInput != null)
        {
            SeedInput.ProcessMode = ProcessModeEnum.Always;
            SeedInput.MaxLength = MaxSeedDigits;
            SeedInput.TextChanged += NormalizeSeedInput;
            SeedInput.TextSubmitted += _ => ConfirmSelection();
        }

        if (!_openRequested)
        {
            Visible = false;
            Modulate = Colors.White;
        }
    }

    public override void _ExitTree()
    {
        _previewTween?.Kill();
        HideDifficultyTooltip();
    }

    private void ApplyMobileTouchLayout()
    {
        if (!MobilePlatform.IsMobile)
            return;

        MobilePlatform.EnsureContainerTouchTarget(DifficultyMinusButton, 72f, 60f);
        MobilePlatform.EnsureContainerTouchTarget(DifficultyPlusButton, 72f, 60f);
        MobilePlatform.EnsureContainerTouchTarget(CancelButton, minimumHeight: 72f);
        MobilePlatform.EnsureContainerTouchTarget(RandomStartButton, minimumHeight: 72f);
        MobilePlatform.EnsureContainerTouchTarget(ConfirmButton, minimumHeight: 72f);
        if (SeedInput != null)
            SeedInput.VirtualKeyboardType = LineEdit.VirtualKeyboardTypeEnum.Number;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _isClosing)
            return;

        if (
            @event is InputEventKey keyEvent
            && keyEvent.Pressed
            && !keyEvent.Echo
            && keyEvent.Keycode == Key.Escape
        )
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (
            !Visible
            || _isAnimating
            || _isClosing
            || @event is not InputEventMouseButton mouseEvent
            || !mouseEvent.Pressed
            || mouseEvent.ButtonIndex != MouseButton.Left
        )
        {
            return;
        }

        var option = FindOptionAtGlobalPosition(mouseEvent.GlobalPosition);
        if (option != null)
        {
            GetViewport().SetInputAsHandled();
            ToggleSelection(option);
            return;
        }

        if (IsButtonAtGlobalPosition(CancelButton, mouseEvent.GlobalPosition))
        {
            GetViewport().SetInputAsHandled();
            Close();
            return;
        }

        if (
            IsButtonAtGlobalPosition(RandomStartButton, mouseEvent.GlobalPosition)
            && RandomStartButton != null
            && !RandomStartButton.Disabled
        )
        {
            GetViewport().SetInputAsHandled();
            RandomizeSelectionAndStart();
            return;
        }

        if (IsButtonAtGlobalPosition(DifficultyMinusButton, mouseEvent.GlobalPosition))
        {
            GetViewport().SetInputAsHandled();
            AdjustDifficulty(-1);
            return;
        }

        if (IsButtonAtGlobalPosition(DifficultyPlusButton, mouseEvent.GlobalPosition))
        {
            GetViewport().SetInputAsHandled();
            AdjustDifficulty(1);
            return;
        }

        if (
            IsButtonAtGlobalPosition(ConfirmButton, mouseEvent.GlobalPosition)
            && ConfirmButton != null
            && !ConfirmButton.Disabled
        )
        {
            GetViewport().SetInputAsHandled();
            ConfirmSelection();
            return;
        }
    }

    public void Open(
        IReadOnlyList<PlayerInfoStructure> availableCharacters,
        int requiredSelectionCount,
        Action<PlayerInfoStructure[], int, int> confirmAction,
        bool playEnterAnimation = true
    )
    {
        _openRequested = true;
        _confirmAction = confirmAction;
        UserSettings.EnsureLoaded();
        _selectedDifficulty = Math.Clamp(
            UserSettings.LastSelectedDifficulty,
            MinDifficulty,
            MaxDifficulty
        );
        _requiredSelectionCount = Math.Max(
            1,
            Math.Min(requiredSelectionCount, availableCharacters?.Count ?? DefaultRequiredSelectionCount)
        );

        PopulateCharacters(availableCharacters ?? Array.Empty<PlayerInfoStructure>());
        RefreshDifficultyState();
        _isClosing = false;
        Visible = true;
        RevealRoster();
        Modulate = Colors.White;
        if (playEnterAnimation)
            _ = PlayEnterAnimationAsync();
        else
            ApplyOpenedVisualState();
    }

    private void PopulateCharacters(IReadOnlyList<PlayerInfoStructure> availableCharacters)
    {
        ClearChildren(CardGrid);
        _options.Clear();
        _displayedFormation.Clear();
        _selectedOptions.Clear();

        for (int i = 0; i < availableCharacters.Count; i++)
        {
            var option = new CharacterOption
            {
                Info = availableCharacters[i],
                Portrait = string.IsNullOrWhiteSpace(availableCharacters[i].PortaitPath)
                    ? null
                    : GD.Load<Texture2D>(availableCharacters[i].PortaitPath),
            };
            option.Hero = LoadHeroTexture(option.Info);
            option.Icon = LoadCharacterIcon(option.Info);

            option.CardButton = CreateCharacterCard(option);
            if (option.CardButton == null)
                continue;

            _options.Add(option);
            CardGrid?.AddChild(option.CardButton);
        }

        for (int i = 0; i < Math.Min(_requiredSelectionCount, _options.Count); i++)
            _selectedOptions.Add(_options[i]);

        _focusedOption = _options.FirstOrDefault();
        RefreshSelectionState();
        RefreshFocusedCharacterPreview(animate: false);
    }

    private CharacterSelectButton CreateCharacterCard(CharacterOption option)
    {
        if (option == null)
            return null;

        _characterCardScene ??= GD.Load<PackedScene>(CharacterCardScenePath);
        var card = _characterCardScene?.Instantiate<CharacterSelectButton>();
        if (card == null)
            return null;

        card.Name = $"{option.Info.CharacterName}Card";
        card.Visible = true;
        card.Disabled = false;
        card.ToggleMode = false;
        card.ButtonPressed = false;
        card.ActionMode = BaseButton.ActionModeEnum.Press;
        card.MouseFilter = MouseFilterEnum.Stop;
        card.FocusMode = FocusModeEnum.None;
        card.SetCharacter(option.Hero, option.Icon ?? option.Portrait, option.Info, ResolveCharacterAssetKey(option.Info));

        ApplyCardVisualState(option);
        card.MouseEntered += () =>
        {
            card.SetHoverMotion(true);
            option.Hovered = true;
            ApplyCardVisualState(option);
        };
        card.MouseExited += () =>
        {
            card.SetHoverMotion(false);
            option.Hovered = false;
            ApplyCardVisualState(option);
        };
        card.Pressed += () => ToggleSelection(option);
        return card;
    }

    private void ToggleSelection(CharacterOption option)
    {
        if (option == null)
            return;

        CharacterOption previousFocus = _focusedOption;
        bool wasSelected = _selectedOptions.Contains(option);
        if (wasSelected)
        {
            _focusedOption = option;
        }
        else if (_selectedOptions.Count < _requiredSelectionCount)
        {
            _selectedOptions.Add(option);
            _focusedOption = option;
        }
        else if (_selectedOptions.Count > 0)
        {
            int replaceIndex = _focusedOption != null
                ? _selectedOptions.IndexOf(_focusedOption)
                : -1;
            if (replaceIndex < 0)
                replaceIndex = _selectedOptions.Count - 1;

            _selectedOptions[replaceIndex] = option;
            _focusedOption = option;
        }
        else
        {
            _selectedOptions.Add(option);
            _focusedOption = option;
        }

        RefreshSelectionState();
        if (_focusedOption != previousFocus)
            RefreshFocusedCharacterPreview();
    }

    private CharacterOption FindOptionAtGlobalPosition(Vector2 globalPosition)
    {
        for (int i = _options.Count - 1; i >= 0; i--)
        {
            var option = _options[i];
            var card = option.CardButton;
            if (
                card == null
                || !GodotObject.IsInstanceValid(card)
                || !card.Visible
                || !card.GetGlobalRect().HasPoint(globalPosition)
            )
            {
                continue;
            }

            return option;
        }

        return null;
    }

    private static bool IsButtonAtGlobalPosition(Button button, Vector2 globalPosition)
    {
        return button != null
            && GodotObject.IsInstanceValid(button)
            && button.Visible
            && button.GetGlobalRect().HasPoint(globalPosition);
    }

    private void RefreshSelectionState()
    {
        for (int i = 0; i < _options.Count; i++)
        {
            var option = _options[i];
            bool selected = _selectedOptions.Contains(option);
            ApplyCardVisualState(option);

            int selectedIndex = _selectedOptions.IndexOf(option);
            bool focused = option == _focusedOption;
            option.CardButton?.SetBadge(
                selectedIndex >= 0 || focused,
                selectedIndex >= 0 ? (selectedIndex + 1).ToString("00") : "·"
            );
        }

        bool exactSelection = _selectedOptions.Count == _requiredSelectionCount;
        if (RandomStartButton != null)
            RandomStartButton.Disabled = _options.Count < _requiredSelectionCount;

        if (ConfirmButton != null)
        {
            ConfirmButton.Disabled = !exactSelection;
            ConfirmButton.Text = exactSelection
                ? I18n.Tr("ui.character_select.confirm", "确认并开始")
                : I18n.Format(
                    "ui.character_select.confirm_progress",
                    "确认并开始 {selected}/{required}",
                    ("selected", _selectedOptions.Count),
                    ("required", _requiredSelectionCount)
                );
        }

        TeamCount.Text = $"{_selectedOptions.Count:00} / {_requiredSelectionCount:00}";
        RefreshTeamSlots();
    }

    private void RefreshTeamSlots()
    {
        var container = TeamSlotsContainer;
        if (container == null)
            return;

        var prior = _displayedFormation.ToArray();
        _displayedFormation.Clear();
        ClearChildren(container);

        for (int i = 0; i < _requiredSelectionCount; i++)
        {
            CharacterOption option = i < _selectedOptions.Count ? _selectedOptions[i] : null;
            var slot = CreateTeamSlot(option, i);
            container.AddChild(slot);
            _displayedFormation.Add(option);
            if (i < prior.Length && prior[i] != option && IsVisibleInTree())
                RevealItem(slot, 0);
        }
    }

    private Button CreateTeamSlot(CharacterOption option, int index)
    {
        bool filled = option != null;
        bool focused = filled && option == _focusedOption;
        var slot = new Button
        {
            Name = $"TeamSlot{index + 1}",
            CustomMinimumSize = new Vector2(148, 60),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseDefaultCursorShape = filled ? CursorShape.PointingHand : CursorShape.Arrow,
            TooltipText = filled ? I18n.Tr("ui.character_select.remove_hint", "点击移出小队") : string.Empty,
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Stop,
            Disabled = !filled,
        };
        slot.AddThemeStyleboxOverride("normal", CreateTeamSlotStyleBox(filled, focused, 0.24f));
        slot.AddThemeStyleboxOverride("hover", CreateTeamSlotStyleBox(filled, true, 0.38f));
        slot.AddThemeStyleboxOverride("pressed", CreateTeamSlotStyleBox(filled, true, 0.44f));
        slot.AddThemeStyleboxOverride("disabled", CreateTeamSlotStyleBox(false, false, 0.16f));

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        margin.AddThemeConstantOverride("margin_left", SpaceMd);
        margin.AddThemeConstantOverride("margin_top", SpaceSm);
        margin.AddThemeConstantOverride("margin_right", SpaceMd);
        margin.AddThemeConstantOverride("margin_bottom", SpaceSm);
        slot.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        row.AddThemeConstantOverride("separation", SpaceMd);
        margin.AddChild(row);

        var portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(SpaceSm * 7, SpaceSm * 7),
            Texture = option?.Icon ?? option?.Portrait,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        row.AddChild(portrait);

        var textStack = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        textStack.AddThemeConstantOverride("separation", 0);
        row.AddChild(textStack);

        var positionLabel = new Label
        {
            Text = filled
                ? I18n.Format("ui.character_select.slot.filled", "#{index} 出战", ("index", index + 1))
                : I18n.Format("ui.character_select.slot.empty", "#{index} 空位", ("index", index + 1)),
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
        };
        positionLabel.AddThemeFontSizeOverride("font_size", 15);
        positionLabel.AddThemeColorOverride(
            "font_color",
            filled
                ? new Color(0.9f, 0.91f, 0.95f, 0.96f)
                : new Color(0.62f, 0.64f, 0.7f, 0.46f)
        );
        textStack.AddChild(positionLabel);

        var nameLabel = new Label
        {
            Text = filled
                ? option.Info.CharacterName
                : I18n.Tr("ui.character_select.slot.click_to_join", "点击角色加入"),
            MouseFilter = MouseFilterEnum.Ignore,
            ClipText = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        nameLabel.AddThemeColorOverride(
            "font_color",
            filled ? new Color(0.94f, 0.95f, 0.97f, 0.96f) : new Color(0.66f, 0.68f, 0.74f, 0.56f)
        );
        textStack.AddChild(nameLabel);

        if (filled)
        {
            slot.Pressed += () =>
            {
                RemoveFromTeam(option);
                RefreshSelectionState();
            };
        }

        return slot;
    }

    private void RemoveFromTeam(CharacterOption option)
    {
        if (option == null)
            return;

        int removedIndex = _selectedOptions.IndexOf(option);
        if (removedIndex < 0)
            return;

        _selectedOptions.RemoveAt(removedIndex);
    }

    private static StyleBoxFlat CreateTeamSlotStyleBox(bool filled, bool focused, float alpha)
    {
        Color borderColor = focused
            ? new Color(0.94f, 0.96f, 0.98f, 0.90f)
            : filled
                ? new Color(0.72f, 0.78f, 0.84f, 0.45f)
                : new Color(0.55f, 0.58f, 0.65f, 0.20f);

        return new StyleBoxFlat
        {
            BgColor = filled
                ? new Color(0.18f, 0.25f, 0.30f, alpha + 0.12f)
                : new Color(0.03f, 0.035f, 0.045f, 0.12f),
            BorderColor = borderColor,
            BorderWidthLeft = focused ? 2 : 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
        };
    }

    private void RefreshFocusedCharacterPreview(bool animate = true)
    {
        _previewTween?.Kill();
        if (_focusedOption == null)
        {
            DossierColumn.Visible = false;
            return;
        }

        DossierColumn.Visible = true;
        ApplyFocusedCharacterPreview(_focusedOption);
        AnimateDossier(animate && IsVisibleInTree());
    }

    private void ApplyFocusedCharacterPreview(CharacterOption option)
    {
        if (option == null)
            return;

        GetNode<Label>(DossierStackPath + "/Vitals/Life/Value").Text = option.Info.LifeMax.ToString();
        GetNode<Label>(DossierStackPath + "/Vitals/Power/Value").Text = option.Info.Power.ToString();
        GetNode<Label>(DossierStackPath + "/Vitals/Survival/Value").Text = option.Info.Survivability.ToString();
        HeroImage.Texture = option.Hero ?? option.Portrait;
        int number = _options.IndexOf(option) + 1;
        Callsign.Text = ResolveCharacterAssetKey(option.Info)?.ToUpperInvariant() ?? $"UNIT {number:00}";
        Folio.Text = $"FIELD ARCHIVE     /     {number:00}";
        GetNode<ScrollContainer>(DossierStackPath + "/DossierScroll").ScrollVertical = 0;

        if (TitleLabel != null)
            TitleLabel.Text = option.Info.CharacterName ?? I18n.Tr("ui.common.character", "Character");

        if (HintLabel != null)
        {
            string passiveTag = I18n.Tr("ui.common.passive_tag", "被动");
            string passiveName = string.IsNullOrWhiteSpace(option.Info.PassiveName)
                ? passiveTag
                : option.Info.PassiveName;
            string passiveDesc = string.IsNullOrWhiteSpace(option.Info.PassiveDescription)
                ? "-"
                : option.Info.PassiveDescription;
            PassiveTitle.Text = $"{passiveName}  /  {passiveTag}";
            HintLabel.Text = StripBbcode(passiveDesc);
        }
    }

    private static Texture2D LoadHeroTexture(PlayerInfoStructure info)
    {
        string characterKey = ResolveCharacterAssetKey(info);
        if (!string.IsNullOrWhiteSpace(characterKey)
            && CharacterHeroPaths.TryGetValue(characterKey, out string heroPath))
        {
            return PreloadeScene.GetTexture(heroPath) ?? GD.Load<Texture2D>(heroPath);
        }

        return null;
    }

    private static Texture2D LoadCharacterIcon(PlayerInfoStructure info)
    {
        string characterKey = ResolveCharacterAssetKey(info);
        if (!string.IsNullOrWhiteSpace(characterKey)
            && CharacterIconPaths.TryGetValue(characterKey, out string iconPath))
        {
            return PreloadeScene.GetTexture(iconPath) ?? GD.Load<Texture2D>(iconPath);
        }

        return null;
    }

    private static string ResolveCharacterAssetKey(PlayerInfoStructure info)
    {
        string scenePath = info.CharacterScenePath ?? string.Empty;
        foreach (string key in CharacterHeroPaths.Keys)
        {
            if (scenePath.Contains($"/{key}/", StringComparison.OrdinalIgnoreCase))
                return key;
        }

        string name = info.CharacterName ?? string.Empty;
        foreach (string key in CharacterHeroPaths.Keys)
        {
            if (string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                return key;
        }

        return null;
    }

    private static string StripBbcode(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return Regex.Replace(text, "\\[[^\\]]+\\]", string.Empty);
    }

    private void ConfirmSelection()
    {
        if (_isClosing || _selectedOptions.Count != _requiredSelectionCount)
            return;

        var selectedCharacters = new List<PlayerInfoStructure>(_requiredSelectionCount);
        for (int i = 0; i < _selectedOptions.Count; i++)
        {
            var option = _selectedOptions[i];
            if (option == null)
                continue;

            selectedCharacters.Add(
                ClonePlayerInfo(option.Info, GameInfo.GetDefaultPlayerFormationPosition(i))
            );
        }

        _ = ConfirmSelectionAsync(selectedCharacters.ToArray(), ResolveSeed(), _selectedDifficulty);
    }

    private void RandomizeSelectionAndStart()
    {
        if (_isClosing || _options.Count < _requiredSelectionCount)
            return;

        var shuffledOptions = new List<CharacterOption>(_options);
        for (int i = shuffledOptions.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Shared.Next(i + 1);
            (shuffledOptions[i], shuffledOptions[swapIndex]) =
                (shuffledOptions[swapIndex], shuffledOptions[i]);
        }

        _selectedOptions.Clear();
        _selectedOptions.AddRange(shuffledOptions.Take(_requiredSelectionCount));
        _focusedOption = _selectedOptions.FirstOrDefault();
        RefreshSelectionState();
        RefreshFocusedCharacterPreview(animate: false);
        ConfirmSelection();
    }

    private void Close()
    {
        if (_isClosing)
            return;

        if (!IsInsideTree())
        {
            QueueFree();
            return;
        }

        _ = CloseAsync();
    }

    private async Task ConfirmSelectionAsync(
        PlayerInfoStructure[] selectedCharacters,
        int seed,
        int difficulty
    )
    {
        if (_isClosing)
            return;

        _isClosing = true;
        _isAnimating = true;
        _overlayTween?.Kill();
        SetSelectionControlsEnabled(false);

        if (ConfirmButton != null)
            ConfirmButton.Text = I18n.Tr("ui.character_select.starting", "启动中...");

        if (GetTree() != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        UserSettings.SetLastSelectedDifficulty(difficulty);
        _confirmAction?.Invoke(selectedCharacters, seed, difficulty);
    }

    private async Task CloseAsync()
    {
        SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);
        if (transitionLayer != null)
        {
            _isClosing = true;
            _isAnimating = true;
            _overlayTween?.Kill();
            _previewTween?.Kill();
            SetSelectionControlsEnabled(false);
            HideDifficultyTooltip();

            await transitionLayer.FadeToBlackAsync(MaskTransitionDuration);

            if (GodotObject.IsInstanceValid(this))
                QueueFree();

            await transitionLayer.FadeFromBlackAsync(MaskTransitionDuration);
            return;
        }

        await PlayExitAnimationAsync();

        if (GodotObject.IsInstanceValid(this))
            QueueFree();
    }

    private async Task PlayEnterAnimationAsync()
    {
        _overlayTween?.Kill();
        _isAnimating = true;

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var shade = Shade;
        var panel = Panel;
        Vector2 panelRestPosition = panel?.Position ?? Vector2.Zero;

        SetCanvasItemAlpha(shade, 0f);
        if (panel != null)
        {
            SetCanvasItemAlpha(panel, 0f);
            panel.Position = panelRestPosition + new Vector2(0f, PanelEnterSlideOffset);
        }

        _overlayTween = CreateTween();
        _overlayTween.SetEase(Tween.EaseType.Out);
        _overlayTween.SetTrans(Tween.TransitionType.Cubic);

        if (shade != null)
            _overlayTween.TweenProperty(shade, "modulate:a", 1f, EnterAnimationDuration * 0.75f);

        if (panel != null)
        {
            _overlayTween
                .Parallel()
                .TweenProperty(panel, "modulate:a", 1f, EnterAnimationDuration);
            _overlayTween
                .Parallel()
                .TweenProperty(panel, "position:y", panelRestPosition.Y, EnterAnimationDuration);
        }

        await ToSignal(_overlayTween, Tween.SignalName.Finished);

        SetCanvasItemAlpha(shade, 1f);
        if (panel != null)
        {
            panel.Position = panelRestPosition;
            SetCanvasItemAlpha(panel, 1f);
        }

        _isAnimating = false;
    }

    private void ApplyOpenedVisualState()
    {
        _overlayTween?.Kill();
        SetCanvasItemAlpha(Shade, 1f);
        if (Panel != null)
            SetCanvasItemAlpha(Panel, 1f);
        _isAnimating = false;
    }

    private async Task PlayExitAnimationAsync()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        _isAnimating = true;
        _overlayTween?.Kill();

        var shade = Shade;
        var panel = Panel;
        Vector2 panelRestPosition = panel?.Position ?? Vector2.Zero;

        SetCanvasItemAlpha(shade, 1f);
        if (panel != null)
        {
            panel.Position = panelRestPosition;
            SetCanvasItemAlpha(panel, 1f);
        }

        _overlayTween = CreateTween();
        _overlayTween.SetEase(Tween.EaseType.In);
        _overlayTween.SetTrans(Tween.TransitionType.Cubic);

        if (shade != null)
            _overlayTween.TweenProperty(shade, "modulate:a", 0f, ExitAnimationDuration);

        if (panel != null)
        {
            _overlayTween
                .Parallel()
                .TweenProperty(panel, "modulate:a", 0f, ExitAnimationDuration);
            _overlayTween
                .Parallel()
                .TweenProperty(
                    panel,
                    "position:y",
                    panelRestPosition.Y + PanelExitSlideOffset,
                    ExitAnimationDuration
                );
        }

        await ToSignal(_overlayTween, Tween.SignalName.Finished);
        _isAnimating = false;
    }

    private static void SetCanvasItemAlpha(CanvasItem item, float alpha)
    {
        if (item == null)
            return;

        Color color = item.Modulate;
        color.A = alpha;
        item.Modulate = color;
    }

    private void SetSelectionControlsEnabled(bool enabled)
    {
        if (CancelButton != null)
            CancelButton.Disabled = !enabled;
        if (ConfirmButton != null)
            ConfirmButton.Disabled = !enabled;
        if (RandomStartButton != null)
            RandomStartButton.Disabled = !enabled || _options.Count < _requiredSelectionCount;
        if (DifficultyMinusButton != null)
            DifficultyMinusButton.Disabled = !enabled || _selectedDifficulty <= MinDifficulty;
        if (DifficultyPlusButton != null)
            DifficultyPlusButton.Disabled = !enabled || _selectedDifficulty >= MaxDifficulty;
        if (SeedInput != null)
            SeedInput.Editable = enabled;

        foreach (var option in _options)
        {
            if (option?.CardButton != null)
                option.CardButton.Disabled = !enabled;
        }
    }

    private void SetupDifficultyButton(Button button, int delta)
    {
        if (button == null)
            return;

        button.ActionMode = BaseButton.ActionModeEnum.Press;
        button.Pressed += () => AdjustDifficulty(delta);
    }

    private void AdjustDifficulty(int delta)
    {
        int nextDifficulty = Math.Clamp(
            _selectedDifficulty + delta,
            MinDifficulty,
            MaxDifficulty
        );
        if (nextDifficulty == _selectedDifficulty)
            return;

        _selectedDifficulty = nextDifficulty;
        RefreshDifficultyState();
        if (_difficultyTooltipVisible)
            ShowDifficultyTooltip();
    }

    private void RefreshDifficultyState()
    {
        if (DifficultyValueLabel != null)
            DifficultyValueLabel.Text = I18n.Format(
                "ui.common.difficulty_value",
                "难度 {value}",
                ("value", _selectedDifficulty)
            );

        if (DifficultyMinusButton != null)
            DifficultyMinusButton.Disabled = _selectedDifficulty <= MinDifficulty;

        if (DifficultyPlusButton != null)
            DifficultyPlusButton.Disabled = _selectedDifficulty >= MaxDifficulty;
    }

    private void LocalizeStaticTexts()
    {
        GetNode<Label>("SafeArea/Center/Panel/Margin/VBox/Header/TitleRow/Guide").Text =
            I18n.Tr("ui.character_select.formation_guide", "满队时，新干员替换当前查看的队员。点击下方出战位可移出。");
        GetNode<Label>(DossierStackPath + "/Vitals/Life/Caption").Text = I18n.Tr("ui.common.life", "生命");
        GetNode<Label>(DossierStackPath + "/Vitals/Power/Caption").Text = I18n.Tr("property.power", "力量");
        GetNode<Label>(DossierStackPath + "/Vitals/Survival/Caption").Text = I18n.Tr("property.survivability", "生存");
        if (TeamLabel != null)
            TeamLabel.Text = I18n.Tr("ui.character_select.team", "TEAM // 出战顺序");
        if (ScreenTitleLabel != null)
            ScreenTitleLabel.Text = I18n.Tr("ui.character_select.screen_title", "选择出击小队");
        if (RosterLabel != null)
            RosterLabel.Text = I18n.Tr("ui.character_select.roster", "ROSTER // 可选干员");
        if (DossierLabel != null)
            DossierLabel.Text = I18n.Tr("ui.character_select.dossier", "DOSSIER // 干员档案");
        if (SeedLabel != null)
            SeedLabel.Text = I18n.Tr("ui.character_select.seed", "SEED");
        if (SeedInput != null)
        {
            SeedInput.PlaceholderText = I18n.Tr(
                "ui.character_select.seed_placeholder",
                "empty = random, 0-999999999"
            );
        }
        if (DifficultyLabel != null)
            DifficultyLabel.Text = I18n.Tr("ui.character_select.difficulty", "DIFFICULTY");
        if (CancelButton != null)
            CancelButton.Text = I18n.Tr("ui.common.cancel", "返回");
        if (RandomStartButton != null)
            RandomStartButton.Text = I18n.Tr("ui.character_select.random_start", "随机并开始");
        if (EyebrowLabel != null)
            EyebrowLabel.Text = I18n.Tr("ui.character_select.eyebrow", "SQUAD ASSEMBLY // 编队组建");
        if (HintLabel != null)
            HintLabel.Text = I18n.Tr("ui.common.passive", "被动");
    }

    private void ShowDifficultyTooltip()
    {
        var tip = DifficultyTooltip;
        if (tip == null)
            return;

        _difficultyTooltipVisible = true;
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);
        tip.MinContentWidth = 360f;
        tip.SetText(GameInfo.BuildDifficultyTooltipText(_selectedDifficulty));
    }

    private void HideDifficultyTooltip()
    {
        _difficultyTooltipVisible = false;
        _difficultyTooltip?.HideTooltip();
    }

    private void WireDifficultyTooltipTarget(Control control)
    {
        if (control == null)
            return;

        control.MouseEntered += ShowDifficultyTooltip;
        control.MouseExited += QueueHideDifficultyTooltipIfNeeded;
        control.FocusEntered += ShowDifficultyTooltip;
        control.FocusExited += QueueHideDifficultyTooltipIfNeeded;
        control.TreeExiting += HideDifficultyTooltip;
    }

    private void QueueHideDifficultyTooltipIfNeeded()
    {
        CallDeferred(nameof(HideDifficultyTooltipIfCursorLeft));
    }

    private void HideDifficultyTooltipIfCursorLeft()
    {
        if (!_difficultyTooltipVisible)
            return;

        if (
            DifficultyBox != null
            && GodotObject.IsInstanceValid(DifficultyBox)
            && DifficultyBox.GetGlobalRect().HasPoint(GetViewport().GetMousePosition())
        )
        {
            return;
        }

        HideDifficultyTooltip();
    }

    private Tip EnsureDifficultyTooltip()
    {
        var tree = GetTree();
        var root = tree?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        var tip = layer.GetNodeOrNull<Tip>("Tip");
        if (tip != null)
            return tip;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        tip = tipScene?.Instantiate<Tip>();
        if (tip == null)
            return null;

        tip.Name = "Tip";
        tip.FollowMouse = true;
        layer.AddChild(tip);
        return tip;
    }

    private int ResolveSeed()
    {
        string text = SanitizeSeedText(SeedInput?.Text);
        if (string.IsNullOrWhiteSpace(text))
            return GenerateRandomSeed();

        return int.TryParse(text, out int numericSeed) ? numericSeed : MaxSeedValue;
    }

    private static int GenerateRandomSeed()
    {
        int seed = Random.Shared.Next(0, MaxSeedValue + 1);
        return seed == 0 ? 1 : seed;
    }

    private void NormalizeSeedInput(string text)
    {
        if (_normalizingSeedInput || SeedInput == null)
            return;

        string sanitized = SanitizeSeedText(text);
        if (string.Equals(text, sanitized, StringComparison.Ordinal))
            return;

        _normalizingSeedInput = true;
        SeedInput.Text = sanitized;
        SeedInput.CaretColumn = sanitized.Length;
        _normalizingSeedInput = false;
    }

    private static string SanitizeSeedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var digits = new char[Math.Min(text.Length, MaxSeedDigits)];
        int count = 0;
        for (int i = 0; i < text.Length && count < MaxSeedDigits; i++)
        {
            if (char.IsDigit(text[i]))
                digits[count++] = text[i];
        }

        if (count == 0)
            return string.Empty;

        string seedText = new(digits, 0, count);
        return long.TryParse(seedText, out long value) && value > MaxSeedValue
            ? MaxSeedValue.ToString()
            : seedText;
    }

    private static PlayerInfoStructure ClonePlayerInfo(PlayerInfoStructure source, int positionIndex)
    {
        return new PlayerInfoStructure
        {
            CharacterScenePath = source.CharacterScenePath,
            Life = source.LifeInitialized ? source.Life : source.LifeMax,
            LifeMax = source.LifeMax,
            LifeInitialized = true,
            Power = source.Power,
            Survivability = source.Survivability,
            TalentPoints = source.TalentPoints,
            TalentOffer = source.TalentOffer != null ? new List<string>(source.TalentOffer) : new List<string>(),
            UnlockedTalents = source.UnlockedTalents != null
                ? new List<string>(source.UnlockedTalents)
                : new List<string>(),
            AppliedTalentMaxLifeBonus = source.AppliedTalentMaxLifeBonus,
            GainedSkills = source.GainedSkills != null
                ? new List<SkillID>(source.GainedSkills)
                : new List<SkillID>(),
            TakenSkills = source.TakenSkills?.ToArray() ?? new SkillID[3],
            AllSkills = source.AllSkills?.ToArray(),
            PositionIndex = positionIndex,
            PortaitPath = source.PortaitPath,
            CharacterName = source.CharacterName,
            PassiveName = source.PassiveName,
            PassiveDescription = source.PassiveDescription,
        };
    }

    private static string FormatPassiveDescription(string description)
    {
        string text = string.IsNullOrWhiteSpace(description) ? "-" : description;
        text = GlobalFunction.ColorizeNumbers(text);
        text = GlobalFunction.ColorizeKeywords(text);
        return text;
    }

    private static void ClearChildren(Node parent)
    {
        if (parent == null)
            return;

        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private static void IgnoreChildMouseFilters(Node parent)
    {
        if (parent == null)
            return;

        foreach (Node child in parent.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = MouseFilterEnum.Ignore;

            IgnoreChildMouseFilters(child);
        }
    }

    private void ApplyCardVisualState(CharacterOption option, bool? selectedOverride = null)
    {
        if (option == null)
            return;

        bool selected = selectedOverride ?? _selectedOptions.Contains(option);
        bool focused = option == _focusedOption;
        ApplyCardStyle(option.CardButton, selected, focused, option.Hovered);
    }

    private static void ApplyCardStyle(Button card, bool selected, bool focused, bool hovered)
    {
        if (card == null)
            return;

        card.Modulate = hovered
            ? new Color(1.08f, 1.08f, 1.08f, 1f)
            : Colors.White;
        card.Scale = Vector2.One;
        card.ZIndex = hovered ? 8 : 0;

        card.AddThemeStyleboxOverride("normal", CreateCardStyleBox(selected, focused, hovered, 0.22f));
        card.AddThemeStyleboxOverride("hover", CreateCardStyleBox(selected, true, true, 0.38f));
        card.AddThemeStyleboxOverride("pressed", CreateCardStyleBox(selected, true, true, 0.50f));
        card.AddThemeStyleboxOverride("focus", CreateCardStyleBox(selected, true, true, 0.38f));
    }

    private static StyleBoxFlat CreateCardStyleBox(bool selected, bool focused, bool hovered, float alpha)
    {
        Color borderColor = selected
            ? new Color(0.68f, 0.85f, 0.90f, focused || hovered ? 1f : 0.56f)
            : focused || hovered
                ? new Color(0.84f, 0.88f, 0.94f, 0.80f)
                : new Color(0.62f, 0.66f, 0.72f, 0.25f);

        return new StyleBoxFlat
        {
            BgColor = selected
                ? new Color(0.17f, 0.22f, 0.27f, focused ? 0.98f : 0.64f)
                : focused || hovered
                    ? new Color(0.10f, 0.12f, 0.16f, 0.26f)
                    : new Color(0.10f, 0.13f, 0.16f, 0.64f),
            BorderColor = borderColor,
            BorderWidthLeft = focused ? 4 : selected ? 2 : 0,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
            ShadowColor = new Color(0f, 0f, 0f, 0f),
            ShadowSize = 0,
        };
    }
}
