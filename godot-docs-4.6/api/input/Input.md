# Input

## Meta

- Name: Input
- Source: Input.xml
- Inherits: Object
- Inheritance Chain: Input -> Object

## Brief Description

A singleton for handling inputs.

## Description

The Input singleton handles key presses, mouse buttons and movement, gamepads, and input actions. Actions and their events can be set in the **Input Map** tab in **Project > Project Settings**, or with the InputMap class. **Note:** Input's methods reflect the global input state and are not affected by Control.accept_event() or Viewport.set_input_as_handled(), as those methods only deal with the way input is propagated in the SceneTree.

## Quick Reference

```
[methods]
action_press(action: StringName, strength: float = 1.0) -> void
action_release(action: StringName) -> void
add_joy_mapping(mapping: String, update_existing: bool = false) -> void
flush_buffered_events() -> void
get_accelerometer() -> Vector3 [const]
get_action_raw_strength(action: StringName, exact_match: bool = false) -> float [const]
get_action_strength(action: StringName, exact_match: bool = false) -> float [const]
get_axis(negative_action: StringName, positive_action: StringName) -> float [const]
get_connected_joypads() -> int[]
get_current_cursor_shape() -> int (Input.CursorShape) [const]
get_gravity() -> Vector3 [const]
get_gyroscope() -> Vector3 [const]
get_joy_axis(device: int, axis: int (JoyAxis)) -> float [const]
get_joy_guid(device: int) -> String [const]
get_joy_info(device: int) -> Dictionary [const]
get_joy_name(device: int) -> String
get_joy_vibration_duration(device: int) -> float
get_joy_vibration_strength(device: int) -> Vector2
get_last_mouse_screen_velocity() -> Vector2
get_last_mouse_velocity() -> Vector2
get_magnetometer() -> Vector3 [const]
get_mouse_button_mask() -> int (MouseButtonMask) [const]
get_vector(negative_x: StringName, positive_x: StringName, negative_y: StringName, positive_y: StringName, deadzone: float = -1.0) -> Vector2 [const]
has_joy_light(device: int) -> bool [const]
is_action_just_pressed(action: StringName, exact_match: bool = false) -> bool [const]
is_action_just_pressed_by_event(action: StringName, event: InputEvent, exact_match: bool = false) -> bool [const]
is_action_just_released(action: StringName, exact_match: bool = false) -> bool [const]
is_action_just_released_by_event(action: StringName, event: InputEvent, exact_match: bool = false) -> bool [const]
is_action_pressed(action: StringName, exact_match: bool = false) -> bool [const]
is_anything_pressed() -> bool [const]
is_joy_button_pressed(device: int, button: int (JoyButton)) -> bool [const]
is_joy_known(device: int) -> bool
is_key_label_pressed(keycode: int (Key)) -> bool [const]
is_key_pressed(keycode: int (Key)) -> bool [const]
is_mouse_button_pressed(button: int (MouseButton)) -> bool [const]
is_physical_key_pressed(keycode: int (Key)) -> bool [const]
parse_input_event(event: InputEvent) -> void
remove_joy_mapping(guid: String) -> void
set_accelerometer(value: Vector3) -> void
set_custom_mouse_cursor(image: Resource, shape: int (Input.CursorShape) = 0, hotspot: Vector2 = Vector2(0, 0)) -> void
set_default_cursor_shape(shape: int (Input.CursorShape) = 0) -> void
set_gravity(value: Vector3) -> void
set_gyroscope(value: Vector3) -> void
set_joy_light(device: int, color: Color) -> void
set_magnetometer(value: Vector3) -> void
should_ignore_device(vendor_id: int, product_id: int) -> bool [const]
start_joy_vibration(device: int, weak_magnitude: float, strong_magnitude: float, duration: float = 0) -> void
stop_joy_vibration(device: int) -> void
vibrate_handheld(duration_ms: int = 500, amplitude: float = -1.0) -> void
warp_mouse(position: Vector2) -> void

[properties]
emulate_mouse_from_touch: bool
emulate_touch_from_mouse: bool
mouse_mode: int (Input.MouseMode)
use_accumulated_input: bool
```

## Tutorials

- [Inputs documentation index]($DOCS_URL/tutorials/inputs/index.html)
- [2D Dodge The Creeps Demo](https://godotengine.org/asset-library/asset/2712)
- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)

## Methods

