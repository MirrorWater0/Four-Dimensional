using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SteamScreenshotRunner : Node
{
    private const string OutputDirectory =
        "C:/godot_project/Four-Dimensional/tmp/steam_screenshots";
    private const int SetupFrameLimit = 720;

    private sealed record BattleShot(
        string FileName,
        LevelNode.LevelType LevelType,
        Func<List<EnemyRegedit>> BuildEnemies,
        int RosterOffset
    );

    public override async void _Ready()
    {
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            GetTree().Root.Size = new Vector2I(1920, 1080);
            HideDebugOverlay();

            BattleShot[] shots =
            [
                new(
                    "01_battle_evil.jpg",
                    LevelNode.LevelType.Normal,
                    () => [BuildEnemy<EvilRegedit>(2)],
                    0
                ),
                new(
                    "02_battle_enemy_squad.jpg",
                    LevelNode.LevelType.Normal,
                    () =>
                    [
                        BuildEnemy<RedHuskRegedit>(1),
                        BuildEnemy<VoidAcolyteRegedit>(2),
                        BuildEnemy<HollowBulwarkRegedit>(3),
                    ],
                    1
                ),
                new(
                    "03_battle_anger_elite.jpg",
                    LevelNode.LevelType.Elite,
                    () => [BuildEnemy<AngerEliteRegedit>(2)],
                    2
                ),
                new(
                    "04_battle_fearworm_turbine.jpg",
                    LevelNode.LevelType.Normal,
                    () =>
                    [
                        BuildEnemy<FearWormRegedit>(1),
                        BuildEnemy<TurbineRegedit>(3),
                    ],
                    3
                ),
                new(
                    "05_battle_war_boss.jpg",
                    LevelNode.LevelType.Boss,
                    () => [BuildEnemy<WarRegedit>(Battle.MaxEnemyFormationSlots)],
                    0
                ),
            ];

            foreach (BattleShot shot in shots)
                await CaptureBattleAsync(shot);

            GD.Print($"[SteamScreenshotRunner] completed: {OutputDirectory}");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SteamScreenshotRunner] failed: {ex}");
            GetTree().Quit(1);
        }
    }

    private async Task CaptureBattleAsync(BattleShot shot)
    {
        SetupGameInfo(shot.RosterOffset);

        PackedScene scene = GD.Load<PackedScene>("res://battle/Battle.tscn");
        Battle battle = scene.Instantiate<Battle>();
        battle.Name = "SteamScreenshotBattle";
        battle.TestBattleTutorial = false;
        battle.BattleRandomNum = 13579 + shot.RosterOffset * 97;
        battle.CurrentLevelNode = new LevelNode
        {
            Type = shot.LevelType,
            State = LevelNode.LevelState.Unlocked,
            EnemiesRegeditList = shot.BuildEnemies(),
        };

        AddChild(battle);
        await battle.WhenPresentationReadyAsync();
        await EnsureVisibleHandAsync(battle);
        await WaitFramesAsync(24);
        await CaptureViewportAsync(Path.Combine(OutputDirectory, shot.FileName));

        battle.QueueFree();
        await WaitFramesAsync(6);
    }

    private static void SetupGameInfo(int rosterOffset)
    {
        BattleTutorialOverlay.MarkTutorialSeen();
        GameInfo.Seed = 4203 + rosterOffset * 101;
        GameInfo.PlayerCharacters = BuildRoster(rosterOffset);
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        GameInfo.InitNewGame();
        GameInfo.HasSeenBattleTutorial = true;
        Battle.Istest = true;
    }

    private static PlayerInfoStructure[] BuildRoster(int offset)
    {
        var registry = new PlayerCharacterRegistry();
        PlayerInfoStructure[] pool =
        [
            registry.Echo,
            registry.Kasiya,
            registry.Mariya,
            registry.Nightingale,
        ];

        return Enumerable
            .Range(0, GameInfo.DefaultPlayerPartySize)
            .Select(index =>
            {
                PlayerInfoStructure source = pool[(index + offset) % pool.Length];
                return ClonePlayerInfo(
                    source,
                    GameInfo.GetDefaultPlayerFormationPosition(index)
                );
            })
            .ToArray();
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

    private static EnemyRegedit BuildEnemy<T>(int positionIndex)
        where T : EnemyRegedit, new()
    {
        EnemyRegedit enemy = new T().GetRegedit();
        enemy.PositionIndex = positionIndex;
        enemy.CurrentLife = -1;
        return enemy;
    }

    private async Task EnsureVisibleHandAsync(Battle battle)
    {
        for (int frame = 0; frame < SetupFrameLimit; frame++)
        {
            if (battle == null || !GodotObject.IsInstanceValid(battle))
                throw new InvalidOperationException("battle was freed during screenshot setup");

            CharacterControl control = battle.CharacterControl;
            if (
                battle.PlayersList.Count > 0
                && control != null
                && GodotObject.IsInstanceValid(control)
            )
            {
                if (frame == 30 || frame == 90 || frame == 180)
                {
                    battle.BeginPlayerTeamAction();
                    battle.TryDrawPlayerTeamBattleCards(8);
                    control.RefreshCurrentTurnUi();
                }

                if (HasVisibleHandCard(control))
                    return;
            }

            await WaitFramesAsync(1);
        }

        throw new TimeoutException("timed out waiting for a visible battle hand");
    }

    private static bool HasVisibleHandCard(CharacterControl control)
    {
        Dictionary<string, object> state = control.GetDebugHandPreviewState();
        if (
            !state.TryGetValue("cards", out object cardsValue)
            || cardsValue is not IEnumerable<object> cards
        )
            return false;

        foreach (object cardValue in cards)
        {
            if (
                cardValue is Dictionary<string, object> card
                && card.TryGetValue("visible", out object visibleValue)
                && visibleValue is bool visible
                && visible
            )
                return true;
        }

        return false;
    }

    private async Task CaptureViewportAsync(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = GetViewport().GetTexture().GetImage();
        if (image == null || image.IsEmpty())
            throw new InvalidOperationException($"viewport image was empty for {path}");

        Error error = image.SaveJpg(path, 0.95f);
        if (error != Error.Ok)
            throw new IOException($"failed to save {path}: {error}");

        GD.Print($"[SteamScreenshotRunner] saved {path} ({image.GetWidth()}x{image.GetHeight()})");
    }

    private void HideDebugOverlay()
    {
        if (GetNodeOrNull<CanvasLayer>("/root/MouseTrail") is { } mouseTrail)
            mouseTrail.Visible = false;
    }

    private async Task WaitFramesAsync(int count)
    {
        SceneTree tree = GetTree();
        for (int i = 0; i < count; i++)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
