using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AimTarget : ColorRect
{
    static PackedScene AimScene = GD.Load<PackedScene>("res://ConsumeItems/AimTarget.tscn");
    private static readonly Color HoverColor = new(1f, 0.95f, 0.62f, 1f);

    private Battle _battle;
    private Character _hoveredTarget;
    private Tween _introTween;
    private Tween _outroTween;
    private Tween _hoverPulseTween;
    private bool _isHiding;

    public static async Task<Character> AimTargetTask(Battle battle)
    {
        if (battle == null || AimScene == null)
            return null;

        var aim = AimScene.Instantiate<AimTarget>();
        aim._battle = battle;
        battle.AddChild(aim);
        aim.SetProcessInput(true);
        aim.PlayAppear();

        var tcs = new TaskCompletionSource<Character>();

        void HandleInput(InputEvent @event)
        {
            if (MobilePlatform.IsCancelPress(@event))
            {
                tcs.TrySetResult(null);
                return;
            }

            if (@event is InputEventScreenTouch { Pressed: true } screenTouch)
            {
                Character touchTarget = FindCharacterAtPoint(battle, screenTouch.Position);
                if (touchTarget != null)
                {
                    touchTarget.PlayTargetLockPulse(HoverColor, 1.1f);
                    tcs.TrySetResult(touchTarget);
                }
                return;
            }

            if (@event is not InputEventMouseButton mouseButton)
                return;
            if (!mouseButton.Pressed)
                return;

            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                tcs.TrySetResult(null);
                return;
            }

            if (mouseButton.ButtonIndex != MouseButton.Left)
                return;

            var target = FindCharacterAtPoint(battle, mouseButton.Position);
            if (target != null)
            {
                target.PlayTargetLockPulse(HoverColor, 1.1f);
                tcs.TrySetResult(target);
            }
        }

        aim.InputReceived += HandleInput;
        aim.CancelRequested += HandleCancelRequested;

        void HandleCancelRequested() => tcs.TrySetResult(null);

        var result = await tcs.Task;

        aim.InputReceived -= HandleInput;
        aim.CancelRequested -= HandleCancelRequested;
        if (GodotObject.IsInstanceValid(aim))
        {
            aim.SetProcessInput(false);
            aim.ClearHoveredTarget();
            await aim.PlayDisappear();
        }
        if (GodotObject.IsInstanceValid(aim))
            aim.QueueFree();

        return result;
    }

    public override void _Ready()
    {
        PivotOffset = Size / 2f;
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 0f);
        Scale = new Vector2(0.72f, 0.72f);
        CreateMobileCancelButton();
    }

    public override void _Process(double delta)
    {
        PivotOffset = Size / 2f;
        GlobalPosition = GetGlobalMousePosition() - (Size * Scale) / 2f;
        RefreshHoveredTarget();
    }

    public event Action<InputEvent> InputReceived;
    public event Action CancelRequested;

    public override void _Input(InputEvent @event)
    {
        InputReceived?.Invoke(@event);
    }

    private void CreateMobileCancelButton()
    {
        if (!MobilePlatform.IsMobile)
            return;

        var layer = new CanvasLayer { Name = "MobileCancelLayer", Layer = 95 };
        var button = new Button
        {
            Name = "CancelButton",
            Text = I18n.Tr("ui.common.cancel", "取消"),
            CustomMinimumSize = new Vector2(168f, 76f),
            FocusMode = FocusModeEnum.None,
        };
        button.AnchorLeft = 1f;
        button.AnchorRight = 1f;
        button.OffsetLeft = -204f;
        button.OffsetRight = -36f;
        button.OffsetTop = 76f;
        button.OffsetBottom = 152f;
        button.Pressed += () => CancelRequested?.Invoke();
        layer.AddChild(button);
        AddChild(layer);
    }

    private void PlayAppear()
    {
        if (!IsInsideTree())
            return;

        _introTween?.Kill();
        _introTween = CreateTween();
        _introTween.SetParallel(true);
        _introTween
            .TweenProperty(this, "scale", Vector2.One, 0.18f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _introTween
            .TweenProperty(this, "modulate:a", 1.0f, 0.14f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private void RefreshHoveredTarget()
    {
        if (_battle == null || !GodotObject.IsInstanceValid(_battle))
            return;

        Character nextTarget = FindCharacterAtPoint(_battle, GetGlobalMousePosition());
        if (_hoveredTarget == nextTarget)
            return;

        ClearHoveredTarget();
        _hoveredTarget = nextTarget;
        if (_hoveredTarget == null || !GodotObject.IsInstanceValid(_hoveredTarget))
            return;

        _hoveredTarget.ShowTargetPreview(HoverColor);
        PlayHoverPulse();
    }

    private void ClearHoveredTarget()
    {
        _hoverPulseTween?.Kill();
        _hoverPulseTween = null;
        if (!_isHiding)
            Scale = Vector2.One;

        if (_hoveredTarget != null && GodotObject.IsInstanceValid(_hoveredTarget))
            _hoveredTarget.HideTargetPreview();

        _hoveredTarget = null;
    }

    private void PlayHoverPulse()
    {
        if (!IsInsideTree())
            return;

        _hoverPulseTween?.Kill();
        _hoverPulseTween = CreateTween();
        _hoverPulseTween.SetLoops();
        _hoverPulseTween
            .TweenProperty(this, "scale", new Vector2(1.08f, 1.08f), 0.42f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        _hoverPulseTween
            .TweenProperty(this, "scale", Vector2.One, 0.42f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private async Task PlayDisappear()
    {
        if (_isHiding || !IsInsideTree())
            return;

        _isHiding = true;
        _introTween?.Kill();
        _hoverPulseTween?.Kill();
        _outroTween?.Kill();
        _outroTween = CreateTween();
        _outroTween.SetParallel(true);
        _outroTween
            .TweenProperty(this, "scale", new Vector2(0.7f, 0.7f), 0.16f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _outroTween
            .TweenProperty(this, "modulate:a", 0.0f, 0.16f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);

        await ToSignal(_outroTween, Tween.SignalName.Finished);
    }

    private static Character FindCharacterAtPoint(Battle battle, Vector2 point)
    {
        if (battle == null)
            return null;

        var candidates = battle
            .GetTeamCharacters(isPlayer: true, includeSummons: true)
            .Concat(battle.GetTeamCharacters(isPlayer: false, includeSummons: true))
            .Where(x => x != null)
            .ToList();

        Character best = null;
        int bestZ = int.MinValue;
        float bestY = float.MinValue;

        foreach (var character in candidates)
        {
            if (!character.IsInsideTree())
                continue;

            var hover = character.Hoverframe;
            if (hover == null)
                continue;

            if (!hover.GetGlobalRect().HasPoint(point))
                continue;

            int z = character.ZIndex;
            float y = character.GlobalPosition.Y;

            if (best == null || z > bestZ || (z == bestZ && y > bestY))
            {
                best = character;
                bestZ = z;
                bestY = y;
            }
        }

        return best;
    }
}
