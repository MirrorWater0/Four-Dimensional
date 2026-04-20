using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class Battle : Node2D
{
    public static bool Istest = false;

    private sealed class EffectSourceContext
    {
        public EffectSourceContext(int contextId, Character sourceCharacter, string actionName)
        {
            ContextId = contextId;
            SourceCharacter = sourceCharacter;
            ActionName = actionName;
        }

        public int ContextId { get; }
        public Character SourceCharacter { get; }
        public string ActionName { get; }
    }

    private sealed class DamageRecordEntry
    {
        public DamageRecordEntry(
            int effectSourceContextId,
            Character sourceCharacter,
            Character targetCharacter,
            int actualDamage,
            int blockedDamage
        )
        {
            EffectSourceContextId = effectSourceContextId;
            SourceCharacter = sourceCharacter;
            TargetCharacter = targetCharacter;
            ActualDamage = actualDamage;
            BlockedDamage = blockedDamage;
        }

        public int EffectSourceContextId { get; }
        public Character SourceCharacter { get; }
        public Character TargetCharacter { get; }
        public int ActualDamage { get; }
        public int BlockedDamage { get; }
    }

    private sealed class EffectSourceScope : IDisposable
    {
        private readonly Battle _battle;
        private bool _disposed;

        public EffectSourceScope(Battle battle)
        {
            _battle = battle;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _battle.PopEffectSource();
        }
    }

    [Export]
    public bool WarmupMode { get; set; }

    [Export]
    public bool HoverPerfLogEnabled { get; set; } = true;

    [Export]
    public double HoverPerfFrameSpikeMs { get; set; } = 28.0;

    [Export]
    public double HoverPerfWorkSpikeMs { get; set; } = 2.0;

    public Random BattleIntentionRandom;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private ulong _battleInstanceId;
    private bool _retreating;
    private ulong _lastHoverPerfTickMsec;
    private ulong _lastHoverFrameSpikeLogTickMsec;
    private string _lastHoverPerfLabel = string.Empty;
    private string _lastHoverPerfCharacter = string.Empty;

    [Signal]
    public delegate void NextEventHandler(Character character);

    PackedScene _test1 = (PackedScene)
        ResourceLoader.Load("res://character/EnemyCharacter/Evil.tscn");
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
        field ??= GetNode<CharacterControl>("CharacterControl");

    public ObservableList<Skill> UsedSkills = new ObservableList<Skill>();
    public Button RetreatButton => field ??= GetNode<Button>("Retreat");
    public bool SuppressSpeedGainThisTurn { get; set; }

    private int _playerSpeed = 0;
    private int _enemySpeed = 0;
    private int _playerActionCount = 0;
    private int _enemyActionCount = 0;
    private readonly Dictionary<ulong, int> _characterActionCounts = new();
    private readonly Dictionary<ulong, int> _pendingExtraActions = new();
    private readonly HashSet<ulong> _activeExtraActionCharacters = new();

    public int PlayerSpeed
    {
        get => _playerSpeed;
        set =>
            SetSpeedValue(
                ref _playerSpeed,
                value,
                PlayerSpeedLabel,
                PlayerSpeedBar,
                GetTeamCharacters(true)
            );
    }

    public int EnemySpeed
    {
        get => _enemySpeed;
        set =>
            SetSpeedValue(
                ref _enemySpeed,
                value,
                EnemySpeedLabel,
                EnemySpeedBar,
                GetTeamCharacters(false)
            );
    }

    public GlowLabel PlayerSpeedLabel =>
        field ??= GetNodeOrNull<GlowLabel>("SpeedBox/PlayerSpeed/Label");
    public GlowLabel EnemySpeedLabel =>
        field ??= GetNodeOrNull<GlowLabel>("SpeedBox/EnemySpeed/Label");
    public ProgressBar PlayerSpeedBar =>
        field ??= GetNodeOrNull<ProgressBar>("SpeedBox/PlayerSpeed");
    public ProgressBar EnemySpeedBar => field ??= GetNodeOrNull<ProgressBar>("SpeedBox/EnemySpeed");
    public LevelNode CurrentLevelNode;
    public Character dummy => field ??= GetNode<Character>("Dummy");
    public RichTextLabel BattleRecord => field ??= GetNodeOrNull<RichTextLabel>("UI/BattleRecord");
    public Button RecordButton => field ??= GetNodeOrNull<Button>("UI/RecordButton");

    private const float RecordSlideDuration = 0.2f;
    private const float RecordHideMargin = 18f;
    private const string RecordIndexColor = "#b0b6c2";
    private const string RecordSourceColor = "#ffd36b";
    private const string RecordTargetColor = "#8fd3ff";
    private const string RecordSkillColor = "#b56bff";
    private const string RecordDamageColor = "#ff7b7b";
    private const string RecordHealColor = "#6bff8f";
    private const string RecordNeutralColor = "#d9e2f2";
    private bool _recordInitialized;
    private bool _recordVisible;
    private float _recordVisibleLeft;
    private float _recordVisibleRight;
    private float _recordHiddenLeft;
    private float _recordHiddenRight;
    private Tween _recordTween;
    private int _recordIndex;
    private int _nextEffectSourceContextId;
    private readonly List<EffectSourceContext> _effectSourceStack = new();
    private readonly List<DamageRecordEntry> _damageRecords = new();
    private const float FormationGapY = 140f;
    private const float FormationGapX = 280f;
    private const float FormationSkew = 10f;
    private const float FormationRowOffset = 100f;
    private const int MaxFormationSlots = 9;
    private const int MaxBattleTurns = 100;
    private const int PostActionDelayMs = 800;
    private const int BattleOverDelayMs = 5000;
    private const int PlayerSpeedHintDelayMs = 200;
    private const int EnemySpeedHintDelayMs = 400;
    private const int SpeedTriggerThreshold = 100;
    private const int EarlyBattleBonusSkillRewardBattles = 3;
    private const int EarlyBattleExtraSkillRewardGroups = 1;
    private const string SpeedTriggerText = "[color=yellow]超速触发[/color]";

    public Character CurrentActionCharacter { get; private set; }
    public bool HasEffectSourceContext => _effectSourceStack.Count > 0;

    public override void _EnterTree()
    {
        _battleInstanceId = GetInstanceId();
    }

    public override void _ExitTree()
    {
        _retreating = true;
        TryCancelLifetime();
    }

    public override async void _Ready()
    {
        if (WarmupMode)
        {
            DisableBattleProcessing();
            return;
        }

        var token = _lifetimeCts.Token;
        InitDummy();
        InitializeBattleUi();
        InitializeBattleCharacters();
        SetCharaterPostion();
        CharacterControl.Connect();
        if (!await DelayOrCancel(PlayerSpeedHintDelayMs, token))
        {
            return;
        }

        CharacterControl.DisableAll();
        if (!await InitializeEnemyIntentions(token))
        {
            return;
        }

        PlayerSpeed = 0;
        EnemySpeed = 0;
        await ApplyRelicBattleEffects(token);
        await BattleBegin1(token);
    }

    public override void _Process(double delta)
    {
        if (!HoverPerfLogEnabled || WarmupMode)
            return;

        double frameMs = delta * 1000.0;
        if (frameMs < HoverPerfFrameSpikeMs)
            return;

        ulong now = Time.GetTicksMsec();
        ulong elapsedFromHover = _lastHoverPerfTickMsec == 0
            ? ulong.MaxValue
            : now - _lastHoverPerfTickMsec;
        if (elapsedFromHover > 500)
            return;

        if (now - _lastHoverFrameSpikeLogTickMsec < 120)
            return;

        _lastHoverFrameSpikeLogTickMsec = now;
        LogHoverPerf(
            $"frame spike {frameMs:F1}ms, {elapsedFromHover}ms after {_lastHoverPerfLabel} on {_lastHoverPerfCharacter}"
        );
    }

    public void MarkHoverPerfEvent(Character character, string label)
    {
        if (!HoverPerfLogEnabled)
            return;

        _lastHoverPerfTickMsec = Time.GetTicksMsec();
        _lastHoverPerfLabel = label ?? "hover";
        _lastHoverPerfCharacter = GetCharacterLogName(character);
    }

    public void LogHoverPerfWork(Character character, string label, ulong startUsec)
    {
        if (!HoverPerfLogEnabled)
            return;

        double elapsedMs = (Time.GetTicksUsec() - startUsec) / 1000.0;
        MarkHoverPerfEvent(character, label);
        if (elapsedMs < HoverPerfWorkSpikeMs)
            return;

        LogHoverPerf($"{label} work {elapsedMs:F2}ms on {GetCharacterLogName(character)}");
    }

    public void LogHoverPerfDuration(Character character, string label, double elapsedMs)
    {
        if (!HoverPerfLogEnabled)
            return;

        MarkHoverPerfEvent(character, label);
        if (elapsedMs < HoverPerfWorkSpikeMs)
            return;

        LogHoverPerf($"{label} work {elapsedMs:F2}ms on {GetCharacterLogName(character)}");
    }

    private void LogHoverPerf(string text)
    {
        string line = $"[HoverPerf] {text}";
        GD.Print(line);
    }

    private static string GetCharacterLogName(Character character)
    {
        if (character == null)
            return "<null>";

        return string.IsNullOrWhiteSpace(character.CharacterName)
            ? character.Name
            : character.CharacterName;
    }

    private void DisableBattleProcessing()
    {
        SetProcess(false);
        SetProcessInput(false);
        SetPhysicsProcess(false);
    }

    private void InitializeBattleCharacters()
    {
        ClearSummons(queueFree: true);
        EnemiesList.Clear();
        foreach (var regedit in CurrentLevelNode.EnemiesRegeditList)
        {
            var enemy = regedit.CharacterScene.Instantiate<EnemyCharacter>();
            enemy.Registry = regedit;
            enemy.PositionIndex = regedit.PositionIndex;
            InitializeCharacter(enemy);
            EnemiesList.Add(enemy);
        }

        PlayersList.Clear();
        for (int i = 0; i < GameInfo.PlayerCharacters.Length; i++)
        {
            var character = GD.Load<PackedScene>(GameInfo.PlayerCharacters[i].CharacterScenePath)
                .Instantiate<PlayerCharacter>();
            character.CharacterIndex = i;
            InitializeCharacter(character);
            PlayersList.Add(character);
        }

        PlayersList = PlayersList.OrderBy(x => x.PositionIndex).ToList();
        EnemiesList = EnemiesList.OrderBy(x => x.PositionIndex).ToList();
    }

    private void InitializeCharacter(Character character)
    {
        character.BattleNode = this;
        character.Initialize();
    }

    private void InitializeBattleUi()
    {
        RetreatButton.ButtonDown += ManualRetreat;
        UsedSkills.Clear();
        UsedSkills.ItemAdded -= OnSkillUsed;
        UsedSkills.ItemAdded += OnSkillUsed;
        _playerActionCount = 0;
        _enemyActionCount = 0;
        _characterActionCounts.Clear();
        _pendingExtraActions.Clear();
        _activeExtraActionCharacters.Clear();
        _recordIndex = 0;
        _nextEffectSourceContextId = 0;
        _damageRecords.Clear();
        if (BattleRecord != null)
        {
            BattleRecord.Text = string.Empty;
        }
        InitRecordButton();
    }

    public IDisposable PushEffectSource(Character sourceCharacter, string actionName = null)
    {
        _effectSourceStack.Add(
            new EffectSourceContext(++_nextEffectSourceContextId, sourceCharacter, actionName)
        );
        return new EffectSourceScope(this);
    }

    public void SetCurrentActionCharacter(Character character)
    {
        CurrentActionCharacter = character;
    }

    public void ClearCurrentActionCharacter(Character character = null)
    {
        if (character == null || CurrentActionCharacter == character)
            CurrentActionCharacter = null;
    }

    private void PopEffectSource()
    {
        if (_effectSourceStack.Count == 0)
            return;

        _effectSourceStack.RemoveAt(_effectSourceStack.Count - 1);
    }

    private EffectSourceContext GetCurrentEffectSourceContext() =>
        _effectSourceStack.Count > 0 ? _effectSourceStack[^1] : null;

    private static string GetRecordCharacterName(Character character)
    {
        if (character == null)
            return "系统";

        if (!string.IsNullOrWhiteSpace(character.CharacterName))
            return character.CharacterName;

        if (!string.IsNullOrWhiteSpace(character.Name))
            return character.Name;

        return character.GetType().Name;
    }

    private static string FormatRecordActor(
        Character character,
        string color,
        string fallback = "系统"
    )
    {
        string name = character == null ? fallback : GetRecordCharacterName(character);
        return $"[color={color}]{name}[/color]";
    }

    private string FormatRecordSource(Character explicitSource = null)
    {
        EffectSourceContext context = GetCurrentEffectSourceContext();
        Character sourceCharacter =
            explicitSource ?? context?.SourceCharacter ?? CurrentActionCharacter;
        string actor = FormatRecordActor(sourceCharacter, RecordSourceColor);
        bool sameSourceAsContext =
            explicitSource == null
            || context?.SourceCharacter == null
            || context.SourceCharacter == explicitSource;
        if (!string.IsNullOrWhiteSpace(context?.ActionName) && sameSourceAsContext)
            return $"{actor}[color={RecordIndexColor}][{context.ActionName}][/color]";

        return actor;
    }

    private void AppendRecordLine(string line, bool indent = false)
    {
        var record = BattleRecord;
        if (record == null || string.IsNullOrWhiteSpace(line))
            return;

        record.AppendText(indent ? $"    {line}\n" : $"{line}\n");
    }

    public void RecordDamage(
        Character target,
        int actualDamage,
        int blockedDamage = 0,
        Character source = null
    )
    {
        if (target == null)
            return;

        EffectSourceContext context = GetCurrentEffectSourceContext();
        Character resolvedSource = source ?? context?.SourceCharacter ?? CurrentActionCharacter;
        _damageRecords.Add(
            new DamageRecordEntry(
                context?.ContextId ?? 0,
                resolvedSource,
                target,
                actualDamage,
                blockedDamage
            )
        );

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        string line =
            actualDamage > 0
                ? $"{sourceText} -> {targetText}  造成  [color={RecordDamageColor}]{actualDamage}[/color] 点伤害"
                : $"{sourceText} -> {targetText}  未造成伤害";

        if (blockedDamage > 0)
            line += $"（格挡吸收 [color={RecordNeutralColor}]{blockedDamage}[/color]）";

        AppendRecordLine(line, indent: true);
    }

    public int GetLastRecordedDamageFromCurrentEffectSource(
        Character source = null,
        Character target = null
    )
    {
        EffectSourceContext context = GetCurrentEffectSourceContext();
        if (context == null)
            return 0;

        Character resolvedSource = source ?? context.SourceCharacter ?? CurrentActionCharacter;
        for (int i = _damageRecords.Count - 1; i >= 0; i--)
        {
            DamageRecordEntry entry = _damageRecords[i];
            if (entry.EffectSourceContextId != context.ContextId)
                continue;
            if (resolvedSource != null && entry.SourceCharacter != resolvedSource)
                continue;
            if (target != null && entry.TargetCharacter != target)
                continue;

            return entry.ActualDamage;
        }

        return 0;
    }

    public void RecordHeal(Character target, int actualHeal, Character source = null)
    {
        if (target == null || actualHeal <= 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        AppendRecordLine(
            $"{sourceText} -> {targetText}  回复  [color={RecordHealColor}]{actualHeal}[/color] 点生命",
            indent: true
        );
    }

    public void RecordBlockGain(Character target, int blockGain, Character source = null)
    {
        if (target == null || blockGain <= 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        AppendRecordLine(
            $"{sourceText} -> {targetText}  获得  [color={RecordNeutralColor}]{blockGain}[/color] 点格挡",
            indent: true
        );
    }

    public void RecordEnergyChange(Character target, int delta, Character source = null)
    {
        if (target == null || delta == 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        string action = delta > 0 ? "获得" : "失去";
        AppendRecordLine(
            $"{sourceText} -> {targetText}  {action}  [color={RecordNeutralColor}]{Math.Abs(delta)}[/color] 点能量",
            indent: true
        );
    }

    public void RecordPropertyChange(
        Character target,
        PropertyType type,
        int delta,
        Character source = null
    )
    {
        if (target == null || delta == 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        string action = delta > 0 ? "获得" : "失去";
        int amount = Math.Abs(delta);
        AppendRecordLine(
            $"{sourceText} -> {targetText}  {action}  [color={RecordNeutralColor}]{amount}[/color] 点{Skill.GetColoredPropertyLabel(type)}",
            indent: true
        );
    }

    public void RecordBuffGain(
        Character target,
        Buff.BuffName buffName,
        int stacks,
        Character source = null
    )
    {
        if (target == null || stacks == 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        AppendRecordLine(
            $"{sourceText} -> {targetText}  获得  [color={RecordNeutralColor}]{stacks}[/color] 层{buffName.GetDescription()}",
            indent: true
        );
    }

    public void RecordSummon(Character summon, Character source = null)
    {
        if (summon == null)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(summon, RecordTargetColor, "召唤物");
        AppendRecordLine($"{sourceText} -> {targetText}  召唤登场", indent: true);
    }

    public void RecordDying(Character target, Character source = null)
    {
        if (target == null)
            return;

        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        Character resolvedSource =
            source ?? GetCurrentEffectSourceContext()?.SourceCharacter ?? CurrentActionCharacter;
        if (resolvedSource == null)
        {
            AppendRecordLine($"{targetText}  进入濒死", indent: true);
            return;
        }

        string sourceText = FormatRecordSource(source);
        AppendRecordLine($"{sourceText} -> {targetText}  使其进入濒死", indent: true);
    }

    public IEnumerable<Character> GetTeamCharacters(bool isPlayer, bool includeSummons = true)
    {
        IEnumerable<Character> core = isPlayer
            ? PlayersList.Cast<Character>()
            : EnemiesList.Cast<Character>();
        if (!includeSummons)
            return core;

        IEnumerable<Character> summons = isPlayer
            ? PlayerSummons.Cast<Character>()
            : EnemySummons.Cast<Character>();
        return core.Concat(summons);
    }

    public Character[] GetOrderedTeamCharacters(
        bool isPlayer,
        bool includeSummons = true,
        bool dyingFilter = false
    )
    {
        IEnumerable<Character> query = GetTeamCharacters(isPlayer, includeSummons)
            .Where(x => x != null);
        if (dyingFilter)
            query = query.Where(x => x.State != Character.CharacterState.Dying);
        return query.OrderBy(x => x.PositionIndex).ToArray();
    }

    public int GetAliveTeamSpeed(bool isPlayer) =>
        GetTeamCharacters(isPlayer)
            .Where(IsCharacterAlive)
            .Where(x => x.CountsTowardTeamSpeed)
            .Sum(x => x.Speed);

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

        return true;
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

    private async Task<bool> InitializeEnemyIntentions(CancellationToken token)
    {
        foreach (var enemy in EnemiesList)
        {
            if (!CanContinue(token))
            {
                return false;
            }

            enemy.IntentionIndex = enemy.RollIntentionIndex();
            await enemy.DisappearIntention();
            if (!CanContinue(token))
            {
                return false;
            }

            enemy.IntentionContorl.Visible = true;
            enemy.DisplayIntention();
        }

        return await DelayOrCancel(PostActionDelayMs, token);
    }

    private async Task ApplyRelicBattleEffects(CancellationToken token)
    {
        var relics = MapNode?.PlayerResourceState?.RelicList;
        if (relics == null)
        {
            return;
        }

        foreach (var relic in relics)
        {
            if (!CanContinue(token))
            {
                return;
            }

            await relic.BattleEffect(this);
        }
    }

    private void SetSpeedValue(
        ref int currentValue,
        int nextValue,
        GlowLabel label,
        ProgressBar bar,
        IEnumerable<Character> characters
    )
    {
        currentValue = Math.Max(nextValue, 0);
        if (!IsBattleAlive())
        {
            return;
        }

        if (GodotObject.IsInstanceValid(label))
        {
            label.Text = $"{currentValue}({SumAliveSpeed(characters)})";
        }

        if (GodotObject.IsInstanceValid(bar))
        {
            CreateTween().TweenProperty(bar, "value", currentValue, 0.3f);
        }
    }

    private static int SumAliveSpeed(IEnumerable<Character> characters) =>
        characters?.Where(IsCharacterAlive).Where(x => x.CountsTowardTeamSpeed).Sum(x => x.Speed)
        ?? 0;

    private void OnSkillUsed(Skill skill)
    {
        var record = BattleRecord;
        if (skill == null || record == null)
        {
            return;
        }

        string characterName = string.IsNullOrWhiteSpace(skill.OwnerCharater?.CharacterName)
            ? skill.OwnerCharater?.Name ?? "Unknown"
            : skill.OwnerCharater.CharacterName;
        string skillName = string.IsNullOrWhiteSpace(skill.SkillName)
            ? skill.GetType().Name
            : skill.SkillName;
        record.AppendText(
            $"[color={RecordIndexColor}]{++_recordIndex:00}[/color]  [color={RecordSourceColor}]{characterName}[/color]  释放  [color={RecordSkillColor}]{skillName}[/color]\n"
        );
    }

    private void InitRecordButton()
    {
        if (_recordInitialized)
        {
            return;
        }

        var record = BattleRecord;
        var button = RecordButton;
        if (record == null || button == null)
        {
            return;
        }

        _recordVisibleLeft = record.OffsetLeft;
        _recordVisibleRight = record.OffsetRight;

        float width = _recordVisibleRight - _recordVisibleLeft;
        float shift = width + RecordHideMargin;
        _recordHiddenLeft = _recordVisibleLeft + shift;
        _recordHiddenRight = _recordVisibleRight + shift;

        button.Pressed += ToggleRecord;
        _recordVisible = false;
        SetRecordOffsets(_recordHiddenLeft, _recordHiddenRight);
        _recordInitialized = true;
    }

    private void ToggleRecord()
    {
        if (!_recordInitialized)
            InitRecordButton();
        ShowBattleRecord(!_recordVisible);
    }

    private void ShowBattleRecord(bool show)
    {
        var record = BattleRecord;
        if (record == null)
        {
            return;
        }

        _recordVisible = show;
        _recordTween?.Kill();
        _recordTween = CreateTween();
        _recordTween.SetParallel();
        _recordTween
            .TweenProperty(
                record,
                "offset_left",
                show ? _recordVisibleLeft : _recordHiddenLeft,
                RecordSlideDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
        _recordTween
            .TweenProperty(
                record,
                "offset_right",
                show ? _recordVisibleRight : _recordHiddenRight,
                RecordSlideDuration
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private void SetRecordOffsets(float left, float right)
    {
        var record = BattleRecord;
        if (record == null)
            return;
        record.OffsetLeft = left;
        record.OffsetRight = right;
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
        int row = (positionIndex - 1) % 3;
        int col = (positionIndex - 1) / 3;
        float xPos =
            col * FormationGapX * side - (row * FormationSkew - FormationRowOffset * (row - 1));
        return new Vector2(xPos, row * FormationGapY);
    }

    private static void ApplyFormationPosition(Character character, int side)
    {
        if (character == null)
            return;

        int row = (character.PositionIndex - 1) % 3;
        character.Position = GetFormationPosition(character.PositionIndex, side);
        character.OriginalPosition = character.Position;
        character.ZIndex = row;
    }

    public T AddSummon<T>(T summon, Character summoner, int slotSelector = 0)
        where T : SummonCharacter
    {
        if (summon == null || summoner == null || !GodotObject.IsInstanceValid(summoner))
            return null;

        int slot = GetAvailableFormationSlot(
            summoner.IsPlayer,
            slotSelector,
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

        var teamSummons = summon.IsPlayer ? PlayerSummons : EnemySummons;
        if (!teamSummons.Contains(summon))
            teamSummons.Add(summon);
        if (!summoner.Summons.Contains(summon))
            summoner.Summons.Add(summon);

        var container = summon.IsPlayer ? Left : Right;
        ReparentCharacter(summon, container);
        RefreshSummonPositions(summoner);
        RecordSummon(summon, summoner);
        return summon;
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
        int slotSelector = 0,
        int anchorPositionIndex = 0
    )
    {
        var occupied = GetTeamCharacters(isPlayer, includeSummons: true)
            .Where(x => x != null && GodotObject.IsInstanceValid(x))
            .Select(x => x.PositionIndex)
            .Where(index => index > 0)
            .ToHashSet();

        int[] emptySlots = Enumerable
            .Range(1, MaxFormationSlots)
            .Where(slot => !occupied.Contains(slot))
            .ToArray();
        if (emptySlots.Length == 0)
            return -1;

        if (slotSelector == 0)
            return emptySlots[0];

        if (slotSelector == MaxFormationSlots)
            return emptySlots[^1];

        if (anchorPositionIndex <= 0)
            return -1;

        return SelectRelativeEmptySlot(slotSelector, anchorPositionIndex, occupied);
    }

    private static int SelectRelativeEmptySlot(
        int slotSelector,
        int anchorPositionIndex,
        HashSet<int> occupied
    )
    {
        if (anchorPositionIndex <= 0 || slotSelector == 0)
            return -1;

        int step = Math.Sign(slotSelector);
        int startSlot = anchorPositionIndex + slotSelector;
        for (int slot = startSlot; slot >= 1 && slot <= MaxFormationSlots; slot += step)
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

        if (
            SuppressSpeedGainThisTurn != true
            && character?.ParticipatesInTurnRotation == true
        )
        {
            if (character.IsPlayer)
                PlayerSpeed += GetAliveTeamSpeed(isPlayer: true);
            else
                EnemySpeed += GetAliveTeamSpeed(isPlayer: false);
        }

        await TriggerSummonsAfterOwner(character);
        EmitSignal(SignalName.Next, character);
        ClearCurrentActionCharacter(character);
    }

    public Task EndEmitExtraActionS(Character character)
    {
        if (character?.IsSummon == true)
        {
            ClearCurrentActionCharacter(character);
            EmitSignal(SignalName.Next, character);
            return Task.CompletedTask;
        }

        EmitSignal(SignalName.Next, character);
        ClearCurrentActionCharacter(character);
        return Task.CompletedTask;
    }

    public List<Func<Task>> StartEffectList = new();

    public async Task BattleBegin1(CancellationToken token)
    {
        for (int i = 0; i < StartEffectList.Count; i++)
        {
            await StartEffectList[i]();
        }

        if (!CanContinue(token))
        {
            return;
        }

        if (GetAliveTeamSpeed(true) < GetAliveTeamSpeed(false))
        {
            await CharacterAction(EnemiesList, token);
        }

        for (int i = 0; i < MaxBattleTurns && CanContinue(token); i++)
        {
            await CharacterAction(PlayersList, token);
            if (!CanContinue(token))
            {
                return;
            }

            await CharacterAction(EnemiesList, token);
        }

        if (CanContinue(token))
        {
            GD.Print("Battle completed after 100 turns");
            Retreat();
        }
    }

    public async Task CharacterAction<T>(List<T> characterlist, CancellationToken token)
        where T : Character
    {
        if (!CanAct(characterlist, token))
        {
            return;
        }

        DyingDetector(characterlist);
        Character actingCharacter = characterlist[0];
        RotateFrontToBack(characterlist);
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
                SetCurrentActionCharacter(actingCharacter);
                actingCharacter.StartAction();
                await WaitForNextFrom(actingCharacter);
            }
            finally
            {
                SetExtraActionState(actingCharacter, false);
            }

            if (!CanContinue(token) || !TryConsumeExtraAction(actingCharacter))
                break;

            isExtraAction = true;
        }

        if (!await DelayOrCancel(PostActionDelayMs, token) || await HandleBattleOver(token))
        {
            return;
        }

        await TryTriggerSpeedBurst(
            PlayersList,
            () => PlayerSpeed,
            value => PlayerSpeed = value,
            PlayerSpeedHintDelayMs,
            token
        );
        if (CanContinue(token))
        {
            await TryTriggerSpeedBurst(
                EnemiesList,
                () => EnemySpeed,
                value => EnemySpeed = value,
                EnemySpeedHintDelayMs,
                token
            );
        }
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

    private void ManualRetreat()
    {
        if (CanManualRetreat())
        {
            Retreat(consumeTransitionEnergy: true);
        }
    }

    public bool CanManualRetreat() =>
        !_retreating
        && (MapNode?.PlayerResourceState?.TransitionEnergy ?? GameInfo.TransitionEnergy) > 0;

    public async void Retreat(bool consumeTransitionEnergy = false)
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

        if (RetreatButton != null)
        {
            RetreatButton.Disabled = true;
        }

        if (consumeTransitionEnergy)
        {
            ConsumeRetreatTransitionEnergy();
        }

        MapNode?.BlackMaskAnimation(0.8f);
        await Task.Delay(PostActionDelayMs);

        if (!IsBattleInstanceValid())
        {
            return;
        }

        bool isWin = IsTeamDefeated(EnemiesList) && HasLivingMember(PlayersList);
        if (isWin || Istest)
        {
            var reward = Reward.Show(this);
            ConfigureRewards(reward);
            if (CurrentLevelNode != null)
            {
                reward.SetCompleteNodeOnClose(CurrentLevelNode);
            }
        }

        PlayersList?.Clear();
        EnemiesList?.Clear();
        ClearSummons(queueFree: true);

        if (IsBattleInstanceValid())
        {
            GetParent()?.QueueFree();
        }
    }

    private bool CanContinue(CancellationToken token) =>
        !token.IsCancellationRequested && IsBattleAlive();

    private bool CanAct<T>(List<T> characters, CancellationToken token)
        where T : Character => characters is { Count: > 0 } && CanContinue(token);

    private async Task<bool> HandleBattleOver(CancellationToken token)
    {
        if (!IsTeamDefeated(PlayersList) && !IsTeamDefeated(EnemiesList))
        {
            return false;
        }

        GD.Print("over");
        Retreat();
        await DelayOrCancel(BattleOverDelayMs, token);
        return true;
    }

    private async Task TryTriggerSpeedBurst<T>(
        List<T> team,
        Func<int> getSpeed,
        Action<int> setSpeed,
        int delayMs,
        CancellationToken token
    )
        where T : Character
    {
        if (team == null || team.Count == 0 || getSpeed() < SpeedTriggerThreshold)
        {
            return;
        }

        setSpeed(getSpeed() - SpeedTriggerThreshold);
        if (!await DelayOrCancel(delayMs, token) || !CanContinue(token))
        {
            return;
        }

        BuffHintLabel.Spawn(team[0], SpeedTriggerText, Vector2.Zero);
        SuppressSpeedGainThisTurn = true;
        try
        {
            await CharacterAction(team, token);
        }
        finally
        {
            SuppressSpeedGainThisTurn = false;
        }
    }

    private static void RotateFrontToBack<T>(List<T> characters)
    {
        if (characters.Count <= 1)
            return;
        characters.Reverse(1, characters.Count - 1);
        characters.Reverse();
    }

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

            SetCurrentActionCharacter(summons[i]);
            summons[i].StartAction();
            await WaitForNextFrom(summons[i]);
        }
    }

    private async Task WaitForNextFrom(Character expectedCharacter)
    {
        while (true)
        {
            Variant[] signalArgs = await ToSignal(this, SignalName.Next);
            if (signalArgs == null || signalArgs.Length == 0)
                return;

            Character emittedCharacter = signalArgs[0].As<Character>();
            if (emittedCharacter == null || emittedCharacter == expectedCharacter)
                return;
        }
    }

    private bool HasBattleEnded() => IsTeamDefeated(PlayersList) || IsTeamDefeated(EnemiesList);

    private void ConsumeRetreatTransitionEnergy()
    {
        var resourceState = MapNode?.PlayerResourceState;
        if (resourceState != null)
        {
            resourceState.TransitionEnergy = Math.Max(0, resourceState.TransitionEnergy - 1);
            return;
        }

        GameInfo.TransitionEnergy = Math.Clamp(
            GameInfo.TransitionEnergy - 1,
            0,
            GameInfo.TransitionEnergyMax
        );
    }

    public void TestBattle()
    {
        EnemiesList =
        [
            _test1.Instantiate<EnemyCharacter>(),
            _test1.Instantiate<EnemyCharacter>(),
            _test1.Instantiate<EnemyCharacter>(),
        ];

        for (int i = 0; i < EnemiesList.Count; i++)
        {
            EnemiesList[i].PositionIndex = i + 1;
            InitializeCharacter(EnemiesList[i]);
        }
    }

    private void ConfigureRewards(Reward reward)
    {
        if (reward == null)
            return;

        reward.ClearRewardItems();
        int skillRewardGroups = GetSkillRewardGroupCount();
        for (int i = 0; i < skillRewardGroups; i++)
            reward.AddSkillRewardEntry();

        var rng = new Random(CurrentLevelNode?.RandomNum ?? System.Environment.TickCount);
        var levelType = CurrentLevelNode?.Type ?? LevelNode.LevelType.Normal;
        bool addRelic = ShouldAddRelicReward(levelType);
        int equipCount = GameInfo.RollBattleEquipmentDropCount(levelType, rng);
        bool addItem = GameInfo.RollBattleItemDrop(rng);

        TryAddRelicReward(reward, rng, addRelic);
        AddEquipmentRewards(reward, rng, equipCount);
        TryAddItemReward(reward, rng, addItem);
    }

    private static int GetSkillRewardGroupCount()
    {
        int completedBattleCount =
            GameInfo.CompletedLevelNodeRecords?.Values.Count(record =>
                record != null
                && record.NodeType
                    is LevelNode.LevelType.Normal
                        or LevelNode.LevelType.Elite
                        or LevelNode.LevelType.Boss
            ) ?? 0;

        int bonusGroups = completedBattleCount < EarlyBattleBonusSkillRewardBattles
            ? EarlyBattleExtraSkillRewardGroups
            : 0;
        return 1 + bonusGroups;
    }

    private static bool ShouldAddRelicReward(LevelNode.LevelType levelType)
    {
        return levelType is LevelNode.LevelType.Boss or LevelNode.LevelType.Elite;
    }

    private static void TryAddRelicReward(Reward reward, Random rng, bool addRelic)
    {
        if (!addRelic)
        {
            return;
        }

        var relicDropPool = Relic.GetUnownedOfferPool();
        if (relicDropPool.Length > 0)
        {
            reward.AddRelicRewardEntry(PickRandom(relicDropPool, rng));
        }
    }

    private static void AddEquipmentRewards(Reward reward, Random rng, int equipCount)
    {
        for (int i = 0; i < equipCount; i++)
        {
            reward.AddEquipmentRewardEntry(Equipment.Clone(PickRandom(Equipment.Catalog, rng)));
        }
    }

    private static void TryAddItemReward(Reward reward, Random rng, bool addItem)
    {
        if (!addItem)
        {
            return;
        }

        ItemID[] itemPool = [ItemID.Health, ItemID.Explosion];
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
            dummy.Speed,
            1_000_000_000
        );
        dummy.Skills = [new Skill(Skill.SkillTypes.Attack)];
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
