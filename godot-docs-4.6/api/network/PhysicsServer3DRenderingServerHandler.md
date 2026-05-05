# PhysicsServer3DRenderingServerHandler

## Meta

- Name: PhysicsServer3DRenderingServerHandler
- Source: PhysicsServer3DRenderingServerHandler.xml
- Inherits: Object
- Inheritance Chain: PhysicsServer3DRenderingServerHandler -> Object

## Brief Description

A class used to provide PhysicsServer3DExtension._soft_body_update_rendering_server() with a rendering handler for soft bodies.

## Quick Reference

```
[methods]
_set_aabb(aabb: AABB) -> void [virtual required]
_set_normal(vertex_id: int, normal: Vector3) -> void [virtual required]
_set_vertex(vertex_id: int, vertex: Vector3) -> void [virtual required]
set_aabb(aabb: AABB) -> void
set_normal(vertex_id: int, normal: Vector3) -> void
set_vertex(vertex_id: int, vertex: Vector3) -> void
```

## Methods

- _set_aabb(aabb: AABB) -> void [virtual required]
  Called by the PhysicsServer3D to set the bounding box for the SoftBody3D.

- _set_normal(vertex_id: int, normal: Vector3) -> void [virtual required]
  Called by the PhysicsServer3D to set the normal for the SoftBody3D vertex at the index specified by vertex_id. **Note:** The normal parameter used to be of type const void* prior to Godot 4.2.

- _set_vertex(vertex_id: int, vertex: Vector3) -> void [virtual required]
  Called by the PhysicsServer3D to set the position for the SoftBody3D vertex at the index specified by vertex_id. **Note:** The vertex parameter used to be of type const void* prior to Godot 4.2.

- set_aabb(aabb: AABB) -> void
  Sets the bounding box for the SoftBody3D.

- set_normal(vertex_id: int, normal: Vector3) -> void
  Sets the normal for the SoftBody3D vertex at the index specified by vertex_id.

- set_vertex(vertex_id: int, vertex: Vector3) -> void
  Sets the position for the SoftBody3D vertex at the index specified by vertex_id.
