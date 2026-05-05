# SegmentShape2D

## Meta

- Name: SegmentShape2D
- Source: SegmentShape2D.xml
- Inherits: Shape2D
- Inheritance Chain: SegmentShape2D -> Shape2D -> Resource -> RefCounted -> Object

## Brief Description

A 2D line segment shape used for physics collision.

## Description

A 2D line segment shape, intended for use in physics. Usually used to provide a shape for a CollisionShape2D.

## Quick Reference

```
[properties]
a: Vector2 = Vector2(0, 0)
b: Vector2 = Vector2(0, 10)
```

## Properties

- a: Vector2 = Vector2(0, 0) [set set_a; get get_a]
  The segment's first point position.

- b: Vector2 = Vector2(0, 10) [set set_b; get get_b]
  The segment's second point position.
