# TreeItem

## Meta

- Name: TreeItem
- Source: TreeItem.xml
- Inherits: Object
- Inheritance Chain: TreeItem -> Object

## Brief Description

An internal control for a single item inside Tree.

## Description

A single item of a Tree control. It can contain other TreeItems as children, which allows it to create a hierarchy. It can also contain text and buttons. TreeItem is not a Node, it is internal to the Tree. To create a TreeItem, use Tree.create_item() or TreeItem.create_child(). To remove a TreeItem, use Object.free(). **Note:** The ID values used for buttons are 32-bit, unlike int which is always 64-bit. They go from -2147483648 to 2147483647.

## Quick Reference

```
[methods]
add_button(column: int, button: Texture2D, id: int = -1, disabled: bool = false, tooltip_text: String = "", description: String = "") -> void
add_child(child: TreeItem) -> void
call_recursive(method: StringName) -> void [vararg]
clear_buttons() -> void
clear_custom_bg_color(column: int) -> void
clear_custom_color(column: int) -> void
create_child(index: int = -1) -> TreeItem
deselect(column: int) -> void
erase_button(column: int, button_index: int) -> void
get_auto_translate_mode(column: int) -> int (Node.AutoTranslateMode) [const]
get_autowrap_mode(column: int) -> int (TextServer.AutowrapMode) [const]
get_button(column: int, button_index: int) -> Texture2D [const]
get_button_by_id(column: int, id: int) -> int [const]
get_button_color(column: int, id: int) -> Color [const]
get_button_count(column: int) -> int [const]
get_button_id(column: int, button_index: int) -> int [const]
get_button_tooltip_text(column: int, button_index: int) -> String [const]
get_cell_mode(column: int) -> int (TreeItem.TreeCellMode) [const]
get_child(index: int) -> TreeItem
get_child_count() -> int
get_children() -> TreeItem[]
get_custom_bg_color(column: int) -> Color [const]
get_custom_color(column: int) -> Color [const]
get_custom_draw_callback(column: int) -> Callable [const]
get_custom_font(column: int) -> Font [const]
get_custom_font_size(column: int) -> int [const]
get_custom_stylebox(column: int) -> StyleBox [const]
get_description(column: int) -> String [const]
get_expand_right(column: int) -> bool [const]
get_first_child() -> TreeItem [const]
get_icon(column: int) -> Texture2D [const]
get_icon_max_width(column: int) -> int [const]
get_icon_modulate(column: int) -> Color [const]
get_icon_overlay(column: int) -> Texture2D [const]
get_icon_region(column: int) -> Rect2 [const]
get_index() -> int
get_language(column: int) -> String [const]
get_metadata(column: int) -> Variant [const]
get_next() -> TreeItem [const]
get_next_in_tree(wrap: bool = false) -> TreeItem
get_next_visible(wrap: bool = false) -> TreeItem
get_parent() -> TreeItem [const]
get_prev() -> TreeItem
get_prev_in_tree(wrap: bool = false) -> TreeItem
get_prev_visible(wrap: bool = false) -> TreeItem
get_range(column: int) -> float [const]
get_range_config(column: int) -> Dictionary
get_structured_text_bidi_override(column: int) -> int (TextServer.StructuredTextParser) [const]
get_structured_text_bidi_override_options(column: int) -> Array [const]
get_suffix(column: int) -> String [const]
get_text(column: int) -> String [const]
get_text_alignment(column: int) -> int (HorizontalAlignment) [const]
get_text_direction(column: int) -> int (Control.TextDirection) [const]
get_text_overrun_behavior(column: int) -> int (TextServer.OverrunBehavior) [const]
get_tooltip_text(column: int) -> String [const]
get_tree() -> Tree [const]
is_any_collapsed(only_visible: bool = false) -> bool
is_button_disabled(column: int, button_index: int) -> bool [const]
is_checked(column: int) -> bool [const]
is_custom_set_as_button(column: int) -> bool [const]
is_edit_multiline(column: int) -> bool [const]
is_editable(column: int) -> bool
is_indeterminate(column: int) -> bool [const]
is_selectable(column: int) -> bool [const]
is_selected(column: int) -> bool
is_visible_in_tree() -> bool [const]
move_after(item: TreeItem) -> void
move_before(item: TreeItem) -> void
propagate_check(column: int, emit_signal: bool = true) -> void
remove_child(child: TreeItem) -> void
select(column: int) -> void
set_auto_translate_mode(column: int, mode: int (Node.AutoTranslateMode)) -> void
set_autowrap_mode(column: int, autowrap_mode: int (TextServer.AutowrapMode)) -> void
set_button(column: int, button_index: int, button: Texture2D) -> void
set_button_color(column: int, button_index: int, color: Color) -> void
set_button_description(column: int, button_index: int, description: String) -> void
set_button_disabled(column: int, button_index: int, disabled: bool) -> void
set_button_tooltip_text(column: int, button_index: int, tooltip: String) -> void
set_cell_mode(column: int, mode: int (TreeItem.TreeCellMode)) -> void
set_checked(column: int, checked: bool) -> void
set_collapsed_recursive(enable: bool) -> void
set_custom_as_button(column: int, enable: bool) -> void
set_custom_bg_color(column: int, color: Color, just_outline: bool = false) -> void
set_custom_color(column: int, color: Color) -> void
set_custom_draw(column: int, object: Object, callback: StringName) -> void
set_custom_draw_callback(column: int, callback: Callable) -> void
set_custom_font(column: int, font: Font) -> void
set_custom_font_size(column: int, font_size: int) -> void
set_custom_stylebox(column: int, stylebox: StyleBox) -> void
set_description(column: int, description: String) -> void
set_edit_multiline(column: int, multiline: bool) -> void
set_editable(column: int, enabled: bool) -> void
set_expand_right(column: int, enable: bool) -> void
set_icon(column: int, texture: Texture2D) -> void
set_icon_max_width(column: int, width: int) -> void
set_icon_modulate(column: int, modulate: Color) -> void
set_icon_overlay(column: int, texture: Texture2D) -> void
set_icon_region(column: int, region: Rect2) -> void
set_indeterminate(column: int, indeterminate: bool) -> void
set_language(column: int, language: String) -> void
set_metadata(column: int, meta: Variant) -> void
set_range(column: int, value: float) -> void
set_range_config(column: int, min: float, max: float, step: float, expr: bool = false) -> void
set_selectable(column: int, selectable: bool) -> void
set_structured_text_bidi_override(column: int, parser: int (TextServer.StructuredTextParser)) -> void
set_structured_text_bidi_override_options(column: int, args: Array) -> void
set_suffix(column: int, text: String) -> void
set_text(column: int, text: String) -> void
set_text_alignment(column: int, text_alignment: int (HorizontalAlignment)) -> void
set_text_direction(column: int, direction: int (Control.TextDirection)) -> void
set_text_overrun_behavior(column: int, overrun_behavior: int (TextServer.OverrunBehavior)) -> void
set_tooltip_text(column: int, tooltip: String) -> void
uncollapse_tree() -> void

[properties]
collapsed: bool
custom_minimum_height: int
disable_folding: bool
visible: bool
```

