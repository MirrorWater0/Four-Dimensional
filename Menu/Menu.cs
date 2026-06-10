using System;
using Godot;

public partial class Menu : Control
{
    private const float OpenDuration = 0.22f;
    private const float CloseDuration = 0.18f;
    private const float MainPanelOffsetLeft = -222f;
    private const float MainPanelOffsetRight = 214f;
    private const float SettingsPanelOffsetLeft = -470f;
    private const float SettingsPanelOffsetRight = 470f;
    private static readonly Vector2 ClosedPanelScale = new(0.92f, 0.92f);
    private static readonly Vector2I[] ResolutionOptions =
    {
        new(1280, 720),
        new(1366, 768),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
    };
    private static readonly PackedScene EncyclopediaScene = GD.Load<PackedScene>(
        "res://Menu/Encyclopedia.tscn"
    );

    private ColorRect Backdrop => field ??= GetNodeOrNull<ColorRect>("Backdrop");
    private ColorRect GlowTopRight => field ??= GetNodeOrNull<ColorRect>("GlowTopRight");
    private ColorRect GlowBottomLeft => field ??= GetNodeOrNull<ColorRect>("GlowBottomLeft");
    private Control CenterPanel => field ??= GetNodeOrNull<Control>("CenterPanel");
    private Label TagLabel => field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/Tag");
    private Label TitleLabel => field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/Title");
    private Label DescriptionLabel =>
        field ??= GetNodeOrNull<Label>("CenterPanel/Margin/VBox/Description");
    private Button SaveQuitButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/SaveQuitButton");
    private Button AbandonGameButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/AbandonGameButton");
    private Button ReturnButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/ReturnButton");
    private Button EncyclopediaButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/EncyclopediaButton");
    private Button SettingsButton =>
        field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/SettingsButton");
    private Control MainButtons =>
        field ??= GetNodeOrNull<Control>("CenterPanel/Margin/VBox/Buttons");
    private Control SettingsPanel =>
        field ??= GetNodeOrNull<Control>("CenterPanel/Margin/VBox/SettingsPanel");
    private Label SettingsTitle =>
        field ??= GetSettingsPanelNode<Label>("SettingsTitle");
    private CheckBox DescriptionModeCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("DescriptionModeCheckBox");
    private CheckBox TurnOrderPreviewCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("TurnOrderPreviewCheckBox");
    private CheckBox IncomingDamagePreviewCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("IncomingDamagePreviewCheckBox");
    private CheckBox IntentionTargetNamesCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("IntentionTargetNamesCheckBox");
    private CheckBox SingleTargetDamageIntentionArrowsCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("SingleTargetDamageIntentionArrowsCheckBox");
    private CheckBox HideEnemySkillsCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("HideEnemySkillsCheckBox");
    private CheckBox GroupBattlePilesByCharacterCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("GroupBattlePilesByCharacterCheckBox");
    private CheckBox KeepManualTargetCardVisibleCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("KeepManualTargetCardVisibleCheckBox");
    private CheckBox ArrowManualTargetSelectionCheckBox =>
        field ??= GetSettingsPanelNode<CheckBox>("ArrowManualTargetSelectionCheckBox");
    private SettingsDropdown TextSizeOptionButton =>
        field ??= GetSettingsPanelNode<SettingsDropdown>("TextSizeOptionButton");
    private SettingsDropdown BattleShakeOptionButton =>
        field ??= GetSettingsPanelNode<SettingsDropdown>("BattleShakeOptionButton");
    private Label LanguageLabel =>
        field ??= GetSettingsPanelNode<Label>("LanguageLabel");
    private SettingsDropdown LanguageOptionButton =>
        field ??= GetSettingsPanelNode<SettingsDropdown>("LanguageOptionButton");
    private Label ResolutionLabel =>
        field ??= GetSettingsPanelNode<Label>("ResolutionLabel");
    private SettingsDropdown ResolutionOptionButton =>
        field ??= GetSettingsPanelNode<SettingsDropdown>("ResolutionOptionButton");
    private Label TextSizeLabel =>
        field ??= GetSettingsPanelNode<Label>("TextSizeLabel");
    private Label BattleShakeLabel =>
        field ??= GetSettingsPanelNode<Label>("BattleShakeLabel");
    private Label MasterVolumeLabel =>
        field ??= GetSettingsPanelNode<Label>("MasterVolumeLabel");
    private Label SfxVolumeLabel =>
        field ??= GetSettingsPanelNode<Label>("SfxVolumeLabel");
    private HSlider MasterVolumeSlider =>
        field ??= GetSettingsPanelNode<HSlider>("MasterVolumeSlider");
    private Label MasterVolumeValueLabel =>
        field ??= GetSettingsPanelNode<Label>("MasterVolumeValueLabel");
    private HSlider SfxVolumeSlider =>
        field ??= GetSettingsPanelNode<HSlider>("SfxVolumeSlider");
    private Label SfxVolumeValueLabel =>
        field ??= GetSettingsPanelNode<Label>("SfxVolumeValueLabel");
    private Button SettingsBackButton =>
        field ??= GetSettingsPanelNode<Button>("SettingsBackButton");
    private Tween _transitionTween;
    private bool _isAbandoningGame;

