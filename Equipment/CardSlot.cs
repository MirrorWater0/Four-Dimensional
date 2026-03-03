using System.Threading.Tasks;
using Godot;

public partial class CardSlot : PanelContainer
{
    private static readonly Color DefaultBorderColor = new("#a7d6ff52");
    private static readonly Color HoverBorderColor = new("#5cff8a");
    private static readonly Color SelectedBorderColor = Colors.Yellow;

    [Export]
    public float HoverBorderTweenDuration = 0.12f;

    [Export]
    public float ParticleBoxPaddingX = 80f;

    [Export]
    public float ParticleYOffset = 12f;

    [Export]
    public bool EnableDrag = true;

    [Signal]
    public delegate void ClickedEventHandler();
    public GpuParticles2D particles2D => field ??= GetNodeOrNull<GpuParticles2D>("Particles2D");
    public Label label =>
        field ??= GetNodeOrNull<Label>("Card1Label") ?? GetNodeOrNull<Label>("Label");
    private GpuParticles2D Particles => field ??= GetNodeOrNull<GpuParticles2D>("GPUParticles2D");

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            TriggerParticles();
            EmitSignal(SignalName.Clicked);
        }
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!EnableDrag || !Visible)
            return default;

        var data = new Godot.Collections.Dictionary
        {
            { "source_path", GetPath() },
        };

        var preview = BuildDragPreview();
        if (preview != null)
            SetDragPreview(preview);

        return data;
    }

    public override void _Ready()
    {
        if (label != null)
            label.MouseFilter = MouseFilterEnum.Ignore;

        _runtimeStyleBox = GetThemeStylebox("panel").Duplicate() as StyleBoxFlat;
        if (_runtimeStyleBox != null)
            AddThemeStyleboxOverride("panel", _runtimeStyleBox);

        Clicked += Select;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        ItemRectChanged += UpdateParticlesLayout;
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

        var rect = GetGlobalRect();
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

    public async Task PlayRemoveAnimation(float moveDistance, float duration)
    {
        if (!Visible)
            return;

        Vector2 baseGlobal = GlobalPosition;
        bool originalTopLevel = TopLevel;
        SetTopLevelPreservePosition(true);
        SetAlpha(1.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.In);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(
            this,
            "global_position",
            baseGlobal + new Vector2(moveDistance, 0),
            duration
        );
        tween.TweenProperty(this, "modulate:a", 0.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetTopLevelPreservePosition(originalTopLevel);
        GlobalPosition = baseGlobal;
        SetAlpha(1.0f);
    }

    public async Task PlayInsertAnimation(float moveDistance, float duration)
    {
        if (!Visible)
            return;

        Vector2 baseGlobal = GlobalPosition;
        bool originalTopLevel = TopLevel;
        SetTopLevelPreservePosition(true);
        GlobalPosition = baseGlobal - new Vector2(moveDistance, 0);
        SetAlpha(0.0f);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "global_position", baseGlobal, duration);
        tween.TweenProperty(this, "modulate:a", 1.0f, duration * 0.95f);

        await ToSignal(tween, Tween.SignalName.Finished);

        SetTopLevelPreservePosition(originalTopLevel);
        GlobalPosition = baseGlobal;
        SetAlpha(1.0f);
    }

    private void SetTopLevelPreservePosition(bool topLevel)
    {
        if (TopLevel == topLevel)
            return;

        Vector2 globalPos = GlobalPosition;
        TopLevel = topLevel;
        GlobalPosition = globalPos;
    }

    private void SetAlpha(float alpha)
    {
        Modulate = Modulate with { A = alpha };
    }

    private Control BuildDragPreview()
    {
        var preview = new PanelContainer();
        preview.CustomMinimumSize = Size;
        preview.Size = Size;
        preview.MouseFilter = MouseFilterEnum.Ignore;
        preview.Modulate = new Color(1, 1, 1, 0.92f);

        var style = GetThemeStylebox("panel")?.Duplicate() as StyleBox;
        if (style != null)
            preview.AddThemeStyleboxOverride("panel", style);

        var textLabel = new Label();
        textLabel.Text = label?.Text ?? string.Empty;
        textLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textLabel.MouseFilter = MouseFilterEnum.Ignore;
        preview.AddChild(textLabel);
        return preview;
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

        Color targetColor = _isSelected
            ? SelectedBorderColor
            : (_isHovered ? HoverBorderColor : DefaultBorderColor);

        if (!animate || _isSelected)
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

        if (Particles.TopLevel)
        {
            var rect = GetGlobalRect();
            _lastGlobalPos = rect.Position;
            _lastSize = rect.Size;
            float targetX = rect.Position.X + rect.Size.X * 0.5f;
            float targetY = rect.Position.Y + rect.Size.Y + ParticleYOffset;
            Particles.GlobalPosition = new Vector2(targetX, targetY);
        }
        else
        {
            Particles.Position = new Vector2(Size.X * 0.5f, Size.Y + ParticleYOffset);
        }

        if (Particles.ProcessMaterial is ParticleProcessMaterial processMaterial)
        {
            Vector3 extents = processMaterial.EmissionBoxExtents;
            float emitX = Size.X * 0.5f + ParticleBoxPaddingX;
            processMaterial.EmissionBoxExtents = new Vector3(emitX, extents.Y, extents.Z);
        }
    }

    private StyleBoxFlat _runtimeStyleBox;
    private Tween _borderTween;
    private Color _currentBorderColor = DefaultBorderColor;
    private bool _isSelected;
    private bool _isHovered;
    private Vector2 _lastGlobalPos;
    private Vector2 _lastSize;
}
