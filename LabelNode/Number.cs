using System;
using Godot;

public partial class Number : Node2D
{
    public Label NumberLabel => field ??= GetNode<Label>("Label");

    private static readonly System.Collections.Generic.List<Number> ActiveNumbers = new();
    private const float BaseLift = 120f;
    private const float OffsetMin = -60f;
    private const float OffsetMax = 60f;
    private const float PopRise1 = 80f;
    private const float PopRise2 = 120f;
    private const float FallDrop = 140f;
    private const float HoverRise = 100f;
    private const int StackAttemptsPerStep = 4;
    private const int MaxOffsetAttempts = 24;
    private const float ViewportPadding = 12f;
    private Rect2 _hoverRect;
    private bool _registered;

    public override async void _Ready()
    {
        GlobalPosition += new Vector2(0, -BaseLift);
        Scale = new Vector2(0.2f, 0.2f);
        Modulate = new Color(1, 1, 1, 0);

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        ApplyOutlineFromFontColor(NumberLabel.GetThemeColor("font_color"));
        GlobalPosition = ClampToViewport(GlobalPosition);
        Vector2 startPos = GlobalPosition;
        Vector2 labelSize = GetLabelSize();
        float stackStep = Mathf.Max(24f, labelSize.Y * 0.6f);
        float padding = Mathf.Max(6f, labelSize.Y * 0.1f);
        (float offset, float stackRise) = PickOffset(
            startPos,
            labelSize,
            OffsetMin,
            OffsetMax,
            stackStep,
            padding
        );
        startPos = ClampToViewport(startPos + new Vector2(0, -stackRise));
        GlobalPosition = startPos;
        Rect2 baseRect = BuildBaseRectAt(startPos, labelSize);
        RegisterHoverRect(OffsetRectForHover(baseRect, offset, padding));

        Tween pop = CreateTween();
        pop.SetParallel(true);
        pop.TweenProperty(this, "modulate", new Color(1, 1, 1, 1f), 0.06f)
            .SetEase(Tween.EaseType.Out);
        pop.TweenProperty(
                this,
                "global_position",
                ClampToViewport(startPos + new Vector2(offset * 0.25f, -PopRise1)),
                0.1f
            )
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        pop.TweenProperty(this, "scale", new Vector2(1.4f, 1.4f), 0.1f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        pop.SetParallel(false);
        pop.TweenProperty(this, "scale", new Vector2(1.0f, 1.0f), 0.08f)
            .SetEase(Tween.EaseType.Out);
        pop.TweenProperty(
                this,
                "global_position",
                ClampToViewport(startPos + new Vector2(offset * 0.35f, -PopRise2)),
                0.08f
            )
            .SetEase(Tween.EaseType.Out);

        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

        Vector2 fallPos = ClampToViewport(startPos + new Vector2(offset * 0.4f, FallDrop));
        Tween fall = CreateTween();
        fall.SetParallel(true);
        fall.TweenProperty(this, "global_position", fallPos, 0.4f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Cubic);
        fall.TweenProperty(this, "scale", new Vector2(0.85f, 0.85f), 0.1f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        Color startColor = NumberLabel.GetThemeColor("font_color");
        fall.TweenMethod(Callable.From<Color>(ApplyNumberColor), startColor, Colors.White, 0.35f)
            .SetEase(Tween.EaseType.Out);
        fall.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.22f)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.18f);

        await ToSignal(GetTree().CreateTimer(0.4f), "timeout");
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (_registered)
        {
            ActiveNumbers.Remove(this);
            _registered = false;
        }
    }

    private Rect2 BuildBaseRect()
    {
        return BuildBaseRectAt(GlobalPosition, GetLabelSize());
    }

    private Rect2 BuildBaseRectAt(Vector2 center, Vector2 size)
    {
        Vector2 topLeft = center - size * 0.5f;
        return new Rect2(topLeft, size);
    }

    private static Rect2 OffsetRectForHover(Rect2 baseRect, float offset, float padding)
    {
        Vector2 delta = new Vector2(offset * 0.45f, -HoverRise);
        Rect2 rect = new Rect2(baseRect.Position + delta, baseRect.Size);
        return rect.Grow(padding);
    }

    private void RegisterHoverRect(Rect2 rect)
    {
        _hoverRect = rect;
        ActiveNumbers.Add(this);
        _registered = true;
    }

    private (float offset, float stackRise) PickOffset(
        Vector2 startPos,
        Vector2 labelSize,
        float min,
        float max,
        float stackStep,
        float padding
    )
    {
        float offset = 0f;
        float stackRise = 0f;
        for (int i = 0; i < MaxOffsetAttempts; i++)
        {
            offset = (float)GD.RandRange(min, max);
            stackRise = (i / StackAttemptsPerStep) * stackStep;
            Vector2 stackedPos = ClampToViewport(startPos + new Vector2(0, -stackRise));
            Rect2 baseRect = BuildBaseRectAt(stackedPos, labelSize);
            Rect2 candidate = OffsetRectForHover(baseRect, offset, padding);
            if (!OverlapsAny(candidate))
                return (offset, stackRise);
        }

        return (offset, stackRise);
    }

    private Vector2 GetLabelSize()
    {
        Vector2 size = NumberLabel.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(80, 80);
        return size;
    }

    public void SetNumberColor(Color color)
    {
        ApplyNumberColor(color);
    }

    private void ApplyOutlineFromFontColor(Color color)
    {
        float r = Mathf.Clamp(color.R * 0.35f, 0f, 1f);
        float g = Mathf.Clamp(color.G * 0.35f, 0f, 1f);
        float b = Mathf.Clamp(color.B * 0.35f, 0f, 1f);
        NumberLabel.AddThemeColorOverride("outline_color", new Color(r, g, b, 0.95f));
    }

    private void ApplyNumberColor(Color color)
    {
        NumberLabel.AddThemeColorOverride("font_color", color);
        ApplyOutlineFromFontColor(color);
    }

    private static bool OverlapsAny(Rect2 rect)
    {
        for (int i = ActiveNumbers.Count - 1; i >= 0; i--)
        {
            var number = ActiveNumbers[i];
            if (!GodotObject.IsInstanceValid(number))
            {
                ActiveNumbers.RemoveAt(i);
                continue;
            }

            if (rect.Intersects(number._hoverRect))
                return true;
        }

        return false;
    }

    private Vector2 ClampToViewport(Vector2 globalPos)
    {
        var viewport = GetViewport();
        if (viewport == null)
            return globalPos;

        Rect2 visible = viewport.GetVisibleRect();
        Vector2 size = NumberLabel.GetCombinedMinimumSize();
        if (size == Vector2.Zero)
            size = new Vector2(80, 80);

        Vector2 half = size * 0.5f;
        Vector2 margin = half + new Vector2(ViewportPadding, ViewportPadding);
        Vector2 end = visible.Position + visible.Size;

        Transform2D canvas = viewport.GetCanvasTransform();
        Transform2D inverse = canvas.AffineInverse();
        Vector2 viewPos = canvas * globalPos;
        float x = Mathf.Clamp(viewPos.X, visible.Position.X + margin.X, end.X - margin.X);
        float y = Mathf.Clamp(viewPos.Y, visible.Position.Y + margin.Y, end.Y - margin.Y);

        return inverse * new Vector2(x, y);
    }
}