    private T GetSettingsPanelNode<T>(string nodeName) where T : Node
    {
        return SettingsPanel?.FindChild(nodeName, recursive: true, owned: false) as T;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetDeferred(Control.PropertyName.PivotOffset, Size * 0.5f);
        if (CenterPanel != null)
            CenterPanel.PivotOffset = CenterPanel.Size * 0.5f;

        ApplyHiddenState();
        Visible = false;

        if (SaveQuitButton != null)
            SaveQuitButton.Pressed += OnSaveQuitPressed;

        if (AbandonGameButton != null)
            AbandonGameButton.Pressed += OnAbandonGamePressed;

        if (ReturnButton != null)
            ReturnButton.Pressed += OnReturnPressed;

        if (EncyclopediaButton != null)
            EncyclopediaButton.Pressed += OnEncyclopediaPressed;

        if (SettingsButton != null)
            SettingsButton.Pressed += OnSettingsPressed;

        if (DescriptionModeCheckBox != null)
            DescriptionModeCheckBox.Pressed += OnDescriptionModePressed;

        if (TurnOrderPreviewCheckBox != null)
            TurnOrderPreviewCheckBox.Visible = false;

        if (IncomingDamagePreviewCheckBox != null)
            IncomingDamagePreviewCheckBox.Pressed += OnIncomingDamagePreviewPressed;

        if (IntentionTargetNamesCheckBox != null)
            IntentionTargetNamesCheckBox.Pressed += OnIntentionTargetNamesPressed;

        if (SingleTargetDamageIntentionArrowsCheckBox != null)
            SingleTargetDamageIntentionArrowsCheckBox.Pressed +=
                OnSingleTargetDamageIntentionArrowsPressed;

        if (HideEnemySkillsCheckBox != null)
            HideEnemySkillsCheckBox.Pressed += OnHideEnemySkillsPressed;

        if (GroupBattlePilesByCharacterCheckBox != null)
            GroupBattlePilesByCharacterCheckBox.Pressed += OnGroupBattlePilesByCharacterPressed;

        if (KeepManualTargetCardVisibleCheckBox != null)
            KeepManualTargetCardVisibleCheckBox.Pressed += OnKeepManualTargetCardVisiblePressed;

        if (ArrowManualTargetSelectionCheckBox != null)
            ArrowManualTargetSelectionCheckBox.Pressed += OnArrowManualTargetSelectionPressed;

        ConfigureTextSizeOptionButton();
        if (TextSizeOptionButton != null)
            TextSizeOptionButton.ItemSelected += OnTextSizeSelected;
        ConfigureBattleShakeOptionButton();
        if (BattleShakeOptionButton != null)
            BattleShakeOptionButton.ItemSelected += OnBattleShakeSelected;
        ConfigureLanguageOptionButton();
        if (LanguageOptionButton != null)
            LanguageOptionButton.ItemSelected += OnLanguageSelected;
        ConfigureResolutionOptionButton();
        if (ResolutionOptionButton != null)
            ResolutionOptionButton.ItemSelected += OnResolutionSelected;
        ConfigureVolumeSlider(MasterVolumeSlider);
        if (MasterVolumeSlider != null)
            MasterVolumeSlider.ValueChanged += OnMasterVolumeChanged;
        ConfigureVolumeSlider(SfxVolumeSlider);
        if (SfxVolumeSlider != null)
            SfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;

        if (SettingsBackButton != null)
            SettingsBackButton.Pressed += ShowMainPanel;

        RefreshSettingsPanel();
    }

