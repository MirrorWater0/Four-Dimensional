using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character, IIntentionPreviewSource
{
    private const float NormalEnemyMaxLifeMultiplier = 1.8f;
    private const float EliteEnemyMaxLifeMultiplier = 1.2f;
    private const float DefaultIntentWeight = 3f;
    private const float ActionStartDelaySeconds = 0.2f;

    public enum EnemyIntentionActionPhase
    {
        PureAttack = 0,
        AttackWithVulnerable = 1,
        NonAttack = 2,
    }

    private const string StunIntentionIconPath = "res://battle/buff/StateIcon/Stun.tscn";
    private static readonly Color IntentionHostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color IntentionFriendlyTargetPreviewColor = new(
        0.48f,
        0.82f,
        0.62f,
        0.82f
    );
    private static readonly Vector2 IntentionDamageLabelOffset = new(-50f, -130f);
    private static readonly Vector2 IntentionDamageSummaryOffset = new(38f, -18f);
    private static readonly Vector2 IntentionDamageSummaryFallbackSize = new(150f, 58f);
    private static readonly Color IntentionDamageColor = new(1f, 0.84f, 0.63f, 1f);
    private static readonly Color IntentionDamageOutlineColor = new(0.02f, 0.03f, 0.06f, 0.95f);

    public EnemyRegedit Registry;
    public Character SourceCharacter => this;
    public Control IntentionControl => IntentionContorl;
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    public ColorRect SurviveIntention => field ??= GetNode<ColorRect>("Intention/Survive");
    public ColorRect SpecialIntention => field ??= GetNode<ColorRect>("Intention/Special");
    public ColorRect StunIntention => field ??= GetOrCreateStunIntention();
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex = -1;
    protected override bool ResolvesTurnStartOnActionStart =>
        BattleNode?.IsResolvingEnemyTeamActionPhase != true;
    private Character[] _intentionPreviewHostileTargets = Array.Empty<Character>();
    private Character[] _intentionPreviewFriendlyTargets = Array.Empty<Character>();
    private int _intentionPreviewHoverDepth;
    private readonly List<VBoxContainer> _intentionDamagePanels = new();
    private Label _intentionDamageSummaryLabel;
    private SkillID[] _openingIntentionSkillIds = Array.Empty<SkillID>();
    private int _openingIntentionStep;
    private Skill _currentOpeningIntentionSkill;
    private bool _preserveCurrentIntentionOnNextRefresh;
    private readonly Dictionary<SkillID, int> _specialIntentionCooldowns = new();
    private Character _lastSingleTargetIntentionLock;

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
        FreeIntentionDamageLabels();
        base._ExitTree();
    }

    public override async Task Dying(Character source = null)
    {
        _intentionPreviewHoverDepth = 0;
        HideIntentionDamageSummary();
        HideIntentionTargetPreview();
        await base.Dying(source);
    }

    public override void Initialize()
    {
        if (Registry != null)
        {
            int effectiveMaxLife = GetEffectiveMaxLife(
                Registry,
                BattleNode?.CurrentLevelNode?.Type
            );
            CharacterName = Registry.CharacterName;
            PassiveName = Registry.PassiveName;
            PassiveDescription = Registry.PassiveDescription;
            SetBaseCombatStatContributions(
                Registry.BasePowerContribution,
                Registry.BaseSurvivabilityContribution
            );
            SetCombatStats(Registry.Power, Registry.Survivability, 0, effectiveMaxLife);
            if (Registry.CurrentLife < 0 || Registry.CurrentLife == Registry.MaxLife)
                Registry.CurrentLife = effectiveMaxLife;
            Skills = (Registry.SkillIDs ?? Array.Empty<SkillID>())
                .Select(Skill.GetSkill)
                .Where(x => x != null)
                .ToArray();
            if (Skills.Length == 0)
                Skills = new Skill[3];

            _openingIntentionSkillIds = Registry.OpeningIntentionSkillIDs ?? Array.Empty<SkillID>();
            _openingIntentionStep = 0;
            _currentOpeningIntentionSkill = null;
            _specialIntentionCooldowns.Clear();
            _lastSingleTargetIntentionLock = null;
        }
        base.Initialize();
        if (Registry != null)
        {
            Life = Math.Clamp(Registry.CurrentLife, 0, BattleMaxLife);
            SyncLifeBarsToCurrent(syncBufferValue: true);
        }
        if (SpeedIconLabel?.GetParent() is CanvasItem speedIcon)
            speedIcon.Visible = false;
    }

    public static bool UsesNormalEnemyLifeBonus(LevelNode.LevelType? levelType) =>
        levelType != LevelNode.LevelType.Elite && levelType != LevelNode.LevelType.Boss;

    public static int GetEffectiveMaxLife(EnemyRegedit regedit, LevelNode.LevelType? levelType)
    {
        if (regedit == null)
            return 0;

        int maxLife = Math.Max(1, regedit.MaxLife);
        if (levelType == LevelNode.LevelType.Elite)
            return Math.Max(1, (int)MathF.Ceiling(maxLife * EliteEnemyMaxLifeMultiplier));

        if (!UsesNormalEnemyLifeBonus(levelType))
            return maxLife;

        return Math.Max(1, (int)MathF.Ceiling(maxLife * NormalEnemyMaxLifeMultiplier));
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
        await ToSignal(GetTree().CreateTimer(ActionStartDelaySeconds), "timeout");
        base.StartAction();
        bool hadActiveStun = HasActiveStun();
        if (hadActiveStun)
            _preserveCurrentIntentionOnNextRefresh = true;

        Skill skill = CurrentIntentionSkill;
        if (!hadActiveStun && !CanExecuteIntentionSkill(skill))
        {
            IntentionIndex = RollIntentionIndex();
            skill = CurrentIntentionSkill;
            LockCurrentIntentionTargets(skill, applyRepeatSingleTargetAvoidance: true);
        }

        await DisappearIntention();
        if (skill != null && (hadActiveStun || CanExecuteIntentionSkill(skill)))
        {
            using (skill.BeginEnergyCostWaiver())
            {
                await skill.Effect();
            }
        }

        EndAction();
    }

    public override void OnTurnEnd()
    {
        if (BattleNode?.IsDeferringEnemyIntentionRefresh != true)
        {
            RefreshNextIntentionAfterAction();
        }

        base.OnTurnEnd();
    }

    public void RefreshNextIntentionAfterAction()
    {
        if (_preserveCurrentIntentionOnNextRefresh)
        {
            _preserveCurrentIntentionOnNextRefresh = false;
            DisplayIntention();
            return;
        }

        AdvanceOpeningIntentionStep();
        AdvanceSpecialIntentionCooldowns();
        IntentionIndex = RollIntentionIndex();
        DisplayIntention();
    }

    public int RollIntentionIndex()
    {
        ClearIntentionSkillPreviewState();
        _currentOpeningIntentionSkill = null;

        if (TryCreateOpeningIntentionSkill(out Skill openingSkill))
        {
            _currentOpeningIntentionSkill = openingSkill;
            RecordSelectedSpecialIntentionCooldown(openingSkill);
            return -1;
        }

        if (Skills == null || Skills.Length == 0)
            return -1;

        int avoidIndex = GetRepeatIntentionAvoidIndex();
        float totalWeight = 0f;
        float[] weights = new float[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            float weight = i == avoidIndex ? 0f : GetIntentionWeight(Skills[i]);
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                if (CanSelectIntentionSkill(Skills[i]))
                    return RecordAndReturnIntentionIndex(i);
            }
            return -1;
        }

        float roll = (float)BattleNode.BattleIntentionRandom.NextDouble() * totalWeight;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return RecordAndReturnIntentionIndex(i);
        }

        for (int i = weights.Length - 1; i >= 0; i--)
        {
            if (weights[i] > 0f)
                return RecordAndReturnIntentionIndex(i);
        }

        return -1;
    }

    private void ClearIntentionSkillPreviewState()
    {
        foreach (var skill in Skills ?? Array.Empty<Skill>())
        {
            skill?.ClearPreviewableRandomHostileTargets();
            skill?.ClearLockedExecutionTargets();
        }

        _currentOpeningIntentionSkill?.ClearPreviewableRandomHostileTargets();
        _currentOpeningIntentionSkill?.ClearLockedExecutionTargets();
    }

    private bool TryCreateOpeningIntentionSkill(out Skill openingSkill)
    {
        openingSkill = null;
        if (
            _openingIntentionSkillIds == null
            || _openingIntentionStep < 0
            || _openingIntentionStep >= _openingIntentionSkillIds.Length
        )
        {
            return false;
        }

        SkillID openingSkillId = _openingIntentionSkillIds[_openingIntentionStep];
        Skill skill = Skill.GetSkill(openingSkillId);
        if (!CanExecuteIntentionSkill(skill))
            return false;

        openingSkill = skill;
        return true;
    }

    private void AdvanceOpeningIntentionStep()
    {
        if (
            _openingIntentionSkillIds == null
            || _openingIntentionSkillIds.Length == 0
            || _openingIntentionStep >= _openingIntentionSkillIds.Length
        )
        {
            return;
        }

        _openingIntentionStep++;
    }

    private int GetRepeatIntentionAvoidIndex()
    {
        if (IntentionIndex < 0 || IntentionIndex >= (Skills?.Length ?? 0))
            return -1;

        if (!CanSelectIntentionSkill(Skills[IntentionIndex]))
            return -1;

        for (int i = 0; i < Skills.Length; i++)
        {
            if (i != IntentionIndex && CanSelectIntentionSkill(Skills[i]))
                return IntentionIndex;
        }

        return -1;
    }

    private float GetIntentionWeight(Skill skill)
    {
        if (!CanSelectIntentionSkill(skill))
            return 0f;

        return DefaultIntentWeight;
    }

    private bool CanSelectIntentionSkill(Skill skill)
    {
        return CanExecuteIntentionSkill(skill) && !IsSpecialIntentionOnCooldown(skill);
    }

    private bool CanExecuteIntentionSkill(Skill skill)
    {
        if (skill == null)
            return false;

        skill.OwnerCharater = this;
        return skill.CanBePlayed && State != CharacterState.Dying;
    }

    private int RecordAndReturnIntentionIndex(int index)
    {
        if (index >= 0 && index < (Skills?.Length ?? 0))
            RecordSelectedSpecialIntentionCooldown(Skills[index]);

        return index;
    }

    private bool IsSpecialIntentionOnCooldown(Skill skill)
    {
        if (skill?.SkillType != Skill.SkillTypes.Special || !skill.SkillId.HasValue)
            return false;

        return _specialIntentionCooldowns.TryGetValue(skill.SkillId.Value, out int remaining)
            && remaining > 0;
    }

    private void RecordSelectedSpecialIntentionCooldown(Skill skill)
    {
        if (skill?.SkillType != Skill.SkillTypes.Special || !skill.SkillId.HasValue)
            return;

        int cooldown = Math.Max(0, skill.EnemySpecialIntentionCooldown);
        if (cooldown <= 0)
        {
            _specialIntentionCooldowns.Remove(skill.SkillId.Value);
            return;
        }

        _specialIntentionCooldowns[skill.SkillId.Value] = cooldown + 1;
    }

    private void AdvanceSpecialIntentionCooldowns()
    {
        if (_specialIntentionCooldowns.Count == 0)
            return;

        foreach (SkillID skillId in _specialIntentionCooldowns.Keys.ToArray())
        {
            int remaining = _specialIntentionCooldowns[skillId] - 1;
            if (remaining <= 0)
                _specialIntentionCooldowns.Remove(skillId);
            else
                _specialIntentionCooldowns[skillId] = remaining;
        }
    }

    public bool HasActiveStun()
    {
        return SkillBuffs?.Any(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Stun && buff.Stack > 0
            ) == true;
    }

    private ColorRect GetOrCreateStunIntention()
    {
        var existing = IntentionContorl.GetNodeOrNull<ColorRect>("Stun");
        if (existing != null)
            return existing;

        PackedScene scene = GD.Load<PackedScene>(StunIntentionIconPath);
        var stun = scene?.Instantiate<ColorRect>();
        if (stun == null)
        {
            stun = new ColorRect { Color = new Color(0.94f, 0.87f, 0.13f, 1f) };
        }

        stun.Name = "Stun";
        stun.Visible = false;
        stun.MouseFilter = Control.MouseFilterEnum.Ignore;
        stun.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        stun.CustomMinimumSize = Vector2.Zero;
        stun.Position = new Vector2(-30f, -30f);
        stun.Size = new Vector2(60f, 60f);
        if (stun.GetChildOrNull<Label>(0) is Label label)
            label.Visible = false;

        IntentionContorl.AddChild(stun);
        return stun;
    }

    public async Task DisappearIntention()
    {
        HideIntentionTargetPreview();
        HideIntentionDamageSummary();
        Buff.GhostExplode(
            IntentionContorl,
            new Vector2(2, 2),
            useOffsetMotion: false,
            removeFirstChild: false
        );
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
        StunIntention.Visible = false;
    }

    public void DisplayIntention()
    {
        var skill = CurrentIntentionSkill;
        if (!ApplyCurrentIntentionIconState(skill, applyRepeatSingleTargetAvoidance: true))
        {
            return;
        }

        IntentionContorl.Modulate = new Color(1, 1, 1, 0);
        IntentionContorl.Scale = new Vector2(1.8f, 1.8f);
        Tween tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(IntentionContorl, "modulate", new Color(1, 1, 1, 1), 0.2f);
        tween
            .TweenProperty(IntentionContorl, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    public void RefreshIntentionDisplayForCurrentState()
    {
        if (State == CharacterState.Dying)
            return;

        if (!ApplyCurrentIntentionIconState(CurrentIntentionSkill, applyRepeatSingleTargetAvoidance: false))
            return;

        IntentionContorl.Modulate = Colors.White;
        IntentionContorl.Scale = Vector2.One;
    }

    private bool ApplyCurrentIntentionIconState(
        Skill skill,
        bool applyRepeatSingleTargetAvoidance
    )
    {
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
        StunIntention.Visible = false;

        bool hasActiveStun = HasActiveStun();
        if (skill == null && !hasActiveStun)
        {
            HideIntentionDamageSummary();
            IntentionContorl.Visible = false;
            return false;
        }

        IntentionContorl.Visible = true;
        if (hasActiveStun)
        {
            StunIntention.Visible = true;
            HideIntentionDamageSummary();
            HideIntentionTargetPreview();
            return true;
        }

        LockCurrentIntentionTargets(skill, applyRepeatSingleTargetAvoidance);
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

        RefreshIntentionDamageSummary();
        return true;
    }

    public void RefreshIntentionDamageSummary()
    {
        if (State == CharacterState.Dying || HasActiveStun())
        {
            HideIntentionDamageSummary();
            return;
        }

        InvalidateSkillTooltipCache();

        var skill = CurrentIntentionSkill;
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

    private void LockCurrentIntentionTargets(Skill skill, bool applyRepeatSingleTargetAvoidance)
    {
        if (skill == null)
            return;

        skill.OwnerCharater = this;
        if (applyRepeatSingleTargetAvoidance)
            skill.SetAvoidRepeatSingleTargetIntentionLock(_lastSingleTargetIntentionLock);
        try
        {
            skill.LockPreviewTargetsForExecution();
            if (applyRepeatSingleTargetAvoidance)
                RecordSingleTargetIntentionLock(skill);
        }
        finally
        {
            skill.ClearAvoidRepeatSingleTargetIntentionLock();
        }
    }

    private void RecordSingleTargetIntentionLock(Skill skill)
    {
        if (skill == null || HasActiveStun())
            return;

        Character[] uniqueTargets = skill
            .GetPreviewHostileDamageEntries(includeTargetVulnerable: false)
            .Where(entry =>
                entry.Target != null
                && GodotObject.IsInstanceValid(entry.Target)
                && entry.Target.State == CharacterState.Normal
                && entry.Damage > 0
            )
            .Select(entry => entry.Target)
            .Distinct()
            .ToArray();

        if (uniqueTargets.Length == 1)
            _lastSingleTargetIntentionLock = uniqueTargets[0];
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
        if (State == CharacterState.Dying || HasActiveStun())
            return;

        var skill = CurrentIntentionSkill;
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
            _intentionPreviewHostileTargets[i]
                .ShowTargetPreview(IntentionHostileTargetPreviewColor);
        }
        for (int i = 0; i < _intentionPreviewFriendlyTargets.Length; i++)
        {
            _intentionPreviewFriendlyTargets[i]
                .ShowTargetPreview(IntentionFriendlyTargetPreviewColor);
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

    public Skill CurrentIntentionSkill
    {
        get
        {
            if (_currentOpeningIntentionSkill != null)
                return _currentOpeningIntentionSkill;

            if (Skills == null || IntentionIndex < 0 || IntentionIndex >= Skills.Length)
                return null;

            return Skills[IntentionIndex];
        }
    }

    public EnemyIntentionActionPhase GetCurrentIntentionActionPhase()
    {
        if (HasActiveStun())
            return EnemyIntentionActionPhase.NonAttack;

        Skill skill = CurrentIntentionSkill;
        if (skill == null)
            return EnemyIntentionActionPhase.NonAttack;

        skill.OwnerCharater = this;
        var entries = skill.GetPreviewEffectEntries(includeTargetVulnerable: false);
        bool hasDamage = entries.Any(entry => entry.Kind == Skill.PreviewEffectKind.Damage);
        if (!hasDamage)
            return EnemyIntentionActionPhase.NonAttack;

        bool appliesVulnerable = entries.Any(entry =>
            entry.Kind == Skill.PreviewEffectKind.Buff && entry.BuffName == Buff.BuffName.Vulnerable
        );
        return appliesVulnerable
            ? EnemyIntentionActionPhase.AttackWithVulnerable
            : EnemyIntentionActionPhase.PureAttack;
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

        _intentionDamageSummaryLabel = new Label { Name = "DamageSummary", Visible = false };
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
        label.AddThemeColorOverride("font_outline_color", IntentionDamageOutlineColor);
    }

    private string BuildIntentionDamageSummaryText(Skill skill)
    {
        if (skill == null)
            return string.Empty;

        Skill.PreviewDamageEntry[] entries = skill
            .GetPreviewHostileDamageEntries(includeTargetVulnerable: true)
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
        string targetSummary = BuildIntentionDamageTargetSummary(skill, uniqueTargets);
        return string.IsNullOrWhiteSpace(targetSummary) ? text : $"{text}\n{targetSummary}";
    }

    private string BuildIntentionDamageTargetSummary(Skill skill, Character[] uniqueTargets)
    {
        if (skill == null)
            return string.Empty;

        uniqueTargets =
            uniqueTargets
                ?.Where(target =>
                    target != null
                    && GodotObject.IsInstanceValid(target)
                    && target.State == CharacterState.Normal
                )
                .Distinct()
                .ToArray() ?? Array.Empty<Character>();
        int targetCount = uniqueTargets.Length;
        if (targetCount == 0)
            return string.Empty;

        int allHostileCount =
            BattleNode
                ?.GetOrderedTeamCharacters(!IsPlayer, includeSummons: true, dyingFilter: true)
                .Count(target =>
                    target != null
                    && GodotObject.IsInstanceValid(target)
                    && target.State == CharacterState.Normal
                ) ?? 0;
        if (allHostileCount > 0 && targetCount >= allHostileCount)
            return I18n.Tr("skill.preview.all_formation", "全阵");

        UserSettings.EnsureLoaded();
        if (UserSettings.ShowIntentionTargetNames)
        {
            string[] targetNames = uniqueTargets
                .OrderBy(target => target.PositionIndex)
                .Select(GetIntentionTargetDisplayName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToArray();
            if (targetNames.Length > 0)
                return string.Join("/", targetNames);
        }

        string[] effectSummaries = skill
            .GetPreviewEffectEntries(includeTargetVulnerable: true)
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

        return $"{targetCount}个";
    }

    private static string GetIntentionTargetDisplayName(Character target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target))
            return string.Empty;

        return string.IsNullOrWhiteSpace(target.CharacterName) ? target.Name : target.CharacterName;
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

    private void ShowDamageLabel(
        Label label,
        Skill.PreviewDamageEntry entry,
        Vector2 targetScreenPosition
    )
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