- action_press(action: StringName, strength: float = 1.0) -> void
  This will simulate pressing the specified action. The strength can be used for non-boolean actions, it's ranged between 0 and 1 representing the intensity of the given action. **Note:** This method will not cause any Node._input() calls. It is intended to be used with is_action_pressed() and is_action_just_pressed(). If you want to simulate _input, use parse_input_event() instead.

- action_release(action: StringName) -> void
  If the specified action is already pressed, this will release it.

- add_joy_mapping(mapping: String, update_existing: bool = false) -> void
  Adds a new mapping entry (in SDL2 format) to the mapping database. Optionally update already connected devices.

- flush_buffered_events() -> void
  Sends all input events which are in the current buffer to the game loop. These events may have been buffered as a result of accumulated input (use_accumulated_input) or agile input flushing (ProjectSettings.input_devices/buffering/agile_event_flushing). The engine will already do this itself at key execution points (at least once per frame). However, this can be useful in advanced cases where you want precise control over the timing of event handling.

- get_accelerometer() -> Vector3 [const]
  Returns the acceleration in m/s² of the device's accelerometer sensor, if the device has one. Otherwise, the method returns Vector3.ZERO. Note this method returns an empty Vector3 when running from the editor even when your device has an accelerometer. You must export your project to a supported device to read values from the accelerometer. **Note:** This method only works on Android and iOS. On other platforms, it always returns Vector3.ZERO. **Note:** For Android, ProjectSettings.input_devices/sensors/enable_accelerometer must be enabled.

- get_action_raw_strength(action: StringName, exact_match: bool = false) -> float [const]
  Returns a value between 0 and 1 representing the raw intensity of the given action, ignoring the action's deadzone. In most cases, you should use get_action_strength() instead. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events.

- get_action_strength(action: StringName, exact_match: bool = false) -> float [const]
  Returns a value between 0 and 1 representing the intensity of the given action. In a joypad, for example, the further away the axis (analog sticks or L2, R2 triggers) is from the dead zone, the closer the value will be to 1. If the action is mapped to a control that has no axis such as the keyboard, the value returned will be 0 or 1. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events.

- get_axis(negative_action: StringName, positive_action: StringName) -> float [const]
  Get axis input by specifying two actions, one negative and one positive. This is a shorthand for writing Input.get_action_strength("positive_action") - Input.get_action_strength("negative_action").

- get_connected_joypads() -> int[]
  Returns an Array containing the device IDs of all currently connected joypads.

- get_current_cursor_shape() -> int (Input.CursorShape) [const]
  Returns the currently assigned cursor shape.

- get_gravity() -> Vector3 [const]
  Returns the gravity in m/s² of the device's accelerometer sensor, if the device has one. Otherwise, the method returns Vector3.ZERO. **Note:** This method only works on Android and iOS. On other platforms, it always returns Vector3.ZERO. **Note:** For Android, ProjectSettings.input_devices/sensors/enable_gravity must be enabled.

- get_gyroscope() -> Vector3 [const]
  Returns the rotation rate in rad/s around a device's X, Y, and Z axes of the gyroscope sensor, if the device has one. Otherwise, the method returns Vector3.ZERO. **Note:** This method only works on Android and iOS. On other platforms, it always returns Vector3.ZERO. **Note:** For Android, ProjectSettings.input_devices/sensors/enable_gyroscope must be enabled.

- get_joy_axis(device: int, axis: int (JoyAxis)) -> float [const]
  Returns the current value of the joypad axis at index axis.

- get_joy_guid(device: int) -> String [const]
  Returns an SDL-compatible device GUID on platforms that use gamepad remapping, e.g. 030000004c050000c405000000010000. Returns an empty string if it cannot be found. Godot uses SDL's internal mappings, supplemented by community-contributed mappings, to determine gamepad names and mappings based on this GUID. On Windows, all XInput joypad GUIDs will be overridden by Godot to __XINPUT_DEVICE__, because their mappings are the same.

- get_joy_info(device: int) -> Dictionary [const]
  Returns a dictionary with extra platform-specific information about the device, e.g. the raw gamepad name from the OS or the Steam Input index. On Windows, Linux, and macOS, the dictionary contains the following fields: raw_name: The name of the controller as it came from the OS, before getting renamed by the controller database. vendor_id: The USB vendor ID of the device. product_id: The USB product ID of the device. steam_input_index: The Steam Input gamepad index, if the device is not a Steam Input device this key won't be present. On Windows, the dictionary can have an additional field: xinput_index: The index of the controller in the XInput system. This key won't be present for devices not handled by XInput. **Note:** The returned dictionary is always empty on Android, iOS, visionOS, and Web.

