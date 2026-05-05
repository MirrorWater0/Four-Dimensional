# OptionButton

## Meta

- Name: OptionButton
- Source: OptionButton.xml
- Inherits: Button
- Inheritance Chain: OptionButton -> Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that brings up a dropdown with selectable options when pressed.

## Description

OptionButton is a type of button that brings up a dropdown with selectable items when pressed. The item selected becomes the "current" item and is displayed as the button text. See also BaseButton which contains common properties and methods associated with this node. **Note:** The IDs used for items are limited to signed 32-bit integers, not the full 64 bits of int. These have a range of -2^31 to 2^31 - 1, that is, -2147483648 to 2147483647. **Note:** The Button.text and Button.icon properties are set automatically based on the selected item. They shouldn't be changed manually.

## Quick Reference

```
[methods]
add_icon_item(texture: Texture2D, label: String, id: int = -1) -> void
add_item(label: String, id: int = -1) -> void
add_separator(text: String = "") -> void
clear() -> void
get_item_auto_translate_mode(idx: int) -> int (Node.AutoTranslateMode) [const]
get_item_icon(idx: int) -> Texture2D [const]
get_item_id(idx: int) -> int [const]
get_item_index(id: int) -> int [const]
get_item_metadata(idx: int) -> Variant [const]
get_item_text(idx: int) -> String [const]
get_item_tooltip(idx: int) -> String [const]
get_popup() -> PopupMenu [const]
get_selectable_item(from_last: bool = false) -> int [const]
get_selected_id() -> int [const]
get_selected_metadata() -> Variant [const]
has_selectable_items() -> bool [const]
is_item_disabled(idx: int) -> bool [const]
is_item_separator(idx: int) -> bool [const]
remove_item(idx: int) -> void
select(idx: int) -> void
set_disable_shortcuts(disabled: bool) -> void
set_item_auto_translate_mode(idx: int, mode: int (Node.AutoTranslateMode)) -> void
set_item_disabled(idx: int, disabled: bool) -> void
set_item_icon(idx: int, texture: Texture2D) -> void
set_item_id(idx: int, id: int) -> void
set_item_metadata(idx: int, metadata: Variant) -> void
set_item_text(idx: int, text: String) -> void
set_item_tooltip(idx: int, tooltip: String) -> void
show_popup() -> void

[properties]
action_mode: int (BaseButton.ActionMode) = 0
alignment: int (HorizontalAlignment) = 0
allow_reselect: bool = false
fit_to_longest_item: bool = true
item_count: int = 0
selected: int = -1
toggle_mode: bool = true
```

## Methods

- add_icon_item(texture: Texture2D, label: String, id: int = -1) -> void
  Adds an item, with a texture icon, text label and (optionally) id. If no id is passed, the item index will be used as the item's ID. New items are appended at the end. **Note:** The item will be selected if there are no other items.

- add_item(label: String, id: int = -1) -> void
  Adds an item, with text label and (optionally) id. If no id is passed, the item index will be used as the item's ID. New items are appended at the end. **Note:** The item will be selected if there are no other items.

- add_separator(text: String = "") -> void
  Adds a separator to the list of items. Separators help to group items, and can optionally be given a text header. A separator also gets an index assigned, and is appended at the end of the item list.

- clear() -> void
  Clears all the items in the OptionButton.

- get_item_auto_translate_mode(idx: int) -> int (Node.AutoTranslateMode) [const]
  Returns the auto translate mode of the item at index idx.

- get_item_icon(idx: int) -> Texture2D [const]
  Returns the icon of the item at index idx.

- get_item_id(idx: int) -> int [const]
  Returns the ID of the item at index idx.

- get_item_index(id: int) -> int [const]
  Returns the index of the item with the given id.

- get_item_metadata(idx: int) -> Variant [const]
  Retrieves the metadata of an item. Metadata may be any type and can be used to store extra information about an item, such as an external string ID.

- get_item_text(idx: int) -> String [const]
  Returns the text of the item at index idx.

- get_item_tooltip(idx: int) -> String [const]
  Returns the tooltip of the item at index idx.

