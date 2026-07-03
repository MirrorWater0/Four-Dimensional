using System.Threading.Tasks;
using Godot;

public partial class BuffTriggerFlashVfx : Node2D
{
    private const float IconSize = 112f;
    private const float VerticalOffset = -36f;

    private Buff _buff;
    private Control _icon;
    private Tween _tween;

    public void Initialize(Buff buff) => _buff = buff;

    public override void _Ready()
    {
        if (
            _buff == null
            || _buff.Owner == null
            || !GodotObject.IsInstanceValid(_buff.Owner)
            || _buff.Owner.State == Character.CharacterState.Dying
        )
        {
            QueueFree();
            return;
        }

        _icon = Buff.CreateBuffTooltipIcon(_buff.ThisBuffName);
        if (_icon == null)
        {
            QueueFree();
            return;
        }

        _icon.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _icon.Size = new Vector2(IconSize, IconSize);
        _icon.PivotOffset = new Vector2(IconSize / 2f, IconSize / 2f);
        _icon.Position = new Vector2(-IconSize / 2f, -IconSize / 2f + VerticalOffset);
        _icon.Modulate = new Color(1f, 1f, 1f, 0.7f);
        AddChild(_icon);
        _ = PlayAsync();
    }

    public override void _ExitTree() => _tween?.Kill();

    private async Task PlayAsync()
    {
        _tween = CreateTween();
        _tween.SetParallel();
        _tween
            .TweenProperty(_icon, "scale", Vector2.One * 1.75f, 0.42)
            .From(Vector2.One * 0.55f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        _tween
            .TweenProperty(_icon, "modulate:a", 1f, 0.22)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        _tween.SetParallel(false);
        _tween
            .TweenProperty(_icon, "scale", Vector2.One * 1.85f, 0.24)
            .SetEase(Tween.EaseType.Out);
        _tween
            .TweenProperty(_icon, "modulate:a", 0f, 0.32)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Sine);
        await ToSignal(_tween, Tween.SignalName.Finished);
        QueueFree();
    }
}