    public void Toggle()
    {
        if (Visible)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        _transitionTween?.Kill();
        Visible = true;
        FindActiveBattle(GetTree()?.Root)?.SetIncomingDamagePreviewSuppressed(true);
        if (CenterPanel != null)
            CenterPanel.PivotOffset = CenterPanel.Size * 0.5f;
        ShowMainPanel();

        _transitionTween = CreateTween();
        _transitionTween.SetParallel(true);
        _transitionTween.TweenProperty(this, "modulate:a", 1.0f, OpenDuration);

        if (Backdrop != null)
            _transitionTween.TweenProperty(Backdrop, "modulate:a", 1.0f, OpenDuration);
        if (GlowTopRight != null)
            _transitionTween.TweenProperty(GlowTopRight, "modulate:a", 1.0f, OpenDuration);
        if (GlowBottomLeft != null)
            _transitionTween.TweenProperty(GlowBottomLeft, "modulate:a", 1.0f, OpenDuration);
        if (CenterPanel != null)
        {
            _transitionTween
                .TweenProperty(CenterPanel, "scale", Vector2.One, OpenDuration)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
            _transitionTween
                .TweenProperty(CenterPanel, "modulate:a", 1.0f, OpenDuration - 0.02f)
                .SetEase(Tween.EaseType.Out);
        }
    }

    public void Close()
    {
        _transitionTween?.Kill();
        CloseAllDropdowns();

        _transitionTween = CreateTween();
        _transitionTween.SetParallel(true);
        _transitionTween.TweenProperty(this, "modulate:a", 0.0f, CloseDuration);

        if (Backdrop != null)
            _transitionTween.TweenProperty(Backdrop, "modulate:a", 0.0f, CloseDuration);
        if (GlowTopRight != null)
            _transitionTween.TweenProperty(GlowTopRight, "modulate:a", 0.0f, CloseDuration);
        if (GlowBottomLeft != null)
            _transitionTween.TweenProperty(GlowBottomLeft, "modulate:a", 0.0f, CloseDuration);
        if (CenterPanel != null)
        {
            _transitionTween
                .TweenProperty(CenterPanel, "scale", ClosedPanelScale, CloseDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.In);
            _transitionTween
                .TweenProperty(CenterPanel, "modulate:a", 0.0f, CloseDuration - 0.02f)
                .SetEase(Tween.EaseType.In);
        }

        _transitionTween.Finished += () =>
        {
            ApplyHiddenState();
            Visible = false;
            FindActiveBattle(GetTree()?.Root)?.SetIncomingDamagePreviewSuppressed(false);
        };
    }

    private void OnSaveQuitPressed()
    {
        AbortActiveBattle();
        SceneTransitionLayer.Ensure(this)?.SwitchScene("res://BeginGame/StartInterface.tscn");
    }

    private async void OnAbandonGamePressed()
    {
        if (_isAbandoningGame)
            return;

        _isAbandoningGame = true;
        if (AbandonGameButton != null)
            AbandonGameButton.Disabled = true;

        var activeBattle = FindActiveBattle(GetTree()?.Root);

        Close();
        activeBattle?.AbortBattle(unlockMapNodes: false);
        GameInfo.RecordCurrentRunHistory(victory: false, includeCurrentNode: true);
        GameOverSummary.Show(this);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        SaveSystem.SaveAllInBackground();
    }

    private void OnReturnPressed()
    {
        if (SettingsPanel?.Visible == true)
        {
            ShowMainPanel();
            return;
        }

        Close();
    }

    private void OnEncyclopediaPressed()
    {
        var encyclopedia = EncyclopediaScene.Instantiate<Encyclopedia>();
        GetParent()?.AddChild(encyclopedia);
        Close();
    }

    private void OnSettingsPressed()
    {
        ShowSettingsPanel();
    }

    private void OnDescriptionModePressed()
    {
        if (DescriptionModeCheckBox == null)
            return;

        UserSettings.SetCompactBattleCardDescriptions(DescriptionModeCheckBox.ButtonPressed);

        var activeBattle = FindActiveBattle(GetTree()?.Root);
        activeBattle?.RefreshBattleCardDescriptionModeFromSettings();
    }

