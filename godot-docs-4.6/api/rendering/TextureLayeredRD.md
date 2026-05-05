# TextureLayeredRD

## Meta

- Name: TextureLayeredRD
- Source: TextureLayeredRD.xml
- Inherits: TextureLayered
- Inheritance Chain: TextureLayeredRD -> TextureLayered -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Abstract base class for layered texture RD types.

## Description

Base class for Texture2DArrayRD, TextureCubemapRD and TextureCubemapArrayRD. Cannot be used directly, but contains all the functions necessary for accessing the derived resource types. **Note:** TextureLayeredRD is intended for low-level usage with RenderingDevice. For most use cases, use TextureLayered instead.

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
