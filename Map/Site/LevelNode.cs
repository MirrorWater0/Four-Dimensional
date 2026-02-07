using System;
using System.Security.Cryptography.X509Certificates;
using Godot;

public partial class LevelNode : ColorRect
{
    public enum LevelState
    {
        Locked,
        Unlocked,
        Completed,
    }

    public enum LevelType
    {
        Battle,
        Event,
        Elite,
    }

    public LevelState State { get; set; }
    public LevelType Type { get; set; }

    public Button Button => field ??= GetNode("Button") as Button;
    public ProgressBar ProgressBar => field ??= GetNode("ProgressBar") as ProgressBar;
    public ShaderMaterial mat;
    public Color LockColor = new Color(0.9f, 0.9f, 0.9f, 1);

    public override void _Ready()
    {
        mat = Material.Duplicate() as ShaderMaterial;
        mat.ResourceLocalToScene = true;
        Material = mat;
        mat.SetShaderParameter("show_inner", false);

        Color = LockColor;
        Button.Disabled = true;
        ProgressBar.Value = 0;
        Button.Disabled = State == LevelState.Locked;
        StartAnimation();
    }

    public void Unlock()
    {
        Color = 2 * new Color(1, 1, 1, 1);
        mat.SetShaderParameter("ring_color", new Color(1, 1f, 1, 1));
        State = LevelState.Unlocked;
        Button.Disabled = false;
    }

    public void CompletedAnimation()
    {
        State = LevelState.Completed;
        Button.Disabled = true;
        mat.SetShaderParameter("show_inner", true);
        mat.SetShaderParameter("ring_color", new Color(1, 0.8f, 0, 1));
        Tween tween = CreateTween();
        tween.TweenProperty(ProgressBar, "value", 100, 0.5f);
    }

    public void StartAnimation()
    {
        ProgressBar.Scale = new Vector2(0, 1);
        Tween tween = CreateTween();
        tween.TweenProperty(ProgressBar, "scale", new Vector2(1, 1), 0.5f);
    }
}
