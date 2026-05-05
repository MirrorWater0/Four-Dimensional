# ImageTexture3D

## Meta

- Name: ImageTexture3D
- Source: ImageTexture3D.xml
- Inherits: Texture3D
- Inheritance Chain: ImageTexture3D -> Texture3D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Texture with 3 dimensions.

## Description

ImageTexture3D is a 3-dimensional ImageTexture that has a width, height, and depth. See also ImageTextureLayered. 3D textures are typically used to store density maps for FogMaterial, color correction LUTs for Environment, vector fields for GPUParticlesAttractorVectorField3D and collision maps for GPUParticlesCollisionSDF3D. 3D textures can also be used in custom shaders.

## Quick Reference

```
[methods]
create(format: int (Image.Format), width: int, height: int, depth: int, use_mipmaps: bool, data: Image[]) -> int (Error)
update(data: Image[]) -> void
```

## Methods

- create(format: int (Image.Format), width: int, height: int, depth: int, use_mipmaps: bool, data: Image[]) -> int (Error)
  Creates the ImageTexture3D with specified format, width, height, and depth. If use_mipmaps is true, generates mipmaps for the ImageTexture3D.

- update(data: Image[]) -> void
  Replaces the texture's existing data with the layers specified in data. The size of data must match the parameters that were used for create(). In other words, the texture cannot be resized or have its format changed by calling update().
