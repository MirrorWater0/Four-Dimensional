# InputEventWithModifiers

## Meta

- Name: InputEventWithModifiers
- Source: InputEventWithModifiers.xml
- Inherits: InputEventFromWindow
- Inheritance Chain: InputEventWithModifiers -> InputEventFromWindow -> InputEvent -> Resource -> RefCounted -> Object

## Brief Description

Abstract base class for input events affected by modifier keys like Shift and Alt.

## Description

Stores information about mouse, keyboard, and touch gesture input events. This includes information about which modifier keys are pressed, such as Shift or Alt. See Node._input(). **Note:** Modifier keys are considered modifiers only when used in combination with another key. As a result, their corresponding member variables, such as ctrl_pressed, will return false if the key is pressed on its own.

## Quick Reference

```
[methods]
get_modifiers_mask() -> int (KeyModifierMask) [const]
is_command_or_control_pressed() -> bool [const]

[properties]
alt_pressed: bool = false
command_or_control_autoremap: bool = false
ctrl_pressed: bool = false
meta_pressed: bool = false
shift_pressed: bool = false
```

## Tutorials

- [Using InputEvent]($DOCS_URL/tutorials/inputs/inputevent.html)

## Methods

- get_modifiers_mask() -> int (KeyModifierMask) [const]
  Returns the keycode combination of modifier keys.

- is_command_or_control_pressed() -> bool [const]
  On macOS, returns true if Meta (Cmd) is pressed. On other platforms, returns true if Ctrl is pressed.

## Properties

- alt_pressed: bool = false [set set_alt_pressed; get is_alt_pressed]
  State of the Alt modifier.

- command_or_control_autoremap: bool = false [set set_command_or_control_autoremap; get is_command_or_control_autoremap]
  Automatically use Meta (Cmd) on macOS and Ctrl on other platforms. If true, ctrl_pressed and meta_pressed cannot be set.

- ctrl_pressed: bool = false [set set_ctrl_pressed; get is_ctrl_pressed]
  State of the Ctrl modifier.

- meta_pressed: bool = false [set set_meta_pressed; get is_meta_pressed]
  State of the Meta modifier. On Windows and Linux, this represents the Windows key (sometimes called "meta" or "super" on Linux). On macOS, this represents the Command key.

- shift_pressed: bool = false [set set_shift_pressed; get is_shift_pressed]
  State of the Shift modifier.
