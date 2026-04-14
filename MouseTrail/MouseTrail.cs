using Godot;

public partial class MouseTrail : CanvasLayer
{
    [Export]
    private Vector2 _cursorHotspot = Vector2.Zero;
    private const float PressLerpSpeed = 14.0f;
    private const float BurstDecaySpeed = 2.8f;
    private const float MotionResponse = 0.02f;

    private Node2D _targetNode;
    private Control _cursor;
    private ShaderMaterial _cursorMaterial;
    private Vector2 _previousMousePosition;
    private float _pressAmount;
    private float _burstAmount;
    private float _motionAmount;

    public override void _Ready()
    {
        _targetNode = GetNodeOrNull<Node2D>("Node2D");
        _cursor = GetNodeOrNull<Control>("Cursor");
        _cursorMaterial = _cursor?.Material as ShaderMaterial;

        Vector2 mousePosition = GetViewport().GetMousePosition();
        _previousMousePosition = mousePosition;

        UpdateCursorPosition(mousePosition);
        Input.MouseMode = Input.MouseModeEnum.Hidden;
    }

    public override void _ExitTree()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Hidden)
            Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        _burstAmount = 1.0f;
    }

    public override void _Process(double delta)
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();
        UpdateCursorPosition(mousePosition);

        float deltaF = (float)delta;
        float speed = deltaF > 0f ? mousePosition.DistanceTo(_previousMousePosition) / deltaF : 0f;
        bool pressed =
            Input.IsMouseButtonPressed(MouseButton.Left)
            || Input.IsMouseButtonPressed(MouseButton.Right)
            || Input.IsMouseButtonPressed(MouseButton.Middle);

        _pressAmount = Mathf.MoveToward(_pressAmount, pressed ? 1.0f : 0.0f, deltaF * PressLerpSpeed);
        _burstAmount = Mathf.MoveToward(_burstAmount, 0.0f, deltaF * BurstDecaySpeed);
        _motionAmount = Mathf.Lerp(_motionAmount, Mathf.Clamp(speed * MotionResponse, 0.0f, 1.0f), 0.18f);

        if (_cursorMaterial != null)
        {
            _cursorMaterial.SetShaderParameter("press_amount", _pressAmount);
            _cursorMaterial.SetShaderParameter("burst_amount", _burstAmount);
            _cursorMaterial.SetShaderParameter("motion_amount", _motionAmount);
        }

        _previousMousePosition = mousePosition;
    }

    private void UpdateCursorPosition(Vector2 mousePosition)
    {
        if (_targetNode != null)
            _targetNode.GlobalPosition = mousePosition;

        if (_cursor != null)
            _cursor.GlobalPosition = mousePosition - _cursorHotspot;
    }
}