## Methods

- add_button(column: int, button: Texture2D, id: int = -1, disabled: bool = false, tooltip_text: String = "", description: String = "") -> void
  Adds a button with Texture2D button to the end of the cell at column column. The id is used to identify the button in the according Tree.button_clicked signal and can be different from the buttons index. If not specified, the next available index is used, which may be retrieved by calling get_button_count() immediately before this method. Optionally, the button can be disabled and have a tooltip_text. description is used as the button description for assistive apps.

- add_child(child: TreeItem) -> void
  Adds a previously unparented TreeItem as a direct child of this one. The child item must not be a part of any Tree or parented to any TreeItem. See also remove_child().

- call_recursive(method: StringName) -> void [vararg]
  Calls the method on the actual TreeItem and its children recursively. Pass parameters as a comma separated list.

- clear_buttons() -> void
  Removes all buttons from all columns of this item.

- clear_custom_bg_color(column: int) -> void
  Resets the background color for the given column to default.

- clear_custom_color(column: int) -> void
  Resets the color for the given column to default.

- create_child(index: int = -1) -> TreeItem
  Creates an item and adds it as a child. The new item will be inserted as position index (the default value -1 means the last position), or it will be the last child if index is higher than the child count.

- deselect(column: int) -> void
  Deselects the given column.

