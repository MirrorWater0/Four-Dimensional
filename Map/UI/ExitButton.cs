using System;
using System.Collections.Generic;
using Godot;

public partial class ExitButton : Button
{
    public delegate void ExitButtonPressed();
    public ObservableList<Action> PressedActions = new (); // List of actions to be executed when the button is pressed>
    public Vector2 OriginalPosition = new Vector2(-10, 130);
    private Tween _hoverTween;

    public override void _Ready()
    {
        Visible = false;
        Position = OriginalPosition;
        MouseEntered += () => AnimateHover(true);
        MouseExited += () => AnimateHover(false);

        PressedActions.ItemAdded += item =>
        {
            Visible = true;
        };
        PressedActions.ItemRemoved += item =>
        {
            if (PressedActions.Count == 0)
            {
                Visible = false;
            }
        };
        Pressed += () =>
        {
            PressedActions[PressedActions.Count - 1]?.Invoke();
            PressedActions.RemoveAt(PressedActions.Count - 1);
        };
    }

    private void AnimateHover(bool hovered)
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween
            .TweenProperty(
                this,
                "position",
                hovered ? OriginalPosition + 50 * Vector2.Right : OriginalPosition,
                0.16f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}
