using System;
using Godot;

// Layout and materials belong to Battle.tscn; this drives their presentation.
[Tool]
public partial class BattleAtmosphere : Control {
    [Export] public Color NormalTint { get; set; } = new(0.62f, 0.63f, 0.66f);
    [Export] public Color EliteTint { get; set; } = new(0.67f, 0.64f, 0.62f);
    [Export] public Color BossTint { get; set; } = new(0.72f, 0.65f, 0.61f);
    [Export] public Texture2D GlowTexture { get; set; }
    [Export] public Vector2 StructureCenter { get; set; } = new(0.5f, 0.43f);
    [Export] public float StructureSize { get; set; } = 116f;
    [Export] public float ProjectionHeight { get; set; } = 1.08f;
    [Export] public Vector2 OrbitRadius { get; set; } = new(226f, 166f);
    [Export] public Vector2 GlowRadius { get; set; } = new(278f, 226f);
    [Export] public Color EnemyPhaseTint { get; set; } = new(0.67f, 0.60f, 0.59f);
    [Export] public bool AnimateEditorPreview { get; set; } = false;
    private readonly Vector2[] _arcPoints = new Vector2[49];
    private readonly Vector2[] _cubeVertices = new Vector2[8];
    private static readonly int[,] CubeEdges = { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };
    private readonly float[] _cubeDepths = new float[8];
    private Battle _battle;
    private ColorRect _spaceControl;
    private ShaderMaterial _space;
    private ShaderMaterial _grade;
    private Texture2D _glow;
    private CanvasLayer _tipLayer;
    private float _time;
    private float _structureTime;
    private float _focus;
    private float _impact;
    private float _impactCooldown;
    private float _pollTime;
    private bool _inspecting;
    private Color _tint;
    private Color _phaseTint;
    private float _arrivalLight;
    private float _encounterStrength = 1f;
    private float _buttonWake;
    private float _buttonPress;
    private bool _focused = true;

    public float FocusAmount => _focus;
    public float ImpactAmount => _impact;

    public override void _Ready() {
        _glow = GlowTexture;
        _tint = _phaseTint = NormalTint;
        _spaceControl = GetNodeOrNull<ColorRect>("../DimensionalSpace");
        _space = _spaceControl?.Material as ShaderMaterial;
        if (Engine.IsEditorHint()) {
            UpdateGrade();
            QueueRedraw();
            return;
        }
        _battle = GetParent()?.GetParent() as Battle;
        if (GetNodeOrNull<ColorRect>("../BackgroundHueNeutralizer") is { } neutralizer
            && neutralizer.Material is ShaderMaterial material) {
            // Each battle owns its pulse and tint, including warmup instances.
            _grade = (ShaderMaterial)material.Duplicate();
            neutralizer.Material = _grade;
        }
        _focused = GetWindow()?.HasFocus() ?? true;
        SetProcess(_focused);
        PollPresentation();
        UpdateGrade();
    }

    public override void _Notification(int what) {
        if (Engine.IsEditorHint())
            return;
        if (what == MainLoop.NotificationApplicationFocusIn || what == MainLoop.NotificationApplicationFocusOut) {
            _focused = what == MainLoop.NotificationApplicationFocusIn;
            SetProcess(_focused);
        }
    }

    public override void _Process(double delta) {
        if (Engine.IsEditorHint()) {
            _glow = GlowTexture;
            _tint = _phaseTint = NormalTint;
            if (AnimateEditorPreview)
                _structureTime += (float)delta;
            _space = GetNodeOrNull<ColorRect>("../DimensionalSpace")?.Material as ShaderMaterial;
            UpdateGrade();
            QueueRedraw();
            return;
        }
        float dt = Mathf.Min((float)delta, 0.1f);
        _pollTime -= dt;
        if (_pollTime <= 0f) {
            _pollTime = 0.10f;
            PollPresentation();
        }
        _focus = Mathf.Lerp(_focus, _inspecting ? 1f : 0f, 1f - Mathf.Exp(-dt * 5f));
        _buttonWake = Mathf.Lerp(_buttonWake, ReadButtonWake(), 1f - Mathf.Exp(-dt * 10f));
        _buttonPress = Mathf.Lerp(_buttonPress, ReadButtonPress(), 1f - Mathf.Exp(-dt * 18f));
        _impact = Mathf.MoveToward(_impact, 0f, dt * 0.85f);
        _arrivalLight = Mathf.MoveToward(_arrivalLight, 0f, dt * 0.8f);
        _impactCooldown = Mathf.Max(0f, _impactCooldown - dt);
        float motion = Mathf.Lerp(0.48f, 0.10f, _focus);
        _time += dt * motion;
        // Keep the distant walls and landmark on one slow clock.
        // Inspection softens their movement without freezing the space.
        _structureTime += dt * Mathf.Lerp(1f, 0.35f, _focus);
        UpdateGrade();
        QueueRedraw();
    }

