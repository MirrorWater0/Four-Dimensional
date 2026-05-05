# WorldBoundaryShape2D

## Meta

- Name: WorldBoundaryShape2D
- Source: WorldBoundaryShape2D.xml
- Inherits: Shape2D
- Inheritance Chain: WorldBoundaryShape2D -> Shape2D -> Resource -> RefCounted -> Object

## Brief Description

A 2D world boundary (half-plane) shape used for physics collision.

## Description

A 2D world boundary shape, intended for use in physics. WorldBoundaryShape2D works like an infinite straight line that forces all physics bodies to stay above it. The line's normal determines which direction is considered as "above" and in the editor, the smaller line over it represents this direction. It can for example be used for endless flat floors.

## Quick Reference

```
[properties]
distance: float = 0.0
normal: Vector2 = Vector2(0, -1)
```

## Properties

- distance: float = 0.0 [set set_distance; get get_distance]
  The distance from the origin to the line, expressed in terms of normal (according to its direction and magnitude). Actual absolute distance from the origin to the line can be calculated as abs(distance) / normal.length(). In the scalar equation of the line ax + by = d, this is d, while the (a, b) coordinates are represented by the normal property.

- normal: Vector2 = Vector2(0, -1) [set set_normal; get get_normal]
  The line's normal, typically a unit vector. Its direction indicates the non-colliding half-plane. Can be of any length but zero. Defaults to Vector2.UP.
