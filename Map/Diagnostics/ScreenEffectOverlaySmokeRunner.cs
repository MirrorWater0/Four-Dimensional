using System;
using System.IO;
using Godot;

/// <summary>
/// Command-line smoke test for the real Map/ScreenEffectOverlay heal pulse.
/// A process-frame state machine is deliberately used here: it also works under
/// headless command-line runs where async scene setup can be interrupted.
/// </summary>
public partial class ScreenEffectOverlaySmokeRunner : Node
{
    private const string OutputDirectory = "C:/godot_project/Four-Dimensional/tmp/screen_effect_smoke";

    private Map _map;
    private ScreenEffectOverlay _overlay;
    private NinePatchRect _healTint;
    private int _frame;
    private bool _triggered;
    private bool _peakCaptured;
    private double _pulseElapsedSeconds;

    public override void _Ready()
    {
        try
        {
            GetTree().Root.Size = new Vector2I(1920, 1080);
            SetupGameInfo();
            PackedScene mapScene = GD.Load<PackedScene>("res://Map/Map.tscn");
            if (mapScene == null)
                throw new InvalidOperationException("Map.tscn failed to load.");

            _map = mapScene.Instantiate<Map>();
            CallDeferred(Node.MethodName.AddChild, _map);
            GD.Print("[ScreenEffectSmoke] RUNNER_READY map queued for add.");
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    public override void _Process(double delta)
    {
        if (_map == null || !GodotObject.IsInstanceValid(_map))
            return;

        _frame++;
        if (_frame == 10)
        {
            _overlay = _map.GetNodeOrNull<ScreenEffectOverlay>("ScreenEffectOverlay");
            _healTint = _overlay?.GetNodeOrNull<NinePatchRect>("HealTint");
            if (_overlay == null || _healTint == null)
            {
                Fail(new InvalidOperationException("Map/ScreenEffectOverlay/HealTint is missing."));
                return;
            }

            Directory.CreateDirectory(OutputDirectory);
            GD.Print(
                $"[ScreenEffectSmoke] BEFORE {Describe()} "
                    + $"partyLife={GameInfo.GetPartyLife()}/{GameInfo.GetPartyMaxLife()}"
            );
            // Exercise Vitality Capsule's real acquisition flow. It raises LifeMax
            // and directly restores each member's Life, which previously bypassed
            // the regular GameInfo healing entry points.
            Relic.RelicAdd(_map.PlayerResourceState, RelicID.VitalityCapsule);
            GD.Print(
                $"[ScreenEffectSmoke] AFTER_VITALITY_CAPSULE "
                    + $"partyLife={GameInfo.GetPartyLife()}/{GameInfo.GetPartyMaxLife()}"
            );
            _triggered = true;
            return;
        }

        if (!_triggered)
            return;

        _pulseElapsedSeconds += delta;
        // Wait until the intentionally soft heal fade-in has completed before
        // sampling the peak. Frame-count based sampling was too early once the
        // pulse was made gentler.
        if (!_peakCaptured && _pulseElapsedSeconds >= 0.28d)
        {
            GD.Print($"[ScreenEffectSmoke] PEAK {Describe()}");
            Capture("peak.png");
            _peakCaptured = true;
            return;
        }

        if (_peakCaptured && _pulseElapsedSeconds >= 1.05d)
        {
            GD.Print($"[ScreenEffectSmoke] AFTER {Describe()}");
            Capture("after.png");
            GD.Print($"[ScreenEffectSmoke] PASS output={OutputDirectory}");
            GetTree().Quit(0);
        }
    }

    private static void SetupGameInfo()
    {
        var registry = new PlayerCharacterRegistry();
        GameInfo.InitNewGame();
        GameInfo.PendingStarterBonusChoice = false;
        GameInfo.PendingBossRelicChoice = false;
        GameInfo.PlayerCharacters =
        [
            ClonePlayer(registry.Echo),
            ClonePlayer(registry.Kasiya),
            ClonePlayer(registry.Mariya),
            ClonePlayer(registry.Nightingale),
        ];
        GameInfo.NormalizePlayerCharacters();
    }

    private static PlayerInfoStructure ClonePlayer(PlayerInfoStructure source)
    {
        return new PlayerInfoStructure
        {
            CharacterScenePath = source.CharacterScenePath,
            Life = Math.Max(1, source.LifeMax / 2),
            LifeMax = source.LifeMax,
            LifeInitialized = true,
            Power = source.Power,
            Survivability = source.Survivability,
            TalentPoints = source.TalentPoints,
            UnlockedTalents = source.UnlockedTalents,
            GainedSkills = source.GainedSkills,
            TakenSkills = source.TakenSkills,
            AllSkills = source.AllSkills,
            PositionIndex = source.PositionIndex,
            PortaitPath = source.PortaitPath,
            CharacterName = source.CharacterName,
            PassiveName = source.PassiveName,
            PassiveDescription = source.PassiveDescription,
        };
    }

    private void Capture(string fileName)
    {
        Image image = GetViewport().GetTexture().GetImage();
        string path = Path.Combine(OutputDirectory, fileName);
        Error error = image.SavePng(path);
        GD.Print($"[ScreenEffectSmoke] CAPTURE path={path} error={error}");
    }

    private string Describe()
    {
        return $"overlayPath={_overlay.GetPath()} layer={_overlay.Layer} visible={_overlay.Visible} "
            + $"tintPath={_healTint.GetPath()} visible={_healTint.Visible} "
            + $"modulate={_healTint.SelfModulate} globalRect={_healTint.GetGlobalRect()} "
            + $"texture={_healTint.Texture?.GetPath()}";
    }

    private void Fail(Exception exception)
    {
        GD.PrintErr($"[ScreenEffectSmoke] FAILED {exception}");
        GetTree().Quit(1);
    }
}
