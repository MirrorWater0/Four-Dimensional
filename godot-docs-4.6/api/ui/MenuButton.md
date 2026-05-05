# MenuButton

## Meta

- Name: MenuButton
- Source: MenuButton.xml
- Inherits: Button
- Inheritance Chain: MenuButton -> Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that brings up a PopupMenu when clicked.

## Description

A button that brings up a PopupMenu when clicked. To create new items inside this PopupMenu, use get_popup().add_item("My Item Name"). You can also create them directly from Godot editor's inspector. See also BaseButton which contains common properties and methods associated with this node.

## Quick Reference

```
[methods]
get_popup() -> PopupMenu [const]
set_disable_shortcuts(disabled: bool) -> void
show_popup() -> void

[properties]
action_mode: int (BaseButton.ActionMode) = 0
flat: bool = true
focus_mode: int (Control.FocusMode) = 3
item_count: int = 0
switch_on_hover: bool = false
toggle_mode: bool = true
```

## Methods

- get_popup() -> PopupMenu [const]
  Returns the PopupMenu contained in this button. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their Window.visible property.

- set_disable_shortcuts(disabled: bool) -> void
  If true, shortcuts are disabled and cannot be used to trigger the button.

- show_popup() -> void
  Adjusts popup position and sizing for the MenuButton, then shows the PopupMenu. Prefer this over using get_popup().popup().

## Properties

- action_mode: int (BaseButton.ActionMode) = 0 [set set_action_mode; get get_action_mode; override BaseButton]

- flat: bool = true [set set_flat; get is_flat; override Button]

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- item_count: int = 0 [set set_item_count; get get_item_count]
  The number of items currently in the list.

- switch_on_hover: bool = false [set set_switch_on_hover; get is_switch_on_hover]
  If true, when the cursor hovers above another MenuButton within the same parent which also has switch_on_hover enabled, it will close the current MenuButton and open the other one.

- toggle_mode: bool = true [set set_toggle_mode; get is_toggle_mode; override BaseButton]

## Signals

- about_to_popup()
  Emitted when the PopupMenu of this MenuButton is about to show.
