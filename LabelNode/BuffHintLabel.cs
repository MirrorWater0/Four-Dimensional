using System;
using System.Collections.Generic;
using Godot;

public partial class BuffHintLabel : RichTextLabel
{
    private static readonly PackedScene HintPackedScene = GD.Load<PackedScene>(
        "res://LabelNode/BuffHintLabel.tscn"
    );
    private static readonly Stack<BuffHintLabel> Pool = new();
    private const int MaxPoolSize = 160;

    [Export]
    public float BaseYOffset { get; set; } = -230f;

    [Export]
    public float RiseHeight { get; set; } = 50f;

    [Export]
    public float StartScale { get; set; } = 0.7f;

    [Export]
    public float PeakScale { get; set; } = 1f;

    [Export]
    public float FadeInBrightness { get; set; } = 1.5f;

    [Export]
    public float PopDuration { get; set; } = 0.2f;

    [Export]
    public float FadeInDuration { get; set; } = 0.3f;

    [Export]
    public float HoldDuration { get; set; } = 1f;

    [Export]
    public float FinalMoveDuration { get; set; } = 0.2f;

    [Export]
    public float FadeOutDuration { get; set; } = 0.2f;

    [Export]
    public float EndDelay { get; set; } = 0.3f;

    [Export]
    public float FinalRiseMultiplier { get; set; } = 1.25f;

    [Export]
    public float RandomXRange { get; set; } = 100f;

    [Export]
    public float RandomYRange { get; set; } = 70f;

    [Export]
    public float RandomPopXFactor { get; set; } = 0.45f;

    [Export]
    public float RandomFallXFactor { get; set; } = 0.75f;

    [Export]
    public float RandomFallYOffset { get; set; } = 80f;

    [Export]
    public float RandomFallYFactor { get; set; } = 0.4f;

    [Export]
    public float RandomFallDuration { get; set; } = 0.32f;

    [Export]
    public float RandomFadeOutDuration { get; set; } = 0.24f;

    [Export]
    public float RandomFadeOutDelay { get; set; } = 0.1f;

    [Export]
    public float RandomEndDelay { get; set; } = 0.35f;

    public bool RandomOffset = false;
    private int _playVersion;
    private readonly List<Tween> _runningTweens = new();

    public enum Which
    {
        vanish,
        gain,
    }

    public Vector2 TargetPosition { get; set; } = Vector2.Zero;

