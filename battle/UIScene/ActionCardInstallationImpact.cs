using System;
using Godot;

/// <summary>A brief local impact flash; no gameplay or full-screen effects.</summary>
public partial class ActionCardInstallationImpact : Node2D {
    private float _elapsed;
    private const float Duration = 0.24f;
    public override void _Ready() {
        ZIndex = 30;
    }
    public override void _Process(double delta) {
        _elapsed += (float)delta;
        if (_elapsed >= Duration) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw() {
        float progress = Mathf.Clamp(_elapsed / Duration, 0, 1);
        float expansion = 1 - Mathf.Pow(1 - progress, 3);
        float alpha = (1 - progress) * (1 - progress);
        var color = new Color(0.72f, 0.92f, 1, alpha);
        float radius = Mathf.Lerp(12, 76, expansion);
        DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 48, color, Mathf.Lerp(5, 1, expansion), true);
        for (int i = 0; i < 10; i++) {
            Vector2 axis = Vector2.FromAngle(i * Mathf.Tau / 10 + 0.18f);
            float length = i % 2 == 0 ? 48 : 23;
            DrawLine(axis * (radius + 6), axis * (radius + 6 + length * (1 - progress)), color, 3 * alpha + 1, true);
        }
        if (progress < 0.30f) {
            float flash = 1 - progress / 0.30f;
            DrawLine(new Vector2(-54, 0), new Vector2(54, 0), new Color(1, 1, 1, flash), 7 * flash, true);
            DrawLine(new Vector2(0, -70), new Vector2(0, 70), new Color(1, 1, 1, flash), 4 * flash, true);
        }
    }
}
