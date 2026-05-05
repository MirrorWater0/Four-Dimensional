# PrismMesh

## Meta

- Name: PrismMesh
- Source: PrismMesh.xml
- Inherits: PrimitiveMesh
- Inheritance Chain: PrismMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Class representing a prism-shaped PrimitiveMesh.

## Description

Class representing a prism-shaped PrimitiveMesh.

## Quick Reference

```
[properties]
left_to_right: float = 0.5
size: Vector3 = Vector3(1, 1, 1)
subdivide_depth: int = 0
subdivide_height: int = 0
subdivide_width: int = 0
```

## Properties

- left_to_right: float = 0.5 [set set_left_to_right; get get_left_to_right]
  Displacement of the upper edge along the X axis. 0.0 positions edge straight above the bottom-left edge.

- size: Vector3 = Vector3(1, 1, 1) [set set_size; get get_size]
  Size of the prism.

- subdivide_depth: int = 0 [set set_subdivide_depth; get get_subdivide_depth]
  Number of added edge loops along the Z axis.

- subdivide_height: int = 0 [set set_subdivide_height; get get_subdivide_height]
  Number of added edge loops along the Y axis.

- subdivide_width: int = 0 [set set_subdivide_width; get get_subdivide_width]
  Number of added edge loops along the X axis.
