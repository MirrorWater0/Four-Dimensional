using System;
using System.Threading.Tasks;
using Godot;

public partial class SelectButton : Control
{
    Color out_orignalColor;
    public Skill MySkill;
    public Label ThisLabel => field ??= GetNode("Control/Label") as Label;
    public Vector2 OriginalScale;
    public Panel Border => field ??= GetNode("Control/Border") as Panel;
    public AnimationPlayer animation => field ??= GetNode("AnimationPlayer") as AnimationPlayer;
    public Button Button => field ??= GetNode("Control/Button") as Button;
    public Control Control => field ??= GetNode("Control") as Control;
    public override void _Process(double delta) { }

    public override void _Ready()
    {
        Border.Visible = false;
        PivotOffset = Size / 2;
        OriginalScale = Scale;
        Button.MouseEntered += mouse_entered;
        Button.MouseExited += mouse_exited;
        Button.Pressed += () =>
        {
            CreateTween().TweenProperty(this, "scale", OriginalScale, 0.1f);
        };
        Button.ButtonDown += () =>
        {
            animation.Play("explode");
        };

        Modulate = new Color(1, 1, 1, 0);

    }

    public void mouse_entered()
    {
        CreateTween().TweenProperty(this, "scale", 1.02f * OriginalScale, 0.15f);
        Border.Visible = true;
    }

    public void mouse_exited()
    {
        CreateTween().TweenProperty(this, "scale", OriginalScale, 0.2f);
        Border.Visible = false;
    }

    public async void StartAnimation(float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        Control.Position = new Vector2(-50, 0);
        CreateTween().TweenProperty(Control,"position", new Vector2(0,0), 0.2f);
        CreateTween().TweenProperty(this, "modulate:a", 1f, 0.15f);
    }

    public async void FadeAnimation(float delay)
    {
        GD.Print("FadeAnimation", delay);
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        CreateTween().TweenProperty(Control,"position", new Vector2(50,0), 0.2f);
        CreateTween().TweenProperty(this, "modulate:a", 0f, 0.2f);
    }
    // public void Selected()
    // {
    //     out_orignalColor = new Color(245f, 180f, 0f, 255f)/255f;
    //     Rect.Color = out_orignalColor;

    // }

    // public void UnSelected()
    // {
    //     out_orignalColor = new Color(100f, 155f, 255f, 255f)/255f;
    // }
}
