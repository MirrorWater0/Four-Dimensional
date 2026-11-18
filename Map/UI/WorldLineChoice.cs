using Godot;

[Tool]
public partial class WorldLineChoice : Button {
    [Export(PropertyHint.Range, "0,4,1")]
    public int WorldLine { get; set; }

    private Control _content;
    private ColorRect _edge;
    private Tween _hoverTween;
    private bool _selected;

    public override void _Ready() {
        _content = GetNode<Control>("Content");
        _edge = GetNode<ColorRect>("Edge");
        Color color = WorldLineVisuals.GetColor(WorldLine);
        GetNode<Label>("Content/List/Caption/Number").Text = $"{WorldLine + 1:00}";
        GetNode<Label>("Content/List/Caption/Name").Text = GameInfo.GetWorldLineName(WorldLine).TrimEnd('线');
        GetNode<WorldLineTrace>("Content/List/Trace").WorldLine = WorldLine;
        GetNode<Label>("Active").AddThemeColorOverride("font_color", color);
        _edge.Color = color;
        _edge.Modulate = new Color(1f, 1f, 1f, ButtonPressed ? 1f : 0f);
        var pressed = (StyleBoxFlat)GetThemeStylebox("pressed").Duplicate();
        pressed.BorderColor = new Color(color, 0.75f);
        pressed.BgColor = new Color(color, 0.045f);
        AddThemeStyleboxOverride("pressed", pressed);
        AddThemeStyleboxOverride("hover_pressed", pressed);
        if (Engine.IsEditorHint())
            return;
        MouseEntered += () => AnimateHover(true);
        MouseExited += () => AnimateHover(false);
    }

    public void SetSelection(bool selected, bool current) {
        _selected = selected;
        SetPressedNoSignal(selected);
        GetNode<Label>("Active").Visible = current;
        _edge.Modulate = new Color(1f, 1f, 1f, selected ? 1f : 0f);
        AnimateHover(IsHovered());
    }

    private void AnimateHover(bool hovered) {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(_content, "modulate",
            new Color(1f, 1f, 1f, _selected || hovered ? 1f : 0.65f), 0.16f);
    }
}