    public void Pulse(float strength) {
        if (_battle?.WarmupMode != false || _impactCooldown > 0f || !_focused)
            return;
        // Respect the existing setting for battle motion; repeated hits do not stack.
        float intensity = Mathf.Clamp(UserSettings.GetBattleShakeMultiplier(), 0f, 1f);
        _impact = Mathf.Max(_impact, Mathf.Clamp(strength, 0f, 0.8f) * intensity);
        _impactCooldown = 0.8f;
    }

    private void PollPresentation() {
        if (!GodotObject.IsInstanceValid(_battle))
            return;
        _inspecting = _battle.CharacterControl.IsInspectingBattlePresentation
            || _battle.IsBattleRecordOpen;
        if (!GodotObject.IsInstanceValid(_tipLayer))
            _tipLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>(Tip.TipLayerName);
        if (GodotObject.IsInstanceValid(_tipLayer)) {
            foreach (Node child in _tipLayer.GetChildren()) {
                if (child is Tip tip && tip.IsVisibleInTree() && tip.Modulate.A > 0.05f) {
                    _inspecting = true;
                    break;
                }
            }
        }
        float previousStrength = _encounterStrength;
        (_tint, _encounterStrength) = _battle.CurrentLevelNode?.Type switch {
            LevelNode.LevelType.Elite => (EliteTint, 1.25f),
            LevelNode.LevelType.Boss => (BossTint, 1.6f),
            _ => (NormalTint, 1f)
        };
        if (_encounterStrength > previousStrength && !_battle.WarmupMode)
            _arrivalLight = _encounterStrength - 0.5f;
        Button endTurn = _battle.CharacterControl?.EndTurnButton;
        _phaseTint = GodotObject.IsInstanceValid(endTurn) && endTurn.Disabled
            ? EnemyPhaseTint
            : _tint;
    }

    private float ReadButtonWake() {
        Button button = _battle?.CharacterControl?.EndTurnButton;
        return GodotObject.IsInstanceValid(button) && !button.Disabled && button.IsHovered() ? 1f : 0f;
    }

    private float ReadButtonPress() {
        Button button = _battle?.CharacterControl?.EndTurnButton;
        return GodotObject.IsInstanceValid(button) && button.ButtonPressed ? 1f : 0f;
    }

    private void UpdateGrade() {
        if (GodotObject.IsInstanceValid(_space)) {
            if (GodotObject.IsInstanceValid(_spaceControl)) {
                _space.SetShaderParameter("field_size", _spaceControl.Size);
                _space.SetShaderParameter("field_origin", _spaceControl.Position + ((Control)GetParent()).Position);
            }
            _space.SetShaderParameter("composition_size", Engine.IsEditorHint() ? new Vector2(1920f, 1080f) : GetViewportRect().Size);
            _space.SetShaderParameter("space_time", _structureTime);
            _space.SetShaderParameter("focus_amount", _focus);
            _space.SetShaderParameter("impact_amount", _impact);
        }
        if (!GodotObject.IsInstanceValid(_grade))
            return;
        _grade.SetShaderParameter("atmosphere_time", _time);
        _grade.SetShaderParameter("focus_amount", _focus);
        _grade.SetShaderParameter("impact_amount", _impact);
        _grade.SetShaderParameter("atmosphere_tint", _tint);
    }

