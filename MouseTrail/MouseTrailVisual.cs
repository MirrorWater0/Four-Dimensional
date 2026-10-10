using System;
using Godot;

public partial class MouseTrailVisual : Node2D {
    [Export(PropertyHint.Range, "0.08,0.5,0.01")]
    public float TrailLifetime = 0.22f;

    [Export(PropertyHint.Range, "40,260,5")]
    public float MaxTrailLength = 150f;

    [Export(PropertyHint.Range, "1,8,0.5")]
    public float PointSpacing = 4f;

    [Export]
    public Color TrailColor = new(0.58f, 0.82f, 0.94f);

    [Export]
    public Color HighlightColor = new(0.93f, 0.97f, 1f);

    [Export]
    public Color ClickColor = new(0.93f, 0.82f, 0.59f);

    private const int Capacity = 96;
    private const int RenderSamples = 64;
    private const float ClickLifetime = 0.34f;
    private readonly Vector2[] _points = new Vector2[Capacity];
    private readonly double[] _birthTimes = new double[Capacity];
    private readonly int[] _pointIds = new int[Capacity];
    private readonly Vector2[] _renderPoints = new Vector2[RenderSamples];
    private readonly float[] _renderStrength = new float[RenderSamples];
    private readonly Color[] _renderColors = new Color[RenderSamples];
    private readonly Vector2[] _clickPositions = new Vector2[3];
    private readonly float[] _clickAges = new float[3];
    private int _pointCount;
    private int _nextPointId;
    private int _clickSlot;
    private bool _tracking;
    private Vector2 _previousPosition;
    private double _clock;
    private float _widthResponse;

    public override void _Ready() {
        Array.Fill(_clickAges, ClickLifetime);
    }

    public void ResetTrail() {
        bool hadVisual = _pointCount > 0 || HasClicks();
        _pointCount = 0;
        _tracking = false;
        _widthResponse = 0f;
        Array.Fill(_clickAges, ClickLifetime);
        if (hadVisual)
            QueueRedraw();
    }

    public void EmitClick(Vector2 position) {
        _clickPositions[_clickSlot] = position;
        _clickAges[_clickSlot] = 0f;
        _clickSlot = (_clickSlot + 1) % _clickAges.Length;
        QueueRedraw();
    }

    public void Advance(float delta, Vector2 position) {
        if (delta <= 0f)
            return;

        bool hadVisual = _pointCount > 0 || HasClicks();
        double previousTime = _clock;
        _clock += delta;
        for (int i = 0; i < _clickAges.Length; i++)
            _clickAges[i] = Math.Min(ClickLifetime, _clickAges[i] + delta);

        float distance = _previousPosition.DistanceTo(position);
        // Refocus, cursor warps and long stalls must not draw across the whole screen.
        if (!_tracking || distance > Math.Max(360f, MaxTrailLength * 2.5f) || delta > 0.15f) {
            _pointCount = 0;
            _tracking = true;
            _previousPosition = position;
            distance = 0f;
        }

        while (_pointCount > 0 && _clock - _birthTimes[0] >= TrailLifetime)
            RemoveOldest();

        float speed = distance / delta;
        _widthResponse = Mathf.Lerp(_widthResponse, Mathf.Clamp(speed / 1400f, 0f, 1f),
            1f - Mathf.Exp(-14f * delta));

        if (distance >= 0.5f) {
            if (_pointCount == 0)
                AddPoint(_previousPosition, previousTime);
            int steps = Math.Clamp((int)Math.Ceiling(distance / Math.Max(1f, PointSpacing)), 1, 64);
            for (int i = 1; i <= steps; i++) {
                float fraction = (float)i / steps;
                AddPoint(_previousPosition.Lerp(position, fraction), previousTime + delta * fraction);
            }
            TrimLength();
        }

        _previousPosition = position;
        if (hadVisual || _pointCount > 0 || HasClicks())
            QueueRedraw();
    }

    private void AddPoint(Vector2 point, double birthTime) {
        if (_pointCount == Capacity)
            RemoveOldest();
        _points[_pointCount] = point;
        _pointIds[_pointCount] = _nextPointId++;
        _birthTimes[_pointCount++] = birthTime;
    }

    private void RemoveOldest() {
        _pointCount--;
        Array.Copy(_points, 1, _points, 0, _pointCount);
        Array.Copy(_birthTimes, 1, _birthTimes, 0, _pointCount);
        Array.Copy(_pointIds, 1, _pointIds, 0, _pointCount);
    }

