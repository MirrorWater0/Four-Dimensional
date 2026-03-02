using Godot;

public partial class DynamicCamera : Camera2D
{
    [Export]
    public float WorldLeftBoundary = 0.0f;

    [Export]
    public float WorldRightBoundary = 3500.0f;

    [Export]
    public float HalfViewportWidth = 960.0f;

    [Export]
    public float FixedCenterY = 540.0f;

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
        GetHorizontalCenterBoundary(out float minX, out float maxX);
        return new Vector2(Mathf.Clamp(position.X, minX, maxX), FixedCenterY);
    }

    public bool IsInsideBoundary(Vector2 position)
    {
        GetHorizontalCenterBoundary(out float minX, out float maxX);
        return position.X >= minX && position.X <= maxX;
    }

    public Vector2 ApplyBoundaryResistance(Vector2 position)
    {
        GetHorizontalCenterBoundary(out float minX, out float maxX);
        return new Vector2(ApplyAxisResistance(position.X, minX, maxX), FixedCenterY);
    }

    private void GetHorizontalCenterBoundary(out float minX, out float maxX)
    {
        float left = Mathf.Min(WorldLeftBoundary, WorldRightBoundary);
        float right = Mathf.Max(WorldLeftBoundary, WorldRightBoundary);
        float halfViewWidth = Mathf.Max(0.0f, HalfViewportWidth);
        minX = left + halfViewWidth;
        maxX = right - halfViewWidth;

        if (minX > maxX)
        {
            float centerX = (left + right) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }
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
