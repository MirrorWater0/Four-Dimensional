# GradientTexture2D

## Meta

- Name: GradientTexture2D
- Source: GradientTexture2D.xml
- Inherits: Texture2D
- Inheritance Chain: GradientTexture2D -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

A 2D texture that creates a pattern with colors obtained from a Gradient.

## Description

A 2D texture that obtains colors from a Gradient to fill the texture data. This texture is able to transform a color transition into different patterns such as a linear or a radial gradient. The texture is filled by interpolating colors starting from fill_from to fill_to offsets by default, but the gradient fill can be repeated to cover the entire texture. The gradient is sampled individually for each pixel so it does not necessarily represent an exact copy of the gradient (see width and height). See also GradientTexture1D, CurveTexture and CurveXYZTexture.

## Quick Reference

```
[properties]
fill: int (GradientTexture2D.Fill) = 0
fill_from: Vector2 = Vector2(0, 0)
fill_to: Vector2 = Vector2(1, 0)
gradient: Gradient
height: int = 64
repeat: int (GradientTexture2D.Repeat) = 0
resource_local_to_scene: bool = false
use_hdr: bool = false
width: int = 64
```

## Properties

- fill: int (GradientTexture2D.Fill) = 0 [set set_fill; get get_fill]
  The gradient's fill type.

- fill_from: Vector2 = Vector2(0, 0) [set set_fill_from; get get_fill_from]
  The initial offset used to fill the texture specified in UV coordinates.

- fill_to: Vector2 = Vector2(1, 0) [set set_fill_to; get get_fill_to]
  The final offset used to fill the texture specified in UV coordinates.

- gradient: Gradient [set set_gradient; get get_gradient]
  The Gradient used to fill the texture.

- height: int = 64 [set set_height; get get_height]
  The number of vertical color samples that will be obtained from the Gradient, which also represents the texture's height.

- repeat: int (GradientTexture2D.Repeat) = 0 [set set_repeat; get get_repeat]
  The gradient's repeat type.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- use_hdr: bool = false [set set_use_hdr; get is_using_hdr]
  If true, the generated texture will support high dynamic range (Image.FORMAT_RGBAF format). This allows for glow effects to work if Environment.glow_enabled is true. If false, the generated texture will use low dynamic range; overbright colors will be clamped (Image.FORMAT_RGBA8 format).

- width: int = 64 [set set_width; get get_width]
  The number of horizontal color samples that will be obtained from the Gradient, which also represents the texture's width.

## Constants

### Enum Fill

- FILL_LINEAR = 0
  The colors are linearly interpolated in a straight line.

- FILL_RADIAL = 1
  The colors are linearly interpolated in a circular pattern.

- FILL_SQUARE = 2
  The colors are linearly interpolated in a square pattern.

### Enum Repeat

- REPEAT_NONE = 0
  The gradient fill is restricted to the range defined by fill_from to fill_to offsets.

- REPEAT = 1
  The texture is filled starting from fill_from to fill_to offsets, repeating the same pattern in both directions.

- REPEAT_MIRROR = 2
  The texture is filled starting from fill_from to fill_to offsets, mirroring the pattern in both directions.
