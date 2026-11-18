using Godot;

/// <summary>Short, frame-animated attack cuts and broad sword slashes. Origin is the hit position.</summary>
[Tool]
public partial class SwordSlashVfx : Node2D {
    public enum SlashStyle { AttackCut, SwordEnergy }

    private const string ScenePath = "res://battle/Effect/SwordSlashVfx.tscn";
    private const float ReferenceLength = 360f;
    private const float ReferenceCanvasSize = ReferenceLength * 1.5f;
    private static PackedScene _scene;

    [Signal] public delegate void FinishedEventHandler();

    [Export] public SlashStyle Style { get; set; } = SlashStyle.AttackCut;
    [Export(PropertyHint.Range, "64,1000,1")] public float Length { get; set; } = 360f;
    [Export(PropertyHint.Range, "-180,180,1")] public float AngleDegrees { get; set; }
    [Export] public bool Mirror { get; set; }
    [Export] public Color BladeColor { get; set; } = new(0.94f, 0.99f, 1f);
    [Export] public Color EnergyColor { get; set; } = new(1f, 0.25f, 0.29f);
    [Export(PropertyHint.Range, "0.3,2,0.05")] public float Thickness { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float TravelDistance { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "0,2,0.05")] public float Brightness { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float PlaybackSpeed { get; set; } = 1f;
    [Export] public bool RandomizeVariation { get; set; } = true;
    [Export] public int VariationSeed { get; set; }
    [Export(PropertyHint.Range, "0,99,1,or_greater")] public int HitIndex { get; set; }
    [Export(PropertyHint.Range, "1,100,1,or_greater")] public int HitCount { get; set; } = 1;
    [Export] public bool AutoPlay { get; set; } = true;
    [Export] public bool AutoFree { get; set; } = true;
    [Export] public bool Loop { get; set; }
    [Export(PropertyHint.Range, "0,3,0.05")] public float LoopDelaySeconds { get; set; } = 0.45f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float EditorPreviewProgress { get; set; } = 0.27f;

    // Match the reference's 4 frames at 15 FPS and 6 frames over 0.3 seconds.
    public float Duration => (Style == SlashStyle.AttackCut ? 4f / 15f : 0.3f)
        / (Mathf.Clamp(PlaybackSpeed, 0.1f, 3f) * CurrentStrike.Speed);
    public float CurrentTime => (float)_elapsed;
    public bool IsPlaying => _playing;

    private ColorRect _surface;
    private ShaderMaterial _material;
    private double _elapsed;
    private bool _playing;
    private SwordSlashSequence _sequence;
    private int _resolvedSeed;
    private ColorRect Surface => _surface ??= GetNode<ColorRect>("Surface");
    private SwordSlashSequence.Strike CurrentStrike {
        get {
            if (!RandomizeVariation) return SwordSlashSequence.Strike.Identity;
            int seed = VariationSeed != 0 ? VariationSeed : _resolvedSeed;
            if (seed == 0) seed = Engine.IsEditorHint() ? 137 : SwordSlashSequence.CreateSeed();
            _resolvedSeed = seed;
            if (_sequence == null || _sequence.Seed != seed || _sequence.HitCount != Mathf.Max(HitCount, 1))
                _sequence = new SwordSlashSequence(HitCount, seed);
            return _sequence.GetStrike(HitIndex);
        }
    }

    public override void _Ready() {
        // Materials and clocks belong to the instance, including when the scene is open in the editor.
        _material = (ShaderMaterial)Surface.Material.Duplicate();
        Surface.Material = _material;
        RefreshAppearance();
        if (Engine.IsEditorHint()) {
            ApplyTime(EditorPreviewProgress * Duration);
            SetProcess(true);
        }
        else if (AutoPlay) Play();
        else Stop();
    }

    /// <summary>Spawn in local coordinates, inheriting scene appearance unless explicitly overridden.</summary>
    public static SwordSlashVfx Spawn(Node2D parent, Vector2 localPosition,
        float? length = null, float? angleDegrees = null, bool? mirror = null,
        SlashStyle? style = null, int sequenceSeed = 0, int hitIndex = 0, int hitCount = 1,
        bool flipHorizontally = false) {
        if (!GodotObject.IsInstanceValid(parent) || parent.IsQueuedForDeletion()) return null;
        _scene ??= GD.Load<PackedScene>(ScenePath);
        var effect = _scene?.Instantiate<SwordSlashVfx>();
        if (effect == null) return null;
        effect.Position = localPosition;
        if (length.HasValue) effect.Length = length.Value;
        if (angleDegrees.HasValue) effect.AngleDegrees = angleDegrees.Value;
        if (mirror.HasValue) effect.Mirror = mirror.Value;
        if (style.HasValue) effect.Style = style.Value;
        effect.Mirror ^= flipHorizontally;
        effect.VariationSeed = sequenceSeed;
        effect.HitIndex = hitIndex;
        effect.HitCount = hitCount;
        parent.AddChild(effect);
        return effect;
    }

    public void RefreshAppearance() {
        if (_material == null) return;
        var strike = CurrentStrike;
        float baseLength = Mathf.Clamp(Length, 64f, 1000f);
        float lengthScale = baseLength / ReferenceLength * strike.LengthScale;
        float thickness = Mathf.Clamp(Thickness * strike.Thickness, 0.3f, 2f);
        float travel = Mathf.Clamp(TravelDistance, 0f, 0.5f);
        Vector2 along = Style == SlashStyle.AttackCut
            ? SwordSlashSequence.NativeStart.Normalized() : Vector2.Up;
        Vector2 across = new(-along.Y, along.X);
        // Stretch along the painted stroke while keeping its perpendicular width independent.
        // Bound the transformed source square so long or thick strokes remain inside the quad.
        float skew = Mathf.Abs(along.X * along.Y * (lengthScale - thickness));
        Vector2 span = new(
            along.X * along.X * lengthScale + across.X * across.X * thickness + skew,
            along.Y * along.Y * lengthScale + across.Y * across.Y * thickness + skew);
        if (Style == SlashStyle.SwordEnergy) span.X += 2f * travel * lengthScale / 1.5f;
        Vector2 size = span * ReferenceCanvasSize;
        Vector2 offset = strike.Offset * baseLength;
        if (Mirror) offset.X = -offset.X;
        float strikeAngle = strike.AngleDegrees;
        // The energy sheet is vertical; align it to the same planned chord as the diagonal cut.
        if (Style == SlashStyle.SwordEnergy && RandomizeVariation && HitCount > 1)
            strikeAngle += Mathf.RadToDeg(SwordSlashSequence.NativeStart.Angle() - along.Angle());
        Surface.Position = offset.Rotated(Mathf.DegToRad(AngleDegrees)) - size * 0.5f;
        Surface.Size = size;
        Surface.PivotOffset = Surface.Size * 0.5f;
        Surface.Rotation = Mathf.DegToRad(AngleDegrees + (Mirror ? -strikeAngle : strikeAngle));
        Surface.Scale = new Vector2(Mirror ? -1f : 1f, 1f);
        _material.SetShaderParameter("blade_color", BladeColor);
        _material.SetShaderParameter("energy_color", EnergyColor);
        _material.SetShaderParameter("stroke_axis", along);
        _material.SetShaderParameter("surface_span", span);
        _material.SetShaderParameter("length_scale", lengthScale);
        _material.SetShaderParameter("thickness", thickness);
        _material.SetShaderParameter("travel_distance", travel);
        _material.SetShaderParameter("brightness", Mathf.Clamp(Brightness * strike.Brightness, 0f, 2f));
        _material.SetShaderParameter("sword_energy", Style == SlashStyle.SwordEnergy);
    }

    public float GetDuration() => Duration;

    // Explicit seeds let multi-hit callers and timeline previews replay the same gesture.
    public void SetSequenceVariant(int seed, int hitIndex, int hitCount) {
        VariationSeed = seed;
        HitIndex = hitIndex;
        HitCount = Mathf.Max(hitCount, 1);
        _sequence = null;
        RefreshAppearance();
        ApplyTime((float)_elapsed);
    }

    public void Play() {
        if (VariationSeed == 0) {
            _resolvedSeed = SwordSlashSequence.CreateSeed();
            _sequence = null;
        }
        _elapsed = 0;
        _playing = true;
        Visible = true;
        RefreshAppearance();
        ApplyTime(0f);
        SetProcess(true);
    }

    public void Pause() {
        _playing = false;
        SetProcess(false);
    }

    public void Resume() {
        if (_elapsed >= Duration) {
            Play();
            return;
        }
        _playing = true;
        Visible = true;
        SetProcess(true);
    }

    public void Stop() {
        _playing = false;
        Visible = false;
        SetProcess(false);
    }

    /// <summary>Freeze on an exact source frame, including the sword slash's motion and fade.</summary>
    public void Seek(float seconds) {
        Pause();
        _elapsed = Mathf.Clamp(seconds, 0f, Duration);
        Visible = true;
        RefreshAppearance();
        ApplyTime((float)_elapsed);
    }

    public override void _Process(double delta) {
        if (Engine.IsEditorHint()) {
            RefreshAppearance();
            ApplyTime(EditorPreviewProgress * Duration);
            return;
        }
        if (!_playing) return;
        _elapsed += delta;
        if (Loop) {
            _elapsed %= Duration + Mathf.Max(LoopDelaySeconds, 0f);
            ApplyTime((float)_elapsed);
            return;
        }
        ApplyTime((float)_elapsed);
        if (_elapsed < Duration) return;
        Stop();
        EmitSignal(SignalName.Finished);
        // A Finished callback may replay or delete the effect.
        if (GodotObject.IsInstanceValid(this) && !_playing && AutoFree && !IsQueuedForDeletion())
            QueueFree();
    }

    private void ApplyTime(float seconds) {
        if (_material == null) return;
        float progress = Mathf.Clamp(seconds / Duration, 0f, 1f);
        int frameCount = Style == SlashStyle.AttackCut ? 4 : 6;
        int frame = Mathf.Min((int)(progress * frameCount), frameCount - 1);
        _material.SetShaderParameter("effect_progress", progress);
        _material.SetShaderParameter("frame_index", frame);
        _material.SetShaderParameter("visible_amount", seconds >= Duration ? 0f : 1f);
    }
}
