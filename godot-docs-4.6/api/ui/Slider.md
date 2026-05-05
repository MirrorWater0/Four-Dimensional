# Slider

## Meta

- Name: Slider
- Source: Slider.xml
- Inherits: Range
- Inheritance Chain: Slider -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

Abstract base class for sliders.

## Description

Abstract base class for sliders, used to adjust a value by moving a grabber along a horizontal or vertical axis. Sliders are Range-based controls.

## Quick Reference

```
[properties]
editable: bool = true
focus_mode: int (Control.FocusMode) = 2
scrollable: bool = true
step: float = 1.0
tick_count: int = 0
ticks_on_borders: bool = false
ticks_position: int (Slider.TickPosition) = 0
```

## Properties

- editable: bool = true [set set_editable; get is_editable]
  If true, the slider can be interacted with. If false, the value can be changed only by code.

- focus_mode: int (Control.FocusMode) = 2 [set set_focus_mode; get get_focus_mode; override Control]

- scrollable: bool = true [set set_scrollable; get is_scrollable]
  If true, the value can be changed using the mouse wheel.

- step: float = 1.0 [set set_step; get get_step; override Range]

- tick_count: int = 0 [set set_ticks; get get_ticks]
  Number of ticks displayed on the slider, including border ticks. Ticks are uniformly-distributed value markers.

- ticks_on_borders: bool = false [set set_ticks_on_borders; get get_ticks_on_borders]
  If true, the slider will display ticks for minimum and maximum values.

- ticks_position: int (Slider.TickPosition) = 0 [set set_ticks_position; get get_ticks_position]
  Sets the position of the ticks. See TickPosition for details.

## Signals

- drag_ended(value_changed: bool)
  Emitted when the grabber stops being dragged. If value_changed is true, Range.value is different from the value when the dragging was started.

- drag_started()
  Emitted when the grabber starts being dragged. This is emitted before the corresponding Range.value_changed signal.

## Constants

### Enum TickPosition

- TICK_POSITION_BOTTOM_RIGHT = 0
  Places the ticks at the bottom of the HSlider, or right of the VSlider.

- TICK_POSITION_TOP_LEFT = 1
  Places the ticks at the top of the HSlider, or left of the VSlider.

- TICK_POSITION_BOTH = 2
  Places the ticks at the both sides of the slider.

- TICK_POSITION_CENTER = 3
  Places the ticks at the center of the slider.

## Theme Items

- center_grabber: int [constant] = 0
  Boolean constant. If 1, the grabber texture size will be ignored and it will fit within slider's bounds based only on its center position.

- grabber_offset: int [constant] = 0
  Vertical or horizontal offset of the grabber.

- tick_offset: int [constant] = 0
  Vertical or horizontal offset of the ticks. The offset is reversed for top or left ticks.

- grabber: Texture2D [icon]
  The texture for the grabber (the draggable element).

- grabber_disabled: Texture2D [icon]
  The texture for the grabber when it's disabled.

- grabber_highlight: Texture2D [icon]
  The texture for the grabber when it's focused.

- tick: Texture2D [icon]
  The texture for the ticks, visible when Slider.tick_count is greater than 0.

- grabber_area: StyleBox [style]
  The background of the area to the left or bottom of the grabber.

- grabber_area_highlight: StyleBox [style]
  The background of the area to the left or bottom of the grabber that displays when it's being hovered or focused.

- slider: StyleBox [style]
  The background for the whole slider. Affects the height or width of the [theme_item grabber_area].
