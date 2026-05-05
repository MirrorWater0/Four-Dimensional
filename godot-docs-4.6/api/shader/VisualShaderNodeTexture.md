# VisualShaderNodeTexture

## Meta

- Name: VisualShaderNodeTexture
- Source: VisualShaderNodeTexture.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeTexture -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Performs a 2D texture lookup within the visual shader graph.

## Description

Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from.

## Quick Reference

```
[properties]
source: int (VisualShaderNodeTexture.Source) = 0
texture: Texture2D
texture_type: int (VisualShaderNodeTexture.TextureType) = 0
```

## Properties

- source: int (VisualShaderNodeTexture.Source) = 0 [set set_source; get get_source]
  Determines the source for the lookup.

- texture: Texture2D [set set_texture; get get_texture]
  The source texture, if needed for the selected source.

- texture_type: int (VisualShaderNodeTexture.TextureType) = 0 [set set_texture_type; get get_texture_type]
  Specifies the type of the texture if source is set to SOURCE_TEXTURE.

## Constants

### Enum Source

- SOURCE_TEXTURE = 0
  Use the texture given as an argument for this function.

- SOURCE_SCREEN = 1
  Use the current viewport's texture as the source.

- SOURCE_2D_TEXTURE = 2
  Use the texture from this shader's texture built-in (e.g. a texture of a Sprite2D).

- SOURCE_2D_NORMAL = 3
  Use the texture from this shader's normal map built-in.

- SOURCE_DEPTH = 4
  Use the depth texture captured during the depth prepass. Only available when the depth prepass is used (i.e. in spatial shaders and in the forward_plus or gl_compatibility renderers).

- SOURCE_PORT = 5
  Use the texture provided in the input port for this function.

- SOURCE_3D_NORMAL = 6
  Use the normal buffer captured during the depth prepass. Only available when the normal-roughness buffer is available (i.e. in spatial shaders and in the forward_plus renderer).

- SOURCE_ROUGHNESS = 7
  Use the roughness buffer captured during the depth prepass. Only available when the normal-roughness buffer is available (i.e. in spatial shaders and in the forward_plus renderer).

- SOURCE_MAX = 8
  Represents the size of the Source enum.

### Enum TextureType

- TYPE_DATA = 0
  No hints are added to the uniform declaration.

- TYPE_COLOR = 1
  Adds source_color as hint to the uniform declaration for proper conversion from nonlinear sRGB encoding to linear encoding.

- TYPE_NORMAL_MAP = 2
  Adds hint_normal as hint to the uniform declaration, which internally converts the texture for proper usage as normal map.

- TYPE_MAX = 3
  Represents the size of the TextureType enum.
