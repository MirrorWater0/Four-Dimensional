# CurveXYZTexture

## Meta

- Name: CurveXYZTexture
- Source: CurveXYZTexture.xml
- Inherits: Texture2D
- Inheritance Chain: CurveXYZTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

A 1D texture where the red, green, and blue color channels correspond to points on 3 curves.

## Description

A 1D texture where the red, green, and blue color channels correspond to points on 3 unit Curve resources. Compared to using separate CurveTextures, this further simplifies the task of saving curves as image files. If you only need to store one curve within a single texture, use CurveTexture instead. See also GradientTexture1D and GradientTexture2D.

## Quick Reference

```
[properties]
curve_x: Curve
curve_y: Curve
curve_z: Curve
resource_local_to_scene: bool = false
width: int = 256
```

## Properties

- curve_x: Curve [set set_curve_x; get get_curve_x]
  The Curve that is rendered onto the texture's red channel. Should be a unit Curve.

- curve_y: Curve [set set_curve_y; get get_curve_y]
  The Curve that is rendered onto the texture's green channel. Should be a unit Curve.

- curve_z: Curve [set set_curve_z; get get_curve_z]
  The Curve that is rendered onto the texture's blue channel. Should be a unit Curve.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- width: int = 256 [set set_width; get get_width]
  The width of the texture (in pixels). Higher values make it possible to represent high-frequency data better (such as sudden direction changes), at the cost of increased generation time and memory usage.
