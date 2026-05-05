# ItemList

## Meta

- Name: ItemList
- Source: ItemList.xml
- Inherits: Control
- Inheritance Chain: ItemList -> Control -> CanvasItem -> Node -> Object

## Brief Description

A vertical list of selectable items with one or multiple columns.

## Description

This control provides a vertical list of selectable items that may be in a single or in multiple columns, with each item having options for text and an icon. Tooltips are supported and may be different for every item in the list. Selectable items in the list may be selected or deselected and multiple selection may be enabled. Selection with right mouse button may also be enabled to allow use of popup context menus. Items may also be "activated" by double-clicking them or by pressing Enter. Item text only supports single-line strings. Newline characters (e.g. \n) in the string won't produce a newline. Text wrapping is enabled in ICON_MODE_TOP mode, but the column's width is adjusted to fully fit its content by default. You need to set fixed_column_width greater than zero to wrap the text. All set_* methods allow negative item indices, i.e. -1 to access the last item, -2 to select the second-to-last item, and so on. **Incremental search:** Like PopupMenu and Tree, ItemList supports searching within the list while the control is focused. Press a key that matches the first letter of an item's name to select the first item starting with the given letter. After that point, there are two ways to perform incremental search: 1) Press the same key again before the timeout duration to select the next item starting with the same letter. 2) Press letter keys that match the rest of the word before the timeout duration to match to select the item in question directly. Both of these actions will be reset to the beginning of the list if the timeout duration has passed since the last keystroke was registered. You can adjust the timeout duration by changing ProjectSettings.gui/timers/incremental_search_max_interval_msec.

## Quick Reference

```
[methods]
add_icon_item(icon: Texture2D, selectable: bool = true) -> int
add_item(text: String, icon: Texture2D = null, selectable: bool = true) -> int
clear() -> void
deselect(idx: int) -> void
deselect_all() -> void
ensure_current_is_visible() -> void
force_update_list_size() -> void
get_h_scroll_bar() -> HScrollBar
get_item_at_position(position: Vector2, exact: bool = false) -> int [const]
get_item_auto_translate_mode(idx: int) -> int (Node.AutoTranslateMode) [const]
get_item_custom_bg_color(idx: int) -> Color [const]
get_item_custom_fg_color(idx: int) -> Color [const]
get_item_icon(idx: int) -> Texture2D [const]
get_item_icon_modulate(idx: int) -> Color [const]
get_item_icon_region(idx: int) -> Rect2 [const]
get_item_language(idx: int) -> String [const]
get_item_metadata(idx: int) -> Variant [const]
get_item_rect(idx: int, expand: bool = true) -> Rect2 [const]
get_item_text(idx: int) -> String [const]
get_item_text_direction(idx: int) -> int (Control.TextDirection) [const]
get_item_tooltip(idx: int) -> String [const]
get_selected_items() -> PackedInt32Array
get_v_scroll_bar() -> VScrollBar
is_anything_selected() -> bool
is_item_disabled(idx: int) -> bool [const]
is_item_icon_transposed(idx: int) -> bool [const]
is_item_selectable(idx: int) -> bool [const]
is_item_tooltip_enabled(idx: int) -> bool [const]
is_selected(idx: int) -> bool [const]
move_item(from_idx: int, to_idx: int) -> void
remove_item(idx: int) -> void
select(idx: int, single: bool = true) -> void
set_item_auto_translate_mode(idx: int, mode: int (Node.AutoTranslateMode)) -> void
set_item_custom_bg_color(idx: int, custom_bg_color: Color) -> void
set_item_custom_fg_color(idx: int, custom_fg_color: Color) -> void
set_item_disabled(idx: int, disabled: bool) -> void
set_item_icon(idx: int, icon: Texture2D) -> void
set_item_icon_modulate(idx: int, modulate: Color) -> void
set_item_icon_region(idx: int, rect: Rect2) -> void
set_item_icon_transposed(idx: int, transposed: bool) -> void
set_item_language(idx: int, language: String) -> void
set_item_metadata(idx: int, metadata: Variant) -> void
set_item_selectable(idx: int, selectable: bool) -> void
set_item_text(idx: int, text: String) -> void
set_item_text_direction(idx: int, direction: int (Control.TextDirection)) -> void
set_item_tooltip(idx: int, tooltip: String) -> void
set_item_tooltip_enabled(idx: int, enable: bool) -> void
sort_items_by_text() -> void

[properties]
allow_reselect: bool = false
allow_rmb_select: bool = false
allow_search: bool = true
auto_height: bool = false
auto_width: bool = false
clip_contents: bool = true
fixed_column_width: int = 0
fixed_icon_size: Vector2i = Vector2i(0, 0)
focus_mode: int (Control.FocusMode) = 2
icon_mode: int (ItemList.IconMode) = 1
icon_scale: float = 1.0
item_count: int = 0
max_columns: int = 1
max_text_lines: int = 1
same_column_width: bool = false
scroll_hint_mode: int (ItemList.ScrollHintMode) = 0
select_mode: int (ItemList.SelectMode) = 0
text_overrun_behavior: int (TextServer.OverrunBehavior) = 3
tile_scroll_hint: bool = false
wraparound_items: bool = true
```

