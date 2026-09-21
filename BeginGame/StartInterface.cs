using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class StartInterface : CanvasLayer
{
    private const string AutosavePath = "user://autosave.cfg";
    private const int RequiredCharacterSelectionCount = GameInfo.DefaultPlayerPartySize;
    private const float MenuButtonHoverTextOffset = 6f;
    private const float MenuButtonHoverDuration = 0.12f;
    private const float CharacterSelectionTransitionDuration = 0.22f;
    private const float ParallaxSmoothingRate = 6f;
    private const float ExitFadeDuration = 0.18f;
    private const float ReturnFromMapEntranceSpeed = 2.5f;

    // Neutral silver keeps the line-based navigation consistent with the background.
    private static readonly Color AccentSteel = new(0.66f, 0.83f, 0.94f);
    private static readonly Color PrimaryInk = new(0.91f, 0.98f, 0.97f);
    private static readonly Color MarkerDisabled = new(0.33f, 0.37f, 0.43f);
    private static readonly Color TextSecondary = new(0.78f, 0.83f, 0.88f);
    private static readonly Color TextDim = new(0.53f, 0.62f, 0.71f);
    private static readonly Color StatusGreen = new(0.60f, 0.79f, 0.74f);
    private static readonly Color StatusAmber = new(0.76f, 0.74f, 0.68f);
    private static readonly Color StatusGrey = new(0.45f, 0.49f, 0.56f);

    private sealed class MenuButtonVisuals
    {
        public StyleBoxFlat Normal;
        public StyleBoxFlat Hover;
        public StyleBoxFlat Pressed;
        public StyleBoxFlat Focus;
        public StyleBoxFlat Disabled;
        public float BaseLeftMargin;
        public Tween Tween;
        public Line2D Arrow;
        public Vector2 ArrowBasePosition;
        public Button Owner;
        public bool PrimaryStyle;
        public ColorRect AccentLine;
        public float HoverAmount;
        public float PressAmount;
        public float ClickPulse;
    }

    private sealed class MenuLayer
    {
        public Control Node;
        public Vector2 BasePosition;
        public float ParallaxStrength;
        public float FloatAmplitude;
    }

    public static PackedScene TipScene =>
        field ??= PreloadeScene.GetPackedScene("res://battle/UIScene/Tip.tscn");
    public static PackedScene _Echo =>
        field ??= PreloadeScene.GetPackedScene("res://character/PlayerCharacter/Echo/Echo.tscn");
    public static PackedScene _Kasiya =>
        field ??= PreloadeScene.GetPackedScene(
            "res://character/PlayerCharacter/Kasiya/kasiya.tscn"
        );
    public static PackedScene _Mariya =>
        field ??= PreloadeScene.GetPackedScene("res://character/PlayerCharacter/Mariya/Mariya.tscn");
    public static PackedScene _Nightingale =>
        field ??= PreloadeScene.GetPackedScene(
            "res://character/PlayerCharacter/Nightingale/Nightingale.tscn"
        );
    private static PackedScene EncyclopediaScene =>
        field ??= PreloadeScene.GetPackedScene("res://Menu/Encyclopedia.tscn");
    private static PackedScene SettingsMenuScene =>
        field ??= PreloadeScene.GetPackedScene("res://Menu/Menu.tscn");
    private const string CharacterSelectionOverlayScenePath =
        "res://BeginGame/CharacterSelectionOverlay.tscn";

    private Button StartGameButton => field ??= GetNodeOrNull<Button>("Layout/Menu/NewGame");
    private Button ContinueGameButton => field ??= GetNodeOrNull<Button>("Layout/Menu/Continue");
    private Button StatisticsButton => field ??= GetNodeOrNull<Button>("Layout/Masthead/Utilities/Statistics");
    private Button EncyclopediaButton => field ??= GetNodeOrNull<Button>("Layout/Masthead/Utilities/Encyclopedia");
    private Button DisplaySettingsButton => field ??= GetNodeOrNull<Button>("Layout/Masthead/Utilities/Settings");
    private Button ExitGameButton => field ??= GetNodeOrNull<Button>("Layout/Masthead/Utilities/Exit");
    private Label StatusLine => field ??= GetNodeOrNull<Label>("Layout/Menu/Status/Text");
    private ColorRect StatusDot => field ??= GetNodeOrNull<ColorRect>("Layout/Menu/Status/Dot");
    private Control TitleBlock => field ??= GetNodeOrNull<Control>("Layout/TitleBlock");
    private Label EyebrowLabel => field ??= GetNodeOrNull<Label>("Layout/TitleBlock/Eyebrow");
    private Label TitleLabel => field ??= GetNodeOrNull<Label>("Layout/TitleBlock/Title");
    private Label SubtitleLabel => field ??= GetNodeOrNull<Label>("Layout/TitleBlock/Subtitle");
    private Control MenuPanel => field ??= GetNodeOrNull<Control>("Layout/Menu");
    private Control ArchivePanel => field ??= GetNodeOrNull<Control>("Layout/Archive");
    private Label ArchiveHeadTitle => field ??= GetNodeOrNull<Label>("Layout/Archive/Heading");
    private HBoxContainer ArchiveRows => field ??= GetNodeOrNull<HBoxContainer>("Layout/Archive/Rows");
    private Label BuildLabel => field ??= GetNodeOrNull<Label>("Layout/Footer/BuildLabel");
    private Control Footer => field ??= GetNodeOrNull<Control>("Layout/Footer");
    private Control Masthead => field ??= GetNodeOrNull<Control>("Layout/Masthead");
    private Control DimensionCaption => field ??= GetNodeOrNull<Control>("Layout/DimensionCaption");
    private Control DimensionField => field ??= GetNodeOrNull<Control>("DimensionField");
    private ColorRect WarpFlashRect => field ??= GetNodeOrNull<ColorRect>("WarpFlash");
    private ShaderMaterial FieldMaterial => field ??= GetNodeOrNull<ColorRect>("Space")?.Material as ShaderMaterial;

    private readonly Dictionary<Button, MenuButtonVisuals> _menuButtonVisuals = new();
    private readonly List<MenuLayer> _parallaxLayers = new();
    private bool _parallaxInitialized;
    private Tween _fieldEmphasisTween;
    private Vector2 _parallaxOffset;
    private float _menuTime;
    private bool _isCharacterSelectionTransitioning;
    private bool _isContinuingGame;
    private bool _isExitingGame;
    private bool _menuHasAutosave;
    private bool _menuAutosaveRunFinished;
    private Menu _settingsMenu;
    private static bool _fastEntranceRequested;
    private CharacterSelectionOverlay CharacterSelection =>
        GetNodeOrNull<CharacterSelectionOverlay>("CharacterSelectionOverlay");

    public bool IsPresentationReady => IsNodeReady();

    public override void _Ready()
    {
        SetProcessUnhandledInput(MobilePlatform.IsMobile);
        SetProcess(true);
        ClearGameplayRootOverlays();

        SaveSystem.LoadRunHistory();
        LocalizeStaticTexts();
        BuildArchivePanel();
        RefreshAutosaveMenuState();
        RefreshContinueButtonState();
        SetupMenuButtonHoverAnimations();
        ApplyMobileTouchLayout();
        FieldMaterial?.SetShaderParameter("emphasis", 0f);
        PlayEntrance(ConsumeEntrancePlaybackSpeed());

        if (StartGameButton != null)
            StartGameButton.Pressed += OpenCharacterSelection;

        if (StatisticsButton != null)
            StatisticsButton.Pressed += ShowStatistics;

        if (EncyclopediaButton != null)
            EncyclopediaButton.Pressed += ShowEncyclopedia;

        if (DisplaySettingsButton != null)
            DisplaySettingsButton.Pressed += OpenSettings;

        if (ExitGameButton != null)
            ExitGameButton.Pressed += ExitGame;

        EnsureTitleTooltips();
    }

    // TipLayer lives under /root. Map-owned pulse layers are freed with the map.
    private void ClearGameplayRootOverlays()
    {
        Node root = GetTree()?.Root;
        if (root == null)
            return;

        root.GetNodeOrNull<GameOverSummary>("GameOverSummary")?.QueueFree();
        root.GetNodeOrNull<GameStatistics>("GameStatistics")?.QueueFree();
        root.GetNodeOrNull<BattlePreviewTutorialOverlay>("BattlePreviewTutorialOverlay")?.QueueFree();

        CanvasLayer tipLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (tipLayer == null)
            return;

        foreach (Node child in tipLayer.GetChildren())
        {
            if (child is Tip tip && (tip.Name == "Tip" || tip.Name == "BuffTip"))
                tip.HideTooltip();
            else
                child.QueueFree();
        }
    }

    private void EnsureTitleTooltips()
    {
        Node root = GetTree()?.Root;
        if (root == null || TipScene == null)
            return;

        CanvasLayer layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        ConfigureTitleTooltip(layer, "Tip", new Vector2(20f, 20f));
        ConfigureTitleTooltip(layer, "BuffTip", new Vector2(-20f, 20f));
    }

    private void ConfigureTitleTooltip(CanvasLayer layer, string name, Vector2 anchorOffset)
    {
        Tip tip = layer.GetNodeOrNull<Tip>(name);
        if (tip == null)
        {
            tip = TipScene.Instantiate<Tip>();
            tip.Name = name;
            layer.AddChild(tip);
        }

        tip.FollowMouse = true;
        tip.AnchorOffset = anchorOffset;
        tip.HideTooltip();
    }

    private void ApplyMobileTouchLayout()
    {
        if (!MobilePlatform.IsMobile)
            return;

        // The two main actions retain their fixed positions; utilities share a container.
        if (StartGameButton != null)
            StartGameButton.CustomMinimumSize = new Vector2(0f, 80f);
        if (ContinueGameButton != null)
            ContinueGameButton.CustomMinimumSize = new Vector2(0f, 80f);
        foreach (Button button in new[] { StatisticsButton, EncyclopediaButton, DisplaySettingsButton, ExitGameButton })
        {
            if (button != null)
                MobilePlatform.EnsureContainerTouchTarget(button, minimumHeight: 64f);
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!MobilePlatform.IsMobile || !MobilePlatform.IsCancelPress(inputEvent))
            return;

        GetViewport()?.SetInputAsHandled();
        ExitGame();
    }

    public void NewStart()
    {
        GameInfo.PlayerCharacters = BuildDefaultRoster()
            .Take(RequiredCharacterSelectionCount)
            .Select(
                (info, index) =>
                    CloneStarterPlayerInfo(info, GameInfo.GetDefaultPlayerFormationPosition(index))
            )
            .ToArray();
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        test();
    }

    public void Start()
    {
        OpenCharacterSelection();
    }

    public void test()
    {
        // Give the first character the whole roster's skill pool for pagination tests.
        // SkillID[] rosterSkills = GameInfo
        //     .PlayerCharacters.Where(info => info.AllSkills != null)
        //     .SelectMany(info => info.AllSkills)
        //     .Distinct()
        //     .ToArray();
        // AddTestSkills(0, rosterSkills);
        GameInfo.ElectricityCoin += 999;
    }

    private static void AddTestSkills(int characterIndex, params SkillID[] skills)
    {
        if (GameInfo.PlayerCharacters == null)
            return;
        if (characterIndex < 0 || characterIndex >= GameInfo.PlayerCharacters.Length)
            return;

        var info = GameInfo.PlayerCharacters[characterIndex];
        info.GainedSkills ??= new List<SkillID>();
        info.GainedSkills.AddRange(skills);
        info.GainedSkills = info.GainedSkills.Distinct().ToList();
        GameInfo.PlayerCharacters[characterIndex] = info;
    }

    public void continueGame()
    {
        _ = ContinueGameAsync();
    }

    private async Task ContinueGameAsync()
    {
        if (_isContinuingGame)
            return;

        _isContinuingGame = true;
        if (ContinueGameButton != null)
            ContinueGameButton.Disabled = true;

        PlayDeparture();
        SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);
        try
        {
            if (transitionLayer != null)
                await transitionLayer.FadeToBlackAsync(CharacterSelectionTransitionDuration);

            SaveSystem.LoadAll();
        }
        catch (Exception e)
        {
            GD.PushError($"ContinueGame load failed: {e}");
            if (transitionLayer != null)
                await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
            _isContinuingGame = false;
            RefreshAutosaveMenuState();
            RefreshContinueButtonState();
            return;
        }

        if (GameInfo.RunFinished)
        {
            if (transitionLayer != null)
                await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
            _isContinuingGame = false;
            RefreshAutosaveMenuState();
            RefreshContinueButtonState();
            return;
        }

        var err = GetTree().ChangeSceneToFile("res://Map/Map.tscn");
        if (err != Error.Ok)
        {
            GD.PushError($"ContinueGame scene switch failed: {err}");
            if (transitionLayer != null)
                await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
            _isContinuingGame = false;
            RefreshAutosaveMenuState();
            RefreshContinueButtonState();
            return;
        }

        if (transitionLayer != null)
            await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
    }

    public void falseTest()
    {
        Battle.Istest = false;
        Start();
    }

    private void ExitGame()
    {
        _ = ExitGameAsync();
    }

    private async Task ExitGameAsync()
    {
        if (_isExitingGame)
            return;

        _isExitingGame = true;
        if (ExitGameButton != null)
            ExitGameButton.Disabled = true;

        try
        {
            SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);
            if (transitionLayer != null)
                await transitionLayer.FadeToBlackAsync(ExitFadeDuration);
        }
        catch (Exception e)
        {
            GD.PushError($"StartInterface exit transition failed: {e}");
        }
        finally
        {
            PreloadeScene.ReleaseCachedResources();
            GetTree()?.Quit();
        }
    }

    public static void RequestFastEntrance()
    {
        _fastEntranceRequested = true;
    }

    private static float ConsumeEntrancePlaybackSpeed()
    {
        bool fastEntranceRequested = _fastEntranceRequested;
        _fastEntranceRequested = false;
        return fastEntranceRequested ? ReturnFromMapEntranceSpeed : 1f;
    }

    private void OpenCharacterSelection()
    {
        _ = OpenCharacterSelectionAsync();
    }

    private async Task OpenCharacterSelectionAsync()
    {
        if (_isCharacterSelectionTransitioning || CharacterSelection != null)
            return;

        _isCharacterSelectionTransitioning = true;
        PlayDeparture();
        SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);

        try
        {
            if (transitionLayer != null)
                await transitionLayer.FadeToBlackAsync(CharacterSelectionTransitionDuration);

            var overlay = CreateCharacterSelectionOverlay();
            if (overlay == null)
            {
                if (transitionLayer != null)
                    await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
                return;
            }

            AddChild(overlay);
            overlay.Open(
                BuildDefaultRoster(),
                RequiredCharacterSelectionCount,
                BeginNewRunFromSelection,
                playEnterAnimation: transitionLayer == null
            );

            if (transitionLayer != null)
                await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
        }
        catch (Exception e)
        {
            GD.PushError($"Open character selection transition failed: {e}");
            if (transitionLayer != null)
                await transitionLayer.FadeFromBlackAsync(CharacterSelectionTransitionDuration);
        }
        finally
        {
            _isCharacterSelectionTransitioning = false;
        }
    }

    private CharacterSelectionOverlay CreateCharacterSelectionOverlay()
    {
        var scene = GD.Load<PackedScene>(CharacterSelectionOverlayScenePath);
        if (scene == null)
        {
            if (StatusLine != null)
                StatusLine.Text = I18n.Tr(
                    "ui.start.character_select_load_failed",
                    "角色选择界面加载失败，请检查 CharacterSelectionOverlay.tscn。"
                );
            GD.PushError(
                $"Character selection scene failed to load: {CharacterSelectionOverlayScenePath}"
            );
            return null;
        }

        var overlay = scene.Instantiate<CharacterSelectionOverlay>();
        if (overlay == null)
        {
            if (StatusLine != null)
                StatusLine.Text = I18n.Tr(
                    "ui.start.character_select_instantiate_failed",
                    "角色选择界面实例化失败。"
                );
            GD.PushError("Character selection scene failed to instantiate.");
            return null;
        }

        overlay.Name = "CharacterSelectionOverlay";
        return overlay;
    }

    private void BeginNewRunFromSelection(PlayerInfoStructure[] selectedCharacters, int seed, int difficulty)
    {
        if (selectedCharacters == null || selectedCharacters.Length == 0)
            return;

        GameInfo.Seed = seed;
        GameInfo.Difficulty = difficulty;
        GameInfo.PlayerCharacters = selectedCharacters;
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        test();
        GameInfo.InitNewGame();
        GameInfo.ApplyDifficultyStartBonuses();
        GameInfo.ApplyDifficultyRunStartPenalties();
        SceneTransitionLayer.Ensure(this)?.SwitchScene("res://Map/Map.tscn");
    }

    private static PlayerInfoStructure[] BuildDefaultRoster()
    {
        var registry = new PlayerCharacterRegistry();
        return
        [
            CloneStarterPlayerInfo(registry.Echo, 1),
            CloneStarterPlayerInfo(registry.Kasiya, 2),
            CloneStarterPlayerInfo(registry.Mariya, 3),
            CloneStarterPlayerInfo(registry.Nightingale, 4),
        ];
    }

    private static PlayerInfoStructure CloneStarterPlayerInfo(
        PlayerInfoStructure source,
        int positionIndex
    )
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
            UnlockedTalents =
                source.UnlockedTalents != null
                    ? new List<string>(source.UnlockedTalents)
                    : new List<string>(),
            AppliedTalentMaxLifeBonus = source.AppliedTalentMaxLifeBonus,
            GainedSkills =
                source.GainedSkills != null
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

    private void ShowStatistics()
    {
        GameStatistics.Show(this);
    }

    private void ShowEncyclopedia()
    {
        if (GetNodeOrNull<Encyclopedia>("Encyclopedia") != null)
            return;

        var encyclopedia = EncyclopediaScene?.Instantiate<Encyclopedia>();
        if (encyclopedia == null)
        {
            GD.PushError("Encyclopedia scene failed to instantiate from StartInterface.");
            return;
        }

        encyclopedia.Name = "Encyclopedia";
        AddChild(encyclopedia);
    }

    private void RefreshAutosaveMenuState()
    {
        _menuHasAutosave = FileAccess.FileExists(AutosavePath);
        _menuAutosaveRunFinished = false;

        if (!_menuHasAutosave)
            return;

        try
        {
            if (SaveSystem.TryReadRunFinished(out bool runFinished))
                _menuAutosaveRunFinished = runFinished;
        }
        catch (Exception e)
        {
            GD.PushError($"StartInterface autosave status read failed: {e}");
        }
    }

    private void RefreshContinueButtonState()
    {
        if (ContinueGameButton == null)
            return;

        ContinueGameButton.Disabled = !_menuHasAutosave || _menuAutosaveRunFinished;

        if (StatusLine != null)
        {
            StatusLine.Text =
                !_menuHasAutosave ? I18n.Tr("ui.start.status.no_autosave", "尚无旅程记录，开始你的第一次冒险。")
                : _menuAutosaveRunFinished ? I18n.Tr("ui.start.status.finished_run", "上次旅程已结束，开启新的冒险。")
                : I18n.Tr("ui.start.status.can_continue", "旅程已保存，可以继续上一次冒险。");

            Color statusColor =
                !_menuHasAutosave ? StatusGrey
                : _menuAutosaveRunFinished ? StatusAmber
                : StatusGreen;
            StatusLine.AddThemeColorOverride("font_color", statusColor);
            if (StatusDot != null)
                StatusDot.Color = statusColor;
        }

        RefreshMenuButtonAccents();
    }

    private void LocalizeStaticTexts()
    {
        string title = I18n.Tr("ui.start.title", "FOUR-DIMENSIONAL");
        if (TitleLabel != null)
            TitleLabel.Text = title.Replace("-", "\n");
        if (EyebrowLabel != null)
            EyebrowLabel.Text = I18n.Tr("ui.start.eyebrow", "STRATEGIC COMMAND / 战术指挥");
        if (ArchiveHeadTitle != null)
            ArchiveHeadTitle.Text = I18n.Tr("ui.start.journey_record", "旅程记录");
        if (BuildLabel != null)
        {
            string version = ProjectSettings.GetSetting("application/config/version").AsString();
            BuildLabel.Text = string.IsNullOrWhiteSpace(version)
                ? I18n.Tr("ui.start.build.unversioned", "PRE-RELEASE BUILD")
                : $"FOUR-DIMENSIONAL  v{version}";
        }
        if (SubtitleLabel != null)
        {
            SubtitleLabel.Text = I18n.Tr(
                "ui.start.subtitle",
                "组建队伍 · 构筑牌组 · 探索未知"
            );
        }
        if (StartGameButton != null)
            StartGameButton.Text = I18n.Tr("ui.start.new_game", "开始新游戏");
        if (ContinueGameButton != null)
            ContinueGameButton.Text = I18n.Tr("ui.start.continue", "继续游戏");
        if (StatisticsButton != null)
            StatisticsButton.Text = I18n.Tr("ui.start.statistics", "统计");
        if (EncyclopediaButton != null)
            EncyclopediaButton.Text = I18n.Tr("ui.start.encyclopedia", "百科");
        if (DisplaySettingsButton != null)
            DisplaySettingsButton.Text = I18n.Tr("ui.start.settings", "设置");
        if (ExitGameButton != null)
            ExitGameButton.Text = I18n.Tr("ui.start.exit", "退出游戏");
        if (StatusLine != null)
            StatusLine.Text = I18n.Tr("ui.start.status.validating", "正在校验自动存档...");
        if (GetNodeOrNull<Label>("Layout/DimensionCaption/LocalizedName") is { } dimensionLabel)
            dimensionLabel.Text = I18n.Tr("ui.start.dimension", "四维空间");
    }

    private void OpenSettings()
    {
        if (_settingsMenu == null || !GodotObject.IsInstanceValid(_settingsMenu))
        {
            _settingsMenu = SettingsMenuScene?.Instantiate<Menu>();
            if (_settingsMenu == null)
            {
                GD.PushError("Settings menu failed to instantiate from Menu.tscn.");
                return;
            }

            _settingsMenu.Name = "StartSettingsMenu";
            AddChild(_settingsMenu);
        }

        _settingsMenu.OpenSettingsOnly();
    }

    // ---------------------------------------------------------------
    // A compact journey record sits beneath the menu, leaving the dimensional structure clear.
    // ---------------------------------------------------------------

    private void BuildArchivePanel()
    {
        if (ArchiveRows == null)
            return;

        foreach (Node child in ArchiveRows.GetChildren())
            child.QueueFree();

        List<RunHistoryRecord> records =
            GameInfo.RunHistoryRecords?.Where(record => record != null).ToList()
            ?? new List<RunHistoryRecord>();

        int totalRuns = records.Count;
        int victories = records.Count(record => record.Victory);
        int bestLevel = records.Count > 0 ? records.Max(record => record.MapLevel) : 0;
        int enemiesDefeated = records.Sum(record => record.EnemiesDefeated);
        long totalSeconds = records.Sum(record => record.SessionPlaySeconds);

        AddArchiveRow(
            I18n.Tr("ui.start.archive.runs", "航次总数"),
            totalRuns.ToString(),
            totalRuns > 0 ? TextSecondary : StatusGrey
        );
        AddArchiveRow(
            I18n.Tr("ui.start.archive.victories", "通关次数"),
            victories.ToString(),
            victories > 0 ? StatusGreen : StatusGrey
        );
        AddArchiveRow(
            I18n.Tr("ui.start.archive.best_level", "最深层数"),
            bestLevel > 0 ? bestLevel.ToString() : "--",
            bestLevel > 0 ? AccentSteel : StatusGrey
        );
        AddArchiveRow(
            I18n.Tr("ui.start.archive.defeated", "累计击破"),
            enemiesDefeated.ToString(),
            enemiesDefeated > 0 ? TextSecondary : StatusGrey
        );
        AddArchiveRow(
            I18n.Tr("ui.start.archive.playtime", "累计航时"),
            FormatPlaytime(totalSeconds),
            totalSeconds > 0 ? TextSecondary : StatusGrey
        );
    }

    private static string FormatPlaytime(long seconds)
    {
        if (seconds <= 0)
            return "--";

        long hours = seconds / 3600;
        long minutes = seconds % 3600 / 60;
        return hours > 0 ? $"{hours}h {minutes:00}m" : $"{minutes}m";
    }

    private void AddArchiveRow(string label, string value, Color valueColor)
    {
        var column = new VBoxContainer {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 0);

        var valueLabel = new Label {
            Text = value,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        valueLabel.AddThemeFontOverride("font", TitleLabel.GetThemeFont("font"));
        valueLabel.AddThemeFontSizeOverride("font_size", 28);
        valueLabel.AddThemeColorOverride("font_color", valueColor);
        column.AddChild(valueLabel);

        var nameLabel = new Label { Text = label, MouseFilter = Control.MouseFilterEnum.Ignore };
        nameLabel.AddThemeFontSizeOverride("font_size", 15);
        nameLabel.AddThemeColorOverride("font_color", TextDim);
        column.AddChild(nameLabel);
        ArchiveRows.AddChild(column);
    }

    private void PlayEntrance(float playbackSpeed = 1f)
    {
        float timingScale = 1f / Mathf.Max(playbackSpeed, 0.01f);
        FadeInControl(DimensionField, 0f, 1.15f * timingScale);
        FadeInControl(GetNodeOrNull<Control>("FlowCurves"), 0.1f * timingScale, 1.8f * timingScale);
        FadeInControl(Masthead, 0.06f * timingScale, 0.45f * timingScale);
        SlideInTitle(TitleLabel, 0.15f * timingScale, 0.65f * timingScale, 18f);
        SlideInTitle(SubtitleLabel, 0.29f * timingScale, 0.55f * timingScale, 10f);
        FadeInControl(MenuPanel, 0.32f * timingScale, 0.45f * timingScale);
        RevealGuide("TopRule", 0.04f * timingScale, 0.75f * timingScale);
        RevealGuide("MenuRail", 0.30f * timingScale, 0.65f * timingScale);
        RevealGuide("BottomRule", 0.52f * timingScale, 0.8f * timingScale);

        float delay = 0.42f * timingScale;
        foreach (Button button in EnumerateMenuButtons())
        {
            FadeInControl(button, delay, 0.45f * timingScale);
            delay += 0.055f * timingScale;
        }
        FadeInControl(ArchiveHeadTitle, 0.65f * timingScale, 0.4f * timingScale);
        if (ArchiveRows != null) {
            float archiveDelay = 0.69f;
            foreach (Node child in ArchiveRows.GetChildren()) {
                if (child is Control column)
                    FadeInControl(column, archiveDelay * timingScale, 0.45f * timingScale);
                archiveDelay += 0.065f;
            }
        }
        FadeInControl(DimensionCaption, 0.75f * timingScale, 0.7f * timingScale);
        FadeInControl(Footer, 0.8f * timingScale, 0.6f * timingScale);
    }

    private IEnumerable<Button> EnumerateMenuButtons()
    {
        if (StartGameButton != null)
            yield return StartGameButton;
        if (ContinueGameButton != null)
            yield return ContinueGameButton;
        if (StatisticsButton != null)
            yield return StatisticsButton;
        if (EncyclopediaButton != null)
            yield return EncyclopediaButton;
        if (DisplaySettingsButton != null)
            yield return DisplaySettingsButton;
        if (ExitGameButton != null)
            yield return ExitGameButton;
    }

    private void SlideInTitle(Control control, float delay, float duration, float distance) {
        if (control == null)
            return;
        Vector2 destination = control.Position;
        control.Position = destination + new Vector2(0f, distance);
        control.Modulate = new Color(1f, 1f, 1f, 0f);
        Tween tween = CreateTween();
        tween.TweenInterval(delay);
        tween.TweenProperty(control, "position", destination, duration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(control, "modulate:a", 1f, duration);
    }

    private void RevealGuide(string name, float delay, float duration) {
        var line = GetNodeOrNull<Line2D>("Layout/ConsoleGuides/" + name);
        if (line == null || line.GetPointCount() != 2)
            return;
        Vector2 start = line.GetPointPosition(0);
        Vector2 end = line.GetPointPosition(1);
        line.SetPointPosition(1, start);
        Tween tween = CreateTween();
        tween.TweenInterval(delay);
        tween.TweenMethod(Callable.From<float>(amount => line.SetPointPosition(1, start.Lerp(end, amount))),
            0f, 1f, duration).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    }

    private void FadeInControl(Control control, float delay, float duration)
    {
        if (control == null)
            return;

        control.Modulate = new Color(1f, 1f, 1f, 0f);
        Tween tween = CreateTween();
        tween.TweenInterval(delay);
        tween
            .TweenProperty(control, "modulate:a", 1f, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void PlayDeparture()
    {
        if (WarpFlashRect == null)
            return;

        Tween tween = CreateTween();
        tween.TweenProperty(WarpFlashRect, "color:a", 0.12f, 0.18f);
        tween.TweenProperty(WarpFlashRect, "color:a", 0f, 0.4f);
    }

    public override void _Process(double delta)
    {
        _menuTime += (float)delta;
        UpdateMenuParallax((float)delta);
        UpdateMenuFeedback((float)delta);
    }

    private void InitializeParallaxLayers()
    {
        _parallaxLayers.Clear();
        AddParallaxLayer(DimensionField, 9f, floatAmplitude: 2f);
        AddParallaxLayer(GetNodeOrNull<Control>("FlowCurves"), 9f, floatAmplitude: 2f);
        _parallaxInitialized = true;
    }

    private void AddParallaxLayer(Control node, float parallaxStrength, float floatAmplitude = 0f)
    {
        if (node == null)
            return;

        _parallaxLayers.Add(
            new MenuLayer
            {
                Node = node,
                BasePosition = new Vector2(node.OffsetLeft, node.OffsetTop),
                ParallaxStrength = parallaxStrength,
                FloatAmplitude = floatAmplitude,
            }
        );
    }

    private void UpdateMenuParallax(float delta)
    {
        if (!_parallaxInitialized)
            InitializeParallaxLayers();

        Viewport viewport = GetViewport();
        if (viewport == null || _parallaxLayers.Count == 0)
            return;

        Vector2 viewportSize = viewport.GetVisibleRect().Size;
        if (viewportSize.X <= 0f || viewportSize.Y <= 0f)
            return;

        Vector2 normalized = Vector2.Zero;
        if (!MobilePlatform.IsMobile)
        {
            normalized = viewport.GetMousePosition() / viewportSize - new Vector2(0.5f, 0.5f);
            normalized = normalized.Clamp(new Vector2(-0.5f, -0.5f), new Vector2(0.5f, 0.5f));
        }

        float smoothing = 1f - Mathf.Exp(-ParallaxSmoothingRate * delta);
        _parallaxOffset = _parallaxOffset.Lerp(normalized, smoothing);

        for (int i = 0; i < _parallaxLayers.Count; i++)
        {
            MenuLayer layer = _parallaxLayers[i];
            if (!IsInstanceValid(layer.Node))
                continue;

            // Keep the projection aligned when the window changes aspect ratio.
            Vector2 anchorOrigin = viewportSize * new Vector2(layer.Node.AnchorLeft, layer.Node.AnchorTop);
            Vector2 target = anchorOrigin + layer.BasePosition - _parallaxOffset * (layer.ParallaxStrength * 2f);
            if (layer.FloatAmplitude != 0f)
            {
                target.Y += Mathf.Sin(_menuTime * 0.28f + i * 1.3f) * layer.FloatAmplitude;
            }
            layer.Node.Position = target;
        }
    }

    // ---------------------------------------------------------------
    // Hover and keyboard focus share a small text shift and an arrow accent.
    // ---------------------------------------------------------------

    private void SetupMenuButtonHoverAnimations()
    {
        foreach (Button button in EnumerateMenuButtons())
            SetupMenuButtonHoverAnimation(button);
    }

    private void SetupMenuButtonHoverAnimation(Button button)
    {
        if (button == null || _menuButtonVisuals.ContainsKey(button))
            return;

        var normal = DuplicateStyleBox(button, "normal");
        var hover = DuplicateStyleBox(button, "hover");
        var pressed = DuplicateStyleBox(button, "pressed");
        var focus = DuplicateStyleBox(button, "focus");
        var disabled = DuplicateStyleBox(button, "disabled");
        if (normal == null || hover == null)
            return;

        var visuals = new MenuButtonVisuals
        {
            Normal = normal,
            Hover = hover,
            Pressed = pressed,
            Focus = focus,
            Disabled = disabled,
            BaseLeftMargin = normal.ContentMarginLeft,
            Owner = button,
            PrimaryStyle = button == StartGameButton,
        };
        visuals.Arrow = button.GetNodeOrNull<Line2D>("Arrow");
        visuals.ArrowBasePosition = visuals.Arrow?.Position ?? Vector2.Zero;
        visuals.AccentLine = new ColorRect {
            Name = "AnimatedUnderline",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetTop = -2f,
            Color = new Color(0.86f, 0.90f, 0.94f, 0f),
        };
        button.AddChild(visuals.AccentLine);
        // Keep a quiet baseline; the animated child supplies hover and focus emphasis.
        hover.BorderColor = normal.BorderColor;
        hover.BorderWidthBottom = normal.BorderWidthBottom;
        if (focus != null)
            focus.BorderWidthBottom = 0;
        ApplyMenuButtonAccent(visuals, false);

        _menuButtonVisuals[button] = visuals;
        button.MouseEntered += () =>
        {
            if (!button.Disabled)
                AudioManager.PlayUiHover(this);
            AnimateMenuButtonLabel(visuals, true);
            if (visuals.PrimaryStyle)
                AnimateFieldEmphasis(true);
        };
        button.MouseExited += () =>
        {
            AnimateMenuButtonLabel(visuals, false);
            if (visuals.PrimaryStyle)
                AnimateFieldEmphasis(button.HasFocus());
        };
        button.FocusEntered += () => {
            AnimateMenuButtonLabel(visuals, true);
            if (visuals.PrimaryStyle)
                AnimateFieldEmphasis(true);
        };
        button.FocusExited += () => {
            bool hovered = button.GetGlobalRect().HasPoint(button.GetGlobalMousePosition());
            AnimateMenuButtonLabel(visuals, hovered);
            if (visuals.PrimaryStyle)
                AnimateFieldEmphasis(hovered);
        };
        button.Pressed += () => {
            visuals.ClickPulse = 1f;
            AudioManager.PlayUiClick(this);
        };
        button.TreeExiting += () =>
        {
            visuals.Tween?.Kill();
        };
    }

    private void UpdateMenuFeedback(float delta) {
        float hoverSmoothing = 1f - Mathf.Exp(-delta * 12f);
        float pressSmoothing = 1f - Mathf.Exp(-delta * 24f);
        foreach (MenuButtonVisuals visuals in _menuButtonVisuals.Values) {
            Button button = visuals.Owner;
            bool enabled = !button.Disabled && button.IsVisibleInTree();
            bool hovered = enabled && (button.IsHovered() || button.HasFocus());
            visuals.HoverAmount = Mathf.Lerp(visuals.HoverAmount, hovered ? 1f : 0f, hoverSmoothing);
            visuals.PressAmount = Mathf.Lerp(visuals.PressAmount, enabled && button.IsPressed() ? 1f : 0f, pressSmoothing);
            visuals.ClickPulse = enabled ? Mathf.Max(0f, visuals.ClickPulse - delta * 4f) : 0f;
            float amount = visuals.HoverAmount;
            visuals.AccentLine.AnchorRight = amount;
            visuals.AccentLine.OffsetTop = -2f - visuals.PressAmount;
            visuals.AccentLine.Color = new Color(0.86f, 0.90f, 0.94f,
                enabled ? Mathf.Min(1f, amount * 0.78f + visuals.ClickPulse * 0.22f + visuals.PressAmount * 0.15f) : 0f);
            if (visuals.Arrow != null) {
                visuals.Arrow.Position = visuals.ArrowBasePosition + new Vector2(amount * 6f + visuals.PressAmount * 3f, 0f);
                ApplyMenuButtonAccent(visuals, hovered);
            }
        }
    }

    private void AnimateFieldEmphasis(bool on)
    {
        if (FieldMaterial == null)
            return;

        _fieldEmphasisTween?.Kill();
        _fieldEmphasisTween = CreateTween();
        _fieldEmphasisTween.TweenProperty(
            FieldMaterial, "shader_parameter/emphasis", on ? 1f : 0f, 0.4f
        ).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
    }

    private void ApplyMenuButtonAccent(MenuButtonVisuals visuals, bool hovered)
    {
        if (visuals?.Arrow == null || !GodotObject.IsInstanceValid(visuals.Arrow))
            return;

        visuals.Arrow.DefaultColor = visuals.Owner.Disabled ? MarkerDisabled
            : visuals.PrimaryStyle ? PrimaryInk
            : hovered ? AccentSteel : TextDim;
    }

    private void RefreshMenuButtonAccents()
    {
        foreach (MenuButtonVisuals visuals in _menuButtonVisuals.Values)
            ApplyMenuButtonAccent(visuals, false);
    }

    private static StyleBoxFlat DuplicateStyleBox(Button button, string styleName)
    {
        if (button.GetThemeStylebox(styleName) is not StyleBoxFlat styleBox)
            return null;

        var duplicate = styleBox.Duplicate() as StyleBoxFlat;
        if (duplicate == null)
            return null;

        button.AddThemeStyleboxOverride(styleName, duplicate);
        return duplicate;
    }

    private void AnimateMenuButtonLabel(MenuButtonVisuals visuals, bool hovered)
    {
        if (visuals == null)
            return;

        hovered = !visuals.Owner.Disabled && (hovered || visuals.Owner.HasFocus());
        ApplyMenuButtonAccent(visuals, hovered);
        visuals.Tween?.Kill();

        float baseMargin = visuals.BaseLeftMargin;
        float targetMargin = hovered ? baseMargin + MenuButtonHoverTextOffset : baseMargin;
        if (!hovered && visuals.Normal != null && visuals.Hover != null)
            visuals.Normal.ContentMarginLeft = visuals.Hover.ContentMarginLeft;

        visuals.Tween = CreateTween();
        visuals.Tween.SetEase(Tween.EaseType.Out);
        visuals.Tween.SetTrans(Tween.TransitionType.Cubic);

        TweenStyleMarginLeft(visuals.Tween, visuals.Normal, targetMargin);
        TweenStyleMarginLeft(visuals.Tween, visuals.Hover, targetMargin);
        TweenStyleMarginLeft(visuals.Tween, visuals.Pressed, targetMargin);
        TweenStyleMarginLeft(visuals.Tween, visuals.Focus, targetMargin);
        TweenStyleMarginLeft(visuals.Tween, visuals.Disabled, baseMargin);
    }

    private static void TweenStyleMarginLeft(Tween tween, StyleBoxFlat styleBox, float targetMargin)
    {
        if (tween == null || styleBox == null)
            return;

        tween
            .Parallel()
            .TweenProperty(styleBox, "content_margin_left", targetMargin, MenuButtonHoverDuration);
    }
}
