using System;
using Godot;


public partial class Frame : TextureRect
{
    public Label NameLabel => field ??= GetNode<Label>("Name");
    public int IDindex;
    public Control SkillButtonContainer => field ??= GetNode("SkillControl") as Control;
    public SkillButton SkillButton1 => field ??= GetNode("SkillControl/skill1") as SkillButton;
    public SkillButton SkillButton2 => field ??= GetNode("SkillControl/skill2") as SkillButton;
    public SkillButton SkillButton3 => field ??= GetNode("SkillControl/skill3") as SkillButton;
    public ColorRect Selected => field ??= GetNode("Selected") as ColorRect;
    public Button ClickButton => field ??= GetNode("ClickButton") as Button;

    // Radius for circular arrangement
    private float _radius = 140f;

    // Angle spacing between buttons (2π/3 = 120 degrees)
    private float _angleSpacing = (float)(2 * Math.PI / 3);

    // Current rotation angle
    private float _currentRotation = 0f;

    // Rotation speed (radians per second)
    private float _rotationSpeed = 0.2f;

    public override void _Ready()
    {
        PivotOffset = Size / 2;
        // _mat = Material as ShaderMaterial;
        _mat.ResourceLocalToScene = true;
        // ClickButton.Visible = false;
        Selected.Visible = false;
        // Set pivot offset for all buttons
        for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
        {
            var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
            skillButton.PivotOffset = skillButton.Size / 2;
        }
        ClickButton.MouseEntered += Mouse_Entered;
        ClickButton.MouseExited += Mouse_Exited;
        for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
        {
            var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
            skillButton.MouseEntered += Mouse_Entered;
            skillButton.MouseExited += Mouse_Exited;
        }
    }

    public override void _Process(double delta)
    {
        ClickButton.Position = Size / 2 - ClickButton.Size / 2;
        UIShaderRotate(delta);
        // Update rotation angle
        _currentRotation += (float)(_rotationSpeed * delta);

        // Calculate origin as center of the container
        Vector2 origin = SkillButtonContainer.Size / 2;

        // Update positions for all buttons (orbiting around origin, no self-rotation)
        for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
        {
            var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
            float angle = _currentRotation + i * _angleSpacing;
            Vector2 circularPosition =
                origin
                + new Vector2((float)Math.Cos(angle) * _radius, (float)Math.Sin(angle) * _radius);
            // Offset by half button size to center the button on the calculated position
            Vector2 centeredPosition = circularPosition - skillButton.Size / 2;
            skillButton.Position = centeredPosition;
        }
    }

    public void SortButtons()
    {
        // Calculate origin as center of the container
        Vector2 origin = SkillButtonContainer.Size / 2;

        // Animate buttons to their circular positions with 2π/3 spacing
        for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
        {
            var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
            float angle = i * _angleSpacing;
            Vector2 circularPosition =
                origin
                + new Vector2((float)Math.Cos(angle) * _radius, (float)Math.Sin(angle) * _radius);
            // Offset by half button size to center the button on the calculated position
            Vector2 centeredPosition = circularPosition - skillButton.Size / 2;
            skillButton.PositionIndex = centeredPosition;
            // Animate both position and rotation (convert to degrees)
            var tween = CreateTween();
            tween.TweenProperty(skillButton, "position", centeredPosition, 0.3f);
            tween.TweenProperty(
                skillButton,
                "rotation_degrees",
                (float)(angle * 180.0 / Math.PI),
                0.3f
            );
        }
    }

    public void Mouse_Entered()
    {
        // ((ShaderMaterial)Material).SetShaderParameter("theme_color", 3 * new Color(0.7f, 1, 1, 1));
        TweenUIshader("hover_progress", 1f, 0.2f);
        ClickButton.Scale = new Vector2(1.7f, 1.7f);
        _targetSpeed = 2f;
    }

    public void Mouse_Exited()
    {
        // ((ShaderMaterial)Material).SetShaderParameter("theme_color", new Color(1f, 1, 1, 1));
        TweenUIshader("hover_progress", 0f, 0.2f);
        ClickButton.Scale = new Vector2(1, 1);
        _targetSpeed = 1.5f;
    }

    private ShaderMaterial _mat => Material as ShaderMaterial;
    private float _currentRotationTime = 0f;
    private float _hoverProgress = 0f;

    // 速度配置
    private float _baseSpeed = 1.5f;
    private float _targetSpeed = 1.5f;
    private float _currentSpeed = 1.2f;

    public void UIShaderRotate(double delta)
    {
		if (_mat == null) return;
        float fDelta = (float)delta;

        // 1. 平滑插值速度 (防止速度突变)
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, fDelta * 5.0f);

        // 2. 累加旋转量 (核心：这就是稳定的秘诀)
        _currentRotationTime += fDelta * _currentSpeed;
        _mat.SetShaderParameter("u_rotation_time", _currentRotationTime);
    }

    public void TweenUIshader(string var, float val, float duration)
    {
        CreateTween()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)Material).SetShaderParameter(var, value)
                ),
                ((ShaderMaterial)Material).GetShaderParameter(var),
                val,
                duration
            );
    }
}