- get_joy_name(device: int) -> String
  Returns the name of the joypad at the specified device index, e.g. PS4 Controller. Godot uses the [SDL2 game controller database](https://github.com/gabomdq/SDL_GameControllerDB) to determine gamepad names.

- get_joy_vibration_duration(device: int) -> float
  Returns the duration of the current vibration effect in seconds.

- get_joy_vibration_strength(device: int) -> Vector2
  Returns the strength of the joypad vibration: x is the strength of the weak motor, and y is the strength of the strong motor.

- get_last_mouse_screen_velocity() -> Vector2
  Returns the last mouse velocity in screen coordinates. To provide a precise and jitter-free velocity, mouse velocity is only calculated every 0.1s. Therefore, mouse velocity will lag mouse movements.

- get_last_mouse_velocity() -> Vector2
  Returns the last mouse velocity. To provide a precise and jitter-free velocity, mouse velocity is only calculated every 0.1s. Therefore, mouse velocity will lag mouse movements.

- get_magnetometer() -> Vector3 [const]
  Returns the magnetic field strength in micro-Tesla for all axes of the device's magnetometer sensor, if the device has one. Otherwise, the method returns Vector3.ZERO. **Note:** This method only works on Android and iOS. On other platforms, it always returns Vector3.ZERO. **Note:** For Android, ProjectSettings.input_devices/sensors/enable_magnetometer must be enabled.

- get_mouse_button_mask() -> int (MouseButtonMask) [const]
  Returns mouse buttons as a bitmask. If multiple mouse buttons are pressed at the same time, the bits are added together. Equivalent to DisplayServer.mouse_get_button_state().

- get_vector(negative_x: StringName, positive_x: StringName, negative_y: StringName, positive_y: StringName, deadzone: float = -1.0) -> Vector2 [const]
  Gets an input vector by specifying four actions for the positive and negative X and Y axes. This method is useful when getting vector input, such as from a joystick, directional pad, arrows, or WASD. The vector has its length limited to 1 and has a circular deadzone, which is useful for using vector input as movement. By default, the deadzone is automatically calculated from the average of the action deadzones. However, you can override the deadzone to be whatever you want (on the range of 0 to 1).

- has_joy_light(device: int) -> bool [const]
  Returns true if the joypad has an LED light that can change colors and/or brightness. See also set_joy_light(). **Note:** This feature is only supported on Windows, Linux, and macOS.

- is_action_just_pressed(action: StringName, exact_match: bool = false) -> bool [const]
  Returns true when the user has *started* pressing the action event in the current frame or physics tick. It will only return true on the frame or tick that the user pressed down the button. This is useful for code that needs to run only once when an action is pressed, instead of every frame while it's pressed. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events. **Note:** Returning true does not imply that the action is *still* pressed. An action can be pressed and released again rapidly, and true will still be returned so as not to miss input. **Note:** Due to keyboard ghosting, is_action_just_pressed() may return false even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information. **Note:** During input handling (e.g. Node._input()), use InputEvent.is_action_pressed() instead to query the action state of the current event. See also is_action_just_pressed_by_event().

- is_action_just_pressed_by_event(action: StringName, event: InputEvent, exact_match: bool = false) -> bool [const]
  Returns true when the user has *started* pressing the action event in the current frame or physics tick, and the first event that triggered action press in the current frame/physics tick was event. It will only return true on the frame or tick that the user pressed down the button. This is useful for code that needs to run only once when an action is pressed, and the action is processed during input handling (e.g. Node._input()). If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events. **Note:** Returning true does not imply that the action is *still* pressed. An action can be pressed and released again rapidly, and true will still be returned so as not to miss input. **Note:** Due to keyboard ghosting, is_action_just_pressed() may return false even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

- is_action_just_released(action: StringName, exact_match: bool = false) -> bool [const]
  Returns true when the user *stops* pressing the action event in the current frame or physics tick. It will only return true on the frame or tick that the user releases the button. **Note:** Returning true does not imply that the action is *still* not pressed. An action can be released and pressed again rapidly, and true will still be returned so as not to miss input. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events. **Note:** During input handling (e.g. Node._input()), use InputEvent.is_action_released() instead to query the action state of the current event. See also is_action_just_released_by_event().

- is_action_just_released_by_event(action: StringName, event: InputEvent, exact_match: bool = false) -> bool [const]
  Returns true when the user *stops* pressing the action event in the current frame or physics tick, and the first event that triggered action release in the current frame/physics tick was event. It will only return true on the frame or tick that the user releases the button. This is useful when an action is processed during input handling (e.g. Node._input()). **Note:** Returning true does not imply that the action is *still* not pressed. An action can be released and pressed again rapidly, and true will still be returned so as not to miss input. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events.

- is_action_pressed(action: StringName, exact_match: bool = false) -> bool [const]
  Returns true if you are pressing the action event. If exact_match is false, it ignores additional input modifiers for InputEventKey and InputEventMouseButton events, and the direction for InputEventJoypadMotion events. **Note:** Due to keyboard ghosting, is_action_pressed() may return false even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

- is_anything_pressed() -> bool [const]
  Returns true if any action, key, joypad button, or mouse button is being pressed. This will also return true if any action is simulated via code by calling action_press().

- is_joy_button_pressed(device: int, button: int (JoyButton)) -> bool [const]
  Returns true if you are pressing the joypad button at index button.

- is_joy_known(device: int) -> bool
  Returns true if the system knows the specified device. This means that it sets all button and axis indices. Unknown joypads are not expected to match these constants, but you can still retrieve events from them.

- is_key_label_pressed(keycode: int (Key)) -> bool [const]
  Returns true if you are pressing the key with the keycode printed on it. You can pass a Key constant or any Unicode character code.

- is_key_pressed(keycode: int (Key)) -> bool [const]
  Returns true if you are pressing the Latin key in the current keyboard layout. You can pass a Key constant. is_key_pressed() is only recommended over is_physical_key_pressed() in non-game applications. This ensures that shortcut keys behave as expected depending on the user's keyboard layout, as keyboard shortcuts are generally dependent on the keyboard layout in non-game applications. If in doubt, use is_physical_key_pressed(). **Note:** Due to keyboard ghosting, is_key_pressed() may return false even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

- is_mouse_button_pressed(button: int (MouseButton)) -> bool [const]
  Returns true if you are pressing the mouse button specified with MouseButton.

- is_physical_key_pressed(keycode: int (Key)) -> bool [const]
  Returns true if you are pressing the key in the physical location on the 101/102-key US QWERTY keyboard. You can pass a Key constant. is_physical_key_pressed() is recommended over is_key_pressed() for in-game actions, as it will make W/A/S/D layouts work regardless of the user's keyboard layout. is_physical_key_pressed() will also ensure that the top row number keys work on any keyboard layout. If in doubt, use is_physical_key_pressed(). **Note:** Due to keyboard ghosting, is_physical_key_pressed() may return false even if one of the action's keys is pressed. See [Input examples]($DOCS_URL/tutorials/inputs/input_examples.html#keyboard-events) in the documentation for more information.

- parse_input_event(event: InputEvent) -> void
  Feeds an InputEvent to the game. Can be used to artificially trigger input events from code. Also generates Node._input() calls.


```
  var cancel_event = InputEventAction.new()
  cancel_event.action = "ui_cancel"
  cancel_event.pressed = true
  Input.parse_input_event(cancel_event)

```

```
  var cancelEvent = new InputEventAction();
  cancelEvent.Action = "ui_cancel";
  cancelEvent.Pressed = true;
  Input.ParseInputEvent(cancelEvent);

```
  **Note:** Calling this function has no influence on the operating system. So for example sending an InputEventMouseMotion will not move the OS mouse cursor to the specified position (use warp_mouse() instead) and sending Alt/Cmd + Tab as InputEventKey won't toggle between active windows.

- remove_joy_mapping(guid: String) -> void
  Removes all mappings from the internal database that match the given GUID. All currently connected joypads that use this GUID will become unmapped. On Android, Godot will map to an internal fallback mapping.

- set_accelerometer(value: Vector3) -> void
  Sets the acceleration value of the accelerometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC. **Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

- set_custom_mouse_cursor(image: Resource, shape: int (Input.CursorShape) = 0, hotspot: Vector2 = Vector2(0, 0)) -> void
  Sets a custom mouse cursor image, which is only visible inside the game window, for the given mouse shape. The hotspot can also be specified. Passing null to the image parameter resets to the system cursor. image can be either Texture2D or Image and its size must be lower than or equal to 256×256. To avoid rendering issues, sizes lower than or equal to 128×128 are recommended. hotspot must be within image's size. **Note:** AnimatedTextures aren't supported as custom mouse cursors. If using an AnimatedTexture, only the first frame will be displayed. **Note:** The **Lossless**, **Lossy** or **Uncompressed** compression modes are recommended. The **Video RAM** compression mode can be used, but it will be decompressed on the CPU, which means loading times are slowed down and no memory is saved compared to lossless modes. **Note:** On the web platform, the maximum allowed cursor image size is 128×128. Cursor images larger than 32×32 will also only be displayed if the mouse cursor image is entirely located within the page for [security reasons](https://chromestatus.com/feature/5825971391299584).

- set_default_cursor_shape(shape: int (Input.CursorShape) = 0) -> void
  Sets the default cursor shape to be used in the viewport instead of CURSOR_ARROW. **Note:** If you want to change the default cursor shape for Control's nodes, use Control.mouse_default_cursor_shape instead. **Note:** This method generates an InputEventMouseMotion to update cursor immediately.

- set_gravity(value: Vector3) -> void
  Sets the gravity value of the accelerometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC. **Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

- set_gyroscope(value: Vector3) -> void
  Sets the value of the rotation rate of the gyroscope sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC. **Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

- set_joy_light(device: int, color: Color) -> void
  Sets the joypad's LED light, if available, to the specified color. See also has_joy_light(). **Note:** There is no way to get the color of the light from a joypad. If you need to know the assigned color, store it separately. **Note:** This feature is only supported on Windows, Linux, and macOS.

- set_magnetometer(value: Vector3) -> void
  Sets the value of the magnetic field of the magnetometer sensor. Can be used for debugging on devices without a hardware sensor, for example in an editor on a PC. **Note:** This value can be immediately overwritten by the hardware sensor value on Android and iOS.

- should_ignore_device(vendor_id: int, product_id: int) -> bool [const]
  Queries whether an input device should be ignored or not. Devices can be ignored by setting the environment variable SDL_GAMECONTROLLER_IGNORE_DEVICES. Read the [SDL documentation](https://wiki.libsdl.org/SDL2) for more information. **Note:** Some 3rd party tools can contribute to the list of ignored devices. For example, *SteamInput* creates virtual devices from physical devices for remapping purposes. To avoid handling the same input device twice, the original device is added to the ignore list.

- start_joy_vibration(device: int, weak_magnitude: float, strong_magnitude: float, duration: float = 0) -> void
  Starts to vibrate the joypad. Joypads usually come with two rumble motors, a strong and a weak one. weak_magnitude is the strength of the weak motor (between 0 and 1) and strong_magnitude is the strength of the strong motor (between 0 and 1). duration is the duration of the effect in seconds (a duration of 0 will try to play the vibration indefinitely). The vibration can be stopped early by calling stop_joy_vibration(). **Note:** Not every hardware is compatible with long effect durations; it is recommended to restart an effect if it has to be played for more than a few seconds. **Note:** For macOS, vibration is only supported in macOS 11 and later.

- stop_joy_vibration(device: int) -> void
  Stops the vibration of the joypad started with start_joy_vibration().

- vibrate_handheld(duration_ms: int = 500, amplitude: float = -1.0) -> void
  Vibrate the handheld device for the specified duration in milliseconds. amplitude is the strength of the vibration, as a value between 0.0 and 1.0. If set to -1.0, the default vibration strength of the device is used. **Note:** This method is implemented on Android, iOS, and Web. It has no effect on other platforms. **Note:** For Android, vibrate_handheld() requires enabling the VIBRATE permission in the export preset. Otherwise, vibrate_handheld() will have no effect. **Note:** For iOS, specifying the duration is only supported in iOS 13 and later. **Note:** For Web, the amplitude cannot be changed. **Note:** Some web browsers such as Safari and Firefox for Android do not support vibrate_handheld().

- warp_mouse(position: Vector2) -> void
  Sets the mouse position to the specified vector, provided in pixels and relative to an origin at the upper left corner of the currently focused Window Manager game window. Mouse position is clipped to the limits of the screen resolution, or to the limits of the game window if MouseMode is set to MOUSE_MODE_CONFINED or MOUSE_MODE_CONFINED_HIDDEN. **Note:** warp_mouse() is only supported on Windows, macOS and Linux. It has no effect on Android, iOS and Web.

## Properties

- emulate_mouse_from_touch: bool [set set_emulate_mouse_from_touch; get is_emulating_mouse_from_touch]
  If true, sends mouse input events when tapping or swiping on the touchscreen. See also ProjectSettings.input_devices/pointing/emulate_mouse_from_touch.

- emulate_touch_from_mouse: bool [set set_emulate_touch_from_mouse; get is_emulating_touch_from_mouse]
  If true, sends touch input events when clicking or dragging the mouse. See also ProjectSettings.input_devices/pointing/emulate_touch_from_mouse.

- mouse_mode: int (Input.MouseMode) [set set_mouse_mode; get get_mouse_mode]
  Controls the mouse mode.

- use_accumulated_input: bool [set set_use_accumulated_input; get is_using_accumulated_input]
  If true, similar input events sent by the operating system are accumulated. When input accumulation is enabled, all input events generated during a frame will be merged and emitted when the frame is done rendering. Therefore, this limits the number of input method calls per second to the rendering FPS. Input accumulation can be disabled to get slightly more precise/reactive input at the cost of increased CPU usage. In applications where drawing freehand lines is required, input accumulation should generally be disabled while the user is drawing the line to get results that closely follow the actual input. **Note:** Input accumulation is *enabled* by default.

## Signals

- joy_connection_changed(device: int, connected: bool)
  Emitted when a joypad device has been connected or disconnected.

## Constants

### Enum MouseMode

- MOUSE_MODE_VISIBLE = 0
  Makes the mouse cursor visible if it is hidden.

- MOUSE_MODE_HIDDEN = 1
  Makes the mouse cursor hidden if it is visible.

- MOUSE_MODE_CAPTURED = 2
  Captures the mouse. The mouse will be hidden and its position locked at the center of the window manager's window. **Note:** If you want to process the mouse's movement in this mode, you need to use InputEventMouseMotion.relative.

- MOUSE_MODE_CONFINED = 3
  Confines the mouse cursor to the game window, and make it visible.

- MOUSE_MODE_CONFINED_HIDDEN = 4
  Confines the mouse cursor to the game window, and make it hidden.

- MOUSE_MODE_MAX = 5
  Max value of the MouseMode.

### Enum CursorShape

- CURSOR_ARROW = 0
  Arrow cursor. Standard, default pointing cursor.

- CURSOR_IBEAM = 1
  I-beam cursor. Usually used to show where the text cursor will appear when the mouse is clicked.

- CURSOR_POINTING_HAND = 2
  Pointing hand cursor. Usually used to indicate the pointer is over a link or other interactable item.

- CURSOR_CROSS = 3
  Cross cursor. Typically appears over regions in which a drawing operation can be performed or for selections.

- CURSOR_WAIT = 4
  Wait cursor. Indicates that the application is busy performing an operation, and that it cannot be used during the operation (e.g. something is blocking its main thread).

- CURSOR_BUSY = 5
  Busy cursor. Indicates that the application is busy performing an operation, and that it is still usable during the operation.

- CURSOR_DRAG = 6
  Drag cursor. Usually displayed when dragging something. **Note:** Windows lacks a dragging cursor, so CURSOR_DRAG is the same as CURSOR_MOVE for this platform.

- CURSOR_CAN_DROP = 7
  Can drop cursor. Usually displayed when dragging something to indicate that it can be dropped at the current position.

- CURSOR_FORBIDDEN = 8
  Forbidden cursor. Indicates that the current action is forbidden (for example, when dragging something) or that the control at a position is disabled.

- CURSOR_VSIZE = 9
  Vertical resize mouse cursor. A double-headed vertical arrow. It tells the user they can resize the window or the panel vertically.

- CURSOR_HSIZE = 10
  Horizontal resize mouse cursor. A double-headed horizontal arrow. It tells the user they can resize the window or the panel horizontally.

- CURSOR_BDIAGSIZE = 11
  Window resize mouse cursor. The cursor is a double-headed arrow that goes from the bottom left to the top right. It tells the user they can resize the window or the panel both horizontally and vertically.

- CURSOR_FDIAGSIZE = 12
  Window resize mouse cursor. The cursor is a double-headed arrow that goes from the top left to the bottom right, the opposite of CURSOR_BDIAGSIZE. It tells the user they can resize the window or the panel both horizontally and vertically.

- CURSOR_MOVE = 13
  Move cursor. Indicates that something can be moved.

- CURSOR_VSPLIT = 14
  Vertical split mouse cursor. On Windows, it's the same as CURSOR_VSIZE.

- CURSOR_HSPLIT = 15
  Horizontal split mouse cursor. On Windows, it's the same as CURSOR_HSIZE.

- CURSOR_HELP = 16
  Help cursor. Usually a question mark.
