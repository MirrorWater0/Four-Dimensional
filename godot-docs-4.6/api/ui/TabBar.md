# TabBar

## Meta

- Name: TabBar
- Source: TabBar.xml
- Inherits: Control
- Inheritance Chain: TabBar -> Control -> CanvasItem -> Node -> Object

## Brief Description

A control that provides a horizontal bar with tabs.

## Description

A control that provides a horizontal bar with tabs. Similar to TabContainer but is only in charge of drawing tabs, not interacting with children.

## Quick Reference

```
[methods]
add_tab(title: String = "", icon: Texture2D = null) -> void
clear_tabs() -> void
ensure_tab_visible(idx: int) -> void
get_offset_buttons_visible() -> bool [const]
get_previous_tab() -> int [const]
get_tab_button_icon(tab_idx: int) -> Texture2D [const]
get_tab_icon(tab_idx: int) -> Texture2D [const]
get_tab_icon_max_width(tab_idx: int) -> int [const]
get_tab_idx_at_point(point: Vector2) -> int [const]
get_tab_language(tab_idx: int) -> String [const]
get_tab_metadata(tab_idx: int) -> Variant [const]
get_tab_offset() -> int [const]
get_tab_rect(tab_idx: int) -> Rect2 [const]
get_tab_text_direction(tab_idx: int) -> int (Control.TextDirection) [const]
get_tab_title(tab_idx: int) -> String [const]
get_tab_tooltip(tab_idx: int) -> String [const]
is_tab_disabled(tab_idx: int) -> bool [const]
is_tab_hidden(tab_idx: int) -> bool [const]
move_tab(from: int, to: int) -> void
remove_tab(tab_idx: int) -> void
select_next_available() -> bool
select_previous_available() -> bool
set_tab_button_icon(tab_idx: int, icon: Texture2D) -> void
set_tab_disabled(tab_idx: int, disabled: bool) -> void
set_tab_hidden(tab_idx: int, hidden: bool) -> void
set_tab_icon(tab_idx: int, icon: Texture2D) -> void
set_tab_icon_max_width(tab_idx: int, width: int) -> void
set_tab_language(tab_idx: int, language: String) -> void
set_tab_metadata(tab_idx: int, metadata: Variant) -> void
set_tab_text_direction(tab_idx: int, direction: int (Control.TextDirection)) -> void
set_tab_title(tab_idx: int, title: String) -> void
set_tab_tooltip(tab_idx: int, tooltip: String) -> void

[properties]
clip_tabs: bool = true
close_with_middle_mouse: bool = true
current_tab: int = -1
deselect_enabled: bool = false
drag_to_rearrange_enabled: bool = false
focus_mode: int (Control.FocusMode) = 2
max_tab_width: int = 0
scroll_to_selected: bool = true
scrolling_enabled: bool = true
select_with_rmb: bool = false
switch_on_drag_hover: bool = true
tab_alignment: int (TabBar.AlignmentMode) = 0
tab_close_display_policy: int (TabBar.CloseButtonDisplayPolicy) = 0
tab_count: int = 0
tabs_rearrange_group: int = -1
```

## Methods

- add_tab(title: String = "", icon: Texture2D = null) -> void
  Adds a new tab.

- clear_tabs() -> void
  Clears all tabs.

- ensure_tab_visible(idx: int) -> void
  Moves the scroll view to make the tab visible.

