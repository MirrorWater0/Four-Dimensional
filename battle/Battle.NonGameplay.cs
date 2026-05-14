using System;
using System.Collections.Generic;
using System.Linq;
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
    private Control ActionPoinBox => field ??= GetNodeOrNull<Control>("ActionPoinBox");

    private const float RecordSlideDuration = 0.2f;
    private const float RecordHideMargin = 18f;
    private const string RecordIndexColor = "#b0b6c2";
    private const string RecordSourceColor = "#ffd36b";
    private const string RecordTargetColor = "#8fd3ff";
    private const string RecordSkillColor = "#b56bff";
    private const string RecordDamageColor = "#ff7b7b";
    private const string RecordHealColor = "#6bff8f";
    private const string RecordNeutralColor = "#d9e2f2";
    private const string ActionPoinTooltipText =
        "[b]\u884c\u52a8\u70b9\u4e0e\u901f\u5ea6[/b]\n"
        + "\u6761\u4e0a\u6570\u5b57\u662f\u5f53\u524d\u884c\u52a8\u70b9\uff0c\u62ec\u53f7\u91cc\u662f\u8be5\u9635\u8425\u5b58\u6d3b\u6210\u5458\u7684\u603b\u901f\u5ea6\u3002\n\n"
        + "\u89d2\u8272\u884c\u52a8\u7ed3\u675f\u540e\uff0c\u6240\u5c5e\u9635\u8425\u6309\u603b\u901f\u5ea6\u7d2f\u79ef\u884c\u52a8\u70b9\u3002\u8fbe\u5230 100 \u65f6\uff0c\u8be5\u9635\u8425\u83b7\u5f97\u4e00\u6b21\u989d\u5916\u51fa\u624b\u673a\u4f1a\uff0c\u5e76\u7ed9\u8fd9\u6b21\u884c\u52a8\u7684\u89d2\u8272 1 \u70b9\u80fd\u91cf\u548c 1 \u70b9\u62bd\u5361\u50a8\u5907\u3002";

    private bool _recordInitialized;
    private bool _actionPoinTooltipInitialized;
    private Tip _actionPoinTooltip;
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
        InitActionPoinTooltip();
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

    private void InitActionPoinTooltip()
    {
        if (_actionPoinTooltipInitialized)
            return;

        var actionPoinBox = ActionPoinBox;
        if (actionPoinBox == null)
            return;

        _actionPoinTooltipInitialized = true;
        actionPoinBox.MouseFilter = Control.MouseFilterEnum.Stop;
        actionPoinBox.MouseEntered += ShowActionPoinTooltip;
        actionPoinBox.MouseExited += HideActionPoinTooltip;
        actionPoinBox.TreeExiting += HideActionPoinTooltip;

        foreach (Node child in actionPoinBox.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = Control.MouseFilterEnum.Pass;
        }
    }

    private void ShowActionPoinTooltip()
    {
        var tip = EnsureActionPoinTooltip();
        if (tip == null)
            return;

        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(20f, 22f);
        tip.MinContentWidth = 430f;
        tip.SetText(ActionPoinTooltipText);
    }

    private void HideActionPoinTooltip()
    {
        _actionPoinTooltip?.HideTooltip();
    }

    private Tip EnsureActionPoinTooltip()
    {
        if (_actionPoinTooltip != null && GodotObject.IsInstanceValid(_actionPoinTooltip))
            return _actionPoinTooltip;

        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.AddChild(layer);
        }

        _actionPoinTooltip = layer.GetNodeOrNull<Tip>("ActionPoinTip");
        if (_actionPoinTooltip != null)
            return _actionPoinTooltip;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return null;

        _actionPoinTooltip = tipScene.Instantiate<Tip>();
        _actionPoinTooltip.Name = "ActionPoinTip";
        layer.AddChild(_actionPoinTooltip);
        return _actionPoinTooltip;
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

    private string GetRecordCharacterName(Character character)
    {
        if (character == null)
            return "系统";

        string baseName = GetRecordBaseCharacterName(character);
        if (ShouldNumberEnemyInRecord(character, baseName))
            return $"{baseName} {GetEnemyRecordNumber(character, baseName)}";

        return baseName;
    }

    private static string GetRecordBaseCharacterName(Character character)
    {
        if (character == null)
            return "系统";

        if (!string.IsNullOrWhiteSpace(character.CharacterName))
            return character.CharacterName;
        if (!string.IsNullOrWhiteSpace(character.Name))
            return character.Name;

        return character.GetType().Name;
    }

    private bool ShouldNumberEnemyInRecord(Character character, string baseName)
    {
        if (character == null || character.IsPlayer || string.IsNullOrWhiteSpace(baseName))
            return false;

        return GetEnemyRecordNameGroup(baseName).Skip(1).Any();
    }

    private int GetEnemyRecordNumber(Character character, string baseName)
    {
        Character[] group = GetEnemyRecordNameGroup(baseName);
        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == character)
                return i + 1;
        }

        return Math.Max(1, group.Length + 1);
    }

    private Character[] GetEnemyRecordNameGroup(string baseName)
    {
        return GetTeamCharacters(isPlayer: false, includeSummons: true)
            .Where(character =>
                character != null
                && GodotObject.IsInstanceValid(character)
                && string.Equals(
                    GetRecordBaseCharacterName(character),
                    baseName,
                    StringComparison.Ordinal
                )
            )
            .OrderBy(character => character.PositionIndex)
            .ThenBy(character => character.GetInstanceId())
            .ToArray();
    }

    private string FormatRecordActor(Character character, string color, string fallback = "系统")
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
            line += $"(格挡吸收 [color={RecordNeutralColor}]{blockedDamage}[/color])";

        AppendRecordLine(line, indent: true);
    }

    public int GetLastRecordedDamageFromCurrentEffectSource(
        Character source = null,
        Character target = null,
        bool includeBlockedDamage = false
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

            return includeBlockedDamage ? entry.ActualDamage + entry.BlockedDamage : entry.ActualDamage;
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

    public void RecordCardDrawReserveChange(Character target, int delta, Character source = null)
    {
        if (delta == 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor);
        string action = delta > 0 ? "\u83b7\u5f97" : "\u5931\u53bb";
        AppendRecordLine(
            $"{sourceText} -> {targetText}  {action}  [color={RecordNeutralColor}]{Math.Abs(delta)}[/color] \u70b9\u62bd\u5361\u50a8\u5907",
            indent: true
        );
    }

    public void RecordCardDrawReserveUse(Character actor, int reserveCost, int drawnCards)
    {
        if (actor == null || reserveCost <= 0 || drawnCards <= 0)
            return;

        string sourceText = FormatRecordSource(actor);
        string targetText = FormatRecordActor(actor, RecordTargetColor);
        AppendRecordLine(
            $"{sourceText} -> {targetText}  \u4f7f\u7528  [color={RecordNeutralColor}]{reserveCost}[/color] \u70b9\u62bd\u5361\u50a8\u5907\uff0c\u62bd\u53d6  [color={RecordNeutralColor}]{drawnCards}[/color] \u5f20\u724c",
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

        string characterName = skill.OwnerCharater == null
            ? "Unknown"
            : GetRecordCharacterName(skill.OwnerCharater);
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