- erase_button(column: int, button_index: int) -> void
  Removes the button at index button_index in column column.

- get_auto_translate_mode(column: int) -> int (Node.AutoTranslateMode) [const]
  Returns the column's auto translate mode.

- get_autowrap_mode(column: int) -> int (TextServer.AutowrapMode) [const]
  Returns the text autowrap mode in the given column. By default it is TextServer.AUTOWRAP_OFF.

- get_button(column: int, button_index: int) -> Texture2D [const]
  Returns the Texture2D of the button at index button_index in column column.

- get_button_by_id(column: int, id: int) -> int [const]
  Returns the button index if there is a button with ID id in column column, otherwise returns -1.

- get_button_color(column: int, id: int) -> Color [const]
  Returns the color of the button with ID id in column column. If the specified button does not exist, returns Color.BLACK.

- get_button_count(column: int) -> int [const]
  Returns the number of buttons in column column.

- get_button_id(column: int, button_index: int) -> int [const]
  Returns the ID for the button at index button_index in column column.

- get_button_tooltip_text(column: int, button_index: int) -> String [const]
  Returns the tooltip text for the button at index button_index in column column.

- get_cell_mode(column: int) -> int (TreeItem.TreeCellMode) [const]
  Returns the column's cell mode.

- get_child(index: int) -> TreeItem
  Returns a child item by its index (see get_child_count()). This method is often used for iterating all children of an item. Negative indices access the children from the last one.

- get_child_count() -> int
  Returns the number of child items.

- get_children() -> TreeItem[]
  Returns an array of references to the item's children.

- get_custom_bg_color(column: int) -> Color [const]
  Returns the custom background color of column column.

- get_custom_color(column: int) -> Color [const]
  Returns the custom color of column column.

- get_custom_draw_callback(column: int) -> Callable [const]
  Returns the custom callback of column column.

- get_custom_font(column: int) -> Font [const]
  Returns custom font used to draw text in the column column.

- get_custom_font_size(column: int) -> int [const]
  Returns custom font size used to draw text in the column column.

- get_custom_stylebox(column: int) -> StyleBox [const]
  Returns the given column's custom StyleBox used to draw the background.

- get_description(column: int) -> String [const]
  Returns the given column's description for assistive apps.

- get_expand_right(column: int) -> bool [const]
  Returns true if expand_right is set.

- get_first_child() -> TreeItem [const]
  Returns the TreeItem's first child.

- get_icon(column: int) -> Texture2D [const]
  Returns the given column's icon Texture2D. Error if no icon is set.

- get_icon_max_width(column: int) -> int [const]
  Returns the maximum allowed width of the icon in the given column.

- get_icon_modulate(column: int) -> Color [const]
  Returns the Color modulating the column's icon.

- get_icon_overlay(column: int) -> Texture2D [const]
  Returns the given column's icon overlay Texture2D.

- get_icon_region(column: int) -> Rect2 [const]
  Returns the icon Texture2D region as Rect2.

- get_index() -> int
  Returns the node's order in the tree. For example, if called on the first child item the position is 0.

- get_language(column: int) -> String [const]
  Returns item's text language code.

- get_metadata(column: int) -> Variant [const]
  Returns the metadata value that was set for the given column using set_metadata().

- get_next() -> TreeItem [const]
  Returns the next sibling TreeItem in the tree or a null object if there is none.

- get_next_in_tree(wrap: bool = false) -> TreeItem
  Returns the next TreeItem in the tree (in the context of a depth-first search) or a null object if there is none. If wrap is enabled, the method will wrap around to the first element in the tree when called on the last element, otherwise it returns null.

- get_next_visible(wrap: bool = false) -> TreeItem
  Returns the next visible TreeItem in the tree (in the context of a depth-first search) or a null object if there is none. If wrap is enabled, the method will wrap around to the first visible element in the tree when called on the last visible element, otherwise it returns null.

- get_parent() -> TreeItem [const]
  Returns the parent TreeItem or a null object if there is none.

- get_prev() -> TreeItem
  Returns the previous sibling TreeItem in the tree or a null object if there is none.

