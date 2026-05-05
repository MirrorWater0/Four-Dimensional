# PinJoint2D

## Meta

- Name: PinJoint2D
- Source: PinJoint2D.xml
- Inherits: Joint2D
- Inheritance Chain: PinJoint2D -> Joint2D -> Node2D -> CanvasItem -> Node -> Object

## Brief Description

A physics joint that attaches two 2D physics bodies at a single point, allowing them to freely rotate.

## Description

A physics joint that attaches two 2D physics bodies at a single point, allowing them to freely rotate. For example, a RigidBody2D can be attached to a StaticBody2D to create a pendulum or a seesaw.

## Quick Reference

```
[properties]
angular_limit_enabled: bool = false
angular_limit_lower: float = 0.0
angular_limit_upper: float = 0.0
motor_enabled: bool = false
motor_target_velocity: float = 0.0
softness: float = 0.0
```

## Properties

- angular_limit_enabled: bool = false [set set_angular_limit_enabled; get is_angular_limit_enabled]
  If true, the pin maximum and minimum rotation, defined by angular_limit_lower and angular_limit_upper are applied.

- angular_limit_lower: float = 0.0 [set set_angular_limit_lower; get get_angular_limit_lower]
  The minimum rotation. Only active if angular_limit_enabled is true.

- angular_limit_upper: float = 0.0 [set set_angular_limit_upper; get get_angular_limit_upper]
  The maximum rotation. Only active if angular_limit_enabled is true.

- motor_enabled: bool = false [set set_motor_enabled; get is_motor_enabled]
  When activated, a motor turns the pin.

- motor_target_velocity: float = 0.0 [set set_motor_target_velocity; get get_motor_target_velocity]
  Target speed for the motor. In radians per second.

- softness: float = 0.0 [set set_softness; get get_softness]
  The higher this value, the more the bond to the pinned partner can flex.
