using Godot;
using System;

public partial class SkillButton : Button
{
    public Frame SelfFrame => field ??= GetParent().GetParent() as Frame;
    public VBoxContainer SelfContainer => field ??= GetParent() as VBoxContainer;
    public Label NameLabel => field ??= GetChild(0) as Label;
    public Area2D Detector => field??= GetChild(1) as Area2D;
    public Vector2 PositionIndex;
    public Skill SelfSkill;
    Color HangColor = new Color(0.6f, 0.7f, 1.2f);
    bool animating = false;
    
    public override void _Ready()
    {
        ((ShaderMaterial)Material).SetShaderParameter("inner_radius", 30f);
        ((ShaderMaterial)Material).SetShaderParameter("outer_radius", 35f);
        ((ShaderMaterial)Material).SetShaderParameter("ring_color", new Color(1.1f, 1.1f, 1.1f));
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        
    }
    
    public override void _Process(double delta)
    {
        if (Disabled)
        {
            ((ShaderMaterial)Material).SetShaderParameter("disable", true);
        }
        else
        {
            ((ShaderMaterial)Material).SetShaderParameter("disable", false);
        }
    }
    public void mouse_entered()
    {
        if(animating) return;
        ((ShaderMaterial)Material).SetShaderParameter("ring_color", new Color(0.6f, 0.7f, 1.2f));
        
        // Create tween and animate shader parameters using TweenMethod
        CreateTween()
            .Parallel()
            .TweenMethod(
                Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("inner_radius", value)),
                30,
                35,
                0.1f
            );
        CreateTween()
            .Parallel()
            .TweenMethod(
                Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("outer_radius", value)),
                35,
                40,
                0.1f
            );
    }
    public void mouse_exited()
    {
        if(animating) return;
        ((ShaderMaterial)Material).SetShaderParameter("ring_color", new Color(1.1f, 1.1f, 1.1f));
            CreateTween()
        .Parallel()
        .TweenMethod(
            Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("inner_radius", value)),
            35,
            30,
            0.1f
        );
    CreateTween()
        .Parallel()
        .TweenMethod(
            Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("outer_radius", value)),
            40,
            35,
            0.1f
        );
    }

    // public void press()
    // {
    //     animating = true;
    //     CreateTween()
    //     .Parallel()
    //     .TweenMethod(
    //         Callable.From<Color>(value => ((ShaderMaterial)Material).SetShaderParameter("ring_color", value)),
    //         HangColor,
    //         new Color(1.5f, 1.5f, 1.5f),
    //         0.2f
    //     );
    //     CreateTween()
    //     .Parallel()
    //     .TweenMethod(
    //         Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("inner_radius", value)),
    //         35,
    //         0,
    //         0.2f
    //     );
    //         CreateTween()
    //     .Parallel()
    //     .TweenMethod(
    //         Callable.From<float>(value => ((ShaderMaterial)Material).SetShaderParameter("outer_radius", value)),
    //         40,
    //         5,
    //         0.3f
    //     );
    //     animating = false;
    // }
}
