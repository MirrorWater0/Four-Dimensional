# PointLight2D

## Meta

- Name: PointLight2D
- Source: PointLight2D.xml
- Inherits: Light2D
- Inheritance Chain: PointLight2D -> Light2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

Positional 2D light source.

## Description

Casts light in a 2D environment. This light's shape is defined by a (usually grayscale) texture.

## Quick Reference

```
[properties]
height: float = 0.0
offset: Vector2 = Vector2(0, 0)
texture: Texture2D
texture_scale: float = 1.0
```

## Tutorials

- [2D lights and shadows]($DOCS_URL/tutorials/2d/2d_lights_and_shadows.html)

## Properties

- height: float = 0.0 [set set_height; get get_height]
  The height of the light. Used with 2D normal mapping. The units are in pixels, e.g. if the height is 100, then it will illuminate an object 100 pixels away at a 45° angle to the plane.

- offset: Vector2 = Vector2(0, 0) [set set_texture_offset; get get_texture_offset]
  The offset of the light's texture.

- texture: Texture2D [set set_texture; get get_texture]
  Texture2D used for the light's appearance.

- texture_scale: float = 1.0 [set set_texture_scale; get get_texture_scale]
  The texture's scale factor.
