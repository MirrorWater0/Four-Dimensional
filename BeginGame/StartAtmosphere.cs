using System;
using Godot;

// A 4D hypercube projected through 3D into the title screen. Reuse all drawing buffers.
public partial class StartAtmosphere : Control {
    private readonly Vector2[] _points = new Vector2[16];
    private readonly Vector3[] _vertices = new Vector3[16];
    private readonly float[] _depths = new float[16];
    private readonly Vector2[] _face = new Vector2[4];
    private readonly Vector2[] _streamQuad = new Vector2[4];
    private readonly Vector2[] _streamUvs = new Vector2[4];
    private readonly Color[] _streamColors = new Color[1];
    private ImageTexture _streamTexture;
    private readonly Color[] _faceColors = new Color[4];
    private readonly int[,] _faces = new int[24, 4];
    private readonly int[] _faceOrder = new int[24];
    private readonly Vector2[] _ring = new Vector2[129];
    private readonly (int A, int B)[] _edges = new (int, int)[32];
    private readonly int[] _edgeOrder = new int[32];
    private readonly Comparison<int> _depthComparison;
    private readonly Comparison<int> _faceDepthComparison;
    private float _time;

    public StartAtmosphere() {
        int edge = 0;
        for (int a = 0; a < 16; a++) {
            for (int axis = 0; axis < 4; axis++) {
                int b = a ^ (1 << axis);
                if (a >= b) continue;
                _edges[edge] = (a, b);
                _edgeOrder[edge] = edge;
                edge++;
            }
        }
        _depthComparison = (a, b) => EdgeDepth(b).CompareTo(EdgeDepth(a));
        int face = 0;
        for (int axisA = 0; axisA < 4; axisA++) {
            for (int axisB = axisA + 1; axisB < 4; axisB++) {
                int bitA = 1 << axisA, bitB = 1 << axisB;
                for (int origin = 0; origin < 16; origin++) {
                    if ((origin & (bitA | bitB)) != 0) continue;
                    _faces[face, 0] = origin;
                    _faces[face, 1] = origin | bitA;
                    _faces[face, 2] = origin | bitA | bitB;
                    _faces[face, 3] = origin | bitB;
                    _faceOrder[face] = face;
                    face++;
                }
            }
        }
        _faceDepthComparison = (a, b) => FaceDepth(b).CompareTo(FaceDepth(a));
    }
    public override void _Ready() {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Linear;
        _streamTexture = CreateStreamTexture();
    }
    public override void _Process(double delta) { _time += (float)delta; QueueRedraw(); }
    private float EdgeDepth(int index) {
        var edge = _edges[index];
        return (_depths[edge.A] + _depths[edge.B]) * 0.5f;
    }
    private float FaceDepth(int index) {
        return (_depths[_faces[index, 0]] + _depths[_faces[index, 1]]
            + _depths[_faces[index, 2]] + _depths[_faces[index, 3]]) * 0.25f;
    }
    private static void RotatePlane(ref float a, ref float b, float angle) {
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        (a, b) = (a * c - b * s, a * s + b * c);
    }
    public override void _Draw() {
        float scale = Size.Y / 1080f * 0.84f;
        Vector2 center = Size * new Vector2(0.718f, 0.445f);
        center.Y += Mathf.Sin(_time * 0.33f) * 8f * scale;
        for (int i = 0; i < 16; i++) {
            float x = (i & 1) == 0 ? -1f : 1f, y = (i & 2) == 0 ? -1f : 1f;
            float z = (i & 4) == 0 ? -1f : 1f, w = (i & 8) == 0 ? -1f : 1f;
            RotatePlane(ref x, ref w, 0.4f + _time * 0.095f);
            RotatePlane(ref z, ref w, 0.22f + _time * 0.037f);
            RotatePlane(ref y, ref z, 0.57f);
            RotatePlane(ref x, ref z, 0.4f + _time * 0.027f);
            float p4 = 2.8f / (3.3f - w);
            x *= p4; y *= p4; z *= p4;
            _vertices[i] = new Vector3(x, y, z);
            _points[i] = center + new Vector2(x, y) * (5f / (5f + z)) * 158f * scale;
            _depths[i] = z;
        }
        DrawProjectionRing(center, scale);
        DrawCrystalFaces(center, scale);
        Array.Sort(_edgeOrder, _depthComparison);
        foreach (int index in _edgeOrder) {
            var edge = _edges[index];
            Vector2 a = _points[edge.A], b = _points[edge.B];
            float front = Mathf.Clamp((1.8f - EdgeDepth(index)) / 3.6f, 0.15f, 1f);
            Color edgeTint = index % 5 == 0 ? new Color(0.94f, 0.74f, 0.82f)
                : index % 3 == 0 ? new Color(0.66f, 0.95f, 0.83f) : new Color(0.75f, 0.89f, 0.97f);
            DrawLine(a, b, new Color(edgeTint, front * 0.06f), 7f * scale, true);
            DrawLine(a, b, new Color(edgeTint, front * 0.18f), 2.8f * scale, true);
            DrawLine(a, b, new Color(edgeTint.Lerp(Colors.White, 0.45f), front * 0.9f), 1.05f * scale, true);
            Vector2 dispersion = (b - a).Orthogonal().Normalized() * 1.6f * scale;
            DrawLine(a + dispersion, b + dispersion, new Color(edgeTint, front * 0.17f), 0.8f * scale, true);
            if (index % 4 == 0) {
                float phase = Mathf.PosMod(_time * 0.12f + index * 0.173f, 1f);
                DrawEdgeStream(a, b, phase, front, scale);
            }
        }
        for (int i = 0; i < 16; i++) {
            float alpha = Mathf.Clamp((1.8f - _depths[i]) / 3.6f, 0.15f, 1f);
            DrawCircle(_points[i], 4.5f * scale, new Color(0.47f, 0.76f, 0.94f, alpha * 0.10f));
            DrawCircle(_points[i], 1.6f * scale, new Color(0.87f, 0.94f, 1f, alpha), antialiased: true);
        }
    }
    private static ImageTexture CreateStreamTexture() {
        // One continuous lance: a fading narrow tail, a bright shoulder, and a zero-width tip.
        // Bake the smooth coverage once so moving highlights have no overlapping line caps.
        const int width = 512, height = 64;
        using Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (int x = 0; x < width; x++) {
            float t = x / (width - 1f);
            float profile = t < 0.72f ? Mathf.Pow(t / 0.72f, 1.25f) : (1f - t) / 0.28f;
            float longitudinal = Mathf.SmoothStep(0f, 0.72f, t) * (1f - Mathf.SmoothStep(0.97f, 1f, t));
            for (int y = 0; y < height; y++) {
                float across = Mathf.Abs(y / (height - 1f) * 2f - 1f);
                float core = 1f - Mathf.SmoothStep(profile * 0.36f, profile * 0.49f, across);
                float halo = 1f - Mathf.SmoothStep(profile * 0.43f, Mathf.Max(0.001f, profile), across);
                float alpha = (core * 0.91f + halo * 0.09f) * longitudinal;
                Color tint = new Color(0.72f, 0.85f, 1f).Lerp(new Color(0.97f, 0.99f, 1f), core);
                image.SetPixel(x, y, new Color(tint, profile > 0f ? alpha : 0f));
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    private void DrawEdgeStream(Vector2 a, Vector2 b, float phase, float front, float scale) {
        float length = a.DistanceTo(b);
        if (length < 2f * scale || _streamTexture == null) return;
        Vector2 normal = ((b - a) / length).Orthogonal();
        const float trailLength = 0.42f;
        // Clip UVs with the geometry, so a tail leaving an edge never restarts across it.
        float head = phase * (1f + trailLength);
        float tail = head - trailLength;
        float start = Mathf.Max(tail, 0f), end = Mathf.Min(head, 1f);
        if ((end - start) * length < 0.01f) return;
        Vector2 from = a.Lerp(b, start), to = a.Lerp(b, end);
        Vector2 halfWidth = normal * (6.5f * scale);
        _streamQuad[0] = from - halfWidth;
        _streamQuad[1] = to - halfWidth;
        _streamQuad[2] = to + halfWidth;
        _streamQuad[3] = from + halfWidth;
        float u0 = (start - tail) / trailLength, u1 = (end - tail) / trailLength;
        _streamUvs[0] = new Vector2(u0, 0f);
        _streamUvs[1] = new Vector2(u1, 0f);
        _streamUvs[2] = new Vector2(u1, 1f);
        _streamUvs[3] = new Vector2(u0, 1f);
        _streamColors[0] = new Color(1f, 1f, 1f, 0.58f + front * 0.42f);
        DrawPrimitive(_streamQuad, _streamColors, _streamUvs, _streamTexture);
    }
    private void DrawCrystalFaces(Vector2 center, float scale) {
        // Sort translucent faces back to front; grazing angles catch more light.
        Array.Sort(_faceOrder, _faceDepthComparison);
        Vector3 light = new Vector3(-0.45f, -0.65f, -1f).Normalized();
        foreach (int index in _faceOrder) {
            Vector3 a = _vertices[_faces[index, 0]];
            Vector3 normal = (_vertices[_faces[index, 1]] - a).Cross(_vertices[_faces[index, 3]] - a);
            if (normal.LengthSquared() < 0.00001f) continue;
            normal = normal.Normalized();
            float grazing = Mathf.Pow(1f - Mathf.Abs(normal.Z), 2f);
            float reflection = Mathf.Pow(Mathf.Abs(normal.Dot(light)), 12f);
            Color pearl = index % 5 == 0 ? new Color(0.90f, 0.69f, 0.79f)
                : index % 3 == 0 ? new Color(0.61f, 0.87f, 0.75f) : new Color(0.64f, 0.82f, 0.91f);
            for (int corner = 0; corner < 4; corner++) {
                int vertex = _faces[index, corner];
                _face[corner] = _points[vertex];
                float height = Mathf.Clamp((_points[vertex].Y - center.Y) / (560f * scale) + 0.5f, 0f, 1f);
                float sheen = Mathf.Pow(Mathf.Max(0f, 1f - height), 2f);
                Color tint = pearl.Lerp(new Color(0.98f, 0.97f, 0.92f), sheen * 0.65f + reflection * 0.3f);
                _faceColors[corner] = new Color(tint, 0.018f + grazing * 0.055f + reflection * 0.12f + sheen * 0.035f);
            }
            float area = (_face[1] - _face[0]).Cross(_face[2] - _face[0])
                + (_face[2] - _face[0]).Cross(_face[3] - _face[0]);
            if (Mathf.Abs(area) > 0.5f * scale * scale)
                DrawPolygon(_face, _faceColors);
        }
    }
    private void DrawProjectionRing(Vector2 center, float scale) {
        Vector2 floor = new(center.X, Size.Y * 0.738f);
        for (int i = 0; i < _ring.Length; i++) {
            float angle = i * Mathf.Tau / (_ring.Length - 1);
            _ring[i] = floor + new Vector2(Mathf.Cos(angle) * 325f, Mathf.Sin(angle) * 52f) * scale;
        }
        DrawPolyline(_ring, new Color(0.42f, 0.63f, 0.75f, 0.19f), 1f * scale, true);
        for (int i = 0; i < 60; i++) {
            float angle = i * Mathf.Tau / 60f;
            Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle) * 0.16f);
            float brightness = 0.12f + Mathf.Max(0f, Mathf.Cos(angle - _time * 0.22f)) * 0.30f;
            DrawLine(floor + direction * 333f * scale, floor + direction * (i % 5 == 0 ? 347f : 339f) * scale,
                new Color(0.58f, 0.76f, 0.87f, brightness), 1f * scale, true);
        }
    }
}
