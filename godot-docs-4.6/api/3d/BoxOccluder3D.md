# BoxOccluder3D

## Meta

- Name: BoxOccluder3D
- Source: BoxOccluder3D.xml
- Inherits: Occluder3D
- Inheritance Chain: BoxOccluder3D -> Occluder3D -> Resource -> RefCounted -> Object

## Brief Description

Cuboid shape for use with occlusion culling in OccluderInstance3D.

## Description

BoxOccluder3D stores a cuboid shape that can be used by the engine's occlusion culling system. See OccluderInstance3D's documentation for instructions on setting up occlusion culling.

## Quick Reference

```
[properties]
size: Vector3 = Vector3(1, 1, 1)
```

## Tutorials

- [Occlusion culling]($DOCS_URL/tutorials/3d/occlusion_culling.html)

## Properties

- size: Vector3 = Vector3(1, 1, 1) [set set_size; get get_size]
  The box's size in 3D units.