    public static BuffHintLabel Spawn(
        Node parent,
        string text,
        Vector2 targetPosition,
        bool randomOffset = false
    )
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent))
            return null;

        BuffHintLabel label;
        while (Pool.Count > 0)
        {
            label = Pool.Pop();
            if (label != null && GodotObject.IsInstanceValid(label))
                goto Found;
        }

        label = HintPackedScene.Instantiate<BuffHintLabel>();

        Found:
        label.StopRunningTweens();
        label._playVersion++;
        label.Text = text ?? string.Empty;
        label.TargetPosition = targetPosition;
        label.RandomOffset = randomOffset;
        label.Visible = false;
        label.TopLevel = true;
        label.SetAnchorsPreset(LayoutPreset.TopLeft);
        label.OffsetLeft = 0f;
        label.OffsetTop = 0f;
        label.OffsetRight = 0f;
        label.OffsetBottom = 0f;
        label.Scale = new Vector2(label.StartScale, label.StartScale);
        label.Modulate = new Color(1, 1, 1, 0);
        parent.AddChild(label);
        label.GlobalPosition = targetPosition + new Vector2(0, label.BaseYOffset);
        label.CallDeferred(nameof(BeginDisplay), label._playVersion);
        return label;
    }

    public void Initialize(Which which, string name)
    {
        Text = which switch
        {
            Which.vanish => $"{name}[color=yellow]消失[/color]",
            Which.gain => $"{name}[color=yellow]获得[/color]",
            _ => name ?? string.Empty,
        };
        return;
    }

    private async void BeginDisplay(int version)
    {
        if (version != _playVersion || !IsInsideTree())
            return;

        ApplyOutlineFromDefaultColor();
        float rise = Mathf.Max(RiseHeight, 0f);
        Vector2 drift = Vector2.Zero;
        if (RandomOffset)
        {
            float randomX = (float)GD.RandRange(-RandomXRange, RandomXRange);
            float randomY = (float)GD.RandRange(-RandomYRange, RandomYRange);
            drift = new Vector2(randomX, randomY);
        }
        // Wait for layout to be calculated
        await ToSignal(GetTree(), "process_frame");
        if (version != _playVersion || !IsInsideTree())
            return;
        // Center the label horizontally by offsetting by half the width
        float centeredX = -Size.X / 2;
        Vector2 basePos = TargetPosition + drift + new Vector2(centeredX, BaseYOffset);
        GlobalPosition = basePos;
        Visible = true;

        PivotOffset = Size / 2;

        bool enableRiseMotion = rise > 0f;
        Scale = enableRiseMotion ? new Vector2(StartScale, StartScale) : Vector2.One;
        Modulate = new Color(1, 1, 1, 0);

        if (RandomOffset)
        {
            Tween pop = CreateManagedTween();
            pop.SetParallel(true);
            pop.TweenProperty(
                    this,
                    "global_position",
                    basePos + new Vector2(drift.X * RandomPopXFactor, -rise),
                    PopDuration
                )
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);
            pop.TweenProperty(this, "modulate", FadeInBrightness * new Color(1, 1, 1, 1f), FadeInDuration)
                .SetEase(Tween.EaseType.Out);
            if (enableRiseMotion)
                pop.TweenProperty(this, "scale", new Vector2(PeakScale, PeakScale), PopDuration)
                    .SetEase(Tween.EaseType.Out);

            await ToSignal(GetTree().CreateTimer(HoldDuration), "timeout");

            Vector2 fallPos = basePos + new Vector2(
                drift.X * RandomFallXFactor,
                RandomFallYOffset + drift.Y * RandomFallYFactor
            );
            Tween fall = CreateManagedTween();
            fall.SetParallel(true);
            fall.TweenProperty(this, "global_position", fallPos, RandomFallDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Cubic);
            fall.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), RandomFadeOutDuration)
                .SetEase(Tween.EaseType.In)
                .SetDelay(RandomFadeOutDelay);

            await ToSignal(GetTree().CreateTimer(RandomEndDelay), "timeout");
            if (version != _playVersion)
                return;
            ReturnToPool();
            return;
        }

        if (enableRiseMotion)
        {
            CreateManagedTween()
                .TweenProperty(this, "global_position", basePos + new Vector2(0, -rise), PopDuration)
                .SetEase(Tween.EaseType.Out);
        }
        CreateManagedTween()
            .TweenProperty(this, "modulate", FadeInBrightness * new Color(1, 1, 1, 1f), FadeInDuration)
            .SetEase(Tween.EaseType.Out);
        if (enableRiseMotion)
        {
            CreateManagedTween()
                .TweenProperty(this, "scale", new Vector2(PeakScale, PeakScale), PopDuration)
                .SetEase(Tween.EaseType.Out);
        }
        await ToSignal(GetTree().CreateTimer(HoldDuration), "timeout");
        if (enableRiseMotion)
        {
            CreateManagedTween()
                .TweenProperty(
                    this,
                    "global_position",
                    basePos + new Vector2(0, -rise * FinalRiseMultiplier),
                    FinalMoveDuration
                )
                .SetEase(Tween.EaseType.Out);
        }
        CreateManagedTween()
            .TweenProperty(this, "modulate", new Color(1, 1, 1, 0), FadeOutDuration)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(GetTree().CreateTimer(EndDelay), "timeout");
        if (version != _playVersion)
            return;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        StopRunningTweens();
        if (GetParent() != null)
            GetParent().RemoveChild(this);

        Visible = false;
        Position = Vector2.Zero;
        GlobalPosition = Vector2.Zero;
        TargetPosition = Vector2.Zero;
        RandomOffset = false;

        if (Pool.Count < MaxPoolSize)
            Pool.Push(this);
        else
            QueueFree();
    }

    private Tween CreateManagedTween()
    {
        var tween = CreateTween();
        _runningTweens.Add(tween);
        tween.Finished += () => _runningTweens.Remove(tween);
        return tween;
    }

    private void StopRunningTweens()
    {
        for (int i = _runningTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = _runningTweens[i];
            if (tween != null && GodotObject.IsInstanceValid(tween))
                tween.Kill();
        }
        _runningTweens.Clear();
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