    public override void _Draw() {
        if (_glow == null || (!Engine.IsEditorHint() && !GodotObject.IsInstanceValid(_battle)))
            return;
        Vector2 size = Engine.IsEditorHint() ? new Vector2(1920f, 1080f) : GetViewportRect().Size;
        if (size.X < 1f || size.Y < 1f)
            return;
        // Use actual viewport coordinates despite the background's camera overscan.
        DrawSetTransformMatrix(Engine.IsEditorHint()
            ? GetGlobalTransform().AffineInverse()
            : GetGlobalTransformWithCanvas().AffineInverse());
        float scale = Mathf.Min(size.X / 1920f, size.Y / 1080f);
        DrawDistantStructure(size, scale);
        if (Engine.IsEditorHint())
            return;
    }

    private void DrawDistantStructure(Vector2 size, float scale) {
        // Only the distant landmark has drawn edges. The room reads as matte faces.
        Vector2 center = size * StructureCenter;
        float strength = (0.69f + 0.10f * (_encounterStrength - 1f)) * (1f - _focus * 0.35f);
        float gateScale = 1f - _buttonPress * 0.045f;
        float angle = 0.53f + _structureTime * 0.085f;
        Color structureTint = _phaseTint.Lerp(new Color(0.59f, 0.73f, 0.85f), 0.45f);
        DrawGlow(center, GlowRadius * scale * gateScale, structureTint, (0.045f + _buttonWake * 0.04f) * strength);

        // Rear orbital paths disappear behind the projected geometry.
        DrawOrbitPlanes(center, angle, scale * gateScale, structureTint, strength, false);
        // A single ordinary cube is the landmark. Its depth is enough to read
        // as a solid object without competing with the battle field.
        for (int i = 0; i < 8; i++) {
            float x = (i & 1) == 0 ? -1f : 1f;
            float y = (i & 2) == 0 ? -1f : 1f;
            float z = (i & 4) == 0 ? -1f : 1f;
            float a = angle;
            float rx = x * Mathf.Cos(a) - z * Mathf.Sin(a);
            float rz = x * Mathf.Sin(a) + z * Mathf.Cos(a);
            float b = _structureTime * 0.058f + 0.26f;
            float ry = y * Mathf.Cos(b) - rz * Mathf.Sin(b);
            rz = y * Mathf.Sin(b) + rz * Mathf.Cos(b);
            float depth = 1.0f / (1.0f + rz * 0.18f);
            _cubeVertices[i] = center + new Vector2(rx, ry * ProjectionHeight)
                * StructureSize * 0.72f * scale * gateScale * depth;
            _cubeDepths[i] = depth;
        }
        for (int edge = 0; edge < CubeEdges.GetLength(0); edge++) {
            Vector2 from = _cubeVertices[CubeEdges[edge, 0]];
            Vector2 to = _cubeVertices[CubeEdges[edge, 1]];
            float depth = 0.5f * (_cubeDepths[CubeEdges[edge, 0]] + _cubeDepths[CubeEdges[edge, 1]]);
            float light = Mathf.SmoothStep(0.90f, 1.18f, depth);
            float alpha = Mathf.Lerp(0.085f, 0.44f, light * light) * strength;
            if (edge == 9)
                alpha += _arrivalLight * 0.26f;
            Color edgeColor = structureTint.Lightened(0.24f) with { A = alpha };
            // Leave a small break in each edge so the cube reads as assembled parts.
            DrawLine(from.Lerp(to, 0.04f), from.Lerp(to, 0.70f), edgeColor, 1.55f * scale, true);
            DrawLine(from.Lerp(to, 0.82f), to.Lerp(from, 0.03f), edgeColor with { A = alpha * 0.72f }, 1.25f * scale, true);
        }
        for (int vertex = 0; vertex < 8; vertex++)
            DrawCircle(_cubeVertices[vertex], 1.6f * scale, structureTint.Lightened(0.48f) with { A = 0.19f * strength });

        // Front arcs pass over the projection, making the depth order legible.
        DrawOrbitPlanes(center, angle, scale * gateScale, structureTint, strength, true);
        Color core = structureTint.Lightened(0.52f) with { A = (0.22f + _buttonWake * 0.28f + _buttonPress * 0.20f) * strength };
        float coreRadius = (13f + _buttonWake * 7f) * scale * gateScale;
        DrawArc(center, Vector2.One * coreRadius, 0f, 0f, Mathf.Tau, core, core.A, scale);
    }

