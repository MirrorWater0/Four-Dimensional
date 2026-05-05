# Texture3DRD

## Meta

- Name: Texture3DRD
- Source: Texture3DRD.xml
- Inherits: Texture3D
- Inheritance Chain: Texture3DRD -> Texture3D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture for 3D that is bound to a texture created on the RenderingDevice.

## Description

This texture class allows you to use a 3D texture created directly on the RenderingDevice as a texture for materials, meshes, etc. **Note:** Texture3DRD is intended for low-level usage with RenderingDevice. For most use cases, use Texture3D instead.

## Quick Reference

```
[properties]
texture_rd_rid: RID
```

## Tutorials

- [Compute Texture demo](https://godotengine.org/asset-library/asset/2764)

## Properties

- texture_rd_rid: RID [set set_texture_rd_rid; get get_texture_rd_rid]
  The RID of the texture object created on the RenderingDevice.
