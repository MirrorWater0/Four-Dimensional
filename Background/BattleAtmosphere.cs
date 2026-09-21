using System;
using Godot;

// Screen composition belongs to battle; the shared starfield also serves menus.
public partial class BattleAtmosphere : Control {
    private static readonly Color NormalTint = new(0.28f, 0.46f, 0.61f);
    private static readonly Color EliteTint = new(0.49f, 0.34f, 0.66f);
    private static readonly Color BossTint = new(0.65f, 0.31f, 0.35f);
    private readonly Vector2[] _arcPoints = new Vector2[49];
    private readonly Vector2[] _vertices = new Vector2[16];
    private readonly float[] _depths = new float[16];
    private Battle _battle;
    private StarfieldBackground3D _starfield;
    private ShaderMaterial _grade;
    private GradientTexture2D _glow;
    private CanvasLayer _tipLayer;
    private float _time;
    private float _structureTime;
    private float _focus;
    private float _impact;
    private float _impactCooldown;
    private float _pollTime;
    private bool _inspecting;
    private Color _tint = NormalTint;
    private float _encounterStrength = 1f;
    private bool _focused = true;

    public float FocusAmount => _focus;
    public float ImpactAmount => _impact;

    public override void _Ready() {
        MouseFilter = MouseFilterEnum.Ignore;
        _battle = GetParent()?.GetParent() as Battle;
        _starfield = GetNodeOrNull<StarfieldBackground3D>("../StarfieldBackground3D");
        if (GetNodeOrNull<ColorRect>("../BackgroundHueNeutralizer") is { } neutralizer
            && neutralizer.Material is ShaderMaterial material) {
            // Each battle owns its pulse and tint, including warmup instances.
            _grade = (ShaderMaterial)material.Duplicate();
            neutralizer.Material = _grade;
        }
        _glow = new GradientTexture2D {
            Width = 128, Height = 128,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(0.5f, 1f),
            Gradient = new Gradient {
                Offsets = new[] { 0f, 0.25f, 0.6f, 1f },
                Colors = new[] { Colors.White, new Color(1f, 1f, 1f, 0.6f), new Color(1f, 1f, 1f, 0.12f), new Color(1f, 1f, 1f, 0f) }
            }
        };
        _focused = GetWindow()?.HasFocus() ?? true;
        SetProcess(_focused);
        PollPresentation();
        UpdateGrade();
    }

    public override void _Notification(int what) {
        if (what == MainLoop.NotificationApplicationFocusIn || what == MainLoop.NotificationApplicationFocusOut) {
            _focused = what == MainLoop.NotificationApplicationFocusIn;
            SetProcess(_focused);
        }
    }

    public override void _Process(double delta) {
        float dt = Mathf.Min((float)delta, 0.1f);
        _pollTime -= dt;
        if (_pollTime <= 0f) {
            _pollTime = 0.10f;
            PollPresentation();
        }
        _focus = Mathf.Lerp(_focus, _inspecting ? 1f : 0f, 1f - Mathf.Exp(-dt * 5f));
        _impact = Mathf.MoveToward(_impact, 0f, dt * 0.85f);
        _impactCooldown = Mathf.Max(0f, _impactCooldown - dt);
        float motion = Mathf.Lerp(0.48f, 0.10f, _focus);
        _time += dt * motion;
        // The landmark must visibly turn within a few seconds, independently of
        // the much slower starfield. Inspection still softens its movement.
        _structureTime += dt * Mathf.Lerp(1f, 0.35f, _focus) * (1f + _impact * 0.5f);
        if (GodotObject.IsInstanceValid(_starfield))
            _starfield.SetPresentationMotionScale(motion);
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
        (_tint, _encounterStrength) = _battle.CurrentLevelNode?.Type switch {
            LevelNode.LevelType.Elite => (EliteTint, 1.25f),
            LevelNode.LevelType.Boss => (BossTint, 1.6f),
            _ => (NormalTint, 1f)
        };
    }

    private void UpdateGrade() {
        if (!GodotObject.IsInstanceValid(_grade))
            return;
        _grade.SetShaderParameter("atmosphere_time", _time);
        _grade.SetShaderParameter("focus_amount", _focus);
        _grade.SetShaderParameter("impact_amount", _impact);
        _grade.SetShaderParameter("atmosphere_tint", _tint);
    }

    public override void _Draw() {
        if (_glow == null || !GodotObject.IsInstanceValid(_battle))
            return;
        Vector2 size = GetViewportRect().Size;
        if (size.X < 1f || size.Y < 1f)
            return;
        // Use actual viewport coordinates despite the background's camera overscan.
        DrawSetTransformMatrix(GetGlobalTransformWithCanvas().AffineInverse());
        float scale = Mathf.Min(size.X / 1920f, size.Y / 1080f);
        DrawDistantStructure(size, scale);
        DrawGlow(new Vector2(size.X * 0.27f, size.Y * 0.51f), new Vector2(size.X * 0.31f, size.Y * 0.17f), NormalTint, 0.065f);
        DrawGlow(new Vector2(size.X * 0.74f, size.Y * 0.51f), new Vector2(size.X * 0.29f, size.Y * 0.18f), _tint, 0.055f);
        DrawCharacterGroup(_battle.PlayersList, NormalTint, scale);
        DrawCharacterGroup(_battle.EnemiesList, _tint, scale);
        DrawCharacterGroup(_battle.PlayerSummons, NormalTint, scale);
        DrawCharacterGroup(_battle.EnemySummons, _tint, scale);
    }

