using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class StartInterface : CanvasLayer
{
    private const string AutosavePath = "user://autosave.cfg";
    private const int RequiredCharacterSelectionCount = 4;
    private const float MenuButtonHoverTextOffset = 20f;
    private const float MenuButtonHoverDuration = 0.12f;

    private sealed class MenuButtonVisuals
    {
        public StyleBoxFlat Normal;
        public StyleBoxFlat Hover;
        public StyleBoxFlat Pressed;
        public StyleBoxFlat Focus;
        public StyleBoxFlat Disabled;
        public float BaseLeftMargin;
        public Tween Tween;
    }

    public static PackedScene TipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
    public static PackedScene _Echo = (PackedScene)
        ResourceLoader.Load("res://character/PlayerCharacter/Echo/Echo.tscn");
    public static PackedScene _Kasiya = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Kasiya/kasiya.tscn"
    );
    public static PackedScene _Mariya = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Mariya/Mariya.tscn"
    );
    public static PackedScene _Nightingale = ResourceLoader.Load<PackedScene>(
        "res://character/PlayerCharacter/Nightingale/Nightingale.tscn"
    );
    private const string CharacterSelectionOverlayScenePath =
        "res://BeginGame/CharacterSelectionOverlay.tscn";
    private Button StartGameButton =>
        field ??= GetNodeOrNull<Button>("Layout/CenterPanel/Margin/VBox/Buttons/Button");
    private Button ContinueGameButton =>
        field ??= GetNodeOrNull<Button>("Layout/CenterPanel/Margin/VBox/Buttons/Button2");
    private Button StatisticsButton =>
        field ??= GetNodeOrNull<Button>("Layout/CenterPanel/Margin/VBox/Buttons/StatisticsButton");
    private Button ExitGameButton =>
        field ??= GetNodeOrNull<Button>("Layout/CenterPanel/Margin/VBox/Buttons/Button3");
    private Label StatusLine =>
        field ??= GetNodeOrNull<Label>("Layout/CenterPanel/Margin/VBox/StatusLine");
    private readonly Dictionary<Button, MenuButtonVisuals> _menuButtonVisuals = new();
    private CharacterSelectionOverlay CharacterSelection =>
        GetNodeOrNull<CharacterSelectionOverlay>("CharacterSelectionOverlay");

    public override void _Ready()
    {
        GetTree().Root.GetNodeOrNull<GameOverSummary>("GameOverSummary")?.QueueFree();

        TryLoadAutosaveForMenu();
        RefreshContinueButtonState();
        SetupMenuButtonHoverAnimations();

        if (StartGameButton != null)
            StartGameButton.Pressed += OpenCharacterSelection;

        if (StatisticsButton != null)
            StatisticsButton.Pressed += ShowStatistics;

        if (ExitGameButton != null)
            ExitGameButton.Pressed += ExitGame;

        var existingLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
        {
            if (!existingLayer.HasNode("Tip"))
            {
                var tip0 = TipScene.Instantiate<Tip>();
                tip0.Name = "Tip";
                tip0.FollowMouse = true;
                tip0.AnchorOffset = new Vector2(20f, 20f);
                existingLayer.AddChild(tip0);
            }

            if (!existingLayer.HasNode("BuffTip"))
            {
                var buffTip0 = TipScene.Instantiate<Tip>();
                buffTip0.Name = "BuffTip";
                buffTip0.FollowMouse = true;
                buffTip0.AnchorOffset = new Vector2(-20f, 20f);
                existingLayer.AddChild(buffTip0);
            }

            return;
        }

        CanvasLayer layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        var tip = TipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 20f);

        var buffTip = TipScene.Instantiate<Tip>();
        buffTip.Name = "BuffTip";
        buffTip.FollowMouse = true;
        buffTip.AnchorOffset = new Vector2(-20f, 20f);

        layer.AddChild(tip);
        layer.AddChild(buffTip);
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, layer);
    }

    public void NewStart()
    {
        GameInfo.PlayerCharacters = BuildDefaultRoster();
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
        try
        {
            SaveSystem.LoadAll();
        }
        catch (Exception e)
        {
            GD.PushError($"ContinueGame load failed: {e}");
            return;
        }

        if (GameInfo.RunFinished)
        {
            RefreshContinueButtonState();
            return;
        }

        SceneTransitionLayer.Ensure(this)?.SwitchScene("res://Map/Map.tscn");
    }

    public void falseTest()
    {
        Battle.Istest = false;
        Start();
    }

    private void ExitGame()
    {
        GetTree().Quit();
    }

    private void OpenCharacterSelection()
    {
        if (CharacterSelection != null)
            return;

        var scene = GD.Load<PackedScene>(CharacterSelectionOverlayScenePath);
        if (scene == null)
        {
            if (StatusLine != null)
                StatusLine.Text = "角色选择界面加载失败，请检查 CharacterSelectionOverlay.tscn。";
            GD.PushError(
                $"Character selection scene failed to load: {CharacterSelectionOverlayScenePath}"
            );
            return;
        }

        var overlay = scene.Instantiate<CharacterSelectionOverlay>();
        if (overlay == null)
        {
            if (StatusLine != null)
                StatusLine.Text = "角色选择界面实例化失败。";
            GD.PushError("Character selection scene failed to instantiate.");
            return;
        }

        overlay.Name = "CharacterSelectionOverlay";
        AddChild(overlay);
        overlay.Open(
            BuildDefaultRoster(),
            RequiredCharacterSelectionCount,
            BeginNewRunFromSelection
        );
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
            LifeMax = source.LifeMax,
            Power = source.Power,
            Survivability = source.Survivability,
            Speed = source.Speed,
            TalentPoints = source.TalentPoints,
            UnlockedTalents =
                source.UnlockedTalents != null
                    ? new List<string>(source.UnlockedTalents)
                    : new List<string>(),
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

    private void TryLoadAutosaveForMenu()
    {
        if (!FileAccess.FileExists(AutosavePath))
            return;

        try
        {
            SaveSystem.LoadAll();
        }
        catch (Exception e)
        {
            GD.PushError($"StartInterface autosave preload failed: {e}");
        }
    }

    private void RefreshContinueButtonState()
    {
        if (ContinueGameButton == null)
            return;

        bool hasAutosave = FileAccess.FileExists(AutosavePath);
        ContinueGameButton.Disabled = !hasAutosave || GameInfo.RunFinished;

        if (StatusLine == null)
            return;

        StatusLine.Text =
            !hasAutosave ? "未检测到自动存档，只能开始新游戏。"
            : GameInfo.RunFinished ? "当前自动存档已结算完成，请开始新一轮游戏。"
            : "检测到自动存档，可以继续上一次游戏。";
    }

    private void SetupMenuButtonHoverAnimations()
    {
        SetupMenuButtonHoverAnimation(StartGameButton);
        SetupMenuButtonHoverAnimation(ContinueGameButton);
        SetupMenuButtonHoverAnimation(StatisticsButton);
        SetupMenuButtonHoverAnimation(ExitGameButton);
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
        };

        _menuButtonVisuals[button] = visuals;
        button.MouseEntered += () => AnimateMenuButtonLabel(visuals, true);
        button.MouseExited += () => AnimateMenuButtonLabel(visuals, false);
        button.TreeExiting += () => visuals.Tween?.Kill();
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