    private void TrimLength() {
        float length = 0f;
        for (int i = _pointCount - 1; i > 0; i--) {
            float segmentLength = _points[i].DistanceTo(_points[i - 1]);
            if (length + segmentLength > MaxTrailLength) {
                float keep = (MaxTrailLength - length) / Math.Max(segmentLength, 0.001f);
                _points[i - 1] = _points[i].Lerp(_points[i - 1], keep);
                _birthTimes[i - 1] = _birthTimes[i] + (_birthTimes[i - 1] - _birthTimes[i]) * keep;
                for (int removed = i - 1; removed > 0; removed--)
                    RemoveOldest();
                break;
            }
            length += segmentLength;
        }
    }

    private bool HasClicks() {
        foreach (float age in _clickAges)
            if (age < ClickLifetime)
                return true;
        return false;
    }

    public override void _Draw() {
        if (_pointCount > 1) {
            BuildRenderPath();
            DrawTrailLayer(5.5f + _widthResponse, 0.045f);
            DrawTrailLayer(2.6f + _widthResponse * 0.5f, 0.16f);
            DrawTrailLayer(0.8f + _widthResponse * 0.65f, 0.72f);
            // Stable IDs keep glints on the same part of the path as old points expire.
            for (int i = 1; i < _pointCount - 1; i++) {
                float life = Mathf.Clamp(1f - (float)(_clock - _birthTimes[i]) / Math.Max(TrailLifetime, 0.01f), 0f, 1f);
                if (_pointIds[i] % 17 == 4 && life > 0.15f && life < 0.85f)
                    DrawGlint(_points[i], 1.1f + life * 0.6f, Alpha(HighlightColor, life * life * 0.45f));
            }
        }

        for (int i = 0; i < _clickAges.Length; i++) {
            if (_clickAges[i] >= ClickLifetime)
                continue;
            float progress = _clickAges[i] / ClickLifetime;
            float fade = (1f - progress) * (1f - progress);
            float radius = 5f + 18f * (1f - Mathf.Pow(1f - progress, 3f));
            Color tint = Alpha(TrailColor.Lerp(ClickColor, 0.65f), fade * 0.5f);
            for (int quadrant = 0; quadrant < 4; quadrant++) {
                float angle = quadrant * Mathf.Pi * 0.5f + 0.18f;
                DrawArc(_clickPositions[i], radius, angle, angle + 0.95f, 10, tint, 0.8f, true);
            }
            DrawGlint(_clickPositions[i] + new Vector2(radius, 0f), 1.8f * (1f - progress),
                Alpha(ClickColor, fade * 0.7f));
        }
    }

    private void BuildRenderPath() {
        float totalLength = 0f;
        for (int i = 1; i < _pointCount; i++)
            totalLength += _points[i - 1].DistanceTo(_points[i]);

        int segment = 1;
        float segmentStart = 0f;
        float segmentLength = _points[0].DistanceTo(_points[1]);
        for (int i = 0; i < RenderSamples; i++) {
            float fraction = (float)i / (RenderSamples - 1);
            float distance = totalLength * fraction;
            while (segment < _pointCount - 1 && segmentStart + segmentLength < distance) {
                segmentStart += segmentLength;
                segment++;
                segmentLength = _points[segment - 1].DistanceTo(_points[segment]);
            }
            float blend = Mathf.Clamp((distance - segmentStart) / Math.Max(segmentLength, 0.001f), 0f, 1f);
            _renderPoints[i] = _points[segment - 1].Lerp(_points[segment], blend);
            double birth = _birthTimes[segment - 1] + (_birthTimes[segment] - _birthTimes[segment - 1]) * blend;
            float life = Mathf.Clamp(1f - (float)(_clock - birth) / Math.Max(TrailLifetime, 0.01f), 0f, 1f);
            _renderStrength[i] = life * life * Mathf.Sqrt(fraction);
        }
    }

    private void DrawTrailLayer(float width, float opacity) {
        for (int i = 0; i < RenderSamples; i++) {
            Color tint = TrailColor.Lerp(HighlightColor, (float)i / (RenderSamples - 1) * 0.65f);
            _renderColors[i] = Alpha(tint, _renderStrength[i] * opacity);
        }
        // One continuous strip avoids bright seams between individual line segments.
        DrawPolylineColors(_renderPoints, _renderColors, width, true);
    }

    private void DrawGlint(Vector2 position, float radius, Color color) {
        DrawLine(position - Vector2.Right * radius, position + Vector2.Right * radius, color, 0.7f, true);
        DrawLine(position - Vector2.Down * radius, position + Vector2.Down * radius, color, 0.7f, true);
    }

    private static Color Alpha(Color color, float alpha) => new(color.R, color.G, color.B, color.A * alpha);
}