    private void OnTurnOrderPreviewPressed()
    {
        if (TurnOrderPreviewCheckBox == null)
            return;

        UserSettings.SetBattleTurnOrderPreview(TurnOrderPreviewCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshTurnOrderPreviewFromSettings();
    }

    private void OnIncomingDamagePreviewPressed()
    {
        if (IncomingDamagePreviewCheckBox == null)
            return;

        UserSettings.SetIncomingDamagePreview(IncomingDamagePreviewCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshIncomingDamagePreviewFromSettings();
    }

    private void OnIntentionTargetNamesPressed()
    {
        if (IntentionTargetNamesCheckBox == null)
            return;

        UserSettings.SetShowIntentionTargetNames(IntentionTargetNamesCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshEnemyIntentionPreviews();
    }

    private void OnSingleTargetDamageIntentionArrowsPressed()
    {
        if (SingleTargetDamageIntentionArrowsCheckBox == null)
            return;

        UserSettings.SetShowSingleTargetDamageIntentionArrows(
            SingleTargetDamageIntentionArrowsCheckBox.ButtonPressed
        );
        FindActiveBattle(GetTree()?.Root)?.RefreshEnemyIntentionPreviews();
    }

    private void OnHideEnemySkillsPressed()
    {
        if (HideEnemySkillsCheckBox == null)
            return;

        UserSettings.SetHideEnemySkills(HideEnemySkillsCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshEnemySkillVisibilityFromSettings();
    }

    private void OnGroupBattlePilesByCharacterPressed()
    {
        if (GroupBattlePilesByCharacterCheckBox == null)
            return;

        UserSettings.SetGroupBattlePilesByCharacter(
            GroupBattlePilesByCharacterCheckBox.ButtonPressed
        );
    }

    private void OnKeepManualTargetCardVisiblePressed()
    {
        if (KeepManualTargetCardVisibleCheckBox == null)
            return;

        UserSettings.SetKeepManualTargetCardVisibleWhenHidden(
            KeepManualTargetCardVisibleCheckBox.ButtonPressed
        );
        FindActiveBattle(GetTree()?.Root)?.RefreshManualTargetCardVisibilityFromSettings();
    }

    private void OnArrowManualTargetSelectionPressed()
    {
        if (ArrowManualTargetSelectionCheckBox == null)
            return;

        UserSettings.SetUseArrowManualTargetSelection(
            ArrowManualTargetSelectionCheckBox.ButtonPressed
        );
    }

    private void OnTextSizeSelected(long index)
    {
        if (TextSizeOptionButton == null)
            return;

        int selectedIndex = (int)index;
        int level = selectedIndex >= 0 && selectedIndex < TextSizeOptionButton.ItemCount
            ? TextSizeOptionButton.GetItemId(selectedIndex)
            : UserSettings.TextSizeLevelStandard;
        UserSettings.SetTextSizeLevel(level);
        FindActiveBattle(GetTree()?.Root)?.RefreshTextSizeFromSettings();
        RefreshTextSizeForTree(GetTree()?.Root);
    }

    private void OnBattleShakeSelected(long index)
    {
        if (BattleShakeOptionButton == null)
            return;

        int selectedIndex = (int)index;
        int level = selectedIndex >= 0 && selectedIndex < BattleShakeOptionButton.ItemCount
            ? BattleShakeOptionButton.GetItemId(selectedIndex)
            : UserSettings.BattleShakeLevelStandard;
        UserSettings.SetBattleShakeLevel(level);
    }

    private void OnLanguageSelected(long index)
    {
        if (LanguageOptionButton == null)
            return;

        int selectedIndex = (int)index;
        string locale =
            selectedIndex >= 0 && selectedIndex < LanguageOptionButton.ItemCount
                ? LanguageOptionButton.GetItemMetadata(selectedIndex).AsString()
                : "zh_CN";
        locale = UserSettings.NormalizeLocale(locale);
        UserSettings.SetLocale(locale);
        I18n.SetLocale(locale);
        RefreshLocalizedSettingsText();
        RefreshSettingsPanel();
    }

    private void OnMasterVolumeChanged(double value)
    {
        int volume = UserSettings.NormalizeVolumePercent(Mathf.RoundToInt((float)value));
        UserSettings.SetMasterVolumePercent(volume);
        SetVolumeLabel(MasterVolumeValueLabel, volume);
    }

    private void OnResolutionSelected(long index)
    {
        if (ResolutionOptionButton == null)
            return;

        int selectedIndex = (int)index;
        string metadata =
            selectedIndex >= 0 && selectedIndex < ResolutionOptionButton.ItemCount
                ? ResolutionOptionButton.GetItemMetadata(selectedIndex).AsString()
                : "default";

        if (string.Equals(metadata, "default", StringComparison.OrdinalIgnoreCase))
        {
            UserSettings.ClearWindowResolution();
        }
        else if (TryParseResolution(metadata, out Vector2I resolution))
        {
            UserSettings.SetWindowResolution(resolution.X, resolution.Y);
        }

        UserSettings.ApplyWindowSettings(GetWindow());
    }

    private void OnSfxVolumeChanged(double value)
    {
        int volume = UserSettings.NormalizeVolumePercent(Mathf.RoundToInt((float)value));
        UserSettings.SetSfxVolumePercent(volume);
        SetVolumeLabel(SfxVolumeValueLabel, volume);
    }

    private void ShowSettingsPanel()
    {
        RefreshSettingsPanel();
        SetCenterPanelWide(true);
        if (MainButtons != null)
            MainButtons.Visible = false;
        if (SettingsPanel != null)
            SettingsPanel.Visible = true;
    }

    private void ShowMainPanel()
    {
        CloseAllDropdowns();
        SetCenterPanelWide(false);
        if (SettingsPanel != null)
            SettingsPanel.Visible = false;
        if (MainButtons != null)
            MainButtons.Visible = true;
    }

    private void SetCenterPanelWide(bool wide)
    {
        if (CenterPanel == null)
            return;

        CenterPanel.OffsetLeft = wide ? SettingsPanelOffsetLeft : MainPanelOffsetLeft;
        CenterPanel.OffsetRight = wide ? SettingsPanelOffsetRight : MainPanelOffsetRight;
        CenterPanel.PivotOffset = CenterPanel.Size * 0.5f;
    }

    private void RefreshSettingsPanel()
    {
        UserSettings.EnsureLoaded();
        RefreshLocalizedSettingsText();
        RebuildLanguageOptionButton();
        RebuildResolutionOptionButton();
        RebuildTextSizeOptionButton();
        RebuildBattleShakeOptionButton();
        if (DescriptionModeCheckBox != null)
            DescriptionModeCheckBox.ButtonPressed = UserSettings.UseCompactBattleCardDescriptions;
        if (TurnOrderPreviewCheckBox != null)
        {
            TurnOrderPreviewCheckBox.Visible = false;
            TurnOrderPreviewCheckBox.ButtonPressed = false;
        }
        if (IncomingDamagePreviewCheckBox != null)
            IncomingDamagePreviewCheckBox.ButtonPressed = UserSettings.ShowIncomingDamagePreview;
        if (IntentionTargetNamesCheckBox != null)
            IntentionTargetNamesCheckBox.ButtonPressed = UserSettings.ShowIntentionTargetNames;
        if (SingleTargetDamageIntentionArrowsCheckBox != null)
            SingleTargetDamageIntentionArrowsCheckBox.ButtonPressed =
                UserSettings.ShowSingleTargetDamageIntentionArrows;
        if (HideEnemySkillsCheckBox != null)
            HideEnemySkillsCheckBox.ButtonPressed = UserSettings.HideEnemySkills;
        if (GroupBattlePilesByCharacterCheckBox != null)
            GroupBattlePilesByCharacterCheckBox.ButtonPressed =
                UserSettings.GroupBattlePilesByCharacter;
        if (KeepManualTargetCardVisibleCheckBox != null)
            KeepManualTargetCardVisibleCheckBox.ButtonPressed =
                UserSettings.KeepManualTargetCardVisibleWhenHidden;
        if (ArrowManualTargetSelectionCheckBox != null)
            ArrowManualTargetSelectionCheckBox.ButtonPressed =
                UserSettings.UseArrowManualTargetSelection;
        SelectResolutionOption(UserSettings.WindowWidth, UserSettings.WindowHeight);
        SelectTextSizeOption(UserSettings.TextSizeLevel);
        SelectBattleShakeOption(UserSettings.BattleShakeLevel);
        SelectLanguageOption(UserSettings.Locale);
        SetVolumeSliderValue(MasterVolumeSlider, MasterVolumeValueLabel, UserSettings.MasterVolumePercent);
        SetVolumeSliderValue(SfxVolumeSlider, SfxVolumeValueLabel, UserSettings.SfxVolumePercent);
    }

    private void ConfigureTextSizeOptionButton()
    {
        if (TextSizeOptionButton == null)
            return;
        RebuildTextSizeOptionButton();
    }

    private void ConfigureBattleShakeOptionButton()
    {
        if (BattleShakeOptionButton == null)
            return;
        RebuildBattleShakeOptionButton();
    }

    private void ConfigureLanguageOptionButton()
    {
        if (LanguageOptionButton == null)
            return;
        RebuildLanguageOptionButton();
    }

    private void ConfigureResolutionOptionButton()
    {
        if (ResolutionOptionButton == null)
            return;
        RebuildResolutionOptionButton();
    }

    private static void ConfigureVolumeSlider(HSlider slider)
    {
        if (slider == null)
            return;

        slider.MinValue = 0;
        slider.MaxValue = 100;
        slider.Step = 1;
        slider.Rounded = true;
    }

    private void SelectTextSizeOption(int level)
    {
        if (TextSizeOptionButton == null)
            return;

        int normalizedLevel = UserSettings.NormalizeTextSizeLevel(level);
        for (int i = 0; i < TextSizeOptionButton.ItemCount; i++)
        {
            if (TextSizeOptionButton.GetItemId(i) != normalizedLevel)
                continue;

            TextSizeOptionButton.Select(i);
            return;
        }
    }

    private void SelectBattleShakeOption(int level)
    {
        if (BattleShakeOptionButton == null)
            return;

        int normalizedLevel = UserSettings.NormalizeBattleShakeLevel(level);
        for (int i = 0; i < BattleShakeOptionButton.ItemCount; i++)
        {
            if (BattleShakeOptionButton.GetItemId(i) != normalizedLevel)
                continue;

            BattleShakeOptionButton.Select(i);
            return;
        }
    }

    private void SelectLanguageOption(string locale)
    {
        if (LanguageOptionButton == null)
            return;

        string normalizedLocale = UserSettings.NormalizeLocale(locale);
        for (int i = 0; i < LanguageOptionButton.ItemCount; i++)
        {
            if (UserSettings.NormalizeLocale(LanguageOptionButton.GetItemMetadata(i).AsString()) != normalizedLocale)
                continue;

            LanguageOptionButton.Select(i);
            return;
        }
    }

    private void SelectResolutionOption(int width, int height)
    {
        if (ResolutionOptionButton == null)
            return;

        if (width <= 0 || height <= 0)
        {
            ResolutionOptionButton.Select(0);
            return;
        }

        string target = $"{width}x{height}";
        for (int i = 0; i < ResolutionOptionButton.ItemCount; i++)
        {
            if (!string.Equals(ResolutionOptionButton.GetItemMetadata(i).AsString(), target, StringComparison.OrdinalIgnoreCase))
                continue;

            ResolutionOptionButton.Select(i);
            return;
        }

        ResolutionOptionButton.AddItem(GetResolutionLabel(new Vector2I(width, height)));
        int customIndex = ResolutionOptionButton.ItemCount - 1;
        ResolutionOptionButton.SetItemMetadata(customIndex, target);
        ResolutionOptionButton.Select(customIndex);
    }

    private void RebuildTextSizeOptionButton()
    {
        if (TextSizeOptionButton == null)
            return;

        int selectedId =
            TextSizeOptionButton.Selected >= 0 && TextSizeOptionButton.Selected < TextSizeOptionButton.ItemCount
                ? TextSizeOptionButton.GetItemId(TextSizeOptionButton.Selected)
                : UserSettings.TextSizeLevel;
        TextSizeOptionButton.Clear();
        for (
            int level = UserSettings.TextSizeLevelSmall;
            level <= UserSettings.TextSizeLevelExtraLarge;
            level++
        )
        {
            TextSizeOptionButton.AddItem(UserSettings.GetTextSizeLevelLabel(level), level);
        }
        SelectTextSizeOption(selectedId);
    }

    private void RebuildBattleShakeOptionButton()
    {
        if (BattleShakeOptionButton == null)
            return;

        int selectedId =
            BattleShakeOptionButton.Selected >= 0 && BattleShakeOptionButton.Selected < BattleShakeOptionButton.ItemCount
                ? BattleShakeOptionButton.GetItemId(BattleShakeOptionButton.Selected)
                : UserSettings.BattleShakeLevel;
        BattleShakeOptionButton.Clear();
        for (
            int level = UserSettings.BattleShakeLevelOff;
            level <= UserSettings.BattleShakeLevelLarge;
            level++
        )
        {
            BattleShakeOptionButton.AddItem(UserSettings.GetBattleShakeLevelLabel(level), level);
        }
        SelectBattleShakeOption(selectedId);
    }

    private static void SetVolumeSliderValue(HSlider slider, Label label, int percent)
    {
        int normalizedPercent = UserSettings.NormalizeVolumePercent(percent);
        if (slider != null)
            slider.SetValueNoSignal(normalizedPercent);
        SetVolumeLabel(label, normalizedPercent);
    }

    private static void SetVolumeLabel(Label label, int percent)
    {
        if (label != null)
            label.Text = UserSettings.GetVolumePercentLabel(percent);
    }

    private void RefreshLocalizedSettingsText()
    {
        if (TagLabel != null)
            TagLabel.Text = I18n.Tr("ui.menu.tag", "SYSTEM MENU");
        if (TitleLabel != null)
            TitleLabel.Text = I18n.Tr("ui.menu.title", "菜单");
        if (DescriptionLabel != null)
            DescriptionLabel.Text = I18n.Tr(
                "ui.menu.description",
                "保存当前进度、调整设置，或直接结束本局。"
            );
        if (ReturnButton != null)
            ReturnButton.Text = I18n.Tr("ui.menu.return", "返回");
        if (SaveQuitButton != null)
            SaveQuitButton.Text = I18n.Tr("ui.menu.save_quit", "保存退出");
        if (SettingsButton != null)
            SettingsButton.Text = I18n.Tr("ui.menu.settings", "设置");
        if (EncyclopediaButton != null)
            EncyclopediaButton.Text = I18n.Tr("ui.menu.encyclopedia", "百科");
        if (AbandonGameButton != null)
            AbandonGameButton.Text = I18n.Tr("ui.menu.abandon", "放弃游戏");
        if (SettingsTitle != null)
            SettingsTitle.Text = I18n.Tr("ui.settings.title", "设置");
        if (DescriptionModeCheckBox != null)
            DescriptionModeCheckBox.Text = I18n.Tr(
                "ui.settings.compact_card_description",
                "战斗卡面显示总数值"
            );
        if (TurnOrderPreviewCheckBox != null)
            TurnOrderPreviewCheckBox.Text = I18n.Tr(
                "ui.settings.turn_order_preview",
                "显示战斗出手顺序"
            );
        if (IncomingDamagePreviewCheckBox != null)
            IncomingDamagePreviewCheckBox.Text = I18n.Tr(
                "ui.settings.incoming_damage_preview",
                "显示角色即将承受的伤害"
            );
        if (IntentionTargetNamesCheckBox != null)
            IntentionTargetNamesCheckBox.Text = I18n.Tr(
                "ui.settings.intention_target_names",
                "意图目标直接显示角色名称"
            );
        if (SingleTargetDamageIntentionArrowsCheckBox != null)
            SingleTargetDamageIntentionArrowsCheckBox.Text = I18n.Tr(
                "ui.settings.single_target_damage_intention_arrows",
                "单体伤害意图显示目标箭头"
            );
        if (HideEnemySkillsCheckBox != null)
            HideEnemySkillsCheckBox.Text = I18n.Tr(
                "ui.settings.hide_enemy_skills",
                "隐藏敌人技能"
            );
        if (GroupBattlePilesByCharacterCheckBox != null)
            GroupBattlePilesByCharacterCheckBox.Text = I18n.Tr(
                "ui.settings.group_battle_piles_by_character",
                "牌堆查看按角色分类"
            );
        if (KeepManualTargetCardVisibleCheckBox != null)
            KeepManualTargetCardVisibleCheckBox.Text = I18n.Tr(
                "ui.settings.keep_manual_target_card_visible",
                "隐藏选人界面时保留卡牌"
            );
        if (ArrowManualTargetSelectionCheckBox != null)
            ArrowManualTargetSelectionCheckBox.Text = I18n.Tr(
                "ui.settings.arrow_manual_target_selection",
                "手动目标使用箭头选择"
            );
        if (LanguageLabel != null)
            LanguageLabel.Text = I18n.Tr("ui.settings.language", "语言");
        if (ResolutionLabel != null)
            ResolutionLabel.Text = I18n.Tr("ui.settings.resolution", "分辨率");
        if (TextSizeLabel != null)
            TextSizeLabel.Text = I18n.Tr("ui.settings.text_size", "文本大小");
        if (BattleShakeLabel != null)
            BattleShakeLabel.Text = I18n.Tr("ui.settings.battle_shake", "战斗震动");
        if (MasterVolumeLabel != null)
            MasterVolumeLabel.Text = I18n.Tr("ui.settings.master_volume", "主音量");
        if (SfxVolumeLabel != null)
            SfxVolumeLabel.Text = I18n.Tr("ui.settings.sfx_volume", "音效音量");
        if (SettingsBackButton != null)
            SettingsBackButton.Text = I18n.Tr("ui.common.back", "返回");
    }

    private static void RefreshTextSizeForTree(Node node)
    {
        if (node == null)
            return;

        if (node is Tip tip)
            tip.RefreshTextSizeFromSettings();
        else if (node is SkillCard card)
            card.RefreshTextSizeFromSettings();

        foreach (Node child in node.GetChildren())
            RefreshTextSizeForTree(child);
    }

    private void AbortActiveBattle(bool unlockMapNodes = true)
    {
        var battle = FindActiveBattle(GetTree()?.Root);
        battle?.AbortBattle(unlockMapNodes);
    }

    private static Battle FindActiveBattle(Node node)
    {
        if (node == null)
            return null;

        if (node is Battle battle)
            return battle;

        foreach (Node child in node.GetChildren())
        {
            var found = FindActiveBattle(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ApplyHiddenState()
    {
        Modulate = new Color(1, 1, 1, 0);

        if (Backdrop != null)
            Backdrop.Modulate = new Color(1, 1, 1, 0);
        if (GlowTopRight != null)
            GlowTopRight.Modulate = new Color(1, 1, 1, 0);
        if (GlowBottomLeft != null)
            GlowBottomLeft.Modulate = new Color(1, 1, 1, 0);
        if (CenterPanel != null)
        {
            CenterPanel.Scale = ClosedPanelScale;
            CenterPanel.Modulate = new Color(1, 1, 1, 0);
        }
    }

    private static string GetResolutionLabel(Vector2I resolution) =>
        $"{resolution.X} x {resolution.Y}";

    private static bool TryParseResolution(string value, out Vector2I resolution)
    {
        resolution = Vector2I.Zero;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out int width) || !int.TryParse(parts[1], out int height))
            return false;

        width = UserSettings.NormalizeWindowDimension(width);
        height = UserSettings.NormalizeWindowDimension(height);
        if (width <= 0 || height <= 0)
            return false;

        resolution = new Vector2I(width, height);
        return true;
    }

    private void RebuildLanguageOptionButton()
    {
        if (LanguageOptionButton == null)
            return;

        string selectedMetadata =
            LanguageOptionButton.Selected >= 0 && LanguageOptionButton.Selected < LanguageOptionButton.ItemCount
                ? LanguageOptionButton.GetItemMetadata(LanguageOptionButton.Selected).AsString()
                : UserSettings.Locale;

        LanguageOptionButton.Clear();
        LanguageOptionButton.AddItem("简体中文", 0, "zh_CN");
        LanguageOptionButton.AddItem("English", 1, "en");
        SelectLanguageOption(selectedMetadata);
    }

    private void RebuildResolutionOptionButton()
    {
        if (ResolutionOptionButton == null)
            return;

        string selectedMetadata =
            ResolutionOptionButton.Selected >= 0 && ResolutionOptionButton.Selected < ResolutionOptionButton.ItemCount
                ? ResolutionOptionButton.GetItemMetadata(ResolutionOptionButton.Selected).AsString()
                : UserSettings.HasCustomWindowResolution
                    ? $"{UserSettings.WindowWidth}x{UserSettings.WindowHeight}"
                    : "default";

        ResolutionOptionButton.Clear();
        ResolutionOptionButton.AddItem(
            I18n.Tr("ui.settings.resolution_default", "默认（全屏）"),
            0,
            "default"
        );

        for (int i = 0; i < ResolutionOptions.Length; i++)
        {
            Vector2I resolution = ResolutionOptions[i];
            ResolutionOptionButton.AddItem(
                GetResolutionLabel(resolution),
                i + 1,
                $"{resolution.X}x{resolution.Y}"
            );
        }

        if (TryParseResolution(selectedMetadata, out Vector2I selectedResolution))
            SelectResolutionOption(selectedResolution.X, selectedResolution.Y);
        else
            SelectResolutionOption(0, 0);
    }

    private void CloseAllDropdowns()
    {
        LanguageOptionButton?.ClosePopup();
        ResolutionOptionButton?.ClosePopup();
        TextSizeOptionButton?.ClosePopup();
        BattleShakeOptionButton?.ClosePopup();
    }
}
