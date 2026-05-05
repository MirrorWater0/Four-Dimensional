# Texture2DRD

## Meta

- Name: Texture2DRD
- Source: Texture2DRD.xml
- Inherits: Texture2D
- Inheritance Chain: Texture2DRD -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture for 2D that is bound to a texture created on the RenderingDevice.

## Description

This texture class allows you to use a 2D texture created directly on the RenderingDevice as a texture for materials, meshes, etc. **Note:** Texture2DRD is intended for low-level usage with RenderingDevice. For most use cases, use Texture2D instead.

## Quick Reference

```
[properties]
resource_local_to_scene: bool = false
texture_rd_rid: RID
```

## Tutorials

- [Compute Texture demo](https://godotengine.org/asset-library/asset/2764)

## Properties

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]

- texture_rd_rid: RID [set set_texture_rd_rid; get get_texture_rd_rid]
  The RID of the texture object created on the RenderingDevice.
