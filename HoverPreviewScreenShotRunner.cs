using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Headless preview runner for the hand-card hover forecast (target frame + effect readout).
/// Hovers a few different cards so damage / block / heal rows can all be reviewed.
/// </summary>
public partial class HoverPreviewScreenShotRunner : Node
{
    private const string OutputDirectory = "C:/godot_project/Four-Dimensional/tmp/hover_preview";
    private const int SetupFrameLimit = 900;

    public override async void _Ready()
    {
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            GetTree().Root.Size = new Vector2I(1920, 1080);

            if (GetNodeOrNull<CanvasLayer>("/root/MouseTrail") is { } mouseTrail)
                mouseTrail.Visible = false;

            SetupGameInfo();

            Battle battle = CreateBattle();
            AddChild(battle);
            await WaitFramesAsync(10);

            GetWindow().GrabFocus();
            GetTree().Root.PropagateNotification((int)MainLoop.NotificationApplicationFocusIn);

            await EnsureHandAsync(battle);
            await WaitFramesAsync(40);

            CharacterControl control = battle.CharacterControl;
            int captured = 0;
            for (int index = 0; index < 8 && captured < 4; index++)
            {
                SkillCard card = control.GetCardSlot(index);
                if (card == null || !GodotObject.IsInstanceValid(card) || !card.Visible)
                    continue;

                card.ShowSkillPreview();
                await WaitFramesAsync(24);

                string name = card.CurrentSkill?.SkillName ?? $"card{index}";
                await CaptureAsync($"{captured + 1:00}_{Sanitize(name)}.png");
                captured++;

                card.HideSkillPreview();
                await WaitFramesAsync(6);
            }

            await CaptureLockPaletteAsync(battle);

            GD.Print($"[HoverPreviewScreenShotRunner] captured {captured} shots: {OutputDirectory}");
            GetTree().Quit(captured > 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HoverPreviewScreenShotRunner] failed: {ex}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Side-by-side shot of the hostile and friendly target-lock palettes.</summary>
    private async Task CaptureLockPaletteAsync(Battle battle)
    {
        var hostile = new Color(1f, 0.32f, 0.32f, 1f);
        var friendly = new Color(0.48f, 0.82f, 0.62f, 0.82f);

        var locked = new List<Character>();
        for (int i = 0; i < battle.PlayersList.Count && i < 2; i++)
        {
            Character player = battle.PlayersList[i];
            if (!GodotObject.IsInstanceValid(player))
                continue;

            player.ShowTargetPreview(i == 0 ? friendly : hostile);
            locked.Add(player);
        }

        foreach (Character enemy in battle.EnemiesList)
        {
            if (!GodotObject.IsInstanceValid(enemy))
                continue;

            enemy.ShowTargetPreview(hostile);
            locked.Add(enemy);
        }

        await WaitFramesAsync(30);
        await CaptureAsync("05_lock_palette.png");

        foreach (Character character in locked)
        {
            if (GodotObject.IsInstanceValid(character))
                character.HideTargetPreview();
        }

        // Let the take-apart finish, otherwise the next build-up starts from a fully
        // assembled frame and the animation is invisible in the stills.
        await WaitFramesAsync(30);

        // Plain inspect frame (mouse hover / radial frame preview), no target lock.
        var inspected = new List<Character>();
        foreach (Character player in battle.PlayersList)
        {
            if (!GodotObject.IsInstanceValid(player))
                continue;

            player.ShowFramePreview();
            inspected.Add(player);
        }

        // Two points on the build-up curve plus the settled frame, so the assemble reads in stills.
        await WaitFramesAsync(4);
        await CaptureAsync("06a_assemble_early.png");
        await WaitFramesAsync(5);
        await CaptureAsync("06b_assemble_mid.png");
        await WaitFramesAsync(30);
        await CaptureAsync("06c_inspect_frame.png");

        foreach (Character character in inspected)
        {
            if (GodotObject.IsInstanceValid(character))
                character.HideFramePreview();
        }

        await WaitFramesAsync(5);
        await CaptureAsync("06d_disassemble.png");
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private static void SetupGameInfo()
    {
        BattleTutorialOverlay.MarkTutorialSeen();
        GameInfo.Seed = 4203;
        var registry = new PlayerCharacterRegistry();
        GameInfo.PlayerCharacters =
        [
            Clone(registry.Echo, GameInfo.GetDefaultPlayerFormationPosition(0)),
            Clone(registry.Kasiya, GameInfo.GetDefaultPlayerFormationPosition(1)),
            Clone(registry.Mariya, GameInfo.GetDefaultPlayerFormationPosition(2)),
        ];
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        GameInfo.InitNewGame();
        GameInfo.HasSeenBattleTutorial = true;
        Battle.Istest = true;
    }

    private static PlayerInfoStructure Clone(PlayerInfoStructure source, int positionIndex)
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

    private static Battle CreateBattle()
    {
        PackedScene scene = GD.Load<PackedScene>("res://battle/Battle.tscn");
        Battle battle = scene.Instantiate<Battle>();
        battle.Name = "HoverPreviewBattle";
        battle.TestBattleTutorial = false;
        battle.BattleRandomNum = 13579;
        battle.CurrentLevelNode = new LevelNode
        {
            Type = LevelNode.LevelType.Normal,
            State = LevelNode.LevelState.Unlocked,
            EnemiesRegeditList = new List<EnemyRegedit>
            {
                new EvilRegedit { PositionIndex = 2, CurrentLife = -1 }.GetRegedit(),
            },
        };
        return battle;
    }

    private async Task EnsureHandAsync(Battle battle)
    {
        for (int frame = 0; frame < SetupFrameLimit; frame++)
        {
            CharacterControl control = battle.CharacterControl;
            if (
                battle.PlayersList.Count > 0
                && control != null
                && GodotObject.IsInstanceValid(control)
            )
            {
                if (frame == 30 || frame == 120 || frame == 240)
                {
                    battle.BeginPlayerTeamAction();
                    battle.TryDrawPlayerTeamBattleCards(8);
                    control.RefreshCurrentTurnUi();
                }

                if (
                    control.GetCardSlot(0) is { } first
                    && GodotObject.IsInstanceValid(first)
                    && first.Visible
                )
                {
                    return;
                }
            }

            await WaitFramesAsync(1);
        }

        throw new TimeoutException("timed out waiting for a visible hand card");
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
            GD.PrintErr($"[HoverPreviewScreenShotRunner] save failed ({error}): {path}");
        else
            GD.Print($"[HoverPreviewScreenShotRunner] saved {path}");
    }
}
