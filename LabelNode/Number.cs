using System;
using System.Threading;
using Godot;

public partial class Number : Node2D
{
    public Label NumberLabel => field ??= GetNode<Label>("Label");

    public override async void _Ready()
    {
        Position += new Vector2(0, -200);
        Random random = new Random();
        int offset = random.Next(-100, 100);
        Scale = new Vector2(0.1f, 0.1f);
        Modulate = new Color(1, 1, 1, 0);
        CreateTween()
            .TweenProperty(this, "position", Position + new Vector2(offset, -80), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0.8f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        CreateTween()
            .TweenProperty(this, "scale", new Vector2(2.5f, 2.5f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(0.6f), "timeout");
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
