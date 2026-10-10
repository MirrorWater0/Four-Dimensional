using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class Character : Node2D
{
    public enum DamageKind
    {
        Other,
        Attack,
    }

    private const ulong IncreasePropertyEffectCooldownMsec = 300;
    private const int SkillTooltipDelayMs = 120;
    private const float BlockDisplayFadeDuration = 0.2f;
    private const float BlockDisplayShowScaleFactor = 1.8f;
    private const float BlockGhostExplodeAlphaScale = 0.45f;
    private const double LifeBarDamageBufferHoldDuration = 0.7;
    private const double LifeBarDamageBufferCatchupDuration = 0.4;
    private static readonly Vector2 BlockGhostExplodeScale = new(1.35f, 1.35f);
    private static readonly Color NormalSpriteModulate = new(1f, 1f, 1f, 1f);
    private static readonly Color InvisibleSpriteModulate = new(0.8f, 0.8f, 1f, 0.95f);
    private static readonly Color BlockedLifeBarFillColor = Color.FromHtml("#70d2ff");
    private static readonly Color BlockedBufferBarFillColor = Color.FromHtml("#9EDBFFF2");
    private static readonly PackedScene TooltipScene = ResourceLoader.Load<PackedScene>(
        "res://battle/UIScene/Tip.tscn"
    );

    [Export]
    public bool WarmupMode { get; set; }

    public enum CharacterState
    {
        Normal,
        Dying,
    }

    public virtual PackedScene CharaterScene { set; get; }

    private CharacterState _state = CharacterState.Normal;
    public CharacterState State
    {
        get => _state;
        set
        {
            bool changed = _state != value;
            _state = value;
            if (_state == CharacterState.Dying)
            {
                AttackCountBuff.ClearTemporary(this);
                BattleNode?.ClearNextActionPreviewCharacter(this);
            }
            if (changed)
                BattleNode?.RefreshTurnOrderPreview();
        }
    }
    public GridContainer StateIconContainer => field ??= GetNode<GridContainer>("State");

    //charater basic properties
    [Export]
    public Texture2D Portrait;
    private string _characterName;
    protected virtual string CharacterNameKey =>
        $"character.{I18n.ToSnakeCase(GetType().Name)}.name";
    public virtual string CharacterName
    {
        get => I18n.Tr(CharacterNameKey, _characterName);
        set => _characterName = value;
    }

    [Export]
    public int BattleMaxLife { get; private set; }
    public int Life { get; set; }

    public int BattlePower { get; private set; }

    public int BattleSurvivability { get; private set; }

    public int BasePowerContribution { get; private set; }

    public int BaseSurvivabilityContribution { get; private set; }

    public int Block { get; protected set; }
    public int CurrentEnergy => IsPlayer ? BattleNode?.PlayerEnergy ?? 0 : 0;

    private string _passiveName;
    protected virtual string PassiveNameKey =>
        $"character.{I18n.ToSnakeCase(GetType().Name)}.passive.name";
    public virtual string PassiveName
    {
        get => I18n.Tr(PassiveNameKey, _passiveName);
        set => _passiveName = value;
    }
    public virtual string PassiveDescription { get; set; }

    //properties label
    public Label LifeLabel => field ??= GetNode("LifeBar/Life") as Label;
    public Label PowerLabel;
    public Label DefenseLabel;
    public Label BlockLabel => field ??= GetNode<Label>("LifeBar/Block");
    private Control BlockIcon =>
        field ??= GetNodeOrNull<Control>("LifeBar/Block/SurvivabilityIcon");
    public ProgressBar LifeBar => field ??= GetNode<ProgressBar>("LifeBar");
    public ProgressBar BufferBar => field ??= GetNode<ProgressBar>("LifeBar/BufferBar");
    public Label PowerIconLabel => field ??= GetNode<Label>("State/PowerIcon/Label");
    public Label SurvivabilityIconLabel =>
        field ??= GetNode<Label>("State/SurvivabilityIcon/Label");
    public Label EnergeIconLabel => field ??= GetNode<Label>("State/EnergeIcon/Label");
    private Control EnergeIcon => field ??= GetNodeOrNull<Control>("State/EnergeIcon");
    private Control TurnOrderPreviewRoot => field ??= GetNodeOrNull<Control>("TurnOrderPreview");
    private ColorRect TurnOrderPreviewCircle =>
        field ??= GetNodeOrNull<ColorRect>("TurnOrderPreview/Circle");
    public Label TurnOrderPreviewLabel => field ??= GetNodeOrNull<Label>("TurnOrderPreview/Value");
    public TextureRect Hoverframe => field ??= GetNode<TextureRect>("Hoverframe");
    public CharacterShadow CharacterShadow => field ??= GetNodeOrNull<CharacterShadow>("CharacterShadow");
    public AnimatedSprite2D absorb => field ??= GetNode<AnimatedSprite2D>("Effect/absorb");
    public AnimatedSprite2D shield => field ??= GetNode<AnimatedSprite2D>("Effect/shield");

    [Export]
    public Node2D Sprite;
    public AnimationPlayer TrailAnimation => field ??= GetNode<AnimationPlayer>("TrailAnimation");
    public Node2D trail => field ??= GetNode<Node2D>("Path2D");
    private Path2D TrailPath => trail as Path2D;
    private Line2D TrailLine => trail?.GetNodeOrNull<Line2D>("Line2D");
    private global::Line TrailLineScript => trail?.GetNodeOrNull<global::Line>("Line2D");
    private PathFollow2D TrailFollow => trail?.GetNodeOrNull<PathFollow2D>("PathFollow2D");

    // public Control SkillControl => field??=GetNode<Control>("SkillControl");
    //action and skill
    public Skill[] Skills = new Skill[3];

    public AnimatedSprite2D Animate1 => field ??= GetNode("Effect/Effect1") as AnimatedSprite2D;
    public AnimationPlayer CAplayer => field ??= GetNode("Player") as AnimationPlayer;
    public Battle BattleNode;

    public int PositionIndex;

    public Vector2 GetVisualCenterGlobalPosition()
    {
        Control hoverframe = GetNodeOrNull<Control>("Hoverframe");
        if (
            hoverframe != null
            && GodotObject.IsInstanceValid(hoverframe)
            && hoverframe.IsInsideTree()
        )
        {
            return hoverframe.GetGlobalRect().GetCenter();
        }

        if (Sprite != null && GodotObject.IsInstanceValid(Sprite) && Sprite.IsInsideTree())
            return Sprite.GetGlobalTransformWithCanvas().Origin;

        return GetGlobalTransformWithCanvas().Origin;
    }

    public PackedScene Number = ResourceLoader.Load<PackedScene>("res://LabelNode/Number.tscn");
    public PackedScene HitParticleScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/HitParticle.tscn"
    );
    public PackedScene CharacterEffectScene = ResourceLoader.Load<PackedScene>(
        "res://battle/Effect/CharacterEffect.tscn"
    );
    public bool IsPlayer;
    public virtual bool IsSummon => false;
    public virtual bool IsFullCharacter => true;
    public virtual bool ParticipatesInTurnRotation => true;
    public virtual bool TriggersSkillUseEvents => true;
    public virtual bool ClearsBlockOnActionStart => true;
    protected virtual bool ResolvesTurnStartOnActionStart => true;
    private readonly HashSet<Buff.BuffName> _seenBuffs = new();
    public List<SummonCharacter> Summons { get; } = new();

    public void MarkBuffSeen(Buff.BuffName buffName)
    {
        _seenBuffs.Add(buffName);
    }

    public bool HasSeenBuff(Buff.BuffName buffName) => _seenBuffs.Contains(buffName);

    public IDisposable BeginEffectSource(string actionName = null) =>
        BattleNode?.PushEffectSource(this, actionName);

    //buff

    public List<DyingBuff> DyingBuffs = new List<DyingBuff>();
    public List<HurtBuff> HurtBuffs = new List<HurtBuff>();
    public List<AttackBuff> AttackBuffs = new List<AttackBuff>();
    public List<StartActionBuff> StartActionBuffs = new List<StartActionBuff>();
    public List<EndActionBuff> EndActionBuffs = new List<EndActionBuff>();
    public List<SpecialBuff> SpecialBuffs = new List<SpecialBuff>();
    public List<SkillBuff> SkillBuffs = new List<SkillBuff>();
    private Tip SkillTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");
    private Tip BuffTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/BuffTip");
    private Tip _localSkillTooltip;
    public Vector2 OriginalPosition;
    private Tween _hurtMoveTween;
    private Tween _spriteImpactTween;
    private Vector2 _spriteRestScale;
    private bool _spriteRestScaleCached;
    private Tween _bufferBarTween;
    private Tween _lifeBarTween;
    private Tween _lifeBarMaxTween;
    private StyleBox _defaultLifeBarFillStyle;
    private StyleBox _defaultBufferBarFillStyle;
    private bool _lifeBarBlockColorActive;
    private Tween _blockDisplayTween;
    private Vector2 _blockLabelBaseScale;
    private bool _blockLabelBaseScaleCached;
    private Tween _nextActionPreviewTween;
    private Tween _trailPreviewTween;
    private Tween _turnOrderPreviewTween;
    private Control _energyUsePreviewFrame;
    private bool _nextActionPreviewVisible;
    private Color _nextActionPreviewColor = new(1f, 1f, 1f, 0.82f);
    private Gradient _defaultTrailLineGradient;
    private Color _defaultTrailLineModulate;
    private bool _defaultTrailLineStyleCached;
    private bool _turnOrderPreviewSceneRectCached;
    private Vector2 _turnOrderPreviewScenePosition;
    private Vector2 _turnOrderPreviewSceneSize;
    private bool _customTrailPreviewVisible;
    private Vector2 _customTrailPreviewTargetGlobalPosition;
    private Color _customTrailPreviewColor = new(1f, 0.42f, 0.32f, 0.78f);
    private Line2D _curvedTrailPreviewLine;
    private ulong _lastIncreasePropertyEffectTickMsec;
    private bool _isHoverframeHovered;

    // Captured from the scene material, not from the shader defaults: CharacterTemplate.tscn
    // overrides the palette, so reverting to the shader's own defaults would change the look.
    private static readonly Color HoverframeInspectLineColor = new(0.90f, 0.97f, 1f, 1f);
    private static readonly Color HoverframeInspectAccentColor = new(0.42f, 0.87f, 1f, 1f);
    private static readonly Color HoverframeInspectFillColor = new(0.05f, 0.2f, 0.3f, 0.14f);
    private ShaderMaterial _hoverframeMaterial;
    private Color _hoverframeBaseLineColor;
    private Color _hoverframeBaseAccentColor;
    private Color _hoverframeBaseFillColor;
    private Tween _hoverframeLockTween;
    private Tween _hoverframeAssembleTween;
    private bool _hoverframeAssembled;
    private bool _isFramePreviewVisible;
    private bool _isTargetPreviewVisible;
    private Color _targetPreviewColor = Colors.White;
    private string _cachedSkillTooltipText;
    private string _cachedBuffTooltipText;
    private bool _skillTooltipCacheDirty = true;
    private bool _buffTooltipCacheDirty = true;
    private bool _buffDrivenVisualInitialized;
    private bool _hasInvisibleBuffVisual;
    private int _skillTooltipHoverVersion;
    private Curve2D _defaultTrailCurve;
    private Vector2 _defaultTrailLinePosition;
    private bool _hasDefaultTrailLinePosition;
    private bool _trailUsesCustomCurve;

    protected void SetCombatStats(int power, int survivability, int maxLife)
    {
        BattlePower = power;
        BattleSurvivability = survivability;
        BattleMaxLife = maxLife;
    }

    protected void SetBaseCombatStatContributions(int power, int survivability)
    {
        BasePowerContribution = power;
        BaseSurvivabilityContribution = survivability;
    }

    public int GetEffectivePowerForSkillScaling() => BattlePower + BasePowerContribution;

    public int GetEffectiveSurvivabilityForSkillScaling() =>
        BattleSurvivability + BaseSurvivabilityContribution;

    public void ConfigureCombatStats(int power, int survivability, int maxLife)
    {
        SetCombatStats(power, survivability, maxLife);
    }

    public void ApplyCombatStatMultiplier(float multiplier, bool refillLife = false)
    {
        if (multiplier <= 0f)
            return;

        SetCombatStats(
            ScaleCombatStat(BattlePower, multiplier),
            ScaleCombatStat(BattleSurvivability, multiplier),
            ScaleCombatStat(BattleMaxLife, multiplier)
        );
        SetBaseCombatStatContributions(
            ScaleCombatStat(BasePowerContribution, multiplier),
            ScaleCombatStat(BaseSurvivabilityContribution, multiplier)
        );

        Life = refillLife ? BattleMaxLife : Math.Min(Life, BattleMaxLife);
        SyncLifeBarsToCurrent(syncBufferValue: true);
        PowerIconLabel.Text = BattlePower.ToString();
        SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
        RefreshCombatStatIconVisibility();
        RefreshEnergyIconVisibility();
        SetEnergyUsePreviewVisible(false);
        InvalidateHoverTooltipCache();
    }

    private static int ScaleCombatStat(int value, float multiplier)
    {
        if (value <= 0)
            return value;

        return Math.Max(1, Mathf.CeilToInt(value * multiplier));
    }

    public virtual void Initialize()
    {
        InvalidateHoverTooltipCache();
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i] == null)
                continue;

            Skills[i].OwnerCharater = this;
            Skills[i].UpdateDescription();
        }
        //初始化数值
        State = CharacterState.Normal;

        Life = BattleMaxLife;
        SyncLifeBarsToCurrent(syncBufferValue: true);
        PowerIconLabel.Text = BattlePower.ToString();
        SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
        RefreshCombatStatIconVisibility();
        RefreshEnergyIconVisibility();

        Block = 0;
        RefreshBlockDisplay(immediate: true);
        RefreshLifeBarBlockColor();

        Hoverframe.PivotOffset = Hoverframe.Size / 2;
        _isHoverframeHovered = false;
        _isFramePreviewVisible = false;
        _isTargetPreviewVisible = false;
        _targetPreviewColor = Colors.White;
        ConfigureTurnOrderPreviewBase();
        HideTurnOrderPreview();
        RefreshHoverframeVisual();
        _cachedSkillTooltipText = BuildSkillTooltipText();
        _cachedBuffTooltipText = BuildBuffTooltipText();
        _skillTooltipCacheDirty = false;
        _buffTooltipCacheDirty = false;
        CacheDefaultTrailGeometry();
        RefreshBuffDrivenVisualState();
    }

    public override async void _Ready()
    {
        SetProcess(false);

        if (WarmupMode)
        {
            if (Sprite?.Material is ShaderMaterial material)
            {
                ShaderMaterial warmMaterial = (ShaderMaterial)material.Duplicate();
                warmMaterial.ResourceLocalToScene = true;
                Sprite.Material = warmMaterial;
                warmMaterial.SetShaderParameter("progress", 1f);
            }
            return;
        }

        Hoverframe.MouseEntered += OnHoverEntered;
        Hoverframe.MouseExited += OnHoverExited;
        Hoverframe.GuiInput += OnHoverframeGuiInput;

        if (Sprite.Material != null)
        {
            ShaderMaterial material = (ShaderMaterial)Sprite.Material.Duplicate();
            material.ResourceLocalToScene = true;
            Sprite.Material = material;
            ((ShaderMaterial)Sprite.Material).SetShaderParameter("progress", 1f);
            Tween tween = CreateTween();
            tween.TweenProperty(Sprite, "material:shader_parameter/progress", 0, 0.8f);
        }
        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        CharacterEffect.Spawn(this, "transition");
    }

    private void OnHoverframeGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton leftMouseButton
            && leftMouseButton.Pressed
            && leftMouseButton.ButtonIndex == MouseButton.Left
            && BattleNode?.CharacterControl?.TrySelectManualTargetFromCharacter(this) == true
        )
        {
            HideHoverTooltips();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (
            @event is not InputEventMouseButton mouseButton
            || !mouseButton.Pressed
            || mouseButton.ButtonIndex != MouseButton.Left
            || State == CharacterState.Dying
        )
        {
            return;
        }

        if (BattleNode?.TryShowCharacterBattleCardPiles(this) == true)
        {
            HideHoverTooltips();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnHoverEntered()
    {
        ulong hoverStartUsec = Time.GetTicksUsec();
        _isHoverframeHovered = true;
        _skillTooltipHoverVersion++;
        CharacterControl characterControl = BattleNode?.CharacterControl;
        bool isManualTargetSelection = characterControl?.IsManualTargetArrowSelectionActive == true;
        characterControl?.NotifyManualTargetHover(this, hovered: true);
        BattleNode?.MarkHoverPerfEvent(this, "character-hover-enter");

        ulong stepStartUsec = Time.GetTicksUsec();
        if (isManualTargetSelection)
            RefreshHoverframeVisual();
        else
            Hover();
        BattleNode?.LogHoverPerfWork(this, "character-hover-visual", stepStartUsec);
        if (State == CharacterState.Dying)
            return;

        stepStartUsec = Time.GetTicksUsec();
        ShowHoverTooltips();
        BattleNode?.LogHoverPerfWork(this, "character-hover-tooltips", stepStartUsec);
        BattleNode?.LogHoverPerfWork(this, "character-hover-enter", hoverStartUsec);
    }

    private void OnHoverExited()
    {
        _isHoverframeHovered = false;
        _skillTooltipHoverVersion++;
        BattleNode?.CharacterControl?.NotifyManualTargetHover(this, hovered: false);
        RefreshHoverframeVisual();
        HideHoverTooltips();
    }

    private void ShowHoverTooltips()
    {
        if (State == CharacterState.Dying)
        {
            HideHoverTooltips();
            return;
        }
        _ = ShowHoverTooltipsDelayed(_skillTooltipHoverVersion);
    }

    private void HideHoverTooltips()
    {
        _localSkillTooltip?.HideTooltip();
        SkillTooltip?.HideTooltip();
        BuffTooltip?.HideTooltip();
    }

    private async Task ShowHoverTooltipsDelayed(int hoverVersion)
    {
        if (State == CharacterState.Dying)
            return;

        if (SkillTooltipDelayMs > 0)
        {
            await ToSignal(GetTree().CreateTimer(SkillTooltipDelayMs / 1000.0f), "timeout");
        }

        if (
            hoverVersion != _skillTooltipHoverVersion
            || !_isHoverframeHovered
            || State == CharacterState.Dying
        )
            return;

        if (_localSkillTooltip != null)
        {
            ulong stepStartUsec = Time.GetTicksUsec();
            bool needBuild = _skillTooltipCacheDirty || _cachedSkillTooltipText == null;
            if (needBuild)
            {
                ulong buildStartUsec = Time.GetTicksUsec();
                string skillText = GetOrBuildSkillTooltipText();
                BattleNode?.LogHoverPerfDuration(
                    this,
                    "character-hover-skilltip-build",
                    (Time.GetTicksUsec() - buildStartUsec) / 1000.0
                );

                ulong preloadStartUsec = Time.GetTicksUsec();
                _localSkillTooltip.PreloadText(skillText);
                BattleNode?.LogHoverPerfDuration(
                    this,
                    "character-hover-skilltip-preload",
                    (Time.GetTicksUsec() - preloadStartUsec) / 1000.0
                );
            }

            ulong showStartUsec = Time.GetTicksUsec();
            _localSkillTooltip.ShowPreloaded(followMouse: true);
            BattleNode?.LogHoverPerfDuration(
                this,
                "character-hover-skilltip-show",
                (Time.GetTicksUsec() - showStartUsec) / 1000.0
            );
            BattleNode?.LogHoverPerfWork(this, "character-hover-skilltip", stepStartUsec);
        }
        else if (SkillTooltip != null)
        {
            ulong stepStartUsec = Time.GetTicksUsec();
            bool needBuild = _skillTooltipCacheDirty || _cachedSkillTooltipText == null;
            string skillText;
            if (needBuild)
            {
                ulong buildStartUsec = Time.GetTicksUsec();
                skillText = GetOrBuildSkillTooltipText();
                BattleNode?.LogHoverPerfDuration(
                    this,
                    "character-hover-skilltip-build",
                    (Time.GetTicksUsec() - buildStartUsec) / 1000.0
                );
            }
            else
            {
                skillText = _cachedSkillTooltipText;
            }

            ulong setStartUsec = Time.GetTicksUsec();
            SkillTooltip.FollowMouse = true;
            SkillTooltip.SetText(skillText);
            BattleNode?.LogHoverPerfDuration(
                this,
                "character-hover-skilltip-set",
                (Time.GetTicksUsec() - setStartUsec) / 1000.0
            );
            BattleNode?.LogHoverPerfWork(this, "character-hover-skilltip", stepStartUsec);
        }

        if (hoverVersion != _skillTooltipHoverVersion || !_isHoverframeHovered)
            return;

        if (BuffTooltip != null)
        {
            ulong stepStartUsec = Time.GetTicksUsec();
            BuffTooltip.FollowMouse = true;
            BuffTooltip.SetText(GetOrBuildBuffTooltipText());
            BattleNode?.LogHoverPerfWork(this, "character-hover-bufftip", stepStartUsec);
        }
    }

    private string BuildSkillTooltipText()
    {
        var sb = new StringBuilder(256);
        string name = string.IsNullOrWhiteSpace(CharacterName)
            ? I18n.Tr("ui.common.character", "Character")
            : CharacterName;
        sb.Append($"[b]{name}[/b]\n");

        AppendPassiveTooltip(sb);

        if (this is PlayerCharacter player)
        {
            AppendPlayerBattleCardPiles(sb, player);
            return sb.ToString().TrimEnd();
        }

        UserSettings.EnsureLoaded();
        if (UserSettings.HideEnemySkills)
            return TrimTrailingTooltipSeparator(sb);

        if (Skills == null || Skills.Length == 0)
            return TrimTrailingTooltipSeparator(sb);

        const string separator = "[hr]\n";
        const string skillNameColor = "#b56bff";
        int skillNameFontSize = UserSettings.ScaleTextFontSize(32);

        var validSkills = Skills.Where(x => x != null).ToArray();
        for (int i = 0; i < validSkills.Length; i++)
        {
            var skill = validSkills[i];

            if (skill.OwnerCharater != this)
                skill.OwnerCharater = this;
            skill.UpdateDescription();

            if (i > 0)
                sb.Append('\n');

            sb.Append(
                $"[font_size={skillNameFontSize}][color={skillNameColor}]{skill.SkillName}[/color][/font_size]  [color=#cccccc]({skill.SkillType.GetDescription()})[/color]\n"
            );
            if (!string.IsNullOrWhiteSpace(skill.Description))
                sb.Append(skill.Description);
            else
                sb.Append("-");
            sb.Append('\n');

            // One rule line as the gap between skills.
            if (i < validSkills.Length - 1)
                sb.Append(separator);
        }

        return sb.ToString().TrimEnd();
    }

    private static string TrimTrailingTooltipSeparator(StringBuilder sb)
    {
        string text = sb.ToString().TrimEnd();
        const string separator = "[hr]";
        if (text.EndsWith(separator, StringComparison.Ordinal))
            text = text[..^separator.Length].TrimEnd();
        return text;
    }

    private static void AppendPlayerBattleCardPiles(StringBuilder sb, PlayerCharacter player)
    {
        if (player == null)
            return;

        SkillID[] drawPileIds =
            GetSkillIdsFromPileEntries(player.BattleNode?.GetOwnedDrawBattleCardPileEntries(player))
            ?? GetOwnedPlayerSkillIds(player.CharacterIndex);
        SkillID[] discardPileIds =
            GetSkillIdsFromPileEntries(
                player.BattleNode?.GetOwnedDiscardBattleCardPileEntries(player)
            ) ?? Array.Empty<SkillID>();
        SkillID[] exhaustedIds =
            GetSkillIdsFromPileEntries(
                player.BattleNode?.GetOwnedExhaustedBattleCardPileEntries(player)
            ) ?? Array.Empty<SkillID>();

        const string skillNameColor = "#b56bff";
        int skillNameFontSize = UserSettings.ScaleTextFontSize(32);
        sb.Append(
            $"[font_size={skillNameFontSize}][color={skillNameColor}]{I18n.Tr("ui.common.draw_pile", "抽牌堆")}[/color][/font_size]\n"
        );
        AppendSkillPileLines(
            sb,
            GetSkillsFromIds(drawPileIds),
            emptyText: I18n.Tr("ui.common.empty", "空")
        );

        sb.Append("\n[hr]\n");
        sb.Append(
            $"[font_size={skillNameFontSize}][color=#9cdacf]{I18n.Tr("ui.common.discard_pile", "弃牌堆")}[/color][/font_size]\n"
        );
        AppendSkillPileLines(
            sb,
            GetSkillsFromIds(discardPileIds),
            emptyText: I18n.Tr("ui.common.empty", "空")
        );

        sb.Append("\n[hr]\n");
        sb.Append(
            $"[font_size={skillNameFontSize}][color=#ffb86b]{I18n.Tr("ui.common.exhaust_pile", "消耗卡堆")}[/color][/font_size]\n"
        );
        AppendSkillPileLines(
            sb,
            GetSkillsFromIds(exhaustedIds),
            emptyText: I18n.Tr("ui.common.empty", "空")
        );
    }

    private static SkillID[] GetOwnedPlayerSkillIds(int characterIndex)
    {
        if (
            GameInfo.PlayerCharacters == null
            || characterIndex < 0
            || characterIndex >= GameInfo.PlayerCharacters.Length
        )
        {
            return Array.Empty<SkillID>();
        }

        return (
            GameInfo.PlayerCharacters[characterIndex].GainedSkills ?? new List<SkillID>()
        ).ToArray();
    }

    private static SkillID[] GetSkillIdsFromPileEntries(
        Battle.BattleCardPileEntry[] entries
    )
    {
        return entries?.Select(entry => entry.SkillId).ToArray();
    }

    private static Skill[] GetSkillsFromIds(IEnumerable<SkillID> skillIds)
    {
        return (skillIds ?? Array.Empty<SkillID>())
            .Select(Skill.GetSkill)
            .Where(skill => skill != null)
            .ToArray();
    }

    private static void AppendSkillPileLines(StringBuilder sb, Skill[] skills, string emptyText)
    {
        if (skills == null || skills.Length == 0)
        {
            sb.Append(emptyText);
            sb.Append('\n');
            return;
        }

        AppendSkillPileNameLine(sb, skills, Skill.SkillTypes.Attack);
        AppendSkillPileNameLine(sb, skills, Skill.SkillTypes.Survive);
        AppendSkillPileNameLine(sb, skills, Skill.SkillTypes.Special);
        AppendSkillPileNameLine(sb, skills, Skill.SkillTypes.Ability);
        AppendSkillPileNameLine(sb, skills, Skill.SkillTypes.Status);
    }

    private static void AppendSkillPileNameLine(
        StringBuilder sb,
        Skill[] skills,
        Skill.SkillTypes skillType
    )
    {
        string[] names = FormatStackedSkillNames(
            skills
                .Where(skill => skill.SkillType == skillType)
                .Select(skill => skill.SkillName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
        );
        if (names.Length == 0)
            return;

        sb.Append($"[color=#cccccc]({skillType.GetDescription()})[/color] ");
        sb.Append(string.Join("、", names));
        sb.Append('\n');
    }

    private static string[] FormatStackedSkillNames(IEnumerable<string> names)
    {
        return (names ?? Array.Empty<string>())
            .GroupBy(name => name)
            .Select(group => group.Count() > 1 ? $"{group.Key} x{group.Count()}" : group.Key)
            .ToArray();
    }

    private void AppendPassiveTooltip(StringBuilder sb)
    {
        string passiveName = PassiveName;
        string passiveDesc = PassiveDescription;
        if (string.IsNullOrWhiteSpace(passiveName) && string.IsNullOrWhiteSpace(passiveDesc))
            return;

        const string passiveColor = "#ffd36b";
        int titleFontSize = UserSettings.ScaleTextFontSize(30);

        string title = string.IsNullOrWhiteSpace(passiveName)
            ? I18n.Tr("ui.common.passive", "Passive")
            : passiveName;
        sb.Append(
            $"[font_size={titleFontSize}][color={passiveColor}]{title}[/color][/font_size]  [color=#cccccc]({I18n.Tr("ui.common.passive_tag", "被动")})[/color]\n"
        );

        if (!string.IsNullOrWhiteSpace(passiveDesc))
            sb.Append(GlobalFunction.ColorizeKeywords(GlobalFunction.ColorizeNumbers(passiveDesc)));
        else
            sb.Append("-");

        sb.Append("\n[hr]\n");
    }

    public string GetSkillTooltipText()
    {
        return GetOrBuildSkillTooltipText();
    }

    public string GetBuffTooltipText()
    {
        return GetOrBuildBuffTooltipText();
    }

    public void InvalidateHoverTooltipCache()
    {
        _skillTooltipCacheDirty = true;
        _buffTooltipCacheDirty = true;
        _cachedSkillTooltipText = null;
        _cachedBuffTooltipText = null;
    }

    public void InvalidateSkillTooltipCache()
    {
        _skillTooltipCacheDirty = true;
        _cachedSkillTooltipText = null;
        RefreshVisibleSkillTooltipText();
    }

    public void RefreshSkillTooltipTextFromSettings()
    {
        InvalidateSkillTooltipCache();

        if (_localSkillTooltip != null && GodotObject.IsInstanceValid(_localSkillTooltip))
            _localSkillTooltip.PreloadText(GetOrBuildSkillTooltipText());

        if (
            _isHoverframeHovered
            && SkillTooltip != null
            && GodotObject.IsInstanceValid(SkillTooltip)
        )
        {
            SkillTooltip.FollowMouse = true;
            SkillTooltip.SetText(GetOrBuildSkillTooltipText());
        }
    }

    public void InvalidateBuffTooltipCache()
    {
        _buffTooltipCacheDirty = true;
        _cachedBuffTooltipText = null;
        RefreshBuffDrivenVisualState();
    }

    public bool HasActiveStartActionBuff(Buff.BuffName name)
    {
        if (StartActionBuffs == null)
            return false;

        for (int i = 0; i < StartActionBuffs.Count; i++)
        {
            StartActionBuff buff = StartActionBuffs[i];
            if (buff != null && buff.ThisBuffName == name && buff.Stack > 0)
                return true;
        }

        return false;
    }

    internal void RefreshBuffDrivenVisualState()
    {
        if (Sprite == null || !GodotObject.IsInstanceValid(Sprite))
            return;

        bool hasInvisible = HasActiveStartActionBuff(Buff.BuffName.Invisible);
        if (_buffDrivenVisualInitialized && _hasInvisibleBuffVisual == hasInvisible)
            return;

        _buffDrivenVisualInitialized = true;
        _hasInvisibleBuffVisual = hasInvisible;
        Sprite.SelfModulate = hasInvisible ? InvisibleSpriteModulate : NormalSpriteModulate;
    }

    private string GetOrBuildSkillTooltipText()
    {
        if (_skillTooltipCacheDirty || _cachedSkillTooltipText == null)
        {
            _cachedSkillTooltipText = BuildSkillTooltipText();
            _skillTooltipCacheDirty = false;
        }

        return _cachedSkillTooltipText;
    }

    private void RefreshVisibleSkillTooltipText()
    {
        if (!_isHoverframeHovered || State == CharacterState.Dying)
            return;

        string skillText = GetOrBuildSkillTooltipText();
        if (_localSkillTooltip != null && GodotObject.IsInstanceValid(_localSkillTooltip))
        {
            if (_localSkillTooltip.Visible)
                _localSkillTooltip.SetText(skillText);
            else
                _localSkillTooltip.PreloadText(skillText);
            return;
        }

        if (
            SkillTooltip != null
            && GodotObject.IsInstanceValid(SkillTooltip)
            && SkillTooltip.Visible
        )
        {
            SkillTooltip.FollowMouse = true;
            SkillTooltip.SetText(skillText);
        }
    }

    private string GetOrBuildBuffTooltipText()
    {
        if (_buffTooltipCacheDirty || _cachedBuffTooltipText == null)
        {
            _cachedBuffTooltipText = BuildBuffTooltipText();
            _buffTooltipCacheDirty = false;
        }

        return _cachedBuffTooltipText;
    }

    private string BuildBuffTooltipText()
    {
        var sb = new StringBuilder(128);
        sb.Append("[b]Buffs[/b]\n");

        bool any = false;
        var colord = "#ffffef";

        if (StartActionBuffs != null)
        {
            foreach (var buff in StartActionBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (EndActionBuffs != null)
        {
            foreach (var buff in EndActionBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (SkillBuffs != null)
        {
            foreach (var buff in SkillBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (AttackBuffs != null)
        {
            foreach (var buff in AttackBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (SpecialBuffs != null)
        {
            foreach (var buff in SpecialBuffs.Where(x => x != null && (x.Stack > 0 || x.ThisBuffName == Buff.BuffName.AttackCount)))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (HurtBuffs != null)
        {
            foreach (var buff in HurtBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (DyingBuffs != null)
        {
            foreach (var buff in DyingBuffs.Where(x => x != null && x.Stack > 0))
            {
                AppendBuffTooltipEntry(sb, buff, colord);
                any = true;
            }
        }

        if (!any)
            sb.Append("None");

        return GlobalFunction.ColorizeNumbers(sb.ToString().TrimEnd());
    }

    private static void AppendBuffTooltipEntry(StringBuilder sb, Buff buff, string effectColor)
    {
        if (buff == null)
            return;

        sb.Append(
            $"{Buff.BuildTooltipIconTag(buff.ThisBuffName)}       {Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n"
        );
        var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
        if (!string.IsNullOrWhiteSpace(effect))
            sb.Append($"[color={effectColor}]{effect}[/color]\n");
    }

    public void PrepareHoverTooltipInstances()
    {
        if (_localSkillTooltip != null || TooltipScene == null)
            return;

        var root = GetTree()?.Root;
        var layer = root?.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
            return;

        _localSkillTooltip = TooltipScene.Instantiate<Tip>();
        _localSkillTooltip.Name = $"{Name}_SkillTip";
        _localSkillTooltip.AnchorOffset = new Vector2(20f, 0f);
        _localSkillTooltip.AnchorHeightRatio = 1f / 3f;
        layer.AddChild(_localSkillTooltip);
        _localSkillTooltip.HideTooltip();
        _localSkillTooltip.PreloadText(GetOrBuildSkillTooltipText());
    }

    public override void _ExitTree()
    {
        if (_curvedTrailPreviewLine != null && GodotObject.IsInstanceValid(_curvedTrailPreviewLine))
        {
            _curvedTrailPreviewLine.QueueFree();
            _curvedTrailPreviewLine = null;
        }

        if (_localSkillTooltip != null && GodotObject.IsInstanceValid(_localSkillTooltip))
        {
            _localSkillTooltip.QueueFree();
            _localSkillTooltip = null;
        }

        base._ExitTree();
    }

    public virtual void StartAction()
    {
        BattleNode?.SetCurrentActionCharacter(this);
        if (ResolvesTurnStartOnActionStart)
            ResolveTurnStartPhase();

        OnActionStart();
        _customTrailPreviewVisible = false;
        RefreshCurvedTrailPreviewLine();
        bool showActionTrail = !(IsPlayer && BattleNode?.IsResolvingPlayerTeamActionPhase == true);
        if (showActionTrail)
            TrailAnimation.Play("trail");
        _nextActionPreviewVisible = false;
        _nextActionPreviewTween?.Kill();
        _trailPreviewTween?.Kill();
        RestoreDefaultTrailLineStyle();
        RestoreDefaultTrailGeometry();
        if (showActionTrail)
            CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 1f), 0.2f);
        else if (trail != null)
            trail.Modulate = new Color(1, 0, 0, 0f);
    }

    public void ShowNextActionPreview()
    {
        ShowNextActionPreview(new Color(1f, 1f, 1f, 0.82f));
    }

    public void ShowNextActionPreview(Color color)
    {
        if (trail == null || State == CharacterState.Dying)
            return;

        _nextActionPreviewColor = color;
        _nextActionPreviewVisible = true;
        RefreshTrailPreviewState();
    }

    public void HideNextActionPreview()
    {
        if (!_nextActionPreviewVisible || trail == null)
            return;

        _nextActionPreviewVisible = false;
        RefreshTrailPreviewState();
    }

    public void ShowCurvedTrailPreviewToTarget(Vector2 targetGlobalPosition, Color color)
    {
        if (State == CharacterState.Dying)
            return;

        _customTrailPreviewVisible = true;
        _customTrailPreviewTargetGlobalPosition = targetGlobalPosition;
        _customTrailPreviewColor = color;
        RefreshCurvedTrailPreviewLine();
    }

    public void HideCurvedTrailPreview()
    {
        _customTrailPreviewVisible = false;
        RefreshCurvedTrailPreviewLine();
    }

    public void ShowTurnOrderPreview(int order)
    {
        var root = TurnOrderPreviewRoot;
        var circle = TurnOrderPreviewCircle;
        var label = TurnOrderPreviewLabel;
        if (root == null || circle == null || label == null || State == CharacterState.Dying)
            return;

        order = Math.Max(0, order);
        bool isCurrent = order == 0;
        bool wasVisible = root.Visible;

        ConfigureTurnOrderPreviewBase();
        ApplyTurnOrderPreviewStyle(order, isCurrent);
        label.Text = (order + 1).ToString();
        root.Visible = true;

        Vector2 targetScale = isCurrent ? new Vector2(1.08f, 1.08f) : Vector2.One;
        _turnOrderPreviewTween?.Kill();
        if (!wasVisible)
        {
            root.Scale = isCurrent ? new Vector2(0.82f, 0.82f) : new Vector2(0.9f, 0.9f);
            root.Modulate = new Color(1f, 1f, 1f, 0f);

            _turnOrderPreviewTween = CreateTween();
            _turnOrderPreviewTween.SetParallel(true);
            _turnOrderPreviewTween
                .TweenProperty(root, "scale", targetScale, isCurrent ? 0.22f : 0.16f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
            _turnOrderPreviewTween
                .TweenProperty(root, "modulate", Colors.White, isCurrent ? 0.18f : 0.14f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Cubic);
        }
        else
        {
            root.Scale = targetScale;
            root.Modulate = Colors.White;
        }
    }

    public void HideTurnOrderPreview()
    {
        _turnOrderPreviewTween?.Kill();

        var root = TurnOrderPreviewRoot;
        if (root != null)
        {
            root.Visible = false;
            root.Scale = Vector2.One;
            root.Modulate = Colors.White;
        }
    }

    private void ConfigureTurnOrderPreviewBase()
    {
        var root = TurnOrderPreviewRoot;
        if (root == null)
            return;

        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        CacheTurnOrderPreviewSceneRect(root);
        ApplyTurnOrderPreviewLayout(root);

        if (TurnOrderPreviewCircle?.Material is ShaderMaterial material)
        {
            TurnOrderPreviewCircle.Color = Colors.White;

            ShaderMaterial localMaterial = material;
            if (!material.ResourceLocalToScene)
            {
                localMaterial = (ShaderMaterial)material.Duplicate();
                localMaterial.ResourceLocalToScene = true;
                TurnOrderPreviewCircle.Material = localMaterial;
            }
        }
    }

    private void ApplyTurnOrderPreviewStyle(int order, bool isCurrent)
    {
        bool isNext = order == 1;
        Color accent =
            isCurrent ? new Color(1f, 0.22f, 0.18f, 1f)
            : isNext ? new Color(1f, 0.88f, 0.18f, 1f)
            : Colors.White;
        Color glow = accent with
        {
            A =
                isCurrent ? 0.22f
                : isNext ? 0.18f
                : 0.1f,
        };

        if (TurnOrderPreviewCircle?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("rim_color", accent);
            material.SetShaderParameter("glow_color", glow);
        }
    }

    private void CacheTurnOrderPreviewSceneRect(Control root)
    {
        if (_turnOrderPreviewSceneRectCached || root == null)
            return;

        _turnOrderPreviewScenePosition = root.Position;
        _turnOrderPreviewSceneSize = root.Size;
        _turnOrderPreviewSceneRectCached = true;
    }

    private void ApplyTurnOrderPreviewLayout(Control root)
    {
        if (root == null || !_turnOrderPreviewSceneRectCached)
            return;

        root.Size = _turnOrderPreviewSceneSize;
        root.Position = IsPlayer
            ? new Vector2(
                -_turnOrderPreviewScenePosition.X - _turnOrderPreviewSceneSize.X,
                _turnOrderPreviewScenePosition.Y
            )
            : _turnOrderPreviewScenePosition;
        root.PivotOffset = root.Size / 2f;
    }

    protected virtual void ResolveTurnStartPhase()
    {
        if (SkillBuffs != null)
        {
            foreach (var buff in SkillBuffs.Where(x => x != null).ToArray())
            {
                buff.ResetTurnState();
            }
        }

        OnTurnStart();

        if (StartActionBuffs == null)
            return;

        // Buffs can remove themselves from the list when triggered (Stack reaches 0).
        // Iterate over a snapshot to avoid skipping the next buff due to index shifting.
        foreach (var buff in StartActionBuffs.Where(x => x != null && x.Stack > 0).ToArray())
        {
            buff.Trigger();
        }
    }

    public void ResolveTeamTurnStartPhase()
    {
        ResolveTurnStartPhase();
    }

    public void ResolveBlockExpirationAtTeamTurnStart()
    {
        if (!ClearsBlockOnActionStart)
            return;

        bool keepBlock =
            StartActionBuffs != null
            && StartActionBuffs.Any(x =>
                x != null && x.Stack > 0 && StartActionBuff.KeepsBlockOnTurnStart(x.ThisBuffName)
            );

        if (!keepBlock)
        {
            Block = 0;
            UpdataBlock(0);
        }

        ClearOwnedSummonsBlock();
    }

    private void ClearOwnedSummonsBlock()
    {
        if (Summons == null || Summons.Count == 0)
            return;

        var ownedSummons = Summons
            .Where(x =>
                x != null && GodotObject.IsInstanceValid(x) && x.State != CharacterState.Dying
            )
            .ToArray();
        for (int i = 0; i < ownedSummons.Length; i++)
        {
            ownedSummons[i].Block = 0;
            ownedSummons[i].UpdataBlock(0);
        }
    }

    public virtual async void EndAction()
    {
        OnActionEnd();
        var battleNode = BattleNode;
        if (battleNode == null || !GodotObject.IsInstanceValid(battleNode))
            return;

        await battleNode.EndEmitS(this);
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 0), 0.2f);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        TrailAnimation.Stop();
    }

    protected virtual async Task ResolveTurnEndPhaseAsync()
    {
        if (EndActionBuffs != null)
        {
            foreach (var buff in EndActionBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                await buff.Trigger();
            }
        }

        OnTurnEnd();
    }

    public Task ResolveTeamTurnEndPhaseAsync()
    {
        return ResolveTurnEndPhaseAsync();
    }

    public virtual async Task GetHurt(
        float damage,
        Character source = null,
        DamageKind damageKind = DamageKind.Other,
        bool ignoreBlock = false
    )
    {
        HitParticle.Spawn(this);

        if (HurtBuffs != null)
        {
            // Hurt buffs can remove themselves from the list when triggered (Stack reaches 0).
            // Iterate over a snapshot to ensure later buffs (e.g. DamageImmune) still trigger.
            foreach (var buff in HurtBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                damage = await buff.Trigger(damage, source, damageKind);
            }
        }

        int incomingDamage = Math.Max((int)damage, 0);
        int previousLife = Life;
        int blockedDamage;
        int actualDamage;

        if (ignoreBlock)
        {
            blockedDamage = 0;
            actualDamage = Math.Clamp(incomingDamage, 0, previousLife);
            Life -= actualDamage;
        }
        else
        {
            int previousBlock = Block;
            blockedDamage = Math.Clamp(
                Math.Min(incomingDamage, previousBlock),
                0,
                incomingDamage
            );
            actualDamage = Math.Clamp(incomingDamage - previousBlock, 0, previousLife);
            Life -= Math.Clamp(incomingDamage - previousBlock, 0, Life);
            Block = Math.Clamp(Block - incomingDamage, 0, 99999);
            UpdataBlock(0);
        }

        if (actualDamage > 0)
            AudioManager.PlayHurt(this);
        else if (blockedDamage > 0)
            AudioManager.PlayBlockImpact(this);
        SpawnDamageResultNumbers(incomingDamage, actualDamage, blockedDamage);
        PlayHurtImpactTween(actualDamage, blockedDamage);
        BattleNode?.PlayHitEffect(actualDamage, blockedDamage);
        if (actualDamage > 0)
            AnimateLifeBarsAfterDamage();
        else
            SyncLifeBarsToCurrent(syncBufferValue: false);
        BattleNode?.SyncPlayerLifeToGameInfo();
        BattleNode?.RecordDamage(this, actualDamage, blockedDamage, source);
        if (actualDamage > 0)
            source?.OnDealUnblockedDamage(this, actualDamage, damageKind);

        PlayHurtMoveTween(actualDamage, blockedDamage);

        if (Life == 0)
        {
            await Dying(source);
        }
    }

    private void SpawnDamageResultNumbers(int incomingDamage, int actualDamage, int blockedDamage)
    {
        if (actualDamage > 0)
        {
            float damageImpact = Mathf.Clamp(0.92f + actualDamage / 36f, 0.92f, 1.46f);
            Color damageColor = actualDamage >= 28
                ? new Color(1f, 0.18f, 0.1f, 1f)
                : new Color(1f, 0.24f, 0.18f, 1f);
            global::Number.Spawn(this, $"-{actualDamage}", damageColor, damageImpact);
        }

        if (blockedDamage > 0)
        {
            float blockImpact = Mathf.Clamp(0.78f + blockedDamage / 70f, 0.78f, 1.08f);
            global::Number.Spawn(
                this,
                $"-{blockedDamage}",
                new Color(0.42f, 0.86f, 1f, 1f),
                blockImpact
            );
        }

        if (incomingDamage <= 0 && actualDamage <= 0 && blockedDamage <= 0)
            global::Number.Spawn(this, "免疫", new Color(0.76f, 0.92f, 1f, 1f), 0.76f);
    }

    private void PlayHurtImpactTween(int actualDamage, int blockedDamage)
    {
        if (Sprite == null || !GodotObject.IsInstanceValid(Sprite))
            return;

        float impact = ResolveLocalHitImpact(actualDamage, blockedDamage);
        Color flashColor = actualDamage > 0
            ? new Color(1.75f, 1.32f, 1.22f, 1f)
            : new Color(1.24f, 1.62f, 1.85f, 1f);

        PlaySpriteImpactTween(
            flashColor,
            1.0f + 0.055f * impact,
            1.0f - 0.045f * impact,
            0.16f + 0.025f * impact
        );
    }

    private void PlayPositiveImpactTween(Color flashColor, float strength = 1f)
    {
        float impact = Mathf.Clamp(strength, 0.65f, 1.35f);
        PlaySpriteImpactTween(
            flashColor,
            1.0f - 0.028f * impact,
            1.0f + 0.055f * impact,
            0.18f + 0.03f * impact
        );
    }

    public void PlayAbilityCardActivationPulse()
    {
        if (State == CharacterState.Dying)
            return;

        PlayPositiveImpactTween(new Color(1.12f, 0.82f, 1.72f, 1f), 1.28f);
    }

    public void PlayTargetLockPulse(Color? flashColor = null, float strength = 1f)
    {
        if (State == CharacterState.Dying)
            return;

        float impact = Mathf.Clamp(strength, 0.75f, 1.5f);
        PlaySpriteImpactTween(
            flashColor ?? new Color(1.55f, 1.48f, 0.78f, 1f),
            1.0f + 0.025f * impact,
            1.0f + 0.055f * impact,
            0.17f + 0.025f * impact
        );

        if (Hoverframe == null || !GodotObject.IsInstanceValid(Hoverframe))
            return;

        Hoverframe.PivotOffset = Hoverframe.Size / 2;
        Hoverframe.SelfModulate = new Color(1f, 0.93f, 0.52f, 1f);
        Hoverframe.Scale = new Vector2(1.18f, 1.18f);
        Tween tween = Hoverframe.CreateTween();
        tween.SetParallel(true);
        tween
            .TweenProperty(Hoverframe, "scale", Vector2.One, 0.22f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .TweenProperty(
                Hoverframe,
                "self_modulate:a",
                _isTargetPreviewVisible ? _targetPreviewColor.A : 0f,
                0.22f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(RefreshHoverframeVisual));
    }

    private void PlaySpriteImpactTween(
        Color flashColor,
        float targetScaleX,
        float targetScaleY,
        float duration
    )
    {
        if (Sprite == null || !GodotObject.IsInstanceValid(Sprite))
            return;

        Vector2 baseScale = GetSpriteRestScale();
        Vector2 impactScale = new(baseScale.X * targetScaleX, baseScale.Y * targetScaleY);

        _spriteImpactTween?.Kill();
        Sprite.Modulate = flashColor;
        Sprite.Scale = baseScale;
        Sprite.Scale = impactScale;
        _spriteImpactTween = CreateTween();
        _spriteImpactTween.SetParallel(true);
        _spriteImpactTween
            .TweenProperty(Sprite, "scale", baseScale, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _spriteImpactTween
            .TweenProperty(Sprite, "modulate", Colors.White, Math.Max(0.1f, duration * 0.82f))
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        Tween activeTween = _spriteImpactTween;
        _spriteImpactTween.Finished += () =>
        {
            if (_spriteImpactTween != activeTween)
                return;

            if (Sprite != null && GodotObject.IsInstanceValid(Sprite))
            {
                Sprite.Scale = baseScale;
                Sprite.Modulate = Colors.White;
            }

            _spriteImpactTween = null;
        };
    }

    private Vector2 GetSpriteRestScale()
    {
        if (!_spriteRestScaleCached)
        {
            Vector2 currentScale = Sprite?.Scale ?? Vector2.One;
            _spriteRestScale = currentScale == Vector2.Zero ? Vector2.One : currentScale;
            _spriteRestScaleCached = true;
        }

        return _spriteRestScale;
    }

    private static float ResolveLocalHitImpact(int actualDamage, int blockedDamage)
    {
        int effectiveDamage = Math.Max(actualDamage, blockedDamage);
        float impact = effectiveDamage switch
        {
            >= 45 => 1.55f,
            >= 28 => 1.32f,
            >= 14 => 1.08f,
            > 0 => 0.82f,
            _ => 0.58f,
        };

        if (actualDamage <= 0 && blockedDamage > 0)
            impact *= 0.75f;

        return Mathf.Clamp(impact, 0.5f, 1.65f);
    }

    private void PlayHurtMoveTween(int actualDamage, int blockedDamage)
    {
        float impact = ResolveLocalHitImpact(actualDamage, blockedDamage);
        float directionScale = actualDamage > 0 ? 1f : 0.62f;
        Vector2 hurtOffset = (IsPlayer ? Vector2.Left : Vector2.Right)
            * (18f + 10f * impact)
            * directionScale;
        Vector2 hurtTarget = OriginalPosition + hurtOffset;
        Vector2 currentPosition = Position;
        float maxOffsetDistance = Math.Max(hurtOffset.Length(), 0.001f);
        float moveDistance = currentPosition.DistanceTo(hurtTarget);
        float moveDuration = 0.3f * Mathf.Clamp(moveDistance / maxOffsetDistance, 0.12f, 1.0f);

        _hurtMoveTween?.Kill();
        _hurtMoveTween = CreateTween();
        _hurtMoveTween.SetTrans(Tween.TransitionType.Sine);

        if (moveDistance > 0.01f)
        {
            _hurtMoveTween
                .TweenProperty(this, "position", hurtTarget, moveDuration)
                .SetEase(Tween.EaseType.Out);
        }

        _hurtMoveTween
            .TweenProperty(this, "position", OriginalPosition, 0.2f)
            .SetEase(Tween.EaseType.In);
        Tween activeTween = _hurtMoveTween;
        _hurtMoveTween.Finished += () =>
        {
            if (_hurtMoveTween != activeTween)
                return;

            Position = OriginalPosition;
            _hurtMoveTween = null;
        };
    }

    public virtual void Recover(int num, bool rebirth = false, Character source = null)
    {
        int heal = Math.Clamp(num, 0, 999);
        ApplyRecover(heal, rebirth, source, canRevive: num > 0);
    }

    public virtual async Task RecoverAsync(int num, bool rebirth = false, Character source = null)
    {
        int heal = Math.Clamp(num, 0, 999);
        bool wasDying = State == CharacterState.Dying;
        CharacterEffect effect = ApplyRecover(heal, rebirth, source, canRevive: num > 0);
        if (effect == null || !GodotObject.IsInstanceValid(effect))
            return;

        if (!effect.IsNodeReady())
            await effect.ToSignal(effect, Node.SignalName.Ready);

        if (GodotObject.IsInstanceValid(effect.Animation))
            await effect.ToSignal(effect.Animation, AnimationPlayer.SignalName.AnimationFinished);

        if (wasDying && State == CharacterState.Normal)
            await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);
    }

    private CharacterEffect ApplyRecover(int heal, bool rebirth, Character source, bool canRevive)
    {
        if (State == CharacterState.Dying)
        {
            if (!rebirth)
                return null;

            if (IsPlayer && BattleNode != null && !BattleNode.CanReviveDyingPlayerNow())
                return null;
        }

        int previousLife = Life;
        Life = Math.Clamp(Life + heal, 0, BattleMaxLife);
        int actualHeal = Life - previousLife;
        AnimateLifeBarsAfterRecover();
        BattleNode?.SyncPlayerLifeToGameInfo();
        if (actualHeal > 0)
        {
            ScreenEffectOverlay.PlayHeal(this, actualHeal);
            global::Number.Spawn(
                this,
                actualHeal.ToString("+0"),
                new Color(0.36f, 1f, 0.48f, 1f),
                Mathf.Clamp(0.86f + actualHeal / 48f, 0.86f, 1.18f)
            );
            PlayPositiveImpactTween(new Color(0.72f, 1.55f, 0.92f, 1f), actualHeal / 18f);
        }

        var effect = CharacterEffect.Spawn(this, "recover");
        if (State == CharacterState.Dying && canRevive && Life > 0)
        {
            State = CharacterState.Normal;
            CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.4f);
            BattleNode?.NotifyHandPreviewContextChanged();
        }

        BattleNode?.RecordHeal(this, actualHeal, source);
        return effect;
    }

    public virtual async Task Dying(Character source = null)
    {
        bool enteredDying = State != CharacterState.Dying;
        State = CharacterState.Dying;
        BattleNode?.RecordDying(this, source);
        if (BattleNode != null)
            await BattleNode.EmitCharacterDying(this, source);

        CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.4f);
        if (DyingBuffs != null)
            // Dying buffs can remove themselves from the list when triggered (Stack reaches 0).
            // Iterate over a snapshot to avoid skipping subsequent buffs.
            foreach (var buff in DyingBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                await buff.Trigger();
            }

        if (State == CharacterState.Dying)
        {
            if (enteredDying)
            {
                BattleNode?.HandleCharacterEnteredDying(this);
                BattleNode?.NotifyHandPreviewContextChanged();
            }
            TriggerOwnedSummonsDying();
            BattleNode?.QueueBattleOverCheck();
        }
    }

    private void TriggerOwnedSummonsDying()
    {
        if (Summons == null || Summons.Count == 0)
            return;

        var ownedSummons = Summons
            .Where(x =>
                x != null && GodotObject.IsInstanceValid(x) && x.State != CharacterState.Dying
            )
            .ToArray();
        for (int i = 0; i < ownedSummons.Length; i++)
        {
            _ = ownedSummons[i].DyingFromSummoner();
        }
    }

    public virtual void DisableSkill() { }

    public virtual void OnDealUnblockedDamage(
        Character target,
        int actualDamage,
        DamageKind damageKind
    ) { }

    internal void RefreshEnergyIconVisibility()
    {
        if (EnergeIcon != null)
            EnergeIcon.Visible = false;
    }

    public void SetEnergyUsePreviewVisible(bool visible)
    {
        Control frame = EnsureEnergyUsePreviewFrame();
        if (frame == null)
            return;

        frame.Visible = visible && IsPlayer && State != CharacterState.Dying;
        if (frame.Visible)
            EnergeIconLabel?.MoveToFront();
    }

    private Control EnsureEnergyUsePreviewFrame()
    {
        return _energyUsePreviewFrame ??= GetNodeOrNull<Control>("State/EnergeIcon/EnergyUsePreviewFrame");
    }

    private void RefreshCombatStatIconVisibility()
    {
        SetCombatStatIconVisibility(PowerIconLabel, BattlePower);
        SetCombatStatIconVisibility(SurvivabilityIconLabel, BattleSurvivability);
    }

    private static void SetCombatStatIconVisibility(Label label, int value)
    {
        if (label?.GetParent() is CanvasItem icon)
            icon.Visible = value != 0;
    }

    public void UpdataBlock(int num, bool record = true, Character source = null)
    {
        if (State == CharacterState.Dying)
            return;

        int previousBlock = Block;
        if (num > 0)
        {
            CharacterEffect.Spawn(this, "shield");
            AudioManager.PlayBlockGain(this);
        }
        Block = Math.Clamp(Block + num, 0, 999);
        RefreshBlockDisplay();
        RefreshLifeBarBlockColor();
        BattleNode?.RefreshEnemyIntentionPreviews();

        if (num > 0)
        {
            global::Number.Spawn(
                this,
                "+" + num.ToString(),
                new Color(180, 220, 255, 255) / 255,
                Mathf.Clamp(0.82f + num / 56f, 0.82f, 1.12f)
            );
            PlayPositiveImpactTween(new Color(0.64f, 1.08f, 1.65f, 1f), num / 18f);
            if (record)
                BattleNode?.RecordBlockGain(this, num, source);

            int gainedBlock = Math.Max(0, Block - previousBlock);
            SpecialBuff.TriggerBeaconBlockShare(this, gainedBlock);
        }
    }

    public async Task DescendingProperties(PropertyType type, int value, Character source = null)
    {
        if (value == 0)
            return;

        if (value > 0 && SpecialBuff.TryConsumeDebuffImmunity(this, source))
            return;

        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                BattlePower -= value;
                icon = PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivability:
                BattleSurvivability -= value;
                icon = SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.MaxLife:
                return;
        }

        if (value <= 0)
            return;

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            RefreshCombatStatIconVisibility();
            Buff.GhostExplode(icon, new Vector2(2f, 2f), useOffsetMotion: false);
        }

        CharacterEffect.Spawn(this, "lightning");

        BuffHintLabel.Spawn(
            this,
            $"{Skill.GetColoredPropertyLabel(type)} -{value}",
            GlobalPosition + new Vector2(0, 150),
            randomOffset: true
        );
        InvalidateSkillTooltipCache();
        BattleNode?.RecordPropertyChange(this, type, -value, source);
        BattleNode?.RefreshEnemyIntentionPreviews();
        BattleNode?.NotifyHandPreviewContextChanged();
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    public async Task DescendingMaxLifeFromPassive(int value, Character source = null)
    {
        if (value <= 0)
            return;

        if (SpecialBuff.TryConsumeDebuffImmunity(this, source))
            return;

        int appliedValue = Math.Min(value, Math.Max(0, BattleMaxLife - 1));
        if (appliedValue <= 0)
            return;

        BattleMaxLife -= appliedValue;
        Life = Math.Min(Life, BattleMaxLife);
        AnimateLifeBarCapacityChange();
        BattleNode?.SyncPlayerLifeToGameInfo();

        CharacterEffect.Spawn(this, "lightning");

        BuffHintLabel.Spawn(
            this,
            $"{Skill.GetColoredPropertyLabel(PropertyType.MaxLife)} -{appliedValue}",
            GlobalPosition + new Vector2(0, 150),
            randomOffset: true
        );
        InvalidateSkillTooltipCache();
        BattleNode?.RecordPropertyChange(this, PropertyType.MaxLife, -appliedValue, source);
        BattleNode?.RefreshEnemyIntentionPreviews();
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    public async Task IncreaseMaxLifeFromPassive(int value, Character source = null)
    {
        if (value <= 0 || State == CharacterState.Dying)
            return;

        int appliedValue = Math.Min(value, Math.Max(0, 999 - BattleMaxLife));
        if (appliedValue <= 0)
            return;

        BattleMaxLife += appliedValue;

        // Passive max-life gains recover an amount equal to the granted maximum life.
        Recover(appliedValue, source: source);
        AnimateLifeBarCapacityChange();

        TryPlayIncreasePropertyEffect();
        BuffHintLabel.Spawn(
            this,
            $"{Skill.GetColoredPropertyLabel(PropertyType.MaxLife)} +{appliedValue}",
            GlobalPosition + new Vector2(0, 150),
            randomOffset: true
        );
        InvalidateSkillTooltipCache();
        BattleNode?.RecordPropertyChange(this, PropertyType.MaxLife, appliedValue, source);
        BattleNode?.RefreshEnemyIntentionPreviews();
        BattleNode?.NotifyHandPreviewContextChanged();
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    public async Task IncreaseProperties(PropertyType type, int value, Character source = null)
    {
        int appliedValue = value;
        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                appliedValue +=
                    SpecialBuffs.Find(x => x.ThisBuffName == Buff.BuffName.ExtraPower)?.Stack ?? 0;
                BattlePower += appliedValue;
                icon = PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivability:
                appliedValue +=
                    SpecialBuffs
                        .Find(x => x.ThisBuffName == Buff.BuffName.ExtraSurvivability)
                        ?.Stack ?? 0;
                BattleSurvivability += appliedValue;
                icon = SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.MaxLife:
                return;
        }

        TryPlayIncreasePropertyEffect();

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            RefreshCombatStatIconVisibility();
            Buff.GhostExplode(icon, new Vector2(2f, 2f), useOffsetMotion: false);
        }

        BuffHintLabel.Spawn(
            this,
            $"{Skill.GetColoredPropertyLabel(type)} +{appliedValue}",
            GlobalPosition + new Vector2(0, 150),
            randomOffset: true
        );
        InvalidateSkillTooltipCache();
        BattleNode?.RecordPropertyChange(this, type, appliedValue, source);
        BattleNode?.RefreshEnemyIntentionPreviews();
        BattleNode?.NotifyHandPreviewContextChanged();
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    private void TryPlayIncreasePropertyEffect()
    {
        ulong now = Time.GetTicksMsec();
        if (
            _lastIncreasePropertyEffectTickMsec != 0
            && now - _lastIncreasePropertyEffectTickMsec < IncreasePropertyEffectCooldownMsec
        )
        {
            return;
        }

        _lastIncreasePropertyEffectTickMsec = now;

        CharacterEffect.Spawn(this, "absorb");
        AudioManager.PlayPropertyGain(this);
        // if (BattleNode != null && GodotObject.IsInstanceValid(BattleNode))
        // {
        //     BattleNode.BattleAnimationPlayer.Play("blue");
        // }
    }

    public void TriggerPassive(Skill skill, bool allowWhenDying = false)
    {
        if (!allowWhenDying && State == CharacterState.Dying)
            return;

        Passive(skill);
    }

    public virtual void Passive(Skill skill) { }

    protected bool IsExtraActionPhase => BattleNode?.IsResolvingExtraAction(this) == true;

    public virtual void OnActionStart() { }

    public virtual void OnActionEnd() { }

    public virtual void OnTurnStart() { }

    public virtual void OnTurnEnd() { }

    private void StopTween(ref Tween tween)
    {
        if (tween == null)
            return;

        if (GodotObject.IsInstanceValid(tween))
            tween.Kill();
        tween = null;
    }

    protected void SyncLifeBarsToCurrent(bool syncBufferValue)
    {
        if (LifeBar == null || BufferBar == null)
            return;

        double clampedLife = Math.Clamp(Life, 0, BattleMaxLife);
        LifeBar.MinValue = 0;
        BufferBar.MinValue = 0;
        LifeBar.MaxValue = BattleMaxLife;
        BufferBar.MaxValue = BattleMaxLife;
        LifeBar.Value = clampedLife;
        BufferBar.Value = syncBufferValue
            ? clampedLife
            : Math.Clamp(BufferBar.Value, 0, BattleMaxLife);
        LifeLabel.Text = $"{Life}/{BattleMaxLife}";
        RefreshLifeBarBlockColor();
    }

    private void RefreshLifeBarBlockColor()
    {
        if (LifeBar == null || BufferBar == null)
            return;

        CacheDefaultLifeBarFillStyles();
        bool hasBlock = Block > 0;
        if (_lifeBarBlockColorActive == hasBlock)
            return;

        _lifeBarBlockColorActive = hasBlock;
        if (hasBlock)
        {
            ApplyProgressBarFillColor(LifeBar, _defaultLifeBarFillStyle, BlockedLifeBarFillColor);
            ApplyProgressBarFillColor(
                BufferBar,
                _defaultBufferBarFillStyle,
                BlockedBufferBarFillColor
            );
            return;
        }

        RestoreProgressBarFillStyle(LifeBar, _defaultLifeBarFillStyle);
        RestoreProgressBarFillStyle(BufferBar, _defaultBufferBarFillStyle);
    }

    private void RefreshBlockDisplay(bool immediate = false)
    {
        CacheBlockLabelBaseScale();
        bool hasBlock = Block > 0;
        bool wasVisible = BlockLabel.Visible;
        BlockLabel.Text = Block.ToString();
        _blockDisplayTween?.Kill();

        if (immediate)
        {
            BlockLabel.Visible = hasBlock;
            BlockLabel.Scale = _blockLabelBaseScale;
            BlockLabel.Modulate = hasBlock ? Colors.White : new Color(1f, 1f, 1f, 0f);
            return;
        }

        if (hasBlock)
        {
            if (!wasVisible)
            {
                BlockLabel.Visible = true;
                BlockLabel.Scale = _blockLabelBaseScale * BlockDisplayShowScaleFactor;
                BlockLabel.Modulate = new Color(1f, 1f, 1f, 0f);

                _blockDisplayTween = CreateTween();
                _blockDisplayTween.SetParallel(true);
                _blockDisplayTween.TweenProperty(
                    BlockLabel,
                    "modulate",
                    Colors.White,
                    BlockDisplayFadeDuration
                );
                _blockDisplayTween
                    .TweenProperty(
                        BlockLabel,
                        "scale",
                        _blockLabelBaseScale,
                        BlockDisplayFadeDuration
                    )
                    .SetEase(Tween.EaseType.Out);
            }
            else
            {
                BlockLabel.Scale = _blockLabelBaseScale;
                BlockLabel.Modulate = Colors.White;
            }
            return;
        }

        if (!wasVisible)
        {
            BlockLabel.Scale = _blockLabelBaseScale;
            BlockLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            return;
        }

        if (BlockIcon != null)
        {
            Buff.GhostExplode(
                BlockIcon,
                BlockGhostExplodeScale,
                LifeBar,
                useOffsetMotion: false,
                removeFirstChild: false,
                alphaScale: BlockGhostExplodeAlphaScale
            );
        }
        BlockLabel.Visible = false;
        BlockLabel.Scale = _blockLabelBaseScale;
        BlockLabel.Modulate = Colors.White;
    }

    private void CacheBlockLabelBaseScale()
    {
        if (_blockLabelBaseScaleCached)
            return;

        _blockLabelBaseScale = BlockLabel.Scale == Vector2.Zero ? Vector2.One : BlockLabel.Scale;
        _blockLabelBaseScaleCached = true;
    }

    private void CacheDefaultLifeBarFillStyles()
    {
        _defaultLifeBarFillStyle ??=
            LifeBar.GetThemeStylebox("fill", "ProgressBar")?.Duplicate() as StyleBox;
        _defaultBufferBarFillStyle ??=
            BufferBar.GetThemeStylebox("fill", "ProgressBar")?.Duplicate() as StyleBox;
    }

    private static void ApplyProgressBarFillColor(
        ProgressBar bar,
        StyleBox defaultFillStyle,
        Color fillColor
    )
    {
        if (bar == null || defaultFillStyle == null)
            return;

        var fillStyle = defaultFillStyle.Duplicate() as StyleBox;
        if (fillStyle is StyleBoxFlat flat)
            flat.BgColor = fillColor;

        bar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    private static void RestoreProgressBarFillStyle(ProgressBar bar, StyleBox defaultFillStyle)
    {
        if (bar == null || defaultFillStyle == null)
            return;

        bar.AddThemeStyleboxOverride("fill", defaultFillStyle.Duplicate() as StyleBox);
    }

    private void AnimateLifeBarsAfterDamage(double duration = LifeBarDamageBufferCatchupDuration)
    {
        StopTween(ref _lifeBarTween);
        StopTween(ref _bufferBarTween);
        SyncLifeBarsToCurrent(syncBufferValue: false);
        LifeBar.Value = Life;

        _bufferBarTween = CreateTween();
        _bufferBarTween
            .TweenProperty(BufferBar, "value", Life, duration)
            .SetDelay(LifeBarDamageBufferHoldDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
        _bufferBarTween.TweenCallback(
            Callable.From(() =>
            {
                BufferBar.Value = Life;
                _bufferBarTween = null;
            })
        );
    }

    private void AnimateLifeBarsAfterRecover(double duration = 0.2)
    {
        StopTween(ref _lifeBarTween);
        StopTween(ref _bufferBarTween);
        SyncLifeBarsToCurrent(syncBufferValue: false);

        _lifeBarTween = CreateTween();
        _lifeBarTween.TweenProperty(LifeBar, "value", Life, duration);
        _lifeBarTween.TweenCallback(
            Callable.From(() =>
            {
                LifeBar.Value = Life;
                _lifeBarTween = null;
            })
        );

        _bufferBarTween = CreateTween();
        _bufferBarTween.TweenProperty(BufferBar, "value", Life, duration);
        _bufferBarTween.TweenCallback(
            Callable.From(() =>
            {
                BufferBar.Value = Life;
                _bufferBarTween = null;
            })
        );
    }

    private void AnimateLifeBarCapacityChange(double duration = 0.5)
    {
        StopTween(ref _lifeBarTween);
        StopTween(ref _bufferBarTween);
        StopTween(ref _lifeBarMaxTween);

        double startMax = LifeBar?.MaxValue ?? BattleMaxLife;
        SyncLifeBarsToCurrent(syncBufferValue: true);
        if (LifeBar == null || BufferBar == null)
            return;

        LifeBar.MaxValue = startMax;
        BufferBar.MaxValue = startMax;
        LifeBar.Value = Math.Clamp(Life, 0, BattleMaxLife);
        BufferBar.Value = Math.Clamp(Life, 0, BattleMaxLife);
        LifeLabel.Text = $"{Life}/{BattleMaxLife}";

        _lifeBarMaxTween = CreateTween();
        _lifeBarMaxTween.TweenMethod(
            Callable.From(
                (double value) =>
                {
                    LifeBar.MaxValue = value;
                    BufferBar.MaxValue = value;
                    LifeBar.Value = Math.Clamp(Life, 0, value);
                    BufferBar.Value = Math.Clamp(Life, 0, value);
                }
            ),
            startMax,
            BattleMaxLife,
            duration
        );
        _lifeBarMaxTween.TweenCallback(
            Callable.From(() =>
            {
                SyncLifeBarsToCurrent(syncBufferValue: true);
                _lifeBarMaxTween = null;
            })
        );
    }

    public void ShowTargetPreview(Color color, bool animate = true)
    {
        if (_isTargetPreviewVisible && _targetPreviewColor == color)
            return;

        bool shouldAnimate = !_isTargetPreviewVisible;
        _isTargetPreviewVisible = true;
        _targetPreviewColor = color;
        if (animate && shouldAnimate)
            Hover();
        RefreshHoverframeVisual();
    }

    public void HideTargetPreview()
    {
        _isTargetPreviewVisible = false;
        RefreshHoverframeVisual();
    }

    public void ShowFramePreview()
    {
        _isFramePreviewVisible = true;
        Hover();
        RefreshHoverframeVisual();
    }

    public void HideFramePreview()
    {
        _isFramePreviewVisible = false;
        RefreshHoverframeVisual();
    }

    public void SetCharacterShadowCardHover(bool hovered, bool instant = false)
    {
        if (!IsPlayer || CharacterShadow == null || !GodotObject.IsInstanceValid(CharacterShadow))
            return;

        CharacterShadow.SetCardHoverHighlight(hovered, instant);
    }

    private void RefreshHoverframeVisual()
    {
        if (_isTargetPreviewVisible)
        {
            // Tinting the whole frame through SelfModulate flattened the corner hardware into one
            // flat outline. Drive the shader palette instead and keep the modulate neutral.
            ApplyHoverframeTargetLock(_targetPreviewColor);
            Hoverframe.SelfModulate = new Color(1, 1, 1, 1);
            PlayHoverframeAssemble(true);
            return;
        }

        if (_isHoverframeHovered || _isFramePreviewVisible)
        {
            ApplyHoverframeInspectPalette();
            Hoverframe.SelfModulate = new Color(1, 1, 1, 1);
            PlayHoverframeAssemble(true);
            return;
        }

        PlayHoverframeAssemble(false);
    }

    /// <summary>
    /// Runs the corner brackets in or out. Hiding waits for the take-apart to finish before the
    /// frame goes fully transparent, so the pieces are seen retracting rather than blinking off.
    /// </summary>
    private void PlayHoverframeAssemble(bool assembled)
    {
        ShaderMaterial material = EnsureHoverframeMaterial();
        if (material == null)
        {
            if (!assembled)
                Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
            return;
        }

        if (_hoverframeAssembled == assembled)
        {
            // Not a transition: only reassert the hidden modulate once the retract has finished,
            // otherwise a redundant refresh would cut the animation short.
            if (!assembled && _hoverframeAssembleTween == null)
                Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
            return;
        }

        _hoverframeAssembled = assembled;
        _hoverframeAssembleTween?.Kill();

        float from = (float)material.GetShaderParameter("assemble");
        float to = assembled ? 1f : 0f;
        float duration = assembled ? 0.28f : 0.18f;

        _hoverframeAssembleTween = CreateTween();
        _hoverframeAssembleTween.SetTrans(Tween.TransitionType.Cubic);
        _hoverframeAssembleTween.SetEase(
            assembled ? Tween.EaseType.Out : Tween.EaseType.In
        );
        _hoverframeAssembleTween.TweenMethod(
            Callable.From<float>(SetHoverframeAssembleAmount),
            from,
            to,
            duration
        );
        _hoverframeAssembleTween.TweenCallback(
            Callable.From(assembled ? FinishHoverframeAssemble : FinishHoverframeDisassemble)
        );
    }

    private void SetHoverframeAssembleAmount(float value)
    {
        if (_hoverframeMaterial != null && GodotObject.IsInstanceValid(_hoverframeMaterial))
            _hoverframeMaterial.SetShaderParameter("assemble", value);
    }

    private void FinishHoverframeAssemble()
    {
        _hoverframeAssembleTween = null;
    }

    private void FinishHoverframeDisassemble()
    {
        _hoverframeAssembleTween = null;
        if (Hoverframe != null && GodotObject.IsInstanceValid(Hoverframe))
            Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
        ClearHoverframeTargetLock();
    }

    private ShaderMaterial EnsureHoverframeMaterial()
    {
        if (Hoverframe == null || !GodotObject.IsInstanceValid(Hoverframe))
            return null;

        if (_hoverframeMaterial != null && GodotObject.IsInstanceValid(_hoverframeMaterial))
            return _hoverframeMaterial;

        // The template's ShaderMaterial is a shared sub-resource, so every character would show
        // the same lock colour unless each one owns a copy.
        if (Hoverframe.Material is not ShaderMaterial shared)
            return null;

        _hoverframeMaterial = (ShaderMaterial)shared.Duplicate();
        _hoverframeMaterial.SetShaderParameter("assemble", 0f);
        _hoverframeBaseLineColor = ReadHoverframeColor(shared, "line_color", new Color(0.90f, 0.97f, 1f, 1f));
        _hoverframeBaseAccentColor = ReadHoverframeColor(shared, "accent_color", new Color(0.34f, 0.86f, 1f, 1f));
        _hoverframeBaseFillColor = ReadHoverframeColor(shared, "fill_color", new Color(0.03f, 0.16f, 0.24f, 0.08f));
        Hoverframe.Material = _hoverframeMaterial;
        return _hoverframeMaterial;
    }

    private static Color ReadHoverframeColor(ShaderMaterial material, string name, Color fallback)
    {
        Variant value = material.GetShaderParameter(name);
        return value.VariantType == Variant.Type.Color ? value.AsColor() : fallback;
    }

    /// <summary>
    /// Plain mouse-hover / frame-preview look. The scene material overrides accent_color to solid
    /// black, which collapses the shader's corner braces and inner line into an invisible mark and
    /// leaves a flat white box — this restores the cool console palette the shader was built for.
    /// </summary>
    private void ApplyHoverframeInspectPalette()
    {
        _hoverframeLockTween?.Kill();
        _hoverframeLockTween = null;

        ShaderMaterial material = EnsureHoverframeMaterial();
        if (material == null)
            return;

        material.SetShaderParameter("line_color", HoverframeInspectLineColor);
        material.SetShaderParameter("accent_color", HoverframeInspectAccentColor);
        material.SetShaderParameter("fill_color", HoverframeInspectFillColor);
        material.SetShaderParameter("scan_color", HoverframeInspectAccentColor);
        material.SetShaderParameter("lock_amount", 0.45f);
        material.SetShaderParameter("pulse", 0.0f);
    }

    private void ApplyHoverframeTargetLock(Color color)
    {
        ShaderMaterial material = EnsureHoverframeMaterial();
        if (material == null)
            return;

        // Two-layer read: the structural rails are lifted so they stay legible against dark art,
        // while the secondary rails and rivets keep the saturated hue that carries the meaning.
        Color line = color.Lerp(Colors.White, 0.45f) with { A = 1f };
        Color accent = color with { A = 1f };
        Color fill = color with { A = 0.13f };

        material.SetShaderParameter("line_color", line);
        material.SetShaderParameter("accent_color", accent);
        material.SetShaderParameter("fill_color", fill);
        material.SetShaderParameter("scan_color", line);
        material.SetShaderParameter("lock_amount", 1.0f);

        StartHoverframeLockPulse();
    }

    private void ClearHoverframeTargetLock()
    {
        _hoverframeLockTween?.Kill();
        _hoverframeLockTween = null;

        if (_hoverframeMaterial == null || !GodotObject.IsInstanceValid(_hoverframeMaterial))
            return;

        _hoverframeMaterial.SetShaderParameter("lock_amount", 0.0f);
        _hoverframeMaterial.SetShaderParameter("pulse", 0.0f);
        _hoverframeMaterial.SetShaderParameter("line_color", _hoverframeBaseLineColor);
        _hoverframeMaterial.SetShaderParameter("accent_color", _hoverframeBaseAccentColor);
        _hoverframeMaterial.SetShaderParameter("fill_color", _hoverframeBaseFillColor);
    }

    private void StartHoverframeLockPulse()
    {
        if (_hoverframeLockTween != null && _hoverframeLockTween.IsValid())
            return;

        if (_hoverframeMaterial == null || !GodotObject.IsInstanceValid(_hoverframeMaterial))
            return;

        _hoverframeLockTween = CreateTween();
        _hoverframeLockTween.SetLoops();
        _hoverframeLockTween.SetTrans(Tween.TransitionType.Sine);
        _hoverframeLockTween
            .TweenProperty(_hoverframeMaterial, "shader_parameter/pulse", 1.0f, 0.62f)
            .SetEase(Tween.EaseType.InOut);
        _hoverframeLockTween
            .TweenProperty(_hoverframeMaterial, "shader_parameter/pulse", 0.0f, 0.62f)
            .SetEase(Tween.EaseType.InOut);
    }

    private void CacheDefaultTrailGeometry()
    {
        if (TrailPath?.Curve != null && _defaultTrailCurve == null)
            _defaultTrailCurve = TrailPath.Curve.Duplicate() as Curve2D;

        if (TrailLine != null && !_hasDefaultTrailLinePosition)
        {
            _defaultTrailLinePosition = TrailLine.Position;
            _hasDefaultTrailLinePosition = true;
        }
    }

    private void RefreshTrailPreviewState()
    {
        if (trail == null)
            return;

        _nextActionPreviewTween?.Kill();
        _trailPreviewTween?.Kill();
        CacheDefaultTrailGeometry();

        if (_nextActionPreviewVisible)
        {
            RestoreDefaultTrailGeometry();
            if (TrailLineScript != null)
                TrailLineScript.ManualPreviewMode = false;
            TrailAnimation.Play("trail");
            ApplyNextActionPreviewTrailStyle();
            TweenTrailToColor(new Color(1f, 1f, 1f, _nextActionPreviewColor.A), 0.18f);
            return;
        }

        RestoreDefaultTrailGeometry();
        TweenTrailToColor(
            new Color(1f, 1f, 1f, 0f),
            0.14f,
            stopWhenFinished: true,
            restoreLineStyleWhenFinished: true
        );
    }

    private void TweenTrailToColor(
        Color color,
        float duration,
        bool stopWhenFinished = false,
        bool restoreLineStyleWhenFinished = false
    )
    {
        if (trail == null)
            return;

        _trailPreviewTween = CreateTween();
        _trailPreviewTween.TweenProperty(trail, "modulate", color, duration);
        if (!stopWhenFinished)
            return;

        _trailPreviewTween.Finished += () =>
        {
            if (
                !_nextActionPreviewVisible
                && !_customTrailPreviewVisible
                && BattleNode?.CurrentActionCharacter != this
            )
            {
                TrailAnimation.Stop();
            }

            if (restoreLineStyleWhenFinished)
                RestoreDefaultTrailLineStyle();
        };
    }

    private void ApplyNextActionPreviewTrailStyle()
    {
        if (TrailLine == null)
            return;

        CacheDefaultTrailLineStyle();
        RestoreDefaultTrailLineStyle();
        TrailLine.Gradient = CreateSolidTrailGradient(_nextActionPreviewColor);
        TrailLine.Modulate = Colors.White;
    }

    private void CacheDefaultTrailLineStyle()
    {
        if (_defaultTrailLineStyleCached || TrailLine == null)
            return;

        _defaultTrailLineGradient = TrailLine.Gradient;
        _defaultTrailLineModulate = TrailLine.Modulate;
        _defaultTrailLineStyleCached = true;
    }

    private void RestoreDefaultTrailLineStyle()
    {
        if (!_defaultTrailLineStyleCached || TrailLine == null)
            return;

        TrailLine.Gradient = _defaultTrailLineGradient;
        TrailLine.Modulate = _defaultTrailLineModulate;
    }

    private static Gradient CreateSolidTrailGradient(Color color)
    {
        Color opaqueColor = new(color.R, color.G, color.B, 1f);
        Color tailColor = new(color.R, color.G, color.B, 0.72f);
        var gradient = new Gradient();
        gradient.SetOffsets(new float[] { 0f, 1f });
        gradient.SetColors(new Color[] { tailColor, opaqueColor });
        return gradient;
    }

    private void ConfigureTrailCurveToTarget(Vector2 targetGlobalPosition)
    {
        if (TrailPath == null)
            return;

        CacheDefaultTrailGeometry();

        Vector2 start =
            _defaultTrailCurve?.PointCount > 0
                ? _defaultTrailCurve.GetPointPosition(0)
                : Vector2.Zero;
        Vector2 end = TrailPath.ToLocal(targetGlobalPosition);
        if (start.DistanceTo(end) <= 1f)
            return;

        Vector2 delta = end - start;
        float arcHeight = Mathf.Clamp(delta.Length() * 0.18f, 46f, 180f);
        Vector2 bend = Vector2.Up * arcHeight;
        Vector2 tangent = delta * 0.35f;

        Curve2D curve = new Curve2D();
        if (_defaultTrailCurve != null)
            curve.BakeInterval = _defaultTrailCurve.BakeInterval;
        curve.AddPoint(start, Vector2.Zero, tangent + bend);
        curve.AddPoint(end, -tangent + bend, Vector2.Zero);

        TrailPath.Curve = curve;
        if (TrailLineScript != null)
            TrailLineScript.ManualPreviewMode = true;
        if (TrailLine != null)
        {
            TrailLine.Position = Vector2.Zero;
            TrailLine.ClearPoints();
            Vector2[] bakedPoints = curve.GetBakedPoints();
            for (int i = 0; i < bakedPoints.Length; i++)
                TrailLine.AddPoint(bakedPoints[i]);
        }
        if (TrailFollow != null)
            TrailFollow.ProgressRatio = 0f;

        _trailUsesCustomCurve = true;
    }

    private void RestoreDefaultTrailGeometry()
    {
        if (!_trailUsesCustomCurve || TrailPath == null)
            return;

        if (_defaultTrailCurve != null)
            TrailPath.Curve = _defaultTrailCurve.Duplicate() as Curve2D;
        if (TrailLine != null)
        {
            if (TrailLineScript != null)
                TrailLineScript.ManualPreviewMode = false;
            if (_hasDefaultTrailLinePosition)
                TrailLine.Position = _defaultTrailLinePosition;
            TrailLine.ClearPoints();
        }
        if (TrailFollow != null)
            TrailFollow.ProgressRatio = 0f;

        _trailUsesCustomCurve = false;
    }

    private void RefreshCurvedTrailPreviewLine()
    {
        Line2D previewLine = EnsureCurvedTrailPreviewLine();
        if (previewLine == null)
            return;

        if (!_customTrailPreviewVisible || State == CharacterState.Dying || !IsInsideTree())
        {
            previewLine.Visible = false;
            previewLine.ClearPoints();
            return;
        }

        Vector2 start = GetCurvedTrailPreviewStartLocalPosition();
        Vector2 end = ToLocal(_customTrailPreviewTargetGlobalPosition);
        if (start.DistanceTo(end) <= 4f)
        {
            previewLine.Visible = false;
            previewLine.ClearPoints();
            return;
        }

        Vector2 delta = end - start;
        float arcHeight = Mathf.Clamp(delta.Length() * 0.16f, 60f, 180f);
        Vector2 control = (start + end) * 0.5f + Vector2.Up * arcHeight;

        previewLine.DefaultColor = _customTrailPreviewColor;
        previewLine.ClearPoints();
        const int sampleCount = 18;
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            previewLine.AddPoint(SampleQuadraticBezier(start, control, end, t));
        }

        previewLine.Visible = true;
    }

    private Line2D EnsureCurvedTrailPreviewLine()
    {
        if (_curvedTrailPreviewLine != null && GodotObject.IsInstanceValid(_curvedTrailPreviewLine))
            return _curvedTrailPreviewLine;

        _curvedTrailPreviewLine = new Line2D
        {
            Name = "CurvedTrailPreviewLine",
            Visible = false,
            Width = 8f,
            Antialiased = true,
            DefaultColor = _customTrailPreviewColor,
            ZIndex = 30,
            ZAsRelative = false,
            JointMode = Line2D.LineJointMode.Round,
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
        };
        AddChild(_curvedTrailPreviewLine);
        return _curvedTrailPreviewLine;
    }

    private Vector2 GetCurvedTrailPreviewStartLocalPosition()
    {
        if (Sprite != null && GodotObject.IsInstanceValid(Sprite))
            return Sprite.Position;

        return Vector2.Zero;
    }

    private static Vector2 SampleQuadraticBezier(
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

    public void Hover()
    {
        Hoverframe.SelfModulate = new Color(1, 1, 1, 1);
        Hoverframe.Scale = new Vector2(0.9f, 0.9f);
        Tween tween = CreateTween();
        tween
            .TweenProperty(Hoverframe, "scale", new Vector2(1.1f, 1.1f), 0.1f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        tween
            .TweenProperty(Hoverframe, "scale", new Vector2(1f, 1f), 0.2f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public async void PlayAnimatedSprite(AnimatedSprite2D animation)
    {
        animation.Frame = 0;
        Tween activetween = CreateTween();

        CreateTween().TweenProperty(animation, "modulate", new Color(1, 1, 1, 1), 0.15f);
        activetween.TweenProperty(
            animation,
            "frame",
            animation.SpriteFrames.GetFrameCount("default") - 1,
            0.5f
        );
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        CreateTween().TweenProperty(animation, "modulate", new Color(1, 1, 1, 0), 0.2f);
    }
}
