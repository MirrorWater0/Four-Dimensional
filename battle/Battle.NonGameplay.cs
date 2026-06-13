using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Battle
{
    private static readonly PackedScene ManualTargetArrowScene = GD.Load<PackedScene>(
        "res://battle/UIScene/ManualTarget/ManualTargetArrowView.tscn"
    );

    public event Action<Character, PropertyType, int, Character> PropertyIncreased;

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

    private sealed class SingleTargetDamageIntentionArrow
    {
        public IIntentionPreviewSource Source;
        public Character Target;
        public ManualTargetArrowView View;
        public Vector2 StartPosition;
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
    private const int SingleTargetDamageIntentionArrowLayerOrder = 4;
    private const string SingleTargetDamageIntentionArrowLayerName =
        "SingleTargetDamageIntentionArrowLayer";
    private static readonly Vector2 IncomingDamagePreviewOffset = new(0f, -300f);
    private const float IncomingDamagePreviewFloatAmplitude = 20f;
    private const float IncomingDamagePreviewFloatHalfDuration = 1.5f;
    private static readonly Vector2 IntentionArrowTargetOffset = new(0f, -170f);
    private static readonly Vector2 IntentionArrowSourceGap = new(-26f, 0f);
    private static readonly Color IntentionArrowColor = new(1f, 0.26f, 0.22f, 0.84f);
    private static readonly Color IntentionArrowShadowColor = new(0.06f, 0.0f, 0.0f, 0.68f);
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
    private readonly Dictionary<Character, int> _playerDamageTotals = new();
    private readonly List<VBoxContainer> _incomingDamagePreviewPanels = new();
    private readonly Dictionary<VBoxContainer, Tween> _incomingDamagePreviewTweens = new();
    private readonly List<SingleTargetDamageIntentionArrow> _singleTargetDamageIntentionArrows =
        new();
    private bool _suppressIncomingDamagePreview;

    public bool HasEffectSourceContext => _effectSourceStack.Count > 0;

    private void InitializeNonGameplayUi()
    {
        UsedSkills.Clear();
        UsedSkills.ItemAdded -= OnSkillUsed;
        UsedSkills.ItemAdded += OnSkillUsed;
        _recordIndex = 0;
        _nextEffectSourceContextId = 0;
        _damageRecords.Clear();
        _playerDamageTotals.Clear();
        if (BattleRecord != null)
        {
            BattleRecord.Text = string.Empty;
        }

        InitRecordButton();
    }

    public void RefreshIncomingDamagePreviewFromSettings() => RefreshIncomingDamagePreview();

    public void RefreshSingleTargetDamageIntentionArrowsFromSettings() =>
        RefreshSingleTargetDamageIntentionArrows();

    public void SetIncomingDamagePreviewSuppressed(bool suppressed)
    {
        if (_suppressIncomingDamagePreview == suppressed)
            return;

        _suppressIncomingDamagePreview = suppressed;
        if (_suppressIncomingDamagePreview)
            HideIncomingDamagePreview();
        else
            RefreshIncomingDamagePreview();
    }

    private void RefreshIncomingDamagePreview()
    {
        UserSettings.EnsureLoaded();
        if (_suppressIncomingDamagePreview || !UserSettings.ShowIncomingDamagePreview || !IsBattleAlive())
        {
            HideIncomingDamagePreview();
            return;
        }

        var incomingDamageEntries = BuildIncomingDamagePreviewEntries();
        if (incomingDamageEntries.Length == 0)
        {
            HideIncomingDamagePreview();
            return;
        }

        int panelIndex = 0;
        foreach (Skill.PreviewEffectEntry entry in incomingDamageEntries)
        {
            var panel = GetOrCreateIncomingDamagePreviewPanel(
                entry.Target,
                panelIndex++,
                out bool resetPosition
            );
            if (panel == null)
                continue;

            PreviewEffectDisplay.ShowPanel(
                panel,
                new[] { entry },
                Vector2.Zero,
                IncomingDamagePreviewOffset,
                preservePosition: !resetPosition
            );
            EnsureIncomingDamagePreviewFloat(panel, panelIndex, restart: resetPosition);
        }

        for (int i = panelIndex; i < _incomingDamagePreviewPanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_incomingDamagePreviewPanels[i]))
            {
                _incomingDamagePreviewPanels[i].Visible = false;
                StopIncomingDamagePreviewFloat(_incomingDamagePreviewPanels[i]);
            }
        }
    }

    private void RefreshSingleTargetDamageIntentionArrows()
    {
        UserSettings.EnsureLoaded();
        if (
            !UserSettings.ShowSingleTargetDamageIntentionArrows
            || !IsBattleAlive()
            || EnemiesList == null
        )
        {
            HideSingleTargetDamageIntentionArrows();
            return;
        }

        var pairs = BuildSingleTargetDamageIntentionArrowPairs();
        if (pairs.Length == 0)
        {
            HideSingleTargetDamageIntentionArrows();
            return;
        }

        var layer = EnsureSingleTargetDamageIntentionArrowLayer();
        if (layer == null)
            return;

        int arrowIndex = 0;
        foreach (var pair in pairs)
        {
            var arrow = GetOrCreateSingleTargetDamageIntentionArrow(layer, arrowIndex++);
            arrow.Source = pair.source;
            arrow.Target = pair.target;
            arrow.StartPosition = GetIntentionArrowSourceScreenPosition(pair.source);
            arrow.View.Visible = true;
            UpdateSingleTargetDamageIntentionArrowEndpoint(arrow);
        }

        for (int i = arrowIndex; i < _singleTargetDamageIntentionArrows.Count; i++)
        {
            var arrow = _singleTargetDamageIntentionArrows[i];
            if (arrow?.View != null && GodotObject.IsInstanceValid(arrow.View))
                arrow.View.Visible = false;
            if (arrow != null)
            {
                arrow.Source = null;
                arrow.Target = null;
                arrow.StartPosition = Vector2.Zero;
            }
        }
    }

    private (IIntentionPreviewSource source, Character target)[]
        BuildSingleTargetDamageIntentionArrowPairs()
    {
        return GetEnemyIntentionPreviewSources()
            .Where(source =>
                source?.SourceCharacter != null
                && GodotObject.IsInstanceValid(source.SourceCharacter)
                && source.SourceCharacter.State == Character.CharacterState.Normal
                && source.IntentionControl?.Visible == true
                && !source.HasActiveStun()
            )
            .Select(source => (source, target: GetSingleTargetDamageIntentionTarget(source)))
            .Where(pair => pair.target != null)
            .ToArray();
    }

    private static Character GetSingleTargetDamageIntentionTarget(IIntentionPreviewSource source)
    {
        Character sourceCharacter = source?.SourceCharacter;
        Skill skill = source?.CurrentIntentionSkill;
        if (skill == null)
            return null;

        skill.OwnerCharater = sourceCharacter;
        Character[] targets = skill
            .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.IsPlayer
                && entry.Target.State == Character.CharacterState.Normal
                && entry.Damage > 0
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();

        return targets.Length == 1 ? targets[0] : null;
    }

    private void UpdateSingleTargetDamageIntentionArrowEndpoints()
    {
        for (int i = 0; i < _singleTargetDamageIntentionArrows.Count; i++)
            UpdateSingleTargetDamageIntentionArrowEndpoint(_singleTargetDamageIntentionArrows[i]);
    }

    private static void UpdateSingleTargetDamageIntentionArrowEndpoint(
        SingleTargetDamageIntentionArrow arrow
    )
    {
        if (
            arrow?.View == null
            || !GodotObject.IsInstanceValid(arrow.View)
            || !arrow.View.Visible
        )
        {
            return;
        }

        if (
            arrow.Source?.SourceCharacter == null
            || arrow.Target == null
            || !GodotObject.IsInstanceValid(arrow.Source.SourceCharacter)
            || !GodotObject.IsInstanceValid(arrow.Target)
            || arrow.Source.SourceCharacter.State != Character.CharacterState.Normal
            || arrow.Target.State != Character.CharacterState.Normal
            || arrow.Source.IntentionControl?.Visible != true
            || arrow.Source.HasActiveStun()
        )
        {
            arrow.View.Visible = false;
            return;
        }

        arrow.View.SetEndpoints(
            arrow.StartPosition,
            GetTargetScreenPosition(arrow.Target) + IntentionArrowTargetOffset
        );
    }

    private static Vector2 GetIntentionArrowSourceScreenPosition(IIntentionPreviewSource source)
    {
        Character sourceCharacter = source?.SourceCharacter;
        Control intention = source?.IntentionControl;
        if (intention == null || !GodotObject.IsInstanceValid(intention))
            return GetTargetScreenPosition(sourceCharacter);

        return intention.GetGlobalTransformWithCanvas().Origin
            + new Vector2(0f, intention.Size.Y * 0.5f)
            + IntentionArrowSourceGap;
    }

    private SingleTargetDamageIntentionArrow GetOrCreateSingleTargetDamageIntentionArrow(
        CanvasLayer layer,
        int index
    )
    {
        while (_singleTargetDamageIntentionArrows.Count <= index)
        {
            var arrow = new SingleTargetDamageIntentionArrow
            {
                View = CreateSingleTargetDamageIntentionArrowView(),
            };
            layer.AddChild(arrow.View);
            _singleTargetDamageIntentionArrows.Add(arrow);
        }

        var pooledArrow = _singleTargetDamageIntentionArrows[index];
        if (pooledArrow == null)
        {
            pooledArrow = new SingleTargetDamageIntentionArrow();
            _singleTargetDamageIntentionArrows[index] = pooledArrow;
        }

        if (!GodotObject.IsInstanceValid(pooledArrow.View))
            pooledArrow.View = CreateSingleTargetDamageIntentionArrowView();

        if (pooledArrow.View.GetParent() == null)
        {
            layer.AddChild(pooledArrow.View);
        }
        else if (pooledArrow.View.GetParent() != layer)
        {
            pooledArrow.View.GetParent().RemoveChild(pooledArrow.View);
            layer.AddChild(pooledArrow.View);
        }

        return pooledArrow;
    }

    private static ManualTargetArrowView CreateSingleTargetDamageIntentionArrowView()
    {
        var arrow = ManualTargetArrowScene?.Instantiate<ManualTargetArrowView>()
            ?? new ManualTargetArrowView();
        arrow.Name = "SingleTargetDamageIntentionArrow";
        arrow.MouseFilter = Control.MouseFilterEnum.Ignore;
        arrow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        arrow.ArrowColor = IntentionArrowColor;
        arrow.ShadowColor = IntentionArrowShadowColor;
        arrow.ArrowWidth = 5f;
        arrow.ShadowWidth = 10f;
        arrow.CurveLift = 145f;
        arrow.HeadSize = new Vector2(30f, 24f);
        arrow.TailSize = new Vector2(18f, 12f);
        arrow.TailShaftInset = 4f;
        arrow.Visible = false;
        return arrow;
    }

    private void HideSingleTargetDamageIntentionArrows()
    {
        for (int i = 0; i < _singleTargetDamageIntentionArrows.Count; i++)
        {
            var arrow = _singleTargetDamageIntentionArrows[i];
            if (arrow?.View != null && GodotObject.IsInstanceValid(arrow.View))
                arrow.View.Visible = false;
            if (arrow != null)
            {
                arrow.Source = null;
                arrow.Target = null;
                arrow.StartPosition = Vector2.Zero;
            }
        }
    }

    private void FreeSingleTargetDamageIntentionArrows()
    {
        for (int i = 0; i < _singleTargetDamageIntentionArrows.Count; i++)
        {
            var arrow = _singleTargetDamageIntentionArrows[i];
            if (arrow?.View != null && GodotObject.IsInstanceValid(arrow.View))
                arrow.View.QueueFree();
        }
        _singleTargetDamageIntentionArrows.Clear();

        GetTree()
            ?.Root
            ?.GetNodeOrNull<CanvasLayer>(SingleTargetDamageIntentionArrowLayerName)
            ?.QueueFree();
    }

    private Skill.PreviewEffectEntry[] BuildIncomingDamagePreviewEntries()
    {
        var totals = new Dictionary<Character, int>();
        foreach (var source in GetIncomingDamagePreviewSources())
        {
            Character sourceCharacter = source?.SourceCharacter;
            if (source == null || sourceCharacter == null || source.HasActiveStun())
                continue;

            Skill skill = source.CurrentIntentionSkill;
            if (skill == null)
                continue;

            skill.OwnerCharater = sourceCharacter;
            foreach (Skill.PreviewEffectEntry entry in skill.GetPreviewEffectEntries())
            {
                if (
                    entry.Kind != Skill.PreviewEffectKind.Damage
                    || entry.Target == null
                    || !GodotObject.IsInstanceValid(entry.Target)
                    || !entry.Target.IsPlayer
                    || entry.Target.State != Character.CharacterState.Normal
                    || entry.Value <= 0
                )
                {
                    continue;
                }

                totals[entry.Target] = totals.TryGetValue(entry.Target, out int current)
                    ? current + entry.Value
                    : entry.Value;
            }
        }

        return totals
            .Where(pair => pair.Value > 0)
            .OrderBy(pair => pair.Key.PositionIndex)
            .Select(pair =>
                Skill.PreviewEffectEntry.Damage(pair.Key, pair.Value, 1, 1, source: null)
            )
            .ToArray();
    }

    private IEnumerable<IIntentionPreviewSource> GetIncomingDamagePreviewSources()
    {
        if (_activeEnemyPhaseOrder.Count > 0)
        {
            int startIndex = Math.Max(_activeEnemyPhaseIndex + 1, 0);
            if (
                CurrentActionCharacter is IIntentionPreviewSource currentSource
                && IsCharacterAlive(currentSource.SourceCharacter)
            )
            {
                yield return currentSource;
                if (currentSource.SourceCharacter is EnemyCharacter currentEnemy)
                {
                    foreach (var summon in GetLivingEnemySummonIntentionSources(currentEnemy))
                        yield return summon;
                }
            }

            for (int i = startIndex; i < _activeEnemyPhaseOrder.Count; i++)
            {
                EnemyCharacter enemy = _activeEnemyPhaseOrder[i];
                if (enemy != null && GodotObject.IsInstanceValid(enemy) && IsCharacterAlive(enemy))
                {
                    yield return enemy;
                    foreach (var summon in GetLivingEnemySummonIntentionSources(enemy))
                        yield return summon;
                }
            }
            yield break;
        }

        foreach (EnemyCharacter enemy in GetEnemyPhaseActionOrder())
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy) && IsCharacterAlive(enemy))
            {
                yield return enemy;
                foreach (var summon in GetLivingEnemySummonIntentionSources(enemy))
                    yield return summon;
            }
        }
    }

    private static IEnumerable<IIntentionPreviewSource> GetLivingEnemySummonIntentionSources(
        Character summoner
    )
    {
        if (summoner?.Summons == null)
            yield break;

        foreach (var summon in summoner.Summons)
        {
            if (
                summon != null
                && GodotObject.IsInstanceValid(summon)
                && !summon.IsPlayer
                && IsCharacterAlive(summon)
            )
            {
                yield return summon;
            }
        }
    }

    private void HideIncomingDamagePreview()
    {
        for (int i = 0; i < _incomingDamagePreviewPanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_incomingDamagePreviewPanels[i]))
            {
                _incomingDamagePreviewPanels[i].Visible = false;
                StopIncomingDamagePreviewFloat(_incomingDamagePreviewPanels[i]);
            }
        }
    }

    private void FreeIncomingDamagePreviewLabels()
    {
        for (int i = 0; i < _incomingDamagePreviewPanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_incomingDamagePreviewPanels[i]))
            {
                StopIncomingDamagePreviewFloat(_incomingDamagePreviewPanels[i]);
                _incomingDamagePreviewPanels[i].QueueFree();
            }
        }
        _incomingDamagePreviewPanels.Clear();
        _incomingDamagePreviewTweens.Clear();
    }

    private void EnsureIncomingDamagePreviewFloat(
        VBoxContainer panel,
        int phaseIndex,
        bool restart = false
    )
    {
        if (panel == null || !GodotObject.IsInstanceValid(panel))
            return;

        if (
            !restart
            && _incomingDamagePreviewTweens.TryGetValue(panel, out Tween activeTween)
            && GodotObject.IsInstanceValid(activeTween)
        )
        {
            return;
        }

        StartIncomingDamagePreviewFloat(panel, phaseIndex);
    }

    private void StartIncomingDamagePreviewFloat(VBoxContainer panel, int phaseIndex)
    {
        if (panel == null || !GodotObject.IsInstanceValid(panel))
            return;

        StopIncomingDamagePreviewFloat(panel);

        Vector2 basePosition = panel.Position;
        Vector2 floatPosition = basePosition + new Vector2(0f, -IncomingDamagePreviewFloatAmplitude);
        float phaseDelay = 0.12f * (phaseIndex % 3);

        Tween tween = panel.CreateTween();
        tween.SetLoops();
        if (phaseDelay > 0f)
            tween.TweenInterval(phaseDelay);
        tween
            .TweenProperty(panel, "position", floatPosition, IncomingDamagePreviewFloatHalfDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween
            .TweenProperty(panel, "position", basePosition, IncomingDamagePreviewFloatHalfDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);

        _incomingDamagePreviewTweens[panel] = tween;
    }

    private void StopIncomingDamagePreviewFloat(VBoxContainer panel)
    {
        if (panel == null)
            return;

        if (_incomingDamagePreviewTweens.TryGetValue(panel, out Tween tween))
        {
            if (GodotObject.IsInstanceValid(tween))
                tween.Kill();
            _incomingDamagePreviewTweens.Remove(panel);
        }
    }

    private VBoxContainer GetOrCreateIncomingDamagePreviewPanel(
        Character target,
        int index,
        out bool resetPosition
    )
    {
        resetPosition = true;
        if (target == null || !GodotObject.IsInstanceValid(target))
            return null;

        while (_incomingDamagePreviewPanels.Count <= index)
        {
            var panel = PreviewEffectDisplay.CreatePanel();
            target.AddChild(panel);
            _incomingDamagePreviewPanels.Add(panel);
        }

        var pooledPanel = _incomingDamagePreviewPanels[index];
        if (!GodotObject.IsInstanceValid(pooledPanel))
        {
            pooledPanel = PreviewEffectDisplay.CreatePanel();
            target.AddChild(pooledPanel);
            _incomingDamagePreviewPanels[index] = pooledPanel;
            resetPosition = true;
        }
        else if (pooledPanel.GetParent() == null)
        {
            target.AddChild(pooledPanel);
            resetPosition = true;
        }
        else if (pooledPanel.GetParent() != target)
        {
            pooledPanel.GetParent().RemoveChild(pooledPanel);
            target.AddChild(pooledPanel);
            resetPosition = true;
        }
        else
        {
            resetPosition = !pooledPanel.Visible;
        }

        return pooledPanel;
    }

    private CanvasLayer EnsureSingleTargetDamageIntentionArrowLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var existingLayer = root.GetNodeOrNull<CanvasLayer>(
            SingleTargetDamageIntentionArrowLayerName
        );
        if (existingLayer != null)
        {
            existingLayer.Layer = SingleTargetDamageIntentionArrowLayerOrder;
            return existingLayer;
        }

        existingLayer = new CanvasLayer
        {
            Layer = SingleTargetDamageIntentionArrowLayerOrder,
            Name = SingleTargetDamageIntentionArrowLayerName,
        };
        root.AddChild(existingLayer);
        return existingLayer;
    }

    private static Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        return target.GetGlobalTransformWithCanvas().Origin;
    }

    public override void _Process(double delta)
    {
        UpdateSingleTargetDamageIntentionArrowEndpoints();

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
        RecordPlayerDamageTotal(target, actualDamage);

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

    private void RecordPlayerDamageTotal(Character target, int totalDamage)
    {
        if (totalDamage <= 0 || target is not PlayerCharacter player)
        {
            return;
        }

        _playerDamageTotals[player] = _playerDamageTotals.TryGetValue(player, out int current)
            ? current + totalDamage
            : totalDamage;
    }

    public List<string> BuildPlayerDamageSummaryLines()
    {
        var lines = new List<string>();
        var players = PlayersList?
            .Where(player => player != null && GodotObject.IsInstanceValid(player))
            .ToArray();

        if (players != null && players.Length > 0)
        {
            foreach (var player in players)
            {
                string name = GetRecordBaseCharacterName(player);
                int damage = _playerDamageTotals.TryGetValue(player, out int total) ? total : 0;
                lines.Add($"{name} {damage}");
            }

            return lines;
        }

        foreach (var entry in _playerDamageTotals.OrderBy(entry => GetRecordBaseCharacterName(entry.Key)))
        {
            if (entry.Key == null)
                continue;

            lines.Add($"{GetRecordBaseCharacterName(entry.Key)} {entry.Value}");
        }

        return lines;
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

    public void RecordPlayerEnergyChange(int delta, Character source = null)
    {
        if (delta == 0)
            return;

        string sourceText = FormatRecordSource(source);
        string action = delta > 0 ? "获得" : "失去";
        AppendRecordLine(
            $"{sourceText} -> [color={RecordTargetColor}]玩家能量[/color]  {action}  [color={RecordNeutralColor}]{Math.Abs(delta)}[/color] 点能量",
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

        if (delta > 0)
            PropertyIncreased?.Invoke(target, type, delta, source);
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

    public void RecordDebuffImmunityConsume(
        Character target,
        Buff.BuffName? blockedBuffName = null,
        Character source = null
    )
    {
        if (target == null)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        string blockedText = blockedBuffName.HasValue
            ? blockedBuffName.Value.GetDescription()
            : "负面状态";
        AppendRecordLine(
            $"{sourceText} -> {targetText}  被{Buff.BuffName.DebuffImmunity.GetDescription()} [color={RecordNeutralColor}]抵消[/color] {blockedText}",
            indent: true
        );
    }

    public void RecordStatusCardInsert(
        Character target,
        SkillID skillId,
        int count,
        bool toHand = false,
        BattleCardPileTarget pileTarget = BattleCardPileTarget.DrawPileCards,
        Character source = null
    )
    {
        if (target == null || count <= 0)
            return;

        string sourceText = FormatRecordSource(source);
        string targetText = FormatRecordActor(target, RecordTargetColor, "未知目标");
        Skill skill = Skill.GetSkill(skillId);
        string skillName = string.IsNullOrWhiteSpace(skill?.SkillName)
            ? skillId.ToString()
            : skill.SkillName;
        string destination = toHand
            ? "手牌"
            : pileTarget switch
            {
                BattleCardPileTarget.HandCards => "手牌",
                BattleCardPileTarget.DiscardPileCards => "弃牌堆",
                _ => "抽牌堆",
            };
        AppendRecordLine(
            $"{sourceText} -> {targetText}  向{destination}塞入  [color={RecordNeutralColor}]{count}[/color] 张[color={RecordSkillColor}]{skillName}[/color]",
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
        Relic.ApplySkillUsedRelicEffects(skill);
        _ = TriggerVoidOnSurviveSkillUsedAsync(skill);

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

    private async Task TriggerVoidOnSurviveSkillUsedAsync(Skill skill)
    {
        Character owner = skill?.OwnerCharater;
        if (
            skill == null
            || skill.SkillType != Skill.SkillTypes.Survive
            || owner == null
            || !GodotObject.IsInstanceValid(owner)
            || owner.State == Character.CharacterState.Dying
            || owner.BattleNode != this
            || HasBattleEnded()
            || !IsBattleAlive()
        )
        {
            return;
        }

        Character[] targets = GetTeamCharacters(owner.IsPlayer, includeSummons: true)
            .Where(target =>
                target != null
                && target != owner
                && GodotObject.IsInstanceValid(target)
                && target.State != Character.CharacterState.Dying
                && target.EndActionBuffs?.Any(buff =>
                    buff != null
                    && buff.ThisBuffName == Buff.BuffName.Void
                    && buff.Stack > 0
                ) == true
            )
            .ToArray();

        for (int i = 0; i < targets.Length; i++)
        {
            Character target = targets[i];
            if (target == null || !GodotObject.IsInstanceValid(target))
                continue;

            int voidStacks =
                target
                    .EndActionBuffs?.Where(buff =>
                        buff != null
                        && buff.ThisBuffName == Buff.BuffName.Void
                        && buff.Stack > 0
                    )
                    .Sum(buff => buff.Stack) ?? 0;
            if (voidStacks <= 0)
                continue;

            using var _ = target.BeginEffectSource(Buff.GetBuffDisplayName(Buff.BuffName.Void));
            await target.IncreaseProperties(PropertyType.Power, voidStacks, target);

            if (HasBattleEnded() || !IsBattleAlive())
                return;
        }
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
