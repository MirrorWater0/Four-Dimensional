# ColorPicker

## Meta

- Name: ColorPicker
- Source: ColorPicker.xml
- Inherits: VBoxContainer
- Inheritance Chain: ColorPicker -> VBoxContainer -> BoxContainer -> Container -> Control -> CanvasItem -> Node -> Object

## Brief Description

A widget that provides an interface for selecting or modifying a color.

## Description

A widget that provides an interface for selecting or modifying a color. It can optionally provide functionalities like a color sampler (eyedropper), color modes, and presets. **Note:** This control is the color picker widget itself. You can use a ColorPickerButton instead if you need a button that brings up a ColorPicker in a popup.

## Quick Reference

```
[methods]
add_preset(color: Color) -> void
add_recent_preset(color: Color) -> void
erase_preset(color: Color) -> void
erase_recent_preset(color: Color) -> void
get_presets() -> PackedColorArray [const]
get_recent_presets() -> PackedColorArray [const]

[properties]
can_add_swatches: bool = true
color: Color = Color(1, 1, 1, 1)
color_mode: int (ColorPicker.ColorModeType) = 0
color_modes_visible: bool = true
deferred_mode: bool = false
edit_alpha: bool = true
edit_intensity: bool = true
hex_visible: bool = true
picker_shape: int (ColorPicker.PickerShapeType) = 0
presets_visible: bool = true
sampler_visible: bool = true
sliders_visible: bool = true
```

## Tutorials

