using System;
using System.Collections.Generic;
using Godot;

public partial class ExitButton : Button
{
    public delegate void ExitButtonPressed();
    public ObservableList<Action> PressedActions = new (); // List of actions to be executed when the button is pressed>
    public Vector2 OriginalPosition = new Vector2(-10, 130);
    public override void _Ready()
    {
        Visible = false;
        Position = OriginalPosition;
        MouseEntered += () =>
        {
            CreateTween().TweenProperty(this, "position", OriginalPosition + 50 * Vector2.Right, 0.2f);
        };
        MouseExited += () =>
        {
            CreateTween().TweenProperty(this, "position", OriginalPosition, 0.2f);
        };

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
}
