using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

// Run this scene explicitly for archive visual/interaction checks. Fixtures stay in memory.
public partial class HistoryScreenShotRunner : Node {
    private const string Content = "CenterPanel/Detail/Scroll/Content";
    private string _output;
    private GameStatistics _archive;

    public override async void _Ready() {
        List<RunHistoryRecord> original = null;
        try {
            GetWindow().Mode = Window.ModeEnum.Windowed;
            int width = OS.GetEnvironment("FD_HISTORY_WIDTH") == "1280" ? 1280 : 1920;
            GetWindow().Size = new Vector2I(width, width * 9 / 16);
            I18n.SetLocale(OS.GetEnvironment("FD_HISTORY_LOCALE") == "en" ? "en" : "zh_CN");
            AddChild(GD.Load<PackedScene>("res://BeginGame/StartInterface.tscn").Instantiate());
            await Frames(100);
            GetWindow().Size = new Vector2I(width, width * 9 / 16);
            await Frames(20);
            if (GetNodeOrNull<CanvasLayer>("/root/MouseTrail") is { } trail) trail.Visible = false;
            original = GameInfo.RunHistoryRecords;
            string mode = OS.GetEnvironment("FD_HISTORY_MODE");
            if (mode == "empty") GameInfo.RunHistoryRecords = new List<RunHistoryRecord>();
            if (mode == "dense") GameInfo.RunHistoryRecords = DenseFixtures();
            _output = ProjectSettings.GlobalizePath($"res://tmp/history_redesign/{(string.IsNullOrEmpty(mode) ? "actual" : mode)}-{width}-{TranslationServer.GetLocale()}");
            Directory.CreateDirectory(Path.GetDirectoryName(_output));
            Click(GetNode<Button>("StartInterface/Layout/Masthead/Utilities/Statistics"));
            await Frames(90);
            _archive = GetNode<GameStatistics>("/root/GameStatistics");
            CheckLayout();
            await Capture("overview");
            var records = GameInfo.RunHistoryRecords;
            if (records.Count > 1) {
                var directory = _archive.GetNode<VBoxContainer>("CenterPanel/Sidebar/HistoryScroll/HistoryList");
                Click(directory.GetNode<Button>("Run_" + (records.Count - 2)));
                await Frames(35);
                Assert(_archive.GetNode<Label>("CenterPanel/Sidebar/PageIndex").Text.StartsWith($"{records.Count - 1:00}"), "Directory selection failed");
                Click(_archive.GetNode<Button>("NextHistoryButton"));
                await Frames(35);
                Assert(_archive.GetNode<Button>("NextHistoryButton").Disabled, "Latest record navigation not disabled");
                // Scroll the directory to the oldest record and select it through real GUI input.
                var directoryScroll = _archive.GetNode<ScrollContainer>("CenterPanel/Sidebar/HistoryScroll");
                directoryScroll.ScrollVertical = 100000;
                await Frames(5);
                Click(directory.GetNode<Button>("Run_0"));
                await Frames(35);
                Assert(_archive.GetNode<Button>("PreviousHistoryButton").Disabled, "Oldest record navigation not disabled");
                // Reopen returns to the latest record and a fresh directory.
                _archive.Open();
                await Frames(45);
            }
            if (records.Count > 0) {
                var scroll = _archive.GetNode<ScrollContainer>("CenterPanel/Detail/Scroll");
                scroll.ScrollVertical = 100000;
                await Frames(8);
                var selector = _archive.GetNode<HBoxContainer>(Content + "/SkillSection/SkillMargin/SkillVBox/CharacterSwitch/CharacterSelectPanel/CharacterButtonList");
                if (selector.GetChildCount() > 1 && selector.GetChild(1) is Button character) {
                    // Bring the tabs into view even for exceptionally long skill lists.
                    scroll.EnsureControlVisible(character);
                    await Frames(5);
                    Click(character);
                    await Frames(25);
                    Assert(character.ButtonPressed, "Character selection failed");
                    Assert(!((Button)selector.GetChild(0)).ButtonPressed, "Previous character stayed selected");
                }
                await Capture("skills");
                CheckLayout();
                if (string.IsNullOrEmpty(mode)) {
                    var columns = _archive.GetNode<HBoxContainer>(Content + "/SkillSection/SkillMargin/SkillVBox/SkillColumns");
                    var cardColumn = columns.GetChild(1).GetChild(0).GetChild(0);
                    var flow = cardColumn.GetChild(cardColumn.GetChildCount() - 1);
                    if (flow.GetChildCount() > 0 && flow.GetChild(0) is PanelContainer pill) {
                        scroll.EnsureControlVisible(pill);
                        await Frames(5);
                        Click(pill, MouseButton.Right);
                        await Frames(25);
                        Assert(_archive.GetNodeOrNull<Control>("SkillCardPreviewRoot")?.Visible == true, "Right click card preview failed");
                        GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
                        await Frames(5);
                        Assert(!_archive.GetNode<Control>("SkillCardPreviewRoot").Visible, "Escape did not dismiss preview");
                    }
                }
                // Switching records after scrolling must reset the scroll without displacing content.
                if (records.Count > 1) {
                    Click(_archive.GetNode<Button>("PreviousHistoryButton"));
                    await Frames(35);
                    Assert(scroll.ScrollVertical == 0, "Record switch did not reset scroll");
                    Assert(Mathf.Abs(_archive.GetNode<Control>(Content).Position.Y) < 1f, "Content shifted after record switch");
                }
            } else {
                Assert(_archive.GetNode<Button>("PreviousHistoryButton").Disabled && _archive.GetNode<Button>("NextHistoryButton").Disabled, "Empty archive navigation enabled");
                Assert(!_archive.GetNode<Control>(Content + "/SkillSection").Visible, "Empty archive shows skill section");
            }
            Click(_archive.GetNode<Button>("ExitButton"));
            await Frames(25);
            Assert(GetNodeOrNull("/root/GameStatistics") == null, "Back button failed");
            _archive = GameStatistics.Show(this);
            await Frames(30);
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
            await Frames(25);
            Assert(GetNodeOrNull("/root/GameStatistics") == null, "Escape did not close archive");
            GD.Print("[HistoryPreview] PASS: layout, directory selection, boundaries, character tabs, scroll reset, back; mode=" + mode);
            GetTree().Quit();
        } catch (Exception ex) {
            GD.PrintErr("[HistoryPreview] FAIL: " + ex);
            GetTree().Quit(1);
        } finally {
            if (original != null) GameInfo.RunHistoryRecords = original;
        }
    }

