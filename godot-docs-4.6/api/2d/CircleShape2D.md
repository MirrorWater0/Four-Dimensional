# CircleShape2D

## Meta

- Name: CircleShape2D
- Source: CircleShape2D.xml
- Inherits: Shape2D
- Inheritance Chain: CircleShape2D -> Shape2D -> Resource -> RefCounted -> Object

## Brief Description

A 2D circle shape used for physics collision.

## Description

A 2D circle shape, intended for use in physics. Usually used to provide a shape for a CollisionShape2D. **Performance:** CircleShape2D is fast to check collisions against. It is faster than RectangleShape2D and CapsuleShape2D.

## Quick Reference

```
[properties]
radius: float = 10.0
```

## Properties

- radius: float = 10.0 [set set_radius; get get_radius]
  The circle's radius.
