# ColorPickerButton

## Meta

- Name: ColorPickerButton
- Source: ColorPickerButton.xml
- Inherits: Button
- Inheritance Chain: ColorPickerButton -> Button -> BaseButton -> Control -> CanvasItem -> Node -> Object

## Brief Description

A button that brings up a ColorPicker when pressed.

## Description

Encapsulates a ColorPicker, making it accessible by pressing a button. Pressing the button will toggle the ColorPicker's visibility. See also BaseButton which contains common properties and methods associated with this node. **Note:** By default, the button may not be wide enough for the color preview swatch to be visible. Make sure to set Control.custom_minimum_size to a big enough value to give the button enough space.

## Quick Reference

```
[methods]
get_picker() -> ColorPicker
get_popup() -> PopupPanel

[properties]
color: Color = Color(0, 0, 0, 1)
edit_alpha: bool = true
edit_intensity: bool = true
toggle_mode: bool = true
```

## Tutorials

- [2D GD Paint Demo](https://godotengine.org/asset-library/asset/2768)
- [GUI Drag And Drop Demo](https://godotengine.org/asset-library/asset/2767)

## Methods

- get_picker() -> ColorPicker
  Returns the ColorPicker that this node toggles. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their CanvasItem.visible property.

- get_popup() -> PopupPanel
  Returns the control's PopupPanel which allows you to connect to popup signals. This allows you to handle events when the ColorPicker is shown or hidden. **Warning:** This is a required internal node, removing and freeing it may cause a crash. If you wish to hide it or any of its children, use their Window.visible property.

## Properties

- color: Color = Color(0, 0, 0, 1) [set set_pick_color; get get_pick_color]
  The currently selected color.

- edit_alpha: bool = true [set set_edit_alpha; get is_editing_alpha]
  If true, the alpha channel in the displayed ColorPicker will be visible.

- edit_intensity: bool = true [set set_edit_intensity; get is_editing_intensity]
  If true, the intensity slider in the displayed ColorPicker will be visible.

- toggle_mode: bool = true [set set_toggle_mode; get is_toggle_mode; override BaseButton]

## Signals

- color_changed(color: Color)
  Emitted when the color changes.

- picker_created()
  Emitted when the ColorPicker is created (the button is pressed for the first time).

- popup_closed()
  Emitted when the ColorPicker is closed.

## Theme Items

- bg: Texture2D [icon]
  The background of the color preview rect on the button.
