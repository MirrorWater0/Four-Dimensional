using System.Threading.Tasks;
using Godot;

public partial class CardSlot : Control
{
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
    private PanelContainer Panel => field ??= GetNodeOrNull<PanelContainer>("Panel");

    public override void _Ready()
    {
        if (label != null)
            label.MouseFilter = MouseFilterEnum.Ignore;

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

        Panel.Position = Vector2.Zero;
        Panel.CustomMinimumSize = Size;
        Panel.Size = Size;
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
    private Vector2 _lastGlobalPos;
    private Vector2 _lastSize;
}
