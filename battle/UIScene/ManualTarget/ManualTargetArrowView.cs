using Godot;

public partial class ManualTargetArrowView : Control
{
    private static readonly Shader TipShader = GD.Load<Shader>(
        "res://shader/UI/ManualTargetArrowTip.gdshader"
    );

    [Export]
    public Color ArrowColor { get; set; } = new(0.76f, 0.95f, 1f, 0.96f);

    [Export]
    public Color ShadowColor { get; set; } = new(0.01f, 0.04f, 0.06f, 0.78f);

    [Export]
    public float ArrowWidth { get; set; } = 6f;

    [Export]
    public float ShadowWidth { get; set; } = 11f;

    [Export]
    public float CurveLift { get; set; } = 96f;

    [Export]
    public int PointCount { get; set; } = 24;

    [Export]
    public Vector2 HeadSize { get; set; } = new(34f, 28f);

    [Export]
    public Vector2 TailSize { get; set; } = new(24f, 16f);

    [Export]
    public float HeadShaftInset { get; set; } = 24f;

    [Export]
    public float TailShaftInset { get; set; } = 14f;

    [Export(PropertyHint.Range, "0,0.35,0.005")]
    public float TipOutlineWidth { get; set; } = 0.11f;

    [Export(PropertyHint.Range, "0,0.08,0.001")]
    public float TipEdgeSoftness { get; set; } = 0.01f;

    public float HeadDefaultScale { get; set; } = 0.95f;
    public float HeadHoverScale { get; set; } = 1.05f;

    private ColorRect HeadTip => field ??= GetNodeOrNull<ColorRect>("HeadTip");
    private ColorRect TailTip => field ??= GetNodeOrNull<ColorRect>("TailTip");
    private ShaderMaterial _headTipMaterial;
    private ShaderMaterial _tailTipMaterial;
    private Tween _headTipScaleTween;
    private float _headTipScale = 0.95f;
    private Vector2[] _lastPoints = System.Array.Empty<Vector2>();
    private Vector2[] _shaftPoints = System.Array.Empty<Vector2>();
    private int _shaftPointCount;
    private bool _curveDirty = true;
    private bool _hasEndpointGeometry;

