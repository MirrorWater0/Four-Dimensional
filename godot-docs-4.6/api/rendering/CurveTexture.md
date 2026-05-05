# CurveTexture

## Meta

- Name: CurveTexture
- Source: CurveTexture.xml
- Inherits: Texture2D
- Inheritance Chain: CurveTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

A 1D texture where pixel brightness corresponds to points on a curve.

## Description

A 1D texture where pixel brightness corresponds to points on a unit Curve resource, either in grayscale or in red. This visual representation simplifies the task of saving curves as image files. If you need to store up to 3 curves within a single texture, use CurveXYZTexture instead. See also GradientTexture1D and GradientTexture2D.

## Quick Reference

```
[properties]
curve: Curve
resource_local_to_scene: bool = false
texture_mode: int (CurveTexture.TextureMode) = 0
width: int = 256
```

## Properties

- curve: Curve [set set_curve; get get_curve]
  The Curve that is rendered onto the texture. Should be a unit Curve.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- texture_mode: int (CurveTexture.TextureMode) = 0 [set set_texture_mode; get get_texture_mode]
  The format the texture should be generated with. When passing a CurveTexture as an input to a Shader, this may need to be adjusted.

- width: int = 256 [set set_width; get get_width]
  The width of the texture (in pixels). Higher values make it possible to represent high-frequency data better (such as sudden direction changes), at the cost of increased generation time and memory usage.

## Constants

### Enum TextureMode

- TEXTURE_MODE_RGB = 0
  Store the curve equally across the red, green and blue channels. This uses more video memory, but is more compatible with shaders that only read the green and blue values.

- TEXTURE_MODE_RED = 1
  Store the curve only in the red channel. This saves video memory, but some custom shaders may not be able to work with this.
