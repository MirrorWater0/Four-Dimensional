# Popup

## Meta

- Name: Popup
- Source: Popup.xml
- Inherits: Window
- Inheritance Chain: Popup -> Window -> Viewport -> Node -> Object

## Brief Description

Base class for contextual windows and panels with fixed position.

## Description

Popup is a base class for contextual windows and panels with fixed position. It's a modal by default (see Window.popup_window) and provides methods for implementing custom popup behavior. **Note:** Popup is invisible by default. To make it visible, call one of the popup_* methods from Window on the node, such as Window.popup_centered_clamped().

## Quick Reference

```
[properties]
borderless: bool = true
maximize_disabled: bool = true
minimize_disabled: bool = true
popup_window: bool = true
popup_wm_hint: bool = true
transient: bool = true
unresizable: bool = true
visible: bool = false
wrap_controls: bool = true
```

## Properties

- borderless: bool = true [set set_flag; get get_flag; override Window]

- maximize_disabled: bool = true [set set_flag; get get_flag; override Window]

- minimize_disabled: bool = true [set set_flag; get get_flag; override Window]

- popup_window: bool = true [set set_flag; get get_flag; override Window]

- popup_wm_hint: bool = true [set set_flag; get get_flag; override Window]

- transient: bool = true [set set_transient; get is_transient; override Window]

- unresizable: bool = true [set set_flag; get get_flag; override Window]

- visible: bool = false [set set_visible; get is_visible; override Window]

- wrap_controls: bool = true [set set_wrap_controls; get is_wrapping_controls; override Window]

## Signals

- popup_hide()
  Emitted when the popup is hidden.
