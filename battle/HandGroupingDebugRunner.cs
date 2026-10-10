using System;
using Godot;

// Settings and live hand-animation regression; preserves the user's settings file byte for byte.
public partial class HandGroupingDebugRunner : Node
{
    public override async void _Ready()
    {
        string path = ProjectSettings.GlobalizePath("user://settings.cfg");
        byte[] original = System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;
        UserSettings.EnsureLoaded();
        bool previous = UserSettings.GroupHandCardsByCharacter;
        bool ok = false;
        Menu menu = null;
        try
        {
            GD.Print("[HandGroupingDebugRunner] start settings smoke");
            SetGrouping(true);
            menu = GD.Load<PackedScene>("res://Menu/Menu.tscn").Instantiate<Menu>();
            AddChild(menu);
            var checkbox = menu.FindChild("GroupHandCardsByCharacterCheckBox", true, false) as CheckBox;
            Check(checkbox != null && checkbox.ButtonPressed, "checkbox initial value");
            checkbox.ButtonPressed = false;
            checkbox.EmitSignal(BaseButton.SignalName.Pressed);
            Check(!UserSettings.GroupHandCardsByCharacter, "toggle off");
            UserSettings.Load();
            Check(!UserSettings.GroupHandCardsByCharacter, "persist off");
            checkbox.ButtonPressed = true;
            checkbox.EmitSignal(BaseButton.SignalName.Pressed);
            SetGrouping(false);
            UserSettings.Load();
            Check(UserSettings.GroupHandCardsByCharacter, "persist on");
            GD.Print("[HandGroupingDebugRunner] PASS settings scene, toggle, save and reload");
            menu.Free();
            menu = null;
            await RunHandAnimationRegressionAsync();
            ok = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[HandGroupingDebugRunner] {ex}");
        }
        finally
        {
            if (original != null)
                System.IO.File.WriteAllBytes(path, original);
            else if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
            SetGrouping(previous);
            menu?.Free();
            GD.Print($"[HandGroupingDebugRunner] {(ok ? "PASS" : "FAIL")}");
            GetTree().Quit(ok ? 0 : 1);
        }
    }

    private static void SetGrouping(bool value) => typeof(UserSettings)
        .GetProperty(nameof(UserSettings.GroupHandCardsByCharacter)).SetValue(null, value);

    private static void Check(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException(name);
    }
}