    private void CheckLayout() {
        Rect2 bounds = _archive.GetNode<Control>("CenterPanel").GetGlobalRect();
        foreach (string path in new[] { "CenterPanel/Sidebar", "CenterPanel/Detail", "ExitButton", "SeedCopyButton", "PreviousHistoryButton", "NextHistoryButton" })
            Assert(bounds.Encloses(_archive.GetNode<Control>(path).GetGlobalRect()), "Outside viewport: " + path);
        var title = _archive.GetNode<Label>("CenterPanel/Sidebar/ArchiveTitle");
        var subtitle = _archive.GetNode<Label>("CenterPanel/Sidebar/Title");
        Assert(!title.GetGlobalRect().Intersects(subtitle.GetGlobalRect()), "Sidebar titles overlap");
        var scroll = _archive.GetNode<ScrollContainer>("CenterPanel/Detail/Scroll");
        Assert(_archive.GetNode<Control>(Content).Size.X <= scroll.Size.X + 1f, "Detail content overflows horizontally");
    }

    private void Click(Control control, MouseButton button = MouseButton.Left) {
        Vector2 position = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = button, Pressed = pressed }, true);
    }

    private async Task Frames(int count) {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Capture(string suffix) {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = GetViewport().GetTexture().GetImage();
        Assert(image.SaveJpg(_output + "-" + suffix + ".jpg", 0.95f) == Error.Ok, "Screenshot failed");
    }

    private static void Assert(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static List<RunHistoryRecord> DenseFixtures() {
        var record = new RunHistoryRecord { RunIndex = 24, Victory = true, Seed = 2147483647, Difficulty = 12, SessionPlaySeconds = 42356, NodesVisited = 70, EnemiesDefeated = 148, EliteDefeated = 16, BossDefeated = 4, ElectricityCoinGained = 123456, RelicGained = 0 };
        for (int i = 0; i < 70; i++)
            record.NodeRecords.Add(new LevelNodeCompletionRecord { MapLevel = i / 35, CompletionOrder = i, NodeType = i % 7 == 0 ? LevelNode.LevelType.Boss : LevelNode.LevelType.Normal });
        foreach (string name in new[] { "回声", "卡西娅", "玛瑞娅", "夜莺" }) {
            var character = new RunHistoryCharacterSkillRecord { CharacterName = name, MaxLife = 180, Power = 35, Survivability = 22, TalentPoints = 4 };
            foreach (var type in new[] { Skill.SkillTypes.Attack, Skill.SkillTypes.Survive, Skill.SkillTypes.Special, Skill.SkillTypes.Ability }) {
                var skills = new RunHistorySkillTypeRecord { SkillType = type };
                for (int i = 0; i < 9; i++) skills.SkillNames.Add($"A long archived skill name / 技能 {i + 1}");
                character.SkillTypeRecords.Add(skills);
            }
            record.CharacterSkillRecords.Add(character);
        }
        return new List<RunHistoryRecord> { new RunHistoryRecord { RunIndex = 1, SessionPlaySeconds = 4 }, record };
    }
}
