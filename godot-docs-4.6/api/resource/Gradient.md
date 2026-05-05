# Gradient

## Meta

- Name: Gradient
- Source: Gradient.xml
- Inherits: Resource
- Inheritance Chain: Gradient -> Resource -> RefCounted -> Object

## Brief Description

A color transition.

## Description

This resource describes a color transition by defining a set of colored points and how to interpolate between them. See also Curve which supports more complex easing methods, but does not support colors.

## Quick Reference

```
[methods]
add_point(offset: float, color: Color) -> void
get_color(point: int) -> Color
get_offset(point: int) -> float
get_point_count() -> int [const]
remove_point(point: int) -> void
reverse() -> void
sample(offset: float) -> Color
set_color(point: int, color: Color) -> void
set_offset(point: int, offset: float) -> void

[properties]
colors: PackedColorArray = PackedColorArray(0, 0, 0, 1, 1, 1, 1, 1)
interpolation_color_space: int (Gradient.ColorSpace) = 0
interpolation_mode: int (Gradient.InterpolationMode) = 0
offsets: PackedFloat32Array = PackedFloat32Array(0, 1)
```

## Methods

- add_point(offset: float, color: Color) -> void
  Adds the specified color to the gradient, with the specified offset.

- get_color(point: int) -> Color
  Returns the color of the gradient color at index point.

- get_offset(point: int) -> float
  Returns the offset of the gradient color at index point.

- get_point_count() -> int [const]
  Returns the number of colors in the gradient.

- remove_point(point: int) -> void
  Removes the color at index point.

- reverse() -> void
  Reverses/mirrors the gradient. **Note:** This method mirrors all points around the middle of the gradient, which may produce unexpected results when interpolation_mode is set to GRADIENT_INTERPOLATE_CONSTANT.

- sample(offset: float) -> Color
  Returns the interpolated color specified by offset. offset should be between 0.0 and 1.0 (inclusive). Using a value lower than 0.0 will return the same color as 0.0, and using a value higher than 1.0 will return the same color as 1.0. If your input value is not within this range, consider using @GlobalScope.remap() on the input value with output values set to 0.0 and 1.0.

- set_color(point: int, color: Color) -> void
  Sets the color of the gradient color at index point.

- set_offset(point: int, offset: float) -> void
  Sets the offset for the gradient color at index point.

## Properties

- colors: PackedColorArray = PackedColorArray(0, 0, 0, 1, 1, 1, 1, 1) [set set_colors; get get_colors]
  Gradient's colors as a PackedColorArray. **Note:** Setting this property updates all colors at once. To update any color individually use set_color().

- interpolation_color_space: int (Gradient.ColorSpace) = 0 [set set_interpolation_color_space; get get_interpolation_color_space]
  The color space used to interpolate between points of the gradient. It does not affect the returned colors, which will always use nonlinear sRGB encoding. **Note:** This setting has no effect when interpolation_mode is set to GRADIENT_INTERPOLATE_CONSTANT.

- interpolation_mode: int (Gradient.InterpolationMode) = 0 [set set_interpolation_mode; get get_interpolation_mode]
  The algorithm used to interpolate between points of the gradient.

- offsets: PackedFloat32Array = PackedFloat32Array(0, 1) [set set_offsets; get get_offsets]
  Gradient's offsets as a PackedFloat32Array. **Note:** Setting this property updates all offsets at once. To update any offset individually use set_offset().

## Constants

### Enum InterpolationMode

- GRADIENT_INTERPOLATE_LINEAR = 0
  Linear interpolation.

- GRADIENT_INTERPOLATE_CONSTANT = 1
  Constant interpolation, color changes abruptly at each point and stays uniform between. This might cause visible aliasing when used for a gradient texture in some cases.

- GRADIENT_INTERPOLATE_CUBIC = 2
  Cubic interpolation.

### Enum ColorSpace

- GRADIENT_COLOR_SPACE_SRGB = 0
  sRGB color space.

- GRADIENT_COLOR_SPACE_LINEAR_SRGB = 1
  Linear sRGB color space.

- GRADIENT_COLOR_SPACE_OKLAB = 2
  Oklab(https://bottosson.github.io/posts/oklab/) color space. This color space provides a smooth and uniform-looking transition between colors.