- get_prev_in_tree(wrap: bool = false) -> TreeItem
  Returns the previous TreeItem in the tree (in the context of a depth-first search) or a null object if there is none. If wrap is enabled, the method will wrap around to the last element in the tree when called on the first visible element, otherwise it returns null.

- get_prev_visible(wrap: bool = false) -> TreeItem
  Returns the previous visible sibling TreeItem in the tree (in the context of a depth-first search) or a null object if there is none. If wrap is enabled, the method will wrap around to the last visible element in the tree when called on the first visible element, otherwise it returns null.

- get_range(column: int) -> float [const]
  Returns the value of a CELL_MODE_RANGE column.

- get_range_config(column: int) -> Dictionary
  Returns a dictionary containing the range parameters for a given column. The keys are "min", "max", "step", and "expr".

- get_structured_text_bidi_override(column: int) -> int (TextServer.StructuredTextParser) [const]
  Returns the BiDi algorithm override set for this cell.

- get_structured_text_bidi_override_options(column: int) -> Array [const]
  Returns the additional BiDi options set for this cell.

- get_suffix(column: int) -> String [const]
  Gets the suffix string shown after the column value.

- get_text(column: int) -> String [const]
  Returns the given column's text.

- get_text_alignment(column: int) -> int (HorizontalAlignment) [const]
  Returns the given column's text alignment.

- get_text_direction(column: int) -> int (Control.TextDirection) [const]
  Returns item's text base writing direction.

- get_text_overrun_behavior(column: int) -> int (TextServer.OverrunBehavior) [const]
  Returns the clipping behavior when the text exceeds the item's bounding rectangle in the given column. By default it is TextServer.OVERRUN_TRIM_ELLIPSIS.

- get_tooltip_text(column: int) -> String [const]
  Returns the given column's tooltip text.

- get_tree() -> Tree [const]
  Returns the Tree that owns this TreeItem.

- is_any_collapsed(only_visible: bool = false) -> bool
  Returns true if this TreeItem, or any of its descendants, is collapsed. If only_visible is true it ignores non-visible TreeItems.

- is_button_disabled(column: int, button_index: int) -> bool [const]
  Returns true if the button at index button_index for the given column is disabled.

- is_checked(column: int) -> bool [const]
  Returns true if the given column is checked.

- is_custom_set_as_button(column: int) -> bool [const]
  Returns true if the cell was made into a button with set_custom_as_button().

- is_edit_multiline(column: int) -> bool [const]
  Returns true if the given column is multiline editable.

- is_editable(column: int) -> bool
  Returns true if the given column is editable.

- is_indeterminate(column: int) -> bool [const]
  Returns true if the given column is indeterminate.

- is_selectable(column: int) -> bool [const]
  Returns true if the given column is selectable.

- is_selected(column: int) -> bool
  Returns true if the given column is selected.

- is_visible_in_tree() -> bool [const]
  Returns true if visible is true and all its ancestors are also visible.

- move_after(item: TreeItem) -> void
  Moves this TreeItem right after the given item. **Note:** You can't move to the root or move the root.

- move_before(item: TreeItem) -> void
  Moves this TreeItem right before the given item. **Note:** You can't move to the root or move the root.

- propagate_check(column: int, emit_signal: bool = true) -> void
  Propagates this item's checked status to its children and parents for the given column. It is possible to process the items affected by this method call by connecting to Tree.check_propagated_to_item. The order that the items affected will be processed is as follows: the item invoking this method, children of that item, and finally parents of that item. If emit_signal is false, then Tree.check_propagated_to_item will not be emitted.

- remove_child(child: TreeItem) -> void
  Removes the given child TreeItem and all its children from the Tree. Note that it doesn't free the item from memory, so it can be reused later (see add_child()). To completely remove a TreeItem use Object.free(). **Note:** If you want to move a child from one Tree to another, then instead of removing and adding it manually you can use move_before() or move_after().

- select(column: int) -> void
  Selects the given column.

- set_auto_translate_mode(column: int, mode: int (Node.AutoTranslateMode)) -> void
  Sets the given column's auto translate mode to mode. All columns use Node.AUTO_TRANSLATE_MODE_INHERIT by default, which uses the same auto translate mode as the Tree itself.

