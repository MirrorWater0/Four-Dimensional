using System;
using System.Collections.Generic;
using Godot;

public partial class ExitButton : Button
{
    public delegate void ExitButtonPressed();
    public ObservableList<Action> PressedActions = new (); // List of actions to be executed when the button is pressed>
    public Vector2 OriginalPosition = new Vector2(-10, 130);

    // When true (legacy), _Ready snaps Position to OriginalPosition.
    // Set false for scenes that position the button via anchors (e.g. GameStatistics).
    [Export]
    public bool SnapPositionOnReady = true;

    // Offset applied on hover. Legacy left-edge tab uses (50, 0); anchored buttons use (0, 0).
    [Export]
    public Vector2 HoverSlideOffset = new Vector2(50f, 0f);

    private Tween _hoverTween;
    private Vector2 _hoverBasePosition;

    public override void _Ready()
    {
        Visible = false;
        if (SnapPositionOnReady)
            Position = OriginalPosition;
        _hoverBasePosition = Position;
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
                hovered ? _hoverBasePosition + HoverSlideOffset : _hoverBasePosition,
                0.16f
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}
