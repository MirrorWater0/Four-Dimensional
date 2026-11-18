using Godot;

// A child scene defines the underline in the editor without
// replacing the parent button's script or gameplay signals.
public partial class ActionButtonFeedback : Control {
    private Button _button;
    private ColorRect _underline;
    private bool _hovered;
    private bool _pressed;
    private float _hoverAmount;
    private float _pressAmount;

    public override void _Ready() {
        _button = GetParent() as Button;
        _underline = GetNode<ColorRect>("Underline");
        if (_button == null) {
            SetProcess(false);
            return;
        }
        _button.MouseEntered += OnMouseEntered;
        _button.MouseExited += OnMouseExited;
        _button.ButtonDown += OnButtonDown;
        _button.ButtonUp += OnButtonUp;
        UpdateFeedback(0f);
    }

    public override void _Process(double delta) => UpdateFeedback((float)delta);

    private void UpdateFeedback(float delta) {
        bool enabled = !_button.Disabled && _button.IsVisibleInTree();
        if (!enabled) {
            _hovered = false;
            _pressed = false;
        }
        float target = enabled && (_hovered || _button.HasFocus()) ? 1f : 0f;
        _hoverAmount = Mathf.Lerp(_hoverAmount, target, Mathf.Min(1f, delta * 12f));
        _pressAmount = Mathf.Lerp(_pressAmount, enabled && _pressed ? 1f : 0f,
            Mathf.Min(1f, delta * 24f));
        Color active = _button.GetThemeColor("font_hover_color");
        _underline.Scale = new Vector2(_hoverAmount, 1f);
        _underline.OffsetTop = -2f - _pressAmount;
        _underline.Color = active with { A = enabled ? _hoverAmount * 0.78f : 0f };
    }

    public override void _ExitTree() {
        if (!GodotObject.IsInstanceValid(_button))
            return;
        _button.MouseEntered -= OnMouseEntered;
        _button.MouseExited -= OnMouseExited;
        _button.ButtonDown -= OnButtonDown;
        _button.ButtonUp -= OnButtonUp;
    }

    private void OnMouseEntered() => _hovered = true;
    private void OnMouseExited() { _hovered = false; _pressed = false; }
    private void OnButtonDown() => _pressed = true;
    private void OnButtonUp() => _pressed = false;

    // Event card selection creates its action buttons dynamically.
    public static void ApplyTo(Button button) {
        button.Theme = GD.Load<Theme>("res://UI/ActionButtonTheme.tres");
        button.ThemeTypeVariation = "";
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" })
            button.RemoveThemeStyleboxOverride(state);
        button.Alignment = HorizontalAlignment.Left;
        button.ClipText = true;
        button.MouseDefaultCursorShape = CursorShape.PointingHand;
        if (!button.HasNode("ActionFeedback"))
            button.AddChild(GD.Load<PackedScene>("res://UI/ActionButtonFeedback.tscn").Instantiate());
    }
}