    public Vector2 StartPosition { get; private set; }
    public Vector2 EndPosition { get; private set; }
    public Vector2 StartTangent { get; private set; } = Vector2.Right;
    public Vector2 EndTangent { get; private set; } = Vector2.Right;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ConfigureTipNode(HeadTip, ref _headTipMaterial);
        ConfigureTipNode(TailTip, ref _tailTipMaterial);
        SetTipsVisible(false);
    }

    public void SetHeadHighlighted(bool highlighted)
    {
        float targetScale = highlighted ? HeadHoverScale : HeadDefaultScale;
        _headTipScaleTween?.Kill();
        _headTipScaleTween = null;

        if (!highlighted || !IsInsideTree())
        {
            SetHeadTipScale(targetScale);
            return;
        }

        _headTipScaleTween = CreateTween();
        _headTipScaleTween
            .TweenMethod(Callable.From<float>(SetHeadTipScale), _headTipScale, targetScale, 0.16f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    public void SetEndpoints(
        Vector2 startPosition,
        Vector2 endPosition,
        Vector2? startTangent = null,
        Vector2? endTangent = null
    )
    {
        Vector2 resolvedStartTangent = GetSafeDirection(
            startTangent ?? endPosition - startPosition,
            Vector2.Right
        );
        Vector2 resolvedEndTangent = GetSafeDirection(
            endTangent ?? endPosition - startPosition,
            resolvedStartTangent
        );
        if (
            _hasEndpointGeometry
            && StartPosition.DistanceSquaredTo(startPosition) < 0.01f
            && EndPosition.DistanceSquaredTo(endPosition) < 0.01f
            && StartTangent.DistanceSquaredTo(resolvedStartTangent) < 0.0001f
            && EndTangent.DistanceSquaredTo(resolvedEndTangent) < 0.0001f
        )
        {
            return;
        }

        StartPosition = startPosition;
        EndPosition = endPosition;
        StartTangent = resolvedStartTangent;
        EndTangent = resolvedEndTangent;
        _hasEndpointGeometry = true;
        _curveDirty = true;
        if (EndPosition.DistanceSquaredTo(StartPosition) < 9f)
        {
            ClearPointBuffers();
            SetTipsVisible(false);
            QueueRedraw();
            return;
        }

        EnsureCurveGeometry();
        UpdateTipNodes();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible)
            return;

        Vector2 delta = EndPosition - StartPosition;
        if (delta.LengthSquared() < 9f)
        {
            SetTipsVisible(false);
            return;
        }

        EnsureCurveGeometry();
        if (_shaftPointCount < 2)
        {
            SetTipsVisible(false);
            return;
        }

        DrawPolyline(_shaftPoints, ShadowColor, ShadowWidth, true);
        DrawPolyline(_shaftPoints, ArrowColor, ArrowWidth, true);
        UpdateTipNodes();
    }

    private void GetCurveControls(
        Vector2 start,
        Vector2 end,
        out Vector2 controlA,
        out Vector2 controlB
    )
    {
        float distance = start.DistanceTo(end);
        float handleLength = Mathf.Clamp(distance * 0.38f, 48f, 260f);
        Vector2 lift = new(0f, -Mathf.Min(CurveLift, distance * 0.28f));
        controlA = start + StartTangent * handleLength + lift * 0.35f;
        controlB = end - EndTangent * handleLength + lift * 0.35f;
    }

    private void EnsureCurveGeometry()
    {
        int expectedPointCount = Mathf.Max(4, PointCount);
        if (!_curveDirty && _lastPoints.Length == expectedPointCount)
            return;

        GetCurveControls(StartPosition, EndPosition, out Vector2 controlA, out Vector2 controlB);
        int pointCount = BuildCurvePoints(StartPosition, controlA, controlB, EndPosition);
        _shaftPointCount = BuildInsetShaftPoints(pointCount);
        _curveDirty = false;
    }

    private int BuildCurvePoints(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end)
    {
        int count = Mathf.Max(4, PointCount);
        EnsurePointBuffers(count);
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            _lastPoints[i] = CubicBezier(start, controlA, controlB, end, t);
        }

        return count;
    }

    private static Vector2 CubicBezier(
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        float t
    )
    {
        float inv = 1f - t;
        return a * (inv * inv * inv)
            + b * (3f * inv * inv * t)
            + c * (3f * inv * t * t)
            + d * (t * t * t);
    }

    private static Vector2 GetSafeDirection(Vector2 value, Vector2 fallback)
    {
        if (value.LengthSquared() >= 0.01f)
            return value.Normalized();

        if (fallback.LengthSquared() >= 0.01f)
            return fallback.Normalized();

        return Vector2.Right;
    }

    private int BuildInsetShaftPoints(int pointCount)
    {
        if (pointCount < 2)
            return 0;

        for (int i = 0; i < pointCount; i++)
            _shaftPoints[i] = _lastPoints[i];

        _shaftPoints[0] = MovePointAlongSegment(_shaftPoints[0], _shaftPoints[1], TailShaftInset);
        int last = pointCount - 1;
        _shaftPoints[last] = MovePointAlongSegment(
            _shaftPoints[last],
            _shaftPoints[last - 1],
            HeadShaftInset
        );
        return pointCount;
    }

    private void EnsurePointBuffers(int count)
    {
        if (_lastPoints.Length != count)
            _lastPoints = new Vector2[count];
        if (_shaftPoints.Length != count)
            _shaftPoints = new Vector2[count];
    }

    private void ClearPointBuffers()
    {
        _lastPoints = System.Array.Empty<Vector2>();
        _shaftPoints = System.Array.Empty<Vector2>();
        _shaftPointCount = 0;
        _curveDirty = true;
    }

    private static Vector2 MovePointAlongSegment(Vector2 point, Vector2 toward, float distance)
    {
        Vector2 direction = toward - point;
        if (direction.LengthSquared() < 0.01f || distance <= 0f)
            return point;

        return point + direction.Normalized() * distance;
    }

    private void ConfigureTipNode(ColorRect tip, ref ShaderMaterial material)
    {
        if (tip == null)
            return;

        tip.MouseFilter = MouseFilterEnum.Ignore;
        tip.PivotOffset = tip.Size * 0.5f;
        material = tip.Material as ShaderMaterial;
        if (material == null)
        {
            material = new ShaderMaterial { Shader = TipShader };
            tip.Material = material;
        }
        else
        {
            material = material.Duplicate() as ShaderMaterial;
            tip.Material = material;
        }

        ApplyTipShaderParameters(material);
    }

    private void UpdateTipNodes()
    {
        if (_lastPoints.Length < 2)
        {
            SetTipsVisible(false);
            return;
        }

        ApplyTipShaderParameters(_headTipMaterial);
        ApplyTipShaderParameters(_tailTipMaterial);
        PositionTip(HeadTip, _lastPoints[^2], _lastPoints[^1], HeadSize, _headTipScale);
        PositionTip(TailTip, _lastPoints[1], _lastPoints[0], TailSize, 1f);
    }

    private void PositionTip(
        ColorRect tip,
        Vector2 beforeEnd,
        Vector2 end,
        Vector2 size,
        float scaleMultiplier
    )
    {
        if (tip == null)
            return;

        Vector2 scaledSize = size * Mathf.Max(0.01f, scaleMultiplier);
        Vector2 direction = end - beforeEnd;
        if (direction.LengthSquared() < 0.01f || scaledSize.X <= 0f || scaledSize.Y <= 0f)
        {
            tip.Visible = false;
            return;
        }

        tip.Visible = Visible;
        tip.Size = scaledSize;
        tip.PivotOffset = scaledSize * 0.5f;
        direction = direction.Normalized();
        tip.Position = end - scaledSize * 0.5f - direction * scaledSize.X * 0.5f;
        tip.Rotation = direction.Angle();
    }

    private void SetHeadTipScale(float scale)
    {
        _headTipScale = scale;
        UpdateTipNodes();
        QueueRedraw();
    }

    private void SetTipsVisible(bool visible)
    {
        if (HeadTip != null)
            HeadTip.Visible = visible;
        if (TailTip != null)
            TailTip.Visible = visible;
    }

    private void ApplyTipShaderParameters(ShaderMaterial material)
    {
        if (material == null)
            return;

        material.SetShaderParameter("fill_color", ArrowColor);
        material.SetShaderParameter("outline_color", ShadowColor);
        material.SetShaderParameter("outline_width", TipOutlineWidth);
        material.SetShaderParameter("edge_softness", TipEdgeSoftness);
    }
}
