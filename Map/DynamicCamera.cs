using Godot;

public partial class DynamicCamera : Camera2D
{
    [Export]
    public Vector2 MinBoundary = new(966, 543);

    [Export]
    public Vector2 MaxBoundary = new(2271, 1423);

    [Export(PropertyHint.Range, "1,400,1")]
    public float OverscrollDistance = 230.0f;

    [Export(PropertyHint.Range, "0.05,1.0,0.01")]
    public float OverscrollResistance = 0.72f;

    [Export(PropertyHint.Range, "1,120,1")]
    public float FollowStrength = 40.0f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float VelocityDamping = 0.97f;

    [Export(PropertyHint.Range, "0,2,0.01")]
    public float ReleaseInertiaMultiplier = 0.18f;

    [Export(PropertyHint.Range, "100,8000,10")]
    public float MaxReleaseSpeed = 1800.0f;

    public Vector2 ClampToBoundary(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.X, MinBoundary.X, MaxBoundary.X),
            Mathf.Clamp(position.Y, MinBoundary.Y, MaxBoundary.Y)
        );
    }

    public bool IsInsideBoundary(Vector2 position)
    {
        return position.X >= MinBoundary.X
            && position.X <= MaxBoundary.X
            && position.Y >= MinBoundary.Y
            && position.Y <= MaxBoundary.Y;
    }

    public Vector2 ApplyBoundaryResistance(Vector2 position)
    {
        return new Vector2(
            ApplyAxisResistance(position.X, MinBoundary.X, MaxBoundary.X),
            ApplyAxisResistance(position.Y, MinBoundary.Y, MaxBoundary.Y)
        );
    }

    private float ApplyAxisResistance(float value, float min, float max)
    {
        if (value < min)
        {
            float beyond = min - value;
            return min - SoftenedOverscroll(beyond);
        }

        if (value > max)
        {
            float beyond = value - max;
            return max + SoftenedOverscroll(beyond);
        }

        return value;
    }

    private float SoftenedOverscroll(float beyond)
    {
        float damped = beyond * OverscrollResistance;
        if (OverscrollDistance <= 0.01f)
        {
            return damped;
        }

        float normalized = damped / OverscrollDistance;
        return OverscrollDistance * (1.0f - Mathf.Exp(-normalized));
    }
}
