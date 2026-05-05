# StaticBody2D

## Meta

- Name: StaticBody2D
- Source: StaticBody2D.xml
- Inherits: PhysicsBody2D
- Inheritance Chain: StaticBody2D -> PhysicsBody2D -> CollisionObject2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A 2D physics body that can't be moved by external forces. When moved manually, it doesn't affect other bodies in its path.

## Description

A static 2D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, AnimationMixers (with AnimationMixer.callback_mode_process set to AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_PHYSICS), and RemoteTransform2D. When StaticBody2D is moved, it is teleported to its new position without affecting other physics bodies in its path. If this is not desired, use AnimatableBody2D instead. StaticBody2D is useful for completely static objects like floors and walls, as well as moving surfaces like conveyor belts and circular revolving platforms (by using constant_linear_velocity and constant_angular_velocity).

## Quick Reference

```
[properties]
constant_angular_velocity: float = 0.0
constant_linear_velocity: Vector2 = Vector2(0, 0)
physics_material_override: PhysicsMaterial
```

## Tutorials

- [Physics introduction]($DOCS_URL/tutorials/physics/physics_introduction.html)
- [Troubleshooting physics issues]($DOCS_URL/tutorials/physics/troubleshooting_physics_issues.html)

## Properties

- constant_angular_velocity: float = 0.0 [set set_constant_angular_velocity; get get_constant_angular_velocity]
  The body's constant angular velocity. This does not rotate the body, but affects touching bodies, as if it were rotating.

- constant_linear_velocity: Vector2 = Vector2(0, 0) [set set_constant_linear_velocity; get get_constant_linear_velocity]
  The body's constant linear velocity. This does not move the body, but affects touching bodies, as if it were moving.

- physics_material_override: PhysicsMaterial [set set_physics_material_override; get get_physics_material_override]
  The physics material override for the body. If a material is assigned to this property, it will be used instead of any other physics material, such as an inherited one.
