# SphereShape3D

## Meta

- Name: SphereShape3D
- Source: SphereShape3D.xml
- Inherits: Shape3D
- Inheritance Chain: SphereShape3D -> Shape3D -> Resource -> RefCounted -> Object

## Brief Description

A 3D sphere shape used for physics collision.

## Description

A 3D sphere shape, intended for use in physics. Usually used to provide a shape for a CollisionShape3D. **Performance:** SphereShape3D is fast to check collisions against. It is faster than BoxShape3D, CapsuleShape3D, and CylinderShape3D.

## Quick Reference

```
[properties]
radius: float = 0.5
```

## Tutorials

- [3D Physics Tests Demo](https://godotengine.org/asset-library/asset/2747)

## Properties

- radius: float = 0.5 [set set_radius; get get_radius]
  The sphere's radius. The shape's diameter is double the radius.
