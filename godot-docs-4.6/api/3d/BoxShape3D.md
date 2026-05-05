# BoxShape3D

## Meta

- Name: BoxShape3D
- Source: BoxShape3D.xml
- Inherits: Shape3D
- Inheritance Chain: BoxShape3D -> Shape3D -> Resource -> RefCounted -> Object

## Brief Description

A 3D box shape used for physics collision.

## Description

A 3D box shape, intended for use in physics. Usually used to provide a shape for a CollisionShape3D. **Performance:** BoxShape3D is fast to check collisions against. It is faster than CapsuleShape3D and CylinderShape3D, but slower than SphereShape3D.

## Quick Reference

```
[properties]
size: Vector3 = Vector3(1, 1, 1)
```

## Tutorials

- [3D Physics Tests Demo](https://godotengine.org/asset-library/asset/2747)
- [3D Kinematic Character Demo](https://godotengine.org/asset-library/asset/2739)
- [3D Platformer Demo](https://godotengine.org/asset-library/asset/2748)

## Properties

- size: Vector3 = Vector3(1, 1, 1) [set set_size; get get_size]
  The box's width, height and depth.
