using System;
using System.Collections.Generic;
using Godot;

public partial class ExitButton : Button
{
    public delegate void ExitButtonPressed();
    public ObservableList<Action> PressedActions = new (); // List of actions to be executed when the button is pressed>

    public override void _Ready()
    {
        Visible = false;
        Position = new Vector2(-10, 50);
        MouseEntered += () =>
        {
            CreateTween().TweenProperty(this, "position", new Vector2(40, 50), 0.2f);
        };
        MouseExited += () =>
        {
            CreateTween().TweenProperty(this, "position", new Vector2(-10, 50), 0.2f);
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
