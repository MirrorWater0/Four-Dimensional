# PopupMenu

## Meta

- Name: PopupMenu
- Source: PopupMenu.xml
- Inherits: Popup
- Inheritance Chain: PopupMenu -> Popup -> Window -> Viewport -> Node -> Object

## Brief Description

A modal window used to display a list of options.

## Description

PopupMenu is a modal window used to display a list of options. Useful for toolbars and context menus. The size of a PopupMenu can be limited by using Window.max_size. If the height of the list of items is larger than the maximum height of the PopupMenu, a ScrollContainer within the popup will allow the user to scroll the contents. If no maximum size is set, or if it is set to 0, the PopupMenu height will be limited by its parent rect. All set_* methods allow negative item indices, i.e. -1 to access the last item, -2 to select the second-to-last item, and so on. **Incremental search:** Like ItemList and Tree, PopupMenu supports searching within the list while the control is focused. Press a key that matches the first letter of an item's name to select the first item starting with the given letter. After that point, there are two ways to perform incremental search: 1) Press the same key again before the timeout duration to select the next item starting with the same letter. 2) Press letter keys that match the rest of the word before the timeout duration to match to select the item in question directly. Both of these actions will be reset to the beginning of the list if the timeout duration has passed since the last keystroke was registered. You can adjust the timeout duration by changing ProjectSettings.gui/timers/incremental_search_max_interval_msec. **Note:** PopupMenu is invisible by default. To make it visible, call one of the popup_* methods from Window on the node, such as Window.popup_centered_clamped(). **Note:** The ID values used for items are limited to 32 bits, not full 64 bits of int. This has a range of -2^32 to 2^32 - 1, i.e. -2147483648 to 2147483647.

## Quick Reference

```
[methods]
activate_item_by_event(event: InputEvent, for_global_only: bool = false) -> bool
add_check_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
add_check_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false) -> void
add_icon_check_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
add_icon_check_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false) -> void
add_icon_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
add_icon_radio_check_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
add_icon_radio_check_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false) -> void
add_icon_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false, allow_echo: bool = false) -> void
add_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
add_multistate_item(label: String, max_states: int, default_state: int = 0, id: int = -1, accel: int (Key) = 0) -> void
add_radio_check_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
add_radio_check_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false) -> void
add_separator(label: String = "", id: int = -1) -> void
add_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false, allow_echo: bool = false) -> void
add_submenu_item(label: String, submenu: String, id: int = -1) -> void
add_submenu_node_item(label: String, submenu: PopupMenu, id: int = -1) -> void
clear(free_submenus: bool = false) -> void
get_focused_item() -> int [const]
get_item_accelerator(index: int) -> int (Key) [const]
get_item_auto_translate_mode(index: int) -> int (Node.AutoTranslateMode) [const]
get_item_icon(index: int) -> Texture2D [const]
get_item_icon_max_width(index: int) -> int [const]
get_item_icon_modulate(index: int) -> Color [const]
get_item_id(index: int) -> int [const]
get_item_indent(index: int) -> int [const]
get_item_index(id: int) -> int [const]
get_item_language(index: int) -> String [const]
get_item_metadata(index: int) -> Variant [const]
get_item_multistate(index: int) -> int [const]
get_item_multistate_max(index: int) -> int [const]
get_item_shortcut(index: int) -> Shortcut [const]
get_item_submenu(index: int) -> String [const]
get_item_submenu_node(index: int) -> PopupMenu [const]
get_item_text(index: int) -> String [const]
get_item_text_direction(index: int) -> int (Control.TextDirection) [const]
get_item_tooltip(index: int) -> String [const]
is_item_checkable(index: int) -> bool [const]
is_item_checked(index: int) -> bool [const]
is_item_disabled(index: int) -> bool [const]
is_item_radio_checkable(index: int) -> bool [const]
is_item_separator(index: int) -> bool [const]
is_item_shortcut_disabled(index: int) -> bool [const]
is_native_menu() -> bool [const]
is_system_menu() -> bool [const]
remove_item(index: int) -> void
scroll_to_item(index: int) -> void
set_focused_item(index: int) -> void
set_item_accelerator(index: int, accel: int (Key)) -> void
set_item_as_checkable(index: int, enable: bool) -> void
set_item_as_radio_checkable(index: int, enable: bool) -> void
set_item_as_separator(index: int, enable: bool) -> void
set_item_auto_translate_mode(index: int, mode: int (Node.AutoTranslateMode)) -> void
set_item_checked(index: int, checked: bool) -> void
set_item_disabled(index: int, disabled: bool) -> void
set_item_icon(index: int, icon: Texture2D) -> void
set_item_icon_max_width(index: int, width: int) -> void
set_item_icon_modulate(index: int, modulate: Color) -> void
set_item_id(index: int, id: int) -> void
set_item_indent(index: int, indent: int) -> void
set_item_language(index: int, language: String) -> void
set_item_metadata(index: int, metadata: Variant) -> void
set_item_multistate(index: int, state: int) -> void
set_item_multistate_max(index: int, max_states: int) -> void
set_item_shortcut(index: int, shortcut: Shortcut, global: bool = false) -> void
set_item_shortcut_disabled(index: int, disabled: bool) -> void
set_item_submenu(index: int, submenu: String) -> void
set_item_submenu_node(index: int, submenu: PopupMenu) -> void
set_item_text(index: int, text: String) -> void
set_item_text_direction(index: int, direction: int (Control.TextDirection)) -> void
set_item_tooltip(index: int, tooltip: String) -> void
toggle_item_checked(index: int) -> void
toggle_item_multistate(index: int) -> void

[properties]
allow_search: bool = true
hide_on_checkable_item_selection: bool = true
hide_on_item_selection: bool = true
hide_on_state_item_selection: bool = false
item_count: int = 0
prefer_native_menu: bool = false
shrink_height: bool = true
shrink_width: bool = true
submenu_popup_delay: float = 0.2
system_menu_id: int (NativeMenu.SystemMenus) = 0
transparent: bool = true
transparent_bg: bool = true
```

