using Godot;
using System;


public partial class SelectButton : Button
{
    Color out_orignalColor;
    public Skill MySkill;
    public Label ThisLabel => field??= GetNode("Label") as Label;
    public Vector2 OriginalScale;
    public Panel Border => field??= GetNode("Border") as Panel;
    public override void _Process(double delta)
    {

    }
    public override void _Ready()
    {
        Border.Visible = false;
        PivotOffset = Scale/2;
        OriginalScale = Scale;
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        Pressed += () =>{CreateTween().TweenProperty(this, "scale", OriginalScale, 0.2f);};
    }

    public void mouse_entered()
    {
        CreateTween().TweenProperty(this, "scale", 1.05f*OriginalScale, 0.15f);
        Border.Visible = true;
    }

    public void mouse_exited()
    {
        CreateTween().TweenProperty(this, "scale", OriginalScale, 0.2f);
        Border.Visible = false;
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
