using System;
using Godot;

public partial class BuffHintLabel : RichTextLabel
{
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
                Text = $"[color=yellow]{name}[/color]" + "结束";
                break;
            case Which.gain:
                Text = $"[color=yellow]{name}[/color]" + "获得";
                break;
        }
    }

    public override async void _Ready()
    {
        // Wait for layout to be calculated
        await ToSignal(GetTree(), "process_frame");
        Modulate *= 2f;
        // Center the label horizontally by offsetting by half the width
        float centeredX = -Size.X / 2;
        Position = TargetPosition + new Vector2(centeredX, -250);

        PivotOffset = Size / 2;

        Random random = new Random();
        Scale = new Vector2(0.1f, 0.1f);
        Modulate = new Color(1, 1, 1, 0);
        CreateTween()
            .TweenProperty(this, "position", Position + new Vector2(0, -40), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0.8f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "scale", new Vector2(1f, 1f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        CreateTween()
            .TweenProperty(this, "position", Position + new Vector2(0, -50), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        QueueFree();
    }
}
