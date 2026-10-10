using Godot;

[Tool]
public partial class CharacterShadow : ColorRect {
    [ExportGroup("Projection")]
    [Export] public Color FillColor { get; set; } = new(0.62f, 0.63f, 0.66f, 1f);
    [Export(PropertyHint.Range, "0,1,0.01")] public float RingAlpha { get; set; } = 0.60f;
    [Export] public Vector2 RingCenter { get; set; } = new(0.5f, 0.8956044f);
    [Export] public Vector2 RingRadius { get; set; } = new(0.325f, 0.05714286f);
    [Export(PropertyHint.Range, "0.1,8,0.1")] public float RingThickness { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,24,0.1")] public float RingGlowWidth { get; set; } = 4f;
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float RingPhase { get; set; }
    [Export(PropertyHint.Range, "-10,10,0.01")] public float RingRotationSpeed { get; set; } = 0.75f;
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float RingArcLength { get; set; } = 2.25f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float NearRingAlpha { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float FarRingAlpha { get; set; } = 0.36f;
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float HighlightPhase { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float HighlightArcLength { get; set; } = 0.20f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float HighlightAlpha { get; set; } = 0.46f;
    [Export] public Vector2 OuterRingScale { get; set; } = new(1.18f, 1.375f);
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float OuterRingPhase { get; set; } = 3.1415927f;
    [Export(PropertyHint.Range, "0,6.283185,0.01")] public float OuterRingArcLength { get; set; } = 2.25f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float OuterRingAlpha { get; set; } = 0.28f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ProjectionGlowAlpha { get; set; } = 0.20f;

    [ExportGroup("Hover Wall")]
    [Export] public bool UseCharacterColorForWall { get; set; } = true;
    [Export] public Color WallColor { get; set; } = new(1f, 0.94f, 0.82f, 1f);
    [Export(PropertyHint.Range, "0.05,1,0.01")] public float WallHeight { get; set; } = 0.80f;
    [Export(PropertyHint.Range, "0.001,0.2,0.001")] public float WallEdgeSoftness { get; set; } = 0.012f;
    [Export(PropertyHint.Range, "0.1,4,0.05")] public float WallFadePower { get; set; } = 1.4f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float WallAlpha { get; set; } = 0.52f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float WallGlow { get; set; } = 0.42f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float HoverTweenDuration { get; set; } = 0.22f;

    [ExportGroup("Editor Preview")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float PreviewHoverAmount { get; set; }

    private ShaderMaterial _material;
    private ShaderMaterial _frontMaterial;
    private Tween _hoverTween;
    private float _hoverAmount;
    private float _ringPhaseOffset;

    public override void _Ready() {
        MouseFilter = MouseFilterEnum.Ignore;
        EnsureMaterial();
        ApplyVisualParameters();
        SetProcess(true);
    }

    public override void _Process(double delta) {
        float elapsed = (float)delta;
        _ringPhaseOffset = Mathf.PosMod(_ringPhaseOffset + elapsed * RingRotationSpeed, Mathf.Tau);
        ApplyVisualParameters();
    }

    public override void _ExitTree() {
        _hoverTween?.Kill();
        _hoverTween = null;
    }

    public void ApplyCharacterColor(Color color) {
        EnsureMaterial();
        FillColor = color;
        ApplyVisualParameters();
    }

    public void SetCardHoverHighlight(bool active, bool instant = false) {
        EnsureMaterial();
        if (_material == null)
            return;

        float target = active ? 1f : 0f;
        _hoverTween?.Kill();
        _hoverTween = null;
        if (instant || !IsInsideTree() || HoverTweenDuration <= 0f) {
            SetHoverAmount(target);
            return;
        }

        _hoverTween = CreateTween();
        _hoverTween.TweenMethod(Callable.From<float>(SetHoverAmount),
            _hoverAmount, target, HoverTweenDuration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    private void SetHoverAmount(float value) {
        _hoverAmount = value;
        if (_material != null && GodotObject.IsInstanceValid(_material))
            _material.SetShaderParameter("hover_amount", value);
        if (_frontMaterial != null && GodotObject.IsInstanceValid(_frontMaterial))
            _frontMaterial.SetShaderParameter("hover_amount", value);
    }

    private void EnsureMaterial() {
        if (_material != null && GodotObject.IsInstanceValid(_material))
            return;

        _material = Material as ShaderMaterial;
        _frontMaterial = GetNodeOrNull<ColorRect>("CharacterShadowFront")?.Material as ShaderMaterial;
        if (_material != null && GodotObject.IsInstanceValid(_material))
            return;

        Shader shader = GD.Load<Shader>("res://shader/UI/CharacterShadow.gdshader");
        if (shader == null)
            return;

        _material = new ShaderMaterial { Shader = shader, ResourceLocalToScene = true };
        Material = _material;
        _frontMaterial = GetNodeOrNull<ColorRect>("CharacterShadowFront")?.Material as ShaderMaterial;
    }

    private void ApplyVisualParameters() {
        if (_material == null || !GodotObject.IsInstanceValid(_material))
            return;

        ApplyVisualParametersTo(_material, -1f);
        if (_frontMaterial != null && GodotObject.IsInstanceValid(_frontMaterial))
            ApplyVisualParametersTo(_frontMaterial, 1f);
    }

    private void ApplyVisualParametersTo(ShaderMaterial material, float depthLayer) {
        if (material == null || !GodotObject.IsInstanceValid(material))
            return;

        material.SetShaderParameter("fill_color", FillColor with { A = RingAlpha });
        material.SetShaderParameter("wall_color", UseCharacterColorForWall ? FillColor : WallColor);
        material.SetShaderParameter("ring_center", RingCenter);
        material.SetShaderParameter("ring_radius", RingRadius);
        material.SetShaderParameter("projection_size", Size);
        material.SetShaderParameter("ring_thickness", RingThickness);
        material.SetShaderParameter("ring_glow_width", RingGlowWidth);
        material.SetShaderParameter("ring_phase", RingPhase + _ringPhaseOffset);
        material.SetShaderParameter("ring_arc_length", RingArcLength);
        material.SetShaderParameter("depth_layer", depthLayer);
        material.SetShaderParameter("near_ring_alpha", NearRingAlpha);
        material.SetShaderParameter("far_ring_alpha", FarRingAlpha);
        material.SetShaderParameter("highlight_phase", HighlightPhase);
        material.SetShaderParameter("highlight_arc_length", HighlightArcLength);
        material.SetShaderParameter("highlight_alpha", HighlightAlpha);
        material.SetShaderParameter("outer_ring_scale", OuterRingScale);
        // The shader adds ring_phase to the fixed outer phase, so both arcs
        // rotate together while keeping their exported angular separation.
        material.SetShaderParameter("outer_ring_phase", OuterRingPhase);
        material.SetShaderParameter("outer_ring_arc_length", OuterRingArcLength);
        material.SetShaderParameter("outer_ring_alpha", OuterRingAlpha);
        material.SetShaderParameter("projection_glow_alpha", ProjectionGlowAlpha);
        material.SetShaderParameter("wall_height", WallHeight);
        material.SetShaderParameter("wall_edge_softness", WallEdgeSoftness);
        material.SetShaderParameter("wall_fade_power", WallFadePower);
        material.SetShaderParameter("wall_alpha", WallAlpha);
        material.SetShaderParameter("wall_glow", WallGlow);
        material.SetShaderParameter("hover_amount", Engine.IsEditorHint() ? PreviewHoverAmount : _hoverAmount);
    }
}
