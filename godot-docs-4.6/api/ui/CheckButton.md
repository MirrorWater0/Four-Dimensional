# CheckButton

## Meta

- Name: CheckButton
- Source: CheckButton.xml
- Inherits: Button
- Inheritance Chain: CheckButton -> Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that represents a binary choice.

## Description

CheckButton is a toggle button displayed as a check field. It's similar to CheckBox in functionality, but it has a different appearance. To follow established UX patterns, it's recommended to use CheckButton when toggling it has an **immediate** effect on something. For example, it can be used when pressing it shows or hides advanced settings, without asking the user to confirm this action. See also BaseButton which contains common properties and methods associated with this node.

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

- button_checked_color: Color [color] = Color(1, 1, 1, 1)
  The color of the checked icon when the checkbox is pressed.

- button_unchecked_color: Color [color] = Color(1, 1, 1, 1)
  The color of the unchecked icon when the checkbox is not pressed.

- check_v_offset: int [constant] = 0
  The vertical offset used when rendering the toggle icons (in pixels).

- checked: Texture2D [icon]
  The icon to display when the CheckButton is checked (for left-to-right layouts).

- checked_disabled: Texture2D [icon]
  The icon to display when the CheckButton is checked and disabled (for left-to-right layouts).

- checked_disabled_mirrored: Texture2D [icon]
  The icon to display when the CheckButton is checked and disabled (for right-to-left layouts).

- checked_mirrored: Texture2D [icon]
  The icon to display when the CheckButton is checked (for right-to-left layouts).

- unchecked: Texture2D [icon]
  The icon to display when the CheckButton is unchecked (for left-to-right layouts).

- unchecked_disabled: Texture2D [icon]
  The icon to display when the CheckButton is unchecked and disabled (for left-to-right layouts).

- unchecked_disabled_mirrored: Texture2D [icon]
  The icon to display when the CheckButton is unchecked and disabled (for right-to-left layouts).

- unchecked_mirrored: Texture2D [icon]
  The icon to display when the CheckButton is unchecked (for right-to-left layouts).
