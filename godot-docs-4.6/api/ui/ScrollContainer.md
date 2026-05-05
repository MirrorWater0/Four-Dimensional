# ScrollContainer

## Meta

- Name: ScrollContainer
- Source: ScrollContainer.xml
- Inherits: Container
- Inheritance Chain: ScrollContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A container used to provide scrollbars to a child control when needed.

## Description

A container used to provide a child control with scrollbars when needed. Scrollbars will automatically be drawn at the right (for vertical) or bottom (for horizontal) and will enable dragging to move the viewable Control (and its children) within the ScrollContainer. Scrollbars will also automatically resize the grabber based on the Control.custom_minimum_size of the Control relative to the ScrollContainer.

## Quick Reference

```
[methods]
ensure_control_visible(control: Control) -> void
get_h_scroll_bar() -> HScrollBar
get_v_scroll_bar() -> VScrollBar

[properties]
clip_contents: bool = true
draw_focus_border: bool = false
follow_focus: bool = false
horizontal_scroll_mode: int (ScrollContainer.ScrollMode) = 1
scroll_deadzone: int = 0
scroll_hint_mode: int (ScrollContainer.ScrollHintMode) = 0
scroll_horizontal: int = 0
scroll_horizontal_custom_step: float = -1.0
scroll_vertical: int = 0
scroll_vertical_custom_step: float = -1.0
tile_scroll_hint: bool = false
vertical_scroll_mode: int (ScrollContainer.ScrollMode) = 1
```

## Tutorials

- [Using Containers]($DOCS_URL/tutorials/ui/gui_containers.html)

## Methods

- ensure_control_visible(control: Control) -> void
  Ensures the given control is visible (must be a direct or indirect child of the ScrollContainer). Used by follow_focus. **Note:** This will not work on a node that was just added during the same frame. If you want to scroll to a newly added child, you must wait until the next frame using SceneTree.process_frame:


```
  add_child(child_node)
  await get_tree().process_frame
  ensure_control_visible(child_node)

```

- get_h_scroll_bar() -> HScrollBar
  Returns the horizontal scrollbar HScrollBar of this ScrollContainer. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to disable or hide a scrollbar, you can use horizontal_scroll_mode.

- get_v_scroll_bar() -> VScrollBar
  Returns the vertical scrollbar VScrollBar of this ScrollContainer. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to disable or hide a scrollbar, you can use vertical_scroll_mode.

## Properties

- clip_contents: bool = true [set set_clip_contents; get is_clipping_contents; override Control]

- draw_focus_border: bool = false [set set_draw_focus_border; get get_draw_focus_border]
  If true, [theme_item focus] is drawn when the ScrollContainer or one of its descendant nodes is focused.

- follow_focus: bool = false [set set_follow_focus; get is_following_focus]
  If true, the ScrollContainer will automatically scroll to focused children (including indirect children) to make sure they are fully visible.

- horizontal_scroll_mode: int (ScrollContainer.ScrollMode) = 1 [set set_horizontal_scroll_mode; get get_horizontal_scroll_mode]
  Controls whether horizontal scrollbar can be used and when it should be visible.

- scroll_deadzone: int = 0 [set set_deadzone; get get_deadzone]
  Deadzone for touch scrolling. Lower deadzone makes the scrolling more sensitive.

- scroll_hint_mode: int (ScrollContainer.ScrollHintMode) = 0 [set set_scroll_hint_mode; get get_scroll_hint_mode]
  The way which scroll hints (indicators that show that the content can still be scrolled in a certain direction) will be shown. **Note:** Hints won't be shown if the content can be scrolled both vertically and horizontally.

- scroll_horizontal: int = 0 [set set_h_scroll; get get_h_scroll]
  The current horizontal scroll value. **Note:** If you are setting this value in the Node._ready() function or earlier, it needs to be wrapped with Object.set_deferred(), since scroll bar's Range.max_value is not initialized yet.

```
func _ready():
    set_deferred("scroll_horizontal", 600)
```

- scroll_horizontal_custom_step: float = -1.0 [set set_horizontal_custom_step; get get_horizontal_custom_step]
  Overrides the ScrollBar.custom_step used when clicking the internal scroll bar's horizontal increment and decrement buttons or when using arrow keys when the ScrollBar is focused.

