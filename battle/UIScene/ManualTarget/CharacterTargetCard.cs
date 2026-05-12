using Godot;

public partial class CharacterTargetCard : Control
{
    private static readonly Vector2 NormalScale = Vector2.One;
    private static readonly Vector2 HoverScale = new(1.08f, 1.08f);

    [Signal]
    public delegate void SelectedEventHandler(Character target);

    private Character _target;
    private Tween _hoverTween;

    private Button Button => field ??= GetNode<Button>("Button");
    private TextureRect Portrait => field ??= GetNode<TextureRect>("Panel/Margin/Stack/Portrait");
    private Label NameLabel => field ??= GetNode<Label>("Panel/Margin/Stack/NameLabel");
    private Label StatsLabel => field ??= GetNode<Label>("Panel/Margin/Stack/StatsLabel");
    private Panel Border => field ??= GetNode<Panel>("Border");

    public override void _Ready()
    {
        Border.Visible = false;
        PivotOffset = CustomMinimumSize * 0.5f;
        Button.MouseEntered += OnMouseEntered;
        Button.MouseExited += OnMouseExited;
        Button.Pressed += () =>
        {
            if (_target != null && GodotObject.IsInstanceValid(_target))
                EmitSignal(SignalName.Selected, _target);
        };
    }

    private void OnMouseEntered()
    {
        Border.Visible = true;
        ZIndex = 20;
        TweenScale(HoverScale, 0.12f);
    }

    private void OnMouseExited()
    {
        Border.Visible = false;
        ZIndex = 0;
        TweenScale(NormalScale, 0.14f);
    }

    private void TweenScale(Vector2 targetScale, float duration)
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween
            .TweenProperty(this, "scale", targetScale, duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public void SetTarget(Character target)
    {
        _target = target;
        NameLabel.Text = target?.CharacterName ?? "-";
        Portrait.Texture = target?.Portrait;
        StatsLabel.Text = target == null
            ? string.Empty
            : $"生命 {target.Life}/{target.BattleMaxLife}  能量 {target.Energy}";
    }
}
