using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class HandPreviewDebugRunner : Node
{
    private const int MaxSetupFrames = 720;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public override async void _Ready()
    {
        bool ok = false;
        try
        {
            GD.Print("[HandPreviewDebugRunner] start");
            SetupDebugGameInfo();

            Battle battle = CreateDebugBattle();
            AddChild(battle);
            await WaitFramesAsync(10);

            await EnsureDebugHandAsync(battle);
            Dictionary<string, object> result =
                await battle.CharacterControl.RunHandPreviewDebugScenarioAsync();

            ok =
                result.TryGetValue("ok", out object okValue)
                && okValue is bool okBool
                && okBool;

            GD.Print("[HandPreviewDebugRunner] result=");
            GD.Print(JsonSerializer.Serialize(result, JsonOptions));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HandPreviewDebugRunner] exception: {ex}");
        }
        finally
        {
            GD.Print($"[HandPreviewDebugRunner] {(ok ? "PASS" : "FAIL")}");
            GetTree()?.Quit(ok ? 0 : 1);
        }
    }

    private static void SetupDebugGameInfo()
    {
        BattleTutorialOverlay.MarkTutorialSeen();
        GameInfo.Seed = 4203;
        GameInfo.PlayerCharacters = BuildDebugRoster();
        GameInfo.NormalizePlayerCharacters();
        GameInfo.SeedTakenSkillsAsGained();
        GameInfo.InitNewGame();
        GameInfo.HasSeenBattleTutorial = true;
        Battle.Istest = true;
    }

    private static PlayerInfoStructure[] BuildDebugRoster()
    {
        var registry = new PlayerCharacterRegistry();
        return
        [
            CloneDebugPlayerInfo(registry.Echo, GameInfo.GetDefaultPlayerFormationPosition(0)),
            CloneDebugPlayerInfo(registry.Kasiya, GameInfo.GetDefaultPlayerFormationPosition(1)),
            CloneDebugPlayerInfo(registry.Nightingale, GameInfo.GetDefaultPlayerFormationPosition(2)),
        ];
    }

    private static PlayerInfoStructure CloneDebugPlayerInfo(
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

    private static Battle CreateDebugBattle()
    {
        PackedScene scene = GD.Load<PackedScene>("res://battle/Battle.tscn");
        Battle battle = scene.Instantiate<Battle>();
        battle.Name = "HandPreviewDebugBattle";
        battle.TestBattleTutorial = false;
        battle.BattleRandomNum = 13579;
        battle.CurrentLevelNode = new LevelNode
        {
            Type = LevelNode.LevelType.Normal,
            State = LevelNode.LevelState.Unlocked,
            EnemiesRegeditList = new List<EnemyRegedit>
            {
                new EvilRegedit
                {
                    PositionIndex = 2,
                    CurrentLife = -1,
                }.GetRegedit(),
            },
        };
        return battle;
    }

    private async Task EnsureDebugHandAsync(Battle battle)
    {
        for (int frame = 0; frame < MaxSetupFrames; frame++)
        {
            if (battle == null || !GodotObject.IsInstanceValid(battle))
                throw new InvalidOperationException("debug battle was freed during setup");

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

                if (HasVisibleDebugHandCard(control))
                    return;
            }

            await WaitFramesAsync(1);
        }

        throw new TimeoutException("timed out waiting for a visible hand card");
    }

    private static bool HasVisibleDebugHandCard(CharacterControl control)
    {
        Dictionary<string, object> state = control.GetDebugHandPreviewState();
        if (
            !state.TryGetValue("cards", out object cardsValue)
            || cardsValue is not IEnumerable<object> cards
        )
            return false;

        foreach (object cardValue in cards)
        {
            if (cardValue is not Dictionary<string, object> card)
                continue;

            bool visible =
                card.TryGetValue("visible", out object visibleValue)
                && visibleValue is bool visibleBool
                && visibleBool;
            if (!visible)
                continue;

            bool blocked =
                card.TryGetValue("drawEntryInputBlocked", out object blockedValue)
                && blockedValue is bool blockedBool
                && blockedBool;
            bool committed =
                card.TryGetValue("committed", out object committedValue)
                && committedValue is bool committedBool
                && committedBool;
            bool buttonDisabled =
                card.TryGetValue("buttonDisabled", out object buttonDisabledValue)
                && buttonDisabledValue is bool buttonDisabledBool
                && buttonDisabledBool;
            if (blocked || committed || buttonDisabled)
                continue;

            if (
                card.TryGetValue("skillPreview", out object previewValue)
                && previewValue is Dictionary<string, object> preview
                && preview.TryGetValue("hasSkill", out object hasSkillValue)
                && hasSkillValue is bool hasSkill
                && hasSkill
            )
            {
                return true;
            }
        }

        return false;
    }

    private async Task WaitFramesAsync(int count)
    {
        SceneTree tree = GetTree();
        if (tree == null)
            return;

        for (int i = 0; i < count; i++)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