- set_autowrap_mode(column: int, autowrap_mode: int (TextServer.AutowrapMode)) -> void
  Sets the autowrap mode in the given column. If set to something other than TextServer.AUTOWRAP_OFF, the text gets wrapped inside the cell's bounding rectangle.

- set_button(column: int, button_index: int, button: Texture2D) -> void
  Sets the given column's button Texture2D at index button_index to button.

- set_button_color(column: int, button_index: int, color: Color) -> void
  Sets the given column's button color at index button_index to color.

- set_button_description(column: int, button_index: int, description: String) -> void
  Sets the given column's button description at index button_index for assistive apps.

- set_button_disabled(column: int, button_index: int, disabled: bool) -> void
  If true, disables the button at index button_index in the given column.

- set_button_tooltip_text(column: int, button_index: int, tooltip: String) -> void
  Sets the tooltip text for the button at index button_index in the given column.

- set_cell_mode(column: int, mode: int (TreeItem.TreeCellMode)) -> void
  Sets the given column's cell mode to mode. This determines how the cell is displayed and edited.

- set_checked(column: int, checked: bool) -> void
  If checked is true, the given column is checked. Clears column's indeterminate status.

- set_collapsed_recursive(enable: bool) -> void
  Collapses or uncollapses this TreeItem and all the descendants of this item.

- set_custom_as_button(column: int, enable: bool) -> void
  Makes a cell with CELL_MODE_CUSTOM display as a non-flat button with a StyleBox.

- set_custom_bg_color(column: int, color: Color, just_outline: bool = false) -> void
  Sets the given column's custom background color and whether to just use it as an outline. **Note:** If a custom StyleBox is set, the background color will be drawn behind it.

- set_custom_color(column: int, color: Color) -> void
  Sets the given column's custom color.

- set_custom_draw(column: int, object: Object, callback: StringName) -> void
  Sets the given column's custom draw callback to the callback method on object. The method named callback should accept two arguments: the TreeItem that is drawn and its position and size as a Rect2.

- set_custom_draw_callback(column: int, callback: Callable) -> void
  Sets the given column's custom draw callback. Use an empty Callable ([code skip-lint]Callable()[/code]) to clear the custom callback. The cell has to be in CELL_MODE_CUSTOM to use this feature. The callback should accept two arguments: the TreeItem that is drawn and its position and size as a Rect2.

- set_custom_font(column: int, font: Font) -> void
  Sets custom font used to draw text in the given column.

- set_custom_font_size(column: int, font_size: int) -> void
  Sets custom font size used to draw text in the given column.

- set_custom_stylebox(column: int, stylebox: StyleBox) -> void
  Sets the given column's custom StyleBox used to draw the background. **Note:** If a custom background color is set, the StyleBox will be drawn in front of it.

- set_description(column: int, description: String) -> void
  Sets the given column's description for assistive apps.

- set_edit_multiline(column: int, multiline: bool) -> void
  If multiline is true, the given column is multiline editable. **Note:** This option only affects the type of control (LineEdit or TextEdit) that appears when editing the column. You can set multiline values with set_text() even if the column is not multiline editable.

- set_editable(column: int, enabled: bool) -> void
  If enabled is true, the given column is editable.

- set_expand_right(column: int, enable: bool) -> void
  If enable is true, the given column is expanded to the right.

- set_icon(column: int, texture: Texture2D) -> void
  Sets the given cell's icon Texture2D. If the cell is in CELL_MODE_ICON mode, the icon is displayed in the center of the cell. Otherwise, the icon is displayed before the cell's text. CELL_MODE_RANGE does not display an icon.

- set_icon_max_width(column: int, width: int) -> void
  Sets the maximum allowed width of the icon in the given column. This limit is applied on top of the default size of the icon and on top of [theme_item Tree.icon_max_width]. The height is adjusted according to the icon's ratio.

- set_icon_modulate(column: int, modulate: Color) -> void
  Modulates the given column's icon with modulate.

- set_icon_overlay(column: int, texture: Texture2D) -> void
  Sets the given cell's icon overlay Texture2D. The cell has to be in CELL_MODE_ICON mode, and icon has to be set. Overlay is drawn on top of icon, in the bottom left corner.