- get_offset_buttons_visible() -> bool [const]
  Returns true if the offset buttons (the ones that appear when there's not enough space for all tabs) are visible.

- get_previous_tab() -> int [const]
  Returns the previously active tab index.

- get_tab_button_icon(tab_idx: int) -> Texture2D [const]
  Returns the icon for the right button of the tab at index tab_idx or null if the right button has no icon.

- get_tab_icon(tab_idx: int) -> Texture2D [const]
  Returns the icon for the tab at index tab_idx or null if the tab has no icon.

- get_tab_icon_max_width(tab_idx: int) -> int [const]
  Returns the maximum allowed width of the icon for the tab at index tab_idx.

- get_tab_idx_at_point(point: Vector2) -> int [const]
  Returns the index of the tab at local coordinates point. Returns -1 if the point is outside the control boundaries or if there's no tab at the queried position.

- get_tab_language(tab_idx: int) -> String [const]
  Returns tab title language code.

- get_tab_metadata(tab_idx: int) -> Variant [const]
  Returns the metadata value set to the tab at index tab_idx using set_tab_metadata(). If no metadata was previously set, returns null by default.

- get_tab_offset() -> int [const]
  Returns the number of hidden tabs offsetted to the left.

- get_tab_rect(tab_idx: int) -> Rect2 [const]
  Returns tab Rect2 with local position and size.

- get_tab_text_direction(tab_idx: int) -> int (Control.TextDirection) [const]
  Returns tab title text base writing direction.

- get_tab_title(tab_idx: int) -> String [const]
  Returns the title of the tab at index tab_idx.

- get_tab_tooltip(tab_idx: int) -> String [const]
  Returns the tooltip text of the tab at index tab_idx.

- is_tab_disabled(tab_idx: int) -> bool [const]
  Returns true if the tab at index tab_idx is disabled.

- is_tab_hidden(tab_idx: int) -> bool [const]
  Returns true if the tab at index tab_idx is hidden.

- move_tab(from: int, to: int) -> void
  Moves a tab from from to to.

- remove_tab(tab_idx: int) -> void
  Removes the tab at index tab_idx.

- select_next_available() -> bool
  Selects the first available tab with greater index than the currently selected. Returns true if tab selection changed.

- select_previous_available() -> bool
  Selects the first available tab with lower index than the currently selected. Returns true if tab selection changed.

- set_tab_button_icon(tab_idx: int, icon: Texture2D) -> void
  Sets an icon for the button of the tab at index tab_idx (located to the right, before the close button), making it visible and clickable (See tab_button_pressed). Giving it a null value will hide the button.

- set_tab_disabled(tab_idx: int, disabled: bool) -> void
  If disabled is true, disables the tab at index tab_idx, making it non-interactable.

- set_tab_hidden(tab_idx: int, hidden: bool) -> void
  If hidden is true, hides the tab at index tab_idx, making it disappear from the tab area.

- set_tab_icon(tab_idx: int, icon: Texture2D) -> void
  Sets an icon for the tab at index tab_idx.

- set_tab_icon_max_width(tab_idx: int, width: int) -> void
  Sets the maximum allowed width of the icon for the tab at index tab_idx. This limit is applied on top of the default size of the icon and on top of [theme_item icon_max_width]. The height is adjusted according to the icon's ratio.

- set_tab_language(tab_idx: int, language: String) -> void
  Sets the language code of the title for the tab at index tab_idx to language. This is used for line-breaking and text shaping algorithms. If language is empty, the current locale is used.

- set_tab_metadata(tab_idx: int, metadata: Variant) -> void
  Sets the metadata value for the tab at index tab_idx, which can be retrieved later using get_tab_metadata().

- set_tab_text_direction(tab_idx: int, direction: int (Control.TextDirection)) -> void
  Sets tab title base writing direction.

- set_tab_title(tab_idx: int, title: String) -> void
  Sets a title for the tab at index tab_idx.

- set_tab_tooltip(tab_idx: int, tooltip: String) -> void
  Sets a tooltip for tab at index tab_idx. **Note:** By default, if the tooltip is empty and the tab text is truncated (not all characters fit into the tab), the title will be displayed as a tooltip. To hide the tooltip, assign " " as the tooltip text.

## Properties

- clip_tabs: bool = true [set set_clip_tabs; get get_clip_tabs]
  If true, tabs overflowing this node's width will be hidden, displaying two navigation buttons instead. Otherwise, this node's minimum size is updated so that all tabs are visible.

- close_with_middle_mouse: bool = true [set set_close_with_middle_mouse; get get_close_with_middle_mouse]
  If true, middle-clicking on a tab will emit the tab_close_pressed signal.

- current_tab: int = -1 [set set_current_tab; get get_current_tab]
  The index of the current selected tab. A value of -1 means that no tab is selected and can only be set when deselect_enabled is true or if all tabs are hidden or disabled.

- deselect_enabled: bool = false [set set_deselect_enabled; get get_deselect_enabled]
  If true, all tabs can be deselected so that no tab is selected. Click on the current tab to deselect it.

- drag_to_rearrange_enabled: bool = false [set set_drag_to_rearrange_enabled; get get_drag_to_rearrange_enabled]
  If true, tabs can be rearranged with mouse drag.

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- max_tab_width: int = 0 [set set_max_tab_width; get get_max_tab_width]
  Sets the maximum width which all tabs should be limited to. Unlimited if set to 0.

- scroll_to_selected: bool = true [set set_scroll_to_selected; get get_scroll_to_selected]
  If true, the tab offset will be changed to keep the currently selected tab visible.

- scrolling_enabled: bool = true [set set_scrolling_enabled; get get_scrolling_enabled]
  if true, the mouse's scroll wheel can be used to navigate the scroll view.

- select_with_rmb: bool = false [set set_select_with_rmb; get get_select_with_rmb]
  If true, enables selecting a tab with the right mouse button.

- switch_on_drag_hover: bool = true [set set_switch_on_drag_hover; get get_switch_on_drag_hover]
  If true, hovering over a tab while dragging something will switch to that tab. Does not have effect when hovering another tab to rearrange. The delay for when this happens is dictated by [theme_item hover_switch_wait_msec].

- tab_alignment: int (TabBar.AlignmentMode) = 0 [set set_tab_alignment; get get_tab_alignment]
  The horizontal alignment of the tabs.

- tab_close_display_policy: int (TabBar.CloseButtonDisplayPolicy) = 0 [set set_tab_close_display_policy; get get_tab_close_display_policy]
  When the close button will appear on the tabs.

- tab_count: int = 0 [set set_tab_count; get get_tab_count]
  The number of tabs currently in the bar.

- tabs_rearrange_group: int = -1 [set set_tabs_rearrange_group; get get_tabs_rearrange_group]
  TabBars with the same rearrange group ID will allow dragging the tabs between them. Enable drag with drag_to_rearrange_enabled. Setting this to -1 will disable rearranging between TabBars.

## Signals

- active_tab_rearranged(idx_to: int)
  Emitted when the active tab is rearranged via mouse drag. See drag_to_rearrange_enabled.

- tab_button_pressed(tab: int)
  Emitted when a tab's right button is pressed. See set_tab_button_icon().

- tab_changed(tab: int)
  Emitted when switching to another tab.

- tab_clicked(tab: int)
  Emitted when a tab is clicked, even if it is the current tab.

- tab_close_pressed(tab: int)
  Emitted when a tab's close button is pressed or, if close_with_middle_mouse is true, when middle-clicking on a tab. **Note:** Tabs are not removed automatically; this behavior needs to be coded manually. For example:

```
$TabBar.tab_close_pressed.connect($TabBar.remove_tab)
```

```
GetNode<TabBar>("TabBar").TabClosePressed += GetNode<TabBar>("TabBar").RemoveTab;
```

- tab_hovered(tab: int)
  Emitted when a tab is hovered by the mouse.

- tab_rmb_clicked(tab: int)
  Emitted when a tab is right-clicked.

- tab_selected(tab: int)
  Emitted when a tab is selected via click, directional input, or script, even if it is the current tab.

## Constants

### Enum AlignmentMode

- ALIGNMENT_LEFT = 0
  Aligns tabs to the left.

- ALIGNMENT_CENTER = 1
  Aligns tabs in the middle.

- ALIGNMENT_RIGHT = 2
  Aligns tabs to the right.

- ALIGNMENT_MAX = 3
  Represents the size of the AlignmentMode enum.

### Enum CloseButtonDisplayPolicy

- CLOSE_BUTTON_SHOW_NEVER = 0
  Never show the close buttons.

- CLOSE_BUTTON_SHOW_ACTIVE_ONLY = 1
  Only show the close button on the currently active tab.

- CLOSE_BUTTON_SHOW_ALWAYS = 2
  Show the close button on all tabs.

- CLOSE_BUTTON_MAX = 3
  Represents the size of the CloseButtonDisplayPolicy enum.

## Theme Items

- drop_mark_color: Color [color] = Color(1, 1, 1, 1)
  Modulation color for the [theme_item drop_mark] icon.

- font_disabled_color: Color [color] = Color(0.875, 0.875, 0.875, 0.5)
  Font color of disabled tabs.

- font_hovered_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Font color of the currently hovered tab. Does not apply to the selected tab.

- font_outline_color: Color [color] = Color(0, 0, 0, 1)
  The tint of text outline of the tab name.

- font_selected_color: Color [color] = Color(0.95, 0.95, 0.95, 1)
  Font color of the currently selected tab.

- font_unselected_color: Color [color] = Color(0.7, 0.7, 0.7, 1)
  Font color of the other, unselected tabs.

- icon_disabled_color: Color [color] = Color(1, 1, 1, 1)
  Icon color of disabled tabs.

- icon_hovered_color: Color [color] = Color(1, 1, 1, 1)
  Icon color of the currently hovered tab. Does not apply to the selected tab.

- icon_selected_color: Color [color] = Color(1, 1, 1, 1)
  Icon color of the currently selected tab.

- icon_unselected_color: Color [color] = Color(1, 1, 1, 1)
  Icon color of the other, unselected tabs.

- h_separation: int [constant] = 4
  The horizontal separation between the elements inside tabs.

- hover_switch_wait_msec: int [constant] = 500
  During a drag-and-drop, this is how many milliseconds to wait before switching the tab.

- icon_max_width: int [constant] = 0
  The maximum allowed width of the tab's icon. This limit is applied on top of the default size of the icon, but before the value set with set_tab_icon_max_width(). The height is adjusted according to the icon's ratio.

- outline_size: int [constant] = 0
  The size of the tab text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- tab_separation: int [constant] = 0
  The space between tabs in the tab bar.

- font: Font [font]
  The font used to draw tab names.

- font_size: int [font_size]
  Font size of the tab names.

- close: Texture2D [icon]
  The icon for the close button (see tab_close_display_policy).

- decrement: Texture2D [icon]
  Icon for the left arrow button that appears when there are too many tabs to fit in the container width. When the button is disabled (i.e. the first tab is visible), it appears semi-transparent.

- decrement_highlight: Texture2D [icon]
  Icon for the left arrow button that appears when there are too many tabs to fit in the container width. Used when the button is being hovered with the cursor.

- drop_mark: Texture2D [icon]
  Icon shown to indicate where a dragged tab is gonna be dropped (see drag_to_rearrange_enabled).

- increment: Texture2D [icon]
  Icon for the right arrow button that appears when there are too many tabs to fit in the container width. When the button is disabled (i.e. the last tab is visible) it appears semi-transparent.

- increment_highlight: Texture2D [icon]
  Icon for the right arrow button that appears when there are too many tabs to fit in the container width. Used when the button is being hovered with the cursor.

- button_highlight: StyleBox [style]
  Background of the tab and close buttons when they're being hovered with the cursor.

- button_pressed: StyleBox [style]
  Background of the tab and close buttons when it's being pressed.

- tab_disabled: StyleBox [style]
  The style of disabled tabs.

- tab_focus: StyleBox [style]
  StyleBox used when the TabBar is focused. The [theme_item tab_focus] StyleBox is displayed *over* the base StyleBox of the selected tab, so a partially transparent StyleBox should be used to ensure the base StyleBox remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- tab_hovered: StyleBox [style]
  The style of the currently hovered tab. Does not apply to the selected tab. **Note:** This style will be drawn with the same width as [theme_item tab_unselected] at minimum.

- tab_selected: StyleBox [style]
  The style of the currently selected tab.

- tab_unselected: StyleBox [style]
  The style of the other, unselected tabs.
