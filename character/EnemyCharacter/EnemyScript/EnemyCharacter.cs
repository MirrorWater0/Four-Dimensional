using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class EnemyCharacter : Character, IIntentionPreviewSource
{
    private const float DefaultIntentWeight = 3f;
    private const float ActionStartDelaySeconds = 0.2f;

    public enum EnemyIntentionActionPhase
    {
        PureAttack = 0,
        AttackWithVulnerable = 1,
        NonAttack = 2,
    }

    private static readonly Color IntentionHostileTargetPreviewColor = new(1f, 0.32f, 0.32f, 1f);
    private static readonly Color IntentionFriendlyTargetPreviewColor = new(
        0.48f,
        0.82f,
        0.62f,
        0.82f
    );

    public EnemyRegedit Registry;
    public Character SourceCharacter => this;
    public Control IntentionControl => IntentionContorl;
    public Control IntentionContorl => field ??= GetNode<Control>("Intention");
    public ColorRect AttackIntention => field ??= GetNode<ColorRect>("Intention/Attack");
    public ColorRect SurviveIntention => field ??= GetNode<ColorRect>("Intention/Survive");
    public ColorRect SpecialIntention => field ??= GetNode<ColorRect>("Intention/Special");
    public ColorRect StunIntention => field ??= GetNode<ColorRect>("Intention/Stun");
    private ProgressBar _lifebar;
    public Battle Battle => field ??= GetNode("/root/Battle") as Battle;
    Label label => field ??= GetNode<Label>("Label");
    public int IntentionIndex = -1;
    public bool IntentionCardsVisible { get; private set; }
    private bool UsesIntentionCards => !IsPlayer && BattleNode?.ActionCardsUi != null;
    private bool _intentionCardHiddenUntilNextDisplay;
    protected override bool ResolvesTurnStartOnActionStart =>
        BattleNode?.IsResolvingEnemyTeamActionPhase != true;
    private Character[] _intentionPreviewHostileTargets = Array.Empty<Character>();
    private Character[] _intentionPreviewFriendlyTargets = Array.Empty<Character>();
    private int _intentionPreviewHoverDepth;
    private readonly List<VBoxContainer> _intentionDamagePanels = new();
    private Label _intentionDamageSummaryLabel;
    private EnemyIntentionStep[] _intentionCycle = Array.Empty<EnemyIntentionStep>();
    private int _intentionCycleStep;
    private (SkillID SkillId, int Turn)[] _intentionCycleInsertions =
        Array.Empty<(SkillID SkillId, int Turn)>();
    private int _intentionTurn = 1;
    private int _nextIntentionInsertion;
    private bool _isInsertedIntention;
    private int[] _randomIntentionIndices;
    private bool _preserveCurrentIntentionOnNextRefresh;
    private Tween _intentionDisplayTween;
    private int _tuningMaxLife;
    private int _tuningPower;
    private int _tuningSurvivability;
    private int _tuningBasePower;
    private int _tuningBaseSurvivability;

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
        bool wasIntentionVisible = IntentionCardsVisible;
        _intentionPreviewHoverDepth = 0;
        HideIntentionDamageSummary();
        HideIntentionTargetPreview();
        IntentionCardsVisible = false;
        BattleNode?.ActionCardsUi?.RefreshIntention(this);
        await base.Dying(source);
        if (State == CharacterState.Normal && wasIntentionVisible)
        {
            IntentionCardsVisible = true;
            BattleNode?.ActionCardsUi?.RefreshIntention(this, refreshText: true);
        }
    }

    public override void Initialize()
    {
        if (Registry != null)
        {
            int maxLife = GetEffectiveMaxLife(Registry, null);
            CharacterName = Registry.CharacterName;
            PassiveName = Registry.PassiveName;
            PassiveDescription = Registry.PassiveDescription;
            SetBaseCombatStatContributions(
                Registry.BasePowerContribution,
                Registry.BaseSurvivabilityContribution
            );
            SetCombatStats(Registry.Power, Registry.Survivability, maxLife);
            CaptureTuningStats();
            if (Registry.CurrentLife < 0 || Registry.CurrentLife == Registry.MaxLife)
                Registry.CurrentLife = maxLife;
            Skills = Registry.GetAllSkillIds()
                .Select(Skill.GetSkill)
                .Where(x => x != null)
                .ToArray();
            if (Skills.Length == 0)
                Skills = new Skill[3];

            _intentionCycle = Registry.IntentionCycle ?? Array.Empty<EnemyIntentionStep>();
            _intentionCycleStep = Registry.RandomizeIntentionCycleStart && _intentionCycle.Length > 1
                ? (BattleNode?.GetEnemyIntentionRandom(this) ?? Random.Shared).Next(_intentionCycle.Length)
                : 0;
            _intentionCycleInsertions = Registry.GetIntentionCycleInsertions();
            _intentionTurn = 1;
            _nextIntentionInsertion = 0;
            _isInsertedIntention = false;
            // Skills also contains fixed-only and insertion-only skills for buffs, descriptions and tuning refresh.
            // Only these indices participate in random selection.
            var randomIds = new HashSet<SkillID>(Registry.SkillIDs ?? Array.Empty<SkillID>());
            _randomIntentionIndices = Enumerable.Range(0, Skills.Length)
                .Where(index => Skills[index]?.SkillId is SkillID id && randomIds.Contains(id))
                .ToArray();
            IntentionIndex = -1;
            _preserveCurrentIntentionOnNextRefresh = false;
        }
        base.Initialize();
        ConfigureCharacterShadow();
        if (Registry != null)
        {
            Life = Math.Clamp(Registry.CurrentLife, 0, BattleMaxLife);
            SyncLifeBarsToCurrent(syncBufferValue: true);
        }
    }

    private void ConfigureCharacterShadow()
    {
        if (CharacterShadow == null || !GodotObject.IsInstanceValid(CharacterShadow))
            return;

        // Preserve the projection that BattleAtmosphere drew for enemies and
        // summons before it moved into the shared character scene.
        if (this is SummonCharacter summon && summon.Summoner?.CharacterShadow is { } ownerShadow
            && GodotObject.IsInstanceValid(ownerShadow))
        {
            CharacterShadow.ApplyCharacterColor(ownerShadow.FillColor);
        }
        else if (BattleNode?.GetNodeOrNull<BattleAtmosphere>("bg/BattleAtmosphere") is { } atmosphere)
        {
            Color tint = BattleNode.CurrentLevelNode?.Type switch
            {
                LevelNode.LevelType.Elite => atmosphere.EliteTint,
                LevelNode.LevelType.Boss => atmosphere.BossTint,
                _ => atmosphere.NormalTint,
            };
            CharacterShadow.ApplyCharacterColor(tint);
        }

        CharacterShadow.Visible = true;
        CharacterShadow.SetCardHoverHighlight(false, instant: true);
    }

    public static int GetEffectiveMaxLife(EnemyRegedit regedit, LevelNode.LevelType? levelType)
    {
        if (regedit == null)
            return 0;

        return Math.Max(1, regedit.MaxLife);
    }

    public virtual bool RefreshTuning()
    {
        if (Registry == null)
            return false;

        // Keep battle gains/losses and missing life when the underlying definition changes.
        int newMaxLife = Math.Max(1, BattleMaxLife + Registry.MaxLife - _tuningMaxLife);
        int missingLife = Math.Max(0, BattleMaxLife - Life);
        bool wasDying = State == CharacterState.Dying || Life <= 0;
        SetCombatStats(
            BattlePower + Registry.Power - _tuningPower,
            BattleSurvivability + Registry.Survivability - _tuningSurvivability,
            newMaxLife
        );
        SetBaseCombatStatContributions(
            BasePowerContribution + Registry.BasePowerContribution - _tuningBasePower,
            BaseSurvivabilityContribution + Registry.BaseSurvivabilityContribution - _tuningBaseSurvivability
        );
        CaptureTuningStats();
        var definition = EnemyCatalog.Create(Registry.TuningKey);
        Registry.PassiveName = definition.PassiveName;
        Registry.PassiveDescription = definition.PassiveDescription;
        PassiveName = Registry.PassiveName;
        PassiveDescription = Registry.PassiveDescription;
        Registry.CurrentLife = wasDying ? 0 : Math.Clamp(newMaxLife - missingLife, 1, newMaxLife);
        Life = Registry.CurrentLife;
        SyncLifeBarsToCurrent(syncBufferValue: true);
        RefreshCombatStatDisplay();
        return true;
    }

    private void CaptureTuningStats() {
        _tuningMaxLife = Registry.MaxLife;
        _tuningPower = Registry.Power;
        _tuningSurvivability = Registry.Survivability;
        _tuningBasePower = Registry.BasePowerContribution;
        _tuningBaseSurvivability = Registry.BaseSurvivabilityContribution;
    }

    internal void TrackTuningStatMultiplier(float multiplier) {
        if (Registry == null)
            return;
        Registry.ApplyStatMultiplier(multiplier, multiplier);
        CaptureTuningStats();
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
            LockCurrentIntentionTargets(skill);
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
        _intentionTurn++;
        if (_preserveCurrentIntentionOnNextRefresh)
        {
            _preserveCurrentIntentionOnNextRefresh = false;
            DisplayIntention();
            return;
        }

        if (_isInsertedIntention)
        {
            // Consume an insertion only when its action is settled; rerolls and stun cannot consume it.
            _nextIntentionInsertion++;
            _isInsertedIntention = false;
        }
        else
        {
            AdvanceIntentionCycleStep();
        }
        IntentionIndex = RollIntentionIndex();
        DisplayIntention();
    }

    public int RollIntentionIndex()
    {
        ClearIntentionSkillPreviewState();
        if (_nextIntentionInsertion < _intentionCycleInsertions.Length
            && _intentionCycleInsertions[_nextIntentionInsertion].Turn <= _intentionTurn)
        {
            // Overdue entries wait behind a stunned intention or earlier entries scheduled for the same turn.
            _isInsertedIntention = true;
            return RollFixedIntentionIndex(_intentionCycleInsertions[_nextIntentionInsertion].SkillId);
        }
        if (_intentionCycle.Length > 0)
        {
            var step = _intentionCycle[_intentionCycleStep];
            if (!step.IsRandom)
            {
                // Fixed steps follow their configured order.
                return RollFixedIntentionIndex(step.SkillId.Value);
            }
        }

        return RollRandomIntentionIndex();
    }

    private int RollFixedIntentionIndex(SkillID skillId)
    {
        int index = Array.FindIndex(Skills ?? Array.Empty<Skill>(),
            skill => skill?.SkillId == skillId);
        return index >= 0 && CanExecuteIntentionSkill(Skills[index])
            ? index : -1;
    }

    private int RollRandomIntentionIndex()
    {
        if (Skills == null || Skills.Length == 0)
            return -1;

        // Summons initialized without a registry use their directly assigned skills.
        int[] pool = _randomIntentionIndices ?? Enumerable.Range(0, Skills.Length).ToArray();
        int avoidIndex = GetRepeatIntentionAvoidIndex();
        float totalWeight = 0f;
        float[] weights = new float[Skills.Length];
        foreach (int i in pool)
        {
            float weight = i == avoidIndex ? 0f : GetIntentionWeight(Skills[i]);
            weights[i] = weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
        {
            foreach (int i in pool)
            {
                if (CanExecuteIntentionSkill(Skills[i]))
                    return i;
            }
            return -1;
        }

        float roll = (float)BattleNode.GetEnemyIntentionRandom(this).NextDouble() * totalWeight;
        foreach (int i in pool)
        {
            if (weights[i] <= 0f)
                continue;
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

    private void ClearIntentionSkillPreviewState()
    {
        foreach (var skill in Skills ?? Array.Empty<Skill>())
        {
            skill?.ClearPreviewableRandomHostileTargets();
            skill?.ClearLockedExecutionTargets();
        }
    }

    private void AdvanceIntentionCycleStep()
    {
        if (_intentionCycle.Length == 0)
            return;

        _intentionCycleStep = (_intentionCycleStep + 1) % _intentionCycle.Length;
    }

    private int GetRepeatIntentionAvoidIndex()
    {
        if (IntentionIndex < 0 || IntentionIndex >= (Skills?.Length ?? 0))
            return -1;

        if (!CanExecuteIntentionSkill(Skills[IntentionIndex]))
            return -1;

        foreach (int i in _randomIntentionIndices ?? Enumerable.Range(0, Skills.Length).ToArray())
        {
            if (i != IntentionIndex && CanExecuteIntentionSkill(Skills[i]))
                return IntentionIndex;
        }

        return -1;
    }

    private float GetIntentionWeight(Skill skill)
    {
        if (!CanExecuteIntentionSkill(skill))
            return 0f;

        return DefaultIntentWeight;
    }

    private bool CanExecuteIntentionSkill(Skill skill)
    {
        if (skill == null)
            return false;

        skill.OwnerCharater = this;
        return skill.CanBePlayed && State != CharacterState.Dying;
    }

    public bool HasActiveStun()
    {
        return SkillBuffs?.Any(buff =>
                buff != null && buff.ThisBuffName == Buff.BuffName.Stun && buff.Stack > 0
            ) == true;
    }

    public async Task DisappearIntention()
    {
        HideIntentionTargetPreview();
        HideIntentionDamageSummary();
        BattleNode?.ActionCardsUi?.PulseIntention(this);
        Buff.GhostExplode(
            IntentionContorl,
            new Vector2(2, 2),
            useOffsetMotion: false,
            removeFirstChild: false
        );
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        ResetIntentionDisplayImmediate();
    }

    public void ResetIntentionDisplayImmediate()
    {
        _intentionCardHiddenUntilNextDisplay = true;
        IntentionCardsVisible = false;
        BattleNode?.ActionCardsUi?.RefreshIntention(this);
        HideIntentionTargetPreview();
        HideIntentionDamageSummary();
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
        StunIntention.Visible = false;
    }

    public void DisplayIntention()
    {
        _intentionCardHiddenUntilNextDisplay = false;
        var skill = CurrentIntentionSkill;
        if (!ApplyCurrentIntentionIconState(skill))
        {
            return;
        }

        IntentionContorl.Modulate = new Color(1, 1, 1, 0);
        IntentionContorl.Scale = new Vector2(1.8f, 1.8f);
        _intentionDisplayTween?.Kill();
        _intentionDisplayTween = CreateTween();
        _intentionDisplayTween.SetParallel(true);
        _intentionDisplayTween
            .TweenProperty(IntentionContorl, "modulate", new Color(1, 1, 1, 1), 0.2f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _intentionDisplayTween
            .TweenProperty(IntentionContorl, "scale", new Vector2(1f, 1f), 0.2f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    public void RefreshIntentionDisplayForCurrentState()
    {
        if (State == CharacterState.Dying)
            return;
        if (_intentionCardHiddenUntilNextDisplay)
            return;

        if (
            !ApplyCurrentIntentionIconState(
                CurrentIntentionSkill,
                relockTargets: false
            )
        )
            return;

        IntentionContorl.Modulate = Colors.White;
        IntentionContorl.Scale = Vector2.One;
    }

    private bool ApplyCurrentIntentionIconState(
        Skill skill,
        bool relockTargets = true
    )
    {
        AttackIntention.Visible = false;
        SurviveIntention.Visible = false;
        SpecialIntention.Visible = false;
        StunIntention.Visible = false;

        bool hasActiveStun = HasActiveStun();
        IntentionCardsVisible = skill != null || hasActiveStun;
        if (skill == null && !hasActiveStun)
        {
            BattleNode?.ActionCardsUi?.RefreshIntention(this);
            HideIntentionDamageSummary();
            IntentionContorl.Visible = false;
            return false;
        }

        IntentionContorl.Visible = true;
        if (hasActiveStun)
        {
            StunIntention.Visible = true;
            if (UsesIntentionCards)
                BattleNode.ActionCardsUi.RefreshIntention(this);
            HideIntentionDamageSummary();
            HideIntentionTargetPreview();
            return true;
        }

        if (relockTargets)
            LockCurrentIntentionTargets(skill);
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
        if (UsesIntentionCards)
        {
            BattleNode.ActionCardsUi.RefreshIntention(this, refreshText: true);
        }
        if (State == CharacterState.Dying || HasActiveStun() || _intentionCardHiddenUntilNextDisplay)
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

        label.Size = label.GetCombinedMinimumSize();

        if (_intentionPreviewHoverDepth > 0)
            ShowIntentionTargetPreview();
    }

    private void HideIntentionDamageSummary()
    {
        if (GodotObject.IsInstanceValid(_intentionDamageSummaryLabel))
            _intentionDamageSummaryLabel.Visible = false;
    }

    private void LockCurrentIntentionTargets(Skill skill)
    {
        if (skill == null)
            return;

        skill.OwnerCharater = this;
        skill.LockPreviewTargetsForExecution();
    }

    private void OnIntentionPreviewHoverEntered()
    {
        if (State == CharacterState.Dying || BattleNode?.CharacterControl?.IsManualTargetArrowSelectionActive == true)
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
        if (State == CharacterState.Dying || HasActiveStun()
            || BattleNode?.CharacterControl?.IsManualTargetArrowSelectionActive == true)
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

    internal override void SuppressHoverTooltipsForTargetSelection()
    {
        base.SuppressHoverTooltipsForTargetSelection();
        _intentionPreviewHoverDepth = 0;
        HideIntentionTargetPreview();
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
            PreviewEffectDisplay.ShowTargetEffectPanel(
                panel,
                group.ToArray(),
                GetTargetScreenPosition(group.Key)
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
        return _intentionDamageSummaryLabel ??= IntentionContorl.GetNode<Label>("DamageSummary");
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
