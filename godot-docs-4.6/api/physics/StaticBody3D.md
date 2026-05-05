# StaticBody3D

## Meta

- Name: StaticBody3D
- Source: StaticBody3D.xml
- Inherits: PhysicsBody3D
- Inheritance Chain: StaticBody3D -> PhysicsBody3D -> CollisionObject3D -> Node3D -> Node -> Object

## Brief Description

A 3D physics body that can't be moved by external forces. When moved manually, it doesn't affect other bodies in its path.

## Description

A static 3D physics body. It can't be moved by external forces or contacts, but can be moved manually by other means such as code, AnimationMixers (with AnimationMixer.callback_mode_process set to AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_PHYSICS), and RemoteTransform3D. When StaticBody3D is moved, it is teleported to its new position without affecting other physics bodies in its path. If this is not desired, use AnimatableBody3D instead. StaticBody3D is useful for completely static objects like floors and walls, as well as moving surfaces like conveyor belts and circular revolving platforms (by using constant_linear_velocity and constant_angular_velocity).

## Quick Reference

```
[properties]
constant_angular_velocity: Vector3 = Vector3(0, 0, 0)
constant_linear_velocity: Vector3 = Vector3(0, 0, 0)
physics_material_override: PhysicsMaterial
```

## Tutorials

- [Physics introduction]($DOCS_URL/tutorials/physics/physics_introduction.html)
- [Troubleshooting physics issues]($DOCS_URL/tutorials/physics/troubleshooting_physics_issues.html)
- [3D Physics Tests Demo](https://godotengine.org/asset-library/asset/2747)
- [Third Person Shooter (TPS) Demo](https://godotengine.org/asset-library/asset/2710)
- [3D Voxel Demo](https://godotengine.org/asset-library/asset/2755)

## Properties

- constant_angular_velocity: Vector3 = Vector3(0, 0, 0) [set set_constant_angular_velocity; get get_constant_angular_velocity]
  The body's constant angular velocity. This does not rotate the body, but affects touching bodies, as if it were rotating.

- constant_linear_velocity: Vector3 = Vector3(0, 0, 0) [set set_constant_linear_velocity; get get_constant_linear_velocity]
  The body's constant linear velocity. This does not move the body, but affects touching bodies, as if it were moving.

- physics_material_override: PhysicsMaterial [set set_physics_material_override; get get_physics_material_override]
  The physics material override for the body. If a material is assigned to this property, it will be used instead of any other physics material, such as an inherited one.
