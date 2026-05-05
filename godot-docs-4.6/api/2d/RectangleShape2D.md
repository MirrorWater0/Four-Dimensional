# RectangleShape2D

## Meta

- Name: RectangleShape2D
- Source: RectangleShape2D.xml
- Inherits: Shape2D
- Inheritance Chain: RectangleShape2D -> Shape2D -> Resource -> RefCounted -> Object

## Brief Description

A 2D rectangle shape used for physics collision.

## Description

A 2D rectangle shape, intended for use in physics. Usually used to provide a shape for a CollisionShape2D. **Performance:** RectangleShape2D is fast to check collisions against. It is faster than CapsuleShape2D, but slower than CircleShape2D.

## Quick Reference

```
[properties]
size: Vector2 = Vector2(20, 20)
```

## Tutorials

- [2D Pong Demo](https://godotengine.org/asset-library/asset/2728)
- [2D Kinematic Character Demo](https://godotengine.org/asset-library/asset/2719)

## Properties

- size: Vector2 = Vector2(20, 20) [set set_size; get get_size]
  The rectangle's width and height.
