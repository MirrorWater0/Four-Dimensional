# SphereMesh

## Meta

- Name: SphereMesh
- Source: SphereMesh.xml
- Inherits: PrimitiveMesh
- Inheritance Chain: SphereMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Class representing a spherical PrimitiveMesh.

## Description

Class representing a spherical PrimitiveMesh.

## Quick Reference

```
[properties]
height: float = 1.0
is_hemisphere: bool = false
radial_segments: int = 64
radius: float = 0.5
rings: int = 32
```

## Properties

- height: float = 1.0 [set set_height; get get_height]
  Full height of the sphere.

- is_hemisphere: bool = false [set set_is_hemisphere; get get_is_hemisphere]
  If true, a hemisphere is created rather than a full sphere. **Note:** To get a regular hemisphere, the height and radius of the sphere must be equal.

- radial_segments: int = 64 [set set_radial_segments; get get_radial_segments]
  Number of radial segments on the sphere.

- radius: float = 0.5 [set set_radius; get get_radius]
  Radius of sphere.

- rings: int = 32 [set set_rings; get get_rings]
  Number of segments along the height of the sphere.
