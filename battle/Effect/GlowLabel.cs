using System;
using Godot;

public partial class GlowLabel : Label
{
    private string _text = "";
    Vector2 OriginalScale;

    public override void _Ready()
    {
        OriginalScale = Scale;
        this.PivotOffset = Size / 2;
    }

    public new string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                base.Text = _text;
                TriggerAnimation();
            }
        }
    }

    public void UpdateNumber(int newValue)
    {
        Text = newValue.ToString();
    }

    private void TriggerAnimation()
    {
        GD.Print("Trigger Animation");
        // Check if the node is inside the scene tree
        if (!IsInsideTree())
        {
            return;
        }
        AddThemeColorOverride("font_color", new Color(0.6f, 0.9f, 1f, 1));
        // 1. Create a ghost label using Duplicate to preserve all inspector properties
        // Duplicate flags: 0 = default (includes script, signals, groups, etc.)
        Label ghost = Duplicate() as Label;
        ghost.AddThemeConstantOverride("outline_size", 0);
        // Remove the GlowLabel script from the ghost to prevent recursion
        // by setting the Script property to null using Variant.From

        AddChild(ghost);
        ghost.Position = Vector2.Zero;
        ghost.PivotOffset = Size / 2; // Ensure scaling from center
        ghost.Scale = Vector2.One;
        // 2. Animation effects
        Tween t1 = CreateTween();
        Tween t2 = CreateTween();
        t1.SetParallel(true);
        // Ghost scales up quickly
        t1.TweenProperty(ghost, "scale", new Vector2(2.5f, 2.5f), 0.5f);
        // Ghost fades out
        t1.TweenProperty(ghost, "theme_override_colors/font_color", new Color(1, 1, 1, 0), 0.5f);
        // Add a slight scale bounce to the original Label
        t2.SetParallel(true);
        t2.TweenProperty(this, "scale", new Vector2(Math.Sign(OriginalScale.X) * 1.2f, 1.2f), 0.4f);
        t2.TweenProperty(this, "theme_override_colors/font_color", new Color(1, 1, 1, 1), 0.2f);
        t2.Chain().TweenProperty(this, "scale", OriginalScale, 0.2f);

        // 3. Delete ghost after animation ends
        t1.Chain().TweenCallback(Callable.From(ghost.QueueFree));
    }
}
