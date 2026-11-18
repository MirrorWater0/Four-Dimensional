using Godot;

public partial class NodeHoloTip : Control {
    private LevelNode _source;
    private ColorRect _glass;
    private RichTextLabel _record;
    private Label _index;
    private ColorRect _rule;
    private Control _window;
    private const float OpenDuration = 0.215f;
    private ShaderMaterial _glassMaterial;
    private Vector2 _anchor, _joint, _end;
    private float _time, _wire, _pageTime;
    private int _page, _pages;
    private string _text;
    private bool _left;
    private int _pageStride;

    public override void _Ready() {
        _glass = GetNode<ColorRect>("Glass");
        _record = GetNode<RichTextLabel>("Glass/Window/Record");
        _index = GetNode<Label>("Glass/Index");
        _rule = GetNode<ColorRect>("Glass/Rule");
        _window = GetNode<Control>("Glass/Window");
        _glassMaterial = (ShaderMaterial)_glass.Material;
        SetProcess(false);
    }

    public bool Tracks(LevelNode node) => Visible && _source == node;

    public void Present(LevelNode node, string text) {
        if (!Tracks(node)) _time = 0f;
        _source = node;
        int font = UserSettings.ScaleTextFontSize(18);
        _record.AddThemeFontSizeOverride("normal_font_size", font);
        _record.AddThemeFontSizeOverride("bold_font_size", font);
        _text = text;
        _page = 0;
        _pageTime = 0;
        Visible = true;
        SetProcess(true);
        LayoutPage();
        Animate(0f);
    }

    public void Dismiss(LevelNode node) {
        if (_source != node) return;
        _source = null;
        Hide();
        SetProcess(false);
    }

    // Keep the panel non-interactive. Overflow records page while the pointer stays on the node.
    private void LayoutPage() {
        Vector2 view = GetViewportRect().Size;
        float width = Mathf.Min(436f, Mathf.Max(1f, view.X - 28f));
        float maxHeight = Mathf.Max(88f, view.Y - 32f);
        _glass.Size = new Vector2(width, maxHeight);
        _record.Text = _text;
        int content = _record.GetContentHeight();
        _record.Size = new Vector2(_record.Size.X, content + 3f);
        _pageStride = Mathf.Max(1, Mathf.FloorToInt(maxHeight - 66f) - UserSettings.ScaleTextFontSize(24));
        _pages = content <= maxHeight - 66f ? 1 : 1 + Mathf.CeilToInt((content - (maxHeight - 66f)) / _pageStride);
        _page = Mathf.Min(_page, _pages - 1);
        _glass.Size = new Vector2(width, Mathf.Clamp(content + 66f, 88f, maxHeight));
        PositionRecord();
        UpdateIndex();
        _glassMaterial.SetShaderParameter("panel_size", _glass.Size);
    }

    private void UpdateIndex() {
        _index.Text = $"NODE ARCHIVE    /    {_source.SelfCoordinate.X + 1:00}.{_source.SelfCoordinate.Y + 1:00}" +
            (_pages > 1 ? $"       {_page + 1} / {_pages}" : "");
    }

    private void PositionRecord() {
        // Clip the full BBCode layout, with a line of overlap so a split line is never lost.
        _record.Position = new Vector2(0, -Mathf.Min(_page * _pageStride,
            Mathf.Max(0f, _record.Size.Y - (_glass.Size.Y - 63f))));
    }

    public override void _Process(double delta) {
        if (!GodotObject.IsInstanceValid(_source) || !_source.IsVisibleInTree()) {
            _source = null; Hide(); SetProcess(false); return;
        }
        if (_pages > 1 && (_pageTime += (float)delta) >= 5f) {
            _pageTime = 0; _page = (_page + 1) % _pages; PositionRecord(); UpdateIndex();
        }
        Animate((float)delta);
    }

    public override void _Notification(int what) {
        if (what == NotificationResized && Visible && GodotObject.IsInstanceValid(_source) && _record != null) LayoutPage();
    }

