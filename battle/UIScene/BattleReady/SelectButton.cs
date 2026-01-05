using Godot;
using System;


public partial class SelectButton : TextureButton
{
    Color out_orignalColor;
    public Label ThisLabel => field??= GetNode("Label") as Label;
    public override void _Process(double delta)
    {

    }
    public override void _Ready()
    {
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        ButtonDown += Selected;
        out_orignalColor = (Color)((ShaderMaterial)Material).GetShaderParameter("outer_rim_color");
    }

    public void mouse_entered()
    {
        ((ShaderMaterial)Material).SetShaderParameter("bg_color", new Color(1.5f, 1.5f, 1.5f));
        ((ShaderMaterial)Material).SetShaderParameter("outer_rim_color",out_orignalColor * new Color(1.5f, 1.5f, 1.5f));
    }

    public void mouse_exited()
    {
        ((ShaderMaterial)Material).SetShaderParameter("bg_color", 0.95f*new Color(1, 1, 1));
        ((ShaderMaterial)Material).SetShaderParameter("outer_rim_color", out_orignalColor);
    }

    public void Selected()
    {
        out_orignalColor = new Color(245f, 180f, 0f, 255f)/255f;
        ((ShaderMaterial)Material).SetShaderParameter("outer_rim_color", out_orignalColor);
    }
}
