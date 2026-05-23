using System.Threading.Tasks;
using Godot;

public partial class CardSlot : Control
{
    private static readonly Shader SkillRewardIconShader = GD.Load<Shader>(
        "res://shader/Effect/SkillRewardIcon.gdshader"
    );
    private static readonly Shader TalentPointRewardIconShader = GD.Load<Shader>(
        "res://shader/Effect/TalentPointReward.gdshader"
    );
    private static readonly Color DefaultBorderColor = new("#a7d6ff52");
    private static readonly Color HoverBorderColor = new("#5cff8a");
    private static readonly Color SelectedBorderColor = Colors.Yellow;
    private static readonly Color DisabledBorderColor = new("#5e6f8670");
    private static readonly Color RejectBorderColor = new("#ff465f");
    private static readonly Color RejectModulate = new(1f, 0.45f, 0.45f, 1f);
    private static readonly Color EnabledModulate = Colors.White;
    private static readonly Color DisabledModulate = new(0.62f, 0.68f, 0.78f, 0.78f);
    private const float RejectShakeOffset = 10f;
    private const float RejectShakeStepDuration = 0.045f;
    private const float RewardIconSize = 68f;
    private const float TalentPointIconSize = 57f;
    private const float RewardIconLeftPadding = 18f;
    private const float RewardTextLeftPadding = 104f;
    private const float RewardTextRightPadding = 18f;

    [Export]
    public float HoverBorderTweenDuration = 0.12f;

    [Export]
    public float ParticleBoxPaddingX = 80f;

    [Export]
    public float ParticleYOffset = 12f;

    [Signal]
    public delegate void ClickedEventHandler();
    public Label label => field ??= GetNodeOrNull<Label>("Panel/Label");
    private GpuParticles2D Particles => field ??= GetNodeOrNull<GpuParticles2D>("GPUParticles2D");
    private Panel Panel => field ??= GetNodeOrNull<Panel>("Panel");

    public override void _Ready()
    {
        if (label != null)
            label.MouseFilter = MouseFilterEnum.Ignore;
        ConfigureRewardLabelText();

        _runtimeStyleBox = Panel?.GetThemeStylebox("panel")?.Duplicate() as StyleBoxFlat;
        if (_runtimeStyleBox != null && Panel != null)
            Panel.AddThemeStyleboxOverride("panel", _runtimeStyleBox);

        Clicked += Select;
        if (Panel != null)
        {
            Panel.GuiInput += OnPanelGuiInput;
            Panel.MouseEntered += OnMouseEntered;
            Panel.MouseExited += OnMouseExited;
            SyncPanelSizeToCard();
        }
        else
        {
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;
        }
        ItemRectChanged += OnItemRectChanged;
        ApplyBorderState();

        if (Particles != null)
        {
            Particles.OneShot = true;
            Particles.Emitting = false;
        }
        CallDeferred(MethodName.UpdateParticlesLayout);
    }

    public override void _Process(double delta)
    {
        if (Particles == null || !Particles.TopLevel)
            return;

        Control targetControl = Panel != null ? Panel : this;
        var rect = targetControl.GetGlobalRect();
        if (_lastGlobalPos != rect.Position || _lastSize != rect.Size)
            UpdateParticlesLayout();
    }

    public void Select()
    {
        _isSelected = true;
        ApplyBorderState();
    }

    public void Unselect()
    {
        _isSelected = false;
        ApplyBorderState();
    }

