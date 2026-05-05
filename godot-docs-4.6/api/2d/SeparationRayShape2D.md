# SeparationRayShape2D

## Meta

- Name: SeparationRayShape2D
- Source: SeparationRayShape2D.xml
- Inherits: Shape2D
- Inheritance Chain: SeparationRayShape2D -> Shape2D -> Resource -> RefCounted -> Object

## Brief Description

A 2D ray shape used for physics collision that tries to separate itself from any collider.

## Description

A 2D ray shape, intended for use in physics. Usually used to provide a shape for a CollisionShape2D. When a SeparationRayShape2D collides with an object, it tries to separate itself from it by moving its endpoint to the collision point. For example, a SeparationRayShape2D next to a character can allow it to instantly move up when touching stairs.

## Quick Reference

```
[properties]
length: float = 20.0
slide_on_slope: bool = false
```

## Properties

- length: float = 20.0 [set set_length; get get_length]
  The ray's length.

- slide_on_slope: bool = false [set set_slide_on_slope; get get_slide_on_slope]
  If false (default), the shape always separates and returns a normal along its own direction. If true, the shape can return the correct normal and separate in any direction, allowing sliding motion on slopes.
