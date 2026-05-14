using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class CharacterSelectionOverlay : Control
{
    private const int DefaultRequiredSelectionCount = 4;
    private const int MinDifficulty = 0;
    private const int MaxDifficulty = 5;
    private const int MaxSeedDigits = 9;
    private const int MaxSeedValue = 999_999_999;
    private const float EnterAnimationDuration = 0.24f;
    private const float ExitAnimationDuration = 0.18f;
    private const float PanelEnterSlideOffset = 28f;
    private const float PanelExitSlideOffset = 18f;

    private sealed class CharacterOption
    {
        public PlayerInfoStructure Info;
        public Texture2D Portrait;
        public Button CardButton;
        public Label SelectedBadge;
        public bool Hovered;
    }

    private readonly List<CharacterOption> _options = new();
    private readonly List<CharacterOption> _selectedOptions = new();

    private Action<PlayerInfoStructure[], int, int> _confirmAction;
    private int _requiredSelectionCount = DefaultRequiredSelectionCount;
    private int _selectedDifficulty = MinDifficulty;
    private bool _openRequested;
    private bool _isAnimating;
    private bool _isClosing;
    private bool _normalizingSeedInput;
    private bool _difficultyTooltipVisible;
    private Tween _overlayTween;
    private Tip _difficultyTooltip;

    private ColorRect Shade => field ??= GetNodeOrNull<ColorRect>("Shade");
    private Control Panel => field ??= GetNodeOrNull<Control>("SafeArea/Center/Panel");
    private Label StatusLabel => field ??= GetNodeOrNull<Label>("SafeArea/Center/Panel/Margin/VBox/StatusLabel");
    private HFlowContainer CardGrid =>
        field ??= GetNodeOrNull<HFlowContainer>("SafeArea/Center/Panel/Margin/VBox/CardGrid");
    private Button ConfirmButton =>
        field ??= GetNodeOrNull<Button>("SafeArea/Center/Panel/Margin/VBox/Footer/ConfirmButton");
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
    private Tip DifficultyTooltip => _difficultyTooltip ??= EnsureDifficultyTooltip();
    private LineEdit SeedInput =>
        field ??= GetNodeOrNull<LineEdit>("SafeArea/Center/Panel/Margin/VBox/Footer/SeedBox/SeedInput");
    private Button CardTemplate =>
        field ??= GetNodeOrNull<Button>("Templates/CharacterCardTemplate");

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ProcessMode = ProcessModeEnum.Always;

        if (CardTemplate != null)
            CardTemplate.Visible = false;

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
        HideDifficultyTooltip();
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
        Action<PlayerInfoStructure[], int, int> confirmAction
    )
    {
        _openRequested = true;
        _confirmAction = confirmAction;
        _selectedDifficulty = MinDifficulty;
        _requiredSelectionCount = Math.Max(
            1,
            Math.Min(requiredSelectionCount, availableCharacters?.Count ?? DefaultRequiredSelectionCount)
        );

        PopulateCharacters(availableCharacters ?? Array.Empty<PlayerInfoStructure>());
        RefreshDifficultyState();
        _isClosing = false;
        Visible = true;
        Modulate = Colors.White;
        _ = PlayEnterAnimationAsync();
    }

    private void PopulateCharacters(IReadOnlyList<PlayerInfoStructure> availableCharacters)
    {
        ClearChildren(CardGrid);
        _options.Clear();
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

            option.CardButton = CreateCharacterCard(option);
            if (option.CardButton == null)
                continue;

            _options.Add(option);
            CardGrid?.AddChild(option.CardButton);
        }

        for (int i = 0; i < Math.Min(_requiredSelectionCount, _options.Count); i++)
            _selectedOptions.Add(_options[i]);

        RefreshSelectionState();
    }

    private Button CreateCharacterCard(CharacterOption option)
    {
        if (CardTemplate == null || option == null)
            return null;

        var card = CardTemplate.Duplicate() as Button;
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
        IgnoreChildMouseFilters(card);

        var portrait = card.GetNodeOrNull<TextureRect>("Content/Stack/PortraitFrame/PortraitMargin/Portrait");
        var nameLabel = card.GetNodeOrNull<Label>("Content/Stack/NameLabel");
        var statsLabel = card.GetNodeOrNull<Label>("Content/Stack/StatsLabel");
        var passiveNameLabel = card.GetNodeOrNull<Label>("Content/Stack/PassiveNameLabel");
        var passiveDescriptionLabel =
            card.GetNodeOrNull<RichTextLabel>("Content/Stack/PassiveDescriptionLabel");

        if (portrait != null)
            portrait.Texture = option.Portrait;
        if (nameLabel != null)
            nameLabel.Text = option.Info.CharacterName;
        if (statsLabel != null)
        {
            statsLabel.Text =
                $"生命 {option.Info.LifeMax}   力量 {option.Info.Power}   生存 {option.Info.Survivability}   速度 {option.Info.Speed}";
        }
        if (passiveNameLabel != null)
            passiveNameLabel.Text = option.Info.PassiveName;
        if (passiveDescriptionLabel != null)
            passiveDescriptionLabel.Text = FormatPassiveDescription(option.Info.PassiveDescription);

        option.SelectedBadge = card.GetNodeOrNull<Label>("SelectedBadge");
        ApplyCardVisualState(option);
        card.MouseEntered += () =>
        {
            option.Hovered = true;
            ApplyCardVisualState(option);
        };
        card.MouseExited += () =>
        {
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

        if (_selectedOptions.Remove(option))
        {
            RefreshSelectionState();
            return;
        }

        if (_selectedOptions.Count >= _requiredSelectionCount)
        {
            UpdateStatusText(selectionFull: true);
            return;
        }

        _selectedOptions.Add(option);
        RefreshSelectionState();
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
            ApplyCardVisualState(option, selected);

            int selectionIndex = _selectedOptions.IndexOf(option);
            if (option.SelectedBadge != null)
            {
                option.SelectedBadge.Visible = selected;
                option.SelectedBadge.Text = selected ? $"已选 #{selectionIndex + 1}" : string.Empty;
            }
        }

        bool exactSelection = _selectedOptions.Count == _requiredSelectionCount;
        if (ConfirmButton != null)
            ConfirmButton.Disabled = !exactSelection;

        UpdateStatusText(selectionFull: false);
    }

    private void UpdateStatusText(bool selectionFull)
    {
        if (StatusLabel == null)
            return;

        string selectionText;
        if (selectionFull)
        {
            selectionText =
                $"已选满 {_requiredSelectionCount}/{_requiredSelectionCount}，如需更换请先取消一名角色。";
        }
        else
        {
            var _selectedCharacterNames = _selectedOptions;
            selectionText = _selectedOptions.Count == _requiredSelectionCount
                ? $"已选择 {_selectedCharacterNames.Count}/{_requiredSelectionCount}。确认后将以当前顺序建立队伍。"
                : $"已选择 {_selectedCharacterNames.Count}/{_requiredSelectionCount}。还需要补满角色才能开始。";
        }

        StatusLabel.Text = selectionText;
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

            selectedCharacters.Add(ClonePlayerInfo(option.Info, i + 1));
        }

        _ = ConfirmSelectionAsync(selectedCharacters.ToArray(), ResolveSeed(), _selectedDifficulty);
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
            ConfirmButton.Text = "启动中...";

        if (GetTree() != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _confirmAction?.Invoke(selectedCharacters, seed, difficulty);
    }

    private async Task CloseAsync()
    {
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
        UpdateStatusText(selectionFull: false);
    }

    private void RefreshDifficultyState()
    {
        if (DifficultyValueLabel != null)
            DifficultyValueLabel.Text = $"难度 {_selectedDifficulty}";

        if (DifficultyMinusButton != null)
            DifficultyMinusButton.Disabled = _selectedDifficulty <= MinDifficulty;

        if (DifficultyPlusButton != null)
            DifficultyPlusButton.Disabled = _selectedDifficulty >= MaxDifficulty;
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
            LifeMax = source.LifeMax,
            Power = source.Power,
            Survivability = source.Survivability,
            Speed = source.Speed,
            TalentPoints = source.TalentPoints,
            UnlockedTalents = source.UnlockedTalents != null
                ? new List<string>(source.UnlockedTalents)
                : new List<string>(),
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
            child.QueueFree();
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

        bool selected =
            selectedOverride
            ?? _selectedOptions.Contains(option);
        ApplyCardStyle(option.CardButton, selected, option.Hovered);
    }

    private static void ApplyCardStyle(Button card, bool selected, bool hovered)
    {
        if (card == null)
            return;

        card.Modulate = hovered
            ? new Color(1.08f, 1.08f, 1.08f, 1f)
            : Colors.White;
        card.Scale = Vector2.One;
        card.ZIndex = hovered ? 8 : 0;

        card.AddThemeStyleboxOverride("normal", CreateCardStyleBox(selected, hovered, 0.22f));
        card.AddThemeStyleboxOverride("hover", CreateCardStyleBox(selected, true, 0.38f));
        card.AddThemeStyleboxOverride("pressed", CreateCardStyleBox(selected, true, 0.50f));
        card.AddThemeStyleboxOverride("focus", CreateCardStyleBox(selected, true, 0.38f));
    }

    private static StyleBoxFlat CreateCardStyleBox(bool selected, bool hovered, float alpha)
    {
        Color borderColor = selected
            ? new Color(1.00f, 0.88f, 0.56f, hovered ? 1.00f : 0.90f)
            : hovered
                ? new Color(0.78f, 0.93f, 1.00f, 0.92f)
                : new Color(0.70f, 0.80f, 0.92f, 0.24f);

        return new StyleBoxFlat
        {
            BgColor = selected
                ? new Color(0.16f, 0.22f, 0.31f, alpha + (hovered ? 0.30f : 0.22f))
                : new Color(0.07f, 0.10f, 0.14f, alpha + (hovered ? 0.24f : 0.0f)),
            BorderColor = borderColor,
            BorderWidthLeft = selected || hovered ? 4 : 1,
            BorderWidthTop = selected || hovered ? 4 : 1,
            BorderWidthRight = selected || hovered ? 4 : 1,
            BorderWidthBottom = selected || hovered ? 4 : 1,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ContentMarginLeft = 0,
            ContentMarginTop = 0,
            ContentMarginRight = 0,
            ContentMarginBottom = 0,
            ShadowColor = hovered
                ? new Color(0.72f, 0.86f, 1.00f, selected ? 0.28f : 0.20f)
                : new Color(0f, 0f, 0f, 0.20f),
            ShadowSize = hovered ? 20 : 10,
        };
    }
}
