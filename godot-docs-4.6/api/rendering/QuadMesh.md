# QuadMesh

## Meta

- Name: QuadMesh
- Source: QuadMesh.xml
- Inherits: PlaneMesh
- Inheritance Chain: QuadMesh -> PlaneMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Class representing a square mesh facing the camera.

## Description

Class representing a square PrimitiveMesh. This flat mesh does not have a thickness. By default, this mesh is aligned on the X and Y axes; this rotation is more suited for use with billboarded materials. A QuadMesh is equivalent to a PlaneMesh except its default PlaneMesh.orientation is PlaneMesh.FACE_Z.

## Quick Reference

```
[properties]
orientation: int (PlaneMesh.Orientation) = 2
size: Vector2 = Vector2(1, 1)
```

## Tutorials

- [GUI in 3D Viewport Demo](https://godotengine.org/asset-library/asset/2807)
- [2D in 3D Viewport Demo](https://godotengine.org/asset-library/asset/2803)

## Properties

- orientation: int (PlaneMesh.Orientation) = 2 [set set_orientation; get get_orientation; override PlaneMesh]

- size: Vector2 = Vector2(1, 1) [set set_size; get get_size; override PlaneMesh]