    private void DrawCharacterGroup<T>(System.Collections.Generic.List<T> characters, Color tint, float scale) where T : Character {
        foreach (Character character in characters) {
            if (!GodotObject.IsInstanceValid(character) || !character.IsInsideTree() || !character.IsVisibleInTree() || character.IsQueuedForDeletion())
                continue;
            Transform2D transform = character.GetGlobalTransformWithCanvas();
            Vector2 center = transform * Vector2.Zero;
            Vector2 foot = transform * new Vector2(0f, 238f);
            float radius = Mathf.Clamp(transform.X.Length() * 130f, 58f * scale, 150f * scale);
            DrawGlow(center + Vector2.Down * 25f * scale, new Vector2(radius * 1.35f, radius * 1.9f), tint, 0.095f);
            DrawGlow(foot, new Vector2(radius * 1.35f, radius * 0.24f), tint, 0.20f);
            float phase = _time * 0.12f + character.PositionIndex * 0.7f;
            DrawArc(foot, new Vector2(radius, radius * 0.16f), 0f, phase, 2.25f, tint, 0.15f, 1f * scale);
            DrawArc(foot, new Vector2(radius * 1.18f, radius * 0.22f), 0f, phase + 3.0f, 1.75f, tint, 0.075f, 1f * scale);
        }
    }

    private void DrawDistantStructure(Vector2 size, float scale) {
        Vector2 center = new(size.X * 0.52f, size.Y * 0.105f);
        float strength = _encounterStrength * (1f - _focus * 0.25f) + _impact * 0.8f;
        float angle = _structureTime * 0.42f;
        DrawGlow(center, new Vector2(360f, 170f) * scale, _tint, 0.14f * strength);
        for (int arc = 0; arc < 4; arc++) {
            float direction = arc % 2 == 0 ? 1f : -1f;
            float start = arc * Mathf.Pi * 0.5f + angle * direction * 0.65f;
            float tilt = -0.16f + Mathf.Sin(_structureTime * 0.24f + arc * 0.8f) * 0.10f;
            float breathing = Mathf.Sin(_structureTime * 0.48f + arc * 1.6f);
            Vector2 radius = new Vector2(245f + arc % 2 * 13f + breathing * 8f, 68f + arc % 2 * 9f + breathing * 8f) * scale;
            DrawArc(center, radius, tilt, start, 0.93f, _tint, 0.11f * strength, 3f * scale);
            DrawArc(center, radius, tilt, start, 0.93f, _tint.Lightened(0.2f), 0.21f * strength, 0.85f * scale);
            float sweep = 0.5f + 0.5f * Mathf.Sin(_structureTime * 0.85f + arc);
            DrawArc(center, radius, tilt, start + sweep * 0.73f, 0.16f,
                _tint.Lightened(0.38f), 0.29f * strength, 1.3f * scale);
        }
        // Independent XW / YW / ZW rotations turn inner cells through outer cells;
        // a slower YZ tumble makes the changing depth legible in projection.
        for (int i = 0; i < 16; i++) {
            float x = (i & 1) == 0 ? -1f : 1f;
            float y = (i & 2) == 0 ? -1f : 1f;
            float z = (i & 4) == 0 ? -1f : 1f;
            float w = (i & 8) == 0 ? -1f : 1f;
            float a = angle + 0.48f;
            float rx = x * Mathf.Cos(a) - w * Mathf.Sin(a);
            float rw = x * Mathf.Sin(a) + w * Mathf.Cos(a);
            float b = _structureTime * 0.29f + 0.24f;
            float rotatedY = y * Mathf.Cos(b) - rw * Mathf.Sin(b);
            rw = y * Mathf.Sin(b) + rw * Mathf.Cos(b);
            float c = _structureTime * -0.21f;
            float rotatedZ = z * Mathf.Cos(c) - rw * Mathf.Sin(c);
            rw = z * Mathf.Sin(c) + rw * Mathf.Cos(c);
            float tumble = 0.62f + _structureTime * 0.13f;
            float ry = rotatedY * Mathf.Cos(tumble) - rotatedZ * Mathf.Sin(tumble);
            float rz = rotatedY * Mathf.Sin(tumble) + rotatedZ * Mathf.Cos(tumble);
            float projection = 2.5f / (3.5f - rw);
            float depth = 3.5f / (4.5f - rz * projection);
            _vertices[i] = center + new Vector2(rx, ry * 0.50f) * projection * depth * 85f * scale;
            _depths[i] = depth;
        }
        for (int i = 0; i < 16; i++) {
            for (int dimension = 0; dimension < 4; dimension++) {
                int next = i ^ (1 << dimension);
                if (next <= i)
                    continue;
                Vector2 from = _vertices[i];
                Vector2 to = _vertices[next];
                float alpha = Mathf.Clamp((_depths[i] + _depths[next]) * 0.075f, 0.07f, 0.19f) * strength;
                // Small breaks keep the structure incomplete, like displaced space.
                DrawLine(from.Lerp(to, 0.05f), from.Lerp(to, 0.86f), _tint.Lightened(0.22f) with { A = alpha }, 1f * scale, true);
                if ((i + dimension) % 3 == 0) {
                    float travel = Mathf.PosMod(_structureTime * 0.32f + i * 0.17f + dimension * 0.23f, 1f);
                    float fade = Mathf.Sin(travel * Mathf.Pi);
                    Vector2 spark = from.Lerp(to, 0.05f + travel * 0.81f);
                    DrawLine(from.Lerp(to, Mathf.Max(0.05f, travel * 0.81f - 0.07f)), spark,
                        _tint.Lightened(0.48f) with { A = alpha * fade * 1.8f }, 1.2f * scale, true);
                    DrawCircle(spark, 1.5f * scale, _tint.Lightened(0.55f) with { A = alpha * fade * 2f });
                }
            }
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
