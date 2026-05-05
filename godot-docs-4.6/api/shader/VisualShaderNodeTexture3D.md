# VisualShaderNodeTexture3D

## Meta

- Name: VisualShaderNodeTexture3D
- Source: VisualShaderNodeTexture3D.xml
- Inherits: VisualShaderNodeSample3D
- Inheritance Chain: VisualShaderNodeTexture3D -> VisualShaderNodeSample3D -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Performs a 3D texture lookup within the visual shader graph.

## Description

Performs a lookup operation on the provided texture, with support for multiple texture sources to choose from.

## Quick Reference

```
[properties]
texture: Texture3D
```

## Properties

- texture: Texture3D [set set_texture; get get_texture]
  A source texture. Used if VisualShaderNodeSample3D.source is set to VisualShaderNodeSample3D.SOURCE_TEXTURE.
