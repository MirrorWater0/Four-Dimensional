using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

[Serializable]
public partial class Character : Node2D
{
    [Export]
    public bool WarmupMode { get; set; }

    public int CharacterIndex;

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
            if (BattleNode == null)
                return;

            if (IsPlayer)
                BattleNode.PlayerSpeed = BattleNode.PlayerSpeed;
            else
                BattleNode.EnemySpeed = BattleNode.EnemySpeed;
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

    [Export]
    private int _battlePower;
    public int BattlePower
    {
        get => _battlePower;
        private set => _battlePower = value;
    }

    [Export]
    private int _battleSurvivability;
    public int BattleSurvivability
    {
        get => _battleSurvivability;
        private set => _battleSurvivability = value;
    }

    [Export]
    private int _speed;
    public int Speed
    {
        get => _speed;
        private set => _speed = value;
    }
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
    public TextureRect Hoverframe => field ??= GetNode<TextureRect>("Hoverframe");
    public AnimatedSprite2D absorb => field ??= GetNode<AnimatedSprite2D>("Effect/absorb");
    public AnimatedSprite2D shield => field ??= GetNode<AnimatedSprite2D>("Effect/shield");

    [Export]
    public Node2D Sprite;
    public AnimationPlayer TrailAnimation => field ??= GetNode<AnimationPlayer>("TrailAnimation");
    public Node2D trail => field ??= GetNode<Node2D>("Path2D");

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

    //buff

    public List<DyingBuff> DyingBuffs = new List<DyingBuff>();
    public List<HurtBuff> HurtBuffs = new List<HurtBuff>();
    public List<StartActionBuff> StartActionBuffs = new List<StartActionBuff>();
    public List<SpecialBuff> SpecialBuffs = new List<SpecialBuff>();
    private Tip SkillTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");
    private Tip BuffTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/BuffTip");
    public Vector2 OriginalPosition;

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
        for (int i = 0; i < Skills.Length; i++)
        {
            Skills[i].OwnerCharater = this;
            Skills[i].UpdateDescription();
        }
        //初始化数值
        State = CharacterState.Normal;

        BlockLabel.Text = Block.ToString();
        Life = BattleMaxLife;

        LifeBar.MaxValue = BattleMaxLife;
        BufferBar.MaxValue = BattleMaxLife;
        LifeBar.MinValue = 0;
        BufferBar.MinValue = 0;
        LifeBar.Value = Life;
        BufferBar.Value = Life;
        PowerIconLabel.Text = BattlePower.ToString();
        SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
        EnergeIconLabel.Text = Energy.ToString();
        SpeedIconLabel.Text = Speed.ToString();
        LifeLabel.Text = Life.ToString() + "/" + BattleMaxLife.ToString();

