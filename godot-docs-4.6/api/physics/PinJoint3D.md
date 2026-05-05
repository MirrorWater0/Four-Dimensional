# PinJoint3D

## Meta

- Name: PinJoint3D
- Source: PinJoint3D.xml
- Inherits: Joint3D
- Inheritance Chain: PinJoint3D -> Joint3D -> Node3D -> Node -> Object

## Brief Description

A physics joint that attaches two 3D physics bodies at a single point, allowing them to freely rotate.

## Description

A physics joint that attaches two 3D physics bodies at a single point, allowing them to freely rotate. For example, a RigidBody3D can be attached to a StaticBody3D to create a pendulum or a seesaw.

## Quick Reference

```
[methods]
get_param(param: int (PinJoint3D.Param)) -> float [const]
set_param(param: int (PinJoint3D.Param), value: float) -> void

[properties]
params/bias: float = 0.3
params/damping: float = 1.0
params/impulse_clamp: float = 0.0
```

## Methods

- get_param(param: int (PinJoint3D.Param)) -> float [const]
  Returns the value of the specified parameter.

- set_param(param: int (PinJoint3D.Param), value: float) -> void
  Sets the value of the specified parameter.

## Properties

- params/bias: float = 0.3 [set set_param; get get_param]
  The force with which the pinned objects stay in positional relation to each other. The higher, the stronger.

- params/damping: float = 1.0 [set set_param; get get_param]
  The force with which the pinned objects stay in velocity relation to each other. The higher, the stronger.

- params/impulse_clamp: float = 0.0 [set set_param; get get_param]
  If above 0, this value is the maximum value for an impulse that this Joint3D produces.

## Constants

### Enum Param

- PARAM_BIAS = 0
  The force with which the pinned objects stay in positional relation to each other. The higher, the stronger.

- PARAM_DAMPING = 1
  The force with which the pinned objects stay in velocity relation to each other. The higher, the stronger.

- PARAM_IMPULSE_CLAMP = 2
  If above 0, this value is the maximum value for an impulse that this Joint3D produces.