    private void DrawOrbitPlanes(Vector2 center, float angle, float scale, Color tint, float strength, bool foreground) {
        for (int orbit = 0; orbit < 2; orbit++) {
            float direction = orbit == 1 ? -1f : 1f;
            float phase = angle * direction * (0.68f + orbit * 0.16f) + orbit * 1.91f;
            float breathing = Mathf.Sin(_structureTime * 0.34f + orbit * 1.7f);
            Vector2 radius = new(
                OrbitRadius.X * (orbit == 0 ? 0.82f : 1.16f) + breathing * (orbit == 0 ? 5f : 8f),
                OrbitRadius.Y * (orbit == 0 ? 0.76f : 1.12f) + breathing * (orbit == 0 ? 3f : 5f));
            float tilt = orbit == 0 ? -0.30f : 0.38f;
            float alpha = (foreground ? (orbit == 0 ? 0.28f : 0.20f) : 0.08f) * strength;
            Color lineTint = foreground ? tint.Lightened(0.20f) : tint;
            // Draw both halves of the same ellipse. The rear half is rendered
            // first, then the front half, so the track remains spatially legible.
            DrawArc(center, radius * scale, tilt, phase + (foreground ? 0f : Mathf.Pi), Mathf.Pi,
                lineTint, alpha, (foreground ? 1.25f : 0.8f) * scale);
            if (!foreground)
                continue;

            float sweep = Mathf.PosMod(_structureTime * (0.048f + orbit * 0.009f) + orbit * 0.37f, 1f);
            float streamAngle = phase + sweep * Mathf.Tau;
            DrawGradientStream(center, radius * scale, tilt, streamAngle,
                0.34f, tint.Lightened(0.40f), 0.29f * strength, 1.55f * scale);
            float markAngle = streamAngle;
            Vector2 mark = center + (new Vector2(Mathf.Cos(markAngle), Mathf.Sin(markAngle)) * radius).Rotated(tilt) * scale;
            DrawCircle(mark, 1.7f * scale, tint.Lightened(0.45f) with { A = 0.34f * strength });
        }
    }

    private void DrawGradientStream(Vector2 center, Vector2 radius, float rotation, float head,
        float length, Color color, float alpha, float width) {
        const int samples = 18;
        Vector2 previous = center + new Vector2(Mathf.Cos(head - length) * radius.X, Mathf.Sin(head - length) * radius.Y).Rotated(rotation);
        for (int i = 1; i <= samples; i++) {
            float t = i / (float)samples;
            float currentAngle = head - length + length * t;
            Vector2 current = center + new Vector2(Mathf.Cos(currentAngle) * radius.X, Mathf.Sin(currentAngle) * radius.Y).Rotated(rotation);
            // A soft bell curve keeps both ends invisible and the head luminous.
            float fade = Mathf.Sin(t * Mathf.Pi * 0.5f);
            fade *= fade;
            float segmentAlpha = alpha * fade * (0.42f + 0.58f * t);
            float segmentWidth = width * (0.42f + 0.58f * fade);
            DrawLine(previous, current, color with { A = segmentAlpha }, segmentWidth, true);
            previous = current;
        }
    }

    private void DrawGlow(Vector2 center, Vector2 radius, Color color, float alpha) {
        DrawTextureRect(_glow, new Rect2(center - radius, radius * 2f), false, color with { A = alpha });
    }

    private void DrawArc(Vector2 center, Vector2 radius, float rotation, float start, float length, Color color, float alpha, float width) {
        for (int i = 0; i < _arcPoints.Length; i++) {
            float angle = start + length * i / (_arcPoints.Length - 1);
            _arcPoints[i] = center + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius).Rotated(rotation);
        }
        DrawPolyline(_arcPoints, color with { A = alpha }, width, true);
    }
}