- set_icon_region(column: int, region: Rect2) -> void
  Sets the given column's icon's texture region.

- set_indeterminate(column: int, indeterminate: bool) -> void
  If indeterminate is true, the given column is marked indeterminate. **Note:** If set true from false, then column is cleared of checked status.

- set_language(column: int, language: String) -> void
  Sets the language code of the given column's text to language. This is used for line-breaking and text shaping algorithms. If language is empty, the current locale is used.

- set_metadata(column: int, meta: Variant) -> void
  Sets the metadata value for the given column, which can be retrieved later using get_metadata(). This can be used, for example, to store a reference to the original data.

- set_range(column: int, value: float) -> void
  Sets the value of a CELL_MODE_RANGE column.

- set_range_config(column: int, min: float, max: float, step: float, expr: bool = false) -> void
  Sets the range of accepted values for a column. The column must be in the CELL_MODE_RANGE mode. If expr is true, the edit mode slider will use an exponential scale as with Range.exp_edit.

- set_selectable(column: int, selectable: bool) -> void
  If selectable is true, the given column is selectable.

- set_structured_text_bidi_override(column: int, parser: int (TextServer.StructuredTextParser)) -> void
  Set BiDi algorithm override for the structured text. Has effect for cells that display text.

- set_structured_text_bidi_override_options(column: int, args: Array) -> void
  Set additional options for BiDi override. Has effect for cells that display text.

- set_suffix(column: int, text: String) -> void
  Sets a string to be shown after a column's value (for example, a unit abbreviation).

- set_text(column: int, text: String) -> void
  Sets the given column's text value.

- set_text_alignment(column: int, text_alignment: int (HorizontalAlignment)) -> void
  Sets the given column's text alignment to text_alignment.

- set_text_direction(column: int, direction: int (Control.TextDirection)) -> void
  Sets item's text base writing direction.

- set_text_overrun_behavior(column: int, overrun_behavior: int (TextServer.OverrunBehavior)) -> void
  Sets the clipping behavior when the text exceeds the item's bounding rectangle in the given column.

- set_tooltip_text(column: int, tooltip: String) -> void
  Sets the given column's tooltip text.

- uncollapse_tree() -> void
  Uncollapses all TreeItems necessary to reveal this TreeItem, i.e. all ancestor TreeItems.

## Properties

- collapsed: bool [set set_collapsed; get is_collapsed]
  If true, the TreeItem is collapsed.

- custom_minimum_height: int [set set_custom_minimum_height; get get_custom_minimum_height]
  The custom minimum height.

- disable_folding: bool [set set_disable_folding; get is_folding_disabled]
  If true, folding is disabled for this TreeItem.

- visible: bool [set set_visible; get is_visible]
  If true, the TreeItem is visible (default). Note that if a TreeItem is set to not be visible, none of its children will be visible either.

## Constants

### Enum TreeCellMode

- CELL_MODE_STRING = 0
  Cell shows a string label, optionally with an icon. When editable, the text can be edited using a LineEdit, or a TextEdit popup if set_edit_multiline() is used.

- CELL_MODE_CHECK = 1
  Cell shows a checkbox, optionally with text and an icon. The checkbox can be pressed, released, or indeterminate (via set_indeterminate()). The checkbox can't be clicked unless the cell is editable.

- CELL_MODE_RANGE = 2
  Cell shows a numeric range. When editable, it can be edited using a range slider. Use set_range() to set the value and set_range_config() to configure the range. This cell can also be used in a text dropdown mode when you assign a text with set_text(). Separate options with a comma, e.g. "Option1,Option2,Option3".

- CELL_MODE_ICON = 3
  Cell shows an icon. It can't be edited nor display text. The icon is always centered within the cell.

- CELL_MODE_CUSTOM = 4
  Cell shows as a clickable button. It will display an arrow similar to OptionButton, but doesn't feature a dropdown (for that you can use CELL_MODE_RANGE). Clicking the button emits the Tree.item_edited signal. The button is flat by default, you can use set_custom_as_button() to display it with a StyleBox. This mode also supports custom drawing using set_custom_draw_callback().
