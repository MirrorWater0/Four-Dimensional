using System;
using Godot;

public partial class BuffHintLabel : RichTextLabel
{
    public bool RandomOffset = false;

    public enum Which
    {
        vanish,
        gain,
    }

    public Vector2 TargetPosition { get; set; } = Vector2.Zero;

    public void Initialize(Which which, string name)
    {
        switch (which)
        {
            case Which.vanish:
                Text = $"{name}[color=yellow]结束[/color]";
                break;
            case Which.gain:
                Text = $"{name}[color=yellow]获得[/color]";
                break;
        }
    }

    public override async void _Ready()
    {
        ApplyOutlineFromDefaultColor();
        Vector2 drift = Vector2.Zero;
        if (RandomOffset)
        {
            float randomX = (float)GD.RandRange(-100, 100);
            float randomY = (float)GD.RandRange(-70, 70);
            drift = new Vector2(randomX, randomY);
            TargetPosition += drift;
        }
        // Wait for layout to be calculated
        await ToSignal(GetTree(), "process_frame");
        // Center the label horizontally by offsetting by half the width
        float centeredX = -Size.X / 2;
        Vector2 basePos = TargetPosition + new Vector2(centeredX, -230);
        Position = basePos;

        PivotOffset = Size / 2;

        Scale = new Vector2(0.7f, 0.7f);
        Modulate = new Color(1, 1, 1, 0);

        if (RandomOffset)
        {
            Tween pop = CreateTween();
            pop.SetParallel(true);
            pop.TweenProperty(this, "position", basePos + new Vector2(drift.X * 0.45f, -40 + drift.Y * 0.2f), 0.2f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            pop.TweenProperty(this, "modulate", 1.5f * new Color(1, 1, 1, 1f), 0.3f)
                .SetEase(Tween.EaseType.Out);
            pop.TweenProperty(this, "scale", new Vector2(1f, 1f), 0.2f)
                .SetEase(Tween.EaseType.Out);

            await ToSignal(GetTree().CreateTimer(1f), "timeout");

            Vector2 fallPos = basePos + new Vector2(drift.X * 0.75f, 80 + drift.Y * 0.4f);
            Tween fall = CreateTween();
            fall.SetParallel(true);
            fall.TweenProperty(this, "position", fallPos, 0.32f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            fall.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.24f)
                .SetEase(Tween.EaseType.In)
                .SetDelay(0.1f);

            await ToSignal(GetTree().CreateTimer(0.35f), "timeout");
            QueueFree();
            return;
        }

        CreateTween()
            .TweenProperty(this, "position", basePos + new Vector2(0, -40), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", 1.5f * new Color(1, 1, 1, 1f), 0.3f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        CreateTween()
            .TweenProperty(this, "position", basePos + new Vector2(0, -50), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        QueueFree();
    }

    private void ApplyOutlineFromDefaultColor()
    {
        Color baseColor = GetThemeColor("default_color");
        // Keep outline dark so the shader treats it as outline (not add-blended text).
        const float factor = 0.08f;
        Color outline = new Color(
            Mathf.Clamp(baseColor.R * factor, 0f, 1f),
            Mathf.Clamp(baseColor.G * factor, 0f, 1f),
            Mathf.Clamp(baseColor.B * factor, 0f, 1f),
            1f
        );
        AddThemeColorOverride("outline_color", outline);
    }
}
