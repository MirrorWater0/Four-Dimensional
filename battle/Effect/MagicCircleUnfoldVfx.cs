using Godot;

/// <summary>Procedural, texture-free spell seal. Local origin is the circle's center.</summary>
public partial class MagicCircleUnfoldVfx : Node2D {
    private const string ScenePath = "res://battle/Effect/MagicCircleUnfoldVfx.tscn";

    [Signal] public delegate void FinishedEventHandler();

    [Export(PropertyHint.Range, "32,1200,1")] public float Radius { get; set; } = 220f;
    [Export(PropertyHint.Range, "0.15,1,0.01")] public float GroundRatio { get; set; } = 0.48f;
    [Export] public Color PrimaryColor { get; set; } = new(0.32f, 0.78f, 1f);
    [Export] public Color AccentColor { get; set; } = new(0.72f, 0.48f, 1f);
    [Export(PropertyHint.Range, "0.1,5,0.05")] public float UnfoldSeconds { get; set; } = 1.05f;
    [Export(PropertyHint.Range, "0,10,0.05")] public float HoldSeconds { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0.1,5,0.05")] public float FadeSeconds { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,3,0.05")] public float Brightness { get; set; } = 1f;
    [Export(PropertyHint.Range, "-1,1,0.01")] public float RotationSpeed { get; set; } = 0.12f;
    [Export] public bool AutoPlay { get; set; } = true;
    [Export] public bool Loop { get; set; }
    [Export(PropertyHint.Range, "0,5,0.05")] public float LoopDelaySeconds { get; set; } = 0.6f;
    [Export] public bool AutoFree { get; set; } = true;

    public float Duration => Mathf.Max(UnfoldSeconds, 0.1f) + Mathf.Max(HoldSeconds, 0f) + Mathf.Max(FadeSeconds, 0.1f);

    private ColorRect _surface;
    private ShaderMaterial _material;
    private double _elapsed;
    private bool _playing;

    private ColorRect Surface => _surface ??= GetNode<ColorRect>("Surface");

    public override void _Ready() {
        // Every instance owns its clock and palette; playing one must not change another.
        _material = (ShaderMaterial)Surface.Material.Duplicate();
        Surface.Material = _material;
        RefreshAppearance();
        if (AutoPlay) Play();
        else Stop();
    }

    /// <summary>Spawn in the parent's local coordinates. Set optional parameters before AddChild.</summary>
    public static MagicCircleUnfoldVfx Spawn(Node2D parent, Vector2 localPosition, float radius = 220f) {
        if (!GodotObject.IsInstanceValid(parent)) return null;
        var effect = GD.Load<PackedScene>(ScenePath).Instantiate<MagicCircleUnfoldVfx>();
        effect.Position = localPosition;
        effect.Radius = radius;
        parent.AddChild(effect);
        return effect;
    }

    public void RefreshAppearance() {
        if (_material == null) return;
        // The artwork's outer seals reach 0.86 of the shader canvas.
        float extent = Mathf.Max(Radius, 1f) / 0.86f;
        var size = new Vector2(extent * 2f, extent * 2f * Mathf.Clamp(GroundRatio, 0.15f, 1f));
        Surface.Position = -size * 0.5f;
        Surface.Size = size;
        _material.SetShaderParameter("primary_color", PrimaryColor);
        _material.SetShaderParameter("accent_color", AccentColor);
        _material.SetShaderParameter("unfold_seconds", Mathf.Max(UnfoldSeconds, 0.1f));
        _material.SetShaderParameter("hold_seconds", Mathf.Max(HoldSeconds, 0f));
        _material.SetShaderParameter("fade_seconds", Mathf.Max(FadeSeconds, 0.1f));
        _material.SetShaderParameter("brightness", Mathf.Max(Brightness, 0f));
        _material.SetShaderParameter("rotation_speed", RotationSpeed);
    }

    public void Play() {
        _elapsed = 0;
        _playing = true;
        Visible = true;
        RefreshAppearance();
        _material?.SetShaderParameter("effect_time", 0f);
        SetProcess(true);
    }

    public void Stop() {
        _playing = false;
        Visible = false;
        SetProcess(false);
    }

    /// <summary>Freeze at a local animation time, useful for editor tools and deterministic captures.</summary>
    public void Seek(float seconds) {
        _playing = false;
        _elapsed = Mathf.Clamp(seconds, 0f, Duration);
        Visible = true;
        RefreshAppearance();
        _material?.SetShaderParameter("effect_time", (float)_elapsed);
        SetProcess(false);
    }

    public override void _Process(double delta) {
        if (!_playing) return;
        _elapsed += delta;
        if (Loop) {
            double cycle = Duration + Mathf.Max(LoopDelaySeconds, 0f);
            _elapsed %= cycle;
            _material.SetShaderParameter("effect_time", (float)_elapsed);
            return;
        }

        _material.SetShaderParameter("effect_time", (float)_elapsed);
        if (_elapsed < Duration) return;
        Stop();
        EmitSignal(SignalName.Finished);
        // A Finished listener may replay the effect or remove it.
        if (GodotObject.IsInstanceValid(this) && !_playing && AutoFree) QueueFree();
    }
}