        Block = 0;
        BlockLabel.Text = Block.ToString();

        Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
        Hoverframe.PivotOffset = Hoverframe.Size / 2;
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
        Hover();
        if (State == CharacterState.Dying)
            return;
        ShowHoverTooltips();
    }

    private void OnHoverExited()
    {
        Hoverframe.SelfModulate = new Color(1, 1, 1, 0);
        HideHoverTooltips();
    }

    private void ShowHoverTooltips()
    {
        if (State == CharacterState.Dying)
        {
            HideHoverTooltips();
            return;
        }

        if (SkillTooltip != null)
        {
            SkillTooltip.FollowMouse = true;
            SkillTooltip.Description.Text = BuildSkillTooltipText();
            SkillTooltip.Visible = true;
        }

        if (BuffTooltip != null)
        {
            BuffTooltip.FollowMouse = true;
            BuffTooltip.Description.Text = BuildBuffTooltipText();
            BuffTooltip.Visible = true;
        }
    }

    private void HideHoverTooltips()
    {
        if (SkillTooltip != null)
            SkillTooltip.Visible = false;
        if (BuffTooltip != null)
            BuffTooltip.Visible = false;
    }

    private string BuildSkillTooltipText()
    {
        var sb = new StringBuilder(256);
        string name = string.IsNullOrWhiteSpace(CharacterName) ? "Character" : CharacterName;
        sb.Append($"[b]{name}[/b]\n");

        AppendPassiveTooltip(sb);

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
        return BuildSkillTooltipText();
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
                sb.Append($"{buff.ThisBuffName.GetDescription()} x{buff.Stack}\n");
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
                sb.Append($"{buff.ThisBuffName.GetDescription()} x{buff.Stack}\n");
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
                sb.Append($"{buff.ThisBuffName.GetDescription()} x{buff.Stack}\n");
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
                sb.Append($"{buff.ThisBuffName.GetDescription()} x{buff.Stack}\n");
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

    public virtual async void StartAction()
    {
        Block = 0;
        UpdataBlock(0);
        UpdataEnergy(1);
        OnTurnStart();
        TrailAnimation.Play("trail");
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 1f), 0.2f);

        if (StartActionBuffs.Any(x => x.ThisBuffName == Buff.BuffName.Stun))
        {
            if (this is EnemyCharacter enemy)
            {
                await enemy.DisappearIntention();
            }
            StartActionBuffs.First(x => x.ThisBuffName == Buff.BuffName.Stun).Trigger();
            var effect = CharacterEffectScene.Instantiate<CharacterEffect>();
            AddChild(effect);
            effect.Animation.Play("stun");
            await ToSignal(effect.Animation, "animation_finished");
            EndAction();
            return;
        }
        if (StartActionBuffs != null)
        {
            // Buffs can remove themselves from the list when triggered (Stack reaches 0).
            // Iterate over a snapshot to avoid skipping the next buff due to index shifting.
            foreach (var buff in StartActionBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                buff.Trigger();
            }
        }
    }

    public virtual async void EndAction()
    {
        await BattleNode.EmitS(this);
        CreateTween().TweenProperty(trail, "modulate", new Color(1, 0, 0, 0), 0.2f);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        TrailAnimation.Stop();
    }

    public virtual async Task GetHurt(float damage)
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
                buff.Trigger(ref damage);
            }
        }

        var attacknum = Number.Instantiate<Number>();
        AddChild(attacknum);
        attacknum.NumberLabel.Text = (-(int)damage).ToString();

        BattleNode.BattleAnimationPlayer.Play("hit");

        Life -= Math.Clamp((int)damage - Block, 0, Life);
        Block = Math.Clamp(Block - (int)damage, 0, 99999);
        UpdataBlock(0);

        Tween tween = CreateTween();
        tween.TweenProperty(BufferBar, "value", Life, 0.2f);
        LifeBar.Value = Life;
        LifeLabel.Text = Life.ToString() + "/" + BattleMaxLife.ToString();

        tween.TweenCallback(Callable.From(() => Sprite.Modulate = new Color(1, 1, 1, 1)));
        if (Life == 0)
        {
            await Dying();
        }
    }

    public virtual void Recover(int num, bool rebirth = false)
    {
        if (State == CharacterState.Dying && !rebirth)
            return;

        num = Math.Clamp(num + BattleSurvivability, 0, 999);
        Life = Math.Clamp(Life + num, 0, BattleMaxLife);
        CreateTween().TweenProperty(BufferBar, "value", Life, 0.2f);
        CreateTween().TweenProperty(LifeBar, "value", Life, 0.2f);
        LifeLabel.Text = Life.ToString() + "/" + BattleMaxLife.ToString();
        var numlabel = Number.Instantiate<Number>();
        AddChild(numlabel);
        numlabel.NumberLabel.Text = num.ToString("+0;-0;0");
        numlabel.SetNumberColor(num >= 0 ? Colors.Green : Colors.Red);

        var effect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(effect);
        effect.Animation.Play("recover");
        if (State == CharacterState.Dying && num > 0)
        {
            State = CharacterState.Normal;
            CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.4f);
        }
    }

    public virtual async Task Dying()
    {
        State = CharacterState.Dying;

        CreateTween().TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.4f);
        if (DyingBuffs != null)
            // Dying buffs can remove themselves from the list when triggered (Stack reaches 0).
            // Iterate over a snapshot to avoid skipping subsequent buffs.
            foreach (var buff in DyingBuffs.Where(x => x != null && x.Stack > 0).ToArray())
            {
                await buff.Trigger();
            }
    }

    public virtual void DisableSkill() { }

    public virtual void UpdataEnergy(int num)
    {
        Energy += num;
        EnergeIconLabel.Text = Energy.ToString();
        var Effect = CharacterEffectScene.Instantiate<CharacterEffect>();
        Effect.Position = new Vector2(0, -50);
        AddChild(Effect);
        Effect.Animation.Play("energe");
        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"[color=#87CEEB]Energy[/color] {num:+0;-0;0}";
        hint.TargetPosition = GlobalPosition;
        AddChild(hint);
    }

    public void UpdataBlock(int num)
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
            Number number = Number.Instantiate<Number>();
            AddChild(number);
            number.NumberLabel.Text = "+" + num.ToString();
            number.SetNumberColor(new Color(180, 220, 255, 255) / 255);
        }
    }

    public async Task DescendingProperties(PropertyType type, int value)
    {
        if (value == 0)
            return;

        if (value > 0 && SpecialBuff.TryConsumeDebuffImmunity(this))
        {
            var immunityHint = Buff.HintScene.Instantiate<BuffHintLabel>();
            immunityHint.Text =
                $"{Buff.BuffName.DebuffImmunity.GetDescription()} [color=yellow]抵消[/color]";
            immunityHint.TargetPosition = GlobalPosition + new Vector2(0, 150);
            immunityHint.RandomOffset = true;
            AddChild(immunityHint);
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
                break;
            case PropertyType.MaxLife:
                BattleMaxLife -= value;
                Life = Math.Min(Life, BattleMaxLife);
                LifeLabel.Text = $"{Life}/{BattleMaxLife}";
                CreateTween()
                    .TweenMethod(
                        Callable.From((int x) => LifeBar.MaxValue = x),
                        LifeBar.MaxValue,
                        BattleMaxLife,
                        0.5f
                    );
                break;
        }

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            SpeedIconLabel.Text = Speed.ToString();
            Buff.GhostExplode(icon, new Vector2(2f, 2f));
        }

        CharacterEffect characterEffect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(characterEffect);
        characterEffect.Animation.Play("lightning");

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{Skill.GetColoredPropertyLabel(type)} -{value}";
        hint.TargetPosition = GlobalPosition + new Vector2(0, 150);
        hint.RandomOffset = true;
        AddChild(hint);
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    public async Task IncreaseProperties(PropertyType type, int value)
    {
        if (value == 0)
            return;

        ColorRect icon = null;
        switch (type)
        {
            case PropertyType.Power:
                BattlePower += value;
                icon = PowerIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Survivability:
                BattleSurvivability += value;
                icon = SurvivabilityIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.Speed:
                Speed += value;
                icon = SpeedIconLabel.GetParent() as ColorRect;
                break;
            case PropertyType.MaxLife:
                BattleMaxLife += value;
                LifeLabel.Text = $"{Life}/{BattleMaxLife}";
                CreateTween()
                    .TweenMethod(
                        Callable.From((int x) => LifeBar.MaxValue = x),
                        LifeBar.MaxValue,
                        BattleMaxLife,
                        0.5f
                    );
                break;
        }

        CharacterEffect characterEffect = CharacterEffectScene.Instantiate<CharacterEffect>();
        AddChild(characterEffect);
        characterEffect.Animation.Play("absorb");
        if (BattleNode != null && GodotObject.IsInstanceValid(BattleNode))
        {
            BattleNode.BattleAnimationPlayer.Play("blue");
        }

        if (icon != null)
        {
            PowerIconLabel.Text = BattlePower.ToString();
            SurvivabilityIconLabel.Text = BattleSurvivability.ToString();
            SpeedIconLabel.Text = Speed.ToString();
            Buff.GhostExplode(icon, new Vector2(2f, 2f));
        }

        var hint = Buff.HintScene.Instantiate<BuffHintLabel>();
        hint.Text = $"{Skill.GetColoredPropertyLabel(type)} +{value}";
        hint.TargetPosition = GlobalPosition + new Vector2(0, 150);
        hint.RandomOffset = true;
        AddChild(hint);
        await ToSignal(GetTree().CreateTimer(0.01f), "timeout");
    }

    public virtual void Passive(Skill skill) { }

    protected virtual void OnTurnStart() { }

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
