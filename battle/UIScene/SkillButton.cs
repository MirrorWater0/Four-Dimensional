using System;
using Godot;

public partial class SkillButton : Button
{
    public const float DisabledAlpha = 0.7f;
    public static readonly Color EnabledModulate = new Color(1, 1, 1, 1f);
    public static readonly Color DisabledModulate = new Color(1, 1, 1, DisabledAlpha);

    [Export]
    public Skill.SkillTypes MySkillType;
    public Frame SelfFrame => field ??= GetParent().GetParent() as Frame;
    public VBoxContainer SelfContainer => field ??= GetParent() as VBoxContainer;

    public Vector2 PositionIndex;
    public Skill SelfSkill;
    private ColorRect ShockWave => field ??= GetNode<ColorRect>("ShockWave");
    private ColorRect AbleRing => field ??= GetNode<ColorRect>("AbleRing");
    private ColorRect SparkLight => field ??= GetNode<ColorRect>("SparkLight");
    private ColorRect SwordIcon => field ??= GetNode<ColorRect>("SwordIcon");
    private ColorRect RhomboidIcon => field ??= GetNode<ColorRect>("RhomboidIcon");
    private ColorRect TerminateSkillIcon => field ??= GetNode<ColorRect>("TerminateSkillIcon");
    Color HangColor = new Color(0.6f, 0.7f, 1.2f);
    bool animating = false;

    public Tip globalTooltip => field ??= GetTree().Root.GetNode<Tip>("TipLayer/Tip");

    public override void _Ready()
    {
        MouseEntered += mouse_entered;
        MouseExited += mouse_exited;
        Modulate = new Color(0.9f, 0.9f, 0.9f);

        ApplySkillTypeIcons();
    }

    public void SetSkillType(Skill.SkillTypes skillType)
    {
        MySkillType = skillType;
        ApplySkillTypeIcons();
    }

    private void ApplySkillTypeIcons()
    {
        if (!IsInsideTree())
            return;

        SwordIcon.Visible = true;
        RhomboidIcon.Visible = true;
        TerminateSkillIcon.Visible = true;

        switch (MySkillType)
        {
            case Skill.SkillTypes.Attack:
                RhomboidIcon.Visible = false;
                TerminateSkillIcon.Visible = false;
                break;
            case Skill.SkillTypes.Survive:
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

    Color changeColor = 0.4f * new Color(0.5f, 0.5f, 0.5f, 0.4f);

    public void mouse_entered()
    {
        Modulate += changeColor;

        // Show tooltip with skill description
        if (SelfSkill != null && globalTooltip != null)
        {
            globalTooltip.FollowMouse = true;
            SelfSkill.UpdateDescription();
            globalTooltip.Description.Text = SelfSkill.Description;
            globalTooltip.Visible = true;
        }
    }

    public void mouse_exited()
    {

        Modulate -= changeColor;

        // Hide tooltip
        if (globalTooltip != null)
        {
            globalTooltip.Visible = false;
        }
    }

    public void Enable()
    {
        Disabled = false;
        var tween = ShockWave.CreateTween();

        CreateTween().Parallel().TweenCallback(Callable.From(() => animating = false));
        tween
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)SparkLight.Material).SetShaderParameter("progress", value)
                ),
                0.3,
                1f,
                0.4f
            )
            .SetEase(Tween.EaseType.Out);

        tween
            .Parallel()
            .TweenMethod(
                Callable.From<float>(value =>
                    ((ShaderMaterial)ShockWave.Material).SetShaderParameter("progress", value)
                ),
                0.3,
                1f,
                0.4f
            )
            .SetEase(Tween.EaseType.Out);
    }
}
