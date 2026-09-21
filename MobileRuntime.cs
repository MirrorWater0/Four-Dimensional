using Godot;

/// <summary>
/// Mobile lifecycle bridge: applies a conservative frame budget, maps the Android/iOS back
/// request to the game's existing Escape flow, and saves the run before suspension.
/// </summary>
public partial class MobileRuntime : Node
{
    private bool _backEventQueued;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        if (!MobilePlatform.IsMobile)
            return;

        Engine.MaxFps = MobilePlatform.TargetFrameRate;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public override void _Notification(int what)
    {
        if (!MobilePlatform.IsMobile)
            return;

        if (what == NotificationWMGoBackRequest)
        {
            QueueBackEvent();
            return;
        }

        if (what == MainLoop.NotificationApplicationPaused)
            SaveActiveRunBeforeSuspend();
    }

    private void QueueBackEvent()
    {
        if (_backEventQueued)
            return;

        _backEventQueued = true;
        Input.ParseInputEvent(
            new InputEventKey
            {
                Keycode = Key.Escape,
                PhysicalKeycode = Key.Escape,
                Pressed = true,
            }
        );
        CallDeferred(MethodName.ReleaseBackEvent);
    }

    private void ReleaseBackEvent()
    {
        Input.ParseInputEvent(
            new InputEventKey
            {
                Keycode = Key.Escape,
                PhysicalKeycode = Key.Escape,
                Pressed = false,
            }
        );
        _backEventQueued = false;
    }

    private static void SaveActiveRunBeforeSuspend()
    {
        if (
            GameInfo.RunFinished
            || GameInfo.PlayerCharacters == null
            || GameInfo.PlayerCharacters.Length == 0
        )
        {
            return;
        }

        try
        {
            SaveSystem.SaveRunCheckpoint(background: false);
        }
        catch (System.Exception exception)
        {
            GD.PrintErr($"移动端挂起存档失败：{exception.Message}");
        }
    }
}
