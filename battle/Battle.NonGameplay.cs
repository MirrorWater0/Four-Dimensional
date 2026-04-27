using System;
using System.Collections.Generic;
using Godot;

public partial class Battle
{
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
    public bool HoverPerfLogEnabled { get; set; } = true;

    [Export]
    public double HoverPerfFrameSpikeMs { get; set; } = 28.0;

    [Export]
    public double HoverPerfWorkSpikeMs { get; set; } = 2.0;

    private ulong _lastHoverPerfTickMsec;
    private ulong _lastHoverFrameSpikeLogTickMsec;
    private string _lastHoverPerfLabel = string.Empty;
    private string _lastHoverPerfCharacter = string.Empty;

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

    public bool HasEffectSourceContext => _effectSourceStack.Count > 0;

    private void InitializeNonGameplayUi()
    {
        UsedSkills.Clear();
        UsedSkills.ItemAdded -= OnSkillUsed;
        UsedSkills.ItemAdded += OnSkillUsed;
        _recordIndex = 0;
        _nextEffectSourceContextId = 0;
        _damageRecords.Clear();
        if (BattleRecord != null)
        {
            BattleRecord.Text = string.Empty;
        }

        InitRecordButton();
    }

    public override void _Process(double delta)
    {
        if (!HoverPerfLogEnabled || WarmupMode)
            return;

        double frameMs = delta * 1000.0;
        if (frameMs < HoverPerfFrameSpikeMs)
            return;

        ulong now = Time.GetTicksMsec();
        ulong elapsedFromHover =
            _lastHoverPerfTickMsec == 0 ? ulong.MaxValue : now - _lastHoverPerfTickMsec;
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

    public IDisposable PushEffectSource(Character sourceCharacter, string actionName = null)
    {
        _effectSourceStack.Add(
            new EffectSourceContext(++_nextEffectSourceContextId, sourceCharacter, actionName)
        );
        return new EffectSourceScope(this);
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

    private static string FormatRecordActor(Character character, string color, string fallback = "系统")
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
}
