using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character
{
    private const float DefaultIntentWeight = 3f;
    private const int DefaultSpecialIntentThreshold = 3;
    private const float SpecialIntentWeightMin = 0.5f;
    private const float SpecialIntentWeightMax = 7f;
    private const float IntentCurveTargetLift = 92f;
    private const float IntentCurveSplitX = 26f;
    private const float IntentCurveGroupSpreadX = 36f;
    private static readonly Color AttackIntentCurveColor = new(1f, 0.42f, 0.32f, 0.82f);
    private static readonly Color DebuffIntentCurveColor = new(0.52f, 0.94f, 1f, 0.88f);
    public const int NextActionEnergyPreviewBonus = 3;
    private static readonly Color IntentionHostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color IntentionFriendlyTargetPreviewColor = new(0.48f, 0.82f, 0.62f, 0.82f);
    private static readonly Vector2 IntentionDamageLabelOffset = new(-50f, -130f);
    private static readonly Vector2 IntentionDamageSummaryOffset = new(38f, -18f);
    private static readonly Vector2 IntentionDamageSummaryFallbackSize = new(150f, 58f);
    private static readonly Color IntentionDamageColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color IntentionDamageOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);

    public EnemyRegedit Registry;
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    public ColorRect SurviveIntention => field ??= GetNode<ColorRect>("Intention/Survive");
    public ColorRect SpecialIntention => field ??= GetNode<ColorRect>("Intention/Special");
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex = -1;
    private Character[] _intentionPreviewHostileTargets = Array.Empty<Character>();
    private Character[] _intentionPreviewFriendlyTargets = Array.Empty<Character>();
    private int _intentionPreviewHoverDepth;
    private readonly List<VBoxContainer> _intentionDamagePanels = new();
    private readonly Dictionary<string, Line2D> _intentPreviewLines = new();
    private Label _intentionDamageSummaryLabel;

    public override void _Ready()
    {
        base._Ready();
        IsPlayer = false;
        Hoverframe.MouseEntered += OnIntentionPreviewHoverEntered;
        Hoverframe.MouseExited += OnIntentionPreviewHoverExited;
        IntentionContorl.MouseEntered += OnIntentionPreviewHoverEntered;
        IntentionContorl.MouseExited += OnIntentionPreviewHoverExited;
    }

    public override void _ExitTree()
    {
        HideIntentionTargetPreview();
        HideAttackIntentCurve();
        FreeIntentionDamageLabels();
        base._ExitTree();
    }

    public override async Task Dying(Character source = null)
    {
        _intentionPreviewHoverDepth = 0;
        HideIntentionDamageSummary();
        HideIntentionTargetPreview();
        HideAttackIntentCurve();
        await base.Dying(source);
    }

    public override void Initialize()
    {
        if (Registry != null)
        {
            CharacterName = Registry.CharacterName;
            PassiveName = Registry.PassiveName;
            PassiveDescription = Registry.PassiveDescription;
            SetCombatStats(Registry.Power, Registry.Survivability, Registry.Speed, Registry.MaxLife);
            Life = BattleMaxLife;
            Skills = (Registry.SkillIDs ?? Array.Empty<SkillID>())
                .Select(Skill.GetSkill)
                .Where(x => x != null)
                .ToArray();
            if (Skills.Length == 0)
                Skills = new Skill[3];
        }
        base.Initialize();
    }

    protected Character[] ChooseHostileTargetsByOrder(
        bool byBehindRow = false,
        bool returnDummyWhenEmpty = true,
        bool normalOnly = true,
        bool dyingFilter = false,
        bool applyTaunt = false
    )
    {
        return Skill.ChooseHostileTargetsByOrder(
            this,
            byBehindRow,
            returnDummyWhenEmpty,
            normalOnly,
            dyingFilter,
            applyTaunt
        );
    }

    public override async void StartAction()
    {
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        base.StartAction();
        Skill skill = GetCurrentIntentionSkill();
        if (!CanUseIntentionSkill(skill, Energy))
        {
            IntentionIndex = RollIntentionIndex();
            skill = GetCurrentIntentionSkill();
        }

        await DisappearIntention();
        if (CanUseIntentionSkill(skill, Energy))
            await skill.Effect();

        EndAction();
    }

    public override void OnTurnEnd()
    {
        IntentionIndex = RollIntentionIndex(NextActionEnergyPreviewBonus);
        DisplayIntention();
        base.OnTurnEnd();
    }

    public int RollIntentionIndex(int energyPreviewBonus = 0)
    {
        if (Skills == null || Skills.Length == 0)
            return -1;

        foreach (var skill in Skills)
            skill?.ClearPreviewableRandomHostileTargets();

        int availableEnergy = Math.Max(Energy + energyPreviewBonus, 0);
        int avoidIndex = GetRepeatIntentionAvoidIndex(availableEnergy);
        float totalWeight = 0f;
        float[] weights = new float[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            float weight = i == avoidIndex ? 0f : GetIntentionWeight(Skills[i], availableEnergy);
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                if (CanUseIntentionSkill(Skills[i], availableEnergy))
                    return i;
            }
            return -1;
        }

        float roll = (float)BattleNode.BattleIntentionRandom.NextDouble() * totalWeight;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return i;
        }

        for (int i = weights.Length - 1; i >= 0; i--)
        {
            if (weights[i] > 0f)
                return i;
        }

        return -1;
    }

    private int GetRepeatIntentionAvoidIndex(int availableEnergy)
    {
        if (IntentionIndex < 0 || IntentionIndex >= (Skills?.Length ?? 0))
            return -1;

        if (!CanUseIntentionSkill(Skills[IntentionIndex], availableEnergy))
            return -1;

        for (int i = 0; i < Skills.Length; i++)
        {
            if (i != IntentionIndex && CanUseIntentionSkill(Skills[i], availableEnergy))
                return IntentionIndex;
        }

        return -1;
    }

    private float GetIntentionWeight(Skill skill, int availableEnergy)
    {
        if (!CanUseIntentionSkill(skill, availableEnergy))
            return 0f;

        if (skill.SkillType != Skill.SkillTypes.Special)
            return DefaultIntentWeight;

        int energyDelta = Energy - DefaultSpecialIntentThreshold;
        return Math.Clamp(
            DefaultIntentWeight + energyDelta,
            SpecialIntentWeightMin,
            SpecialIntentWeightMax
        );
    }

    private bool CanUseIntentionSkill(Skill skill, int availableEnergy)
    {
        if (skill == null)
            return false;

        skill.OwnerCharater = this;
        return skill.CanUseEnergy(availableEnergy);
    }

    public async Task DisappearIntention()
    {
        HideIntentionTargetPreview();
        HideIntentionDamageSummary();
        Buff.GhostExplode(IntentionContorl, new Vector2(2, 2), useOffsetMotion: false);
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
    }

    public void DisplayIntention()
    {
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;

        var skill = GetCurrentIntentionSkill();
        if (skill == null)
        {
            HideIntentionDamageSummary();
            IntentionContorl.Visible = false;
            return;
        }

        IntentionContorl.Visible = true;
        IntentionContorl.Modulate = new Color(1, 1, 1, 0);
        IntentionContorl.Scale = new Vector2(1.8f, 1.8f);
        switch (skill.SkillType)
        {
            case Skill.SkillTypes.Attack:
                AttackIntention.Visible = true;
                break;
            case Skill.SkillTypes.Survive:
                SurviveIntention.Visible = true;
                break;
            case Skill.SkillTypes.Special:
                SpecialIntention.Visible = true;
                break;
        }

        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(IntentionContorl, "modulate", new Color(1, 1, 1, 1), 0.2f);
        tween
            .TweenProperty(IntentionContorl, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);

        RefreshIntentionDamageSummary();

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    public void RefreshIntentionDamageSummary()
    {
        if (State == CharacterState.Dying)
        {
            HideIntentionDamageSummary();
            return;
        }

        InvalidateSkillTooltipCache();

        var skill = GetCurrentIntentionSkill();
        if (skill == null)
        {
            HideIntentionDamageSummary();
            return;
        }

        string text = BuildIntentionDamageSummaryText(skill);
        if (string.IsNullOrWhiteSpace(text))
        {
            HideIntentionDamageSummary();
            return;
        }

        Label label = GetOrCreateIntentionDamageSummaryLabel();
        label.Text = text;
        label.Visible = true;

        Vector2 size = label.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = IntentionDamageSummaryFallbackSize;
        label.Size = size;
        label.Position = IntentionDamageSummaryOffset;

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    private void HideIntentionDamageSummary()
    {
        if (GodotObject.IsInstanceValid(_intentionDamageSummaryLabel))
            _intentionDamageSummaryLabel.Visible = false;
    }

    private void OnIntentionPreviewHoverEntered()
    {
        if (State == CharacterState.Dying)
            return;

        ulong hoverStartUsec = Time.GetTicksUsec();
        _intentionPreviewHoverDepth++;
        if (_intentionPreviewHoverDepth == 1)
        {
            BattleNode?.MarkHoverPerfEvent(this, "enemy-intention-hover");
            ShowIntentionTargetPreview();
            BattleNode?.LogHoverPerfWork(this, "enemy-intention-hover", hoverStartUsec);
        }
    }

    private void OnIntentionPreviewHoverExited()
    {
        _intentionPreviewHoverDepth = Math.Max(0, _intentionPreviewHoverDepth - 1);
        if (_intentionPreviewHoverDepth == 0)
            HideIntentionTargetPreview();
    }

    private void ShowIntentionTargetPreview()
    {
        ulong totalStartUsec = Time.GetTicksUsec();
        HideIntentionTargetPreview();
        if (State == CharacterState.Dying)
            return;

        var skill = GetCurrentIntentionSkill();
        if (skill == null)
            return;

        ulong stepStartUsec = Time.GetTicksUsec();
        _intentionPreviewHostileTargets = skill
            .GetPreviewHostileTargets()
            .Where(GodotObject.IsInstanceValid)
            .Distinct()
            .ToArray();
        _intentionPreviewFriendlyTargets = skill
            .GetPreviewFriendlyTargets()
            .Where(GodotObject.IsInstanceValid)
            .Distinct()
            .ToArray();
        BattleNode?.LogHoverPerfWork(this, "enemy-intention-targets", stepStartUsec);

        stepStartUsec = Time.GetTicksUsec();
        for (int i = 0; i < _intentionPreviewHostileTargets.Length; i++)
        {
            _intentionPreviewHostileTargets[i].ShowTargetPreview(
                IntentionHostileTargetPreviewColor
            );
        }
        for (int i = 0; i < _intentionPreviewFriendlyTargets.Length; i++)
        {
            _intentionPreviewFriendlyTargets[i].ShowTargetPreview(
                IntentionFriendlyTargetPreviewColor
            );
        }
        BattleNode?.LogHoverPerfWork(this, "enemy-intention-highlight", stepStartUsec);

        stepStartUsec = Time.GetTicksUsec();
        var entries = skill.GetPreviewEffectEntries();
        BattleNode?.LogHoverPerfWork(this, "enemy-intention-damage-preview", stepStartUsec);

        stepStartUsec = Time.GetTicksUsec();
        ShowIntentionDamageLabels(entries);
        BattleNode?.LogHoverPerfWork(this, "enemy-intention-tip-show", stepStartUsec);
        BattleNode?.LogHoverPerfWork(this, "enemy-intention-preview-total", totalStartUsec);
    }

    private void HideIntentionTargetPreview()
    {
        if (
            (_intentionPreviewHostileTargets == null || _intentionPreviewHostileTargets.Length == 0)
            && (
                _intentionPreviewFriendlyTargets == null
                || _intentionPreviewFriendlyTargets.Length == 0
            )
        )
        {
            _intentionPreviewHostileTargets = Array.Empty<Character>();
            _intentionPreviewFriendlyTargets = Array.Empty<Character>();
            ClearIntentionDamageLabels();
            return;
        }

        for (int i = 0; i < _intentionPreviewHostileTargets.Length; i++)
        {
            if (GodotObject.IsInstanceValid(_intentionPreviewHostileTargets[i]))
                _intentionPreviewHostileTargets[i].HideTargetPreview();
        }

        for (int i = 0; i < _intentionPreviewFriendlyTargets.Length; i++)
        {
            if (GodotObject.IsInstanceValid(_intentionPreviewFriendlyTargets[i]))
                _intentionPreviewFriendlyTargets[i].HideTargetPreview();
        }

        _intentionPreviewHostileTargets = Array.Empty<Character>();
        _intentionPreviewFriendlyTargets = Array.Empty<Character>();
        ClearIntentionDamageLabels();
    }

    private Skill GetCurrentIntentionSkill()
    {
        if (Skills == null || IntentionIndex < 0 || IntentionIndex >= Skills.Length)
            return null;

        return Skills[IntentionIndex];
    }

    public Character[] GetCurrentIntentionPreviewTargets()
    {
        var skill = GetCurrentIntentionSkill();
        return skill?
                .GetPreviewHostileTargets()
                .Where(GodotObject.IsInstanceValid)
                .Distinct()
                .ToArray()
            ?? Array.Empty<Character>();
    }

    public Skill.PreviewDamageEntry[] GetCurrentIntentionPreviewDamageEntries()
    {
        var skill = GetCurrentIntentionSkill();
        return skill?.GetPreviewHostileDamageEntries() ?? Array.Empty<Skill.PreviewDamageEntry>();
    }

    public Skill.PreviewDamageEntry[] GetCurrentIntentionPreviewDamageEntries(
        bool includeTargetVulnerable
    )
    {
        var skill = GetCurrentIntentionSkill();
        return skill?.GetPreviewHostileDamageEntries(includeTargetVulnerable)
            ?? Array.Empty<Skill.PreviewDamageEntry>();
    }

    public Skill.PreviewEffectEntry[] GetCurrentIntentionPreviewEffectEntries()
    {
        var skill = GetCurrentIntentionSkill();
        return skill?.GetPreviewEffectEntries() ?? Array.Empty<Skill.PreviewEffectEntry>();
    }

    public Character[] GetCurrentIntentionPreviewDebuffTargets()
    {
        var skill = GetCurrentIntentionSkill();
        return skill?
                .GetPreviewHostileDebuffTargets()
                .Where(GodotObject.IsInstanceValid)
                .Distinct()
                .ToArray()
            ?? Array.Empty<Character>();
    }

    public Character[] GetCurrentIntentionPreviewAttackTargets()
    {
        return GetCurrentIntentionPreviewDamageEntries()
            .Select(entry => entry.Target)
            .Where(target =>
                target != null
                && GodotObject.IsInstanceValid(target)
                && target.State == CharacterState.Normal
            )
            .Distinct()
            .ToArray();
    }

    public void ShowIntentionPreviewCurves(
        IReadOnlyList<Character> attackTargets,
        IReadOnlyList<Character> debuffTargets
    )
    {
        var validAttackTargets = (attackTargets ?? Array.Empty<Character>())
            .Where(target =>
                target != null
                && GodotObject.IsInstanceValid(target)
                && target.State == CharacterState.Normal
            )
            .Distinct()
            .ToArray();
        var validDebuffTargets = (debuffTargets ?? Array.Empty<Character>())
            .Where(target =>
                target != null
                && GodotObject.IsInstanceValid(target)
                && target.State == CharacterState.Normal
            )
            .Distinct()
            .ToArray();

        if (validAttackTargets.Length == 0 && validDebuffTargets.Length == 0)
        {
            HideAttackIntentCurve();
            return;
        }

        var activeKeys = new HashSet<string>();
        var attackTargetIds = validAttackTargets.Select(target => target.GetInstanceId()).ToHashSet();
        var debuffTargetIds = validDebuffTargets.Select(target => target.GetInstanceId()).ToHashSet();

        for (int i = 0; i < validAttackTargets.Length; i++)
        {
            Character target = validAttackTargets[i];
            bool hasDebuffLine = debuffTargetIds.Contains(target.GetInstanceId());
            string key = $"attack:{target.GetInstanceId()}";
            activeKeys.Add(key);
            UpdateIntentPreviewLine(
                key,
                BuildIntentPreviewTargetPoint(
                    target,
                    indexInGroup: i,
                    totalInGroup: validAttackTargets.Length,
                    lateralSplit: hasDebuffLine ? -IntentCurveSplitX : 0f,
                    verticalExtraLift: hasDebuffLine ? 0f : 8f
                ),
                AttackIntentCurveColor,
                width: 7f,
                controlOffsetX: hasDebuffLine ? -18f : 0f
            );
        }

        for (int i = 0; i < validDebuffTargets.Length; i++)
        {
            Character target = validDebuffTargets[i];
            bool hasAttackLine = attackTargetIds.Contains(target.GetInstanceId());
            string key = $"debuff:{target.GetInstanceId()}";
            activeKeys.Add(key);
            UpdateIntentPreviewLine(
                key,
                BuildIntentPreviewTargetPoint(
                    target,
                    indexInGroup: i,
                    totalInGroup: validDebuffTargets.Length,
                    lateralSplit: hasAttackLine ? IntentCurveSplitX : 0f,
                    verticalExtraLift: hasAttackLine ? 24f : 18f
                ),
                DebuffIntentCurveColor,
                width: 5f,
                controlOffsetX: hasAttackLine ? 18f : 0f
            );
        }

        foreach (var kv in _intentPreviewLines)
        {
            if (!activeKeys.Contains(kv.Key) && GodotObject.IsInstanceValid(kv.Value))
            {
                kv.Value.Visible = false;
                kv.Value.ClearPoints();
            }
        }
    }

    public void HideAttackIntentCurve()
    {
        foreach (var kv in _intentPreviewLines)
        {
            if (!GodotObject.IsInstanceValid(kv.Value))
                continue;

            kv.Value.Visible = false;
            kv.Value.ClearPoints();
        }
    }

    private Vector2 BuildIntentPreviewTargetPoint(
        Character target,
        int indexInGroup,
        int totalInGroup,
        float lateralSplit,
        float verticalExtraLift
    )
    {
        float centeredIndex = indexInGroup - (totalInGroup - 1) * 0.5f;
        float groupOffsetX = centeredIndex * IntentCurveGroupSpreadX;
        return target.GlobalPosition
            + new Vector2(groupOffsetX + lateralSplit, -IntentCurveTargetLift - verticalExtraLift);
    }

    private void UpdateIntentPreviewLine(
        string key,
        Vector2 targetGlobalPoint,
        Color color,
        float width,
        float controlOffsetX
    )
    {
        Line2D line = GetOrCreateIntentPreviewLine(key);
        Vector2 start = Sprite != null && GodotObject.IsInstanceValid(Sprite) ? Sprite.Position : Vector2.Zero;
        Vector2 end = ToLocal(targetGlobalPoint);
        if (start.DistanceTo(end) <= 4f)
        {
            line.Visible = false;
            line.ClearPoints();
            return;
        }

        Vector2 delta = end - start;
        float arcHeight = Mathf.Clamp(delta.Length() * 0.18f, 80f, 190f);
        Vector2 control = (start + end) * 0.5f + new Vector2(controlOffsetX, -arcHeight);

        line.DefaultColor = color;
        line.Width = width;
        line.ClearPoints();
        const int sampleCount = 18;
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            line.AddPoint(SampleIntentPreviewCurve(start, control, end, t));
        }

        line.Visible = true;
    }

    private Line2D GetOrCreateIntentPreviewLine(string key)
    {
        if (_intentPreviewLines.TryGetValue(key, out Line2D existingLine))
        {
            if (GodotObject.IsInstanceValid(existingLine))
                return existingLine;
        }

        var line = new Line2D
        {
            Name = $"IntentPreviewLine_{key}",
            Visible = false,
            Width = 6f,
            Antialiased = true,
            ZIndex = 30,
            ZAsRelative = false,
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        AddChild(line);
        _intentPreviewLines[key] = line;
        return line;
    }

    private static Vector2 SampleIntentPreviewCurve(
        Vector2 start,
        Vector2 control,
        Vector2 end,
        float t
    )
    {
        Vector2 startToControl = start.Lerp(control, t);
        Vector2 controlToEnd = control.Lerp(end, t);
        return startToControl.Lerp(controlToEnd, t);
    }

    private void ShowIntentionDamageLabels(IReadOnlyList<Skill.PreviewEffectEntry> entries)
    {
        ClearIntentionDamageLabels();
        if (entries == null || entries.Count == 0)
            return;

        var layer = EnsureTipLayer();
        if (layer == null)
            return;

        var groupedEntries = entries
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.State == CharacterState.Normal
            )
            .GroupBy(entry => entry.Target)
            .ToArray();

        for (int i = 0; i < groupedEntries.Length; i++)
        {
            var group = groupedEntries[i];
            var panel = GetOrCreateIntentionDamagePanel(layer, i);
            ulong showStartUsec = Time.GetTicksUsec();
            PreviewEffectDisplay.ShowPanel(
                panel,
                group.ToArray(),
                GetTargetScreenPosition(group.Key),
                IntentionDamageLabelOffset
            );
            BattleNode?.LogHoverPerfWork(this, "enemy-intention-single-label", showStartUsec);
        }
    }

    private void ClearIntentionDamageLabels()
    {
        for (int i = 0; i < _intentionDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_intentionDamagePanels[i]))
                _intentionDamagePanels[i].Visible = false;
        }
    }

    private void FreeIntentionDamageLabels()
    {
        for (int i = 0; i < _intentionDamagePanels.Count; i++)
        {
            if (GodotObject.IsInstanceValid(_intentionDamagePanels[i]))
                _intentionDamagePanels[i].QueueFree();
        }
        _intentionDamagePanels.Clear();
    }

    private VBoxContainer GetOrCreateIntentionDamagePanel(CanvasLayer layer, int index)
    {
        while (_intentionDamagePanels.Count <= index)
        {
            var panel = PreviewEffectDisplay.CreatePanel();
            layer.AddChild(panel);
            _intentionDamagePanels.Add(panel);
        }

        var pooledPanel = _intentionDamagePanels[index];
        if (!GodotObject.IsInstanceValid(pooledPanel))
        {
            pooledPanel = PreviewEffectDisplay.CreatePanel();
            layer.AddChild(pooledPanel);
            _intentionDamagePanels[index] = pooledPanel;
        }
        else if (pooledPanel.GetParent() == null)
        {
            layer.AddChild(pooledPanel);
        }

        return pooledPanel;
    }

    private Label GetOrCreateIntentionDamageSummaryLabel()
    {
        if (GodotObject.IsInstanceValid(_intentionDamageSummaryLabel))
        {
            ConfigureIntentionDamageSummaryLabel(_intentionDamageSummaryLabel);
            return _intentionDamageSummaryLabel;
        }

        _intentionDamageSummaryLabel = new Label
        {
            Name = "DamageSummary",
            Visible = false,
        };
        ConfigureIntentionDamageSummaryLabel(_intentionDamageSummaryLabel);
        IntentionContorl.AddChild(_intentionDamageSummaryLabel);
        return _intentionDamageSummaryLabel;
    }

    private static void ConfigureIntentionDamageSummaryLabel(Label label)
    {
        if (label == null)
            return;

        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.ClipText = false;
        label.ZIndex = 2;
        label.AddThemeFontSizeOverride("font_size", 22);
        label.AddThemeConstantOverride("outline_size", 4);
        label.AddThemeColorOverride("font_color", IntentionDamageColor);
        label.AddThemeColorOverride(
            "font_outline_color",
            IntentionDamageOutlineColor
        );
    }

    private string BuildIntentionDamageSummaryText(Skill skill)
    {
        if (skill == null)
            return string.Empty;

        Skill.PreviewDamageEntry[] entries = skill
            .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.State == CharacterState.Normal
                && entry.Damage > 0
            )
            .ToArray();
        if (entries.Length == 0)
            return string.Empty;

        var uniqueTargets = entries
            .Select(entry => entry.Target)
            .Where(target => target != null && GodotObject.IsInstanceValid(target))
            .Distinct()
            .ToArray();
        if (uniqueTargets.Length == 0)
            return string.Empty;

        string[] damageTexts = entries
            .Select(FormatIntentionDamageEntry)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct()
            .ToArray();
        if (damageTexts.Length == 0)
            return string.Empty;

        string text = string.Join("/", damageTexts);
        string targetSummary = BuildIntentionDamageTargetSummary(skill, uniqueTargets.Length);
        return string.IsNullOrWhiteSpace(targetSummary)
            ? text
            : $"{text}\n{targetSummary}";
    }

    private string BuildIntentionDamageTargetSummary(Skill skill, int targetCount)
    {
        if (skill == null)
            return string.Empty;

        string[] effectSummaries = skill
            .GetPreviewEffectEntries(includeTargetVulnerable: false)
            .Where(entry =>
                entry.Kind == Skill.PreviewEffectKind.Damage
                && entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.State == CharacterState.Normal
                && entry.Value > 0
                && !string.IsNullOrWhiteSpace(entry.TargetSummary)
            )
            .Select(entry => entry.TargetSummary.Trim())
            .Distinct()
            .ToArray();

        if (effectSummaries.Length > 0)
            return string.Join("/", effectSummaries);

        if (targetCount <= 1)
            return string.Empty;

        int allHostileCount =
            BattleNode
                ?.GetOrderedTeamCharacters(!IsPlayer, includeSummons: true, dyingFilter: true)
                .Count(target =>
                    target != null
                    && GodotObject.IsInstanceValid(target)
                    && target.State == CharacterState.Normal
                ) ?? 0;
        return allHostileCount > 0 && targetCount >= allHostileCount ? "全体" : $"{targetCount}个";
    }

    private static string FormatIntentionDamageEntry(Skill.PreviewDamageEntry entry)
    {
        int hitCount = Math.Max(entry.HitCount, 1);
        int damage = Math.Max(entry.Damage, 0);
        string powerMultiplierText = FormatPowerMultiplierText(entry.PowerMultiplier);
        if (hitCount <= 1)
            return $"{damage}{powerMultiplierText}";

        if (damage % hitCount == 0)
            return $"{damage / hitCount}x{hitCount}{powerMultiplierText}";

        return $"{damage}({hitCount}段){powerMultiplierText}";
    }

    private static string FormatPowerMultiplierText(int powerMultiplier)
    {
        return powerMultiplier >= 2 ? $"（{powerMultiplier}倍）" : string.Empty;
    }

    private void ShowDamageLabel(Label label, Skill.PreviewDamageEntry entry, Vector2 targetScreenPosition)
    {
        label.Text =
            entry.HitCount > 1
                ? $"{entry.Damage}({entry.HitCount}次){FormatPowerMultiplierText(entry.PowerMultiplier)}"
                : $"{entry.Damage}{FormatPowerMultiplierText(entry.PowerMultiplier)}";
        label.AddThemeColorOverride("font_color", IntentionDamageColor);
        label.AddThemeColorOverride("font_outline_color", IntentionDamageOutlineColor);
        label.Modulate = Colors.White;
        label.Scale = Vector2.One;
        label.Visible = true;

        Vector2 size = label.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(120f, 44f);
        label.Size = size;

        Vector2 anchor = targetScreenPosition + IntentionDamageLabelOffset;
        label.Position = anchor - size / 2f;
    }

    private CanvasLayer EnsureTipLayer()
    {
        var root = GetTree()?.Root;
        if (root == null)
            return null;

        var existingLayer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (existingLayer != null)
            return existingLayer;

        existingLayer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
        root.AddChild(existingLayer);
        return existingLayer;
    }

    private static string BuildDamagePreviewText(Skill.PreviewDamageEntry entry)
    {
        string damageText =
            $"[font_size=30][color=#ffd7a1][b]{entry.Damage}[/b][/color][/font_size]";
        string hitSuffix =
            entry.HitCount > 1
                ? $"  [color=#d7e6f5][font_size=18]x{entry.HitCount}[/font_size][/color]"
                : string.Empty;
        return $"{damageText}{hitSuffix}";
    }

    private Vector2 GetTargetScreenPosition(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return Vector2.Zero;

        return target.GetGlobalTransformWithCanvas().Origin;
    }
}