## Methods

- add_icon_item(icon: Texture2D, selectable: bool = true) -> int
  Adds an item to the item list with no text, only an icon. Returns the index of an added item.

- add_item(text: String, icon: Texture2D = null, selectable: bool = true) -> int
  Adds an item to the item list with specified text. Returns the index of an added item. Specify an icon, or use null as the icon for a list item with no icon. If selectable is true, the list item will be selectable.

- clear() -> void
  Removes all items from the list.

- deselect(idx: int) -> void
  Ensures the item associated with the specified index is not selected.

- deselect_all() -> void
  Ensures there are no items selected.

- ensure_current_is_visible() -> void
  Ensure current selection is visible, adjusting the scroll position as necessary.

- force_update_list_size() -> void
  Forces an update to the list size based on its items. This happens automatically whenever size of the items, or other relevant settings like auto_height, change. The method can be used to trigger the update ahead of next drawing pass.

- get_h_scroll_bar() -> HScrollBar
  Returns the horizontal scrollbar. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their CanvasItem.visible property.

- get_item_at_position(position: Vector2, exact: bool = false) -> int [const]
  Returns the item index at the given position. When there is no item at that point, -1 will be returned if exact is true, and the closest item index will be returned otherwise. **Note:** The returned value is unreliable if called right after modifying the ItemList, before it redraws in the next frame.

- get_item_auto_translate_mode(idx: int) -> int (Node.AutoTranslateMode) [const]
  Returns item's auto translate mode.

- get_item_custom_bg_color(idx: int) -> Color [const]
  Returns the custom background color of the item specified by idx index.

- get_item_custom_fg_color(idx: int) -> Color [const]
  Returns the custom foreground color of the item specified by idx index.

- get_item_icon(idx: int) -> Texture2D [const]
  Returns the icon associated with the specified index.

- get_item_icon_modulate(idx: int) -> Color [const]
  Returns a Color modulating item's icon at the specified index.

- get_item_icon_region(idx: int) -> Rect2 [const]
  Returns the region of item's icon used. The whole icon will be used if the region has no area.

- get_item_language(idx: int) -> String [const]
  Returns item's text language code.

- get_item_metadata(idx: int) -> Variant [const]
  Returns the metadata value of the specified index.

- get_item_rect(idx: int, expand: bool = true) -> Rect2 [const]
  Returns the position and size of the item with the specified index, in the coordinate system of the ItemList node. If expand is true the last column expands to fill the rest of the row. **Note:** The returned value is unreliable if called right after modifying the ItemList, before it redraws in the next frame.

- get_item_text(idx: int) -> String [const]
  Returns the text associated with the specified index.

- get_item_text_direction(idx: int) -> int (Control.TextDirection) [const]
  Returns item's text base writing direction.

- get_item_tooltip(idx: int) -> String [const]
  Returns the tooltip hint associated with the specified index.

- get_selected_items() -> PackedInt32Array
  Returns an array with the indexes of the selected items.

- get_v_scroll_bar() -> VScrollBar
  Returns the vertical scrollbar. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their CanvasItem.visible property.

- is_anything_selected() -> bool
  Returns true if one or more items are selected.

- is_item_disabled(idx: int) -> bool [const]
  Returns true if the item at the specified index is disabled.

- is_item_icon_transposed(idx: int) -> bool [const]
  Returns true if the item icon will be drawn transposed, i.e. the X and Y axes are swapped.

- is_item_selectable(idx: int) -> bool [const]
  Returns true if the item at the specified index is selectable.

- is_item_tooltip_enabled(idx: int) -> bool [const]
  Returns true if the tooltip is enabled for specified item index.

- is_selected(idx: int) -> bool [const]
  Returns true if the item at the specified index is currently selected.

- move_item(from_idx: int, to_idx: int) -> void
  Moves item from index from_idx to to_idx.

- remove_item(idx: int) -> void
  Removes the item specified by idx index from the list.

- select(idx: int, single: bool = true) -> void
  Select the item at the specified index. **Note:** This method does not trigger the item selection signal.

