using Godot;

// Long pearl filaments behind the menu. Drawing buffers are reused every frame.
public partial class StartFlowCurves : Control {
    private const int Samples = 193;
    private readonly Vector2[] _points = new Vector2[Samples];
    private readonly Color[] _colors = new Color[Samples];
    private readonly Color[] _glowColors = new Color[Samples];
    private readonly Vector2[] _spark = new Vector2[8];
    private float _time;

    public override void _Ready() { MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Process(double delta) { _time += (float)delta; QueueRedraw(); }

    public override void _Draw() {
        if (Size.X <= 0f || Size.Y <= 0f) return;
        float scale = Size.Y / 1080f;
        // A clear leading sweep with broken, uneven filaments sharing its center.
        float head = Mathf.PosMod(_time * 0.045f + 0.16f, 1f);
        for (int strand = 3; strand >= 0; strand--) {
            for (int sample = 0; sample < Samples; sample++) {
                float t = sample / (Samples - 1f);
                Vector2 uv = CurvePoint(strand, t);
                _points[sample] = uv * Size;
                float visibility = Visibility(uv, t);
                if (strand > 0) {
                    float arc = Mathf.Sin(t * Mathf.Tau + strand * 1.65f);
                    visibility *= Mathf.SmoothStep(-0.45f, 0.5f, arc);
                }
                float variation = 0.75f + 0.25f * Mathf.Sin(t * Mathf.Tau);
                float behind = Mathf.PosMod(head + strand * 0.19f - t, 1f);
                float stream = behind >= 0f && behind < 0.22f
                    ? Mathf.Pow(1f - behind / 0.22f, 1.6f) * Mathf.SmoothStep(0f, 0.012f, behind) : 0f;
                float alpha = (strand == 0 ? 0.30f : strand == 1 ? 0.22f : 0.16f) * variation;
                alpha += stream * (strand == 0 ? 0.70f : 0.16f);
                _colors[sample] = new Color(0.92f, 0.94f, 0.94f, alpha * visibility);
                _glowColors[sample] = new Color(0.86f, 0.91f, 0.92f,
                    (0.012f + stream * 0.10f) * visibility * (strand == 0 ? 1f : 0.35f));
            }
            DrawPolylineColors(_points, _glowColors, 5f * scale, true);
            DrawPolylineColors(_points, _colors, (strand == 0 ? 1.05f : 0.7f) * scale, true);
        }
        Vector2 sparkUv = CurvePoint(0, head);
        DrawSpark(sparkUv * Size, Visibility(sparkUv, head), scale);
    }

    private Vector2 CurvePoint(int strand, float t) {
        float angle = t * Mathf.Tau;
        float breathing = Mathf.Sin(_time * 0.22f) * 8f;
        float drift = Mathf.Sin(angle * 2f + strand * 1.2f + _time * 0.12f);
        float radiusX = strand switch { 1 => 505f, 2 => 438f, 3 => 510f, _ => 465f };
        float radiusY = strand switch { 1 => 225f, 2 => 305f, 3 => 280f, _ => 250f };
        float tilt = strand switch { 1 => -0.44f, 2 => -0.12f, 3 => -0.54f, _ => -0.32f };
        float irregularity = strand == 0 ? 6f : 22f;
        Vector2 orbit = new Vector2(Mathf.Cos(angle) * (radiusX + breathing + drift * irregularity),
            Mathf.Sin(angle) * (radiusY + breathing * 0.5f + drift * irregularity));
        orbit = orbit.Rotated(tilt) * (Size.Y / 1080f);
        Vector2 center = Size * new Vector2(0.718f, 0.445f);
        center.Y += Mathf.Sin(_time * 0.33f) * 8f * (Size.Y / 1080f) * 0.84f;
        return (center + orbit) / Size;
    }

    private static float Visibility(Vector2 uv, float t) {
        // Recede above the subject, then catch light along the lower sweep.
        float depth = Mathf.Lerp(0.18f, 1f, Mathf.SmoothStep(-0.6f, 0.75f, Mathf.Sin(t * Mathf.Tau)));
        return depth * Mathf.SmoothStep(0.43f, 0.50f, uv.X);
    }

    private void DrawSpark(Vector2 position, float opacity, float scale) {
        float pulse = 0.78f + 0.22f * Mathf.Sin(_time * 1.5f);
        DrawCircle(position, 7f * scale, new Color(0.96f, 0.94f, 0.81f, opacity * 0.035f), antialiased: true);
        DrawCircle(position, 3.2f * scale, new Color(1f, 0.97f, 0.85f, opacity * 0.13f), antialiased: true);
        for (int i = 0; i < _spark.Length; i++) {
            float angle = i * Mathf.Tau / _spark.Length;
            float radius = (i % 2 == 0 ? 3.8f : 0.9f) * scale * pulse;
            _spark[i] = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        DrawColoredPolygon(_spark, new Color(1f, 0.98f, 0.89f, opacity * 0.85f));
    }
}
