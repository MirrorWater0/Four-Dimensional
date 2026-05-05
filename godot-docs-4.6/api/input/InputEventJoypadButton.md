# InputEventJoypadButton

## Meta

- Name: InputEventJoypadButton
- Source: InputEventJoypadButton.xml
- Inherits: InputEvent
- Inheritance Chain: InputEventJoypadButton -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Represents a gamepad button being pressed or released.

## Description

Input event type for gamepad buttons. For gamepad analog sticks and joysticks, see InputEventJoypadMotion.

## Quick Reference

```
[properties]
button_index: int (JoyButton) = 0
pressed: bool = false
pressure: float = 0.0
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Properties

- button_index: int (JoyButton) = 0 [set set_button_index; get get_button_index]
  Button identifier. One of the JoyButton button constants.

- pressed: bool = false [set set_pressed; get is_pressed]
  If true, the button's state is pressed. If false, the button's state is released.

- pressure: float = 0.0 [set set_pressure; get get_pressure]
