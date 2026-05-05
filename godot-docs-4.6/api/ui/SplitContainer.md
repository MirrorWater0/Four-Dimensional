# SplitContainer

## Meta

- Name: SplitContainer
- Source: SplitContainer.xml
- Inherits: Container
- Inheritance Chain: SplitContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container that arranges child controls horizontally or vertically and provides grabbers for adjusting the split ratios between them.

## Description

A container that arranges child controls horizontally or vertically and creates grabbers between them. The grabbers can be dragged around to change the size relations between the child controls.

## Quick Reference

```
[methods]
clamp_split_offset(priority_index: int = 0) -> void
get_drag_area_control() -> Control
get_drag_area_controls() -> Control[]

[properties]
collapsed: bool = false
drag_area_highlight_in_editor: bool = false
drag_area_margin_begin: int = 0
drag_area_margin_end: int = 0
drag_area_offset: int = 0
dragger_visibility: int (SplitContainer.DraggerVisibility) = 0
dragging_enabled: bool = true
split_offset: int = 0
split_offsets: PackedInt32Array = PackedInt32Array(0)
touch_dragger_enabled: bool = false
vertical: bool = false
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Methods

- clamp_split_offset(priority_index: int = 0) -> void
  Clamps the split_offsets values to ensure they are within valid ranges and do not overlap with each other. When overlaps occur, this method prioritizes one split offset (at index priority_index) by clamping any overlapping split offsets to it.

- get_drag_area_control() -> Control
  Returns the drag area Control. For example, you can move a pre-configured button into the drag area Control so that it rides along with the split bar. Try setting the Button anchors to center prior to the reparent() call.


```
  $BarnacleButton.reparent($SplitContainer.get_drag_area_control())

```
  **Note:** The drag area Control is drawn over the SplitContainer's children, so CanvasItem draw objects called from the Control and children added to the Control will also appear over the SplitContainer's children. Try setting Control.mouse_filter of custom children to Control.MOUSE_FILTER_IGNORE to prevent blocking the mouse from dragging if desired. **Warning:** This is a required internal node, removing and freeing it may cause a crash.

- get_drag_area_controls() -> Control[]
  Returns an Array of the drag area Controls. These are the interactable Control nodes between each child. For example, this can be used to add a pre-configured button to a drag area Control so that it rides along with the split bar. Try setting the Button anchors to center prior to the Node.reparent() call.


```
  $BarnacleButton.reparent($SplitContainer.get_drag_area_controls()0)

