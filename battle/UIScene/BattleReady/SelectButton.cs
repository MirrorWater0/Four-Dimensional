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
    public bool AllowPressEffect { get; set; } = true;
    public Label ThisLabel => field ??= GetNode("Control/Label") as Label;
    public Vector2 OriginalScale;
    public Panel Border => field ??= GetNode("Control/Border") as Panel;
    public AnimationPlayer animation => field ??= GetNode("AnimationPlayer") as AnimationPlayer;
    public Button Button => field ??= GetNode("Control/Button") as Button;
    public Control Control => field ??= GetNode("Control") as Control;
    private Tip GlobalTooltip => field ??= GetTree().Root.GetNodeOrNull<Tip>("TipLayer/Tip");
    private Tween _scaleTween;
    private Tween _enterTween;
    private Tween _fadeTween;

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
            if (!AllowPressEffect)
                return;

            TweenScale(OriginalScale, 0.1f);
        };
        Button.ButtonDown += () =>
        {
            if (!AllowPressEffect)
                return;

            animation.Play("explode");
        };

        Modulate = new Color(1, 1, 1, 0);
    }

    public void mouse_entered()
    {
        TweenScale(1.02f * OriginalScale, 0.12f);
        Border.Visible = true;

        if (MySkill == null || GlobalTooltip == null)
            return;

        MySkill.UpdateDescription();
        GlobalTooltip.FollowMouse = true;
        GlobalTooltip.AnchorOffset = new Vector2(24f, 20f);
        GlobalTooltip.SetText(BuildSkillTooltipText(MySkill));
    }

    public void mouse_exited()
    {
        TweenScale(OriginalScale, 0.14f);
        Border.Visible = false;

        GlobalTooltip?.HideTooltip();
    }

    public async void StartAnimation(float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        Control.Position = new Vector2(-50, 0);
        _enterTween?.Kill();
        _enterTween = CreateTween();
        _enterTween
            .TweenProperty(Control, "position", new Vector2(0, 0), SkillButtonEnterDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween
            .TweenProperty(this, "modulate:a", 1f, SkillButtonEnterFadeDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public async void FadeAnimation(float delay)
    {
        await ToSignal(GetTree().CreateTimer(delay), "timeout");
        _enterTween?.Kill();
        _enterTween = CreateTween();
        _enterTween
            .TweenProperty(Control, "position", new Vector2(50, 0), SkillButtonExitDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween
            .TweenProperty(this, "modulate:a", 0f, SkillButtonExitFadeDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void TweenScale(Vector2 targetScale, float duration)
    {
        _scaleTween?.Kill();
        _scaleTween = CreateTween();
        _scaleTween
            .TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
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
