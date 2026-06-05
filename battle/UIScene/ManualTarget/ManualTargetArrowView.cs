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

    private ColorRect HeadTip => field ??= GetNodeOrNull<ColorRect>("HeadTip");
    private ColorRect TailTip => field ??= GetNodeOrNull<ColorRect>("TailTip");
    private ShaderMaterial _headTipMaterial;
    private ShaderMaterial _tailTipMaterial;
    private Vector2[] _lastPoints = System.Array.Empty<Vector2>();

    public Vector2 StartPosition { get; private set; }
    public Vector2 EndPosition { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ConfigureTipNode(HeadTip, ref _headTipMaterial);
        ConfigureTipNode(TailTip, ref _tailTipMaterial);
        SetTipsVisible(false);
    }

    public void SetEndpoints(
        Vector2 startPosition,
        Vector2 endPosition,
        Vector2? startTangent = null,
        Vector2? endTangent = null
    )
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        if (EndPosition.DistanceSquaredTo(StartPosition) < 9f)
        {
            _lastPoints = System.Array.Empty<Vector2>();
            SetTipsVisible(false);
        }
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

        Vector2 control = (StartPosition + EndPosition) * 0.5f + new Vector2(0f, -CurveLift);
        _lastPoints = BuildCurvePoints(StartPosition, control, EndPosition);
        Vector2[] shaftPoints = BuildInsetShaftPoints(_lastPoints);
        if (shaftPoints.Length < 2)
        {
            SetTipsVisible(false);
            return;
        }

        DrawPolyline(shaftPoints, ShadowColor, ShadowWidth, true);
        DrawPolyline(shaftPoints, ArrowColor, ArrowWidth, true);
        UpdateTipNodes();
    }

    private Vector2[] BuildCurvePoints(Vector2 start, Vector2 control, Vector2 end)
    {
        int count = Mathf.Max(4, PointCount);
        Vector2[] points = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            points[i] = start.Lerp(control, t).Lerp(control.Lerp(end, t), t);
        }

        return points;
    }

    private Vector2[] BuildInsetShaftPoints(Vector2[] points)
    {
        if (points == null || points.Length < 2)
            return System.Array.Empty<Vector2>();

        Vector2[] result = (Vector2[])points.Clone();
        result[0] = MovePointAlongSegment(result[0], result[1], TailShaftInset);
        result[^1] = MovePointAlongSegment(result[^1], result[^2], HeadShaftInset);
        return result;
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
        PositionTip(HeadTip, _lastPoints[^2], _lastPoints[^1], HeadSize);
        PositionTip(TailTip, _lastPoints[1], _lastPoints[0], TailSize);
    }

    private void PositionTip(ColorRect tip, Vector2 beforeEnd, Vector2 end, Vector2 size)
    {
        if (tip == null)
            return;

        Vector2 direction = end - beforeEnd;
        if (direction.LengthSquared() < 0.01f || size.X <= 0f || size.Y <= 0f)
        {
            tip.Visible = false;
            return;
        }

        tip.Visible = Visible;
        tip.Size = size;
        tip.PivotOffset = size * 0.5f;
        direction = direction.Normalized();
        tip.Position = end - size * 0.5f - direction * size.X * 0.5f;
        tip.Rotation = direction.Angle();
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
