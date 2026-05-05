# CanvasTexture

## Meta

- Name: CanvasTexture
- Source: CanvasTexture.xml
- Inherits: Texture2D
- Inheritance Chain: CanvasTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture with optional normal and specular maps for use in 2D rendering.

## Description

CanvasTexture is an alternative to ImageTexture for 2D rendering. It allows using normal maps and specular maps in any node that inherits from CanvasItem. CanvasTexture also allows overriding the texture's filter and repeat mode independently of the node's properties (or the project settings). **Note:** CanvasTexture cannot be used in 3D. It will not display correctly when applied to any VisualInstance3D, such as Sprite3D or Decal. For physically-based materials in 3D, use BaseMaterial3D instead.

## Quick Reference

```
[properties]
diffuse_texture: Texture2D
normal_texture: Texture2D
resource_local_to_scene: bool = false
specular_color: Color = Color(1, 1, 1, 1)
specular_shininess: float = 1.0
specular_texture: Texture2D
texture_filter: int (CanvasItem.TextureFilter) = 0
texture_repeat: int (CanvasItem.TextureRepeat) = 0
```

## Tutorials

- [2D Lights and Shadows]($DOCS_URL/tutorials/2d/2d_lights_and_shadows.html)

## Properties

- diffuse_texture: Texture2D [set set_diffuse_texture; get get_diffuse_texture]
  The diffuse (color) texture to use. This is the main texture you want to set in most cases.

- normal_texture: Texture2D [set set_normal_texture; get get_normal_texture]
  The normal map texture to use. Only has a visible effect if Light2Ds are affecting this CanvasTexture. **Note:** Godot expects the normal map to use X+, Y+, and Z+ coordinates. See [this page](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Common_Swizzle_Coordinates) for a comparison of normal map coordinates expected by popular engines.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- specular_color: Color = Color(1, 1, 1, 1) [set set_specular_color; get get_specular_color]
  The multiplier for specular reflection colors. The Light2D's color is also taken into account when determining the reflection color. Only has a visible effect if Light2Ds are affecting this CanvasTexture.

- specular_shininess: float = 1.0 [set set_specular_shininess; get get_specular_shininess]
  The specular exponent for Light2D specular reflections. Higher values result in a more glossy/"wet" look, with reflections becoming more localized and less visible overall. The default value of 1.0 disables specular reflections entirely. Only has a visible effect if Light2Ds are affecting this CanvasTexture.

- specular_texture: Texture2D [set set_specular_texture; get get_specular_texture]
  The specular map to use for Light2D specular reflections. This should be a grayscale or colored texture, with brighter areas resulting in a higher specular_shininess value. Using a colored specular_texture allows controlling specular shininess on a per-channel basis. Only has a visible effect if Light2Ds are affecting this CanvasTexture.

- texture_filter: int (CanvasItem.TextureFilter) = 0 [set set_texture_filter; get get_texture_filter]
  The texture filtering mode to use when drawing this CanvasTexture.

- texture_repeat: int (CanvasItem.TextureRepeat) = 0 [set set_texture_repeat; get get_texture_repeat]
  The texture repeat mode to use when drawing this CanvasTexture.
