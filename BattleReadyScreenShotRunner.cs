using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Headless preview runner for the pre-battle tactical screen (BattleReady).
/// Captures one shot per available mode so UI work can be reviewed without playing to a map node.
/// </summary>
public partial class BattleReadyScreenShotRunner : Node
{
    private const string OutputDirectory = "C:/godot_project/Four-Dimensional/tmp/battle_ready";
    private const int SettleFrames = 150;

    private const string ModeButtonRoot = "ModeSelectorRoot/ModeButtonsMargin/ModeButtons";

    private sealed record ModeShot(string FileName, string ButtonPath);

    public override async void _Ready()
    {
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            GetTree().Root.Size = new Vector2I(1920, 1080);

            if (GetNodeOrNull<CanvasLayer>("/root/MouseTrail") is { } mouseTrail)
                mouseTrail.Visible = false;

            SetupPreviewGameInfo();

            var scene = GD.Load<PackedScene>("res://battle/UIScene/BattleReady/BattleReady.tscn");
            if (scene == null)
            {
                GD.PrintErr("[BattleReadyScreenShotRunner] BattleReady.tscn failed to load.");
                GetTree().Quit(1);
                return;
            }

            Control ready = scene.Instantiate<Control>();
            AddChild(ready);

            SceneTree tree = GetTree();
            for (int i = 0; i < 20; i++)
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            // Background subviewports pause while the window is unfocused; automated runs
            // often never receive focus, so broadcast a synthetic focus-in like the other runners.
            GetWindow().GrabFocus();
            tree.Root.PropagateNotification((int)MainLoop.NotificationApplicationFocusIn);

            ready.Call("StartAnimation");
            await WaitFramesAsync(SettleFrames);
            await CaptureAsync("01_tactics.png");

            ModeShot[] shots =
            [
                new("02_talent.png", $"{ModeButtonRoot}/TalentModeButton"),
                new("03_tactics_back.png", $"{ModeButtonRoot}/TacticsModeButton"),
            ];

            foreach (ModeShot shot in shots)
            {
                if (ready.GetNodeOrNull<Button>(shot.ButtonPath) is not { } button)
                {
                    GD.PrintErr($"[BattleReadyScreenShotRunner] missing button: {shot.ButtonPath}");
                    continue;
                }

                button.EmitSignal(BaseButton.SignalName.Pressed);
                await WaitFramesAsync(SettleFrames);
                await CaptureAsync(shot.FileName);
            }

            GD.Print($"[BattleReadyScreenShotRunner] completed: {OutputDirectory}");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BattleReadyScreenShotRunner] failed: {ex}");
            GetTree().Quit(1);
        }
    }

    private static void SetupPreviewGameInfo()
    {
        var registry = new PlayerCharacterRegistry();
        GameInfo.Seed = 4203;
        GameInfo.PlayerCharacters =
        [
            ClonePlayerInfo(registry.Echo, GameInfo.GetDefaultPlayerFormationPosition(0)),
            ClonePlayerInfo(registry.Kasiya, GameInfo.GetDefaultPlayerFormationPosition(1)),
            ClonePlayerInfo(registry.Mariya, GameInfo.GetDefaultPlayerFormationPosition(2)),
            ClonePlayerInfo(registry.Nightingale, GameInfo.GetDefaultPlayerFormationPosition(3)),
        ];
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        GameInfo.InitNewGame();
    }

    private static PlayerInfoStructure ClonePlayerInfo(
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
            TalentPoints = Math.Max(source.TalentPoints, 3),
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

    private async Task WaitFramesAsync(int count)
    {
        SceneTree tree = GetTree();
        if (tree == null)
            return;

        for (int i = 0; i < count; i++)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private async Task CaptureAsync(string fileName)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = GetViewport().GetTexture().GetImage();
        string path = Path.Combine(OutputDirectory, fileName);
        Error error = image.SavePng(path);
        if (error != Error.Ok)
            GD.PrintErr($"[BattleReadyScreenShotRunner] save failed ({error}): {path}");
        else
            GD.Print($"[BattleReadyScreenShotRunner] saved {path}");
    }
}
