using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class AimTarget : ColorRect
{
    static PackedScene AimScene = GD.Load<PackedScene>("res://ConsumeItems/AimTarget.tscn");
    private Tween _outroTween;
    private bool _isHiding;

    public static async Task<Character> AimTargetTask(Battle battle)
    {
        if (battle == null || AimScene == null)
            return null;

        var aim = AimScene.Instantiate<AimTarget>();
        battle.AddChild(aim);
        aim.SetProcessInput(true);

        var tcs = new TaskCompletionSource<Character>();

        void HandleInput(InputEvent @event)
        {
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
                tcs.TrySetResult(target);
        }

        aim.InputReceived += HandleInput;

        var result = await tcs.Task;

        aim.InputReceived -= HandleInput;
        if (GodotObject.IsInstanceValid(aim))
        {
            aim.SetProcessInput(false);
            await aim.PlayDisappear();
        }
        if (GodotObject.IsInstanceValid(aim))
            aim.QueueFree();

        return result;
    }

    public override void _Ready() { }

    public override void _Process(double delta)
    {
        PivotOffset = Size / 2f;
        GlobalPosition = GetGlobalMousePosition() - (Size * Scale) / 2f;
    }

    public event Action<InputEvent> InputReceived;

    public override void _Input(InputEvent @event)
    {
        InputReceived?.Invoke(@event);
    }

    private async Task PlayDisappear()
    {
        if (_isHiding || !IsInsideTree())
            return;

        _isHiding = true;
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

        var candidates = new List<Character>();
        if (battle.PlayersList != null)
            candidates.AddRange(battle.PlayersList.Where(x => x != null));
        if (battle.EnemiesList != null)
            candidates.AddRange(battle.EnemiesList.Where(x => x != null));

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