- set_item_auto_translate_mode(idx: int, mode: int (Node.AutoTranslateMode)) -> void
  Sets the auto translate mode of the item associated with the specified index. Items use Node.AUTO_TRANSLATE_MODE_INHERIT by default, which uses the same auto translate mode as the ItemList itself.

- set_item_custom_bg_color(idx: int, custom_bg_color: Color) -> void
  Sets the background color of the item specified by idx index to the specified Color.

- set_item_custom_fg_color(idx: int, custom_fg_color: Color) -> void
  Sets the foreground color of the item specified by idx index to the specified Color.

- set_item_disabled(idx: int, disabled: bool) -> void
  Disables (or enables) the item at the specified index. Disabled items cannot be selected and do not trigger activation signals (when double-clicking or pressing Enter).

- set_item_icon(idx: int, icon: Texture2D) -> void
  Sets (or replaces) the icon's Texture2D associated with the specified index.

- set_item_icon_modulate(idx: int, modulate: Color) -> void
  Sets a modulating Color of the item associated with the specified index.

- set_item_icon_region(idx: int, rect: Rect2) -> void
  Sets the region of item's icon used. The whole icon will be used if the region has no area.

- set_item_icon_transposed(idx: int, transposed: bool) -> void
  Sets whether the item icon will be drawn transposed.

- set_item_language(idx: int, language: String) -> void
  Sets the language code of the text for the item at the given index to language. This is used for line-breaking and text shaping algorithms. If language is empty, the current locale is used.

- set_item_metadata(idx: int, metadata: Variant) -> void
  Sets a value (of any type) to be stored with the item associated with the specified index.

- set_item_selectable(idx: int, selectable: bool) -> void
  Allows or disallows selection of the item associated with the specified index.

- set_item_text(idx: int, text: String) -> void
  Sets text of the item associated with the specified index.

- set_item_text_direction(idx: int, direction: int (Control.TextDirection)) -> void
  Sets item's text base writing direction.

- set_item_tooltip(idx: int, tooltip: String) -> void
  Sets the tooltip hint for the item associated with the specified index.

- set_item_tooltip_enabled(idx: int, enable: bool) -> void
  Sets whether the tooltip hint is enabled for specified item index.

- sort_items_by_text() -> void
  Sorts items in the list by their text.

## Properties

- allow_reselect: bool = false [set set_allow_reselect; get get_allow_reselect]
  If true, the currently selected item can be selected again.

- allow_rmb_select: bool = false [set set_allow_rmb_select; get get_allow_rmb_select]
  If true, right mouse button click can select items.

- allow_search: bool = true [set set_allow_search; get get_allow_search]
  If true, allows navigating the ItemList with letter keys through incremental search.

- auto_height: bool = false [set set_auto_height; get has_auto_height]
  If true, the control will automatically resize the height to fit its content.

- auto_width: bool = false [set set_auto_width; get has_auto_width]
  If true, the control will automatically resize the width to fit its content.

- clip_contents: bool = true [set set_clip_contents; get is_clipping_contents; override Control]

- fixed_column_width: int = 0 [set set_fixed_column_width; get get_fixed_column_width]
  The width all columns will be adjusted to. A value of zero disables the adjustment, each item will have a width equal to the width of its content and the columns will have an uneven width.

- fixed_icon_size: Vector2i = Vector2i(0, 0) [set set_fixed_icon_size; get get_fixed_icon_size]
  The size all icons will be adjusted to. If either X or Y component is not greater than zero, icon size won't be affected.

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- icon_mode: int (ItemList.IconMode) = 1 [set set_icon_mode; get get_icon_mode]
  The icon position, whether above or to the left of the text. See the IconMode constants.

- icon_scale: float = 1.0 [set set_icon_scale; get get_icon_scale]
  The scale of icon applied after fixed_icon_size and transposing takes effect.

- item_count: int = 0 [set set_item_count; get get_item_count]
  The number of items currently in the list.

- max_columns: int = 1 [set set_max_columns; get get_max_columns]
  Maximum columns the list will have. If greater than zero, the content will be split among the specified columns. A value of zero means unlimited columns, i.e. all items will be put in the same row.

- max_text_lines: int = 1 [set set_max_text_lines; get get_max_text_lines]
  Maximum lines of text allowed in each item. Space will be reserved even when there is not enough lines of text to display. **Note:** This property takes effect only when icon_mode is ICON_MODE_TOP. To make the text wrap, fixed_column_width should be greater than zero.

- same_column_width: bool = false [set set_same_column_width; get is_same_column_width]
  Whether all columns will have the same width. If true, the width is equal to the largest column width of all columns.

- scroll_hint_mode: int (ItemList.ScrollHintMode) = 0 [set set_scroll_hint_mode; get get_scroll_hint_mode]
  The way which scroll hints (indicators that show that the content can still be scrolled in a certain direction) will be shown.

