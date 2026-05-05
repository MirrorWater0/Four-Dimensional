# SphereOccluder3D

## Meta

- Name: SphereOccluder3D
- Source: SphereOccluder3D.xml
- Inherits: Occluder3D
- Inheritance Chain: SphereOccluder3D -> Occluder3D -> Resource -> RefCounted -> Object

## Brief Description

Spherical shape for use with occlusion culling in OccluderInstance3D.

## Description

SphereOccluder3D stores a sphere shape that can be used by the engine's occlusion culling system. See OccluderInstance3D's documentation for instructions on setting up occlusion culling.

## Quick Reference

```
[properties]
radius: float = 1.0
```

## Tutorials

- [Occlusion culling]($DOCS_URL/tutorials/3d/occlusion_culling.html)

## Properties

- radius: float = 1.0 [set set_radius; get get_radius]
  The sphere's radius in 3D units.
