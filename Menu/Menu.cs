using Godot;

public partial class Menu : Control
{
    private Button SaveQuitButton => field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/SaveQuitButton");
    private Button AbandonGameButton => field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/AbandonGameButton");
    private Button SettingsButton => field ??= GetNodeOrNull<Button>("CenterPanel/Margin/VBox/Buttons/SettingsButton");

    public override void _Ready()
    {
        Visible = false;

        if (SaveQuitButton != null)
            SaveQuitButton.Pressed += OnSaveQuitPressed;

        if (AbandonGameButton != null)
            AbandonGameButton.Pressed += OnAbandonGamePressed;

        if (SettingsButton != null)
            SettingsButton.Pressed += OnSettingsPressed;
    }

    public void Toggle()
    {
        if (Visible)
        {
            Close();
            return;
        }

        Open();
    }

    public void Open()
    {
        Visible = true;
    }

    public void Close()
    {
        Visible = false;
    }

    private void OnSaveQuitPressed()
    {
        SaveSystem.SaveAll();
        GetTree().ChangeSceneToFile("res://BeginGame/StartInterface.tscn");
    }

    private void OnAbandonGamePressed()
    {
        GetTree().ChangeSceneToFile("res://BeginGame/StartInterface.tscn");
    }

    private void OnSettingsPressed()
    {
        GD.Print("Settings menu is not implemented yet.");
    }
}
