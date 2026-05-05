# VisualShaderNodeCubemap

## Meta

- Name: VisualShaderNodeCubemap
- Source: VisualShaderNodeCubemap.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeCubemap -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A Cubemap sampling node to be used within the visual shader graph.

## Description

Translated to texture(cubemap, vec3) in the shader language. Returns a color vector and alpha channel as scalar.

## Quick Reference

```
[properties]
cube_map: TextureLayered
source: int (VisualShaderNodeCubemap.Source) = 0
texture_type: int (VisualShaderNodeCubemap.TextureType) = 0
```

## Properties

- cube_map: TextureLayered [set set_cube_map; get get_cube_map]
  The Cubemap texture to sample when using SOURCE_TEXTURE as source.

- source: int (VisualShaderNodeCubemap.Source) = 0 [set set_source; get get_source]
  Defines which source should be used for the sampling.

- texture_type: int (VisualShaderNodeCubemap.TextureType) = 0 [set set_texture_type; get get_texture_type]
  Defines the type of data provided by the source texture.

## Constants

### Enum Source

- SOURCE_TEXTURE = 0
  Use the Cubemap set via cube_map. If this is set to source, the samplerCube port is ignored.

- SOURCE_PORT = 1
  Use the Cubemap sampler reference passed via the samplerCube port. If this is set to source, the cube_map texture is ignored.

- SOURCE_MAX = 2
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
