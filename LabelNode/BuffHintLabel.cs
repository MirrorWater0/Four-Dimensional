using System;
using Godot;

public partial class BuffHintLabel : RichTextLabel
{
    public enum Which
    {
        vanish,
        gain
    }

    public void Initialize(Which which, string name)
    {
        switch (which)
        {
            case Which.vanish:
                Text = $"[color=yellow]{name}[/color]"+"结束";
                break;
            case Which.gain:
                Text = $"[color=yellow]{name}[/color]"+"获得";
                break;
        }
    }
    public override async void _Ready()
    {
        Position += new Vector2(0, -250);
        PivotOffset = Size / 2;
        Random random = new Random();
        int offset = random.Next(-100, 100);
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
        await ToSignal(GetTree().CreateTimer(0.6f), "timeout");
        CreateTween()
            .TweenProperty(this, "position", Position + new Vector2(0, -50), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        // QueueFree();
    }
}