```
  **Note:** The drag area Controls are drawn over the SplitContainer's children, so CanvasItem draw objects called from a drag area and children added to it will also appear over the SplitContainer's children. Try setting Control.mouse_filter of custom children to Control.MOUSE_FILTER_IGNORE to prevent blocking the mouse from dragging if desired. **Warning:** These are required internal nodes, removing or freeing them may cause a crash.

## Properties

- collapsed: bool = false [set set_collapsed; get is_collapsed]
  If true, the draggers will be disabled and the children will be sized as if all split_offsets were 0.

- drag_area_highlight_in_editor: bool = false [set set_drag_area_highlight_in_editor; get is_drag_area_highlight_in_editor_enabled]
  Highlights the drag area Rect2 so you can see where it is during development. The drag area is gold if dragging_enabled is true, and red if false.

- drag_area_margin_begin: int = 0 [set set_drag_area_margin_begin; get get_drag_area_margin_begin]
  Reduces the size of the drag area and split bar [theme_item split_bar_background] at the beginning of the container.

- drag_area_margin_end: int = 0 [set set_drag_area_margin_end; get get_drag_area_margin_end]
  Reduces the size of the drag area and split bar [theme_item split_bar_background] at the end of the container.

- drag_area_offset: int = 0 [set set_drag_area_offset; get get_drag_area_offset]
  Shifts the drag area in the axis of the container to prevent the drag area from overlapping the ScrollBar or other selectable Control of a child node.

- dragger_visibility: int (SplitContainer.DraggerVisibility) = 0 [set set_dragger_visibility; get get_dragger_visibility]
  Determines the dragger's visibility. This property does not determine whether dragging is enabled or not. Use dragging_enabled for that.

- dragging_enabled: bool = true [set set_dragging_enabled; get is_dragging_enabled]
  Enables or disables split dragging.

- split_offset: int = 0 [set set_split_offset; get get_split_offset]
  The first element of split_offsets.

- split_offsets: PackedInt32Array = PackedInt32Array(0) [set set_split_offsets; get get_split_offsets]
  Offsets for each dragger in pixels. Each one is the offset of the split between the Control nodes before and after the dragger, with 0 being the default position. The default position is based on the Control nodes expand flags and minimum sizes. See Control.size_flags_horizontal, Control.size_flags_vertical, and Control.size_flags_stretch_ratio. If none of the Control nodes before the dragger are expanded, the default position will be at the start of the SplitContainer. If none of the Control nodes after the dragger are expanded, the default position will be at the end of the SplitContainer. If the dragger is in between expanded Control nodes, the default position will be in the middle, based on the Control.size_flags_stretch_ratios and minimum sizes. **Note:** If the split offsets cause Control nodes to overlap, the first split will take priority when resolving the positions.

- touch_dragger_enabled: bool = false [set set_touch_dragger_enabled; get is_touch_dragger_enabled]
  If true, a touch-friendly drag handle will be enabled for better usability on smaller screens. Unlike the standard grabber, this drag handle overlaps the SplitContainer's children and does not affect their minimum separation. The standard grabber will no longer be drawn when this option is enabled.

- vertical: bool = false [set set_vertical; get is_vertical]
  If true, the SplitContainer will arrange its children vertically, rather than horizontally. Can't be changed when using HSplitContainer and VSplitContainer.

## Signals

- drag_ended()
  Emitted when the user ends dragging.

- drag_started()
  Emitted when the user starts dragging.

- dragged(offset: int)
  Emitted when any dragger is dragged by user.

## Constants

### Enum DraggerVisibility

- DRAGGER_VISIBLE = 0
  The split dragger icon is always visible when [theme_item autohide] is false, otherwise visible only when the cursor hovers it. The size of the grabber icon determines the minimum [theme_item separation]. The dragger icon is automatically hidden if the length of the grabber icon is longer than the split bar.

- DRAGGER_HIDDEN = 1
  The split dragger icon is never visible regardless of the value of [theme_item autohide]. The size of the grabber icon determines the minimum [theme_item separation].

- DRAGGER_HIDDEN_COLLAPSED = 2
  The split dragger icon is not visible, and the split bar is collapsed to zero thickness.

## Theme Items

- touch_dragger_color: Color [color] = Color(1, 1, 1, 0.3)
  The color of the touch dragger.

- touch_dragger_hover_color: Color [color] = Color(1, 1, 1, 0.6)
  The color of the touch dragger when hovered.

- touch_dragger_pressed_color: Color [color] = Color(1, 1, 1, 1)
  The color of the touch dragger when pressed.

- autohide: int [constant] = 1
  Boolean value. If 1 (true), the grabbers will hide automatically when they aren't under the cursor. If 0 (false), the grabbers are always visible. The dragger_visibility must be DRAGGER_VISIBLE.

- minimum_grab_thickness: int [constant] = 6
  The minimum thickness of the area users can click on to grab a split bar. This ensures that the split bar can still be dragged if [theme_item separation] or [theme_item h_grabber] / [theme_item v_grabber]'s size is too narrow to easily select.

- separation: int [constant] = 12
  The split bar thickness, i.e., the gap between each child of the container. This is overridden by the size of the grabber icon if dragger_visibility is set to DRAGGER_VISIBLE, or DRAGGER_HIDDEN, and [theme_item separation] is smaller than the size of the grabber icon in the same axis. **Note:** To obtain [theme_item separation] values less than the size of the grabber icon, for example a 1 px hairline, set [theme_item h_grabber] or [theme_item v_grabber] to a new ImageTexture, which effectively sets the grabber icon size to 0 px.

- grabber: Texture2D [icon]
  The icon used for the grabbers drawn in the separations. This is only used in HSplitContainer and VSplitContainer. For SplitContainer, see [theme_item h_grabber] and [theme_item v_grabber] instead.

- h_grabber: Texture2D [icon]
  The icon used for the grabbers drawn in the separations when vertical is false.

- h_touch_dragger: Texture2D [icon]
  The icon used for the drag handle when touch_dragger_enabled is true and vertical is false.

- touch_dragger: Texture2D [icon]
  The icon used for the drag handle when touch_dragger_enabled is true. This is only used in HSplitContainer and VSplitContainer. For SplitContainer, see [theme_item h_touch_dragger] and [theme_item v_touch_dragger] instead.

- v_grabber: Texture2D [icon]
  The icon used for the grabbers drawn in the separations when vertical is true.

- v_touch_dragger: Texture2D [icon]
  The icon used for the drag handle when touch_dragger_enabled is true and vertical is true.

- split_bar_background: StyleBox [style]
  Determines the background of the split bar if its thickness is greater than zero.
