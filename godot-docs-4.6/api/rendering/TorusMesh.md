# TorusMesh

## Meta

- Name: TorusMesh
- Source: TorusMesh.xml
- Inherits: PrimitiveMesh
- Inheritance Chain: TorusMesh -> PrimitiveMesh -> Mesh -> Resource -> RefCounted -> Object

## Brief Description

Class representing a torus PrimitiveMesh.

## Description

Class representing a torus PrimitiveMesh.

## Quick Reference

```
[properties]
inner_radius: float = 0.5
outer_radius: float = 1.0
ring_segments: int = 32
rings: int = 64
```

## Properties

- inner_radius: float = 0.5 [set set_inner_radius; get get_inner_radius]
  The inner radius of the torus.

- outer_radius: float = 1.0 [set set_outer_radius; get get_outer_radius]
  The outer radius of the torus.

- ring_segments: int = 32 [set set_ring_segments; get get_ring_segments]
  The number of edges each ring of the torus is constructed of.

- rings: int = 64 [set set_rings; get get_rings]
  The number of slices the torus is constructed of.