- scroll_vertical: int = 0 [set set_v_scroll; get get_v_scroll]
  The current vertical scroll value. **Note:** Setting it early needs to be deferred, just like in scroll_horizontal.

```
func _ready():
    set_deferred("scroll_vertical", 600)
```

- scroll_vertical_custom_step: float = -1.0 [set set_vertical_custom_step; get get_vertical_custom_step]
  Overrides the ScrollBar.custom_step used when clicking the internal scroll bar's vertical increment and decrement buttons or when using arrow keys when the ScrollBar is focused.

- tile_scroll_hint: bool = false [set set_tile_scroll_hint; get is_scroll_hint_tiled]
  If true, the scroll hint texture will be tiled instead of stretched. See scroll_hint_mode.

- vertical_scroll_mode: int (ScrollContainer.ScrollMode) = 1 [set set_vertical_scroll_mode; get get_vertical_scroll_mode]
  Controls whether vertical scrollbar can be used and when it should be visible.

## Signals

- scroll_ended()
  Emitted when scrolling stops when dragging the scrollable area *with a touch event*. This signal is *not* emitted when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events. **Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when ProjectSettings.input_devices/pointing/emulate_touch_from_mouse is enabled.

- scroll_started()
  Emitted when scrolling starts when dragging the scrollable area w*ith a touch event*. This signal is *not* emitted when scrolling by dragging the scrollbar, scrolling with the mouse wheel or scrolling with keyboard/gamepad events. **Note:** This signal is only emitted on Android or iOS, or on desktop/web platforms when ProjectSettings.input_devices/pointing/emulate_touch_from_mouse is enabled.

## Constants

### Enum ScrollMode

- SCROLL_MODE_DISABLED = 0
  Scrolling disabled, scrollbar will be invisible.

- SCROLL_MODE_AUTO = 1
  Scrolling enabled, scrollbar will be visible only if necessary, i.e. container's content is bigger than the container.

- SCROLL_MODE_SHOW_ALWAYS = 2
  Scrolling enabled, scrollbar will be always visible.

- SCROLL_MODE_SHOW_NEVER = 3
  Scrolling enabled, scrollbar will be hidden.

- SCROLL_MODE_RESERVE = 4
  Combines SCROLL_MODE_AUTO and SCROLL_MODE_SHOW_ALWAYS. The scrollbar is only visible if necessary, but the content size is adjusted as if it was always visible. It's useful for ensuring that content size stays the same regardless if the scrollbar is visible.

### Enum ScrollHintMode

- SCROLL_HINT_MODE_DISABLED = 0
  Scroll hints will never be shown.

- SCROLL_HINT_MODE_ALL = 1
  Scroll hints will be shown at the top and bottom (if vertical), or left and right (if horizontal).

- SCROLL_HINT_MODE_TOP_AND_LEFT = 2
  Scroll hints will be shown at the top (if vertical), or the left (if horizontal).

- SCROLL_HINT_MODE_BOTTOM_AND_RIGHT = 3
  Scroll hints will be shown at the bottom (if horizontal), or the right (if horizontal).

## Theme Items

- scroll_hint_horizontal_color: Color [color] = Color(0, 0, 0, 1)
  Color used to modulate the [theme_item scroll_hint_horizontal] texture.

- scroll_hint_vertical_color: Color [color] = Color(0, 0, 0, 1)
  Color used to modulate the [theme_item scroll_hint_vertical] texture.

- scrollbar_h_separation: int [constant] = 0
  The space between the ScrollContainer's vertical scroll bar and its content, in pixels. No space will be added when the content's minimum size is larger than the ScrollContainer's size.

- scrollbar_v_separation: int [constant] = 0
  The space between the ScrollContainer's horizontal scroll bar and its content, in pixels. No space will be added when the content's minimum size is larger than the ScrollContainer's size.

- scroll_hint_horizontal: Texture2D [icon]
  The indicator that will be shown when the content can still be scrolled horizontally. See scroll_hint_mode.

- scroll_hint_vertical: Texture2D [icon]
  The indicator that will be shown when the content can still be scrolled vertically. See scroll_hint_mode.

- focus: StyleBox [style]
  The focus border StyleBox of the ScrollContainer. Only used if draw_focus_border is true.

- panel: StyleBox [style]
  The background StyleBox of the ScrollContainer.
