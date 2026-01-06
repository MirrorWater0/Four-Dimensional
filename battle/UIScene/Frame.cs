using Godot;
using System;

public partial class Frame : TextureRect
{
	public Label NameLabel => field??=GetNode<Label>("Name");
	public int IDindex;
	public Control SkillButtonContainer => field ??= GetNode("SkillControl") as Control;
	public SkillButton SkillButton1 => field ??= GetNode("SkillControl/skill1") as SkillButton;
	public SkillButton SkillButton2 => field ??= GetNode("SkillControl/skill2") as SkillButton;
	public SkillButton SkillButton3 => field ??= GetNode("SkillControl/skill3") as SkillButton;
	public ColorRect Selected => field ??= GetNode("Selected") as ColorRect;
	public Button ClickButton => field ??= GetNode("ClickButton") as Button;
	
	// Radius for circular arrangement
	private float _radius = 135f;
	// Angle spacing between buttons (2π/3 = 120 degrees)
	private float _angleSpacing = (float)(2 * Math.PI / 3);
	// Current rotation angle
	private float _currentRotation = 0f;
	// Rotation speed (radians per second)
	private float _rotationSpeed = 0.2f;
	
	public override void _Ready()
	{
		ClickButton.Visible = false;
		Selected.Visible = false;
		// Set pivot offset for all buttons
		for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
		{
			var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
			skillButton.PivotOffset = skillButton.Size / 2;
		}
		MouseEntered += Mouse_Entered;
		MouseExited += Mouse_Exited;
	}

	   public override void _Process(double delta)
	   {
	 // Update rotation angle
	 _currentRotation += (float)(_rotationSpeed * delta);
	 
	 // Calculate origin as center of the container
	 Vector2 origin = SkillButtonContainer.Size / 2;
	 
	 // Update positions for all buttons (orbiting around origin, no self-rotation)
	 for (int i = 0; i < SkillButtonContainer.GetChildCount(); i++)
	 {
	  var skillButton = SkillButtonContainer.GetChild(i) as SkillButton;
	  float angle = _currentRotation + i * _angleSpacing;
	  Vector2 circularPosition = origin + new Vector2(
	   (float)Math.Cos(angle) * _radius,
	   (float)Math.Sin(angle) * _radius
	  );
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
			Vector2 circularPosition = origin + new Vector2(
				(float)Math.Cos(angle) * _radius,
				(float)Math.Sin(angle) * _radius
			);
			// Offset by half button size to center the button on the calculated position
			Vector2 centeredPosition = circularPosition - skillButton.Size / 2;
			skillButton.PositionIndex = centeredPosition;
			// Animate both position and rotation (convert to degrees)
			var tween = CreateTween();
			tween.TweenProperty(skillButton, "position", centeredPosition, 0.3f);
			tween.TweenProperty(skillButton, "rotation_degrees", (float)(angle * 180.0 / Math.PI), 0.3f);
		}
	}

	public void Mouse_Entered()
	{
		((ShaderMaterial)Material).SetShaderParameter("theme_color", 3*new Color(0.7f, 1, 1, 1));

	}
	public void Mouse_Exited()
	{
		((ShaderMaterial)Material).SetShaderParameter("theme_color", new Color(1f, 1, 1, 1));
	}
}
