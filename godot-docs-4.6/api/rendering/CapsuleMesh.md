# CapsuleMesh

## Meta

- Name: CapsuleMesh
- Source: CapsuleMesh.xml
- Inherits: PrimitiveMesh
- Inheritance Chain: CapsuleMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Class representing a capsule-shaped PrimitiveMesh.

## Description

Class representing a capsule-shaped PrimitiveMesh.

## Quick Reference

```
[properties]
height: float = 2.0
radial_segments: int = 64
radius: float = 0.5
rings: int = 8
```

## Properties

- height: float = 2.0 [set set_height; get get_height]
  Total height of the capsule mesh (including the hemispherical ends). **Note:** The height of a capsule must be at least twice its radius. Otherwise, the capsule becomes a circle. If the height is less than twice the radius, the properties adjust to a valid value.

- radial_segments: int = 64 [set set_radial_segments; get get_radial_segments]
  Number of radial segments on the capsule mesh.

- radius: float = 0.5 [set set_radius; get get_radius]
  Radius of the capsule mesh. **Note:** The radius of a capsule cannot be greater than half of its height. Otherwise, the capsule becomes a circle. If the radius is greater than half of the height, the properties adjust to a valid value.

- rings: int = 8 [set set_rings; get get_rings]
  Number of rings along the height of the capsule.
