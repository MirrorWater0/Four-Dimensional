using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle : Node2D
{
    private static readonly PackedScene DebugConsoleScene = GD.Load<PackedScene>(
        "res://Map/DebugConsole.tscn"
    );

    public static bool Istest = true;

    [Export]
    public bool WarmupMode { get; set; }

    [Export]
    public bool TestBattleTutorial { get; set; }

    [ExportGroup("Background Color")]
    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float BackgroundSaturation
    {
        get => _backgroundSaturation;
        set
        {
            _backgroundSaturation = Mathf.Clamp(value, 0.0f, 1.0f);
            ApplyBackgroundSaturation();
        }
    }

    [ExportGroup("Standalone Test Party")]
    [Export]
    public PlayerCharacterKey StandaloneTestPlayer1 { get; set; } = PlayerCharacterKey.Echo;

    [Export]
    public PlayerCharacterKey StandaloneTestPlayer2 { get; set; } = PlayerCharacterKey.Kasiya;

    [Export]
    public PlayerCharacterKey StandaloneTestPlayer3 { get; set; } = PlayerCharacterKey.Nightingale;

    public Random BattleIntentionRandom;
    public int? BattleRandomNum;
    private Random _carrySkillRandom;
    private Random _playerTeamBattleSkillRandom;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly Dictionary<string, PlayerBattleCardPiles> _playerBattleCardPiles = new();
    private readonly Dictionary<string, Random> _playerBattleSkillRandoms = new();
    private PlayerTeamBattleCardPiles _playerTeamBattleCardPiles;
    private Skill[] _playerTeamBattleHand;
    private ulong _battleInstanceId;
    private ulong _nextBattleCardInstanceId;
    private bool _retreating;
    private bool _battleOverCheckQueued;
    private bool _battleOverCheckPendingAfterEffect;
    private bool _allowBattleEndRevive;
    private bool _standaloneTestBattleContext;
    private TaskCompletionSource<bool> _presentationReadyTcs;
    private Tween _screenShakeTween;
    private Vector2 _battleBasePosition;
    private bool _battleBasePositionInitialized;
    private int _hitStopVersion;
    private bool _hitStopActive;
    private double _hitStopOriginalTimeScale = 1.0;
    private float _backgroundSaturation = 0.08f;
    private const float HitBackgroundDimBase = 0.78f;
    private const float HitStopMinTimeScale = 0.12f;

    private sealed class PlayerBattleCardPiles
    {
        public readonly List<SkillID> DrawPile = new();
        public readonly List<SkillID> DiscardPile = new();
        public readonly List<SkillID> Exhausted = new();
    }

    private sealed class PlayerTeamBattleCardPiles
    {
        public readonly List<BattleCardPileEntry> DrawPile = new();
        public readonly List<BattleCardPileEntry> DiscardPile = new();
        public readonly List<BattleCardPileEntry> Exhausted = new();
    }

    public readonly struct BattleCardPileEntry
    {
        public BattleCardPileEntry(PlayerCharacter owner, SkillID skillId, ulong instanceId)
        {
            Owner = owner;
            SkillId = skillId;
            InstanceId = instanceId;
        }

        /// <summary>状态牌等全队诅咒为 null；玩家技能牌为具体角色。</summary>
        public PlayerCharacter Owner { get; }
        public SkillID SkillId { get; }
        public ulong InstanceId { get; }
    }

    private readonly struct HandStatusExhaustInfo
    {
        public HandStatusExhaustInfo(
            List<int> indexes,
            List<SkillID> skillIds,
            List<PlayerCharacter> owners = null
        )
        {
            Indexes = indexes;
            SkillIds = skillIds;
            Owners = owners ?? new List<PlayerCharacter>();
        }

        public List<int> Indexes { get; }
        public List<SkillID> SkillIds { get; }
        public List<PlayerCharacter> Owners { get; }
        public bool HasAny => Indexes?.Count > 0;
    }

    [Signal]
    public delegate void NextEventHandler(Character character);

    public Map MapNode => field ??= GetNodeOrNull<Map>("/root/Map");
    public List<PlayerCharacter> PlayersList = new();
    public List<EnemyCharacter> EnemiesList = new();
    public List<SummonCharacter> PlayerSummons = new();
    public List<SummonCharacter> EnemySummons = new();
    public Node2D Right => field ??= GetNode<Node2D>("Right");
    public Node2D Left => field ??= GetNode<Node2D>("Left");

    public AnimationPlayer BattleAnimationPlayer =>
        field ??= GetNode<AnimationPlayer>("BattlePlayer");
    public CharacterControl CharacterControl =>
        field ??= GetNode<CharacterControl>("CharacterControlLayer/CharacterControl");
    private BattleAtmosphere Atmosphere =>
        field ??= GetNodeOrNull<BattleAtmosphere>("bg/BattleAtmosphere");
    public void PlayHitEffect(int actualDamage = 1, int blockedDamage = 0)
    {
        float impact = ResolveHitImpact(actualDamage, blockedDamage);
        if (actualDamage >= 14)
            Atmosphere?.Pulse((impact - 0.75f) * 0.7f);
        ScreenEffectOverlay.PlayHit(this, impact, actualDamage, blockedDamage);
        PlayCameraShake(impact);
        PlayHitStop(ResolveHitStopDuration(impact, actualDamage), ResolveHitStopScale(impact));
    }

    public void PlayAbilityCardImpact(float impact = 0.68f)
    {
        Atmosphere?.Pulse(impact * 0.6f);
        PlayCameraShake(Mathf.Clamp(impact, 0.45f, 1.1f));
    }

    private static float ResolveHitImpact(int actualDamage, int blockedDamage)
    {
        int effectiveDamage = Math.Max(actualDamage, blockedDamage);
        float damageImpact = effectiveDamage switch
        {
            >= 45 => 1.65f,
            >= 28 => 1.35f,
            >= 14 => 1.08f,
            > 0 => 0.82f,
            _ => 0.55f,
        };

        if (actualDamage <= 0 && blockedDamage > 0)
            damageImpact *= 0.76f;

        return Mathf.Clamp(damageImpact, 0.45f, 1.8f);
    }

    private void CacheBattleBasePosition()
    {
        if (_battleBasePositionInitialized)
            return;

        _battleBasePosition = Position;
        _battleBasePositionInitialized = true;
    }

    private void PlayCameraShake(float impact)
    {
        CacheBattleBasePosition();
        if (!_battleBasePositionInitialized)
            return;

        float shakeScale = UserSettings.GetBattleShakeMultiplier();
        if (shakeScale <= 0.0f)
            return;

        if (_screenShakeTween != null && GodotObject.IsInstanceValid(_screenShakeTween))
            _screenShakeTween.Kill();

        Position = _battleBasePosition;
        float amplitude = shakeScale * Mathf.Clamp(impact, 0.45f, 1.8f);
        _screenShakeTween = CreateTween();
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(12, 9) * amplitude,
                0.032f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(-17, -6) * amplitude,
                0.035f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(
                this,
                "position",
                _battleBasePosition + new Vector2(8, -4) * amplitude,
                0.028f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _screenShakeTween
            .TweenProperty(this, "position", _battleBasePosition, 0.055f + 0.02f * impact)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        _screenShakeTween.Finished += OnCameraShakeFinished;
    }

    private static float ResolveHitStopDuration(float impact, int actualDamage)
    {
        if (actualDamage <= 0)
            return 0.018f;

        return Mathf.Clamp(0.026f + 0.018f * impact, 0.028f, 0.065f);
    }

    private static float ResolveHitStopScale(float impact) =>
        Mathf.Clamp(0.55f - 0.12f * impact, HitStopMinTimeScale, 0.48f);

    private async void PlayHitStop(float duration, float timeScale)
    {
        if (duration <= 0f || !IsInsideTree())
            return;

        int version = ++_hitStopVersion;
        if (!_hitStopActive)
        {
            _hitStopOriginalTimeScale = Engine.TimeScale;
            _hitStopActive = true;
        }

        Engine.TimeScale = Math.Min(Engine.TimeScale, timeScale);
        await ToSignal(GetTree().CreateTimer(duration, true, false, true), SceneTreeTimer.SignalName.Timeout);

        if (version != _hitStopVersion || !GodotObject.IsInstanceValid(this))
            return;

        RestoreHitStopTimeScale();
    }

    private void RestoreHitStopTimeScale()
    {
        if (!_hitStopActive)
            return;

        Engine.TimeScale = _hitStopOriginalTimeScale;
        _hitStopActive = false;
    }

    private void OnCameraShakeFinished()
    {
        Position = _battleBasePosition;

        _screenShakeTween = null;
    }

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();

    private int _playerEnergy = 0;
    private int _handPreviewContextRevision;
    private int _playerActionCount = 0;
    private int _enemyActionCount = 0;
    private int _elapsedTurnCount = 0;
    private readonly Dictionary<ulong, int> _characterActionCounts = new();
    public int ElapsedTurnCount => _elapsedTurnCount;
    public int PlayerTotalTurnCount => _elapsedTurnCount;
    public int EnemyTotalTurnCount => 0;
    private readonly Dictionary<ulong, int> _pendingExtraActions = new();
    private readonly HashSet<ulong> _activeExtraActionCharacters = new();
    private Character _nextActionPreviewCharacter;
    private Character _nextTurnOrderGroundPreviewCharacter;
    private Character _secondTurnOrderGroundPreviewCharacter;
    private List<PlayerCharacter> _activePlayerPhaseOrder = new();
    private int _activePlayerPhaseIndex = -1;
    private bool _isResolvingPlayerTeamActionPhase;
    private List<EnemyCharacter> _activeEnemyPhaseOrder = new();
    private int _activeEnemyPhaseIndex = -1;
    private bool _isResolvingEnemyTeamActionPhase;
    private int _enemyIntentionRefreshDeferDepth;
    private bool _pendingEnemyIntentionPreviewRefresh;
    private readonly List<Func<Task>> _enemyPhaseEndEffects = new();
    private bool? _battleStartPlayerActsFirst;
    private bool _chainingForesightDraw;
    private int _pendingSearchShuffleCount;
    private bool _resolvingSearchAfterShuffle;
    private bool _searchShuffleResolutionDeferred;
    private ulong _searchShuffleResolveAfterMsec;
    private int _pendingPredictionViewCount;
    private double _skillTuningAutoReloadElapsed;
    private ulong _lastSkillTuningFilesFingerprint;
    private ulong _pendingSkillTuningFilesFingerprint;
    private const double SkillTuningAutoReloadPollSeconds = 0.5;

    [Export]
    public bool EnableDevelopmentTuningAutoReload { get; set; } = false;

    public Label PlayerEnergyLabel =>
        field ??= GetNodeOrNull<Label>("CharacterControlLayer/EnergyBG/EnergyLabel");
    public int PlayerEnergy => _playerEnergy;
    public int HandPreviewContextRevision => _handPreviewContextRevision;

    public void NotifyHandPreviewContextChanged()
    {
        _handPreviewContextRevision++;
        CardEffectPreviewEcs.InvalidateBattle(this);
        CharacterControl?.RefreshActiveHandSkillPreviews();
    }

    private void PollSkillTuningAutoReload(double delta)
    {
        if (WarmupMode || !OS.IsDebugBuild() || !EnableDevelopmentTuningAutoReload)
            return;

        _skillTuningAutoReloadElapsed += delta;
        if (_skillTuningAutoReloadElapsed < SkillTuningAutoReloadPollSeconds)
            return;

        _skillTuningAutoReloadElapsed = 0;

        ulong fingerprint = GetDevelopmentTuningFilesFingerprint();
        if (_lastSkillTuningFilesFingerprint == 0)
        {
            _lastSkillTuningFilesFingerprint = fingerprint;
            return;
        }

        if (fingerprint == _lastSkillTuningFilesFingerprint)
        {
            _pendingSkillTuningFilesFingerprint = 0;
            return;
        }

        if (_pendingSkillTuningFilesFingerprint != fingerprint)
        {
            _pendingSkillTuningFilesFingerprint = fingerprint;
            return;
        }

        _lastSkillTuningFilesFingerprint = fingerprint;
        _pendingSkillTuningFilesFingerprint = 0;
        ReloadSkillTuningForDevelopment();
    }

    public override void _Input(InputEvent @event)
    {
        if (WarmupMode || !OS.IsDebugBuild())
            return;

        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
            return;

        Key keycode = keyEvent.Keycode != Key.None ? keyEvent.Keycode : keyEvent.PhysicalKeycode;
        if (keycode != Key.F9)
            return;

        ReloadSkillTuningForDevelopment(exportSourceDefaults: true);
        GetViewport().SetInputAsHandled();
    }

    private void ReloadSkillTuningForDevelopment(bool exportSourceDefaults = false)
    {
        if (exportSourceDefaults)
            ExportTuningFromSourceForDevelopment();

        bool skillSuccess = SkillTuning.Reload(out string skillMessage);
        bool enemySuccess = EnemyTuning.Reload(out string enemyMessage);
        if (skillSuccess)
            RefreshSkillTuningInstances();
        if (enemySuccess)
            RefreshEnemyTuningInstances();

        GD.Print($"[Tuning] {skillMessage}");
        GD.Print($"[Tuning] {enemyMessage}");
    }

    private static void ExportTuningFromSourceForDevelopment()
    {
        string scriptPath = ProjectSettings.GlobalizePath(
            "res://scripts/export_skill_tuning_from_source.ps1"
        );
        if (!File.Exists(scriptPath))
        {
            GD.PushWarning($"[TuningExport] 未找到导出脚本：{scriptPath}");
            return;
        }

        string projectRoot = ProjectSettings.GlobalizePath("res://");
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = projectRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        try
        {
            using Process process = Process.Start(startInfo);
            if (process == null)
            {
                GD.PushWarning("[TuningExport] 无法启动 PowerShell 导出进程。");
                return;
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000))
            {
                process.Kill(entireProcessTree: true);
                GD.PushWarning("[TuningExport] 从 C# 同步数值表超时。");
                return;
            }

            Task.WaitAll(outputTask, errorTask);
            string output = outputTask.Result.Trim();
            string error = errorTask.Result.Trim();
            if (process.ExitCode != 0)
            {
                GD.PushWarning(
                    $"[TuningExport] 从 C# 同步数值表失败（退出码 {process.ExitCode}）：{error}"
                );
                return;
            }

            GD.Print(
                string.IsNullOrWhiteSpace(output)
                    ? "[TuningExport] 已从 C# 同步数值表。"
                    : output
            );
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[TuningExport] 从 C# 同步数值表失败：{exception.Message}");
        }
    }

    private static ulong GetDevelopmentTuningFilesFingerprint()
    {
        ulong hash = 1469598103934665603UL;
        AddFingerprintValue(SkillTuning.GetTuningFilesFingerprint(), ref hash);
        AddFingerprintValue(EnemyTuning.GetTuningFilesFingerprint(), ref hash);
        return hash;
    }

    private static void AddFingerprintValue(ulong value, ref ulong hash)
    {
        hash ^= value;
        hash *= 1099511628211UL;
    }

    private void RefreshSkillTuningInstances()
    {
        int refreshed = 0;
        foreach (Character character in EnumerateBattleCharacters())
        {
            refreshed += RefreshSkillArrayTuning(character?.Skills);
            character?.InvalidateSkillTooltipCache();
        }

        refreshed += RefreshSkillArrayTuning(_playerTeamBattleHand);

        foreach (EnemyCharacter enemy in EnemiesList)
        {
            enemy?.CurrentIntentionSkill?.RefreshTuning();
            enemy?.RefreshIntentionDisplayForCurrentState();
        }

        CharacterControl?.InvalidateHandCardHoverPreviewCaches();
        CharacterControl?.RefreshCurrentTurnUi();
        NotifyHandPreviewContextChanged();
        GD.Print($"[SkillTuning] refreshed active skill instances: {refreshed}");
    }

    private void RefreshEnemyTuningInstances()
    {
        int refreshed = 0;
        foreach (EnemyCharacter enemy in EnumerateBattleCharacters().OfType<EnemyCharacter>())
        {
            if (enemy?.RefreshTuning() == true)
                refreshed++;
        }

        if (refreshed > 0)
        {
            RefreshTurnOrderPreview();
        }

        GD.Print($"[EnemyTuning] refreshed active enemy and summon instances: {refreshed}");
    }

    private IEnumerable<Character> EnumerateBattleCharacters()
    {
        foreach (PlayerCharacter player in PlayersList)
            yield return player;

        foreach (EnemyCharacter enemy in EnemiesList)
            yield return enemy;

        foreach (SummonCharacter summon in PlayerSummons)
            yield return summon;

        foreach (SummonCharacter summon in EnemySummons)
            yield return summon;
    }

    private static int RefreshSkillArrayTuning(IEnumerable<Skill> skills)
    {
        if (skills == null)
            return 0;

        int refreshed = 0;
        var refreshedSkills = new HashSet<Skill>();
        foreach (Skill skill in skills)
        {
            if (skill == null || !refreshedSkills.Add(skill))
                continue;

            skill.RefreshTuning();
            refreshed++;
        }

        return refreshed;
    }
    public LevelNode CurrentLevelNode;
    public Character dummy => field ??= GetNode<Character>("Dummy");
    public const float FormationGapX = 230f;
    public const int MaxFormationSlots = 5;
    public const int CenterFormationSlot = (MaxFormationSlots + 1) / 2;
    public const int MaxEnemyFormationSlots = 4;
    public const int EnemyCenterFormationSlot = (MaxEnemyFormationSlots + 1) / 2;
    private const int MaxBattleTurns = 100;
    private const int PostActionDelayMs = 420;
    private const int BattleStartIntentionDelayMs = 120;
    private const int PlayerPostActionDelayMs = 120;
    private const int EnemyPostActionDelayMs = 220;
    private const int BattleOverDelayMs = 5000;
    private const int DefeatGameOverDelayMs = 500;
    private const int BattleStartEffectIntervalMs = 100;
    private PlayerResourceState _mapResourceState;

    public Character CurrentActionCharacter { get; private set; }
    public bool IsResolvingPlayerTeamActionPhase => _isResolvingPlayerTeamActionPhase;
    public bool IsResolvingEnemyTeamActionPhase => _isResolvingEnemyTeamActionPhase;
    public bool IsDeferringEnemyIntentionRefresh => _enemyIntentionRefreshDeferDepth > 0;

    public override void _EnterTree()
    {
        _battleInstanceId = GetInstanceId();
    }

    public override void _ExitTree()
    {
        _retreating = true;
        RestoreHitStopTimeScale();
        ClearTurnOrderGroundPreviewCharacter();
        FreeIncomingDamagePreviewLabels();
        TryCancelLifetime();
        if (_mapResourceState != null && GodotObject.IsInstanceValid(_mapResourceState))
        {
            _mapResourceState.SetBattleActive(false);
        }
        _mapResourceState = null;
        MapNode?.Camera?.MakeCurrent();
    }

    public override async void _Ready()
    {
        ApplyBackgroundSaturation();
        _presentationReadyTcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        if (WarmupMode)
        {
            DisableBattleProcessing();
            MarkPresentationReady(true);
            return;
        }

        GetNodeOrNull<MouseTrail>("/root/MouseTrail")?.ResetPointerTrackingDeferred();

        _mapResourceState = MapNode?.PlayerResourceState;
        if (_mapResourceState != null && GodotObject.IsInstanceValid(_mapResourceState))
        {
            _mapResourceState.SetBattleActive(true);
        }

        var token = _lifetimeCts.Token;
        try
        {
            InitDummy();
            EnsureStandaloneTestBattleContext();
            EnsureStandaloneDebugConsole();
            if (CurrentLevelNode == null)
            {
                GD.PushError("Battle requires CurrentLevelNode unless Battle.Istest is enabled.");
                MarkPresentationReady(false);
                return;
            }

            InitializeBattleUi();
            CacheBattleBasePosition();
            InitializeBattleCharacters();
            CaptureBattleStartInitiative();
            SetCharaterPostion();
            RefreshTurnOrderPreview();
            CharacterControl.Connect();
            CharacterControl.DisableAll();
            if (!await InitializeEnemyIntentions(token))
            {
                MarkPresentationReady(false);
                return;
            }

            RecordAutomationSnapshot("enemy_intentions_initialized");
            MarkPresentationReady(true);
            GetNodeOrNull<MouseTrail>("/root/MouseTrail")?.ResetPointerTrackingDeferred();

            await BattleBegin1(token);
        }
        catch (Exception e)
        {
            GD.PushError($"Battle initialization failed: {e.Message}");
            MarkPresentationReady(false);
        }
    }

    private void ApplyBackgroundSaturation()
    {
        var backgroundNeutralizer = GetNodeOrNull<ColorRect>("bg/BackgroundHueNeutralizer");
        if (
            backgroundNeutralizer?.Material is ShaderMaterial material
            && GodotObject.IsInstanceValid(material)
        )
        {
            material.SetShaderParameter("max_saturation", _backgroundSaturation);
        }
    }

    public Task WhenPresentationReadyAsync()
    {
        return _presentationReadyTcs?.Task ?? Task.CompletedTask;
    }

    private void MarkPresentationReady(bool success)
    {
        _presentationReadyTcs?.TrySetResult(success);
    }

    private void DisableBattleProcessing()
    {
        SetProcess(false);
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    private async Task<bool> ShowFirstBattleTutorialIfNeeded(CancellationToken token)
    {
        if (_standaloneTestBattleContext && !TestBattleTutorial)
        {
            GameInfo.HasSeenBattleTutorial = true;
            return true;
        }

        if (!TestBattleTutorial && BattleTutorialOverlay.HasSeenTutorial())
        {
            GameInfo.HasSeenBattleTutorial = true;
            return true;
        }

        await BattleTutorialOverlay.ShowAsync(this);
        if (!CanContinue(token))
            return false;

        if (TestBattleTutorial)
            return true;

        GameInfo.HasSeenBattleTutorial = true;
        BattleTutorialOverlay.MarkTutorialSeen();
        return true;
    }

    private void EnsureStandaloneTestBattleContext()
    {
        if (!Istest)
            return;

        bool createdPlayerContext = false;
        if (GameInfo.PlayerCharacters == null || GameInfo.PlayerCharacters.Length == 0)
        {
            GameInfo.Seed = 4203;
            GameInfo.PlayerCharacters = BuildStandaloneTestRoster();
            GameInfo.NormalizePlayerCharacters();
            GameInfo.SeedTakenSkillsAsGained();
            GameInfo.InitNewGame();
            createdPlayerContext = true;
        }
        else
        {
            GameInfo.NormalizePlayerCharacters();
            GameInfo.SeedTakenSkillsAsGained();
        }

        bool createdLevelContext = false;
        if (
            CurrentLevelNode?.EnemiesRegeditList == null
            || CurrentLevelNode.EnemiesRegeditList.Count == 0
        )
        {
            CurrentLevelNode = BuildStandaloneTestLevelNode();
            createdLevelContext = true;
        }

        if (!BattleRandomNum.HasValue)
            BattleRandomNum = 13579;

        _standaloneTestBattleContext = createdPlayerContext || createdLevelContext;
    }

    private void EnsureStandaloneDebugConsole()
    {
        if (GetTree()?.CurrentScene != this || GetNodeOrNull<DebugConsole>("DebugConsole") != null)
            return;

        var console = DebugConsoleScene?.Instantiate<DebugConsole>() ?? new DebugConsole();
        console.Name = "DebugConsole";
        AddChild(console);
    }

    private PlayerInfoStructure[] BuildStandaloneTestRoster()
    {
        var registry = new PlayerCharacterRegistry();
        PlayerCharacterKey[] configuredKeys =
        [
            StandaloneTestPlayer1,
            StandaloneTestPlayer2,
            StandaloneTestPlayer3,
        ];

        PlayerCharacterKey[] fallbackKeys =
        [
            PlayerCharacterKey.Echo,
            PlayerCharacterKey.Kasiya,
            PlayerCharacterKey.Mariya,
            PlayerCharacterKey.Nightingale,
        ];
        var rosterKeys = configuredKeys.Distinct().ToList();
        for (int i = 0; i < fallbackKeys.Length && rosterKeys.Count < GameInfo.DefaultPlayerPartySize; i++)
        {
            if (!rosterKeys.Contains(fallbackKeys[i]))
                rosterKeys.Add(fallbackKeys[i]);
        }

        return rosterKeys
            .Take(GameInfo.DefaultPlayerPartySize)
            .Select(
                (characterKey, index) =>
                    CloneStandaloneTestPlayerInfo(
                        GetStandaloneTestPlayerInfo(registry, characterKey),
                        GameInfo.GetDefaultPlayerFormationPosition(index)
                    )
            )
            .ToArray();
    }

    private static PlayerInfoStructure GetStandaloneTestPlayerInfo(
        PlayerCharacterRegistry registry,
        PlayerCharacterKey characterKey
    )
    {
        return characterKey switch
        {
            PlayerCharacterKey.Kasiya => registry.Kasiya,
            PlayerCharacterKey.Mariya => registry.Mariya,
            PlayerCharacterKey.Nightingale => registry.Nightingale,
            _ => registry.Echo,
        };
    }

    private static PlayerInfoStructure CloneStandaloneTestPlayerInfo(
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

    private static LevelNode BuildStandaloneTestLevelNode()
    {
        return new LevelNode
        {
            Type = LevelNode.LevelType.Normal,
            State = LevelNode.LevelState.Unlocked,
            EnemiesRegeditList =
            [
                CreateStandaloneTestEnemy(1),
                CreateStandaloneTestEnemy(2),
                CreateStandaloneTestEnemy(3),
            ],
        };
    }

    private static EnemyRegedit CreateStandaloneTestEnemy(int positionIndex)
    {
        return new EvilRegedit
        {
            PositionIndex = positionIndex,
            CurrentLife = -1,
        }.GetRegedit();
    }

    private void InitializeBattleCharacters()
    {
        ClearSummons(queueFree: true);
        EnemiesList.Clear();
        int enemyCount = CurrentLevelNode?.EnemiesRegeditList?.Count ?? 0;
        LevelNode.LevelType? levelType = CurrentLevelNode?.Type;
        foreach (var regedit in CurrentLevelNode.EnemiesRegeditList)
        {
            if (regedit == null)
                continue;
            if (regedit.CurrentLife == 0)
                continue;

            var enemy = regedit.CharacterScene.Instantiate<EnemyCharacter>();
            enemy.Registry = regedit;
            int requestedPositionIndex =
                regedit is WarRegedit ? MaxEnemyFormationSlots : regedit.PositionIndex;
            regedit.PositionIndex = requestedPositionIndex;
            enemy.PositionIndex = ResolveEnemyFormationPositionIndex(
                levelType,
                enemyCount,
                requestedPositionIndex
            );
            InitializeCharacter(enemy);
            EnemiesList.Add(enemy);
        }

        PlayersList.Clear();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var characterScene = PreloadeScene.GetPackedScene(
                GameInfo.PlayerCharacters[i].CharacterScenePath
            );
            var character = characterScene?.Instantiate<PlayerCharacter>();
            if (character == null)
                continue;

            character.CharacterIndex = i;
            InitializeCharacter(character);
            PlayersList.Add(character);
        }

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
        _playerTeamBattleCardPiles = null;
        SyncPlayerLifeToGameInfo(refreshResourceUi: true);
    }

    private void InitializeCharacter(Character character)
    {
        character.BattleNode = this;
        character.Initialize();
    }

    private void InitializeBattleUi()
    {
        BattleIntentionRandom ??= CreateBattleRandom(unchecked((int)0x13572468));
        _carrySkillRandom ??= CreateBattleRandom(unchecked((int)0x5A17C0DE));
        _playerActionCount = 0;
        _enemyActionCount = 0;
        _elapsedTurnCount = 0;
        _playerBattleCardPiles.Clear();
        _playerTeamBattleCardPiles = null;
        _battleCardKeywords.Clear();
        _nextBattleCardInstanceId = 0;
        _playerBattleSkillRandoms.Clear();
        _playerTeamBattleSkillRandom = null;
        _playerTeamBattleHand = null;
        _pendingSearchShuffleCount = 0;
        _resolvingSearchAfterShuffle = false;
        _searchShuffleResolutionDeferred = false;
        _searchShuffleResolveAfterMsec = 0;
        _pendingPredictionViewCount = 0;
        _playerEnergy = 0;
        RefreshPlayerEnergyUi();
        _characterActionCounts.Clear();
        _pendingExtraActions.Clear();
        _activeExtraActionCharacters.Clear();
        InitializeNonGameplayUi();
    }

    private Random CreateBattleRandom(int salt)
    {
        int baseSeed =
            BattleRandomNum
            ?? (
                CurrentLevelNode != null
                    ? GameInfo.CreateRunRngSeed(
                        GameInfo.GetStreamForNodeType(CurrentLevelNode.Type),
                        GameInfo.GetBattleRngQueueIndex(CurrentLevelNode)
                    )
                    : GameInfo.Seed
            );
        return new Random(baseSeed ^ salt);
    }

    public Skill DrawPlayerBattleSkill(
        PlayerCharacter player,
        Skill.SkillTypes skillType = Skill.SkillTypes.none,
        SkillID? avoidSkillId = null
    )
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        if (player.CharacterIndex < 0 || player.CharacterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        if (EndActionBuff.TryBlockPlayerTeamDraw(this))
            return null;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return null;

        if (!TryEnsureDrawableTeamCards(piles, skillType, player))
            return null;

        int pickedIndex = PickDrawableTeamCardIndex(
            piles.DrawPile,
            skillType,
            player,
            avoidSkillId
        );
        if (pickedIndex < 0)
            return null;

        BattleCardPileEntry pickedEntry = piles.DrawPile[pickedIndex];
        piles.DrawPile.RemoveAt(pickedIndex);
        Skill pickedSkill = Skill.GetSkill(pickedEntry.SkillId);
        if (pickedSkill != null)
        {
            pickedSkill.OwnerCharater = pickedEntry.Owner ?? player;
            pickedSkill.BattleCardInstanceId = pickedEntry.InstanceId;
        }
        player.InvalidateSkillTooltipCache();
        return pickedSkill;
    }

    public Skill[] GetPlayerTeamBattleHand()
    {
        EnsurePlayerTeamBattleHandSize();
        return _playerTeamBattleHand;
    }

    public Skill GetPlayerTeamBattleHandSkill(int index)
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        return index >= 0 && index < hand.Length ? hand[index] : null;
    }

    public async void BeginPlayerTeamAction()
    {
        PlayerCharacter teamContext = GetPlayerPhaseActionOrder().FirstOrDefault(IsCharacterAlive);
        if (teamContext == null)
            return;

        EnsurePlayerTeamBattleHandSize();
        CharacterControl?.ShowPlayerTurn(teamContext);
        CharacterControl?.SetPlayerInputsEnabled(teamContext, false);
        try
        {
            await ResolvePendingPredictionAtTurnStartAsync(teamContext);
            await ResolvePendingSearchAfterShuffleAsync();
            await DrawPlayerTeamTurnStartCardsWithShuffleBreaksAsync(
                GetPlayerTeamTurnStartDrawCount(),
                _lifetimeCts.Token
            );
            Skill[] oldHand = GetPlayerTeamBattleHand().ToArray();
            Relic.ApplyPlayerTeamTurnStartRelicEffects(this, teamContext, refreshUi: false);
            ResolvePlayerTeamExtraDraw();
            CharacterControl?.QueueBatchDrawMakeRoomAnimation(
                oldHand,
                GetPlayerTeamBattleHand()
            );
            CharacterControl?.RefreshCurrentTurnUi();
            RecordAutomationSnapshot("player_turn_ready");
        }
        finally
        {
            if (IsBattleAlive() && !HasBattleEnded())
            {
                CharacterControl?.SetPlayerInputsEnabled(teamContext, true);
                RecordAutomationSnapshot("player_inputs_enabled");
            }
        }
    }

    private int GetPlayerTeamTurnStartDrawCount()
    {
        var playerPhaseOrder = GetPlayerPhaseActionOrder();
        int alivePlayers = playerPhaseOrder.Count(IsCharacterAlive);
        return PlayerCharacter.TeamTurnStartDrawBase
            + alivePlayers * PlayerCharacter.TeamTurnStartDrawPerAlivePlayer
            + Relic.GetTurnStartDrawBonus(this, playerPhaseOrder.FirstOrDefault(IsCharacterAlive));
    }

    public int GetAvailableEnergy(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return 0;

        return character.IsPlayer ? PlayerEnergy : 0;
    }

    public int UpdataEnergy(Character target, int delta, Character source = null)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return 0;

        if (!target.IsPlayer)
            return 0;

        int actualPlayerDelta = UpdataPlayerEnergy(delta, source ?? target);
        if (actualPlayerDelta == 0)
            return 0;

        if (actualPlayerDelta < 0)
            PlayEnergyLossEffect(target);

        SpawnEnergyHint(target, actualPlayerDelta);
        return actualPlayerDelta;
    }

    public int UpdataPlayerEnergySilently(int delta)
    {
        return UpdataPlayerEnergy(delta, recordChange: false, playGainAnimation: false);
    }

    private int UpdataPlayerEnergy(
        int delta,
        Character source = null,
        bool recordChange = true,
        bool playGainAnimation = true
    )
    {
        int oldEnergy = Math.Max(0, _playerEnergy);
        int newEnergy = Math.Max(0, oldEnergy + delta);
        int actualDelta = newEnergy - oldEnergy;
        if (actualDelta == 0)
            return 0;

        _playerEnergy = newEnergy;
        RefreshPlayerEnergyUi();
        InvalidatePlayerTeamSkillTooltips();
        CharacterControl?.RefreshCurrentTurnUi();
        if (actualDelta > 0 && playGainAnimation)
            BattleAnimationPlayer?.Play("blue");
        if (recordChange)
        {
            RecordPlayerEnergyChange(actualDelta, source);
            RecordAutomationSnapshot("player_energy_updated");
        }
        return actualDelta;
    }

    private static void PlayEnergyLossEffect(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        CharacterEffect.Spawn(target, "energe", new Vector2(0, -50));
    }

    private static void SpawnEnergyHint(Character target, int delta)
    {
        if (target == null || !GodotObject.IsInstanceValid(target) || delta == 0)
            return;

        BuffHintLabel.Spawn(
            target,
            $"[color=#87CEEB]Energy[/color] {delta:+0;-0;0}",
            target.GlobalPosition
        );
    }

    private void RefreshPlayerEnergyUi()
    {
        if (PlayerEnergyLabel != null)
            PlayerEnergyLabel.Text = _playerEnergy.ToString();
    }

    private int GetPlayerTeamTurnStartEnergyGain(IReadOnlyList<PlayerCharacter> playerPhaseOrder)
    {
        int alivePlayerCount = 0;
        int sourceBonus = 0;
        PlayerCharacter energyBonusContext = null;
        if (playerPhaseOrder != null)
        {
            PlayerCharacter[] alivePlayers = playerPhaseOrder
                .Where(IsCharacterAlive)
                .ToArray();
            alivePlayerCount = alivePlayers.Length;
            energyBonusContext = alivePlayers.FirstOrDefault();
            foreach (PlayerCharacter player in alivePlayers)
            {
                int playerSourceBonus = SpecialBuff.GetSourceEnergyBonus(player);
                if (playerSourceBonus <= 0)
                    continue;

                sourceBonus += playerSourceBonus;
                Buff.FlashTriggersOnOwner(player, Buff.BuffName.Source);
            }
        }

        return PlayerCharacter.TeamTurnStartEnergyBase
            + alivePlayerCount * PlayerCharacter.TeamTurnStartEnergyPerAlivePlayer
            + sourceBonus
            + Relic.GetTurnStartEnergyGainBonus(energyBonusContext);
    }

    private int GetPlayerTeamEnergyStorageReduction()
    {
        return GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Where(IsCharacterAlive)
            .Sum(character => SpecialBuff.GetEnergyStorageReduction(character));
    }

    private void ResolvePlayerTeamEnergyLossAtTurnEnd()
    {
        if (_playerEnergy <= 0)
            return;

        int energyLoss = Math.Max(0, _playerEnergy - GetPlayerTeamEnergyStorageReduction());
        if (energyLoss > 0)
            UpdataPlayerEnergy(-energyLoss);
    }

    private void DrawPlayerTeamBattleCards(int count)
    {
        if (count <= 0)
            return;

        while (count > 0)
        {
            if (!DrawOnePlayerTeamBattleCard())
                return;

            count--;
        }

        InvalidatePlayerTeamSkillTooltips();
    }

    public void NotifyPlayerTeamBattleCardsGeneratedByFriendlySkill()
    {
        if (_chainingForesightDraw)
            return;

        int drawCount = SpecialBuff.GetTotalForesightDrawCount(this);
        if (drawCount <= 0 || GetPlayerTeamBattleHandEmptySlotCount() <= 0)
            return;

        foreach (
            Character character in GetTeamCharacters(isPlayer: true, includeSummons: false).Where(
                IsCharacterAlive
            )
        )
        {
            Buff.FlashTriggersOnOwner(character, Buff.BuffName.Foresight);
        }

        _chainingForesightDraw = true;
        try
        {
            TryDrawPlayerTeamBattleCards(drawCount, refreshUi: false);
        }
        finally
        {
            _chainingForesightDraw = false;
        }
    }

    private void QueueSearchAfterShuffle(float animationDuration = 0f)
    {
        if (SpecialBuff.GetSearchOwners(this).Length == 0)
            return;

        _pendingSearchShuffleCount++;
        ulong resolveAfterMsec = Time.GetTicksMsec()
            + (ulong)Math.Max(0, Mathf.CeilToInt(animationDuration * 1000f));
        _searchShuffleResolveAfterMsec = Math.Max(
            _searchShuffleResolveAfterMsec,
            resolveAfterMsec
        );
        if (_searchShuffleResolutionDeferred)
            return;

        _searchShuffleResolutionDeferred = true;
        CallDeferred(nameof(ResolvePendingSearchAfterShuffleDeferred));
    }

    private async void ResolvePendingSearchAfterShuffleDeferred()
    {
        _searchShuffleResolutionDeferred = false;
        await ResolvePendingSearchAfterShuffleAsync();
    }

    public async Task ResolvePendingSearchAfterShuffleAsync()
    {
        if (
            _resolvingSearchAfterShuffle
            || _pendingSearchShuffleCount <= 0
            || !IsResolvingPlayerTeamActionPhase
            || CharacterControl == null
            || !GodotObject.IsInstanceValid(CharacterControl)
        )
        {
            return;
        }

        _resolvingSearchAfterShuffle = true;
        try
        {
            while (_pendingSearchShuffleCount > 0)
            {
                ulong nowMsec = Time.GetTicksMsec();
                if (_searchShuffleResolveAfterMsec > nowMsec)
                {
                    bool continued = await DelayOrCancel(
                        (int)Math.Min(_searchShuffleResolveAfterMsec - nowMsec, int.MaxValue),
                        _lifetimeCts.Token
                    );
                    if (!continued)
                        return;
                    continue;
                }

                _pendingSearchShuffleCount--;
                if (_pendingSearchShuffleCount == 0)
                    _searchShuffleResolveAfterMsec = 0;
                if (!IsBattleAlive() || HasBattleEnded())
                {
                    _pendingSearchShuffleCount = 0;
                    _searchShuffleResolveAfterMsec = 0;
                    break;
                }

                PlayerCharacter[] owners = SpecialBuff.GetSearchOwners(this);
                for (int i = 0; i < owners.Length; i++)
                {
                    PlayerCharacter owner = owners[i];
                    if (
                        !IsCharacterAlive(owner)
                        || GetPlayerTeamBattleHandEmptySlotCount() <= 0
                        || GetDrawBattleCardPileEntries(owner).Length == 0
                    )
                    {
                        break;
                    }

                    Buff.FlashTriggersOnOwner(owner, Buff.BuffName.Search);
                    await CharacterControl.SelectDrawPileCardsToHandAsync(owner, 1);
                }
            }
        }
        finally
        {
            _resolvingSearchAfterShuffle = false;
        }
    }

    private async Task DrawPlayerTeamTurnStartCardsWithShuffleBreaksAsync(
        int count,
        CancellationToken token
    )
    {
        if (count <= 0 || EndActionBuff.TryBlockPlayerTeamDraw(this))
            return;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return;

        Skill[] batchOldHand = GetPlayerTeamBattleHand().ToArray();
        bool batchDrewAny = false;
        int remaining = count;
        while (remaining > 0)
        {
            if (PickDrawableTeamCardIndex(piles.DrawPile, Skill.SkillTypes.none) < 0)
            {
                if (piles.DrawPile.Count > 0 || piles.DiscardPile.Count == 0)
                    break;

                if (batchDrewAny)
                {
                    CharacterControl?.QueueBatchDrawMakeRoomAnimation(
                        batchOldHand,
                        GetPlayerTeamBattleHand()
                    );
                    CharacterControl?.RefreshCurrentTurnUi();
                    batchOldHand = GetPlayerTeamBattleHand().ToArray();
                    batchDrewAny = false;
                }

                if (ShufflePlayerTeamBattleDeck(piles, moveDiscardIntoDrawPile: true) > 0)
                {
                    await ResolvePendingSearchAfterShuffleAsync();
                    if (!CanContinue(token) || HasBattleEnded() || !IsBattleAlive())
                        return;
                }
                batchOldHand = GetPlayerTeamBattleHand().ToArray();
                continue;
            }

            if (!DrawOneAvailablePlayerTeamBattleCard(piles))
                break;

            batchDrewAny = true;
            remaining--;
        }

        if (batchDrewAny)
        {
            CharacterControl?.QueueBatchDrawMakeRoomAnimation(
                batchOldHand,
                GetPlayerTeamBattleHand()
            );
            CharacterControl?.RefreshCurrentTurnUi();
        }

        InvalidatePlayerTeamSkillTooltips();
    }

    private bool DrawOneAvailablePlayerTeamBattleCard(PlayerTeamBattleCardPiles piles)
    {
        if (piles == null)
            return false;

        // Card effects can draw while queued plays are still resolving. Normalize the
        // shared hand first so a recycled card cannot fill a hole ahead of later cards.
        CompactPlayerTeamBattleHand(queueReorderAnimation: false);
        Skill[] hand = GetPlayerTeamBattleHand();
        int handIndex = Array.FindIndex(hand, skill => skill == null);
        if (handIndex < 0)
            return false;

        int pickedIndex = PickDrawableTeamCardIndex(piles.DrawPile, Skill.SkillTypes.none);
        if (pickedIndex < 0)
            return false;

        BattleCardPileEntry picked = piles.DrawPile[pickedIndex];
        piles.DrawPile.RemoveAt(pickedIndex);
        Skill drawnSkill = Skill.GetSkill(picked.SkillId);
        if (drawnSkill == null)
            return false;

        drawnSkill.OwnerCharater = picked.Owner;
        drawnSkill.BattleCardInstanceId = picked.InstanceId;
        PlayerCharacter contextPlayer = ResolveTeamCardContextPlayer(picked.Owner);
        drawnSkill.UpdateDescription();
        hand[handIndex] = drawnSkill;
        contextPlayer?.InvalidateSkillTooltipCache();
        drawnSkill.OnDrawnToHand(contextPlayer);
        AudioManager.PlayCardDeal(this);
        return true;
    }

    private bool DrawOnePlayerTeamBattleCard()
    {
        CompactPlayerTeamBattleHand(queueReorderAnimation: false);
        Skill[] hand = GetPlayerTeamBattleHand();
        int handIndex = Array.FindIndex(hand, skill => skill == null);
        if (handIndex < 0)
            return false;

        Skill drawnSkill = DrawPlayerTeamBattleSkill();
        if (drawnSkill == null)
            return false;

        PlayerCharacter owner = drawnSkill.OwnerCharater as PlayerCharacter;
        drawnSkill.UpdateDescription();
        hand[handIndex] = drawnSkill;
        owner?.InvalidateSkillTooltipCache();
        drawnSkill.OnDrawnToHand(owner);
        AudioManager.PlayCardDeal(this);
        return true;
    }

    public bool TryDrawPlayerTeamBattleCards(int count, bool refreshUi = true)
    {
        if (count <= 0 || EndActionBuff.TryBlockPlayerTeamDraw(this))
            return false;

        int beforeCount = GetPlayerTeamBattleHandCardCount();
        DrawPlayerTeamBattleCards(count);
        bool drewAny = GetPlayerTeamBattleHandCardCount() > beforeCount;
        if (drewAny && refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        return drewAny;
    }

    private Skill DrawPlayerTeamBattleSkill(Skill.SkillTypes skillType = Skill.SkillTypes.none)
    {
        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null || !TryEnsureDrawableTeamCards(piles, skillType))
            return null;

        int pickedIndex = PickDrawableTeamCardIndex(piles.DrawPile, skillType);
        if (pickedIndex < 0)
            return null;

        BattleCardPileEntry picked = piles.DrawPile[pickedIndex];
        piles.DrawPile.RemoveAt(pickedIndex);

        Skill pickedSkill = Skill.GetSkill(picked.SkillId);
        if (pickedSkill != null)
        {
            pickedSkill.OwnerCharater = picked.Owner;
            pickedSkill.BattleCardInstanceId = picked.InstanceId;
        }
        return pickedSkill;
    }

    private void ResolvePlayerTeamExtraDraw()
    {
        foreach (PlayerCharacter player in GetPlayerPhaseActionOrder())
        {
            if (!IsCharacterAlive(player))
                continue;

            if (
                GetPlayerTeamBattleHandEmptySlotCount() <= 0
                || SpecialBuff.GetCardRefreshStack(player) <= 0
            )
                continue;

            int beforeCount = GetPlayerTeamBattleHandCardCount();
            TryDrawPlayerTeamBattleCards(1, refreshUi: false);
            int drawnCount = Math.Max(0, GetPlayerTeamBattleHandCardCount() - beforeCount);
            SpecialBuff.ConsumeCardRefresh(player, drawnCount);
        }
    }

    private void EnsurePlayerTeamBattleHandSize()
    {
        if (_playerTeamBattleHand != null && _playerTeamBattleHand.Length == PlayerCharacter.MaxBattleHandSize)
            return;

        Skill[] resized = new Skill[PlayerCharacter.MaxBattleHandSize];
        if (_playerTeamBattleHand != null)
            Array.Copy(_playerTeamBattleHand, resized, Math.Min(_playerTeamBattleHand.Length, resized.Length));
        _playerTeamBattleHand = resized;
    }

    private int GetPlayerTeamBattleHandCardCount()
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        return hand.Count(skill => skill != null);
    }

    public int GetPlayerTeamBattleHandEmptySlotCount()
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        return hand.Count(skill => skill == null);
    }

    public void QueuePredictionAtTurnStart(Character owner, int stacks)
    {
        PlayerCharacter player = ResolveCardPileOwner(owner);
        if (!IsCharacterAlive(player) || stacks <= 0)
            return;

        _pendingPredictionViewCount += stacks;
    }

    private async Task ResolvePendingPredictionAtTurnStartAsync(PlayerCharacter teamContext)
    {
        int viewCount = _pendingPredictionViewCount;
        _pendingPredictionViewCount = 0;
        if (
            viewCount <= 0
            || !IsCharacterAlive(teamContext)
            || CharacterControl == null
            || !GodotObject.IsInstanceValid(CharacterControl)
            || GetDrawBattleCardPileEntries(teamContext).Length == 0
        )
        {
            return;
        }

        await CharacterControl.FilterTopDrawPileCardsAsync(teamContext, viewCount);
    }

    public bool DrawPlayerBattleCardsToTeamHand(PlayerCharacter player, int count, bool refreshUi = true)
    {
        if (
            count <= 0
            || !IsCharacterAlive(player)
            || EndActionBuff.TryBlockPlayerTeamDraw(this)
        )
            return false;

        CompactPlayerTeamBattleHand(queueReorderAnimation: false);
        Skill[] hand = GetPlayerTeamBattleHand();
        int beforeCount = GetPlayerTeamBattleHandCardCount();
        var filledIndexes = new List<int>();
        for (int i = 0; i < hand.Length && count > 0; i++)
        {
            if (hand[i] != null)
                continue;

            Skill drawnSkill = DrawPlayerBattleSkill(player);
            if (drawnSkill == null)
                break;

            drawnSkill.OwnerCharater = player;
            drawnSkill.UpdateDescription();
            hand[i] = drawnSkill;
            filledIndexes.Add(i);
            drawnSkill.OnDrawnToHand(player);
            AudioManager.PlayCardDeal(this);
            count--;
        }

        bool drewAny = GetPlayerTeamBattleHandCardCount() > beforeCount;
        if (drewAny)
        {
            player.InvalidateSkillTooltipCache();
            if (refreshUi)
            {
                CharacterControl?.PrepareHandCardDrawEntry(
                    filledIndexes,
                    HandCardEntryOrigin.DrawPile
                );
                CharacterControl?.RefreshCurrentTurnUi();
            }
        }
        return drewAny;
    }

    public bool TryMoveDrawPileCardToDiscard(int pileIndex, bool refreshUi = true)
    {
        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null || pileIndex < 0 || pileIndex >= piles.DrawPile.Count)
            return false;

        BattleCardPileEntry entry = piles.DrawPile[pileIndex];
        piles.DrawPile.RemoveAt(pileIndex);
        piles.DiscardPile.Add(entry);
        entry.Owner?.InvalidateSkillTooltipCache();
        InvalidatePlayerTeamSkillTooltips();
        if (refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        return true;
    }

    public bool TryMoveDrawPileCardToTeamHand(
        PlayerCharacter player,
        int pileIndex,
        bool refreshUi = true
    ) => TryMoveBattlePileCardToTeamHand(
        player,
        pileIndex,
        piles => piles.DrawPile,
        refreshUi
    );

    public bool TryMoveDiscardPileCardToTeamHand(
        PlayerCharacter player,
        int pileIndex,
        bool refreshUi = true
    ) => TryMoveBattlePileCardToTeamHand(
        player,
        pileIndex,
        piles => piles.DiscardPile,
        refreshUi
    );

    private bool TryMoveBattlePileCardToTeamHand(
        PlayerCharacter player,
        int pileIndex,
        Func<PlayerTeamBattleCardPiles, List<BattleCardPileEntry>> selectPile,
        bool refreshUi
    )
    {
        if (pileIndex < 0 || selectPile == null)
            return false;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        List<BattleCardPileEntry> sourcePile = piles == null ? null : selectPile(piles);
        if (sourcePile == null || pileIndex >= sourcePile.Count)
            return false;

        CompactPlayerTeamBattleHand(queueReorderAnimation: false);
        Skill[] hand = GetPlayerTeamBattleHand();
        int handIndex = Array.FindIndex(hand, skill => skill == null);
        if (handIndex < 0)
            return false;

        BattleCardPileEntry entry = sourcePile[pileIndex];
        PlayerCharacter contextPlayer = ResolveTeamCardContextPlayer(entry.Owner) ?? player;
        Skill skill = Skill.GetSkill(entry.SkillId);
        if (skill == null)
            return false;

        sourcePile.RemoveAt(pileIndex);
        skill.OwnerCharater = entry.Owner;
        skill.BattleCardInstanceId = entry.InstanceId;
        skill.UpdateDescription();
        hand[handIndex] = skill;
        skill.OnDrawnToHand(contextPlayer);
        AudioManager.PlayCardDeal(this);
        contextPlayer?.InvalidateSkillTooltipCache();

        InvalidatePlayerTeamSkillTooltips();
        if (refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        return true;
    }

    public void RemovePlayerTeamBattleHandCardAt(
        int skillIndex,
        bool queueReorderAnimation = true
    )
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        if (skillIndex < 0 || skillIndex >= hand.Length)
            return;

        hand[skillIndex] = null;
        CompactPlayerTeamBattleHand(queueReorderAnimation);
        InvalidatePlayerTeamSkillTooltips();
    }

    public bool TryRestorePlayerTeamBattleHandCardAt(int skillIndex, Skill skill)
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        if (skill == null || skillIndex < 0 || skillIndex >= hand.Length)
            return false;

        if (hand[skillIndex] != null)
        {
            int emptyIndex = Array.FindIndex(hand, skillIndex, x => x == null);
            if (emptyIndex < 0)
                return false;

            for (int i = emptyIndex; i > skillIndex; i--)
                hand[i] = hand[i - 1];
        }

        if (skill.OwnerCharater is PlayerCharacter owner)
            owner.InvalidateSkillTooltipCache();
        skill.UpdateDescription();
        hand[skillIndex] = skill;
        return true;
    }

    private bool CompactPlayerTeamBattleHand(bool queueReorderAnimation = true)
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        Skill[] oldHand = hand.ToArray();
        Skill[] compacted = new Skill[PlayerCharacter.MaxBattleHandSize];
        int next = 0;
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null)
                continue;

            compacted[next++] = hand[i];
        }

        bool changed = oldHand.Length != compacted.Length;
        if (!changed)
        {
            for (int i = 0; i < oldHand.Length; i++)
            {
                if (!ReferenceEquals(oldHand[i], compacted[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return false;

        if (queueReorderAnimation)
            CharacterControl?.QueueHandReorderAnimation(oldHand, compacted);
        _playerTeamBattleHand = compacted;
        return true;
    }

    private PlayerCharacter GetBattlePileOwner(Skill skill, PlayerCharacter fallback = null)
    {
        return skill?.OwnerCharater as PlayerCharacter ?? fallback;
    }

    public PlayerCharacter ResolveTeamCardContextPlayer(PlayerCharacter explicitOwner = null)
    {
        if (explicitOwner != null && GodotObject.IsInstanceValid(explicitOwner))
            return explicitOwner;

        if (CurrentActionCharacter is PlayerCharacter current && IsCharacterAlive(current))
            return current;

        return GetPlayerPhaseActionOrder().FirstOrDefault(IsCharacterAlive);
    }

    private void InvalidatePlayerTeamSkillTooltips()
    {
        foreach (PlayerCharacter player in GetPlayerPhaseActionOrder())
            player?.InvalidateSkillTooltipCache();

        // Carry availability is derived from the shared draw pile. Updating only the
        // character tooltip cache leaves a hovered card's preview revision unchanged,
        // so messages such as "无可用牌" survive a shuffle or draw-pile mutation.
        NotifyHandPreviewContextChanged();
    }

    private static int PickDrawableCardIndex(
        List<SkillID> drawPile,
        Skill.SkillTypes skillType,
        SkillID? avoidSkillId = null
    )
    {
        int[] candidateIndexes = GetDrawableCardIndexes(drawPile, skillType);
        if (candidateIndexes.Length == 0)
            return -1;

        if (avoidSkillId.HasValue && candidateIndexes.Length > 1)
        {
            foreach (int index in candidateIndexes)
            {
                if (drawPile[index] != avoidSkillId.Value)
                    return index;
            }
        }

        return candidateIndexes[0];
    }

    public void AddPlayerBattleStatusCardsToDrawPile(
        PlayerCharacter player,
        SkillID skillId,
        int count,
        Character source = null
    ) => AddPlayerBattleStatusCards(player, skillId, count, BattleCardPileTarget.DrawPileCards, source);

    public void EnsureDifficultyBattleStartDazeCards(int count)
    {
        if (count <= 0)
            return;

        GetOrCreatePlayerTeamBattleCardPiles();
        AddPlayerBattleStatusCards(
            null,
            SkillID.DazeStatus,
            count,
            BattleCardPileTarget.DrawPileCards,
            null
        );
    }

    public SkillID? PickRandomOwnedBattleSkillId(PlayerCharacter player) =>
        GameInfo.PickRandomOwnedBattleSkillId(player, GetOrCreatePlayerTeamBattleSkillRandom());

    public SkillID? PickRandomTransformBattleSkillId(
        PlayerCharacter player,
        SkillID sourceSkillId
    ) =>
        GameInfo.PickRandomTransformBattleSkillId(
            player,
            sourceSkillId,
            GetOrCreatePlayerTeamBattleSkillRandom()
        );

    public void AddPlayerBattleStatusCards(
        PlayerCharacter cardOwner,
        SkillID skillId,
        int count,
        BattleCardPileTarget pileTarget = BattleCardPileTarget.DrawPileCards,
        Character source = null
    )
    {
        if (count <= 0)
            return;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return;

        Random rng = GetOrCreatePlayerTeamBattleSkillRandom();
        if (pileTarget == BattleCardPileTarget.HandCards)
        {
            AddPlayerBattleStatusCardsToHand(
                cardOwner,
                skillId,
                count,
                source,
                entryOrigin: HandCardEntryOrigin.PlayedCard
            );
            return;
        }

        if (pileTarget == BattleCardPileTarget.DiscardPileCards)
        {
            for (int i = 0; i < count; i++)
            {
                piles.DiscardPile.Add(
                    new BattleCardPileEntry(cardOwner, skillId, CreateBattleCardInstanceId())
                );
            }
        }
        else
        {
            if (piles.DrawPile.Count == 0 && piles.DiscardPile.Count > 0)
                ShufflePlayerTeamBattleDeck(piles, moveDiscardIntoDrawPile: true);
            for (int i = 0; i < count; i++)
            {
                int insertIndex = rng.Next(piles.DrawPile.Count + 1);
                piles.DrawPile.Insert(
                    insertIndex,
                    new BattleCardPileEntry(cardOwner, skillId, CreateBattleCardInstanceId())
                );
            }
        }

        if (cardOwner != null && GodotObject.IsInstanceValid(cardOwner))
            cardOwner.InvalidateSkillTooltipCache();
        else
            InvalidatePlayerTeamSkillTooltips();

        RecordStatusCardInsert(cardOwner, skillId, count, pileTarget: pileTarget, source: source);
    }

    public int AddPlayerBattleStatusCardsToHand(
        PlayerCharacter cardOwner,
        SkillID skillId,
        int count,
        Character source = null,
        HandCardEntryOrigin entryOrigin = HandCardEntryOrigin.Default
    )
    {
        if (count <= 0)
            return 0;

        CompactPlayerTeamBattleHand(queueReorderAnimation: false);
        Skill[] hand = GetPlayerTeamBattleHand();
        if (hand == null)
            return 0;

        PlayerCharacter contextPlayer = ResolveTeamCardContextPlayer(cardOwner);
        int added = 0;
        var filledIndexes = new List<int>(count);
        for (int i = 0; i < hand.Length && added < count; i++)
        {
            if (hand[i] != null)
                continue;

            Skill skill = Skill.GetSkill(skillId);
            if (skill == null)
                continue;

            skill.OwnerCharater = cardOwner;
            skill.BattleCardInstanceId = CreateBattleCardInstanceId();
            skill.UpdateDescription();
            hand[i] = skill;
            filledIndexes.Add(i);
            skill.OnDrawnToHand(contextPlayer);
            added++;
        }

        if (added > 0)
        {
            if (cardOwner != null && GodotObject.IsInstanceValid(cardOwner))
                cardOwner.InvalidateSkillTooltipCache();
            else
                InvalidatePlayerTeamSkillTooltips();

            RecordStatusCardInsert(cardOwner, skillId, added, toHand: true, source: source);
            CharacterControl?.PrepareHandCardDrawEntry(filledIndexes, entryOrigin);
            CharacterControl?.RefreshCurrentTurnUi();
        }

        return added;
    }

    public void DiscardBattleSkill(
        Character actor,
        Skill skill,
        bool atTurnEnd = false,
        bool forceDiscard = false,
        bool triggerExhaustEffects = true
    )
    {
        if (actor is not PlayerCharacter player || skill == null || !skill.SkillId.HasValue)
        {
            return;
        }

        if (skill.IsAbilityCard && !atTurnEnd && !forceDiscard)
        {
            player.InvalidateSkillTooltipCache();
            return;
        }

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return;

        SkillID skillId = skill.SkillId.Value;
        var entry = new BattleCardPileEntry(
            player,
            skillId,
            GetOrCreateBattleCardInstanceId(skill)
        );
        bool exhausted =
            !forceDiscard
            && (
                (!atTurnEnd && skill.ResolvesExhaustsAfterUse)
                || (atTurnEnd && skill.ResolvesExhaustsAtTurnEndInHand)
            );
        if (exhausted)
        {
            piles.Exhausted.Add(entry);
            if (triggerExhaustEffects)
                RecordPlayerBattleCardsExhausted(1);
        }
        else
            piles.DiscardPile.Add(entry);

        player.InvalidateSkillTooltipCache();
    }

    public void RecordPlayerBattleCardsExhausted(int count)
    {
        if (count <= 0)
            return;

        SpecialBuff.TriggerExhaustShieldBlock(this, count);
        SpecialBuff.TriggerRecyclingDraw(this, count);
        EndActionBuff.TriggerDemonPowerOnExhaust(this, count);
    }

    public Task ExhaustAllPlayerBattleStatusCardsAsync(Character source = null)
    {
        var previewEntries = new List<CharacterControl.StatusCardExhaustAnimationEntry>();
        var handIndexes = new List<int>();

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        PlayerCharacter teamContext = GetPlayerPhaseActionOrder().FirstOrDefault(IsCharacterAlive);
        int exhaustedCount = 0;
        if (piles != null)
        {
            exhaustedCount += MoveStatusCardsToExhausted(
                piles.DrawPile,
                piles.Exhausted,
                previewEntries,
                teamContext
            );
            exhaustedCount += MoveStatusCardsToExhausted(
                piles.DiscardPile,
                piles.Exhausted,
                previewEntries,
                teamContext
            );
        }

        HandStatusExhaustInfo handInfo = CollectHandStatusCardIndexes(piles);
        if (handInfo.HasAny)
        {
            exhaustedCount += handInfo.SkillIds.Count;
            handIndexes.AddRange(handInfo.Indexes);
            for (int i = 0; i < handInfo.SkillIds.Count; i++)
            {
                PlayerCharacter owner =
                    i < handInfo.Owners.Count ? handInfo.Owners[i] : null;
                previewEntries.Add(
                    new CharacterControl.StatusCardExhaustAnimationEntry(
                        owner ?? teamContext,
                        handInfo.SkillIds[i]
                    )
                );
            }
        }

        if (exhaustedCount <= 0)
            return Task.CompletedTask;

        _ = FinishAllPlayerBattleStatusExhaustAsync(handIndexes, previewEntries, exhaustedCount);
        return Task.CompletedTask;
    }

    private async Task FinishAllPlayerBattleStatusExhaustAsync(
        List<int> handIndexes,
        List<CharacterControl.StatusCardExhaustAnimationEntry> previewEntries,
        int exhaustedCount
    )
    {
        CharacterControl control = CharacterControl;
        try
        {
            if (
                previewEntries.Count > 0
                && control != null
                && GodotObject.IsInstanceValid(control)
            )
            {
                await control.PlayDyingOwnedCardExhaustAnimationAsync(
                    previewEntries,
                    handIndexes
                );
            }
        }
        finally
        {
            foreach (int index in handIndexes.OrderByDescending(x => x))
                RemovePlayerTeamBattleHandCardAt(index);

            if (control != null && GodotObject.IsInstanceValid(control))
            {
                control.ClearHandStatusExhaustPending(handIndexes);
                if (handIndexes.Count > 0)
                    control.HideHandCardsForDyingRemoval(handIndexes);
            }

            foreach (PlayerCharacter player in GetPlayerPhaseActionOrder())
                player?.InvalidateSkillTooltipCache();
            RecordPlayerBattleCardsExhausted(exhaustedCount);
            control?.RefreshCurrentTurnUi();
        }
    }

    public async Task<int> ExhaustPlayerTeamBattleCardsAsync(
        BattleCardPileTarget pileTarget,
        int count,
        bool random = true,
        PlayerCharacter teamContext = null
    )
    {
        count = Math.Max(0, count);
        if (count <= 0)
            return 0;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return 0;

        teamContext ??= GetPlayerPhaseActionOrder().FirstOrDefault(IsCharacterAlive);
        var handIndexes = new List<int>();
        var previewEntries = new List<CharacterControl.StatusCardExhaustAnimationEntry>();
        Random rng = random ? GetOrCreatePlayerTeamBattleSkillRandom() : null;

        int exhaustedCount = pileTarget switch
        {
            BattleCardPileTarget.HandCards => ExhaustPlayerTeamHandCards(
                piles,
                count,
                random,
                teamContext,
                handIndexes,
                previewEntries
            ),
            BattleCardPileTarget.DiscardPileCards => ExhaustBattlePileCards(
                piles.DiscardPile,
                piles.Exhausted,
                count,
                random,
                rng,
                previewEntries
            ),
            _ => ExhaustBattlePileCards(
                piles.DrawPile,
                piles.Exhausted,
                count,
                random,
                rng,
                previewEntries
            ),
        };

        if (exhaustedCount <= 0)
            return 0;

        if (CharacterControl != null && GodotObject.IsInstanceValid(CharacterControl))
        {
            var animationTasks = new List<Task>();
            if (
                handIndexes.Count > 0
                && teamContext != null
                && CharacterControl.CanAnimateHandCardsFor(teamContext)
            )
            {
                animationTasks.Add(
                    CharacterControl.PlayHandCardExhaustAnimationAsync(teamContext, handIndexes)
                );
            }
            if (previewEntries.Count > 0)
                animationTasks.Add(CharacterControl.PlayOwnedCardExhaustPreviewAnimationAsync(previewEntries));

            if (animationTasks.Count > 0)
                await Task.WhenAll(animationTasks);
        }

        Skill[] hand = GetPlayerTeamBattleHand();
        foreach (int index in handIndexes.OrderByDescending(x => x))
        {
            if (index < 0 || index >= hand.Length)
                continue;

            hand[index] = null;
        }
        CompactPlayerTeamBattleHand();
        InvalidatePlayerTeamSkillTooltips();
        CharacterControl?.RefreshCurrentTurnUi();
        RecordPlayerBattleCardsExhausted(exhaustedCount);
        return exhaustedCount;
    }

    public Task<int> TransformPlayerTeamBattleCardsAsync(
        BattleCardPileTarget pileTarget,
        int count,
        SkillID replacementSkillId = SkillID.None,
        bool random = true,
        PlayerCharacter teamContext = null
    )
    {
        count = Math.Max(0, count);
        if (count <= 0)
            return Task.FromResult(0);

        teamContext ??= GetPlayerPhaseActionOrder().FirstOrDefault(IsCharacterAlive);
        int transformedCount = pileTarget == BattleCardPileTarget.HandCards
            ? TransformPlayerTeamHandCards(count, replacementSkillId, random, teamContext)
            : TransformBattlePileCards(
                pileTarget,
                count,
                replacementSkillId,
                random,
                teamContext
            );

        if (transformedCount > 0)
        {
            InvalidatePlayerTeamSkillTooltips();
            CharacterControl?.RefreshCurrentTurnUi();
        }

        return Task.FromResult(transformedCount);
    }

    public bool TryCreateTransformedBattleSkill(
        Skill sourceSkill,
        SkillID replacementSkillId,
        PlayerCharacter fallbackOwner,
        out Skill transformedSkill
    )
    {
        transformedSkill = null;
        if (sourceSkill?.SkillId is not SkillID sourceSkillId)
            return false;

        PlayerCharacter sourceOwner = GetBattlePileOwner(sourceSkill, fallbackOwner);
        SkillID? resolvedSkillId = ResolveBattleCardTransformSkillId(
            sourceOwner,
            sourceSkillId,
            replacementSkillId
        );
        if (!resolvedSkillId.HasValue)
            return false;

        Skill replacement = Skill.GetSkill(resolvedSkillId.Value);
        if (replacement == null)
            return false;

        replacement.OwnerCharater = replacement.IsStatusCard ? null : sourceOwner;
        replacement.BattleCardInstanceId = GetOrCreateBattleCardInstanceId(sourceSkill);
        replacement.UpdateDescription();
        transformedSkill = replacement;
        return true;
    }

    public bool TryTransformBattlePileCard(
        BattleCardPileTarget pileTarget,
        int pileIndex,
        SkillID replacementSkillId,
        PlayerCharacter fallbackOwner = null,
        bool refreshUi = true
    )
    {
        if (pileIndex < 0)
            return false;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        List<BattleCardPileEntry> sourcePile = pileTarget switch
        {
            BattleCardPileTarget.DiscardPileCards => piles?.DiscardPile,
            BattleCardPileTarget.DrawPileCards => piles?.DrawPile,
            _ => null,
        };
        if (sourcePile == null || pileIndex >= sourcePile.Count)
            return false;

        BattleCardPileEntry entry = sourcePile[pileIndex];
        PlayerCharacter sourceOwner = entry.Owner ?? fallbackOwner;
        SkillID? resolvedSkillId = ResolveBattleCardTransformSkillId(
            sourceOwner,
            entry.SkillId,
            replacementSkillId
        );
        if (!resolvedSkillId.HasValue)
            return false;

        Skill replacement = Skill.GetSkill(resolvedSkillId.Value);
        if (replacement == null)
            return false;

        PlayerCharacter replacementOwner = replacement.IsStatusCard ? null : sourceOwner;
        sourcePile[pileIndex] = new BattleCardPileEntry(
            replacementOwner,
            resolvedSkillId.Value,
            entry.InstanceId
        );
        entry.Owner?.InvalidateSkillTooltipCache();
        replacementOwner?.InvalidateSkillTooltipCache();
        InvalidatePlayerTeamSkillTooltips();
        if (refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        return true;
    }

    private int TransformPlayerTeamHandCards(
        int count,
        SkillID replacementSkillId,
        bool random,
        PlayerCharacter teamContext
    )
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        int[] indexes = hand
            .Select((skill, index) => new { Skill = skill, Index = index })
            .Where(entry => entry.Skill?.SkillId.HasValue == true)
            .Select(entry => entry.Index)
            .ToArray();
        if (random && indexes.Length > 1)
            ShuffleIndexes(indexes, GetOrCreatePlayerTeamBattleSkillRandom());

        int transformedCount = 0;
        for (int i = 0; i < indexes.Length && transformedCount < count; i++)
        {
            int handIndex = indexes[i];
            Skill sourceSkill = hand[handIndex];
            if (
                !TryCreateTransformedBattleSkill(
                    sourceSkill,
                    replacementSkillId,
                    teamContext,
                    out Skill transformedSkill
                )
            )
            {
                continue;
            }

            hand[handIndex] = transformedSkill;
            transformedCount++;
        }

        return transformedCount;
    }

    private int TransformBattlePileCards(
        BattleCardPileTarget pileTarget,
        int count,
        SkillID replacementSkillId,
        bool random,
        PlayerCharacter teamContext
    )
    {
        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        List<BattleCardPileEntry> sourcePile = pileTarget switch
        {
            BattleCardPileTarget.DiscardPileCards => piles?.DiscardPile,
            BattleCardPileTarget.DrawPileCards => piles?.DrawPile,
            _ => null,
        };
        if (sourcePile == null || sourcePile.Count == 0)
            return 0;

        int[] indexes = Enumerable.Range(0, sourcePile.Count).ToArray();
        if (random && indexes.Length > 1)
            ShuffleIndexes(indexes, GetOrCreatePlayerTeamBattleSkillRandom());

        int transformedCount = 0;
        for (int i = 0; i < indexes.Length && transformedCount < count; i++)
        {
            if (
                TryTransformBattlePileCard(
                    pileTarget,
                    indexes[i],
                    replacementSkillId,
                    teamContext,
                    refreshUi: false
                )
            )
            {
                transformedCount++;
            }
        }

        return transformedCount;
    }

    private SkillID? ResolveBattleCardTransformSkillId(
        PlayerCharacter sourceOwner,
        SkillID sourceSkillId,
        SkillID replacementSkillId
    )
    {
        if (replacementSkillId != SkillID.None)
        {
            return replacementSkillId != sourceSkillId
                && Skill.GetSkill(replacementSkillId) != null
                ? replacementSkillId
                : null;
        }

        if (sourceOwner == null || !GodotObject.IsInstanceValid(sourceOwner))
            return null;

        return PickRandomTransformBattleSkillId(sourceOwner, sourceSkillId);
    }

    public int ExhaustPlayerTeamBattleSkills(IEnumerable<Skill> skills, PlayerCharacter fallbackOwner = null)
    {
        if (skills == null)
            return 0;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return 0;

        int exhaustedCount = 0;
        foreach (Skill skill in skills)
        {
            if (skill == null || !skill.SkillId.HasValue)
                continue;

            PlayerCharacter owner = GetBattlePileOwner(skill, fallbackOwner);
            piles.Exhausted.Add(
                new BattleCardPileEntry(
                    owner,
                    skill.SkillId.Value,
                    GetOrCreateBattleCardInstanceId(skill)
                )
            );
            owner?.InvalidateSkillTooltipCache();
            exhaustedCount++;
        }

        if (exhaustedCount > 0)
        {
            InvalidatePlayerTeamSkillTooltips();
            CharacterControl?.RefreshCurrentTurnUi();
            RecordPlayerBattleCardsExhausted(exhaustedCount);
        }

        return exhaustedCount;
    }

    public bool TryExhaustBattlePileCard(
        BattleCardPileTarget pileTarget,
        int pileIndex,
        bool refreshUi = true
    )
    {
        if (pileIndex < 0)
            return false;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        List<BattleCardPileEntry> sourcePile = pileTarget switch
        {
            BattleCardPileTarget.DiscardPileCards => piles?.DiscardPile,
            BattleCardPileTarget.DrawPileCards => piles?.DrawPile,
            _ => null,
        };
        if (sourcePile == null || pileIndex >= sourcePile.Count)
            return false;

        BattleCardPileEntry entry = sourcePile[pileIndex];
        sourcePile.RemoveAt(pileIndex);
        piles.Exhausted.Add(entry);
        entry.Owner?.InvalidateSkillTooltipCache();
        InvalidatePlayerTeamSkillTooltips();
        if (refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        RecordPlayerBattleCardsExhausted(1);
        return true;
    }

    private int ExhaustPlayerTeamHandCards(
        PlayerTeamBattleCardPiles piles,
        int count,
        bool random,
        PlayerCharacter teamContext,
        List<int> handIndexes,
        List<CharacterControl.StatusCardExhaustAnimationEntry> previewEntries
    )
    {
        if (piles == null || count <= 0 || handIndexes == null)
            return 0;

        Skill[] hand = GetPlayerTeamBattleHand();
        bool canAnimateHand =
            CharacterControl != null
            && GodotObject.IsInstanceValid(CharacterControl)
            && teamContext != null
            && CharacterControl.CanAnimateHandCardsFor(teamContext);

        int[] indexes = hand
            .Select((skill, index) => new { skill, index })
            .Where(entry => entry.skill?.SkillId.HasValue == true)
            .Select(entry => entry.index)
            .ToArray();
        if (random && indexes.Length > 1)
            ShuffleIndexes(indexes, GetOrCreatePlayerTeamBattleSkillRandom());

        int exhaustedCount = 0;
        for (int i = 0; i < indexes.Length && exhaustedCount < count; i++)
        {
            int handIndex = indexes[i];
            Skill skill = hand[handIndex];
            if (skill == null || !skill.SkillId.HasValue)
                continue;

            PlayerCharacter owner = GetBattlePileOwner(skill, teamContext);
            SkillID skillId = skill.SkillId.Value;
            piles.Exhausted.Add(
                new BattleCardPileEntry(owner, skillId, GetOrCreateBattleCardInstanceId(skill))
            );
            handIndexes.Add(handIndex);
            if (!canAnimateHand && owner != null)
            {
                previewEntries?.Add(
                    new CharacterControl.StatusCardExhaustAnimationEntry(owner, skillId)
                );
            }
            exhaustedCount++;
        }

        return exhaustedCount;
    }

    private static int ExhaustBattlePileCards(
        List<BattleCardPileEntry> source,
        List<BattleCardPileEntry> exhausted,
        int count,
        bool random,
        Random rng,
        List<CharacterControl.StatusCardExhaustAnimationEntry> previewEntries = null
    )
    {
        if (source == null || exhausted == null || count <= 0)
            return 0;

        int exhaustedCount = 0;
        for (int i = 0; source.Count > 0 && exhaustedCount < count; i++)
        {
            int index = random ? (rng ?? new Random()).Next(source.Count) : 0;
            BattleCardPileEntry entry = source[index];
            source.RemoveAt(index);
            exhausted.Add(entry);
            if (entry.Owner != null)
            {
                previewEntries?.Add(
                    new CharacterControl.StatusCardExhaustAnimationEntry(
                        entry.Owner,
                        entry.SkillId
                    )
                );
            }
            exhaustedCount++;
        }

        return exhaustedCount;
    }

    private static void ShuffleIndexes(int[] indexes, Random rng)
    {
        if (indexes == null || indexes.Length <= 1)
            return;

        rng ??= new Random();
        for (int i = indexes.Length - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (indexes[i], indexes[swapIndex]) = (indexes[swapIndex], indexes[i]);
        }
    }

    public async Task RemovePlayerOwnedBattleCardsOnDyingAsync(PlayerCharacter player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return;

        var previewEntries = new List<CharacterControl.StatusCardExhaustAnimationEntry>();
        bool changed = false;

        changed |= RemoveOwnedCardsFromPile(piles.DrawPile, player, previewEntries);
        changed |= RemoveOwnedCardsFromPile(piles.DiscardPile, player, previewEntries);
        changed |= RemoveOwnedCardsFromPile(piles.Exhausted, player, previewEntries);

        Skill[] hand = GetPlayerTeamBattleHand();
        var handIndexes = new List<int>();

        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill == null || GetBattlePileOwner(skill) != player)
                continue;

            changed = true;
            handIndexes.Add(i);
            if (skill.SkillId.HasValue)
            {
                previewEntries.Add(
                    new CharacterControl.StatusCardExhaustAnimationEntry(player, skill.SkillId.Value)
                );
            }
        }

        if (
            previewEntries.Count > 0
            && CharacterControl != null
            && GodotObject.IsInstanceValid(CharacterControl)
        )
        {
            await CharacterControl.PlayDyingOwnedCardExhaustAnimationAsync(
                previewEntries,
                handIndexes
            );
            CharacterControl.ClearHandStatusExhaustPending(handIndexes);
            CharacterControl.HideHandCardsForDyingRemoval(handIndexes);
        }
        else if (
            handIndexes.Count > 0
            && CharacterControl != null
            && GodotObject.IsInstanceValid(CharacterControl)
        )
        {
            CharacterControl.HideHandCardsForDyingRemoval(handIndexes);
        }

        foreach (int index in handIndexes.OrderByDescending(x => x))
        {
            if (index < 0 || index >= hand.Length)
                continue;

            hand[index] = null;
        }

        CompactPlayerTeamBattleHand();
        if (changed)
            InvalidatePlayerTeamSkillTooltips();

        CharacterControl?.RefreshCurrentTurnUi();
    }

    private static bool RemoveOwnedCardsFromPile(
        List<BattleCardPileEntry> pile,
        PlayerCharacter owner,
        List<CharacterControl.StatusCardExhaustAnimationEntry> previewEntries
    )
    {
        if (pile == null || owner == null || pile.Count == 0)
            return false;

        bool removedAny = false;
        for (int i = pile.Count - 1; i >= 0; i--)
        {
            BattleCardPileEntry entry = pile[i];
            if (entry.Owner != owner)
                continue;

            pile.RemoveAt(i);
            previewEntries?.Add(
                new CharacterControl.StatusCardExhaustAnimationEntry(owner, entry.SkillId)
            );
            removedAny = true;
        }

        return removedAny;
    }

    private HandStatusExhaustInfo CollectHandStatusCardIndexes(PlayerTeamBattleCardPiles piles)
    {
        var indexes = new List<int>();
        var skillIds = new List<SkillID>();
        var owners = new List<PlayerCharacter>();
        Skill[] hand = GetPlayerTeamBattleHand();
        if (hand == null || piles == null)
            return new HandStatusExhaustInfo(indexes, skillIds, owners);

        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill == null || !skill.SkillId.HasValue)
                continue;

            SkillID skillId = skill.SkillId.Value;
            if (!IsBattleStatusCard(skillId))
                continue;

            PlayerCharacter owner = skill.OwnerCharater as PlayerCharacter;
            piles.Exhausted.Add(
                new BattleCardPileEntry(owner, skillId, GetOrCreateBattleCardInstanceId(skill))
            );
            indexes.Add(i);
            skillIds.Add(skillId);
            owners.Add(owner);
        }

        return new HandStatusExhaustInfo(indexes, skillIds, owners);
    }

    private static int MoveStatusCardsToExhausted(
        List<BattleCardPileEntry> source,
        List<BattleCardPileEntry> exhausted,
        List<CharacterControl.StatusCardExhaustAnimationEntry> previewEntries = null,
        PlayerCharacter previewOwnerFallback = null
    )
    {
        if (source == null || exhausted == null || source.Count == 0)
            return 0;

        int movedCount = 0;
        for (int i = source.Count - 1; i >= 0; i--)
        {
            BattleCardPileEntry entry = source[i];
            SkillID skillId = entry.SkillId;
            if (!IsBattleStatusCard(skillId))
                continue;

            source.RemoveAt(i);
            exhausted.Add(entry);
            previewEntries?.Add(
                new CharacterControl.StatusCardExhaustAnimationEntry(
                    entry.Owner ?? previewOwnerFallback,
                    skillId
                )
            );
            movedCount++;
        }

        return movedCount;
    }

    public async Task DiscardPlayerTeamBattleHandAtTurnEndAsync(PlayerCharacter teamContext)
    {
        Skill[] hand = GetPlayerTeamBattleHand();
        if (hand == null)
            return;

        var discardIndexes = new HashSet<int>();
        var toolboxRetainedCounts = new Dictionary<(int PlayerIndex, SkillID SkillId), int>();
        bool handChanged = false;
        int exhaustedAtTurnEndCount = 0;
        for (int i = 0; i < hand.Length; i++)
        {
            Skill skill = hand[i];
            if (skill == null)
                continue;

            PlayerCharacter skillOwner = GetBattlePileOwner(skill, teamContext);
            if (skill.TriggersAtTurnEndInHand)
            {
                if (CharacterControl != null && GodotObject.IsInstanceValid(CharacterControl))
                {
                    await CharacterControl.PlayTurnEndStatusTriggerAnimationAsync(
                        teamContext,
                        i,
                        skill,
                        () => skill.OnTurnEndInHand(skillOwner)
                    );
                }
                else
                    await skill.OnTurnEndInHand(skillOwner);

                if (hand[i] == skill && !ShouldRetainSkillAtTurnEnd(skill, toolboxRetainedCounts))
                {
                    DiscardBattleSkill(skillOwner, skill, atTurnEnd: true, forceDiscard: true);
                    hand[i] = null;
                    handChanged = true;
                }
                continue;
            }

            await skill.OnTurnEndInHand(skillOwner);
            if (hand[i] == skill && !ShouldRetainSkillAtTurnEnd(skill, toolboxRetainedCounts))
                discardIndexes.Add(i);
        }

        if (discardIndexes.Count == 0)
        {
            bool compacted = CompactPlayerTeamBattleHand();
            if (handChanged || compacted)
                CharacterControl?.RefreshCurrentTurnUi();
            InvalidatePlayerTeamSkillTooltips();
            return;
        }

        if (CharacterControl != null && GodotObject.IsInstanceValid(CharacterControl))
            _ = CharacterControl.PlayEndTurnHandDiscardAnimationsAsync(teamContext, discardIndexes);

        foreach (int index in discardIndexes)
        {
            if (index < 0 || index >= hand.Length)
                continue;

            Skill skill = hand[index];
            if (skill == null)
                continue;

            bool exhaustsAtTurnEnd = skill.ResolvesExhaustsAtTurnEndInHand;
            DiscardBattleSkill(
                GetBattlePileOwner(skill, teamContext),
                skill,
                atTurnEnd: true,
                triggerExhaustEffects: false
            );
            hand[index] = null;
            handChanged = true;
            if (exhaustsAtTurnEnd)
                exhaustedAtTurnEndCount++;
        }

        bool compactedAfterDiscard = CompactPlayerTeamBattleHand();
        if (handChanged || compactedAfterDiscard)
            CharacterControl?.RefreshCurrentTurnUi();
        InvalidatePlayerTeamSkillTooltips();
        // A Recycling draw refreshes the hand UI. Clear and compact the real hand first,
        // so that refresh cannot introduce a new card into a slot that is still moving.
        if (exhaustedAtTurnEndCount > 0)
            RecordPlayerBattleCardsExhausted(exhaustedAtTurnEndCount);
    }

    private bool ShouldRetainSkillAtTurnEnd(
        Skill skill,
        Dictionary<(int PlayerIndex, SkillID SkillId), int> toolboxRetainedCounts
    )
    {
        if (skill == null)
            return false;

        if (skill.IntrinsicRetainsAtTurnEndInHand)
            return true;

        if (skill.SkillId is not SkillID skillId || skill.OwnerCharater is not PlayerCharacter player)
            return skill.RetainsAtTurnEndInHand;

        int retainLimit = GameInfo.GetToolboxRetainCount(player.CharacterIndex, skillId);
        if (retainLimit <= 0)
            return skill.RetainsAtTurnEndInHand;

        var toolboxKey = (player.CharacterIndex, skillId);
        toolboxRetainedCounts.TryGetValue(toolboxKey, out int toolboxRetainedCount);
        if (toolboxRetainedCount >= retainLimit)
            return false;

        toolboxRetainedCounts[toolboxKey] = toolboxRetainedCount + 1;
        return true;
    }

    internal bool ShouldShowRetainKeyword(Skill skill)
    {
        if (skill == null)
            return false;

        Skill[] hand = GetPlayerTeamBattleHand();
        if (hand == null)
            return skill.RetainsAtTurnEndInHand;

        var toolboxRetainedCounts = new Dictionary<(int PlayerIndex, SkillID SkillId), int>();
        for (int i = 0; i < hand.Length; i++)
        {
            Skill handSkill = hand[i];
            if (handSkill == null)
                continue;

            bool retainsAtTurnEnd = ShouldRetainSkillAtTurnEnd(handSkill, toolboxRetainedCounts);
            if (ReferenceEquals(handSkill, skill))
                return retainsAtTurnEnd;
        }

        return skill.RetainsAtTurnEndInHand;
    }

    private static bool IsBattleStatusCard(SkillID skillId)
    {
        Skill skill = Skill.GetSkill(skillId);
        return skill?.IsStatusCard == true;
    }

    public SkillID[] GetDrawBattleCardPile(PlayerCharacter player) =>
        GetDrawBattleCardPileEntries(player).Select(entry => entry.SkillId).ToArray();

    public SkillID[] GetDiscardBattleCardPile(PlayerCharacter player) =>
        GetDiscardBattleCardPileEntries(player).Select(entry => entry.SkillId).ToArray();

    public SkillID[] GetExhaustedBattleCardPile(PlayerCharacter player) =>
        GetExhaustedBattleCardPileEntries(player).Select(entry => entry.SkillId).ToArray();

    public BattleCardPileEntry[] GetDrawBattleCardPileEntries(PlayerCharacter player) =>
        GetTeamBattleCardPileEntries(piles => piles.DrawPile);

    public BattleCardPileEntry[] GetDiscardBattleCardPileEntries(PlayerCharacter player) =>
        GetTeamBattleCardPileEntries(piles => piles.DiscardPile);

    public BattleCardPileEntry[] GetExhaustedBattleCardPileEntries(PlayerCharacter player) =>
        GetTeamBattleCardPileEntries(piles => piles.Exhausted);

    public BattleCardPileEntry[] GetOwnedDrawBattleCardPileEntries(PlayerCharacter player) =>
        GetOwnedBattleCardPileEntries(player, piles => piles.DrawPile, piles => piles.DrawPile);

    public BattleCardPileEntry[] GetOwnedDiscardBattleCardPileEntries(PlayerCharacter player) =>
        GetOwnedBattleCardPileEntries(
            player,
            piles => piles.DiscardPile,
            piles => piles.DiscardPile
        );

    public BattleCardPileEntry[] GetOwnedExhaustedBattleCardPileEntries(PlayerCharacter player) =>
        GetOwnedBattleCardPileEntries(player, piles => piles.Exhausted, piles => piles.Exhausted);

    private bool ShouldShowTeamBattleCardPiles(PlayerCharacter player)
    {
        return player != null
            && _isResolvingPlayerTeamActionPhase;
    }

    private BattleCardPileEntry[] GetPlayerBattleCardPileEntries(
        PlayerCharacter player,
        Func<PlayerBattleCardPiles, List<SkillID>> selectPile
    )
    {
        if (player == null || selectPile == null)
            return Array.Empty<BattleCardPileEntry>();

        PlayerBattleCardPiles piles = GetOrCreatePlayerBattleCardPiles(player);
        List<SkillID> pile = piles == null ? null : selectPile(piles);
        return (pile ?? new List<SkillID>())
            .Select(skillId => new BattleCardPileEntry(player, skillId, 0))
            .ToArray();
    }

    private BattleCardPileEntry[] GetTeamBattleCardPileEntries(
        Func<PlayerTeamBattleCardPiles, List<BattleCardPileEntry>> selectPile
    )
    {
        if (selectPile == null)
            return Array.Empty<BattleCardPileEntry>();

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        return (piles == null ? null : selectPile(piles))?.ToArray()
            ?? Array.Empty<BattleCardPileEntry>();
    }

    private BattleCardPileEntry[] GetOwnedBattleCardPileEntries(
        PlayerCharacter player,
        Func<PlayerTeamBattleCardPiles, List<BattleCardPileEntry>> selectTeamPile,
        Func<PlayerBattleCardPiles, List<SkillID>> selectPlayerPile
    )
    {
        if (player == null || selectTeamPile == null || selectPlayerPile == null)
            return Array.Empty<BattleCardPileEntry>();

        if (!ShouldShowTeamBattleCardPiles(player))
            return GetPlayerBattleCardPileEntries(player, selectPlayerPile);

        return GetTeamBattleCardPileEntries(selectTeamPile)
            .Where(entry => entry.Owner == player || entry.Owner == null)
            .ToArray();
    }

    public bool TryShowCharacterBattleCardPiles(Character character)
    {
        PlayerCharacter player = ResolveCardPileOwner(character);
        if (
            player == null
            || !GodotObject.IsInstanceValid(player)
            || CharacterControl == null
            || !GodotObject.IsInstanceValid(CharacterControl)
        )
        {
            return false;
        }

        return CharacterControl.ShowPlayerBattleCardPiles(player);
    }

    private static PlayerCharacter ResolveCardPileOwner(Character character)
    {
        if (character is PlayerCharacter player)
            return player;

        if (character is SummonCharacter summon)
        {
            if (summon.Summoner is PlayerCharacter summoner)
                return summoner;
            if (summon.LastSummoner is PlayerCharacter lastSummoner)
                return lastSummoner;
        }

        return null;
    }

    public bool HasDrawablePlayerBattleSkill(
        PlayerCharacter player,
        Skill.SkillTypes skillType = Skill.SkillTypes.none
    )
    {
        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return false;

        if (HasDrawableTeamCards(piles.DrawPile, skillType, player))
            return true;

        return piles.DrawPile.Count == 0 && HasDrawableTeamCards(piles.DiscardPile, skillType, player);
    }

    public bool HasDrawablePlayerCarrySkill(
        PlayerCharacter player,
        Skill.SkillTypes skillType = Skill.SkillTypes.none
    )
    {
        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        return piles != null && PickCarryTeamCardIndex(piles.DrawPile, player, skillType) >= 0;
    }

    public int ShufflePlayerTeamBattleDeck(
        bool moveDiscardIntoDrawPile = true,
        bool playAnimation = true,
        bool triggerSearch = true,
        bool refreshUi = true
    )
    {
        return ShufflePlayerTeamBattleDeck(
            GetOrCreatePlayerTeamBattleCardPiles(),
            moveDiscardIntoDrawPile,
            playAnimation,
            triggerSearch,
            refreshUi,
            recordAutomation: true
        );
    }

    private int ShufflePlayerTeamBattleDeck(
        PlayerTeamBattleCardPiles piles,
        bool moveDiscardIntoDrawPile,
        bool playAnimation = true,
        bool triggerSearch = true,
        bool refreshUi = true,
        bool recordAutomation = true
    )
    {
        if (piles == null)
            return 0;

        int shuffledCardCount = ShuffleBattleDeck(
            piles.DrawPile,
            piles.DiscardPile,
            GetOrCreatePlayerTeamBattleSkillRandom(),
            moveDiscardIntoDrawPile,
            playAnimation,
            out int movedCardCount,
            out float animationDuration
        );
        if (shuffledCardCount <= 0)
            return 0;
        if (triggerSearch)
            QueueSearchAfterShuffle(animationDuration);

        if (refreshUi || triggerSearch || recordAutomation)
            InvalidatePlayerTeamSkillTooltips();
        if (refreshUi)
            CharacterControl?.RefreshCurrentTurnUi();
        if (recordAutomation)
            RecordAutomationSnapshot("player_team_deck_shuffled");
        return movedCardCount > 0 ? movedCardCount : shuffledCardCount;
    }

    private PlayerTeamBattleCardPiles GetOrCreatePlayerTeamBattleCardPiles()
    {
        if (_playerTeamBattleCardPiles != null)
            return _playerTeamBattleCardPiles;

        if (GameInfo.PlayerCharacters == null)
            return null;

        if (PlayersList == null || PlayersList.Count < GameInfo.PlayerCharacters.Length)
            return null;

        var playerOrder = GetPlayerPhaseActionOrder().ToList();
        if (playerOrder.Count == 0)
            return null;

        var piles = new PlayerTeamBattleCardPiles();
        foreach (PlayerCharacter player in playerOrder)
        {
            if (
                player == null
                || !GodotObject.IsInstanceValid(player)
                || player.CharacterIndex < 0
                || player.CharacterIndex >= GameInfo.PlayerCharacters.Length
            )
            {
                continue;
            }

            var info = GameInfo.PlayerCharacters[player.CharacterIndex];
            foreach (SkillID skillId in info.GainedSkills ?? new List<SkillID>())
            {
                Skill skill = Skill.GetSkill(skillId);
                if (skill == null || skill.SkillType == Skill.SkillTypes.none || skill.IsStatusCard)
                    continue;

                piles.DrawPile.Add(
                    new BattleCardPileEntry(player, skillId, CreateBattleCardInstanceId())
                );
            }
        }

        ShufflePlayerTeamBattleDeck(
            piles,
            moveDiscardIntoDrawPile: false,
            playAnimation: false,
            triggerSearch: false,
            refreshUi: false,
            recordAutomation: false
        );
        _playerTeamBattleCardPiles = piles;
        return _playerTeamBattleCardPiles;
    }

    private ulong CreateBattleCardInstanceId()
    {
        _nextBattleCardInstanceId++;
        if (_nextBattleCardInstanceId == 0)
            _nextBattleCardInstanceId++;
        return _nextBattleCardInstanceId;
    }

    private ulong GetOrCreateBattleCardInstanceId(Skill skill)
    {
        if (skill == null)
            return 0;

        if (skill.BattleCardInstanceId == 0)
            skill.BattleCardInstanceId = CreateBattleCardInstanceId();
        return skill.BattleCardInstanceId;
    }

    private PlayerBattleCardPiles GetOrCreatePlayerBattleCardPiles(PlayerCharacter player)
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        string characterKey = GetPlayerBattleSkillCharacterKey(player);
        if (string.IsNullOrEmpty(characterKey))
            return null;

        if (_playerBattleCardPiles.TryGetValue(characterKey, out var piles))
            return piles;

        piles = new PlayerBattleCardPiles();
        var info = GameInfo.PlayerCharacters[player.CharacterIndex];
        foreach (SkillID skillId in info.GainedSkills ?? new List<SkillID>())
        {
            Skill skill = Skill.GetSkill(skillId);
            if (skill == null || skill.SkillType == Skill.SkillTypes.none || skill.IsStatusCard)
                continue;

            piles.DrawPile.Add(skillId);
        }

        ShuffleBattleDeck(
            piles.DrawPile,
            piles.DiscardPile,
            GetOrCreatePlayerBattleSkillRandom(player),
            moveDiscardIntoDrawPile: false,
            playAnimation: false,
            out _,
            out _
        );
        _playerBattleCardPiles[characterKey] = piles;
        return piles;
    }

    private bool TryEnsureDrawableCards(
        PlayerCharacter player,
        PlayerBattleCardPiles piles,
        Skill.SkillTypes skillType
    )
    {
        if (player == null || piles == null)
            return false;

        int[] drawPileIndexes = GetDrawableCardIndexes(piles.DrawPile, skillType);
        if (drawPileIndexes.Length > 0)
            return true;

        if (piles.DrawPile.Count > 0)
            return false;

        if (piles.DiscardPile.Count == 0)
            return false;

        ShuffleBattleDeck(
            piles.DrawPile,
            piles.DiscardPile,
            GetOrCreatePlayerBattleSkillRandom(player),
            moveDiscardIntoDrawPile: true,
            playAnimation: true,
            out _,
            out _
        );
        return GetDrawableCardIndexes(piles.DrawPile, skillType).Length > 0;
    }

    private bool TryEnsureDrawableTeamCards(
        PlayerTeamBattleCardPiles piles,
        Skill.SkillTypes skillType,
        PlayerCharacter owner = null
    )
    {
        if (piles == null)
            return false;

        if (PickDrawableTeamCardIndex(piles.DrawPile, skillType, owner) >= 0)
            return true;

        if (piles.DrawPile.Count > 0 || piles.DiscardPile.Count == 0)
            return false;

        ShufflePlayerTeamBattleDeck(piles, moveDiscardIntoDrawPile: true);
        return PickDrawableTeamCardIndex(piles.DrawPile, skillType, owner) >= 0;
    }

    private static void ShufflePile<T>(IList<T> pile, Random rng)
    {
        if (pile == null || pile.Count <= 1)
            return;

        rng ??= new Random();
        for (int i = pile.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (pile[i], pile[swapIndex]) = (pile[swapIndex], pile[i]);
        }
    }

    private int ShuffleBattleDeck<T>(
        List<T> drawPile,
        List<T> discardPile,
        Random rng,
        bool moveDiscardIntoDrawPile,
        bool playAnimation,
        out int movedCardCount,
        out float animationDuration
    )
    {
        movedCardCount = 0;
        animationDuration = 0f;
        if (drawPile == null)
            return 0;

        if (moveDiscardIntoDrawPile)
        {
            movedCardCount = discardPile?.Count ?? 0;
            if (movedCardCount <= 0)
                return 0;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
        }

        int shuffledCardCount = drawPile.Count;
        if (shuffledCardCount <= 0)
            return 0;

        int animationCardCount = movedCardCount > 0 ? movedCardCount : shuffledCardCount;
        if (playAnimation)
        {
            animationDuration =
                CharacterControl?.PlayBattleDeckShuffleAnimation(animationCardCount) ?? 0f;
        }

        ShufflePile(drawPile, rng);
        return shuffledCardCount;
    }

    private static int PickDrawableTeamCardIndex(
        List<BattleCardPileEntry> drawPile,
        Skill.SkillTypes skillType,
        PlayerCharacter owner = null,
        SkillID? avoidSkillId = null
    )
    {
        if (drawPile == null || drawPile.Count == 0)
            return -1;

        int fallbackIndex = -1;
        for (int index = 0; index < drawPile.Count; index++)
        {
            BattleCardPileEntry entry = drawPile[index];
            if (owner != null && entry.Owner != owner)
                continue;

            Skill candidate = Skill.GetSkill(entry.SkillId);
            if (candidate == null)
                continue;
            if (skillType != Skill.SkillTypes.none && candidate.SkillType != skillType)
                continue;

            if (fallbackIndex < 0)
                fallbackIndex = index;
            if (!avoidSkillId.HasValue || entry.SkillId != avoidSkillId.Value)
                return index;
        }

        return fallbackIndex;
    }

    private static bool HasDrawableTeamCards(
        List<BattleCardPileEntry> pile,
        Skill.SkillTypes skillType,
        PlayerCharacter owner = null
    )
    {
        return PickDrawableTeamCardIndex(pile, skillType, owner) >= 0;
    }

    private static int[] GetDrawableCardIndexes(List<SkillID> drawPile, Skill.SkillTypes skillType)
    {
        if (drawPile == null || drawPile.Count == 0)
            return Array.Empty<int>();

        return drawPile
            .Select((id, index) => new { id, index })
            .Where(entry =>
            {
                if (skillType == Skill.SkillTypes.none)
                    return Skill.GetSkill(entry.id) != null;

                Skill candidate = Skill.GetSkill(entry.id);
                return candidate != null && candidate.SkillType == skillType;
            })
            .Select(entry => entry.index)
            .ToArray();
    }

    private static bool HasDrawableCards(List<SkillID> pile, Skill.SkillTypes skillType)
    {
        if (pile == null || pile.Count == 0)
            return false;

        return pile.Any(id =>
        {
            Skill candidate = Skill.GetSkill(id);
            if (candidate == null)
                return false;

            return skillType == Skill.SkillTypes.none || candidate.SkillType == skillType;
        });
    }

    private Random GetOrCreatePlayerBattleSkillRandom(PlayerCharacter player)
    {
        string characterKey = GetPlayerBattleSkillCharacterKey(player);
        if (_playerBattleSkillRandoms.TryGetValue(characterKey, out Random rng))
            return rng;

        rng = new Random(CreatePlayerBattleSkillSeed(characterKey));
        _playerBattleSkillRandoms[characterKey] = rng;
        return rng;
    }

    private Random GetOrCreatePlayerTeamBattleSkillRandom()
    {
        return _playerTeamBattleSkillRandom ??= new Random(
            HashSeed(GetBattleBaseSeed(), unchecked((int)0x52A4E11D))
        );
    }

    private static void EnsureBattleHandSize(PlayerCharacter player)
    {
        if (player == null)
            return;

        if (player.Skills != null && player.Skills.Length == PlayerCharacter.MaxBattleHandSize)
            return;

        Skill[] resized = new Skill[PlayerCharacter.MaxBattleHandSize];
        if (player.Skills != null)
            Array.Copy(player.Skills, resized, Math.Min(player.Skills.Length, resized.Length));
        player.Skills = resized;
    }

    private string GetPlayerBattleSkillCharacterKey(PlayerCharacter player)
    {
        if (
            player == null
            || GameInfo.PlayerCharacters == null
            || player.CharacterIndex < 0
            || player.CharacterIndex >= GameInfo.PlayerCharacters.Length
        )
        {
            return string.Empty;
        }

        PlayerInfoStructure info = GameInfo.PlayerCharacters[player.CharacterIndex];
        if (!string.IsNullOrWhiteSpace(info.CharacterScenePath))
            return $"{player.CharacterIndex}:{info.CharacterScenePath}";
        if (!string.IsNullOrWhiteSpace(info.CharacterName))
            return $"{player.CharacterIndex}:{info.CharacterName}";

        return $"character-index:{player.CharacterIndex}";
    }

    private int CreatePlayerBattleSkillSeed(string characterKey)
    {
        return HashSeed(GetBattleBaseSeed(), unchecked((int)0x24681357), StableStringHash(characterKey));
    }

    private int GetBattleBaseSeed()
    {
        if (BattleRandomNum.HasValue)
            return BattleRandomNum.Value;

        if (CurrentLevelNode != null)
        {
            return GameInfo.CreateRunRngSeed(
                GameInfo.GetStreamForNodeType(CurrentLevelNode.Type),
                GameInfo.GetBattleRngQueueIndex(CurrentLevelNode)
            );
        }

        return GameInfo.Seed;
    }

    private static int HashSeed(params int[] values)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (int value in values)
            {
                hash ^= value;
                hash *= 16777619;
            }

            return hash;
        }
    }

    private static int StableStringHash(string value)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in value ?? string.Empty)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return hash;
        }
    }

    public Skill DrawCarrySkill(Character character, Skill.SkillTypes skillType)
    {
        if (character == null)
            return null;

        if (character is PlayerCharacter player)
            return DrawRandomPlayerCarrySkill(player, skillType);

        Skill[] candidates = (character.Skills ?? Array.Empty<Skill>())
            .Where(skill =>
                skill != null
                && skill.SkillType != Skill.SkillTypes.none
                && !skill.IsStatusCard
                && (skillType == Skill.SkillTypes.none || skill.SkillType == skillType)
            )
            .ToArray();
        if (candidates.Length == 0)
            return null;

        _carrySkillRandom ??= CreateBattleRandom(unchecked((int)0x5A17C0DE));
        Skill pickedSkill =
            candidates.Length == 1
                ? candidates[0]
                : candidates[_carrySkillRandom.Next(candidates.Length)];
        if (pickedSkill != null)
            pickedSkill.OwnerCharater = character;
        return pickedSkill;
    }

    private Skill DrawRandomPlayerCarrySkill(PlayerCharacter player, Skill.SkillTypes skillType)
    {
        if (player == null || GameInfo.PlayerCharacters == null)
            return null;

        if (player.CharacterIndex < 0 || player.CharacterIndex >= GameInfo.PlayerCharacters.Length)
            return null;

        PlayerTeamBattleCardPiles piles = GetOrCreatePlayerTeamBattleCardPiles();
        if (piles == null)
            return null;

        int pickedIndex = PickCarryTeamCardIndex(piles.DrawPile, player, skillType);
        if (pickedIndex < 0)
            return null;

        BattleCardPileEntry picked = piles.DrawPile[pickedIndex];
        piles.DrawPile.RemoveAt(pickedIndex);

        Skill pickedSkill = Skill.GetSkill(picked.SkillId);
        if (pickedSkill != null)
        {
            pickedSkill.OwnerCharater = picked.Owner ?? player;
            pickedSkill.BattleCardInstanceId = picked.InstanceId;
        }
        InvalidatePlayerTeamSkillTooltips();
        return pickedSkill;
    }

    private static int PickCarryTeamCardIndex(
        List<BattleCardPileEntry> pile,
        PlayerCharacter owner,
        Skill.SkillTypes skillType
    )
    {
        if (pile == null || owner == null || pile.Count == 0)
            return -1;

        for (int index = 0; index < pile.Count; index++)
        {
            BattleCardPileEntry entry = pile[index];
            if (entry.Owner != owner)
                continue;

            Skill candidate = Skill.GetSkill(entry.SkillId);
            if (
                candidate == null
                || candidate.SkillType == Skill.SkillTypes.none
                || candidate.IsStatusCard
                || (skillType != Skill.SkillTypes.none && candidate.SkillType != skillType)
            )
                continue;

            return index;
        }

        return -1;
    }

    public void SetCurrentActionCharacter(Character character)
    {
        if (character?.IsSummon == true)
            return;

        ClearNextActionPreviewCharacter(character);
        CurrentActionCharacter = character;
        RefreshTurnOrderPreview();
    }

    public void ClearCurrentActionCharacter(Character character = null)
    {
        if (character == null || CurrentActionCharacter == character)
        {
            CurrentActionCharacter = null;
            RefreshTurnOrderPreview();
        }
    }

    public void SetNextActionPreviewCharacter(Character character)
    {
        ClearNextActionPreviewCharacter();
    }

    public void ClearNextActionPreviewCharacter(Character character = null)
    {
        bool cleared = false;
        if (_nextActionPreviewCharacter != null && (character == null || _nextActionPreviewCharacter == character))
        {
            _nextActionPreviewCharacter = null;
            cleared = true;
        }

        Character oldNextGroundPreview = _nextTurnOrderGroundPreviewCharacter;
        Character oldSecondGroundPreview = _secondTurnOrderGroundPreviewCharacter;
        ClearTurnOrderGroundPreviewCharacter(character);
        cleared |= oldNextGroundPreview != _nextTurnOrderGroundPreviewCharacter;
        cleared |= oldSecondGroundPreview != _secondTurnOrderGroundPreviewCharacter;

        if (cleared)
            RefreshTurnOrderPreview();
    }

    public IEnumerable<Character> GetTeamCharacters(bool isPlayer, bool includeSummons = true)
    {
        if (isPlayer)
        {
            if (PlayersList != null)
            {
                for (int i = 0; i < PlayersList.Count; i++)
                    yield return PlayersList[i];
            }

            if (!includeSummons || PlayerSummons == null)
                yield break;

            for (int i = 0; i < PlayerSummons.Count; i++)
                yield return PlayerSummons[i];
            yield break;
        }

        if (EnemiesList != null)
        {
            for (int i = 0; i < EnemiesList.Count; i++)
                yield return EnemiesList[i];
        }

        if (!includeSummons || EnemySummons == null)
            yield break;

        for (int i = 0; i < EnemySummons.Count; i++)
            yield return EnemySummons[i];
    }

    public Character[] GetOrderedTeamCharacters(
        bool isPlayer,
        bool includeSummons = true,
        bool dyingFilter = false
    )
    {
        var characters = new List<Character>(
            includeSummons
                ? (isPlayer ? PlayersList.Count + PlayerSummons.Count : EnemiesList.Count + EnemySummons.Count)
                : (isPlayer ? PlayersList.Count : EnemiesList.Count)
        );

        foreach (Character character in GetTeamCharacters(isPlayer, includeSummons))
        {
            if (
                character == null
                || (dyingFilter && character.State == Character.CharacterState.Dying)
            )
            {
                continue;
            }

            characters.Add(character);
        }

        characters.Sort((a, b) => a.PositionIndex.CompareTo(b.PositionIndex));
        return characters.ToArray();
    }

    public void RefreshTurnOrderPreview()
    {
        RefreshEnemyIntentionPreviewSurfaces();
        HideAllTurnOrderPreviews();
    }

    public void RefreshTurnOrderPreviewFromSettings() => RefreshTurnOrderPreview();

    public void RefreshEnemyIntentionPreviews()
    {
        RefreshEnemyIntentionPreviewSurfaces();
    }

    public void RetargetEnemySingleTargetDamageIntentionsForInvisible(Character target)
    {
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || !target.IsPlayer
            || EnemiesList == null
        )
        {
            return;
        }

        foreach (var source in GetEnemyIntentionPreviewSources())
        {
            Character sourceCharacter = source?.SourceCharacter;
            if (
                sourceCharacter == null
                || !GodotObject.IsInstanceValid(sourceCharacter)
                || sourceCharacter.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            Skill skill = source.CurrentIntentionSkill;
            if (skill == null)
                continue;

            skill.OwnerCharater = sourceCharacter;
            bool targetsInvisible = skill
                .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
                .Any(entry =>
                    entry.Target == target
                    && entry.Target != null
                    && GodotObject.IsInstanceValid(entry.Target)
                    && entry.Damage > 0
                );
            if (!targetsInvisible)
                continue;

            skill.LockPreviewTargetsForExecution();
        }

        RefreshEnemyIntentionPreviews();
    }

    public void RetargetEnemyDamageIntentionsAfterInvisibleEnds(Character target)
    {
        if (
            target == null
            || !GodotObject.IsInstanceValid(target)
            || !target.IsPlayer
            || EnemiesList == null
        )
        {
            return;
        }

        foreach (var source in GetEnemyIntentionPreviewSources())
        {
            Character sourceCharacter = source?.SourceCharacter;
            if (
                sourceCharacter == null
                || !GodotObject.IsInstanceValid(sourceCharacter)
                || sourceCharacter.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            Skill skill = source.CurrentIntentionSkill;
            if (skill == null)
                continue;

            skill.OwnerCharater = sourceCharacter;
            bool hasHostileDamage = skill
                .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
                .Any(entry => entry.Damage > 0);
            if (!hasHostileDamage)
                continue;

            // The old intention may have been locked while this character was hidden.
            // Re-lock after visibility returns so area attacks include the new valid target.
            skill.LockPreviewTargetsForExecution();
        }

        RefreshEnemyIntentionPreviews();
    }

    public void RetargetEnemyIntentionsForTaunt()
    {
        if (
            GetTeamCharacters(isPlayer: true, includeSummons: true)
                .All(character => !Skill.HasTauntBuff(character))
        )
        {
            return;
        }

        foreach (var source in GetEnemyIntentionPreviewSources())
        {
            Character sourceCharacter = source?.SourceCharacter;
            if (
                sourceCharacter == null
                || !GodotObject.IsInstanceValid(sourceCharacter)
                || sourceCharacter.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            Skill skill = source.CurrentIntentionSkill;
            if (skill == null)
                continue;

            skill.OwnerCharater = sourceCharacter;
            bool targetsNonTaunt = skill
                .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
                .Any(entry =>
                    entry.Target != null
                    && GodotObject.IsInstanceValid(entry.Target)
                    && entry.Target.IsPlayer
                    && !Skill.HasTauntBuff(entry.Target)
                    && entry.Damage > 0
                );
            if (!targetsNonTaunt)
                continue;

            skill.LockPreviewTargetsForExecution();
        }

        RefreshEnemyIntentionPreviews();
    }

    private void BeginDeferEnemyIntentionRefresh()
    {
        _enemyIntentionRefreshDeferDepth++;
    }

    private void EndDeferEnemyIntentionRefresh()
    {
        _enemyIntentionRefreshDeferDepth = Math.Max(0, _enemyIntentionRefreshDeferDepth - 1);
    }

    private void RefreshDeferredEnemyIntentionPreviews()
    {
        _pendingEnemyIntentionPreviewRefresh = false;
        RefreshEnemyIntentionDamageSummaries();
        RefreshIncomingDamagePreview();
    }

    private void RefreshEnemyIntentionPreviewSurfaces()
    {
        if (IsDeferringEnemyIntentionRefresh)
        {
            _pendingEnemyIntentionPreviewRefresh = true;
            return;
        }

        RefreshEnemyIntentionDamageSummaries();
        RefreshIncomingDamagePreview();
    }

    public void RefreshTextSizeFromSettings()
    {
        CharacterControl?.RefreshTextSizeFromSettings();

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: true))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.RefreshSkillTooltipTextFromSettings();
        }
    }

    public void RefreshBattleCardDescriptionModeFromSettings()
    {
        CharacterControl?.RefreshDisplayedSkillDescriptions();

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: true))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.RefreshSkillTooltipTextFromSettings();
        }
    }

    public void RefreshStatXKeywordTooltipsFromSettings()
    {
        CharacterControl?.InvalidateHandCardHoverPreviewCaches();

        foreach (
            var character in GetTeamCharacters(isPlayer: true, includeSummons: true)
                .Concat(GetTeamCharacters(isPlayer: false, includeSummons: true))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.RefreshSkillTooltipTextFromSettings();
        }
    }

    public void RefreshEnemySkillVisibilityFromSettings()
    {
        foreach (var enemy in GetTeamCharacters(isPlayer: false, includeSummons: true))
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy))
                enemy.RefreshSkillTooltipTextFromSettings();
        }
    }

    private void ClearTurnOrderGroundPreviewCharacter(Character character = null)
    {
        if (_nextTurnOrderGroundPreviewCharacter == null && _secondTurnOrderGroundPreviewCharacter == null)
            return;

        Character nextCharacter = _nextTurnOrderGroundPreviewCharacter;
        Character secondCharacter = _secondTurnOrderGroundPreviewCharacter;

        bool clearNext = character == null || nextCharacter == character;
        bool clearSecond = character == null || secondCharacter == character;
        if (!clearNext && !clearSecond)
            return;

        if (clearNext)
            _nextTurnOrderGroundPreviewCharacter = null;
        if (clearSecond)
            _secondTurnOrderGroundPreviewCharacter = null;

        if (clearNext && GodotObject.IsInstanceValid(nextCharacter))
            nextCharacter.HideNextActionPreview();
        if (
            clearSecond
            && secondCharacter != nextCharacter
            && GodotObject.IsInstanceValid(secondCharacter)
        )
        {
            secondCharacter.HideNextActionPreview();
        }
    }

    private void HideAllTurnOrderPreviews()
    {
        ClearTurnOrderGroundPreviewCharacter();
        foreach (
            var character in GetTeamCharacters(isPlayer: true)
                .Concat(GetTeamCharacters(isPlayer: false))
        )
        {
            if (character != null && GodotObject.IsInstanceValid(character))
                character.HideTurnOrderPreview();
        }
    }

    private void RefreshEnemyIntentionDamageSummaries()
    {
        foreach (var source in GetEnemyIntentionPreviewSources())
        {
            Character character = source?.SourceCharacter;
            if (character != null && GodotObject.IsInstanceValid(character))
            {
                source.RefreshIntentionDisplayForCurrentState();
            }
        }
    }

    private IEnumerable<IIntentionPreviewSource> GetEnemyIntentionPreviewSources(
        bool includeSummons = true
    )
    {
        foreach (var enemy in EnemiesList ?? Enumerable.Empty<EnemyCharacter>())
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy))
                yield return enemy;
        }

        if (!includeSummons)
            yield break;

        foreach (var summon in EnemySummons ?? Enumerable.Empty<SummonCharacter>())
        {
            if (summon != null && GodotObject.IsInstanceValid(summon))
                yield return summon;
        }
    }

    private void CaptureBattleStartInitiative()
    {
        _battleStartPlayerActsFirst = true;
    }

    private bool BattleStartPlayerActsFirst() =>
        _battleStartPlayerActsFirst
        ?? true;

    private void RegisterAction(Character character)
    {
        if (character == null)
            return;

        if (character.IsPlayer)
            _playerActionCount++;
        else
            _enemyActionCount++;

        ulong id = character.GetInstanceId();
        _characterActionCounts[id] = _characterActionCounts.TryGetValue(id, out int currentCount)
            ? currentCount + 1
            : 1;
    }

    private void RegisterPlayerTurnStart()
    {
        _elapsedTurnCount++;
    }

    public void RequestExtraAction(Character character, int count = 1)
    {
        if (
            count <= 0
            || character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return;
        }

        ulong id = character.GetInstanceId();
        _pendingExtraActions[id] = _pendingExtraActions.TryGetValue(id, out int currentCount)
            ? currentCount + count
            : count;
        RefreshTurnOrderPreview();
    }

    public bool IsResolvingExtraAction(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return false;

        return _activeExtraActionCharacters.Contains(character.GetInstanceId());
    }

    private void SetExtraActionState(Character character, bool active)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return;

        ulong id = character.GetInstanceId();
        if (active)
            _activeExtraActionCharacters.Add(id);
        else
            _activeExtraActionCharacters.Remove(id);
    }

    private bool TryConsumeExtraAction(Character character)
    {
        if (character == null || !GodotObject.IsInstanceValid(character))
            return false;

        ulong id = character.GetInstanceId();
        if (!_pendingExtraActions.TryGetValue(id, out int pendingCount) || pendingCount <= 0)
            return false;

        if (!IsCharacterAlive(character))
        {
            _pendingExtraActions.Remove(id);
            return false;
        }

        pendingCount--;
        if (pendingCount <= 0)
            _pendingExtraActions.Remove(id);
        else
            _pendingExtraActions[id] = pendingCount;

        RefreshTurnOrderPreview();
        return true;
    }

    private bool HasPendingExtraAction(Character character)
    {
        if (
            character == null
            || !GodotObject.IsInstanceValid(character)
            || !character.ParticipatesInTurnRotation
            || !IsCharacterAlive(character)
        )
        {
            return false;
        }

        return _pendingExtraActions.TryGetValue(character.GetInstanceId(), out int pendingCount)
            && pendingCount > 0;
    }

    public int GetAlliedActionCountExcludingSelf(Character character)
    {
        if (character == null)
            return 0;

        int teamTotal = character.IsPlayer ? _playerActionCount : _enemyActionCount;
        int selfActions = _characterActionCounts.TryGetValue(
            character.GetInstanceId(),
            out int actionCount
        )
            ? actionCount
            : 0;
        return Math.Max(0, teamTotal - selfActions);
    }

    public int GetElapsedTurnCount(Character character)
    {
        if (character == null)
            return 0;

        return ElapsedTurnCount;
    }

    private async Task<bool> InitializeEnemyIntentions(CancellationToken token)
    {
        var sources = GetEnemyIntentionPreviewSources().ToList();
        foreach (var source in sources)
        {
            if (!CanContinue(token))
                return false;

            if (source is EnemyCharacter enemy)
                enemy.IntentionIndex = enemy.RollIntentionIndex();
        }

        var displayTasks = new List<Task>(sources.Count);
        foreach (var source in sources)
        {
            if (!CanContinue(token))
                return false;

            displayTasks.Add(PrepareInitialIntentionDisplayAsync(source, token));
        }

        await Task.WhenAll(displayTasks);
        if (!CanContinue(token))
            return false;

        return await DelayOrCancel(BattleStartIntentionDelayMs, token);
    }

    private async Task PrepareInitialIntentionDisplayAsync(
        IIntentionPreviewSource source,
        CancellationToken token
    )
    {
        if (ShouldAnimateIntentionDisappear(source))
            await source.DisappearIntention();
        else if (source is EnemyCharacter enemy)
            enemy.ResetIntentionDisplayImmediate();

        if (!CanContinue(token))
            return;

        source.IntentionControl.Visible = true;
        source.DisplayIntention();
        RecordAutomationSnapshot("enemy_intention_displayed");
    }

    private static bool ShouldAnimateIntentionDisappear(IIntentionPreviewSource source)
    {
        Control control = source.IntentionControl;
        if (control == null || !GodotObject.IsInstanceValid(control))
            return false;

        return control.Visible && control.Modulate.A > 0.05f;
    }

    private async Task ApplyRelicBattleEffects(CancellationToken token)
    {
        var relics = MapNode?.PlayerResourceState?.RelicList;
        if (relics == null)
        {
            return;
        }

        var relicSnapshot = relics.Where(relic => relic != null).ToArray();
        for (int i = 0; i < relicSnapshot.Length; i++)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await relicSnapshot[i].BattleEffect(this);
            if (i < relicSnapshot.Length - 1)
            {
                await DelayOrCancel(BattleStartEffectIntervalMs, token);
            }
        }
    }

    public void SetCharaterPostion()
    {
        SetCharacterPositionGroup(PlayersList, Left, -1);
        SetCharacterPositionGroup(EnemiesList, Right, 1);
    }

    private void SetCharacterPositionGroup<T>(List<T> characters, Node container, int side)
        where T : Character
    {
        foreach (var character in characters)
        {
            ReparentCharacter(character, container);
            ApplyFormationPosition(character, side);
        }
    }

    private static void ReparentCharacter(Character character, Node container)
    {
        if (character.GetParent() == container)
        {
            character.PrepareHoverTooltipInstances();
            return;
        }

        character.GetParent()?.RemoveChild(character);
        container.AddChild(character);
        character.PrepareHoverTooltipInstances();
    }

    private static Vector2 GetFormationPosition(int positionIndex, int side)
    {
        int slot = Math.Max(positionIndex - 1, 0);
        return new Vector2(slot * FormationGapX * side, 0f);
    }

    public static int ResolveEnemyFormationPositionIndex(
        LevelNode.LevelType? levelType,
        int enemyCount,
        int positionIndex
    )
    {
        if (positionIndex > 0)
            return Math.Clamp(positionIndex, 1, MaxEnemyFormationSlots);

        if (
            enemyCount == 1
            && (levelType == LevelNode.LevelType.Elite || levelType == LevelNode.LevelType.Boss)
        )
        {
            return EnemyCenterFormationSlot;
        }

        return Math.Clamp(positionIndex, 1, MaxEnemyFormationSlots);
    }

    private static void ApplyFormationPosition(Character character, int side)
    {
        if (character == null)
            return;

        character.Position = GetFormationPosition(character.PositionIndex, side);
        character.OriginalPosition = character.Position;
        character.ZIndex = Math.Max(character.PositionIndex, 0);
        BattleHudChrome.StyleCharacter(character);
    }

    public T AddSummon<T>(
        T summon,
        Character summoner,
        SummonPositionMode positionMode = SummonPositionMode.Random
    )
        where T : SummonCharacter
    {
        if (summon == null || summoner == null || !GodotObject.IsInstanceValid(summoner))
            return null;

        int slot = GetAvailableFormationSlot(
            summoner.IsPlayer,
            positionMode,
            summoner.PositionIndex
        );
        if (slot < 0)
        {
            summon.Free();
            return null;
        }

        summon.BindToSummoner(summoner);
        summon.PositionIndex = slot;
        InitializeCharacter(summon);
        ApplyBossSummonStatMultiplierIfNeeded(summon, summoner);

        var teamSummons = summon.IsPlayer ? PlayerSummons : EnemySummons;
        if (!teamSummons.Contains(summon))
            teamSummons.Add(summon);
        if (!summoner.Summons.Contains(summon))
            summoner.Summons.Add(summon);

        var container = summon.IsPlayer ? Left : Right;
        ReparentCharacter(summon, container);
        RefreshSummonPositions(summoner);
        RecordSummon(summon, summoner);
        if (!summon.IsPlayer)
        {
            summon.DisplayIntention();
            RefreshEnemyIntentionPreviews();
        }
        return summon;
    }

    private void ApplyBossSummonStatMultiplierIfNeeded(SummonCharacter summon, Character summoner)
    {
        if (
            summon == null
            || summoner == null
            || summoner.IsPlayer
            || CurrentLevelNode?.Type != LevelNode.LevelType.Boss
            || GameInfo.CurrentLevel <= 0
        )
        {
            return;
        }

        summon.ApplyCombatStatMultiplier(LevelNode.RegionTwoBossStatMultiplier, refillLife: true);
    }

    public void RemoveSummon(SummonCharacter summon, bool queueFree = true)
    {
        if (summon == null)
            return;

        var summoner = summon.Summoner;
        PlayerSummons.Remove(summon);
        EnemySummons.Remove(summon);
        summoner?.Summons.Remove(summon);
        summon.DetachFromSummoner();

        if (summoner != null)
            RefreshSummonPositions(summoner);

        if (!summon.IsPlayer)
            RefreshEnemyIntentionPreviews();

        if (queueFree && GodotObject.IsInstanceValid(summon))
            summon.QueueFree();
    }

    public void RefreshSummonPositions(Character summoner)
    {
        if (summoner == null)
            return;

        var summons = summoner
            .Summons.Where(x => x != null && GodotObject.IsInstanceValid(x))
            .ToArray();
        for (int i = 0; i < summons.Length; i++)
        {
            var summon = summons[i];
            ApplyFormationPosition(summon, summon.IsPlayer ? -1 : 1);
        }
    }

    public int GetAvailableFormationSlot(
        bool isPlayer,
        SummonPositionMode positionMode = SummonPositionMode.Random,
        int anchorPositionIndex = 0
    )
    {
        var occupied = GetTeamCharacters(isPlayer, includeSummons: true)
            .Where(x => x != null && GodotObject.IsInstanceValid(x))
            .Select(x => x.PositionIndex)
            .Where(index => index > 0)
            .ToHashSet();

        int maxFormationSlots = GetMaxFormationSlots(isPlayer);
        int[] emptySlots = Enumerable
            .Range(1, maxFormationSlots)
            .Where(slot => !occupied.Contains(slot))
            .ToArray();
        if (emptySlots.Length == 0)
            return -1;

        Random random = BattleIntentionRandom ?? _carrySkillRandom ?? new Random();
        if (positionMode == SummonPositionMode.Random)
            return emptySlots[random.Next(emptySlots.Length)];

        if (positionMode == SummonPositionMode.RandomHasEnemy)
            return PickRandomSlotFacingHostile(isPlayer, emptySlots, random);

        if (anchorPositionIndex <= 0)
            return -1;

        int slotSelector = positionMode switch
        {
            SummonPositionMode.Next => 1,
            SummonPositionMode.Previous => -1,
            _ => 0,
        };

        return SelectRelativeEmptySlot(slotSelector, anchorPositionIndex, occupied, maxFormationSlots);
    }

    private static int GetMaxFormationSlots(bool isPlayer) =>
        isPlayer ? MaxFormationSlots : MaxEnemyFormationSlots;

    private int PickRandomSlotFacingHostile(bool isPlayer, int[] emptySlots, Random random)
    {
        var hostileSlots = GetTeamCharacters(!isPlayer, includeSummons: false)
            .Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
                && x.PositionIndex > 0
            )
            .Select(x => x.PositionIndex)
            .ToHashSet();

        int[] prioritizedSlots = emptySlots
            .Where(hostileSlots.Contains)
            .ToArray();

        int[] pickPool = prioritizedSlots.Length > 0 ? prioritizedSlots : emptySlots;
        return pickPool[random.Next(pickPool.Length)];
    }

    private static int SelectRelativeEmptySlot(
        int slotSelector,
        int anchorPositionIndex,
        HashSet<int> occupied,
        int maxFormationSlots
    )
    {
        if (anchorPositionIndex <= 0 || slotSelector == 0)
            return -1;

        int step = Math.Sign(slotSelector);
        int startSlot = anchorPositionIndex + slotSelector;
        for (int slot = startSlot; slot >= 1 && slot <= maxFormationSlots; slot += step)
        {
            if (!occupied.Contains(slot))
                return slot;
        }

        return -1;
    }

    private void ClearSummons(bool queueFree)
    {
        var summons = PlayerSummons.Concat(EnemySummons).ToArray();
        for (int i = 0; i < summons.Length; i++)
        {
            RemoveSummon(summons[i], queueFree);
        }
        PlayerSummons.Clear();
        EnemySummons.Clear();
    }

    public List<Func<Character, Task>> EmitList = new();
    public List<Func<Character, Character, Task>> DyingEmitList = new();
    public List<Func<bool, Task>> TeamTurnEndEmitList = new();

    public async Task EmitCharacterDying(Character target, Character source)
    {
        if (target == null || DyingEmitList == null || DyingEmitList.Count == 0)
            return;

        for (int i = 0; i < DyingEmitList.Count; i++)
        {
            await DyingEmitList[i](target, source);
        }
    }

    public async Task EndEmitS(Character character)
    {
        // Summons advance their local action chain directly via Next and should not
        // participate in the regular end-of-action emit pipeline.
        if (character?.IsSummon == true)
        {
            ClearCurrentActionCharacter(character);
            EmitSignal(SignalName.Next, character);
            return;
        }

        for (int i = 0; i < EmitList.Count; i++)
        {
            await EmitList[i](character);
        }

        RegisterAction(character);

        await TriggerSummonsAfterOwner(character);
        EmitSignal(SignalName.Next, character);
        ClearCurrentActionCharacter(character);
    }

    public async Task EmitTeamTurnEnd(bool isPlayer)
    {
        if (TeamTurnEndEmitList == null || TeamTurnEndEmitList.Count == 0)
            return;

        for (int i = 0; i < TeamTurnEndEmitList.Count; i++)
        {
            await TeamTurnEndEmitList[i](isPlayer);
        }
    }

    public async Task TriggerGlobalTurnEndBuffs(Character actingCharacter)
    {
        if (actingCharacter == null || HasBattleEnded() || !IsBattleAlive())
            return;

        Character[] targets = GetTeamCharacters(true)
            .Concat(GetTeamCharacters(false))
            .Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
            )
            .ToArray();

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (target == null || target.IsPlayer != actingCharacter.IsPlayer)
                continue;

            EndActionBuff disaster = target.EndActionBuffs?.FirstOrDefault(x =>
                x != null && x.ThisBuffName == Buff.BuffName.Disaster && x.Stack > 0
            );
            if (disaster == null)
                continue;

            disaster.FlashTrigger();
            using var _ = target.BeginEffectSource(Buff.BuffName.Disaster.GetDescription());
            await target.GetHurt(disaster.Stack, target);
            disaster.ConsumeOneStack();

            if (HasBattleEnded() || !IsBattleAlive())
                return;
        }

    }

    public bool ShouldDeferEnemyPhasePowerGain(Character target)
    {
        return _isResolvingEnemyTeamActionPhase
            && target != null
            && GodotObject.IsInstanceValid(target)
            && !target.IsPlayer
            && target.State != Character.CharacterState.Dying;
    }

    public bool QueueEnemyPhaseEndPowerGain(
        Character target,
        int value,
        Character source,
        string effectName = null
    )
    {
        if (value <= 0 || !ShouldDeferEnemyPhasePowerGain(target))
            return false;

        _enemyPhaseEndEffects.Add(async () =>
        {
            if (
                target == null
                || !GodotObject.IsInstanceValid(target)
                || target.State == Character.CharacterState.Dying
                || HasBattleEnded()
                || !IsBattleAlive()
            )
            {
                return;
            }

            using var _ = string.IsNullOrWhiteSpace(effectName)
                ? null
                : target.BeginEffectSource(effectName);
            await target.IncreaseProperties(PropertyType.Power, value, source ?? target);
        });
        return true;
    }

    private async Task ResolveEnemyPhaseEndEffects(CancellationToken token)
    {
        if (_enemyPhaseEndEffects.Count == 0)
            return;

        var effects = _enemyPhaseEndEffects.ToArray();
        _enemyPhaseEndEffects.Clear();
        for (int i = 0; i < effects.Length; i++)
        {
            if (!CanContinue(token))
                return;

            await effects[i]();
        }
    }

    public Task EndEmitExtraActionS(Character character) => EndEmitS(character);

    public List<Func<Task>> StartEffectList = new();

    public async Task BattleBegin1(CancellationToken token)
    {
        AudioManager.PlayBattleStart(this);

        for (int i = 0; i < StartEffectList.Count; i++)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await StartEffectList[i]();
            if (i < StartEffectList.Count - 1)
            {
                await DelayOrCancel(BattleStartEffectIntervalMs, token);
            }
        }

        if (!CanContinue(token))
        {
            return;
        }

        GameInfo.ApplyDifficultyBattleStartPenalty(this);

        if (!CanContinue(token))
        {
            return;
        }

        await ApplyRelicBattleEffects(token);

        if (!CanContinue(token))
        {
            return;
        }

        if (!BattleStartPlayerActsFirst())
        {
            await EnemyTeamActionPhase(token);
        }

        for (int i = 0; i < MaxBattleTurns && CanContinue(token); i++)
        {
            await PlayerActionPhase(token);
            if (!CanContinue(token))
            {
                return;
            }

            await EnemyTeamActionPhase(token);
        }

        if (CanContinue(token))
        {
            GD.Print("Battle completed after 100 turns");
            Retreat();
        }
    }

    private async Task PlayerActionPhase(CancellationToken token)
    {
        if (!CanAct(PlayersList, token))
            return;

        var playerPhaseOrder = GetPlayerPhaseActionOrder().ToList();
        _activePlayerPhaseOrder = playerPhaseOrder;
        _activePlayerPhaseIndex = -1;
        if (playerPhaseOrder.Count == 0)
            return;

        AudioManager.PlayPlayerTurn(this);
        RegisterPlayerTurnStart();
        ResolveTeamBlockExpiration(isPlayer: true);
        ConsumeTeamTurnStartHurtDebuffStacks(isPlayer: true);

        bool completedPhase = false;
        _isResolvingPlayerTeamActionPhase = true;
        try
        {
            ResolvePlayerTeamTurnStartPhase(playerPhaseOrder);
            if (!CanContinue(token) || !CanAct(playerPhaseOrder, token))
                return;

            _activePlayerPhaseIndex = 0;
            PlayerCharacter teamActingPlayer = playerPhaseOrder.FirstOrDefault(IsCharacterAlive);
            if (teamActingPlayer == null)
                return;

            SetNextActionPreviewCharacter(GetNextEnemyPhaseCharacter());
            await ResolveCharacterActionLoop(
                teamActingPlayer,
                token,
                () => GetNextEnemyPhaseCharacter()
            );

            if (!await DelayOrCancel(PlayerPostActionDelayMs, token) || await HandleBattleOver(token))
                return;

            await ResolveTeamTurnEndPhase(isPlayer: true);
            if (!CanContinue(token) || !IsBattleAlive() || HasBattleEnded())
                return;

            await TriggerGlobalTurnEndBuffs(teamActingPlayer);
            if (!CanContinue(token) || !IsBattleAlive() || HasBattleEnded())
                return;

            completedPhase = CanContinue(token);
        }
        finally
        {
            _isResolvingPlayerTeamActionPhase = false;
            _activePlayerPhaseOrder = new List<PlayerCharacter>();
            _activePlayerPhaseIndex = -1;
        }

        if (completedPhase && CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
            await EmitTeamTurnEnd(isPlayer: true);
        if (completedPhase && CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
            ConsumeTeamTurnEndDebuffStacks(isPlayer: true);
        if (completedPhase && CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
            ConsumeOpposingTeamTurnEndProtectionStacks(activeTeamIsPlayer: true);
    }

    private async Task EnemyTeamActionPhase(CancellationToken token)
    {
        if (!CanAct(EnemiesList, token))
            return;

        var enemyPhaseOrder = GetEnemyPhaseActionOrder().ToList();
        _activeEnemyPhaseOrder = enemyPhaseOrder;
        _activeEnemyPhaseIndex = -1;
        if (enemyPhaseOrder.Count == 0)
            return;

        AudioManager.PlayEnemyTurn(this);
        ResolveTeamBlockExpiration(isPlayer: false);
        ConsumeTeamTurnStartHurtDebuffStacks(isPlayer: false);

        bool completedPhase = false;
        BeginDeferEnemyIntentionRefresh();
        try
        {
            ResolveEnemyTeamTurnStartPhase(enemyPhaseOrder);
            if (!CanContinue(token))
                return;

            _isResolvingEnemyTeamActionPhase = true;
            for (int i = 0; i < enemyPhaseOrder.Count && CanContinue(token); i++)
            {
                _activeEnemyPhaseIndex = i;
                Character actingCharacter = enemyPhaseOrder[i];
                if (!IsCharacterAlive(actingCharacter))
                    continue;

                int nextIndex = i + 1;
                SetNextActionPreviewCharacter(GetNextEnemyPhaseCharacter(enemyPhaseOrder, nextIndex));
                await ResolveCharacterActionLoop(
                    actingCharacter,
                    token,
                    () => GetNextEnemyPhaseCharacter(enemyPhaseOrder, nextIndex)
                );

                if (!await DelayOrCancel(EnemyPostActionDelayMs, token) || await HandleBattleOver(token))
                    return;
            }

            completedPhase = CanContinue(token);
        }
        finally
        {
            _isResolvingEnemyTeamActionPhase = false;
            _activeEnemyPhaseOrder = new List<EnemyCharacter>();
            _activeEnemyPhaseIndex = -1;
            if (!completedPhase)
            {
                _enemyPhaseEndEffects.Clear();
                _pendingEnemyIntentionPreviewRefresh = false;
                EndDeferEnemyIntentionRefresh();
            }
        }

        if (completedPhase)
        {
            bool refreshIntentions = false;
            try
            {
                await ResolveEnemyPhaseEndEffects(token);
                if (CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
                    await ResolveTeamTurnEndPhase(isPlayer: false);
                if (CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
                    await TriggerGlobalTurnEndBuffs(enemyPhaseOrder.FirstOrDefault(IsCharacterAlive));
                if (CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
                    await EmitTeamTurnEnd(isPlayer: false);
                if (CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
                    ConsumeTeamTurnEndDebuffStacks(isPlayer: false);
                if (CanContinue(token) && IsBattleAlive() && !HasBattleEnded())
                    ConsumeOpposingTeamTurnEndProtectionStacks(activeTeamIsPlayer: false);

                refreshIntentions = CanContinue(token) && IsBattleAlive() && !HasBattleEnded();
                if (refreshIntentions)
                    RefreshEnemyFormationIntentions();
                else
                    _pendingEnemyIntentionPreviewRefresh = false;
            }
            finally
            {
                EndDeferEnemyIntentionRefresh();
            }

            if (refreshIntentions)
                RefreshDeferredEnemyIntentionPreviews();
        }

        SetNextActionPreviewCharacter(GetNextLivingCharacter(PlayersList));
    }

    private void RefreshEnemyFormationIntentions()
    {
        foreach (var source in GetEnemyIntentionPreviewSources())
        {
            Character character = source?.SourceCharacter;
            if (
                character == null
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            source.RefreshNextIntentionAfterAction();
        }
        RecordAutomationSnapshot("enemy_intentions_refreshed");
    }

    private void ConsumeTeamTurnStartHurtDebuffStacks(bool isPlayer)
    {
        foreach (Character character in GetTeamCharacters(isPlayer, includeSummons: true))
        {
            if (
                character == null
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            foreach (
                HurtBuff hurtBuff in character
                    .HurtBuffs?.Where(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Vulnerable
                        && buff.Stack > 0
                    )
                    .ToArray() ?? Array.Empty<HurtBuff>()
            )
            {
                hurtBuff.ConsumeTeamTurnStartStack();
            }
        }
    }

    private void ConsumeTeamTurnEndDebuffStacks(bool isPlayer)
    {
        foreach (Character character in GetTeamCharacters(isPlayer, includeSummons: true))
        {
            if (
                character == null
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            foreach (
                AttackBuff weaken in character
                    .AttackBuffs?.Where(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Weaken
                        && buff.Stack > 0
                    )
                    .ToArray() ?? Array.Empty<AttackBuff>()
            )
            {
                weaken.ConsumeTeamTurnEndStack();
            }
        }
    }

    private void ConsumeOpposingTeamTurnEndProtectionStacks(bool activeTeamIsPlayer)
    {
        bool protectedTeamIsPlayer = !activeTeamIsPlayer;
        foreach (Character character in GetTeamCharacters(protectedTeamIsPlayer, includeSummons: true))
        {
            if (
                character == null
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            foreach (
                StartActionBuff invisible in character
                    .StartActionBuffs?.Where(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Invisible
                        && buff.Stack > 0
                    )
                    .ToArray() ?? Array.Empty<StartActionBuff>()
            )
            {
                invisible.ConsumeOpposingTeamTurnEndStack();
            }

            foreach (
                HurtBuff taunt in character
                    .HurtBuffs?.Where(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Taunt
                        && buff.Stack > 0
                    )
                    .ToArray() ?? Array.Empty<HurtBuff>()
            )
            {
                taunt.ConsumeOpposingTeamTurnEndStack();
            }
        }
    }

    private void ResolveEnemyTeamTurnStartPhase(IReadOnlyList<EnemyCharacter> enemyPhaseOrder)
    {
        if (enemyPhaseOrder == null || enemyPhaseOrder.Count == 0)
            return;

        for (int i = 0; i < enemyPhaseOrder.Count; i++)
        {
            EnemyCharacter enemy = enemyPhaseOrder[i];
            if (!IsCharacterAlive(enemy))
                continue;

            enemy.ResolveTeamTurnStartPhase();
        }
    }

    private void ResolvePlayerTeamTurnStartPhase(IReadOnlyList<PlayerCharacter> playerPhaseOrder)
    {
        if (playerPhaseOrder == null || playerPhaseOrder.Count == 0)
            return;

        UpdataPlayerEnergy(GetPlayerTeamTurnStartEnergyGain(playerPhaseOrder));
        for (int i = 0; i < playerPhaseOrder.Count; i++)
        {
            PlayerCharacter player = playerPhaseOrder[i];
            if (!IsCharacterAlive(player))
                continue;

            player.ResolveTeamTurnStartPhase();
        }
    }

    private async Task ResolveTeamTurnEndPhase(bool isPlayer)
    {
        Character[] characters = GetTeamCharacters(isPlayer, includeSummons: true)
            .Where(IsCharacterAlive)
            .OrderBy(character => character.PositionIndex)
            .ToArray();
        for (int i = 0; i < characters.Length; i++)
        {
            Character character = characters[i];
            if (!IsCharacterAlive(character))
                continue;

            await character.ResolveTeamTurnEndPhaseAsync();
        }

        if (isPlayer)
            ResolvePlayerTeamEnergyLossAtTurnEnd();
    }

    private void ResolveTeamBlockExpiration(bool isPlayer)
    {
        foreach (Character character in GetTeamCharacters(isPlayer, includeSummons: false))
        {
            if (
                character == null
                || !GodotObject.IsInstanceValid(character)
                || character.State == Character.CharacterState.Dying
            )
            {
                continue;
            }

            character.ResolveBlockExpirationAtTeamTurnStart();
        }
    }

    public async Task CharacterAction<T>(
        List<T> characterlist,
        CancellationToken token,
        Func<Character> getNextAfterAction = null
    )
        where T : Character
    {
        if (!CanAct(characterlist, token))
        {
            return;
        }

        DyingDetector(characterlist);
        Character actingCharacter = characterlist[0];
        RotateFrontToBack(characterlist);
        Character nextPreview = GetNextActionPreviewAfterCurrentAction(
            actingCharacter,
            characterlist,
            getNextAfterAction
        );
        if (nextPreview == actingCharacter)
            nextPreview = null;
        SetNextActionPreviewCharacter(nextPreview);
        await ResolveCharacterActionLoop(
            actingCharacter,
            token,
            () => GetNextActionPreviewAfterCurrentAction(
                actingCharacter,
                characterlist,
                getNextAfterAction
            )
        );

        if (!await DelayOrCancel(PostActionDelayMs, token) || await HandleBattleOver(token))
        {
            return;
        }

    }

    private async Task ResolveCharacterActionLoop(
        Character actingCharacter,
        CancellationToken token,
        Func<Character> getNextAfterAction
    )
    {
        bool isExtraAction = false;
        while (true)
        {
            if (
                !CanContinue(token)
                || actingCharacter == null
                || !GodotObject.IsInstanceValid(actingCharacter)
                || actingCharacter.State == Character.CharacterState.Dying
            )
            {
                break;
            }

            SetExtraActionState(actingCharacter, isExtraAction);
            try
            {
                await StartActionAndWaitForNext(actingCharacter);
            }
            finally
            {
                SetExtraActionState(actingCharacter, false);
            }

            bool hasPendingExtraAction = HasPendingExtraAction(actingCharacter);
            Character nextPreview =
                hasPendingExtraAction && IsCharacterAlive(actingCharacter)
                    ? actingCharacter
                    : getNextAfterAction?.Invoke();
            SetNextActionPreviewCharacter(nextPreview);

            if (!CanContinue(token) || !TryConsumeExtraAction(actingCharacter))
                break;

            isExtraAction = true;
        }
    }

    private Character GetNextActionPreviewAfterCurrentAction<T>(
        Character actingCharacter,
        List<T> rotatedCurrentTeam,
        Func<Character> getNextAfterAction
    )
        where T : Character =>
        getNextAfterAction != null
            ? getNextAfterAction()
            : GetNextLivingCharacter(rotatedCurrentTeam);

    private void UpdateTurnOrderPreviewAfterResolvedAction<T>(
        Character actingCharacter,
        List<T> currentTeam,
        Func<Character> getNextAfterAction,
        bool sameCharacterActsAgain
    )
        where T : Character
    {
        if (!IsBattleAlive())
            return;

        Character nextPreview;
        if (sameCharacterActsAgain && IsCharacterAlive(actingCharacter))
        {
            nextPreview = actingCharacter;
        }
        else
        {
            nextPreview =
                getNextAfterAction != null
                    ? getNextAfterAction()
                    : GetNextLivingCharacter(currentTeam);
        }

        SetNextActionPreviewCharacter(nextPreview);
    }

    public void DyingDetector<T>(List<T> c)
        where T : Character
    {
        if (c == null || c.Count == 0 || IsTeamDefeated(c))
        {
            return;
        }

        while (c[0].State == Character.CharacterState.Dying)
        {
            RotateFrontToBack(c);
        }
    }

    private static int GetTurnRotationActionCount<T>(IEnumerable<T> characters)
        where T : Character =>
        characters
            ?.Count(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && character.ParticipatesInTurnRotation
                && IsCharacterAlive(character)
            ) ?? 0;

    private IEnumerable<EnemyCharacter> GetEnemyPhaseActionOrder()
    {
        return EnemiesList
            .Where(enemy =>
                enemy != null
                && GodotObject.IsInstanceValid(enemy)
                && enemy.ParticipatesInTurnRotation
                && IsCharacterAlive(enemy)
            )
            .OrderBy(enemy => enemy.GetCurrentIntentionActionPhase())
            .ThenBy(enemy => enemy.PositionIndex);
    }

    private IEnumerable<PlayerCharacter> GetPlayerPhaseActionOrder()
    {
        return PlayersList
            .Where(player =>
                player != null
                && GodotObject.IsInstanceValid(player)
                && player.ParticipatesInTurnRotation
                && IsCharacterAlive(player)
            )
            .OrderBy(player => player.PositionIndex);
    }

    private Character GetNextEnemyPhaseCharacter()
    {
        return GetEnemyPhaseActionOrder().FirstOrDefault();
    }

    private static Character GetNextEnemyPhaseCharacter(
        IReadOnlyList<EnemyCharacter> enemyPhaseOrder,
        int startIndex
    )
    {
        if (enemyPhaseOrder == null || enemyPhaseOrder.Count == 0)
            return null;

        for (int i = Math.Max(startIndex, 0); i < enemyPhaseOrder.Count; i++)
        {
            if (IsCharacterAlive(enemyPhaseOrder[i]))
                return enemyPhaseOrder[i];
        }

        return null;
    }

    private static Character GetNextPlayerPhaseCharacter(
        IReadOnlyList<PlayerCharacter> playerPhaseOrder,
        int startIndex
    )
    {
        if (playerPhaseOrder == null || playerPhaseOrder.Count == 0)
            return null;

        for (int i = Math.Max(startIndex, 0); i < playerPhaseOrder.Count; i++)
        {
            if (IsCharacterAlive(playerPhaseOrder[i]))
                return playerPhaseOrder[i];
        }

        return null;
    }

    public async void Retreat(bool preserveEnemyState = false)
    {
        if (_retreating)
        {
            return;
        }

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();
        if (!IsBattleInstanceValid())
        {
            return;
        }

        if (preserveEnemyState)
        {
            SyncCurrentLevelNodeBattleStatistics();
            SaveEnemyBattleStateToCurrentNode();
        }

        bool isWin = IsTeamDefeated(EnemiesList) && HasLivingMember(PlayersList);
        if (isWin)
        {
            _allowBattleEndRevive = true;
            try
            {
                Relic.ApplyBattleEndRelicEffects(this);
                await TriggerMariyaBattleEndPassivesAsync();
                await Task.Delay(320);
            }
            finally
            {
                _allowBattleEndRevive = false;
            }
        }

        SyncPlayerLifeToGameInfo(refreshResourceUi: true);

        SceneTransitionLayer transitionLayer = SceneTransitionLayer.Ensure(this);
        if (transitionLayer != null)
            await transitionLayer.FadeToBlackAsync(0.8f);
        else
            await Task.Delay(PostActionDelayMs);

        if (!IsBattleInstanceValid())
        {
            return;
        }

        if (isWin || Istest)
        {
            var reward = Reward.Show(this);
            ConfigureRewards(reward);
            if (reward != null && CurrentLevelNode != null)
            {
                SyncCurrentLevelNodeBattleStatistics();
                reward.SetCompleteNodeOnClose(CurrentLevelNode);
            }
        }

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);
        UnlockMapNodes();

        SceneTree tree = GetTree();
        QueueFreeBattleRoot();

        if (transitionLayer != null && GodotObject.IsInstanceValid(transitionLayer))
        {
            if (tree != null)
                await transitionLayer.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            await transitionLayer.FadeFromBlackAsync(0.24f);
        }
    }

    private void SaveEnemyBattleStateToCurrentNode()
    {
        if (CurrentLevelNode?.EnemiesRegeditList == null || EnemiesList == null)
            return;

        foreach (var enemy in EnemiesList)
        {
            if (enemy?.Registry == null)
                continue;

            enemy.Registry.CurrentLife =
                enemy.State == Character.CharacterState.Dying
                    ? 0
                    : Math.Clamp(enemy.Life, 0, Math.Max(1, enemy.Registry.MaxLife));
        }
    }

    public void AbortBattle(bool unlockMapNodes = true)
    {
        if (_retreating)
            return;

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();

        if (!IsBattleInstanceValid())
            return;

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);
        if (unlockMapNodes)
            UnlockMapNodes();

        QueueFreeBattleRoot();
    }

    private void QueueFreeBattleRoot()
    {
        if (!IsBattleInstanceValid())
            return;

        if (_standaloneTestBattleContext)
        {
            QueueFree();
            return;
        }

        GetParent()?.QueueFree();
    }

    private async Task HandleDefeatAsync()
    {
        if (_retreating)
            return;

        _retreating = true;
        TryCancelLifetime();
        TryEmitNextToUnblock();

        await ShowGameOverAsync();
    }

    private async Task ShowGameOverAsync()
    {
        SyncPlayerLifeToGameInfo(refreshResourceUi: true);
        SyncCurrentLevelNodeBattleStatistics();
        if (CurrentLevelNode != null)
            GameInfo.EnsureActiveLevelNodeSnapshot(CurrentLevelNode);
        GameInfo.RecordCurrentRunHistory(victory: false);
        SaveSystem.SaveRunCheckpoint(background: false);

        await Task.Delay(DefeatGameOverDelayMs);

        if (!IsBattleInstanceValid())
            return;

        GameOverSummary summary = GameOverSummary.Show(this);
        if (summary == null)
        {
            GD.PushError("Battle: failed to show game over summary.");
            SceneTransitionLayer.Ensure(this)?.SwitchScene(
                "res://BeginGame/StartInterface.tscn"
            );
            return;
        }

        if (GetTree() != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);
        QueueFreeBattleRoot();
    }

    private void SyncCurrentLevelNodeBattleStatistics()
    {
        if (CurrentLevelNode == null)
            return;

        CurrentLevelNode.PlayerDamageSummaryLines = BuildPlayerDamageSummaryLines();
        CurrentLevelNode.PlayerTotalTurnCount = ElapsedTurnCount;
        CurrentLevelNode.EnemyTotalTurnCount = 0;
        GameInfo.UpdateActiveLevelNodeBattleStatistics(CurrentLevelNode);
    }

    private void UnlockMapNodes()
    {
        var levelProgress = MapNode?.GetNodeOrNull<LevelProgress>("LevelProgress");
        levelProgress ??= CurrentLevelNode?.GetParent()?.GetParent() as LevelProgress;
        levelProgress?.UnlockAllNodes();
    }

    private bool CanContinue(CancellationToken token) =>
        !token.IsCancellationRequested && IsBattleAlive();

    private bool CanAct<T>(List<T> characters, CancellationToken token)
        where T : Character => characters is { Count: > 0 } && CanContinue(token);

    public bool ShouldAbortSkillResolution() => _retreating || !IsBattleAlive();

    public async Task<bool> ResolveBattleOverAfterSkillAsync()
    {
        if (!IsBattleAlive())
            return true;

        _battleOverCheckPendingAfterEffect = false;
        return await HandleBattleOver(_lifetimeCts.Token, delayAfterHandling: false);
    }

    private async Task<bool> HandleBattleOver(
        CancellationToken token,
        bool delayAfterHandling = true
    )
    {
        bool playersDefeated = IsTeamDefeated(PlayersList);
        bool enemiesDefeated = IsTeamDefeated(EnemiesList);
        if (!playersDefeated && !enemiesDefeated)
        {
            return false;
        }

        GD.Print("over");
        if (enemiesDefeated && HasLivingMember(PlayersList))
        {
            AudioManager.PlayVictory(this);
            Retreat();
        }
        else
        {
            await HandleDefeatAsync();
        }

        if (delayAfterHandling)
            await DelayOrCancel(BattleOverDelayMs, token);
        return true;
    }

    private static void RotateFrontToBack<T>(List<T> characters)
    {
        if (characters.Count <= 1)
            return;
        characters.Reverse(1, characters.Count - 1);
        characters.Reverse();
    }

    private static Character GetNextLivingCharacter<T>(IEnumerable<T> characters)
        where T : Character => characters?.FirstOrDefault(IsCharacterAlive);

    private static bool IsTeamDefeated<T>(IEnumerable<T> characters)
        where T : Character =>
        characters?.Any() == true && characters.All(character => !IsCharacterAlive(character));

    private static bool HasLivingMember<T>(IEnumerable<T> characters)
        where T : Character => characters?.Any(IsCharacterAlive) == true;

    private static bool IsCharacterAlive(Character character) =>
        character != null && character.State != Character.CharacterState.Dying;

    private async Task TriggerSummonsAfterOwner(Character character)
    {
        if (
            character == null
            || character.Summons == null
            || character.Summons.Count == 0
            || HasBattleEnded()
            || !IsBattleAlive()
        )
        {
            return;
        }

        var summons = character
            .Summons.Where(x =>
                x != null
                && GodotObject.IsInstanceValid(x)
                && x.State != Character.CharacterState.Dying
            )
            .ToArray();

        for (int i = 0; i < summons.Length; i++)
        {
            if (HasBattleEnded() || !IsBattleAlive())
                return;

            await StartActionAndWaitForNext(summons[i]);
        }
    }

    private async Task StartActionAndWaitForNext(Character expectedCharacter)
    {
        if (!CanStartActionAndWait(expectedCharacter))
            return;

        bool receivedNext = false;
        void OnNext(Character emittedCharacter)
        {
            if (emittedCharacter == null || emittedCharacter == expectedCharacter)
                receivedNext = true;
        }

        Next += OnNext;
        try
        {
            SetCurrentActionCharacter(expectedCharacter);
            Relic.ApplyPlayerActionStartRelicEffects(
                this,
                expectedCharacter,
                GetUpcomingPlayerActionNumber(expectedCharacter)
            );
            expectedCharacter.StartAction();

            while (CanWaitForActionNext(expectedCharacter) && !receivedNext)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        finally
        {
            Next -= OnNext;
        }

        if (!receivedNext && expectedCharacter?.State == Character.CharacterState.Dying)
            CleanupDiedCurrentActionCharacter(expectedCharacter);
    }

    private int GetUpcomingPlayerActionNumber(Character character)
    {
        if (
            character == null
            || !character.IsPlayer
            || !character.ParticipatesInTurnRotation
            || character.IsSummon
        )
        {
            return 0;
        }

        return _playerActionCount + 1;
    }

    private bool CanWaitForActionNext(Character expectedCharacter)
    {
        return CanStartActionAndWait(expectedCharacter);
    }

    private bool CanStartActionAndWait(Character expectedCharacter)
    {
        return IsBattleAlive()
            && !HasBattleEnded()
            && expectedCharacter != null
            && GodotObject.IsInstanceValid(expectedCharacter)
            && expectedCharacter.State != Character.CharacterState.Dying;
    }

    private void CleanupDiedCurrentActionCharacter(Character character)
    {
        ClearCurrentActionCharacter(character);

        if (character is not PlayerCharacter player)
            return;

        player.DiscardBattleHand();
        CharacterControl?.DisablePlayerActions(player);
        MapNode?.PlayerResourceState?.SetItemsEnabled(false);
    }

    private bool HasBattleEnded() => IsTeamDefeated(PlayersList) || IsTeamDefeated(EnemiesList);

    public bool CanReviveDyingPlayerNow() => IsBattleAlive() || _allowBattleEndRevive;

    public void QueueBattleOverCheck()
    {
        if (_retreating || !IsBattleInstanceValid())
            return;

        if (HasEffectSourceContext)
        {
            _battleOverCheckPendingAfterEffect = true;
            return;
        }

        if (_battleOverCheckQueued)
            return;

        _battleOverCheckQueued = true;
        CallDeferred(MethodName.DeferredHandleBattleOverCheck);
    }

    private async void DeferredHandleBattleOverCheck()
    {
        _battleOverCheckQueued = false;
        if (_retreating || !IsBattleInstanceValid())
            return;

        if (HasEffectSourceContext)
        {
            _battleOverCheckPendingAfterEffect = true;
            return;
        }

        SceneTree tree = GetTree();
        if (tree != null)
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        if (_retreating || !IsBattleInstanceValid())
            return;

        if (HasEffectSourceContext)
        {
            _battleOverCheckPendingAfterEffect = true;
            return;
        }

        if (!HasBattleEnded())
            return;

        await HandleBattleOver(CancellationToken.None, delayAfterHandling: false);
    }

    public void HandleCharacterEnteredDying(Character target)
    {
        if (target is not PlayerCharacter player || target.IsSummon)
            return;

        SyncPlayerLifeToGameInfo(refreshResourceUi: true);
        _ = RemovePlayerOwnedBattleCardsOnDyingAsync(player);
    }

    public void SyncPlayerLifeToGameInfo(bool refreshResourceUi = true)
    {
        if (PlayersList == null)
            return;

        foreach (PlayerCharacter player in PlayersList)
        {
            if (player == null || !GodotObject.IsInstanceValid(player) || player.IsSummon)
                continue;

            player.SyncPersistentLife();
        }

        if (refreshResourceUi)
            MapNode?.PlayerResourceState?.RefreshPartyLifeResource();
    }

    private async Task TriggerMariyaBattleEndPassivesAsync()
    {
        if (PlayersList == null)
            return;

        var tasks = PlayersList
            .OfType<Mariya>()
            .Where(x => x != null && !x.IsSummon && x.State == Character.CharacterState.Normal)
            .Select(mariya => mariya.TriggerBattleEndPassiveAsync())
            .ToArray();

        if (tasks.Length == 0)
            return;

        await Task.WhenAll(tasks);
    }

    public void TestBattle()
    {
        EnemiesList = new List<EnemyCharacter>();
        for (int i = 1; i <= 3; i++)
        {
            EnemyRegedit regedit = CreateStandaloneTestEnemy(i);
            var enemy = regedit.CharacterScene.Instantiate<EnemyCharacter>();
            enemy.Registry = regedit;
            enemy.PositionIndex = regedit.PositionIndex;
            InitializeCharacter(enemy);
            EnemiesList.Add(enemy);
        }
    }

    private void ConfigureRewards(Reward reward)
    {
        if (reward == null)
            return;

        reward.ClearRewardItems();
        var levelType = CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal;
        bool allowRareSkillRewards =
            GameInfo.GetNodeRegionBattleQueueIndex(CurrentLevelNode) >= 2;
        reward.AllowRareSkillRewards = allowRareSkillRewards;
        int skillRewardGroups = GameInfo.GetBattleSkillRewardGroupCount();
        for (int i = 0; i < skillRewardGroups; i++)
            reward.AddSkillRewardEntry(
                forceRare: allowRareSkillRewards && levelType == LevelNode.LevelType.Boss
            );

        var talentRewardPreview = GameInfo.PreviewBattleTalentPointReward(CurrentLevelNode);
        if (talentRewardPreview.Granted)
            reward.AddTalentPointRewardEntry(talentRewardPreview);

        var rng = GameInfo.CreateBattleRewardRng(CurrentLevelNode);
        bool addRelic = ShouldAddRelicReward(levelType);
        bool addItem = GameInfo.RollBattleItemDrop(CurrentLevelNode);
        var pendingRelics = new HashSet<RelicID>();

        TryAddRelicReward(reward, CurrentLevelNode, addRelic, rewardOffset: 0, pendingRelics);
        TryAddItemReward(reward, rng, addItem);
    }

    private static bool ShouldAddRelicReward(LevelNode.LevelType levelType)
    {
        return levelType == LevelNode.LevelType.Elite;
    }

    private static void TryAddRelicReward(
        Reward reward,
        LevelNode battleNode,
        bool addRelic,
        int rewardOffset,
        HashSet<RelicID> pendingRelics = null
    )
    {
        if (!addRelic)
            return;

        RelicID? relicId = GameInfo.GetBattleRelicOffer(battleNode, rewardOffset);
        if (!relicId.HasValue)
            return;
        if (pendingRelics != null && pendingRelics.Contains(relicId.Value))
            return;

        reward.AddRelicRewardEntry(relicId.Value);
        pendingRelics?.Add(relicId.Value);
    }

    private static void TryAddItemReward(Reward reward, Random rng, bool addItem)
    {
        if (!addItem)
        {
            return;
        }

        ItemID[] itemPool =
        [
            ItemID.Health,
            ItemID.Explosion,
            ItemID.ElectromagneticInterference,
            ItemID.SpaceOscillation,
            ItemID.StreamingTransmission,
            ItemID.Battery,
        ];
        reward.AddItemRewardEntry(PickRandom(itemPool, rng));
    }

    private static T PickRandom<T>(IReadOnlyList<T> pool, Random rng) => pool[rng.Next(pool.Count)];

    private bool IsBattleInstanceValid() =>
        _battleInstanceId != 0 && GodotObject.IsInstanceIdValid(_battleInstanceId);

    private bool IsBattleAlive() =>
        !_retreating && !_lifetimeCts.IsCancellationRequested && IsBattleInstanceValid();

    private void TryCancelLifetime()
    {
        try
        {
            _lifetimeCts.Cancel();
        }
        catch (ObjectDisposedException) { }
    }

    private void TryEmitNextToUnblock()
    {
        if (!IsBattleInstanceValid())
            return;
        try
        {
            EmitSignal(SignalName.Next);
        }
        catch (ObjectDisposedException) { }
    }

    private void InitDummy()
    {
        dummy.BattleNode = this;
        dummy.Visible = false;
        dummy.Position = new Vector2(10000, -10000);
        dummy.ConfigureCombatStats(
            dummy.BattlePower,
            dummy.BattleSurvivability,
            1_000_000_000
        );
        dummy.Skills = [Skill.CreatePlaceholder(Skill.SkillTypes.Attack)];
        dummy.Initialize();
    }

    private async Task<bool> DelayOrCancel(int milliseconds, CancellationToken token)
    {
        try
        {
            await Task.Delay(milliseconds, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
