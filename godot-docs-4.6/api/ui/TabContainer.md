# TabContainer

## Meta

- Name: TabContainer
- Source: TabContainer.xml
- Inherits: Container
- Inheritance Chain: TabContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that creates a tab for each child control, displaying only the active tab's control.

## Description

Arranges child controls into a tabbed view, creating a tab for each one. The active tab's corresponding control is made visible, while all other child controls are hidden. Ignores non-control children. **Note:** The drawing of the clickable tabs is handled by this node; TabBar is not needed.

## Quick Reference

```
[methods]
get_current_tab_control() -> Control [const]
get_popup() -> Popup [const]
get_previous_tab() -> int [const]
get_tab_bar() -> TabBar [const]
get_tab_button_icon(tab_idx: int) -> Texture2D [const]
get_tab_control(tab_idx: int) -> Control [const]
get_tab_count() -> int [const]
get_tab_icon(tab_idx: int) -> Texture2D [const]
get_tab_icon_max_width(tab_idx: int) -> int [const]
get_tab_idx_at_point(point: Vector2) -> int [const]
get_tab_idx_from_control(control: Control) -> int [const]
get_tab_metadata(tab_idx: int) -> Variant [const]
get_tab_title(tab_idx: int) -> String [const]
get_tab_tooltip(tab_idx: int) -> String [const]
is_tab_disabled(tab_idx: int) -> bool [const]
is_tab_hidden(tab_idx: int) -> bool [const]
select_next_available() -> bool
select_previous_available() -> bool
set_popup(popup: Node) -> void
set_tab_button_icon(tab_idx: int, icon: Texture2D) -> void
set_tab_disabled(tab_idx: int, disabled: bool) -> void
set_tab_hidden(tab_idx: int, hidden: bool) -> void
set_tab_icon(tab_idx: int, icon: Texture2D) -> void
set_tab_icon_max_width(tab_idx: int, width: int) -> void
set_tab_metadata(tab_idx: int, metadata: Variant) -> void
set_tab_title(tab_idx: int, title: String) -> void
set_tab_tooltip(tab_idx: int, tooltip: String) -> void

[properties]
all_tabs_in_front: bool = false
clip_tabs: bool = true
current_tab: int = -1
deselect_enabled: bool = false
drag_to_rearrange_enabled: bool = false
switch_on_drag_hover: bool = true
tab_alignment: int (TabBar.AlignmentMode) = 0
tab_focus_mode: int (Control.FocusMode) = 2
tabs_position: int (TabContainer.TabPosition) = 0
tabs_rearrange_group: int = -1
tabs_visible: bool = true
use_hidden_tabs_for_min_size: bool = false
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Methods

- get_current_tab_control() -> Control [const]
  Returns the child Control node located at the active tab index.

- get_popup() -> Popup [const]
  Returns the Popup node instance if one has been set already with set_popup(). **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their Window.visible property.

- get_previous_tab() -> int [const]
  Returns the previously active tab index.

- get_tab_bar() -> TabBar [const]
  Returns the TabBar contained in this container. **Warning:** This is a required internal node, removing and freeing it or editing its tabs may cause a crash. If you wish to edit the tabs, use the methods provided in TabContainer.

- get_tab_button_icon(tab_idx: int) -> Texture2D [const]
  Returns the button icon from the tab at index tab_idx.

- get_tab_control(tab_idx: int) -> Control [const]
  Returns the Control node from the tab at index tab_idx.

- get_tab_count() -> int [const]
  Returns the number of tabs.

- get_tab_icon(tab_idx: int) -> Texture2D [const]
  Returns the Texture2D for the tab at index tab_idx or null if the tab has no Texture2D.

- get_tab_icon_max_width(tab_idx: int) -> int [const]
  Returns the maximum allowed width of the icon for the tab at index tab_idx.

- get_tab_idx_at_point(point: Vector2) -> int [const]
  Returns the index of the tab at local coordinates point. Returns -1 if the point is outside the control boundaries or if there's no tab at the queried position.

- get_tab_idx_from_control(control: Control) -> int [const]
  Returns the index of the tab tied to the given control. The control must be a child of the TabContainer.

- get_tab_metadata(tab_idx: int) -> Variant [const]
  Returns the metadata value set to the tab at index tab_idx using set_tab_metadata(). If no metadata was previously set, returns null by default.

- get_tab_title(tab_idx: int) -> String [const]
  Returns the title of the tab at index tab_idx. Tab titles default to the name of the indexed child node, but this can be overridden with set_tab_title().

- get_tab_tooltip(tab_idx: int) -> String [const]
  Returns the tooltip text of the tab at index tab_idx.

- is_tab_disabled(tab_idx: int) -> bool [const]
  Returns true if the tab at index tab_idx is disabled.

- is_tab_hidden(tab_idx: int) -> bool [const]
  Returns true if the tab at index tab_idx is hidden.

- select_next_available() -> bool
  Selects the first available tab with greater index than the currently selected. Returns true if tab selection changed.

- select_previous_available() -> bool
  Selects the first available tab with lower index than the currently selected. Returns true if tab selection changed.

- set_popup(popup: Node) -> void
  If set on a Popup node instance, a popup menu icon appears in the top-right corner of the TabContainer (setting it to null will make it go away). Clicking it will expand the Popup node.

- set_tab_button_icon(tab_idx: int, icon: Texture2D) -> void
  Sets the button icon from the tab at index tab_idx.

- set_tab_disabled(tab_idx: int, disabled: bool) -> void
  If disabled is true, disables the tab at index tab_idx, making it non-interactable.

- set_tab_hidden(tab_idx: int, hidden: bool) -> void
  If hidden is true, hides the tab at index tab_idx, making it disappear from the tab area.

- set_tab_icon(tab_idx: int, icon: Texture2D) -> void
  Sets an icon for the tab at index tab_idx.

- set_tab_icon_max_width(tab_idx: int, width: int) -> void
  Sets the maximum allowed width of the icon for the tab at index tab_idx. This limit is applied on top of the default size of the icon and on top of [theme_item icon_max_width]. The height is adjusted according to the icon's ratio.

- set_tab_metadata(tab_idx: int, metadata: Variant) -> void
  Sets the metadata value for the tab at index tab_idx, which can be retrieved later using get_tab_metadata().

- set_tab_title(tab_idx: int, title: String) -> void
  Sets a custom title for the tab at index tab_idx (tab titles default to the name of the indexed child node). Set it back to the child's name to make the tab default to it again.

- set_tab_tooltip(tab_idx: int, tooltip: String) -> void
  Sets a custom tooltip text for tab at index tab_idx. **Note:** By default, if the tooltip is empty and the tab text is truncated (not all characters fit into the tab), the title will be displayed as a tooltip. To hide the tooltip, assign " " as the tooltip text.

## Properties

- all_tabs_in_front: bool = false [set set_all_tabs_in_front; get is_all_tabs_in_front]
  If true, all tabs are drawn in front of the panel. If false, inactive tabs are drawn behind the panel.

- clip_tabs: bool = true [set set_clip_tabs; get get_clip_tabs]
  If true, tabs overflowing this node's width will be hidden, displaying two navigation buttons instead. Otherwise, this node's minimum size is updated so that all tabs are visible.

- current_tab: int = -1 [set set_current_tab; get get_current_tab]
  The current tab index. When set, this index's Control node's visible property is set to true and all others are set to false. A value of -1 means that no tab is selected.

- deselect_enabled: bool = false [set set_deselect_enabled; get get_deselect_enabled]
  If true, all tabs can be deselected so that no tab is selected. Click on the current_tab to deselect it. Only the tab header will be shown if no tabs are selected.

- drag_to_rearrange_enabled: bool = false [set set_drag_to_rearrange_enabled; get get_drag_to_rearrange_enabled]
  If true, tabs can be rearranged with mouse drag.

- switch_on_drag_hover: bool = true [set set_switch_on_drag_hover; get get_switch_on_drag_hover]
  If true, hovering over a tab while dragging something will switch to that tab. Does not have effect when hovering another tab to rearrange.

- tab_alignment: int (TabBar.AlignmentMode) = 0 [set set_tab_alignment; get get_tab_alignment]
  The position at which tabs will be placed.

- tab_focus_mode: int (Control.FocusMode) = 2 [set set_tab_focus_mode; get get_tab_focus_mode]
  The focus access mode for the internal TabBar node.

- tabs_position: int (TabContainer.TabPosition) = 0 [set set_tabs_position; get get_tabs_position]
  The horizontal alignment of the tabs.

- tabs_rearrange_group: int = -1 [set set_tabs_rearrange_group; get get_tabs_rearrange_group]
  TabContainers with the same rearrange group ID will allow dragging the tabs between them. Enable drag with drag_to_rearrange_enabled. Setting this to -1 will disable rearranging between TabContainers.

- tabs_visible: bool = true [set set_tabs_visible; get are_tabs_visible]
  If true, tabs are visible. If false, tabs' content and titles are hidden.

- use_hidden_tabs_for_min_size: bool = false [set set_use_hidden_tabs_for_min_size; get get_use_hidden_tabs_for_min_size]
  If true, child Control nodes that are hidden have their minimum size take into account in the total, instead of only the currently visible one.

## Signals

- active_tab_rearranged(idx_to: int)
  Emitted when the active tab is rearranged via mouse drag. See drag_to_rearrange_enabled.

- pre_popup_pressed()
  Emitted when the TabContainer's Popup button is clicked. See set_popup() for details.

- tab_button_pressed(tab: int)
  Emitted when the user clicks on the button icon on this tab.

- tab_changed(tab: int)
  Emitted when switching to another tab.

- tab_clicked(tab: int)
  Emitted when a tab is clicked, even if it is the current tab.

- tab_hovered(tab: int)
  Emitted when a tab is hovered by the mouse.

- tab_selected(tab: int)
  Emitted when a tab is selected via click, directional input, or script, even if it is the current tab.

## Constants

### Enum TabPosition

- POSITION_TOP = 0
  Places the tab bar at the top.

- POSITION_BOTTOM = 1
  Places the tab bar at the bottom. The tab bar's StyleBox will be flipped vertically.

- POSITION_MAX = 2
  Represents the size of the TabPosition enum.

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

- icon_max_width: int [constant] = 0
  The maximum allowed width of the tab's icon. This limit is applied on top of the default size of the icon, but before the value set with TabBar.set_tab_icon_max_width(). The height is adjusted according to the icon's ratio.

- icon_separation: int [constant] = 4
  Space between tab's name and its icon.

- outline_size: int [constant] = 0
  The size of the tab text outline. **Note:** If using a font with FontFile.multichannel_signed_distance_field enabled, its FontFile.msdf_pixel_range must be set to at least *twice* the value of [theme_item outline_size] for outline rendering to look correct. Otherwise, the outline may appear to be cut off earlier than intended.

- side_margin: int [constant] = 8
  The space at the left or right edges of the tab bar, accordingly with the current tab_alignment. The margin is ignored with TabBar.ALIGNMENT_RIGHT if the tabs are clipped (see clip_tabs) or a popup has been set (see set_popup()). The margin is always ignored with TabBar.ALIGNMENT_CENTER.

- tab_separation: int [constant] = 0
  The space between tabs in the tab bar.

- font: Font [font]
  The font used to draw tab names.

- font_size: int [font_size]
  Font size of the tab names.

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

- menu: Texture2D [icon]
  The icon for the menu button (see set_popup()).

- menu_highlight: Texture2D [icon]
  The icon for the menu button (see set_popup()) when it's being hovered with the cursor.

- panel: StyleBox [style]
  The style for the background fill.

- tab_disabled: StyleBox [style]
  The style of disabled tabs.

- tab_focus: StyleBox [style]
  StyleBox used when the TabBar is focused. The [theme_item tab_focus] StyleBox is displayed *over* the base StyleBox of the selected tab, so a partially transparent StyleBox should be used to ensure the base StyleBox remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- tab_hovered: StyleBox [style]
  The style of the currently hovered tab. **Note:** This style will be drawn with the same width as [theme_item tab_unselected] at minimum.

- tab_selected: StyleBox [style]
  The style of the currently selected tab.

- tab_unselected: StyleBox [style]
  The style of the other, unselected tabs.

- tabbar_background: StyleBox [style]
  The style for the background fill of the TabBar area.
