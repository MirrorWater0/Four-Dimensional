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
        BuffGain,
        PropertyGain,
        UiHover,
        UiClick,
        UiButtonDown,
        UiButtonUp,
        CardDeal,
        CardHover,
        CardPickup,
        CardExhaust,
        BattleStart,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Deny,
    }

    private const string SfxBusName = "SFX";
    private const int MaxPlayerCount = 16;
    private const ulong AttackCueCooldownMsec = 80;
    private const ulong HurtCueCooldownMsec = 65;
    private const ulong BlockImpactCueCooldownMsec = 65;
    private const ulong BuffGainCueCooldownMsec = 250;
    private const ulong UiHoverCueCooldownMsec = 35;
    private const ulong UiClickCueCooldownMsec = 35;
    private const ulong UiButtonCueCooldownMsec = 20;
    private const ulong CardDealCueCooldownMsec = 40;
    private const ulong CardHoverCueCooldownMsec = 55;
    private const ulong CardPickupCueCooldownMsec = 45;
    private const ulong CardExhaustCueCooldownMsec = 250;

    private readonly List<AudioStreamPlayer> _runtimeSfxPlayers = new();
    private readonly Dictionary<AudioCue, AudioStreamPlayer> _cuePlayers = new();
    private readonly Dictionary<AudioCue, ulong> _lastCuePlayTicks = new();
    private readonly HashSet<ulong> _boundButtonInstanceIds = new();

    public static AudioManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        SceneTree tree = GetTree();
        if (tree != null)
            tree.NodeAdded -= OnTreeNodeAdded;

        _boundButtonInstanceIds.Clear();

        if (Instance == this)
            Instance = null;
    }

    public override void _Ready()
    {
        EnsureSfxBus();
        BindCuePlayers();
        RefreshSettings();
        BindExistingButtonSfx();

        SceneTree tree = GetTree();
        if (tree != null)
            tree.NodeAdded += OnTreeNodeAdded;
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

    public static void PlayBuffGain(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.BuffGain, source, volumeDbOffset, pitchScale);
    }

    public static void PlayPropertyGain(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.PropertyGain, source, volumeDbOffset, pitchScale);
    }

    public static void PlayUiHover(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.UiHover, source, volumeDbOffset, pitchScale);
    }

    public static void PlayUiClick(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.UiClick, source, volumeDbOffset, pitchScale);
    }

    public static void PlayUiButtonDown(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.UiButtonDown, source, volumeDbOffset, pitchScale);
    }

    public static void PlayUiButtonUp(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.UiButtonUp, source, volumeDbOffset, pitchScale);
    }

    public static void PlayCardDeal(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.CardDeal, source, volumeDbOffset, pitchScale);
    }

    public static void PlayCardHover(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.CardHover, source, volumeDbOffset, pitchScale);
    }

    public static void PlayCardPickup(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.CardPickup, source, volumeDbOffset, pitchScale);
    }

    public static void PlayCardExhaust(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.CardExhaust, source, volumeDbOffset, pitchScale);
    }

    public static void PlayBattleStart(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.BattleStart, source, volumeDbOffset, pitchScale);
    }

    public static void PlayPlayerTurn(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.PlayerTurn, source, volumeDbOffset, pitchScale);
    }

    public static void PlayEnemyTurn(
        Node source = null,
        float volumeDbOffset = 0f,
        float pitchScale = 1f
    )
    {
        Instance?.PlayCue(AudioCue.EnemyTurn, source, volumeDbOffset, pitchScale);
    }

    public static void PlayVictory(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.Victory, source, volumeDbOffset, pitchScale);
    }

    public static void PlayDeny(Node source = null, float volumeDbOffset = 0f, float pitchScale = 1f)
    {
        Instance?.PlayCue(AudioCue.Deny, source, volumeDbOffset, pitchScale);
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
        BindCuePlayer(AudioCue.BuffGain, "BuffGainPlayer");
        BindCuePlayer(AudioCue.PropertyGain, "PropertyGainPlayer");
        BindCuePlayer(AudioCue.UiHover, "UiHoverPlayer");
        BindCuePlayer(AudioCue.UiClick, "UiClickPlayer");
        BindCuePlayer(AudioCue.UiButtonDown, "UiButtonDownPlayer");
        BindCuePlayer(AudioCue.UiButtonUp, "UiButtonUpPlayer");
        BindCuePlayer(AudioCue.CardDeal, "CardDealPlayer");
        BindCuePlayer(AudioCue.CardHover, "CardHoverPlayer");
        BindCuePlayer(AudioCue.CardPickup, "CardPickupPlayer");
        BindCuePlayer(AudioCue.CardExhaust, "CardExhaustPlayer");
        BindCuePlayer(AudioCue.BattleStart, "BattleStartPlayer");
        BindCuePlayer(AudioCue.PlayerTurn, "PlayerTurnPlayer");
        BindCuePlayer(AudioCue.EnemyTurn, "EnemyTurnPlayer");
        BindCuePlayer(AudioCue.Victory, "VictoryPlayer");
        BindCuePlayer(AudioCue.Deny, "DenyPlayer");
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
            AudioCue.Attack => AttackCueCooldownMsec,
            AudioCue.Hurt => HurtCueCooldownMsec,
            AudioCue.BlockImpact => BlockImpactCueCooldownMsec,
            AudioCue.BuffGain => BuffGainCueCooldownMsec,
            AudioCue.UiHover => UiHoverCueCooldownMsec,
            AudioCue.UiClick => UiClickCueCooldownMsec,
            AudioCue.UiButtonDown => UiButtonCueCooldownMsec,
            AudioCue.UiButtonUp => UiButtonCueCooldownMsec,
            AudioCue.CardDeal => CardDealCueCooldownMsec,
            AudioCue.CardHover => CardHoverCueCooldownMsec,
            AudioCue.CardPickup => CardPickupCueCooldownMsec,
            AudioCue.CardExhaust => CardExhaustCueCooldownMsec,
            _ => 0,
        };
    }

    private void BindExistingButtonSfx()
    {
        Node root = GetTree()?.Root;
        if (root == null)
            return;

        BindButtonSfxRecursive(root);
    }

    private void OnTreeNodeAdded(Node node)
    {
        BindButtonSfxRecursive(node);
    }

    private void BindButtonSfxRecursive(Node node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return;

        if (node is Button button)
            BindButtonSfx(button);

        foreach (Node child in node.GetChildren())
            BindButtonSfxRecursive(child);
    }

    private void BindButtonSfx(Button button)
    {
        if (button == null || !GodotObject.IsInstanceValid(button))
            return;

        ulong instanceId = button.GetInstanceId();
        if (!_boundButtonInstanceIds.Add(instanceId))
            return;

        button.MouseEntered += () => OnButtonMouseEntered(button);
        button.ButtonDown += () => OnButtonDown(button);
        button.ButtonUp += () => OnButtonUp(button);
        button.TreeExiting += () => _boundButtonInstanceIds.Remove(instanceId);
    }

    private void OnButtonMouseEntered(Button button)
    {
        if (
            !CanPlayButtonSfx(button)
            || (button.HasMeta("suppress_ui_hover_sfx")
                && button.GetMeta("suppress_ui_hover_sfx").AsBool())
        )
            return;

        PlayCue(AudioCue.UiHover, button, 0f, 1f);
    }

    private void OnButtonDown(Button button)
    {
        if (!CanPlayButtonClickSfx(button))
            return;

        PlayCue(AudioCue.UiButtonDown, button, 0f, 1f);
    }

    private void OnButtonUp(Button button)
    {
        if (!CanPlayButtonClickSfx(button))
            return;

        PlayCue(AudioCue.UiButtonUp, button, 0f, 1f);
    }

    private static bool CanPlayButtonClickSfx(Button button)
    {
        return CanPlayButtonSfx(button)
            && (!button.HasMeta("suppress_ui_click_sfx")
                || !button.GetMeta("suppress_ui_click_sfx").AsBool());
    }

    private static bool CanPlayButtonSfx(Button button)
    {
        return button != null
            && GodotObject.IsInstanceValid(button)
            && button.IsVisibleInTree()
            && !button.Disabled;
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
