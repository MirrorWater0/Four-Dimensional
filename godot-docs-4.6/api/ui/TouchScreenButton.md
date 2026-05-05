# TouchScreenButton

## Meta

- Name: TouchScreenButton
- Source: TouchScreenButton.xml
- Inherits: Node2D
- Inheritance Chain: TouchScreenButton -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Button for touch screen devices for gameplay use.

## Description

TouchScreenButton allows you to create on-screen buttons for touch devices. It's intended for gameplay use, such as a unit you have to touch to move. Unlike Button, TouchScreenButton supports multitouch out of the box. Several TouchScreenButtons can be pressed at the same time with touch input. This node inherits from Node2D. Unlike with Control nodes, you cannot set anchors on it. If you want to create menus or user interfaces, you may want to use Button nodes instead. To make button nodes react to touch events, you can enable ProjectSettings.input_devices/pointing/emulate_mouse_from_touch in the Project Settings. You can configure TouchScreenButton to be visible only on touch devices, helping you develop your game both for desktop and mobile devices.

## Quick Reference

```
[methods]
is_pressed() -> bool [const]

[properties]
action: String = ""
bitmask: BitMap
passby_press: bool = false
shape: Shape2D
shape_centered: bool = true
shape_visible: bool = true
texture_normal: Texture2D
texture_pressed: Texture2D
visibility_mode: int (TouchScreenButton.VisibilityMode) = 0
```

## Methods

- is_pressed() -> bool [const]
  Returns true if this button is currently pressed.

## Properties

- action: String = "" [set set_action; get get_action]
  The button's action. Actions can be handled with InputEventAction.

- bitmask: BitMap [set set_bitmask; get get_bitmask]
  The button's bitmask.

- passby_press: bool = false [set set_passby_press; get is_passby_press_enabled]
  If true, the pressed and released signals are emitted whenever a pressed finger goes in and out of the button, even if the pressure started outside the active area of the button. **Note:** This is a "pass-by" (not "bypass") press mode.

- shape: Shape2D [set set_shape; get get_shape]
  The button's shape.

- shape_centered: bool = true [set set_shape_centered; get is_shape_centered]
  If true, the button's shape is centered in the provided texture. If no texture is used, this property has no effect.

- shape_visible: bool = true [set set_shape_visible; get is_shape_visible]
  If true, the button's shape is visible in the editor.

- texture_normal: Texture2D [set set_texture_normal; get get_texture_normal]
  The button's texture for the normal state.

- texture_pressed: Texture2D [set set_texture_pressed; get get_texture_pressed]
  The button's texture for the pressed state.

- visibility_mode: int (TouchScreenButton.VisibilityMode) = 0 [set set_visibility_mode; get get_visibility_mode]
  The button's visibility mode.

## Signals

- pressed()
  Emitted when the button is pressed (down).

- released()
  Emitted when the button is released (up).

## Constants

### Enum VisibilityMode

- VISIBILITY_ALWAYS = 0
  Always visible.

- VISIBILITY_TOUCHSCREEN_ONLY = 1
  Visible on touch screens only.