    public async Task PlayRemoveAnimation(float moveDistance, float duration, bool keepHidden = false)
    {
        if (!Visible || Panel == null || !Panel.Visible)
            return;

        Vector2 basePos = Vector2.Zero;
        Panel.Position = basePos;
        SetAlpha(Panel, 1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(Panel, "position", basePos + new Vector2(moveDistance, 0), duration);
        tween.TweenProperty(Panel, "modulate:a", 0.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        Panel.Position = basePos;
        SetAlpha(Panel, keepHidden ? 0.0f : 1.0f);
    }

    public async Task PlayInsertAnimation(float moveDistance, float duration)
    {
        if (!Visible || Panel == null || !Panel.Visible)
            return;

        SyncPanelSizeToCard();
        Vector2 basePos = Vector2.Zero;
        Panel.Position = basePos;
        Panel.Position = basePos - new Vector2(moveDistance, 0);
        SetAlpha(Panel, 0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(Panel, "position", basePos, duration);
        tween.TweenProperty(Panel, "modulate:a", 1.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        Panel.Position = basePos;
        SetAlpha(Panel, 1.0f);
    }

    public void SyncPanelSizeToCard()
    {
        if (Panel == null)
            return;

        Panel.CustomMinimumSize = Size;
        Panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ConfigureRewardLabelText();
        UpdateRewardIconLayout();
    }

    public void ResetPanelVisualState()
    {
        if (Panel == null)
            return;

        SyncPanelSizeToCard();
        Panel.Visible = true;
        SetAlpha(Panel, 1.0f);
    }

    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        if (!_isInteractable)
        {
            _isHovered = false;
            _isSelected = false;
        }

        if (Panel != null)
            Panel.MouseFilter = _isInteractable ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        Modulate = _isInteractable ? EnabledModulate : DisabledModulate;
        ApplyBorderState();
    }

    public void ConfigureTalentPointRewardStyle()
    {
        ConfigureIconOnlyReward(CreateTalentPointRewardIcon(), TalentPointIconSize);
        ConfigureTalentPointParticles();
        ApplyBorderState();
    }

    public void ConfigureSkillRewardStyle()
    {
        var icon = new ColorRect
        {
            Name = "SkillRewardIcon",
            Color = Colors.White,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        if (SkillRewardIconShader != null)
        {
            icon.Material = new ShaderMaterial
            {
                Shader = SkillRewardIconShader,
                ResourceLocalToScene = true,
            };
        }

        ConfigureIconOnlyReward(icon, RewardIconSize);
    }

    public void ConfigureRelicRewardStyle(RelicID relicId)
    {
        var icon = Relic.IconScene?.Instantiate<ColorRect>();
        if (icon == null)
            icon = new ColorRect { Color = Colors.White };

        Relic.ApplyIconVisual(icon, relicId);
        HideGeneratedIconText(icon);
        ConfigureIconOnlyReward(icon, RewardIconSize);
    }

    public void ConfigureItemRewardStyle(ItemID itemId)
    {
        var icon = ConsumeItem.IconSence?.Instantiate<ColorRect>();
        if (icon == null)
            icon = new ColorRect { Color = Colors.White };

        ConsumeItem.ConfigureIcon(icon, itemId);
        ConfigureIconOnlyReward(icon, RewardIconSize);
    }

    public void ConfigureIconOnlyReward(Control icon, float iconSize = RewardIconSize)
    {
        if (label != null)
        {
            label.Visible = true;
            ConfigureRewardLabelText();
        }

        RemoveRewardIcon();
        _rewardIcon = icon;
        _rewardIconSize = iconSize;
        if (_rewardIcon == null)
            return;

        _rewardIcon.MouseFilter = MouseFilterEnum.Ignore;
        _rewardIcon.CustomMinimumSize = Vector2.Zero;
        _rewardIcon.SetAnchorsPreset(LayoutPreset.TopLeft);
        _rewardIcon.OffsetLeft = 0f;
        _rewardIcon.OffsetTop = 0f;
        _rewardIcon.OffsetRight = 0f;
        _rewardIcon.OffsetBottom = 0f;

        Node parent = _rewardIcon.GetParent();
        parent?.RemoveChild(_rewardIcon);
        Node iconParent = Panel ?? (Node)this;
        iconParent.AddChild(_rewardIcon);
        if (Panel != null)
            Panel.MoveChild(_rewardIcon, 0);
        UpdateRewardIconLayout();
        CallDeferred(nameof(UpdateRewardIconLayout));
    }

    public void PrepareForInsertVisual()
    {
        if (Panel == null)
            return;

        SyncPanelSizeToCard();
        Panel.Visible = true;
        SetAlpha(Panel, 0.0f);
    }

    public void PlayRejectAnimation()
    {
        if (!Visible || Panel == null || !Panel.Visible)
            return;

        _rejectTween?.Kill();
        _borderTween?.Kill();

        SyncPanelSizeToCard();
        Vector2 basePosition = Vector2.Zero;
        Color baseModulate = new(1f, 1f, 1f, Panel.Modulate.A);
        Color targetBorderColor = GetBorderStateColor();

        Panel.Position = basePosition;
        Panel.Modulate = RejectModulate with { A = baseModulate.A };
        if (_runtimeStyleBox != null)
        {
            _runtimeStyleBox.BorderColor = RejectBorderColor;
            _currentBorderColor = RejectBorderColor;
        }

        _rejectTween = CreateTween();
        _rejectTween.SetTrans(Tween.TransitionType.Sine);
        _rejectTween.SetEase(Tween.EaseType.Out);
        _rejectTween.TweenProperty(
            Panel,
            "position",
            basePosition + new Vector2(-RejectShakeOffset, 0),
            RejectShakeStepDuration
        );
        _rejectTween.TweenProperty(
            Panel,
            "position",
            basePosition + new Vector2(RejectShakeOffset, 0),
            RejectShakeStepDuration
        );
        _rejectTween.TweenProperty(
            Panel,
            "position",
            basePosition + new Vector2(-RejectShakeOffset * 0.55f, 0),
            RejectShakeStepDuration
        );
        _rejectTween.TweenProperty(Panel, "position", basePosition, RejectShakeStepDuration);
        _rejectTween.SetParallel(true);
        _rejectTween.TweenProperty(Panel, "modulate", baseModulate, 0.16f);
        if (_runtimeStyleBox != null)
        {
            Color startBorderColor = RejectBorderColor;
            _rejectTween.TweenMethod(
                Callable.From<float>(t =>
                {
                    _currentBorderColor = startBorderColor.Lerp(targetBorderColor, t);
                    _runtimeStyleBox.BorderColor = _currentBorderColor;
                }),
                0.0f,
                1.0f,
                0.16f
            );
        }

        _rejectTween.Finished += () =>
        {
            if (Panel != null && GodotObject.IsInstanceValid(Panel))
            {
                Panel.Position = basePosition;
                Panel.Modulate = baseModulate;
            }

            ApplyBorderState();
        };
    }

    private static void SetAlpha(CanvasItem node, float alpha)
    {
        if (node == null)
            return;
        node.Modulate = node.Modulate with { A = alpha };
    }

    private void OnPanelGuiInput(InputEvent @event)
    {
        if (!_isInteractable)
            return;

        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            return;

        TriggerParticles();
        EmitSignal(SignalName.Clicked);
    }

    private void OnItemRectChanged()
    {
        SyncPanelSizeToCard();
        UpdateRewardIconLayout();
        UpdateParticlesLayout();
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        ApplyBorderState(animate: true);
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        ApplyBorderState(animate: true);
    }

    private void ApplyBorderState(bool animate = false)
    {
        if (_runtimeStyleBox == null)
            return;

        Color targetColor = GetBorderStateColor();

        if (!animate || _isSelected || !_isInteractable)
        {
            _borderTween?.Kill();
            _runtimeStyleBox.BorderColor = targetColor;
            _currentBorderColor = targetColor;
            return;
        }

        _borderTween?.Kill();
        Color startColor = _currentBorderColor;
        _borderTween = CreateTween();
        _borderTween.SetEase(Tween.EaseType.Out);
        _borderTween.SetTrans(Tween.TransitionType.Cubic);
        _borderTween.TweenMethod(
            Callable.From<float>(t =>
            {
                _currentBorderColor = startColor.Lerp(targetColor, t);
                _runtimeStyleBox.BorderColor = _currentBorderColor;
            }),
            0.0f,
            1.0f,
            HoverBorderTweenDuration
        );
    }

    private Color GetBorderStateColor()
    {
        return !_isInteractable
            ? DisabledBorderColor
            : _isSelected
                ? SelectedBorderColor
                : (_isHovered ? HoverBorderColor : DefaultBorderColor);
    }

    private void TriggerParticles()
    {
        if (Particles == null)
            return;

        Particles.Restart();
        Particles.Emitting = true;
    }

    private void ConfigureTalentPointParticles()
    {
        if (Particles == null)
            return;

        if (TalentPointRewardIconShader != null)
        {
            var material = new ShaderMaterial
            {
                Shader = TalentPointRewardIconShader,
                ResourceLocalToScene = true,
            };
            material.SetShaderParameter("glow_color", new Color(1f, 0.75f, 0.12f, 1f));
            Particles.Material = material;
        }

        Particles.Amount = 42;
        Particles.Lifetime = 0.78f;
        Particles.Explosiveness = 0.68f;

        if (Particles.ProcessMaterial is ParticleProcessMaterial processMaterial)
        {
            var duplicated = processMaterial.Duplicate() as ParticleProcessMaterial;
            if (duplicated != null)
            {
                duplicated.ScaleMin = 7.5f;
                duplicated.ScaleMax = 28f;
                duplicated.InitialVelocityMin = 120f;
                duplicated.InitialVelocityMax = 210f;
                duplicated.Gravity = new Vector3(0f, -6f, 0f);
                duplicated.HueVariationMin = -0.02f;
                duplicated.HueVariationMax = 0.04f;
                Particles.ProcessMaterial = duplicated;
            }
        }
    }

    private ColorRect CreateTalentPointRewardIcon()
    {
        var icon = new ColorRect
        {
            Name = "TalentPointIcon",
            MouseFilter = MouseFilterEnum.Ignore,
            Color = Colors.White,
        };

        if (TalentPointRewardIconShader != null)
        {
            var material = new ShaderMaterial
            {
                Shader = TalentPointRewardIconShader,
                ResourceLocalToScene = true,
            };
            material.SetShaderParameter("glow_color", new Color(1f, 0.78f, 0.16f, 1f));
            material.SetShaderParameter("core_radius", 0.11f);
            material.SetShaderParameter("glow_radius", 0.5f);
            material.SetShaderParameter("glow_strength", 1.9f);
            icon.Material = material;
        }

        return icon;
    }

    private void RemoveRewardIcon()
    {
        if (_rewardIcon == null)
            return;

        if (GodotObject.IsInstanceValid(_rewardIcon))
            _rewardIcon.QueueFree();
        _rewardIcon = null;
    }

    private void UpdateRewardIconLayout()
    {
        ConfigureRewardLabelText();
        if (_rewardIcon == null || !GodotObject.IsInstanceValid(_rewardIcon))
            return;

        float size = Mathf.Max(1f, _rewardIconSize);
        _rewardIcon.Position = new Vector2(
            RewardIconLeftPadding,
            Mathf.Max(0f, (Size.Y - size) * 0.5f)
        );
        _rewardIcon.Size = new Vector2(size, size);
    }

    private static void HideGeneratedIconText(Control icon)
    {
        icon.GetNodeOrNull<Label>("Label")?.Hide();
        icon.GetNodeOrNull<Panel>("Panel")?.Hide();
    }

    private void ConfigureRewardLabelText()
    {
        if (label == null)
            return;

        label.Visible = true;
        label.HorizontalAlignment = HorizontalAlignment.Left;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = true;
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        label.OffsetLeft = _rewardIcon == null ? RewardTextRightPadding : RewardTextLeftPadding;
        label.OffsetTop = 6f;
        label.OffsetRight = -RewardTextRightPadding;
        label.OffsetBottom = -6f;
    }

    private void UpdateParticlesLayout()
    {
        if (Particles == null)
            return;

        Control targetNode = Panel != null ? Panel : this;
        Vector2 targetSize = targetNode.Size;

        if (Particles.TopLevel)
        {
            var rect = targetNode.GetGlobalRect();
            _lastGlobalPos = rect.Position;
            _lastSize = rect.Size;
            float targetX = rect.Position.X + rect.Size.X * 0.5f;
            float targetY = rect.Position.Y + rect.Size.Y + ParticleYOffset;
            Particles.GlobalPosition = new Vector2(targetX, targetY);
        }
        else
        {
            Particles.Position = new Vector2(targetSize.X * 0.5f, targetSize.Y + ParticleYOffset);
        }

        if (Particles.ProcessMaterial is ParticleProcessMaterial processMaterial)
        {
            Vector3 extents = processMaterial.EmissionBoxExtents;
            float emitX = targetSize.X * 0.5f + ParticleBoxPaddingX;
            processMaterial.EmissionBoxExtents = new Vector3(emitX, extents.Y, extents.Z);
        }
    }

    private StyleBoxFlat _runtimeStyleBox;
    private Tween _borderTween;
    private Tween _rejectTween;
    private Color _currentBorderColor = DefaultBorderColor;
    private bool _isSelected;
    private bool _isHovered;
    private bool _isInteractable = true;
    private Control _rewardIcon;
    private float _rewardIconSize = RewardIconSize;
    private Vector2 _lastGlobalPos;
    private Vector2 _lastSize;
}
