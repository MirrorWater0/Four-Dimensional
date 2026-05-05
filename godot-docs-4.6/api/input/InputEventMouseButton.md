# InputEventMouseButton

## Meta

- Name: InputEventMouseButton
- Source: InputEventMouseButton.xml
- Inherits: InputEventMouse
- Inheritance Chain: InputEventMouseButton -> InputEventMouse -> InputEventWithModifiers -> InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Represents a mouse button being pressed or released.

## Description

Stores information about mouse click events. See Node._input(). **Note:** On Wear OS devices, rotary input is mapped to MOUSE_BUTTON_WHEEL_UP and MOUSE_BUTTON_WHEEL_DOWN. This can be changed to MOUSE_BUTTON_WHEEL_LEFT and MOUSE_BUTTON_WHEEL_RIGHT with the ProjectSettings.input_devices/pointing/android/rotary_input_scroll_axis setting.

## Quick Reference

```
[properties]
button_index: int (MouseButton) = 0
canceled: bool = false
double_click: bool = false
factor: float = 1.0
pressed: bool = false
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)
- [Mouse and input coordinates]($DOCS_URL/tutorials/inputs/mouse_and_input_coordinates.html)

## Properties

- button_index: int (MouseButton) = 0 [set set_button_index; get get_button_index]
  The mouse button identifier, one of the MouseButton button or button wheel constants.

- canceled: bool = false [set set_canceled; get is_canceled]
  If true, the mouse button event has been canceled.

- double_click: bool = false [set set_double_click; get is_double_click]
  If true, the mouse button's state is a double-click.

- factor: float = 1.0 [set set_factor; get get_factor]
  The amount (or delta) of the event. When used for high-precision scroll events, this indicates the scroll amount (vertical or horizontal). This is only supported on some platforms; the reported sensitivity varies depending on the platform. May be 0 if not supported.

- pressed: bool = false [set set_pressed; get is_pressed]
  If true, the mouse button's state is pressed. If false, the mouse button's state is released.
