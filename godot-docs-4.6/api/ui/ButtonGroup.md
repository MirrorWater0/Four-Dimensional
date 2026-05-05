# ButtonGroup

## Meta

- Name: ButtonGroup
- Source: ButtonGroup.xml
- Inherits: Resource
- Inheritance Chain: ButtonGroup -> Resource -> RefCounted -> Object

## Brief Description

A group of buttons that doesn't allow more than one button to be pressed at a time.

## Description

A group of BaseButton-derived buttons. The buttons in a ButtonGroup are treated like radio buttons: No more than one button can be pressed at a time. Some types of buttons (such as CheckBox) may have a special appearance in this state. Every member of a ButtonGroup should have BaseButton.toggle_mode set to true.

## Quick Reference

```
[methods]
get_buttons() -> BaseButton[]
get_pressed_button() -> BaseButton

[properties]
allow_unpress: bool = false
resource_local_to_scene: bool = true
```

## Methods

- get_buttons() -> BaseButton[]
  Returns an Array of Buttons who have this as their ButtonGroup (see BaseButton.button_group).

- get_pressed_button() -> BaseButton
  Returns the current pressed button.

## Properties

- allow_unpress: bool = false [set set_allow_unpress; get is_allow_unpress]
  If true, it is possible to unpress all buttons in this ButtonGroup.

- resource_local_to_scene: bool = true [set set_local_to_scene; get is_local_to_scene; override Resource]

## Signals

- pressed(button: BaseButton)
  Emitted when one of the buttons of the group is pressed.