    private void Animate(float dt) {
        float previousTime = _time;
        _time = Mathf.Min(OpenDuration, _time + dt);
        Vector2 oldAnchor = _anchor, oldEnd = _end;
        bool wasLeft = _left;
        var toCanvas = GetGlobalTransformWithCanvas().AffineInverse();
        _anchor = toCanvas * (_source.GetGlobalTransformWithCanvas() * (_source.Size * 0.5f));
        Vector2 view = GetViewportRect().Size;
        Vector2 size = _glass.Size;
        _left = _anchor.X + 120f + size.X > view.X - 14f && _anchor.X > view.X * 0.5f;
        float sign = _left ? -1f : 1f;
        float x = Mathf.Clamp(_left ? _anchor.X - 120f - size.X : _anchor.X + 120f, 14f, Mathf.Max(14f, view.X - size.X - 14f));
        bool above = _anchor.Y >= size.Y + 48f;
        float y = above ? _anchor.Y - size.Y - 26f : _anchor.Y - 96f;
        float portY = above ? size.Y - 24f : 25f;
        _glass.Position = new Vector2(x, Mathf.Clamp(y, 14f, Mathf.Max(14f, view.Y - size.Y - 14f)));
        _end = _glass.Position + new Vector2(_left ? size.X : 0, portY);
        _joint = _end - new Vector2(sign * 32f, 0);
        bool moved = !oldAnchor.IsEqualApprox(_anchor) || !oldEnd.IsEqualApprox(_end);
        if (previousTime < OpenDuration || dt == 0f || wasLeft != _left || moved) {
            // The pane itself stays at its final size. Only its glass silhouette folds;
            // the readable layer enters at native resolution once the leaves are open.
            _wire = 1f - Mathf.Pow(1f - Mathf.Clamp(_time / 0.052f, 0f, 1f), 2f);
            _glassMaterial.SetShaderParameter("deploy", _time / OpenDuration);
            _glassMaterial.SetShaderParameter("port_uv", new Vector2(_left ? 1f : 0f, portY / size.Y));
            float heading = Mathf.SmoothStep(0.132f, 0.175f, _time);
            float copy = Mathf.SmoothStep(0.143f, 0.197f, _time);
            _index.Modulate = Colors.White with { A = heading };
            _rule.Modulate = Colors.White with { A = heading };
            _window.Modulate = Colors.White with { A = copy };
            // Move the type inside its existing clip, leaving overflow pagination intact.
            _record.Position = new Vector2(sign * (1f - copy) * 9f, _record.Position.Y);
            QueueRedraw();
        }
    }

    public override void _Draw() {
        if (!Visible || _wire <= 0f) return;
        Color light = new(0.70f, 0.81f, 0.85f, 0.86f);
        Vector2 from = _anchor + _anchor.DirectionTo(_joint) * 9f;
        float first = from.DistanceTo(_joint);
        float length = (first + _joint.DistanceTo(_end)) * _wire;
        DrawLine(from, from.MoveToward(_joint, Mathf.Min(length, first)), light, 1.5f, true);
        if (length > first) DrawLine(_joint, _joint.MoveToward(_end, length - first), light, 1.5f, true);
        DrawCircle(from, 2.5f, light);
        if (_time >= OpenDuration) return;
        // A single travelling contact, then two opposed brackets snap onto the port.
        Vector2 tip = length < first ? from.MoveToward(_joint, length) : _joint.MoveToward(_end, length - first);
        DrawCircle(tip, 2.8f, new Color(0.87f, 0.95f, 0.98f, 1f - Mathf.SmoothStep(0.05f, 0.12f, _time)));
        float lockIn = Mathf.SmoothStep(0.035f, 0.105f, _time);
        float ink = lockIn * (1f - Mathf.SmoothStep(0.12f, OpenDuration, _time));
        if (ink > 0f) {
            float radius = Mathf.Lerp(10f, 3f, lockIn);
            for (int side = -1; side <= 1; side += 2) {
                Vector2 point = _end + new Vector2(0, radius * side);
                Color edge = new(0.80f, 0.91f, 0.96f, ink);
                DrawLine(point + new Vector2(-4f, 3f * side), point, edge, 1.4f, true);
                DrawLine(point, point + new Vector2(4f, 3f * side), edge, 1.4f, true);
            }
        }
    }
}
