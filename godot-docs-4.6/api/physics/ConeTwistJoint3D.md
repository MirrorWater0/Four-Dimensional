# ConeTwistJoint3D

## Meta

- Name: ConeTwistJoint3D
- Source: ConeTwistJoint3D.xml
- Inherits: Joint3D
- Inheritance Chain: ConeTwistJoint3D -> Joint3D -> Node3D -> Node -> Object

## Brief Description

A physics joint that connects two 3D physics bodies in a way that simulates a ball-and-socket joint.

## Description

A physics joint that connects two 3D physics bodies in a way that simulates a ball-and-socket joint. The twist axis is initiated as the X axis of the ConeTwistJoint3D. Once the physics bodies swing, the twist axis is calculated as the middle of the X axes of the joint in the local space of the two physics bodies. Useful for limbs like shoulders and hips, lamps hanging off a ceiling, etc.

## Quick Reference

```
[methods]
get_param(param: int (ConeTwistJoint3D.Param)) -> float [const]
set_param(param: int (ConeTwistJoint3D.Param), value: float) -> void

[properties]
bias: float = 0.3
relaxation: float = 1.0
softness: float = 0.8
swing_span: float = 0.7853982
twist_span: float = 3.1415927
```

## Methods

- get_param(param: int (ConeTwistJoint3D.Param)) -> float [const]
  Returns the value of the specified parameter.

- set_param(param: int (ConeTwistJoint3D.Param), value: float) -> void
  Sets the value of the specified parameter.

## Properties

- bias: float = 0.3 [set set_param; get get_param]
  The speed with which the swing or twist will take place. The higher, the faster.

- relaxation: float = 1.0 [set set_param; get get_param]
  Defines, how fast the swing- and twist-speed-difference on both sides gets synced.

- softness: float = 0.8 [set set_param; get get_param]
  The ease with which the joint starts to twist. If it's too low, it takes more force to start twisting the joint.

- swing_span: float = 0.7853982 [set set_param; get get_param]
  Swing is rotation from side to side, around the axis perpendicular to the twist axis. The swing span defines, how much rotation will not get corrected along the swing axis. Could be defined as looseness in the ConeTwistJoint3D. If below 0.05, this behavior is locked.

- twist_span: float = 3.1415927 [set set_param; get get_param]
  Twist is the rotation around the twist axis, this value defined how far the joint can twist. Twist is locked if below 0.05.

## Constants

### Enum Param

- PARAM_SWING_SPAN = 0
  Swing is rotation from side to side, around the axis perpendicular to the twist axis. The swing span defines, how much rotation will not get corrected along the swing axis. Could be defined as looseness in the ConeTwistJoint3D. If below 0.05, this behavior is locked.

- PARAM_TWIST_SPAN = 1
  Twist is the rotation around the twist axis, this value defined how far the joint can twist. Twist is locked if below 0.05.

- PARAM_BIAS = 2
  The speed with which the swing or twist will take place. The higher, the faster.

- PARAM_SOFTNESS = 3
  The ease with which the joint starts to twist. If it's too low, it takes more force to start twisting the joint.

- PARAM_RELAXATION = 4
  Defines, how fast the swing- and twist-speed-difference on both sides gets synced.

- PARAM_MAX = 5
  Represents the size of the Param enum.
