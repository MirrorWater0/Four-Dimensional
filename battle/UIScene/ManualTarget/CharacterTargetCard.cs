using System.Threading.Tasks;
using Godot;

public partial class CharacterTargetCard : Control
{
    private static readonly Vector2 NormalScale = Vector2.One;
    private static readonly Vector2 HoverScale = new(1.08f, 1.08f);

    [Signal]
    public delegate void SelectedEventHandler(Character target);

    private Character _target;
    private Tween _hoverTween;
    private bool _selectionPending;

    private Button Button => field ??= GetNode<Button>("Button");
    private TextureRect Portrait => field ??= GetNode<TextureRect>("Panel/Margin/Stack/Portrait");
    private Label NameLabel => field ??= GetNode<Label>("Panel/Margin/Stack/NameLabel");
    private Label StatsLabel => field ??= GetNode<Label>("Panel/Margin/Stack/StatsLabel");
    private Panel Border => field ??= GetNode<Panel>("Border");
    private Tip _skillTooltip;
    private Tip _buffTooltip;

    public bool Selectable { get; set; } = true;
    public bool ShowTooltipOnHover { get; set; } = true;

    public override void _Ready()
    {
        Border.Visible = false;
        PivotOffset = CustomMinimumSize * 0.5f;
        Button.MouseEntered += OnMouseEntered;
        Button.MouseExited += OnMouseExited;
        Button.Pressed += OnButtonPressed;
    }

    public override void _ExitTree()
    {
        HideTargetTooltip();
    }

    private void OnMouseEntered()
    {
        Border.Visible = true;
        ZIndex = 20;
        TweenScale(HoverScale, 0.12f);
        if (ShowTooltipOnHover)
            ShowTargetTooltip();
    }

    private void OnMouseExited()
    {
        Border.Visible = false;
        ZIndex = 0;
        TweenScale(NormalScale, 0.14f);
        HideTargetTooltip();
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

    private async void OnButtonPressed()
    {
        if (
            _selectionPending
            || !Selectable
            || _target == null
            || !GodotObject.IsInstanceValid(_target)
        )
        {
            return;
        }

        _selectionPending = true;
        HideTargetTooltip();
        await PlaySelectionPulseAsync();
        if (_target != null && GodotObject.IsInstanceValid(_target))
        {
            _target.PlayTargetLockPulse();
            EmitSignal(SignalName.Selected, _target);
        }
        _selectionPending = false;
    }

    private async Task PlaySelectionPulseAsync()
    {
        if (!IsInsideTree())
            return;

        _hoverTween?.Kill();
        Vector2 baseScale = Scale == Vector2.Zero ? NormalScale : Scale;
        _hoverTween = CreateTween();
        _hoverTween.SetParallel(true);
        _hoverTween
            .TweenProperty(this, "scale", baseScale * 0.94f, 0.055f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _hoverTween
            .TweenProperty(this, "modulate", new Color(1.28f, 1.22f, 0.74f, 1f), 0.055f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(this, "scale", HoverScale, 0.12f).SetDelay(0.055f);
        _hoverTween.TweenProperty(this, "modulate", Colors.White, 0.12f).SetDelay(0.055f);
        await ToSignal(_hoverTween, Tween.SignalName.Finished);
    }

    public void SetTarget(Character target)
    {
        _target = target;
        NameLabel.Text = target?.CharacterName ?? "-";
        Portrait.Texture = target?.Portrait;
        StatsLabel.Text = target switch
        {
            null => string.Empty,
            { IsPlayer: true } => I18n.Format(
                "ui.manual_target.character_stats",
                "生命 {life}/{max_life}  能量 {energy}",
                ("life", target.Life),
                ("max_life", target.BattleMaxLife),
                ("energy", target.CurrentEnergy)
            ),
            _ => I18n.Format(
                "ui.manual_target.character_stats_no_energy",
                "生命 {life}/{max_life}",
                ("life", target.Life),
                ("max_life", target.BattleMaxLife)
            ),
        };
    }

    public void SetSelectable(bool selectable)
    {
        Selectable = selectable;
        Button.MouseDefaultCursorShape = selectable
            ? CursorShape.PointingHand
            : CursorShape.Arrow;
    }

    public void SetTooltipOnHover(bool enabled)
    {
        ShowTooltipOnHover = enabled;
        if (!enabled)
            HideTargetTooltip();
    }

    public void HideTargetTooltip()
    {
        _skillTooltip?.HideTooltip();
        _buffTooltip?.HideTooltip();
    }

    private void ShowTargetTooltip()
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target))
            return;

        Tip skillTooltip = GetSkillTooltip();
        if (skillTooltip != null)
        {
            skillTooltip.FollowMouse = true;
            skillTooltip.SetText(_target.GetSkillTooltipText());
        }

        Tip buffTooltip = GetBuffTooltip();
        if (buffTooltip != null)
        {
            buffTooltip.FollowMouse = true;
            buffTooltip.SetText(_target.GetBuffTooltipText());
        }
    }

    private Tip GetSkillTooltip()
    {
        if (_skillTooltip != null && GodotObject.IsInstanceValid(_skillTooltip))
            return _skillTooltip;

        _skillTooltip = GetTree()?.Root?.GetNodeOrNull<Tip>("TipLayer/Tip");
        return _skillTooltip;
    }

    private Tip GetBuffTooltip()
    {
        if (_buffTooltip != null && GodotObject.IsInstanceValid(_buffTooltip))
            return _buffTooltip;

        _buffTooltip = GetTree()?.Root?.GetNodeOrNull<Tip>("TipLayer/BuffTip");
        return _buffTooltip;
    }
}
