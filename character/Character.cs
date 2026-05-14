using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public partial class Character : Node2D
{
    private const int TurnStartEnergyGain = 3;

    public enum DamageKind
    {
        Other,
        Attack,
    }

    private const ulong IncreasePropertyEffectCooldownMsec = 300;
    private const int SkillTooltipDelayMs = 120;
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
            _state = value;
            if (_state == CharacterState.Dying)
                BattleNode?.ClearNextActionPreviewCharacter(this);
            RefreshBattleActionPoinUi();
        }
    }
    public BoxContainer StateIconContainer => field ??= GetNode<BoxContainer>("State");

    //charater basic properties
    [Export]
    public Texture2D Portrait;
    public virtual string CharacterName { get; set; }

    [Export]
    public int BattleMaxLife { get; private set; }
    public int Life { get; set; }

    public int BattlePower { get; private set; }

    public int BattleSurvivability { get; private set; }

    public int Speed { get; private set; }
    public int Block { get; protected set; }
    public int Energy { get; protected set; } = 1;

    public virtual string PassiveName { get; set; }
    public virtual string PassiveDescription { get; set; }

    //properties label
    public Label LifeLabel => field ??= GetNode("LifeBar/Life") as Label;
    public Label PowerLabel;
    public Label DefenseLabel;
    public Label SpeedLabel;
    public Label BlockLabel => field ??= GetNode<Label>("LifeBar/Block");
    public ProgressBar BlockBar => field ??= GetNode<ProgressBar>("LifeBar/BlockBar");
    public ProgressBar LifeBar => field ??= GetNode<ProgressBar>("LifeBar");
    public ProgressBar BufferBar => field ??= GetNode<ProgressBar>("LifeBar/BufferBar");
    public Label PowerIconLabel => field ??= GetNode<Label>("State/PowerIcon/Label");
    public Label SurvivabilityIconLabel =>
        field ??= GetNode<Label>("State/SurvivabilityIcon/Label");
    public Label SpeedIconLabel => field ??= GetNode<Label>("State/SpeedIcon/Label");
    public Label EnergeIconLabel => field ??= GetNode<Label>("State/EnergeIcon/Label");
    private Control TurnOrderPreviewRoot => field ??= GetNodeOrNull<Control>("TurnOrderPreview");
    private ColorRect TurnOrderPreviewCircle =>
        field ??= GetNodeOrNull<ColorRect>("TurnOrderPreview/Circle");
    public Label TurnOrderPreviewLabel => field ??= GetNodeOrNull<Label>("TurnOrderPreview/Value");
    public TextureRect Hoverframe => field ??= GetNode<TextureRect>("Hoverframe");
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
    public virtual bool CountsTowardTeamSpeed => true;
    public virtual bool TriggersSkillUseEvents => true;
    public virtual bool ClearsBlockOnActionStart => true;
    public List<SummonCharacter> Summons { get; } = new();

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
    private Tween _bufferBarTween;
    private Tween _lifeBarTween;
    private Tween _lifeBarMaxTween;
    private Tween _nextActionPreviewTween;
    private Tween _trailPreviewTween;
    private Tween _turnOrderPreviewTween;
    private bool _nextActionPreviewVisible;
    private bool _customTrailPreviewVisible;
    private Vector2 _customTrailPreviewTargetGlobalPosition;
    private Color _customTrailPreviewColor = new(1f, 0.42f, 0.32f, 0.78f);
    private Line2D _curvedTrailPreviewLine;
    private ulong _lastIncreasePropertyEffectTickMsec;
    private bool _isHoverframeHovered;
    private bool _isFramePreviewVisible;
    private bool _isTargetPreviewVisible;
    private Color _targetPreviewColor = Colors.White;
    private string _cachedSkillTooltipText;
    private string _cachedBuffTooltipText;
    private bool _skillTooltipCacheDirty = true;
    private bool _buffTooltipCacheDirty = true;
    private int _skillTooltipHoverVersion;
    private Curve2D _defaultTrailCurve;
    private Vector2 _defaultTrailLinePosition;
    private bool _hasDefaultTrailLinePosition;
    private bool _trailUsesCustomCurve;

    protected void SetCombatStats(int power, int survivability, int speed, int MaxLife)
    {
        BattlePower = power;
        BattleSurvivability = survivability;
        Speed = speed;
        BattleMaxLife = MaxLife;
    }

    public void ConfigureCombatStats(int power, int survivability, int speed, int maxLife)
    {
        SetCombatStats(power, survivability, speed, maxLife);
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

        BlockLabel.Text = Block.ToString();
        Life = BattleMaxLife;
        SyncLifeBarsToCurrent(syncBufferValue: true);
        PowerIconLabel.Text = BattlePower.ToString();
        SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
        EnergeIconLabel.Text = Energy.ToString();
        SpeedIconLabel.Text = Speed.ToString();

        Block = 0;
        BlockLabel.Text = Block.ToString();

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
    }

    public override async void _Ready()
    {
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
        var effect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(effect);
        effect.Animation.Play("transition");
    }

    private void OnHoverEntered()
    {
        ulong hoverStartUsec = Time.GetTicksUsec();
        _isHoverframeHovered = true;
        _skillTooltipHoverVersion++;
        BattleNode?.MarkHoverPerfEvent(this, "character-hover-enter");

        ulong stepStartUsec = Time.GetTicksUsec();
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
        string name = string.IsNullOrWhiteSpace(CharacterName) ? "Character" : CharacterName;
        sb.Append($"[b]{name}[/b]\n");

        AppendPassiveTooltip(sb);

        if (this is PlayerCharacter player)
        {
            AppendPlayerBattleCardPiles(sb, player);
            return sb.ToString().TrimEnd();
        }

        if (Skills == null || Skills.Length == 0)
            return sb.ToString().TrimEnd();

        const string separator = "[hr]\n";
        const string skillNameColor = "#b56bff";
        const int skillNameFontSize = 32;

        var validSkills = Skills.Where(x => x != null).ToArray();
        for (int i = 0; i < validSkills.Length; i++)
        {
            var skill = validSkills[i];

            skill.UpdateDescription();

            if (i > 0)
                sb.Append('\n');

            sb.Append(
                $"[font_size={skillNameFontSize}][color={skillNameColor}]{skill.SkillName}[/color][/font_size]  [color=#cccccc]({skill.SkillType.GetDescription()})[/color]\n"
            );
            sb.Append($"[color=#87ceeb]耗能[/color] {skill.CardEnergyCostText}\n");
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

    private static void AppendPlayerBattleCardPiles(StringBuilder sb, PlayerCharacter player)
    {
        if (player == null)
            return;

        SkillID[] drawPileIds =
            player.BattleNode?.GetDrawBattleCardPile(player)
            ?? GetOwnedPlayerSkillIds(player.CharacterIndex);
        SkillID[] discardPileIds =
            player.BattleNode?.GetDiscardBattleCardPile(player) ?? Array.Empty<SkillID>();
        SkillID[] exhaustedIds =
            player.BattleNode?.GetExhaustedBattleCardPile(player) ?? Array.Empty<SkillID>();

        const string skillNameColor = "#b56bff";
        const int skillNameFontSize = 32;
        sb.Append(
            $"[font_size={skillNameFontSize}][color={skillNameColor}]抽牌堆[/color][/font_size]\n"
        );
        AppendSkillPileLines(sb, GetSkillsFromIds(drawPileIds), emptyText: "空");

        sb.Append("\n[hr]\n");
        sb.Append(
            $"[font_size={skillNameFontSize}][color=#9cdacf]弃牌堆[/color][/font_size]\n"
        );
        AppendSkillPileLines(sb, GetSkillsFromIds(discardPileIds), emptyText: "空");

        sb.Append("\n[hr]\n");
        sb.Append(
            $"[font_size={skillNameFontSize}][color=#ffb86b]消耗卡堆[/color][/font_size]\n"
        );
        AppendSkillPileLines(sb, GetSkillsFromIds(exhaustedIds), emptyText: "空");
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

        return (GameInfo.PlayerCharacters[characterIndex].GainedSkills ?? new List<SkillID>())
            .ToArray();
    }

    private static Skill[] GetSkillsFromIds(IEnumerable<SkillID> skillIds)
    {
        return (skillIds ?? Array.Empty<SkillID>())
            .Select(Skill.GetSkill)
            .Where(skill => skill != null && skill.SkillType != Skill.SkillTypes.none)
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
        const int titleFontSize = 30;

        string title = string.IsNullOrWhiteSpace(passiveName) ? "Passive" : passiveName;
        sb.Append(
            $"[font_size={titleFontSize}][color={passiveColor}]{title}[/color][/font_size]  [color=#cccccc](被动)[/color]\n"
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
    }

    public void InvalidateBuffTooltipCache()
    {
        _buffTooltipCacheDirty = true;
        _cachedBuffTooltipText = null;
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
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (EndActionBuffs != null)
        {
            foreach (var buff in EndActionBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (SkillBuffs != null)
        {
            foreach (var buff in SkillBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (AttackBuffs != null)
        {
            foreach (var buff in AttackBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (SpecialBuffs != null)
        {
            foreach (var buff in SpecialBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (HurtBuffs != null)
        {
            foreach (var buff in HurtBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (DyingBuffs != null)
        {
            foreach (var buff in DyingBuffs.Where(x => x != null && x.Stack > 0))
            {
                sb.Append($"{Buff.GetBuffDisplayName(buff.ThisBuffName)} x{buff.Stack}\n");
                var effect = Buff.GetBuffEffectText(buff.ThisBuffName);
                if (!string.IsNullOrWhiteSpace(effect))
                    sb.Append($"[color={colord}]{effect}[/color]\n");
                any = true;
            }
        }

        if (!any)
            sb.Append("None");

        return GlobalFunction.ColorizeNumbers(sb.ToString().TrimEnd());
    }

    public override void _Process(double delta)
    {
        if (Block > 0)
        {
            BlockBar.Visible = true;
        }
        else
        {
            BlockBar.Visible = false;
        }

        if (StartActionBuffs.Any(x => x.ThisBuffName == Buff.BuffName.Invisible))
        {
            Sprite.SelfModulate = new Color(0.8f, 0.8f, 1f, 0.95f);
        }
        else
        {
            Sprite.SelfModulate = new Color(1f, 1f, 1f, 1f);
        }
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
        ResolveTurnStartPhase();

        OnActionStart();
        _customTrailPreviewVisible = false;
        RefreshCurvedTrailPreviewLine();
        TrailAnimation.Play("trail");
        _nextActionPreviewVisible = false;
        _nextActionPreviewTween?.Kill();
        _trailPreviewTween?.Kill();
        RestoreDefaultTrailGeometry();
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 1f), 0.2f);
    }

    public void ShowNextActionPreview()
    {
        if (trail == null || State == CharacterState.Dying)
            return;

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
                .SetTrans(Tween.TransitionType.Back);
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
        root.PivotOffset = root.Size / 2f;

        if (TurnOrderPreviewCircle?.Material is ShaderMaterial material)
        {
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
        Color accent = isCurrent
            ? new Color(1f, 0.84f, 0.33f, 1f)
            : IsPlayer
                ? new Color(0.42f, 0.88f, 1f, 1f)
                : new Color(1f, 0.47f, 0.42f, 1f);
        Color fill = isCurrent
            ? new Color(0.22f, 0.16f, 0.05f, 0.92f)
            : IsPlayer
                ? new Color(0.05f, 0.14f, 0.22f, 0.88f)
                : new Color(0.2f, 0.07f, 0.08f, 0.88f);
        Color glow = accent with { A = isCurrent ? 0.95f : 0.72f };
        Color valueColor = isCurrent
            ? new Color(1f, 0.95f, 0.78f, 1f)
            : new Color(0.97f, 0.99f, 1f, 0.98f);

        if (TurnOrderPreviewCircle?.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("fill_color", fill);
            material.SetShaderParameter("rim_color", accent);
            material.SetShaderParameter("glow_color", glow);
            material.SetShaderParameter("ring_width", isCurrent ? 0.1f : 0.085f);
            material.SetShaderParameter("glow_size", isCurrent ? 0.16f : 0.11f);
            material.SetShaderParameter("center_brightness", isCurrent ? 0.26f : 0.15f);
        }

        if (TurnOrderPreviewLabel != null)
        {
            TurnOrderPreviewLabel.AddThemeColorOverride("font_color", valueColor);
            TurnOrderPreviewLabel.AddThemeColorOverride(
                "font_outline_color",
                new Color(0.015f, 0.025f, 0.04f, 1f)
            );
            TurnOrderPreviewLabel.AddThemeConstantOverride("outline_size", isCurrent ? 7 : 6);
            TurnOrderPreviewLabel.AddThemeFontSizeOverride("font_size", isCurrent ? 30 : 28);
        }
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

        if (ClearsBlockOnActionStart)
        {
            bool keepBlock =
                StartActionBuffs != null
                && StartActionBuffs.Any(x =>
                    x != null
                    && x.Stack > 0
                    && StartActionBuff.KeepsBlockOnTurnStart(x.ThisBuffName)
                );

            if (!keepBlock)
            {
                Block = 0;
                UpdataBlock(0);
            }

            ClearOwnedSummonsBlock();
        }

        UpdataEnergy(TurnStartEnergyGain);
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
        await ResolveTurnEndPhaseAsync();

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

    public virtual async Task GetHurt(
        float damage,
        Character source = null,
        DamageKind damageKind = DamageKind.Other
    )
    {
        Sprite.Modulate = 1.5f * new Color(1, 1, 1, 1);
        HitParticle hitParticle = HitParticleScene.Instantiate<HitParticle>();
        AddChild(hitParticle);

        if (HurtBuffs != null)
        {
            // Hurt buffs can remove themselves from the list when triggered (Stack reaches 0).
            // Iterate over a snapshot to ensure later buffs (e.g. DamageImmune) still trigger.
            foreach (var buff in HurtBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                damage = await buff.Trigger(damage, source, damageKind);
            }
        }

        global::Number.Spawn(this, (-(int)damage).ToString(), Colors.Red);

        BattleNode.BattleAnimationPlayer.Play("hit");

        int incomingDamage = Math.Max((int)damage, 0);
        int previousBlock = Block;
        int previousLife = Life;
        int blockedDamage = Math.Clamp(Math.Min(incomingDamage, previousBlock), 0, incomingDamage);
        int actualDamage = Math.Clamp(incomingDamage - previousBlock, 0, previousLife);

        Life -= Math.Clamp((int)damage - Block, 0, Life);
        Block = Math.Clamp(Block - (int)damage, 0, 99999);
        UpdataBlock(0);
        AnimateLifeBarsAfterDamage();
        BattleNode?.RecordDamage(this, actualDamage, blockedDamage, source);

        PlayHurtMoveTween();
        Tween tween = CreateTween();
        tween.TweenInterval(0.2f);
        tween.TweenCallback(Callable.From(() => Sprite.Modulate = new Color(1, 1, 1, 1)));

        if (Life == 0)
        {
            await Dying(source);
        }
    }

    private void PlayHurtMoveTween()
    {
        Vector2 hurtOffset = IsPlayer ? 20 * Vector2.Left : 20 * Vector2.Right;
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
        _hurtMoveTween.Finished += () => _hurtMoveTween = null;
    }

    public virtual void Recover(int num, bool rebirth = false, Character source = null)
    {
        int heal = Math.Clamp(num + BattleSurvivability, 0, 999);
        ApplyRecover(heal, rebirth, source, canRevive: num > 0);
    }

    private void ApplyRecover(int heal, bool rebirth, Character source, bool canRevive)
    {
        if (State == CharacterState.Dying && !rebirth)
            return;

        int previousLife = Life;
        Life = Math.Clamp(Life + heal, 0, BattleMaxLife);
        int actualHeal = Life - previousLife;
        AnimateLifeBarsAfterRecover();
        global::Number.Spawn(this, heal.ToString("+0;-0;0"), heal >= 0 ? Colors.Green : Colors.Red);

        var effect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(effect);
        effect.Animation.Play("recover");
        if (State == CharacterState.Dying && canRevive && Life > 0)
        {
            State = CharacterState.Normal;
            CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.4f);
        }

        BattleNode?.RecordHeal(this, actualHeal, source);
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
                BattleNode?.HandleCharacterEnteredDying(this);
            TriggerOwnedSummonsDying();
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

    public virtual void UpdataEnergy(int num, Character source = null)
    {
        Energy += num;
        EnergeIconLabel.Text = Energy.ToString();
        InvalidateSkillTooltipCache();
        var Effect = CharacterEffectScene.Instantiate<CharacterEffect>();
        Effect.Position = new Vector2(0, -50);
        AddChild(Effect);
        Effect.Animation.Play("energe");
        BuffHintLabel.Spawn(this, $"[color=#87CEEB]Energy[/color] {num:+0;-0;0}", GlobalPosition);

        if (source != null || BattleNode?.HasEffectSourceContext == true)
            BattleNode?.RecordEnergyChange(this, num, source);
    }

    public void UpdataBlock(int num, bool record = true, Character source = null)
    {
        if (State == CharacterState.Dying)
            return;
        if (num > 0)
        {
            CharacterEffect characterEffect = CharacterEffectScene.Instantiate<CharacterEffect>();
            AddChild(characterEffect);
            characterEffect.Animation.Play("shield");
        }
        Block = Math.Clamp(Block + num, 0, 999);
        BlockLabel.Text = Block.ToString();

        if (num > 0)
        {
            global::Number.Spawn(this, "+" + num.ToString(), new Color(180, 220, 255, 255) / 255);
            if (record)
                BattleNode?.RecordBlockGain(this, num, source);
        }
    }

    public async Task DescendingProperties(PropertyType type, int value, Character source = null)
    {
        if (value == 0)
            return;

        if (value > 0 && SpecialBuff.TryConsumeDebuffImmunity(this))
        {
            BuffHintLabel.Spawn(
                this,
                $"{Buff.BuffName.DebuffImmunity.GetDescription()} [color=yellow]抵消[/color]",
                GlobalPosition + new Vector2(0, 150),
                randomOffset: true
            );
            return;
        }

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
            case PropertyType.Speed:
                Speed -= value;
                icon = SpeedIconLabel.GetParent() as ColorRect;
                RefreshBattleActionPoinUi();
                break;
            case PropertyType.MaxLife:
                BattleMaxLife -= value;
                Life = Math.Min(Life, BattleMaxLife);
                AnimateLifeBarCapacityChange();
                break;
        }

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            SpeedIconLabel.Text = Speed.ToString();
            Buff.GhostExplode(icon, new Vector2(2f, 2f), useOffsetMotion: false);
        }

        CharacterEffect characterEffect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(characterEffect);
        characterEffect.Animation.Play("lightning");

        BuffHintLabel.Spawn(
            this,
            $"{Skill.GetColoredPropertyLabel(type)} -{value}",
            GlobalPosition + new Vector2(0, 150),
            randomOffset: true
        );
        InvalidateSkillTooltipCache();
        BattleNode?.RecordPropertyChange(this, type, -value, source);
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
            case PropertyType.Speed:
                Speed += appliedValue;
                icon = SpeedIconLabel.GetParent() as ColorRect;
                RefreshBattleActionPoinUi();
                break;
            case PropertyType.MaxLife:
                BattleMaxLife += appliedValue;
                AnimateLifeBarCapacityChange();
                break;
        }

        TryPlayIncreasePropertyEffect();

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            SpeedIconLabel.Text = Speed.ToString();
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
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    private void RefreshBattleActionPoinUi()
    {
        if (BattleNode == null || !GodotObject.IsInstanceValid(BattleNode))
            return;

        BattleNode.RequestActionPoinUiRefresh(IsPlayer);
        BattleNode.RefreshTurnOrderPreview();
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

        CharacterEffect characterEffect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(characterEffect);
        characterEffect.Animation.Play("absorb");
        if (BattleNode != null && GodotObject.IsInstanceValid(BattleNode))
        {
            BattleNode.BattleAnimationPlayer.Play("blue");
        }
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

    public virtual void OnTurnEnd()
    {
        if (IsPlayer && Energy > 0)
            UpdataEnergy(-Energy, this);
    }

    private void StopTween(ref Tween tween)
    {
        if (tween == null)
            return;

        if (GodotObject.IsInstanceValid(tween))
            tween.Kill();
        tween = null;
    }

    private void SyncLifeBarsToCurrent(bool syncBufferValue)
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
    }

    private void AnimateLifeBarsAfterDamage(double duration = 0.2)
    {
        StopTween(ref _lifeBarTween);
        StopTween(ref _bufferBarTween);
        SyncLifeBarsToCurrent(syncBufferValue: false);
        LifeBar.Value = Life;

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

    public void ShowTargetPreview(Color color)
    {
        _isTargetPreviewVisible = true;
        _targetPreviewColor = color;
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

    private void RefreshHoverframeVisual()
    {
        if (_isHoverframeHovered)
        {
            Hoverframe.SelfModulate = new Color(1, 1, 1, 1);
            return;
        }

        if (_isFramePreviewVisible)
        {
            Hoverframe.SelfModulate = new Color(1, 1, 1, 1);
            return;
        }

        if (_isTargetPreviewVisible)
        {
            Hoverframe.SelfModulate = _targetPreviewColor;
            return;
        }

        Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
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
            TweenTrailToColor(new Color(0.2f, 1f, 0.25f, 0.78f), 0.18f);
            return;
        }

        RestoreDefaultTrailGeometry();
        TweenTrailToColor(
            new Color(trail.Modulate.R, trail.Modulate.G, trail.Modulate.B, 0f),
            0.14f,
            stopWhenFinished: true
        );
    }

    private void TweenTrailToColor(Color color, float duration, bool stopWhenFinished = false)
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
        };
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
        Hoverframe.Size = new Vector2(0.9f, 0.9f);
        Tween tween = CreateTween();
        tween
            .TweenProperty(Hoverframe, "scale", new Vector2(1.1f, 1.1f), 0.1f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        tween
            .TweenProperty(Hoverframe, "scale", new Vector2(1f, 1f), 0.2f)
            .SetTrans(Tween.TransitionType.Back)
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
