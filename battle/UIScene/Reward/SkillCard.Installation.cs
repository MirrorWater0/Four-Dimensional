using System;
using System.Threading.Tasks;
using Godot;

public sealed class CardInstallationOptions {
    public float WindupDuration { get; init; } = 0.09f;
    public float FlyDuration { get; init; } = 0.32f;
    public float ArcHeight { get; init; } = 400;
    public float Pullback { get; init; } = 58;
    public float TrailWidth { get; init; } = 42;
    public float FlightScale { get; init; } = 0.035f;
    public Action<float, float> OnArrival { get; init; }
}

public partial class SkillCard {
    public async Task<bool> FlyInstallationWithImpactAsync(Vector2 endCenter, CardInstallationOptions options) {
        if (!IsInsideTree())
            return false;
        StopBattleMotion();
        Button.Disabled = true;
        HoverHint.Visible = false;
        Vector2 start = GetGlobalRect().GetCenter();
        Vector2 direction = (endCenter - start).Normalized();
        if (direction.IsZeroApprox()) direction = Vector2.Down;
        float side = endCenter.X < start.X ? 1 : -1;
        Vector2 recoil = start - direction * options.Pullback;
        float flightScale = Math.Clamp(options.FlightScale, 0.01f, 0.10f);
        PivotOffset = CardBaseSize / 2;
        Vector2 initialScale = Scale;
        float initialRotation = Rotation;
        PressEffectPartial(0, 1.45f, options.WindupDuration);
        Tween windup = CreateTween();
        windup.TweenMethod(Callable.From<float>(t => {
            if (!GodotObject.IsInstanceValid(this)) return;
            Scale = initialScale.Lerp(Vector2.One * flightScale, t);
            Rotation = Mathf.Lerp(initialRotation, -side * 0.32f, t);
            SetPivotCenterAt(start.Lerp(recoil, t));
        }), 0f, 1f, Math.Max(0.01f, options.WindupDuration))
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        await ToSignal(windup, Tween.SignalName.Finished);
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return false;

        // Throw away from the rack, hook around the far upper point, then dive back into it.
        Rect2 viewport = GetViewportRect();
        float height = Math.Clamp(options.ArcHeight, 80, viewport.Size.Y * 0.65f);
        float reach = Math.Clamp(start.DistanceTo(endCenter) * 0.9f, 240, 460);
        Vector2 control1 = recoil + new Vector2(side * reach, -height * 0.65f);
        Vector2 control2 = recoil + new Vector2(side * reach * 1.9f, -height * 1.55f);
        control1.X = Math.Clamp(control1.X, viewport.Position.X + 65, viewport.End.X - 65);
        control2.X = Math.Clamp(control2.X, viewport.Position.X + 65, viewport.End.X - 65);
        control1.Y = Math.Max(viewport.Position.Y + 65, control1.Y);
        control2.Y = Math.Max(viewport.Position.Y + 65, control2.Y);
        PrepareMoveTrail(out Line trail, out GpuParticles2D particles);
        float originalWidth = trail?.Width ?? 0;
        int originalLength = trail?.TrailLength ?? 0;
        if (trail != null) {
            trail.ManualPreviewMode = true;
            trail.Width = Math.Max(1, options.TrailWidth);
            trail.TrailLength = 90;
            trail.Modulate = new Color(1.3f, 1.55f, 1.7f, 1);
        }
        PressEffectPartial(0, 1.6f, options.FlyDuration);
        float previousT = 0;
        float parentRotation = GetGlobalTransformWithCanvas().X.Angle() - Rotation;
        Tween flight = CreateTween();
        flight.TweenMethod(Callable.From<float>(time => {
            if (!GodotObject.IsInstanceValid(this)) return;
            // Fast launch, readable crest, then a sharply accelerating final strike.
            float t = time < 0.42f
                ? 0.46f * (1 - Mathf.Pow(1 - time / 0.42f, 2))
                : 0.46f + 0.54f * Mathf.Pow((time - 0.42f) / 0.58f, 2.8f);
            Vector2 center = InstallationBezier(recoil, control1, control2, endCenter, t);
            float u = 1 - t;
            Vector2 velocity = 3 * u * u * (control1 - recoil)
                + 6 * u * t * (control2 - control1) + 3 * t * t * (endCenter - control2);
            Scale = Vector2.One * flightScale;
            Rotation = GetRotationWithTopFacingVelocity(velocity) - parentRotation;
            SetPivotCenterAt(center);
            UpdateTrailParticlesRotation(particles, velocity);
            if (trail != null && GodotObject.IsInstanceValid(trail)) {
                // Resample the curve, so even a fast final dive has a continuous trail at low frame rates.
                int segments = Math.Max(1, (int)MathF.Ceiling((t - previousT) * 100));
                for (int i = 1; i <= segments; i++) {
                    float sample = Mathf.Lerp(previousT, t, (float)i / segments);
                    Vector2 point = InstallationBezier(recoil, control1, control2, endCenter, sample);
                    trail.AddPoint(trail.ToLocal(point));
                    if (trail.GetPointCount() > trail.TrailLength) trail.RemovePoint(0);
                }
            }
            previousT = t;
        }), 0f, 1f, Math.Max(0.05f, options.FlyDuration));
        await ToSignal(flight, Tween.SignalName.Finished);
        if (!GodotObject.IsInstanceValid(this)) return false;
        float arrivalRotation = GetGlobalTransformWithCanvas().X.Angle();
        SetCardVisualVisible(false);
        options.OnArrival?.Invoke(arrivalRotation, flightScale);
        try {
            await FadeAndHideMoveTrailAsync(trail, particles, 0.10f);
        }
        finally {
            if (trail != null && GodotObject.IsInstanceValid(trail)) {
                trail.Width = originalWidth;
                trail.TrailLength = originalLength;
            }
        }
        return true;
    }

    private static Vector2 InstallationBezier(Vector2 start, Vector2 a, Vector2 b, Vector2 end, float t) {
        float u = 1 - t;
        return u * u * u * start + 3 * u * u * t * a + 3 * u * t * t * b + t * t * t * end;
    }
}