- get_popup() -> PopupMenu [const]
  Returns the PopupMenu contained in this button. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their Window.visible property.

- get_selectable_item(from_last: bool = false) -> int [const]
  Returns the index of the first item which is not disabled, or marked as a separator. If from_last is true, the items will be searched in reverse order. Returns -1 if no item is found.

- get_selected_id() -> int [const]
  Returns the ID of the selected item, or -1 if no item is selected.

- get_selected_metadata() -> Variant [const]
  Gets the metadata of the selected item. Metadata for items can be set using set_item_metadata().

- has_selectable_items() -> bool [const]
  Returns true if this button contains at least one item which is not disabled, or marked as a separator.

- is_item_disabled(idx: int) -> bool [const]
  Returns true if the item at index idx is disabled.

- is_item_separator(idx: int) -> bool [const]
  Returns true if the item at index idx is marked as a separator.

- remove_item(idx: int) -> void
  Removes the item at index idx.

- select(idx: int) -> void
  Selects an item by index and makes it the current item. This will work even if the item is disabled. Passing -1 as the index deselects any currently selected item.

- set_disable_shortcuts(disabled: bool) -> void
  If true, shortcuts are disabled and cannot be used to trigger the button.

- set_item_auto_translate_mode(idx: int, mode: int (Node.AutoTranslateMode)) -> void
  Sets the auto translate mode of the item at index idx. Items use Node.AUTO_TRANSLATE_MODE_INHERIT by default, which uses the same auto translate mode as the OptionButton itself.

- set_item_disabled(idx: int, disabled: bool) -> void
  Sets whether the item at index idx is disabled. Disabled items are drawn differently in the dropdown and are not selectable by the user. If the current selected item is set as disabled, it will remain selected.

- set_item_icon(idx: int, texture: Texture2D) -> void
  Sets the icon of the item at index idx.

- set_item_id(idx: int, id: int) -> void
  Sets the ID of the item at index idx.

- set_item_metadata(idx: int, metadata: Variant) -> void
  Sets the metadata of an item. Metadata may be of any type and can be used to store extra information about an item, such as an external string ID.

- set_item_text(idx: int, text: String) -> void
  Sets the text of the item at index idx.

- set_item_tooltip(idx: int, tooltip: String) -> void
  Sets the tooltip of the item at index idx.

- show_popup() -> void
  Adjusts popup position and sizing for the OptionButton, then shows the PopupMenu. Prefer this over using get_popup().popup().

## Properties

- action_mode: int (BaseButton.ActionMode) = 0 [set set_action_mode; get get_action_mode; override BaseButton]

- alignment: int (HorizontalAlignment) = 0 [set set_text_alignment; get get_text_alignment; override Button]

- allow_reselect: bool = false [set set_allow_reselect; get get_allow_reselect]
  If true, the currently selected item can be selected again.

- fit_to_longest_item: bool = true [set set_fit_to_longest_item; get is_fit_to_longest_item]
  If true, minimum size will be determined by the longest item's text, instead of the currently selected one's. **Note:** For performance reasons, the minimum size doesn't update immediately when adding, removing or modifying items.

- item_count: int = 0 [set set_item_count; get get_item_count]
  The number of items to select from.

- selected: int = -1 [set _select_int; get get_selected]
  The index of the currently selected item, or -1 if no item is selected.

- toggle_mode: bool = true [set set_toggle_mode; get is_toggle_mode; override BaseButton]

## Signals

- item_focused(index: int)
  Emitted when the user navigates to an item using the ProjectSettings.input/ui_up or ProjectSettings.input/ui_down input actions. The index of the item selected is passed as argument.

- item_selected(index: int)
  Emitted when the current item has been changed by the user. The index of the item selected is passed as argument. allow_reselect must be enabled to reselect an item.

## Theme Items

- arrow_margin: int [constant] = 4
  The horizontal space between the arrow icon and the right edge of the button.

- modulate_arrow: int [constant] = 0
  If different than 0, the arrow icon will be modulated to the font color.

- arrow: Texture2D [icon]
  The arrow icon to be drawn on the right end of the button.
