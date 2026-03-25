using System;
using System.Threading.Tasks;
using Godot;

public partial class SelectButton : Control
{
    private const float SkillButtonEnterDuration = 0.10f;
    private const float SkillButtonEnterFadeDuration = 0.12f;
    private const float SkillButtonExitDuration = 0.10f;
    private const float SkillButtonExitFadeDuration = 0.12f;

    Color out_orignalColor;
    public Skill MySkill;
    public Label ThisLabel => field ??= GetNode("Control/Label") as Label;
    public Vector2 OriginalScale;
    public Panel Border => field ??= GetNode("Control/Border") as Panel;
    public AnimationPlayer animation => field ??= GetNode("AnimationPlayer") as AnimationPlayer;
    public Button Button => field ??= GetNode("Control/Button") as Button;
    public Control Control => field ??= GetNode("Control") as Control;
    private Tip GlobalTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");

    public override void _Process(double delta) { }

    public override void _ExitTree()
    {
        if (GlobalTooltip != null)
            GlobalTooltip.Visible = false;
    }

    public override void _Ready()
    {
        EnsureTipLayer();
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

        if (MySkill == null || GlobalTooltip == null)
            return;

        MySkill.UpdateDescription();
        GlobalTooltip.FollowMouse = true;
        GlobalTooltip.AnchorOffset = new Vector2(24f, 20f);
        GlobalTooltip.Description.Text = BuildSkillTooltipText(MySkill);
        GlobalTooltip.Visible = true;
    }

    public void mouse_exited()
    {
        CreateTween().TweenProperty(this, "scale", OriginalScale, 0.2f);
        Border.Visible = false;

        if (GlobalTooltip != null)
            GlobalTooltip.Visible = false;
    }

    public async void StartAnimation(float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        Control.Position = new Vector2(-50, 0);
        CreateTween()
            .TweenProperty(Control, "position", new Vector2(0, 0), SkillButtonEnterDuration)
            .SetEase(Tween.EaseType.Out);
        CreateTween().TweenProperty(this, "modulate:a", 1f, SkillButtonEnterFadeDuration);
    }

    public async void FadeAnimation(float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        CreateTween().TweenProperty(Control, "position", new Vector2(50, 0), SkillButtonExitDuration);
        CreateTween().TweenProperty(this, "modulate:a", 0f, SkillButtonExitFadeDuration);
    }

    private static string BuildSkillTooltipText(Skill skill)
    {
        string name = string.IsNullOrWhiteSpace(skill.SkillName) ? "未知技能" : skill.SkillName;
        string desc = string.IsNullOrWhiteSpace(skill.Description) ? "-" : skill.Description;
        desc = GlobalFunction.ColorizeNumbers(desc);
        desc = GlobalFunction.ColorizeKeywords(desc);
        return $"[b]{name}[/b]  [color=#cccccc]({skill.SkillType.GetDescription()})[/color]\n{desc}";
    }

    private void EnsureTipLayer()
    {
        var root = GetTree().Root;
        var layer = root.GetNodeOrNull<CanvasLayer>("TipLayer");
        if (layer == null)
        {
            layer = new CanvasLayer { Layer = 6, Name = "TipLayer" };
            root.CallDeferred(Node.MethodName.AddChild, layer);
        }

        if (layer.HasNode("Tip"))
            return;

        var tipScene = GD.Load<PackedScene>("res://battle/UIScene/Tip.tscn");
        if (tipScene == null)
            return;

        var tip = tipScene.Instantiate<Tip>();
        tip.Name = "Tip";
        tip.FollowMouse = true;
        tip.AnchorOffset = new Vector2(24f, 20f);
        layer.AddChild(tip);
    }
}
