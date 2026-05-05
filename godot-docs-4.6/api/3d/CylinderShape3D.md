# CylinderShape3D

## Meta

- Name: CylinderShape3D
- Source: CylinderShape3D.xml
- Inherits: Shape3D
- Inheritance Chain: CylinderShape3D -> Shape3D -> Resource -> RefCounted -> Object

## Brief Description

A 3D cylinder shape used for physics collision.

## Description

A 3D cylinder shape, intended for use in physics. Usually used to provide a shape for a CollisionShape3D. **Note:** There are several known bugs with cylinder collision shapes. Using CapsuleShape3D or BoxShape3D instead is recommended. **Performance:** CylinderShape3D is fast to check collisions against, but it is slower than CapsuleShape3D, BoxShape3D, and SphereShape3D.

## Quick Reference

```
[properties]
height: float = 2.0
radius: float = 0.5
```

## Tutorials

- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)
- [3D Physics Tests Demo](https://godotengine.org/asset-library/asset/2747)
- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)

## Properties

- height: float = 2.0 [set set_height; get get_height]
  The cylinder's height.

- radius: float = 0.5 [set set_radius; get get_radius]
  The cylinder's radius.
