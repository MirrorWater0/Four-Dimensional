using Godot;

[Tool]
public partial class WorldLineTrace : Control {
    private const int Samples = 96;
    private int _worldLine;
    [Export(PropertyHint.Range, "0,4,1")]
    public int WorldLine {
        get => _worldLine;
        set {
            _worldLine = value;
            QueueRedraw();
        }
    }
    [Export]
    public bool Compact { get; set; }

    private readonly Vector2[] _source = new Vector2[Samples + 1];
    private readonly Vector2[] _points = new Vector2[Samples + 1];
    private readonly Vector2[] _reference = new Vector2[Samples + 1];
    private int _comparisonLine = -1;
    private float _blend = 1f;
    private float _phase;
    private bool _initialized;

    public override void _Ready() {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
        SetProcess(!Engine.IsEditorHint() && !Compact);
    }

    public void ShowWorldLine(int worldLine, int currentLine, bool animate) {
        for (int sample = 0; sample <= Samples; sample++)
            _source[sample] = GetDisplayedPoint(sample);
        WorldLine = worldLine;
        _comparisonLine = currentLine == worldLine ? -1 : currentLine;
        _blend = animate && _initialized ? 0f : 1f;
        _initialized = true;
        QueueRedraw();
    }

    public override void _Process(double delta) {
        if (!IsVisibleInTree())
            return;
        _phase = Mathf.PosMod(_phase + (float)delta * 0.16f, 1f);
        _blend = Mathf.MoveToward(_blend, 1f, (float)delta * 3f);
        QueueRedraw();
    }

    private Vector2 GetDisplayedPoint(int sample) {
        Vector2 target = WorldLineVisuals.GetPoint(sample / (float)Samples, WorldLine);
        float weight = Mathf.SmoothStep(0f, 1f, _blend);
        return _initialized ? _source[sample].Lerp(target, weight) : target;
    }

    private Vector2 ToCanvas(Vector2 point) {
        Vector2 padding = Compact ? new Vector2(4f, 4f) : new Vector2(9f, 8f);
        return padding + point * new Vector2(Mathf.Max(1f, Size.X - padding.X * 2f),
            Mathf.Max(1f, Size.Y - padding.Y * 2f));
    }

    public override void _Draw() {
        if (Size.X < 20f || Size.Y < 18f)
            return;
        Color color = WorldLineVisuals.GetColor(WorldLine);
        if (!Compact) {
            Color grid = new(0.65f, 0.8f, 0.9f, 0.07f);
            for (int column = 0; column <= 8; column++) {
                float x = 9f + (Size.X - 18f) * column / 8f;
                DrawLine(new Vector2(x, 8f), new Vector2(x, Size.Y - 8f), grid);
            }
            DrawLine(new Vector2(9f, Size.Y * 0.5f), new Vector2(Size.X - 9f, Size.Y * 0.5f), grid);
        }

        if (_comparisonLine >= 0) {
            for (int i = 0; i <= Samples; i++)
                _reference[i] = ToCanvas(WorldLineVisuals.GetPoint(i / (float)Samples, _comparisonLine));
            DrawPolyline(_reference, new Color(WorldLineVisuals.GetColor(_comparisonLine), 0.2f), 1f, true);
        }

        for (int i = 0; i <= Samples; i++)
            _points[i] = ToCanvas(GetDisplayedPoint(i));
        if (!Compact)
            DrawPolyline(_points, new Color(color, 0.08f), 7f, true);
        DrawPolyline(_points, new Color(color, Compact ? 0.8f : 0.95f), Compact ? 1.3f : 1.8f, true);
        DrawEndpoint(_points[0], color);
        DrawEndpoint(_points[Samples], color);
        if (!Compact && !Engine.IsEditorHint()) {
            Vector2 cursor = _points[Mathf.Clamp((int)(_phase * Samples), 0, Samples)];
            DrawCircle(cursor, 6f, new Color(color, 0.09f));
            DrawCircle(cursor, 2.4f, color);
        }
    }

    private void DrawEndpoint(Vector2 point, Color color) {
        float radius = Compact ? 2.3f : 3.5f;
        DrawCircle(point, radius, new Color(0.035f, 0.06f, 0.08f));
        DrawArc(point, radius, 0f, Mathf.Tau, 20, color, 1f, true);
        DrawCircle(point, 1f, color);
    }
}
