# QuadOccluder3D

## Meta

- Name: QuadOccluder3D
- Source: QuadOccluder3D.xml
- Inherits: Occluder3D
- Inheritance Chain: QuadOccluder3D -> Occluder3D -> Resource -> RefCounted -> Object

## Brief Description

Flat plane shape for use with occlusion culling in OccluderInstance3D.

## Description

QuadOccluder3D stores a flat plane shape that can be used by the engine's occlusion culling system. See also PolygonOccluder3D if you need to customize the quad's shape. See OccluderInstance3D's documentation for instructions on setting up occlusion culling.

## Quick Reference

```
[properties]
size: Vector2 = Vector2(1, 1)
```

## Tutorials

- [Occlusion culling]($DOCS_URL/tutorials/3d/occlusion_culling.html)

## Properties

- size: Vector2 = Vector2(1, 1) [set set_size; get get_size]
  The quad's size in 3D units.