- [Tween Interpolation Demo](https://godotengine.org/asset-library/asset/2733)

## Methods

- add_preset(color: Color) -> void
  Adds the given color to a list of color presets. The presets are displayed in the color picker and the user will be able to select them. **Note:** The presets list is only for *this* color picker.

- add_recent_preset(color: Color) -> void
  Adds the given color to a list of color recent presets so that it can be picked later. Recent presets are the colors that were picked recently, a new preset is automatically created and added to recent presets when you pick a new color. **Note:** The recent presets list is only for *this* color picker.

- erase_preset(color: Color) -> void
  Removes the given color from the list of color presets of this color picker.

- erase_recent_preset(color: Color) -> void
  Removes the given color from the list of color recent presets of this color picker.

- get_presets() -> PackedColorArray [const]
  Returns the list of colors in the presets of the color picker.

- get_recent_presets() -> PackedColorArray [const]
  Returns the list of colors in the recent presets of the color picker.

## Properties

- can_add_swatches: bool = true [set set_can_add_swatches; get are_swatches_enabled]
  If true, it's possible to add presets under Swatches. If false, the button to add presets is disabled.

- color: Color = Color(1, 1, 1, 1) [set set_pick_color; get get_pick_color]
  The currently selected color.

- color_mode: int (ColorPicker.ColorModeType) = 0 [set set_color_mode; get get_color_mode]
  The currently selected color mode.

- color_modes_visible: bool = true [set set_modes_visible; get are_modes_visible]
  If true, the color mode buttons are visible.

- deferred_mode: bool = false [set set_deferred_mode; get is_deferred_mode]
  If true, the color will apply only after the user releases the mouse button, otherwise it will apply immediately even in mouse motion event (which can cause performance issues).

- edit_alpha: bool = true [set set_edit_alpha; get is_editing_alpha]
  If true, shows an alpha channel slider (opacity).

- edit_intensity: bool = true [set set_edit_intensity; get is_editing_intensity]
  If true, shows an intensity slider. The intensity is applied as follows: convert the color to linear encoding, multiply it by 2 ** intensity, and then convert it back to nonlinear sRGB encoding.

- hex_visible: bool = true [set set_hex_visible; get is_hex_visible]
  If true, the hex color code input field is visible.

- picker_shape: int (ColorPicker.PickerShapeType) = 0 [set set_picker_shape; get get_picker_shape]
  The shape of the color space view.

- presets_visible: bool = true [set set_presets_visible; get are_presets_visible]
  If true, the Swatches and Recent Colors presets are visible.

- sampler_visible: bool = true [set set_sampler_visible; get is_sampler_visible]
  If true, the color sampler and color preview are visible.

- sliders_visible: bool = true [set set_sliders_visible; get are_sliders_visible]
  If true, the color sliders are visible.

## Signals

- color_changed(color: Color)
  Emitted when the color is changed.

- preset_added(color: Color)
  Emitted when a preset is added.

- preset_removed(color: Color)
  Emitted when a preset is removed.

## Constants

### Enum ColorModeType

- MODE_RGB = 0
  Allows editing the color with Red/Green/Blue sliders in sRGB color space.

- MODE_HSV = 1
  Allows editing the color with Hue/Saturation/Value sliders.

- MODE_RAW = 2

- MODE_LINEAR = 2
  Allows editing the color with Red/Green/Blue sliders in linear color space.

- MODE_OKHSL = 3
  Allows editing the color with Hue/Saturation/Lightness sliders. OKHSL is a new color space similar to HSL but that better match perception by leveraging the Oklab color space which is designed to be simple to use, while doing a good job at predicting perceived lightness, chroma and hue. [Okhsv and Okhsl color spaces](https://bottosson.github.io/posts/colorpicker/)

### Enum PickerShapeType

- SHAPE_HSV_RECTANGLE = 0
  HSV Color Model rectangle color space.

- SHAPE_HSV_WHEEL = 1
  HSV Color Model rectangle color space with a wheel.

- SHAPE_VHS_CIRCLE = 2
  HSV Color Model circle color space. Use Saturation as a radius.

- SHAPE_OKHSL_CIRCLE = 3
  HSL OK Color Model circle color space.

- SHAPE_NONE = 4
  The color space shape and the shape select button are hidden. Can't be selected from the shapes popup.

- SHAPE_OK_HS_RECTANGLE = 5
  OKHSL Color Model rectangle with constant lightness.

- SHAPE_OK_HL_RECTANGLE = 6
  OKHSL Color Model rectangle with constant saturation.

## Theme Items

- focused_not_editing_cursor_color: Color [color] = Color(1, 1, 1, 0.275)
  Color of rectangle or circle drawn when a picker shape part is focused but not editable via keyboard or joypad. Displayed *over* the picker shape, so a partially transparent color should be used to ensure the picker shape remains visible.

- center_slider_grabbers: int [constant] = 1
  Overrides the [theme_item Slider.center_grabber] theme property of the sliders.

- h_width: int [constant] = 30
  The width of the hue selection slider.

- label_width: int [constant] = 10
  The minimum width of the color labels next to sliders.

- margin: int [constant] = 4
  The margin around the ColorPicker.

- sv_height: int [constant] = 256
  The height of the saturation-value selection box.

- sv_width: int [constant] = 256
  The width of the saturation-value selection box.

- add_preset: Texture2D [icon]
  The icon for the "Add Preset" button.

- bar_arrow: Texture2D [icon]
  The texture for the arrow grabber.

- color_hue: Texture2D [icon]
  Custom texture for the hue selection slider on the right.

- color_script: Texture2D [icon]
  The icon for the button that switches color text to hexadecimal.

- expanded_arrow: Texture2D [icon]
  The icon for color preset drop down menu when expanded.

- folded_arrow: Texture2D [icon]
  The icon for color preset drop down menu when folded.

- menu_option: Texture2D [icon]
  The icon for color preset option menu.

- overbright_indicator: Texture2D [icon]
  The indicator used to signalize that the color value is outside the 0-1 range.

- picker_cursor: Texture2D [icon]
  The image displayed over the color box/circle (depending on the picker_shape), marking the currently selected color.

- picker_cursor_bg: Texture2D [icon]
  The fill image displayed behind the picker cursor.

- sample_bg: Texture2D [icon]
  Background panel for the color preview box (visible when the color is translucent).

- sample_revert: Texture2D [icon]
  The icon for the revert button (visible on the middle of the "old" color when it differs from the currently selected color). This icon is modulated with a dark color if the "old" color is bright enough, so the icon should be bright to ensure visibility in both scenarios.

- screen_picker: Texture2D [icon]
  The icon for the screen color picker button.

- shape_circle: Texture2D [icon]
  The icon for circular picker shapes.

- shape_rect: Texture2D [icon]
  The icon for rectangular picker shapes.

- shape_rect_wheel: Texture2D [icon]
  The icon for rectangular wheel picker shapes.

- picker_focus_circle: StyleBox [style]
  The StyleBox used when the circle-shaped part of the picker is focused. Displayed *over* the picker shape, so a partially transparent StyleBox should be used to ensure the picker shape remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- picker_focus_rectangle: StyleBox [style]
  The StyleBox used when the rectangle-shaped part of the picker is focused. Displayed *over* the picker shape, so a partially transparent StyleBox should be used to ensure the picker shape remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.

- sample_focus: StyleBox [style]
  The StyleBox used for the old color sample part when it is focused. Displayed *over* the sample, so a partially transparent StyleBox should be used to ensure the picker shape remains visible. A StyleBox that represents an outline or an underline works well for this purpose. To disable the focus visual effect, assign a StyleBoxEmpty resource. Note that disabling the focus visual effect will harm keyboard/controller navigation usability, so this is not recommended for accessibility reasons.
