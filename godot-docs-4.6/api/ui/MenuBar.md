# MenuBar

## Meta

- Name: MenuBar
- Source: MenuBar.xml
- Inherits: Control
- Inheritance Chain: MenuBar -> Control -> CanvasItem -> Node -> Object

## Brief Description

A horizontal menu bar that creates a menu for each PopupMenu child.

## Description

A horizontal menu bar that creates a menu for each PopupMenu child. New items are created by adding PopupMenus to this node. Item title is determined by Window.title, or node name if Window.title is empty. Item title can be overridden using set_menu_title().

## Quick Reference

```
[methods]
get_menu_count() -> int [const]
get_menu_popup(menu: int) -> PopupMenu [const]
get_menu_title(menu: int) -> String [const]
get_menu_tooltip(menu: int) -> String [const]
is_menu_disabled(menu: int) -> bool [const]
is_menu_hidden(menu: int) -> bool [const]
is_native_menu() -> bool [const]
set_disable_shortcuts(disabled: bool) -> void
set_menu_disabled(menu: int, disabled: bool) -> void
set_menu_hidden(menu: int, hidden: bool) -> void
set_menu_title(menu: int, title: String) -> void
set_menu_tooltip(menu: int, tooltip: String) -> void

[properties]
flat: bool = false
focus_mode: int (Control.FocusMode) = 3
language: String = ""
prefer_global_menu: bool = true
start_index: int = -1
switch_on_hover: bool = true
text_direction: int (Control.TextDirection) = 0
```

## Methods

- get_menu_count() -> int [const]
  Returns number of menu items.

- get_menu_popup(menu: int) -> PopupMenu [const]
  Returns PopupMenu associated with menu item.

- get_menu_title(menu: int) -> String [const]
  Returns menu item title.

- get_menu_tooltip(menu: int) -> String [const]
  Returns menu item tooltip.

- is_menu_disabled(menu: int) -> bool [const]
  Returns true if the menu item is disabled.

- is_menu_hidden(menu: int) -> bool [const]
  Returns true if the menu item is hidden.

- is_native_menu() -> bool [const]
  Returns true if the current system's global menu is supported and used by this MenuBar.

- set_disable_shortcuts(disabled: bool) -> void
  If true, shortcuts are disabled and cannot be used to trigger the button.

- set_menu_disabled(menu: int, disabled: bool) -> void
  If true, menu item is disabled.

- set_menu_hidden(menu: int, hidden: bool) -> void
  If true, menu item is hidden.

- set_menu_title(menu: int, title: String) -> void
  Sets menu item title.

- set_menu_tooltip(menu: int, tooltip: String) -> void
  Sets menu item tooltip.

## Properties

- flat: bool = false [set set_flat; get is_flat]
  Flat MenuBar don't display item decoration.

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- language: String = "" [set set_language; get get_language]
  Language code used for line-breaking and text shaping algorithms. If left empty, the current locale is used instead.

- prefer_global_menu: bool = true [set set_prefer_global_menu; get is_prefer_global_menu]
  If true, MenuBar will use system global menu when supported. **Note:** If true and global menu is supported, this node is not displayed, has zero size, and all its child nodes except PopupMenus are inaccessible. **Note:** This property overrides the value of the PopupMenu.prefer_native_menu property of the child nodes.

- start_index: int = -1 [set set_start_index; get get_start_index]
  Position order in the global menu to insert MenuBar items at. All menu items in the MenuBar are always inserted as a continuous range. Menus with lower start_index are inserted first. Menus with start_index equal to -1 are inserted last.

- switch_on_hover: bool = true [set set_switch_on_hover; get is_switch_on_hover]
  If true, when the cursor hovers above menu item, it will close the current PopupMenu and open the other one.

- text_direction: int (Control.TextDirection) = 0 [set set_text_direction; get get_text_direction]
  Base text writing direction.

## Theme Items

- font_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  Default text Color of the menu item.

- font_disabled_color: Color [color] = Color(0.875, 0.875, 0.875, 0.5)
  Text Color used when the menu item is disabled.

- font_focus_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the menu item is focused. Only replaces the normal text color of the menu item. Disabled, hovered, and pressed states take precedence over this color.

- font_hover_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the menu item is being hovered.

- font_hover_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the menu item is being hovered and pressed.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the menu item.

- font_pressed_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the menu item is being pressed.

- h_separation: int [constant] = 4
  The horizontal space between menu items.

- outline_size: int [constant] = 0
  The size of the text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- font: Font [font]
  Font of the menu item's text.

- font_size: int [font_size]
  Font size of the menu item's text.

- disabled: StyleBox [style]
  StyleBox used when the menu item is disabled.

- disabled_mirrored: StyleBox [style]
  StyleBox used when the menu item is disabled (for right-to-left layouts).

- hover: StyleBox [style]
  StyleBox used when the menu item is being hovered.

- hover_mirrored: StyleBox [style]
  StyleBox used when the menu item is being hovered (for right-to-left layouts).

- hover_pressed: StyleBox [style]
  StyleBox used when the menu item is being pressed and hovered at the same time.

- hover_pressed_mirrored: StyleBox [style]
  StyleBox used when the menu item is being pressed and hovered at the same time (for right-to-left layouts).

- normal: StyleBox [style]
  Default StyleBox for the menu item.

- normal_mirrored: StyleBox [style]
  Default StyleBox for the menu item (for right-to-left layouts).

- pressed: StyleBox [style]
  StyleBox used when the menu item is being pressed.

- pressed_mirrored: StyleBox [style]
  StyleBox used when the menu item is being pressed (for right-to-left layouts).
