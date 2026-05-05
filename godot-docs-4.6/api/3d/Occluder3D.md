# Occluder3D

## Meta

- Name: Occluder3D
- Source: Occluder3D.xml
- Inherits: Resource
- Inheritance Chain: Occluder3D -> Resource -> RefCounted -> Object

## Brief Description

Occluder shape resource for use with occlusion culling in OccluderInstance3D.

## Description

Occluder3D stores an occluder shape that can be used by the engine's occlusion culling system. See OccluderInstance3D's documentation for instructions on setting up occlusion culling.

## Quick Reference

```
[methods]
get_indices() -> PackedInt32Array [const]
get_vertices() -> PackedVector3Array [const]
```

## Tutorials

- [Occlusion culling]($DOCS_URL/tutorials/3d/occlusion_culling.html)

## Methods

- get_indices() -> PackedInt32Array [const]
  Returns the occluder shape's vertex indices.

- get_vertices() -> PackedVector3Array [const]
  Returns the occluder shape's vertex positions.
