# MeshTexture

## Meta

- Name: MeshTexture
- Source: MeshTexture.xml
- Inherits: Texture2D
- Inheritance Chain: MeshTexture -> Texture2D -> Texture -> Resource -> RefCounted -> Object

## Brief Description

Simple texture that uses a mesh to draw itself.

## Description

Simple texture that uses a mesh to draw itself. It's limited because flags can't be changed and region drawing is not supported.

## Quick Reference

```
[properties]
base_texture: Texture2D
image_size: Vector2 = Vector2(0, 0)
mesh: Mesh
resource_local_to_scene: bool = false
```

## Properties

- base_texture: Texture2D [set set_base_texture; get get_base_texture]
  Sets the base texture that the Mesh will use to draw.

- image_size: Vector2 = Vector2(0, 0) [set set_image_size; get get_image_size]
  Sets the size of the image, needed for reference.

- mesh: Mesh [set set_mesh; get get_mesh]
  Sets the mesh used to draw. It must be a mesh using 2D vertices.

- resource_local_to_scene: bool = false [set set_local_to_scene; get is_local_to_scene; override Resource]