## Methods

- activate_item_by_event(event: InputEvent, for_global_only: bool = false) -> bool
  Checks the provided event against the PopupMenu's shortcuts and accelerators, and activates the first item with matching events. If for_global_only is true, only shortcuts and accelerators with global set to true will be called. Returns true if an item was successfully activated. **Note:** Certain Controls, such as MenuButton, will call this method automatically.

- add_check_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new checkable item with text label. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_check_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false) -> void
  Adds a new checkable item and assigns the specified Shortcut to it. Sets the label of the checkbox to the Shortcut's name. An id can optionally be provided. If no id is provided, one will be created from the index. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_icon_check_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new checkable item with text label and icon texture. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_icon_check_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false) -> void
  Adds a new checkable item and assigns the specified Shortcut and icon texture to it. Sets the label of the checkbox to the Shortcut's name. An id can optionally be provided. If no id is provided, one will be created from the index. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_icon_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new item with text label and icon texture. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators.

- add_icon_radio_check_item(texture: Texture2D, label: String, id: int = -1, accel: int (Key) = 0) -> void
  Same as add_icon_check_item(), but uses a radio check button.

- add_icon_radio_check_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false) -> void
  Same as add_icon_check_shortcut(), but uses a radio check button.

- add_icon_shortcut(texture: Texture2D, shortcut: Shortcut, id: int = -1, global: bool = false, allow_echo: bool = false) -> void
  Adds a new item and assigns the specified Shortcut and icon texture to it. Sets the label of the checkbox to the Shortcut's name. An id can optionally be provided. If no id is provided, one will be created from the index. If allow_echo is true, the shortcut can be activated with echo events.

- add_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new item with text label. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators. **Note:** The provided id is used only in id_pressed and id_focused signals. It's not related to the index arguments in e.g. set_item_checked().

