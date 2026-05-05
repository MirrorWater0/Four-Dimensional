# VisualShaderNodeTexture2DArray

## Meta

- Name: VisualShaderNodeTexture2DArray
- Source: VisualShaderNodeTexture2DArray.xml
- Inherits: VisualShaderNodeSample3D
- Inheritance Chain: VisualShaderNodeTexture2DArray -> VisualShaderNodeSample3D -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A 2D texture uniform array to be used within the visual shader graph.

## Description

Translated to uniform sampler2DArray in the shader language.

## Quick Reference

```
[properties]
texture_array: TextureLayered
```

## Properties

- texture_array: TextureLayered [set set_texture_array; get get_texture_array]
  A source texture array. Used if VisualShaderNodeSample3D.source is set to VisualShaderNodeSample3D.SOURCE_TEXTURE.
