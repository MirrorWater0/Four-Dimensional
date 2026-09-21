using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class StartScreenShotRunner : Node
{
    private const string OutputPath =
        "C:/godot_project/Four-Dimensional/tmp/start_interface_preview.jpg";
    private const int SettleFrames = 210;

    public override async void _Ready()
    {
        try
        {
            GetTree().Root.Size = new Vector2I(1920, 1080);
            string locale = OS.GetEnvironment("FD_PREVIEW_LOCALE");
            if (!string.IsNullOrWhiteSpace(locale))
                I18n.SetLocale(locale);

            var scene = GD.Load<PackedScene>("res://BeginGame/StartInterface.tscn");
            if (scene == null)
            {
                GD.PrintErr("[StartScreenShotRunner] StartInterface.tscn failed to load.");
                GetTree().Quit(1);
                return;
            }

            AddChild(scene.Instantiate());

            if (GetNodeOrNull<CanvasLayer>("/root/MouseTrail") is { } mouseTrail)
                mouseTrail.Visible = false;

            if (OS.GetEnvironment("FD_FLOW_PREVIEW") == "1") {
                await CaptureFlowPreviewAsync();
                GetTree().Quit(0);
                return;
            }

            if (OS.GetEnvironment("FD_MOTION_PREVIEW") == "1") {
                await CaptureMotionPreviewAsync();
                GetTree().Quit(0);
                return;
            }

            SceneTree tree = GetTree();
            for (int i = 0; i < 30; i++)
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            if (int.TryParse(OS.GetEnvironment("FD_PREVIEW_WIDTH"), out int width) && width > 0
                && int.TryParse(OS.GetEnvironment("FD_PREVIEW_HEIGHT"), out int height) && height > 0) {
                GetWindow().Mode = Window.ModeEnum.Windowed;
                GetTree().Root.Size = new Vector2I(width, height);
            }

            // FD_HOVER_NODE=<scene-relative path> parks the cursor over a control so
            // hover styling shows up in the capture.
            string hoverPath = OS.GetEnvironment("FD_HOVER_NODE");

            // The starfield pauses its subviewport while the app is unfocused; automated
            // windows often never receive focus, so broadcast a synthetic focus-in.
            GetWindow().GrabFocus();
            tree.Root.PropagateNotification((int)MainLoop.NotificationApplicationFocusIn);

            // FD_SETTLE_FRAMES overrides the settle time, e.g. to preview a
            // later moment of the orbiting-camera tour.
            int settleFrames = SettleFrames;
            if (int.TryParse(OS.GetEnvironment("FD_SETTLE_FRAMES"), out int custom)
                && custom > 0)
            {
                settleFrames = custom;
            }

            for (int i = 0; i < settleFrames; i++)
                await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            ValidateMenuLayout();

            if (!string.IsNullOrWhiteSpace(hoverPath))
            {
                if (GetNodeOrNull<Control>(hoverPath) is { } target)
                {
                    // A real cursor warp only registers when the window is
                    // foreground, which automated runs cannot rely on; feeding the
                    // viewport a synthetic motion event drives GUI hover directly.
                    Rect2 rect = target.GetGlobalRect();
                    Vector2 center = rect.Position + rect.Size * 0.5f;
                    PushLocalInput(
                            new InputEventMouseMotion { Position = center, GlobalPosition = center }
                        );
                    for (int i = 0; i < 40; i++)
                        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                }
                else
                {
                    GD.PrintErr($"[StartScreenShotRunner] hover node not found: {hoverPath}");
                }
            }

            string focusPath = OS.GetEnvironment("FD_FOCUS_NODE");
            if (!string.IsNullOrWhiteSpace(focusPath) && GetNodeOrNull<Control>(focusPath) is { } focusTarget) {
                focusTarget.GrabFocus();
                for (int i = 0; i < 30; i++)
                    await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }

            if (OS.GetEnvironment("FD_PREVIEW_PRESS") == "1"
                && !string.IsNullOrWhiteSpace(hoverPath) && GetNodeOrNull<Control>(hoverPath) is { } pressTarget) {
                Vector2 center = pressTarget.GetGlobalRect().GetCenter();
                PushLocalInput(new InputEventMouseButton {
                    Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = true,
                });
                for (int i = 0; i < 20; i++)
                    await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }

            string clickPath = OS.GetEnvironment("FD_CLICK_NODE");
            if (!string.IsNullOrWhiteSpace(clickPath)) {
                var button = GetNode<Button>(clickPath);
                Vector2 center = button.GetGlobalRect().GetCenter();
                PushLocalInput(new InputEventMouseMotion { Position = center, GlobalPosition = center });
                PushLocalInput(new InputEventMouseButton {
                    Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = true,
                });
                PushLocalInput(new InputEventMouseButton {
                    Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = false,
                });
                for (int i = 0; i < 180; i++)
                    await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                string expectedPath = OS.GetEnvironment("FD_EXPECT_NODE");
                if (string.IsNullOrWhiteSpace(expectedPath) || GetNodeOrNull(expectedPath) == null)
                    throw new InvalidOperationException("Click did not open expected view: " + expectedPath);
                GD.Print("[StartScreenShotRunner] Click PASS: " + clickPath);
            }

            string outputPath = OS.GetEnvironment("FD_PREVIEW_OUTPUT");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = OutputPath;
            await CaptureViewportAsync(outputPath);
            GD.Print($"[StartScreenShotRunner] completed: {outputPath}");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[StartScreenShotRunner] failed: {ex}");
            GetTree().Quit(1);
        }
    }

    private async Task CaptureViewportAsync(string path)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = GetViewport().GetTexture().GetImage();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Error error = image.SaveJpg(path, 0.95f);
        if (error != Error.Ok)
            GD.PrintErr($"[StartScreenShotRunner] save failed: {error}");
    }

    private async Task CaptureFlowPreviewAsync() {
        string directory = ProjectSettings.GlobalizePath("res://tmp/start_crystal/flow-motion");
        Directory.CreateDirectory(directory);
        GetWindow().Mode = Window.ModeEnum.Windowed;
        GetTree().Root.Size = new Vector2I(1280, 720);
        for (int i = 0; i < 120; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        for (int frame = 0; frame < 90; frame++) {
            for (int i = 0; i < 6; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = GetViewport().GetTexture().GetImage();
            image.Resize(960, 540, Image.Interpolation.Lanczos);
            if (image.SavePng(Path.Combine(directory, $"{frame:000}.png")) != Error.Ok)
                throw new IOException("Flow capture failed.");
        }
        ValidateMenuLayout();
        GD.Print("[StartScreenShotRunner] Flow preview complete: 90 frames.");
    }

    private async Task CaptureMotionPreviewAsync() {
        string directory = ProjectSettings.GlobalizePath("res://tmp/start_crystal/ui-motion");
        Directory.CreateDirectory(directory);
        GetWindow().Mode = Window.ModeEnum.Windowed;
        GetTree().Root.Size = new Vector2I(1280, 720);
        GetTree().Root.PropagateNotification((int)MainLoop.NotificationApplicationFocusIn);
        Button primary = GetNode<Button>("StartInterface/Layout/Menu/NewGame");
        Button settings = GetNode<Button>("StartInterface/Layout/Masthead/Utilities/Settings");
        Button resume = GetNode<Button>("StartInterface/Layout/Menu/Continue");
        for (int frame = 0; frame < 60; frame++) {
            if (frame == 18 || frame == 28 || frame == 38 || frame == 46) {
                Button target = frame == 28 ? settings : frame == 38 ? resume : primary;
                Vector2 center = target.GetGlobalRect().GetCenter();
                PushLocalInput(new InputEventMouseMotion { Position = center, GlobalPosition = center });
            }
            if (frame == 54) {
                Vector2 center = primary.GetGlobalRect().GetCenter();
                PushLocalInput(new InputEventMouseButton {
                    Position = center, GlobalPosition = center, ButtonIndex = MouseButton.Left, Pressed = true,
                });
            }
            for (int i = 0; i < 6; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (frame == 26 && primary.GetNode<ColorRect>("AnimatedUnderline").AnchorRight < 0.95f)
                throw new InvalidOperationException("Primary underline did not expand.");
            if (frame == 36 && (settings.GetNode<ColorRect>("AnimatedUnderline").AnchorRight < 0.95f
                || primary.GetNode<ColorRect>("AnimatedUnderline").AnchorRight > 0.05f))
                throw new InvalidOperationException("Hover transfer did not settle.");
            if (frame == 44 && resume.Disabled && resume.GetNode<ColorRect>("AnimatedUnderline").Color.A != 0f)
                throw new InvalidOperationException("Disabled action showed hover feedback.");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Image image = GetViewport().GetTexture().GetImage();
            image.Resize(960, 540, Image.Interpolation.Lanczos);
            if (image.SavePng(Path.Combine(directory, $"{frame:000}.png")) != Error.Ok)
                throw new IOException("Motion capture failed.");
        }
        ValidateMenuLayout();
        GD.Print("[StartScreenShotRunner] Motion PASS: entrance, hover transfer, disabled state, press; 60 frames.");
    }

    private void PushLocalInput(InputEvent inputEvent) {
        GetViewport().PushInput(inputEvent, inLocalCoords: true);
    }

    private void ValidateMenuLayout() {
        string[] paths = {
            "StartInterface/Layout/Menu/NewGame", "StartInterface/Layout/Menu/Continue",
            "StartInterface/Layout/Masthead/Utilities/Statistics", "StartInterface/Layout/Masthead/Utilities/Encyclopedia",
            "StartInterface/Layout/Masthead/Utilities/Settings", "StartInterface/Layout/Masthead/Utilities/Exit",
        };
        Rect2 bounds = GetNode<Control>("StartInterface/Layout").GetGlobalRect();
        for (int i = 0; i < paths.Length; i++) {
            Button button = GetNode<Button>(paths[i]);
            Rect2 rect = button.GetGlobalRect();
            if (!bounds.Encloses(rect))
                throw new InvalidOperationException("Button outside layout: " + paths[i]);
            for (int j = 0; j < i; j++) {
                if (rect.Intersects(GetNode<Control>(paths[j]).GetGlobalRect()))
                    throw new InvalidOperationException("Overlapping menu buttons: " + paths[i]);
            }
            float textWidth = button.GetThemeFont("font").GetStringSize(button.Text, fontSize: button.GetThemeFontSize("font_size")).X;
            if (textWidth > rect.Size.X - button.GetThemeStylebox("normal").GetMinimumSize().X + 1f)
                throw new InvalidOperationException("Clipped button text: " + paths[i]);
        }
        GD.Print("[StartScreenShotRunner] Layout PASS: six menu entries.");
    }
}
