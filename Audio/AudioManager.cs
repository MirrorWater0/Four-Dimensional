using System;
using System.Collections.Generic;
using Godot;

public partial class AudioManager : Node
{
    public enum AudioCue
    {
        Attack,
        Hurt,
        BlockGain,
        BlockImpact,
    }

    private const string SfxBusName = "SFX";
    private const int MaxPlayerCount = 16;
    private const ulong HurtCueCooldownMsec = 65;
    private const ulong BlockImpactCueCooldownMsec = 65;

    private readonly List<AudioStreamPlayer> _runtimeSfxPlayers = new();
    private readonly Dictionary<AudioCue, AudioStreamPlayer> _cuePlayers = new();
    private readonly Dictionary<AudioCue, ulong> _lastCuePlayTicks = new();

    public static AudioManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        EnsureSfxBus();
        BindCuePlayers();
        RefreshSettings();
    }

    public static void RefreshSettings()
    {
        if (Instance == null || !GodotObject.IsInstanceValid(Instance))
            return;

        UserSettings.EnsureLoaded();
        Instance.ApplyBusVolumes();
    }

    public static void PlayAttack(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.Attack, source, volumeDbOffset, pitchScale);
    }

    public static void PlayHurt(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.Hurt, source, volumeDbOffset, pitchScale);
    }

    public static void PlayBlockGain(
        Node source = null,
        float volumeDbOffset = -2f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.BlockGain, source, volumeDbOffset, pitchScale);
    }

    public static void PlayBlockImpact(
        Node source = null,
        float volumeDbOffset = -1f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.BlockImpact, source, volumeDbOffset, pitchScale);
    }

    public void SetCueStream(AudioCue cue, AudioStream stream)
    {
        if (_cuePlayers.TryGetValue(cue, out AudioStreamPlayer player) && player != null)
            player.Stream = stream;
    }

    private void BindCuePlayers()
    {
        _cuePlayers.Clear();
        BindCuePlayer(AudioCue.Attack, "AttackPlayer");
        BindCuePlayer(AudioCue.Hurt, "HurtPlayer");
        BindCuePlayer(AudioCue.BlockGain, "BlockGainPlayer");
        BindCuePlayer(AudioCue.BlockImpact, "BlockImpactPlayer");
    }

    private void ApplyBusVolumes()
    {
        SetBusVolume("Master", UserSettings.MasterVolumePercent);
        SetBusVolume(SfxBusName, UserSettings.SfxVolumePercent);
    }

    private static void SetBusVolume(string busName, int percent)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex < 0)
            return;

        float linear = Mathf.Clamp(percent / 100.0f, 0.0f, 1.0f);
        float db = linear <= 0.0001f ? -80.0f : Mathf.LinearToDb(linear);
        AudioServer.SetBusVolumeDb(busIndex, db);
    }

    private static void EnsureSfxBus()
    {
        int existingIndex = AudioServer.GetBusIndex(SfxBusName);
        if (existingIndex >= 0)
        {
            AudioServer.SetBusSend(existingIndex, "Master");
            return;
        }

        int newIndex = AudioServer.BusCount;
        AudioServer.AddBus(newIndex);
        AudioServer.SetBusName(newIndex, SfxBusName);
        AudioServer.SetBusSend(newIndex, "Master");
    }

    private void PlayCue(AudioCue cue, Node source, float volumeDbOffset, float pitchScale)
    {
        if (IsCueCoolingDown(cue))
            return;

        if (!_cuePlayers.TryGetValue(cue, out AudioStreamPlayer template) || template == null)
            return;
        if (template.Stream == null)
            return;

        AudioStreamPlayer player = AcquirePlayer(template);
        if (player == null)
            return;

        player.VolumeDb = template.VolumeDb + volumeDbOffset;
        player.PitchScale = Math.Max(0.05f, template.PitchScale * pitchScale);
        player.Play();
        _lastCuePlayTicks[cue] = Time.GetTicksMsec();
    }

    private bool IsCueCoolingDown(AudioCue cue)
    {
        ulong cooldown = GetCueCooldownMsec(cue);
        if (cooldown == 0)
            return false;

        if (!_lastCuePlayTicks.TryGetValue(cue, out ulong lastPlayTick))
            return false;

        return Time.GetTicksMsec() - lastPlayTick < cooldown;
    }

    private static ulong GetCueCooldownMsec(AudioCue cue)
    {
        return cue switch
        {
            AudioCue.Hurt => HurtCueCooldownMsec,
            AudioCue.BlockImpact => BlockImpactCueCooldownMsec,
            _ => 0,
        };
    }

    private void BindCuePlayer(AudioCue cue, string nodeName)
    {
        AudioStreamPlayer player = GetNodeOrNull<AudioStreamPlayer>(nodeName);
        if (player == null)
            return;

        player.Bus = SfxBusName;
        player.ProcessMode = ProcessModeEnum.Always;
        _cuePlayers[cue] = player;
    }

    private AudioStreamPlayer AcquirePlayer(AudioStreamPlayer template)
    {
        if (!template.Playing)
            return template;

        for (int i = 0; i < _runtimeSfxPlayers.Count; i++)
        {
            AudioStreamPlayer player = _runtimeSfxPlayers[i];
            if (player == null || !GodotObject.IsInstanceValid(player))
                continue;
            if (!player.Playing)
                return ConfigureRuntimePlayer(player, template);
        }

        if (_runtimeSfxPlayers.Count < MaxPlayerCount)
            return CreatePlayer(template);

        return template;
    }

    private static AudioStreamPlayer ConfigureRuntimePlayer(
        AudioStreamPlayer player,
        AudioStreamPlayer template
    )
    {
        player.Stream = template.Stream;
        player.Bus = SfxBusName;
        return player;
    }

    private AudioStreamPlayer CreatePlayer(AudioStreamPlayer template)
    {
        var player = new AudioStreamPlayer
        {
            Name = $"SfxPlayer{_runtimeSfxPlayers.Count + 1}",
            Stream = template.Stream,
            Bus = SfxBusName,
            ProcessMode = ProcessModeEnum.Always,
        };
        AddChild(player);
        _runtimeSfxPlayers.Add(player);
        return player;
    }
}
