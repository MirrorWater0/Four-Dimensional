using System;
using Godot;

/// <summary>
/// Shared mobile/touch decisions. Pass --mobile-preview as a user argument to exercise the
/// mobile path from a desktop editor build.
/// </summary>
public static class MobilePlatform
{
    public const float MinimumTouchTarget = 88f;
    public const int TargetFrameRate = 60;

    private static bool? _isMobile;

    public static bool IsMobile => _isMobile ??= DetectMobile();

    public static bool IsTouchPrimary => IsMobile;

    public static bool IsCancelPress(InputEvent inputEvent)
    {
        return inputEvent is InputEventKey
            {
                Pressed: true,
                Echo: false,
                Keycode: Key.Escape,
            }
            || inputEvent is InputEventMouseButton
            {
                Pressed: true,
                ButtonIndex: MouseButton.Right,
            };
    }

    public static bool TryGetPointerPosition(InputEvent inputEvent, out Vector2 position)
    {
        switch (inputEvent)
        {
            case InputEventScreenTouch touch:
                position = touch.Position;
                return true;
            case InputEventScreenDrag drag:
                position = drag.Position;
                return true;
            case InputEventMouseButton mouseButton:
                position = mouseButton.Position;
                return true;
            case InputEventMouseMotion mouseMotion:
                position = mouseMotion.Position;
                return true;
            default:
                position = Vector2.Zero;
                return false;
        }
    }

    public static bool TryGetTouchScrollDelta(
        InputEvent inputEvent,
        out float delta,
        float multiplier = 1f
    )
    {
        if (inputEvent is InputEventScreenDrag screenDrag)
        {
            delta = -screenDrag.Relative.Y * multiplier;
            return Mathf.Abs(delta) > 0.01f;
        }

        delta = 0f;
        return false;
    }

    /// <summary>
    /// Enlarges a fixed-position control's hit rectangle while keeping its visual center.
    /// Use only for controls that are not managed by a Container.
    /// </summary>
    public static void EnsureMinimumTouchTarget(
        Control control,
        float minimumWidth = MinimumTouchTarget,
        float minimumHeight = MinimumTouchTarget
    )
    {
        if (!IsMobile || control == null || !GodotObject.IsInstanceValid(control))
            return;

        Vector2 currentSize = control.Size;
        Vector2 targetSize = new(
            Mathf.Max(currentSize.X, minimumWidth),
            Mathf.Max(currentSize.Y, minimumHeight)
        );
        if (currentSize.DistanceSquaredTo(targetSize) <= 0.01f)
            return;

        Vector2 center = control.Position + currentSize * 0.5f;
        control.CustomMinimumSize = new Vector2(
            Mathf.Max(control.CustomMinimumSize.X, targetSize.X),
            Mathf.Max(control.CustomMinimumSize.Y, targetSize.Y)
        );
        control.Size = targetSize;
        control.Position = center - targetSize * 0.5f;
        control.FocusMode = Control.FocusModeEnum.None;
    }

    /// <summary>Raises the minimum size for a control managed by a Container.</summary>
    public static void EnsureContainerTouchTarget(
        Control control,
        float minimumWidth = 0f,
        float minimumHeight = MinimumTouchTarget
    )
    {
        if (!IsMobile || control == null || !GodotObject.IsInstanceValid(control))
            return;

        control.CustomMinimumSize = new Vector2(
            Mathf.Max(control.CustomMinimumSize.X, minimumWidth),
            Mathf.Max(control.CustomMinimumSize.Y, minimumHeight)
        );
        control.FocusMode = Control.FocusModeEnum.None;
    }

    private static bool DetectMobile()
    {
        if (
            OS.HasFeature("mobile")
            || OS.HasFeature("android")
            || OS.HasFeature("ios")
        )
        {
            return true;
        }

        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (string.Equals(argument, "--mobile-preview", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
