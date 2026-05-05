# ScrollBar

## Meta

- Name: ScrollBar
- Source: ScrollBar.xml
- Inherits: Range
- Inheritance Chain: ScrollBar -> Range -> Control -> CanvasItem -> Node -> Object

## Brief Description

Abstract base class for scrollbars.

## Description

Abstract base class for scrollbars, typically used to navigate through content that extends beyond the visible area of a control. Scrollbars are Range-based controls.

## Quick Reference

```
[properties]
custom_step: float = -1.0
focus_mode: int (Control.FocusMode) = 3
step: float = 0.0
```

## Properties

- custom_step: float = -1.0 [set set_custom_step; get get_custom_step]
  Overrides the step used when clicking increment and decrement buttons or when using arrow keys when the ScrollBar is focused.

- focus_mode: int (Control.FocusMode) = 3 [set set_focus_mode; get get_focus_mode; override Control]

- step: float = 0.0 [set set_step; get get_step; override Range]

## Signals

- scrolling()
  Emitted when the scrollbar is being scrolled.

## Theme Items

- decrement: Texture2D [icon]
  Icon used as a button to scroll the ScrollBar left/up. Supports custom step using the ScrollBar.custom_step property.

- decrement_highlight: Texture2D [icon]
  Displayed when the mouse cursor hovers over the decrement button.

- decrement_pressed: Texture2D [icon]
  Displayed when the decrement button is being pressed.

- increment: Texture2D [icon]
  Icon used as a button to scroll the ScrollBar right/down. Supports custom step using the ScrollBar.custom_step property.

- increment_highlight: Texture2D [icon]
  Displayed when the mouse cursor hovers over the increment button.

- increment_pressed: Texture2D [icon]
  Displayed when the increment button is being pressed.

- grabber: StyleBox [style]
  Used as texture for the grabber, the draggable element representing current scroll.

- grabber_highlight: StyleBox [style]
  Used when the mouse hovers over the grabber.

- grabber_pressed: StyleBox [style]
  Used when the grabber is being dragged.

- scroll: StyleBox [style]
  Used as background of this ScrollBar.

- scroll_focus: StyleBox [style]
  Used as background when the ScrollBar has the GUI focus.
