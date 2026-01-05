using Godot;
using System;

public partial class SkillButton : Button
{
    [Export]
    public Skill.SkillTypes MySkillType;
    public Frame SelfFrame => field ??= GetParent().GetParent() as Frame;
    public VBoxContainer SelfContainer => field ??= GetParent() as VBoxContainer;
    public Label NameLabel => field ??= GetChild(0) as Label;
    public Area2D Detector => field??= GetChild(1) as Area2D;
    
    public Vector2 PositionIndex;
    public Skill SelfSkill;
    private ColorRect ShockWave => field??= GetNode<ColorRect>("ShockWave");
    private ColorRect AbleRing => field??= GetNode<ColorRect>("AbleRing");
    private ColorRect SparkLight => field??= GetNode<ColorRect>("SparkLight");
    private ColorRect SwordIcon => field??= GetNode<ColorRect>("SwordIcon");
    private ColorRect RhomboidIcon => field??= GetNode<ColorRect>("RhomboidIcon");
    private ColorRect TerminateSkillIcon => field??= GetNode<ColorRect>("TerminateSkillIcon");
    Color HangColor = new Color(0.6f, 0.7f, 1.2f);
    bool animating = false;
    
    public override void _Ready()
    {
        
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        Modulate = new Color(0.92f, 0.92f, 0.92f);

        switch (MySkillType)
        {
            case Skill.SkillTypes.Attack:
                RhomboidIcon.Visible = false;
                TerminateSkillIcon.Visible = false;
                break;
            case Skill.SkillTypes.Defence:
                SwordIcon.Visible = false;
                TerminateSkillIcon.Visible = false;
                break;
            case Skill.SkillTypes.Special:
                RhomboidIcon.Visible = false;
                SwordIcon.Visible = false;
                break;
        }       
    }
    
    public override void _Process(double delta)
    {
        if (Disabled)
        {
            AbleRing.Visible = false;
        }
        else
        {
            AbleRing.Visible = true;
        }
    }
    public void mouse_entered()
    {
        Modulate = new Color(2.5f, 2.5f, 2.5f);
        
        
    }
    public void mouse_exited()
    {
        Modulate = new Color(0.9f, 0.9f, 0.9f);
    }

    public void Enable()
    {
        Disabled = false;
        var tween = ShockWave.CreateTween();

        CreateTween().Parallel().TweenCallback(Callable.From(() => animating = false));
                tween.TweenMethod(
            Callable.From<float>(value => ((ShaderMaterial)SparkLight.Material).SetShaderParameter("progress", value)),
            0.3,
            1f,
            0.4f
        ).SetEase(Tween.EaseType.Out);

        tween.Parallel().TweenMethod(
            Callable.From<float>(value => ((ShaderMaterial)ShockWave.Material).SetShaderParameter("progress", value)),
            0.3,
            1f,
            0.4f
        ).SetEase(Tween.EaseType.Out);
        
    }


}
