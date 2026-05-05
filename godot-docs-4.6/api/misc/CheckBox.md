# CheckBox

## Meta

- Name: CheckBox
- Source: CheckBox.xml
- Inherits: Button
- Inheritance Chain: CheckBox -> Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that represents a binary choice.

## Description

CheckBox allows the user to choose one of only two possible options. It's similar to CheckButton in functionality, but it has a different appearance. To follow established UX patterns, it's recommended to use CheckBox when toggling it has **no** immediate effect on something. For example, it could be used when toggling it will only do something once a confirmation button is pressed. See also BaseButton which contains common properties and methods associated with this node. When BaseButton.button_group specifies a ButtonGroup, CheckBox changes its appearance to that of a radio button and uses the various radio_* theme properties.

## Quick Reference

```
[properties]
alignment: int (HorizontalAlignment) = 0
toggle_mode: bool = true
```

## Properties

- alignment: int (HorizontalAlignment) = 0 [set set_text_alignment; get get_text_alignment; override Button]

- toggle_mode: bool = true [set set_toggle_mode; get is_toggle_mode; override BaseButton]

## Theme Items

- checkbox_checked_color: Color [color] = Color(1, 1, 1, 1)
  The color of the checked icon when the checkbox is pressed.

- checkbox_unchecked_color: Color [color] = Color(1, 1, 1, 1)
  The color of the unchecked icon when the checkbox is not pressed.

- check_v_offset: int [constant] = 0
  The vertical offset used when rendering the check icons (in pixels).

- checked: Texture2D [icon]
  The check icon to display when the CheckBox is checked.

- checked_disabled: Texture2D [icon]
  The check icon to display when the CheckBox is checked and is disabled.

- radio_checked: Texture2D [icon]
  The check icon to display when the CheckBox is configured as a radio button and is checked.

- radio_checked_disabled: Texture2D [icon]
  The check icon to display when the CheckBox is configured as a radio button, is disabled, and is unchecked.

- radio_unchecked: Texture2D [icon]
  The check icon to display when the CheckBox is configured as a radio button and is unchecked.

- radio_unchecked_disabled: Texture2D [icon]
  The check icon to display when the CheckBox is configured as a radio button, is disabled, and is unchecked.

- unchecked: Texture2D [icon]
  The check icon to display when the CheckBox is unchecked.

- unchecked_disabled: Texture2D [icon]
  The check icon to display when the CheckBox is unchecked and is disabled.
