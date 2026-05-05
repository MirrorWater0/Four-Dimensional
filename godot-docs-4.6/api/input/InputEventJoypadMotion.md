# InputEventJoypadMotion

## Meta

- Name: InputEventJoypadMotion
- Source: InputEventJoypadMotion.xml
- Inherits: InputEvent
- Inheritance Chain: InputEventJoypadMotion -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Represents axis motions (such as joystick or analog triggers) from a gamepad.

## Description

Stores information about joystick motions. One InputEventJoypadMotion represents one axis at a time. For gamepad buttons, see InputEventJoypadButton.

## Quick Reference

```
[properties]
axis: int (JoyAxis) = 0
axis_value: float = 0.0
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Properties

- axis: int (JoyAxis) = 0 [set set_axis; get get_axis]
  Axis identifier.

- axis_value: float = 0.0 [set set_axis_value; get get_axis_value]
  Current position of the joystick on the given axis. The value ranges from -1.0 to 1.0. A value of 0 means the axis is in its resting position.