- select_mode: int (ItemList.SelectMode) = 0 [set set_select_mode; get get_select_mode]
  Allows single or multiple item selection. See the SelectMode constants.

- text_overrun_behavior: int (TextServer.OverrunBehavior) = 3 [set set_text_overrun_behavior; get get_text_overrun_behavior]
  The clipping behavior when the text exceeds an item's bounding rectangle.

- tile_scroll_hint: bool = false [set set_tile_scroll_hint; get is_scroll_hint_tiled]
  If true, the scroll hint texture will be tiled instead of stretched. See scroll_hint_mode.

- wraparound_items: bool = true [set set_wraparound_items; get has_wraparound_items]
  If true, the control will automatically move items into a new row to fit its content. See also HFlowContainer for this behavior. If false, the control will add a horizontal scrollbar to make all items visible.

## Signals

- empty_clicked(at_position: Vector2, mouse_button_index: int)
  Emitted when any mouse click is issued within the rect of the list but on empty space. at_position is the click position in this control's local coordinate system.

- item_activated(index: int)
  Emitted when specified list item is activated via double-clicking or by pressing Enter.

- item_clicked(index: int, at_position: Vector2, mouse_button_index: int)
  Emitted when specified list item has been clicked with any mouse button. at_position is the click position in this control's local coordinate system.

- item_selected(index: int)
  Emitted when specified item has been selected. Only applicable in single selection mode. allow_reselect must be enabled to reselect an item.

- multi_selected(index: int, selected: bool)
  Emitted when a multiple selection is altered on a list allowing multiple selection.

## Constants

### Enum IconMode

- ICON_MODE_TOP = 0
  Icon is drawn above the text.

- ICON_MODE_LEFT = 1
  Icon is drawn to the left of the text.

### Enum SelectMode

- SELECT_SINGLE = 0
  Only allow selecting a single item.

- SELECT_MULTI = 1
  Allows selecting multiple items by holding Ctrl or Shift.

- SELECT_TOGGLE = 2
  Allows selecting multiple items by toggling them on and off.

### Enum ScrollHintMode

- SCROLL_HINT_MODE_DISABLED = 0
  Scroll hints will never be shown.

- SCROLL_HINT_MODE_BOTH = 1
  Scroll hints will be shown at the top and bottom.

- SCROLL_HINT_MODE_TOP = 2
  Only the top scroll hint will be shown.

- SCROLL_HINT_MODE_BOTTOM = 3
  Only the bottom scroll hint will be shown.

## Theme Items

- font_color: Color [color] = Color(0.65, 0.65, 0.65, 1)
  Default text Color of the item.

- font_hovered_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Text Color used when the item is hovered and not selected yet.

- font_hovered_selected_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the item is hovered and selected.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the item.

- font_selected_color: Color [color] = Color(1, 1, 1, 1)
  Text Color used when the item is selected, but not hovered.

- guide_color: Color [color] = Color(0.7, 0.7, 0.7, 0.25)
  Color of the guideline. The guideline is a line drawn between each row of items.

- scroll_hint_color: Color [color] = Color(0, 0, 0, 1)
  Color used to modulate the [theme_item scroll_hint] texture.

- h_separation: int [constant] = 4
  The horizontal spacing between items.

- icon_margin: int [constant] = 4
  The spacing between item's icon and text.

- line_separation: int [constant] = 2
  The vertical spacing between each line of text.

- outline_size: int [constant] = 0
  The size of the item text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- v_separation: int [constant] = 4
  The vertical spacing between items.

- font: Font [font]
  Font of the item's text.

- font_size: int [font_size]
  Font size of the item's text.

- scroll_hint: Texture2D [icon]
  The indicator that will be shown when the content can still be scrolled. See scroll_hint_mode.

- cursor: StyleBox [style]
  StyleBox used for the cursor, when the ItemList is being focused.

- cursor_unfocused: StyleBox [style]
  StyleBox used for the cursor, when the ItemList is not being focused.

- focus: StyleBox [style]
  The focused style for the ItemList, drawn on top of everything.

- hovered: StyleBox [style]
  StyleBox for the hovered, but not selected items.

- hovered_selected: StyleBox [style]
  StyleBox for the hovered and selected items, used when the ItemList is not being focused.

- hovered_selected_focus: StyleBox [style]
  StyleBox for the hovered and selected items, used when the ItemList is being focused.

- panel: StyleBox [style]
  The background style for the ItemList.

- selected: StyleBox [style]
  StyleBox for the selected items, used when the ItemList is not being focused.

- selected_focus: StyleBox [style]
  StyleBox for the selected items, used when the ItemList is being focused.