- add_multistate_item(label: String, max_states: int, default_state: int = 0, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new multistate item with text label. Contrarily to normal binary items, multistate items can have more than two states, as defined by max_states. The default value is defined by default_state. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators.


```
  func _ready():
      add_multistate_item("Item", 3, 0)

      index_pressed.connect(func(index: int):
              toggle_item_multistate(index)
              match get_item_multistate(index):
                  0:
                      print("First state")
                  1:
                      print("Second state")
                  2:
                      print("Third state")
          )

```
  **Note:** Multistate items don't update their state automatically and must be done manually. See toggle_item_multistate(), set_item_multistate() and get_item_multistate() for more info on how to control it.

- add_radio_check_item(label: String, id: int = -1, accel: int (Key) = 0) -> void
  Adds a new radio check button with text label. An id can optionally be provided, as well as an accelerator (accel). If no id is provided, one will be created from the index. If no accel is provided, then the default value of 0 (corresponding to @GlobalScope.KEY_NONE) will be assigned to the item (which means it won't have any accelerator). See get_item_accelerator() for more info on accelerators. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_radio_check_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false) -> void
  Adds a new radio check button and assigns a Shortcut to it. Sets the label of the checkbox to the Shortcut's name. An id can optionally be provided. If no id is provided, one will be created from the index. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually. See set_item_checked() for more info on how to control it.

- add_separator(label: String = "", id: int = -1) -> void
  Adds a separator between items. Separators also occupy an index, which you can set by using the id parameter. A label can optionally be provided, which will appear at the center of the separator.

- add_shortcut(shortcut: Shortcut, id: int = -1, global: bool = false, allow_echo: bool = false) -> void
  Adds a Shortcut. An id can optionally be provided. If no id is provided, one will be created from the index. If allow_echo is true, the shortcut can be activated with echo events.

- add_submenu_item(label: String, submenu: String, id: int = -1) -> void
  Adds an item that will act as a submenu of the parent PopupMenu node when clicked. The submenu argument must be the name of an existing PopupMenu that has been added as a child to this node. This submenu will be shown when the item is clicked, hovered for long enough, or activated using the ui_select or ui_right input actions. An id can optionally be provided. If no id is provided, one will be created from the index.

- add_submenu_node_item(label: String, submenu: PopupMenu, id: int = -1) -> void
  Adds an item that will act as a submenu of the parent PopupMenu node when clicked. This submenu will be shown when the item is clicked, hovered for long enough, or activated using the ui_select or ui_right input actions. submenu must be either child of this PopupMenu or has no parent node (in which case it will be automatically added as a child). If the submenu popup has another parent, this method will fail. An id can optionally be provided. If no id is provided, one will be created from the index.

- clear(free_submenus: bool = false) -> void
  Removes all items from the PopupMenu. If free_submenus is true, the submenu nodes are automatically freed.

- get_focused_item() -> int [const]
  Returns the index of the currently focused item. Returns -1 if no item is focused.

- get_item_accelerator(index: int) -> int (Key) [const]
  Returns the accelerator of the item at the given index. An accelerator is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. The return value is an integer which is generally a combination of KeyModifierMasks and Keys using bitwise OR such as KEY_MASK_CTRL | KEY_A (Ctrl + A). If no accelerator is defined for the specified index, get_item_accelerator() returns 0 (corresponding to @GlobalScope.KEY_NONE).

- get_item_auto_translate_mode(index: int) -> int (Node.AutoTranslateMode) [const]
  Returns the auto translate mode of the item at the given index.

- get_item_icon(index: int) -> Texture2D [const]
  Returns the icon of the item at the given index.

- get_item_icon_max_width(index: int) -> int [const]
  Returns the maximum allowed width of the icon for the item at the given index.

- get_item_icon_modulate(index: int) -> Color [const]
  Returns a Color modulating the item's icon at the given index.

- get_item_id(index: int) -> int [const]
  Returns the ID of the item at the given index. id can be manually assigned, while index can not.

- get_item_indent(index: int) -> int [const]
  Returns the horizontal offset of the item at the given index.

- get_item_index(id: int) -> int [const]
  Returns the index of the item containing the specified id. Index is automatically assigned to each item by the engine and can not be set manually.

- get_item_language(index: int) -> String [const]
  Returns item's text language code.

- get_item_metadata(index: int) -> Variant [const]
  Returns the metadata of the specified item, which might be of any type. You can set it with set_item_metadata(), which provides a simple way of assigning context data to items.

- get_item_multistate(index: int) -> int [const]
  Returns the state of the item at the given index.

- get_item_multistate_max(index: int) -> int [const]
  Returns the max states of the item at the given index.

- get_item_shortcut(index: int) -> Shortcut [const]
  Returns the Shortcut associated with the item at the given index.

- get_item_submenu(index: int) -> String [const]
  Returns the submenu name of the item at the given index. See add_submenu_item() for more info on how to add a submenu.

- get_item_submenu_node(index: int) -> PopupMenu [const]
  Returns the submenu of the item at the given index, or null if no submenu was added. See add_submenu_node_item() for more info on how to add a submenu.

- get_item_text(index: int) -> String [const]
  Returns the text of the item at the given index.

- get_item_text_direction(index: int) -> int (Control.TextDirection) [const]
  Returns item's text base writing direction.

- get_item_tooltip(index: int) -> String [const]
  Returns the tooltip associated with the item at the given index.

- is_item_checkable(index: int) -> bool [const]
  Returns true if the item at the given index is checkable in some way, i.e. if it has a checkbox or radio button. **Note:** Checkable items just display a checkmark or radio button, but don't have any built-in checking behavior and must be checked/unchecked manually.

- is_item_checked(index: int) -> bool [const]
  Returns true if the item at the given index is checked.

- is_item_disabled(index: int) -> bool [const]
  Returns true if the item at the given index is disabled. When it is disabled it can't be selected, or its action invoked. See set_item_disabled() for more info on how to disable an item.

- is_item_radio_checkable(index: int) -> bool [const]
  Returns true if the item at the given index has radio button-style checkability. **Note:** This is purely cosmetic; you must add the logic for checking/unchecking items in radio groups.

- is_item_separator(index: int) -> bool [const]
  Returns true if the item is a separator. If it is, it will be displayed as a line. See add_separator() for more info on how to add a separator.

- is_item_shortcut_disabled(index: int) -> bool [const]
  Returns true if the specified item's shortcut is disabled.

- is_native_menu() -> bool [const]
  Returns true if the system native menu is supported and currently used by this PopupMenu.

- is_system_menu() -> bool [const]
  Returns true if the menu is bound to the special system menu.

- remove_item(index: int) -> void
  Removes the item at the given index from the menu. **Note:** The indices of items after the removed item will be shifted by one.

- scroll_to_item(index: int) -> void
  Moves the scroll view to make the item at the given index visible.

- set_focused_item(index: int) -> void
  Sets the currently focused item as the given index. Passing -1 as the index makes so that no item is focused.

- set_item_accelerator(index: int, accel: int (Key)) -> void
  Sets the accelerator of the item at the given index. An accelerator is a keyboard shortcut that can be pressed to trigger the menu button even if it's not currently open. accel is generally a combination of KeyModifierMasks and Keys using bitwise OR such as KEY_MASK_CTRL | KEY_A (Ctrl + A).

- set_item_as_checkable(index: int, enable: bool) -> void
  Sets whether the item at the given index has a checkbox. If false, sets the type of the item to plain text. **Note:** Checkable items just display a checkmark, but don't have any built-in checking behavior and must be checked/unchecked manually.

- set_item_as_radio_checkable(index: int, enable: bool) -> void
  Sets the type of the item at the given index to radio button. If false, sets the type of the item to plain text.

- set_item_as_separator(index: int, enable: bool) -> void
  Mark the item at the given index as a separator, which means that it would be displayed as a line. If false, sets the type of the item to plain text.

- set_item_auto_translate_mode(index: int, mode: int (Node.AutoTranslateMode)) -> void
  Sets the auto translate mode of the item at the given index. Items use Node.AUTO_TRANSLATE_MODE_INHERIT by default, which uses the same auto translate mode as the PopupMenu itself.

- set_item_checked(index: int, checked: bool) -> void
  Sets the checkstate status of the item at the given index.

- set_item_disabled(index: int, disabled: bool) -> void
  Enables/disables the item at the given index. When it is disabled, it can't be selected and its action can't be invoked.

- set_item_icon(index: int, icon: Texture2D) -> void
  Replaces the Texture2D icon of the item at the given index.

- set_item_icon_max_width(index: int, width: int) -> void
  Sets the maximum allowed width of the icon for the item at the given index. This limit is applied on top of the default size of the icon and on top of [theme_item icon_max_width]. The height is adjusted according to the icon's ratio.

- set_item_icon_modulate(index: int, modulate: Color) -> void
  Sets a modulating Color of the item's icon at the given index.

- set_item_id(index: int, id: int) -> void
  Sets the id of the item at the given index. The id is used in id_pressed and id_focused signals.

- set_item_indent(index: int, indent: int) -> void
  Sets the horizontal offset of the item at the given index.

- set_item_language(index: int, language: String) -> void
  Sets the language code of the text for the item at the given index to language. This is used for line-breaking and text shaping algorithms. If language is empty, the current locale is used.

- set_item_metadata(index: int, metadata: Variant) -> void
  Sets the metadata of an item, which may be of any type. You can later get it with get_item_metadata(), which provides a simple way of assigning context data to items.

- set_item_multistate(index: int, state: int) -> void
  Sets the state of a multistate item. See add_multistate_item() for details.

- set_item_multistate_max(index: int, max_states: int) -> void
  Sets the max states of a multistate item. See add_multistate_item() for details.

- set_item_shortcut(index: int, shortcut: Shortcut, global: bool = false) -> void
  Sets a Shortcut for the item at the given index.

- set_item_shortcut_disabled(index: int, disabled: bool) -> void
  Disables the Shortcut of the item at the given index.

- set_item_submenu(index: int, submenu: String) -> void
  Sets the submenu of the item at the given index. The submenu is the name of a child PopupMenu node that would be shown when the item is clicked.

- set_item_submenu_node(index: int, submenu: PopupMenu) -> void
  Sets the submenu of the item at the given index. The submenu is a PopupMenu node that would be shown when the item is clicked. It must either be a child of this PopupMenu or has no parent (in which case it will be automatically added as a child). If the submenu popup has another parent, this method will fail.

- set_item_text(index: int, text: String) -> void
  Sets the text of the item at the given index.

- set_item_text_direction(index: int, direction: int (Control.TextDirection)) -> void
  Sets item's text base writing direction.

- set_item_tooltip(index: int, tooltip: String) -> void
  Sets the String tooltip of the item at the given index.

- toggle_item_checked(index: int) -> void
  Toggles the check state of the item at the given index.

- toggle_item_multistate(index: int) -> void
  Cycle to the next state of a multistate item. See add_multistate_item() for details.

## Properties

- allow_search: bool = true [set set_allow_search; get get_allow_search]
  If true, allows navigating PopupMenu with letter keys.

- hide_on_checkable_item_selection: bool = true [set set_hide_on_checkable_item_selection; get is_hide_on_checkable_item_selection]
  If true, hides the PopupMenu when a checkbox or radio button is selected.

- hide_on_item_selection: bool = true [set set_hide_on_item_selection; get is_hide_on_item_selection]
  If true, hides the PopupMenu when an item is selected.

- hide_on_state_item_selection: bool = false [set set_hide_on_state_item_selection; get is_hide_on_state_item_selection]
  If true, hides the PopupMenu when a state item is selected.

- item_count: int = 0 [set set_item_count; get get_item_count]
  The number of items currently in the list.

- prefer_native_menu: bool = false [set set_prefer_native_menu; get is_prefer_native_menu]
  If true, MenuBar will use native menu when supported. **Note:** If PopupMenu is linked to StatusIndicator, MenuBar, or another PopupMenu item it can use native menu regardless of this property, use is_native_menu() to check it.

- shrink_height: bool = true [set set_shrink_height; get get_shrink_height]
  If true, shrinks PopupMenu to minimum height when it's shown.

- shrink_width: bool = true [set set_shrink_width; get get_shrink_width]
  If true, shrinks PopupMenu to minimum width when it's shown.

- submenu_popup_delay: float = 0.2 [set set_submenu_popup_delay; get get_submenu_popup_delay]
  Sets the delay time in seconds for the submenu item to popup on mouse hovering. If the popup menu is added as a child of another (acting as a submenu), it will inherit the delay time of the parent menu item. **Note:** If the mouse is exiting a submenu item with an open submenu and enters a different submenu item, the submenu popup delay time is affected by the direction of the mouse movement toward the open submenu. If the mouse is moving toward the submenu, the open submenu will wait approximately 0.5 seconds before closing, which then allows the hovered submenu item to open. This additional delay allows the mouse time to move to the open submenu across other menu items without prematurely closing. If the mouse is not moving toward the open submenu, for example in a downward direction, the open submenu will close immediately.

- system_menu_id: int (NativeMenu.SystemMenus) = 0 [set set_system_menu; get get_system_menu]
  If set to one of the values of NativeMenu.SystemMenus, this PopupMenu is bound to the special system menu. Only one PopupMenu can be bound to each special menu at a time.

- transparent: bool = true [set set_flag; get get_flag; override Window]

- transparent_bg: bool = true [set set_transparent_background; get has_transparent_background; override Viewport]

## Signals

- id_focused(id: int)
  Emitted when the user navigated to an item of some id using the ProjectSettings.input/ui_up or ProjectSettings.input/ui_down input action.

- id_pressed(id: int)
  Emitted when an item of some id is pressed. Also emitted when its accelerator is activated on macOS. **Note:** If id is negative (either explicitly or due to overflow), this will return the corresponding index instead.

- index_pressed(index: int)
  Emitted when an item of some index is pressed. Also emitted when its accelerator is activated on macOS.

- menu_changed()
  Emitted when any item is added, modified or removed.

## Theme Items

- font_accelerator_color: Color [color] = Color(0.7, 0.7, 0.7, 0.8)
  The text Color used for shortcuts and accelerators that show next to the menu item name when defined. See get_item_accelerator() for more info on accelerators.

- font_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  The default text Color for menu items' names.

- font_disabled_color: Color [color] = Color(0.4, 0.4, 0.4, 0.8)
  Color used for disabled menu items' text.

- font_hover_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  Color used for the hovered text.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the menu item.

- font_separator_color: Color [color] = Color(0.875, 0.875, 0.875, 1)
  Color used for labeled separators' text. See add_separator().

- font_separator_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the labeled separator.

- gutter_compact: int [constant] = 1
  If not 0, the icon gutter will be merged with the checkbox gutter when possible. This acts as a boolean.

- h_separation: int [constant] = 4
  The horizontal space between the item's elements.

- icon_max_width: int [constant] = 0
  The maximum allowed width of the item's icon. This limit is applied on top of the default size of the icon, but before the value set with set_item_icon_max_width(). The height is adjusted according to the icon's ratio.

- indent: int [constant] = 10
  Width of the single indentation level.

- item_end_padding: int [constant] = 2
  Horizontal padding to the right of the items (or left, in RTL layout).

- item_start_padding: int [constant] = 2
  Horizontal padding to the left of the items (or right, in RTL layout).

- outline_size: int [constant] = 0
  The size of the item text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- separator_outline_size: int [constant] = 0
  The size of the labeled separator text outline.

- v_separation: int [constant] = 4
  The vertical space between each menu item.

- font: Font [font]
  Font used for the menu items.

- font_separator: Font [font]
  Font used for the labeled separator.

- font_separator_size: int [font_size]
  Font size of the labeled separator.

- font_size: int [font_size]
  Font size of the menu items.

- checked: Texture2D [icon]
  Texture2D icon for the checked checkbox items.

- checked_disabled: Texture2D [icon]
  Texture2D icon for the checked checkbox items when they are disabled.

- radio_checked: Texture2D [icon]
  Texture2D icon for the checked radio button items.

- radio_checked_disabled: Texture2D [icon]
  Texture2D icon for the checked radio button items when they are disabled.

- radio_unchecked: Texture2D [icon]
  Texture2D icon for the unchecked radio button items.

- radio_unchecked_disabled: Texture2D [icon]
  Texture2D icon for the unchecked radio button items when they are disabled.

- submenu: Texture2D [icon]
  Texture2D icon for the submenu arrow (for left-to-right layouts).

- submenu_mirrored: Texture2D [icon]
  Texture2D icon for the submenu arrow (for right-to-left layouts).

- unchecked: Texture2D [icon]
  Texture2D icon for the unchecked checkbox items.

- unchecked_disabled: Texture2D [icon]
  Texture2D icon for the unchecked checkbox items when they are disabled.

- hover: StyleBox [style]
  StyleBox displayed when the PopupMenu item is hovered.

- labeled_separator_left: StyleBox [style]
  StyleBox for the left side of labeled separator. See add_separator().

- labeled_separator_right: StyleBox [style]
  StyleBox for the right side of labeled separator. See add_separator().

- panel: StyleBox [style]
  StyleBox for the background panel.

- separator: StyleBox [style]
  StyleBox used for the separators. See add_separator().
