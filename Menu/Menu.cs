using Godot;

public partial class Menu : Control
{
    private const float OpenDuration = 0.22f;
    private const float CloseDuration = 0.18f;
    private static readonly Vector2 ClosedPanelScale = new(0.92f, 0.92f);
    private static readonly PackedScene EncyclopediaScene = GD.Load<PackedScene>(
        "res://Menu/Encyclopedia.tscn"
    );

    private ColorRect Backdrop => field ??= GetNodeOrNull<ColorRect>("Backdrop");
    private ColorRect GlowTopRight => field ??= GetNodeOrNull<ColorRect>("GlowTopRight");
    private ColorRect GlowBottomLeft => field ??= GetNodeOrNull<ColorRect>("GlowBottomLeft");
    private Control CenterPanel => field ??= GetNodeOrNull<Control>("CenterPanel");
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
    private CheckBox DescriptionModeCheckBox =>
        field ??= GetNodeOrNull<CheckBox>(
            "CenterPanel/Margin/VBox/SettingsPanel/DescriptionModeCheckBox"
        );
    private CheckBox TurnOrderPreviewCheckBox =>
        field ??= GetNodeOrNull<CheckBox>(
            "CenterPanel/Margin/VBox/SettingsPanel/TurnOrderPreviewCheckBox"
        );
    private CheckBox EnemyAttackPreviewCheckBox =>
        field ??= GetNodeOrNull<CheckBox>(
            "CenterPanel/Margin/VBox/SettingsPanel/EnemyAttackPreviewCheckBox"
        );
    private Button SettingsBackButton =>
        field ??= GetNodeOrNull<Button>(
            "CenterPanel/Margin/VBox/SettingsPanel/SettingsBackButton"
        );
    private Tween _transitionTween;

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
            TurnOrderPreviewCheckBox.Pressed += OnTurnOrderPreviewPressed;

        if (EnemyAttackPreviewCheckBox != null)
            EnemyAttackPreviewCheckBox.Pressed += OnEnemyAttackPreviewPressed;

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
        };
    }

    private void OnSaveQuitPressed()
    {
        AbortActiveBattle();
        SceneTransitionLayer.Ensure(this)?.SwitchScene("res://BeginGame/StartInterface.tscn");
    }

    private void OnAbandonGamePressed()
    {
        var activeBattle = FindActiveBattle(GetTree()?.Root);

        Close();
        activeBattle?.AbortBattle(unlockMapNodes: false);
        GameInfo.RecordCurrentRunHistory(victory: false, includeCurrentNode: true);
        SaveSystem.SaveAll();
        GameOverSummary.Show(this);
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
        activeBattle?.CharacterControl?.RefreshDisplayedSkillDescriptions();
    }

    private void OnTurnOrderPreviewPressed()
    {
        if (TurnOrderPreviewCheckBox == null)
            return;

        UserSettings.SetBattleTurnOrderPreview(TurnOrderPreviewCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshTurnOrderPreviewFromSettings();
    }

    private void OnEnemyAttackPreviewPressed()
    {
        if (EnemyAttackPreviewCheckBox == null)
            return;

        UserSettings.SetEnemyAttackPreview(EnemyAttackPreviewCheckBox.ButtonPressed);
        FindActiveBattle(GetTree()?.Root)?.RefreshEnemyAttackPreviewFromSettings();
    }

    private void ShowSettingsPanel()
    {
        RefreshSettingsPanel();
        if (MainButtons != null)
            MainButtons.Visible = false;
        if (SettingsPanel != null)
            SettingsPanel.Visible = true;
    }

    private void ShowMainPanel()
    {
        if (SettingsPanel != null)
            SettingsPanel.Visible = false;
        if (MainButtons != null)
            MainButtons.Visible = true;
    }

    private void RefreshSettingsPanel()
    {
        UserSettings.EnsureLoaded();
        if (DescriptionModeCheckBox != null)
            DescriptionModeCheckBox.ButtonPressed = UserSettings.UseCompactBattleCardDescriptions;
        if (TurnOrderPreviewCheckBox != null)
            TurnOrderPreviewCheckBox.ButtonPressed = UserSettings.ShowBattleTurnOrderPreview;
        if (EnemyAttackPreviewCheckBox != null)
            EnemyAttackPreviewCheckBox.ButtonPressed = UserSettings.ShowEnemyAttackPreview;
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
}
