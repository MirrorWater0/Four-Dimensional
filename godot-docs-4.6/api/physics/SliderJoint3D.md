# SliderJoint3D

## Meta

- Name: SliderJoint3D
- Source: SliderJoint3D.xml
- Inherits: Joint3D
- Inheritance Chain: SliderJoint3D -> Joint3D -> Node3D -> Node -> Object

## Brief Description

A physics joint that restricts the movement of a 3D physics body along an axis relative to another physics body.

## Description

A physics joint that restricts the movement of a 3D physics body along an axis relative to another physics body. For example, Body A could be a StaticBody3D representing a piston base, while Body B could be a RigidBody3D representing the piston head, moving up and down.

## Quick Reference

```
[methods]
get_param(param: int (SliderJoint3D.Param)) -> float [const]
set_param(param: int (SliderJoint3D.Param), value: float) -> void

[properties]
angular_limit/damping: float = 0.0
angular_limit/lower_angle: float = 0.0
angular_limit/restitution: float = 0.7
angular_limit/softness: float = 1.0
angular_limit/upper_angle: float = 0.0
angular_motion/damping: float = 1.0
angular_motion/restitution: float = 0.7
angular_motion/softness: float = 1.0
angular_ortho/damping: float = 1.0
angular_ortho/restitution: float = 0.7
angular_ortho/softness: float = 1.0
linear_limit/damping: float = 1.0
linear_limit/lower_distance: float = -1.0
linear_limit/restitution: float = 0.7
linear_limit/softness: float = 1.0
linear_limit/upper_distance: float = 1.0
linear_motion/damping: float = 0.0
linear_motion/restitution: float = 0.7
linear_motion/softness: float = 1.0
linear_ortho/damping: float = 1.0
linear_ortho/restitution: float = 0.7
linear_ortho/softness: float = 1.0
```

## Methods

- get_param(param: int (SliderJoint3D.Param)) -> float [const]
  Returns the value of the given parameter.

- set_param(param: int (SliderJoint3D.Param), value: float) -> void
  Assigns value to the given parameter.

## Properties

- angular_limit/damping: float = 0.0 [set set_param; get get_param]
  The amount of damping of the rotation when the limit is surpassed. A lower damping value allows a rotation initiated by body A to travel to body B slower.

- angular_limit/lower_angle: float = 0.0 [set set_param; get get_param]
  The lower limit of rotation in the slider.

- angular_limit/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution of the rotation when the limit is surpassed. Does not affect damping.

- angular_limit/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the all rotation once the limit is surpassed. Makes all rotation slower when between 0 and 1.

- angular_limit/upper_angle: float = 0.0 [set set_param; get get_param]
  The upper limit of rotation in the slider.

- angular_motion/damping: float = 1.0 [set set_param; get get_param]
  The amount of damping of the rotation in the limits.

- angular_motion/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution of the rotation in the limits.

- angular_motion/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the all rotation in the limits.

- angular_ortho/damping: float = 1.0 [set set_param; get get_param]
  The amount of damping of the rotation across axes orthogonal to the slider.

- angular_ortho/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution of the rotation across axes orthogonal to the slider.

- angular_ortho/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the all rotation across axes orthogonal to the slider.

- linear_limit/damping: float = 1.0 [set set_param; get get_param]
  The amount of damping that happens once the limit defined by linear_limit/lower_distance and linear_limit/upper_distance is surpassed.

- linear_limit/lower_distance: float = -1.0 [set set_param; get get_param]
  The minimum difference between the pivot points on their X axis before damping happens.

- linear_limit/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution once the limits are surpassed. The lower, the more velocity-energy gets lost.

- linear_limit/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the movement across the slider axis once the limits get surpassed. The lower, the slower the movement.

- linear_limit/upper_distance: float = 1.0 [set set_param; get get_param]
  The maximum difference between the pivot points on their X axis before damping happens.

- linear_motion/damping: float = 0.0 [set set_param; get get_param]
  The amount of damping inside the slider limits.

- linear_motion/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution inside the slider limits.

- linear_motion/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the movement across the slider axis as long as the slider is in the limits. The lower, the slower the movement.

- linear_ortho/damping: float = 1.0 [set set_param; get get_param]
  The amount of damping when movement is across axes orthogonal to the slider.

- linear_ortho/restitution: float = 0.7 [set set_param; get get_param]
  The amount of restitution when movement is across axes orthogonal to the slider.

- linear_ortho/softness: float = 1.0 [set set_param; get get_param]
  A factor applied to the movement across axes orthogonal to the slider.

## Constants

### Enum Param

- PARAM_LINEAR_LIMIT_UPPER = 0
  Constant for accessing linear_limit/upper_distance. The maximum difference between the pivot points on their X axis before damping happens.

- PARAM_LINEAR_LIMIT_LOWER = 1
  Constant for accessing linear_limit/lower_distance. The minimum difference between the pivot points on their X axis before damping happens.

- PARAM_LINEAR_LIMIT_SOFTNESS = 2
  Constant for accessing linear_limit/softness. A factor applied to the movement across the slider axis once the limits get surpassed. The lower, the slower the movement.

- PARAM_LINEAR_LIMIT_RESTITUTION = 3
  Constant for accessing linear_limit/restitution. The amount of restitution once the limits are surpassed. The lower, the more velocity-energy gets lost.

- PARAM_LINEAR_LIMIT_DAMPING = 4
  Constant for accessing linear_limit/damping. The amount of damping once the slider limits are surpassed.

- PARAM_LINEAR_MOTION_SOFTNESS = 5
  Constant for accessing linear_motion/softness. A factor applied to the movement across the slider axis as long as the slider is in the limits. The lower, the slower the movement.

- PARAM_LINEAR_MOTION_RESTITUTION = 6
  Constant for accessing linear_motion/restitution. The amount of restitution inside the slider limits.

- PARAM_LINEAR_MOTION_DAMPING = 7
  Constant for accessing linear_motion/damping. The amount of damping inside the slider limits.

- PARAM_LINEAR_ORTHOGONAL_SOFTNESS = 8
  Constant for accessing linear_ortho/softness. A factor applied to the movement across axes orthogonal to the slider.

- PARAM_LINEAR_ORTHOGONAL_RESTITUTION = 9
  Constant for accessing linear_motion/restitution. The amount of restitution when movement is across axes orthogonal to the slider.

- PARAM_LINEAR_ORTHOGONAL_DAMPING = 10
  Constant for accessing linear_motion/damping. The amount of damping when movement is across axes orthogonal to the slider.

- PARAM_ANGULAR_LIMIT_UPPER = 11
  Constant for accessing angular_limit/upper_angle. The upper limit of rotation in the slider.

- PARAM_ANGULAR_LIMIT_LOWER = 12
  Constant for accessing angular_limit/lower_angle. The lower limit of rotation in the slider.

- PARAM_ANGULAR_LIMIT_SOFTNESS = 13
  Constant for accessing angular_limit/softness. A factor applied to the all rotation once the limit is surpassed.

- PARAM_ANGULAR_LIMIT_RESTITUTION = 14
  Constant for accessing angular_limit/restitution. The amount of restitution of the rotation when the limit is surpassed.

- PARAM_ANGULAR_LIMIT_DAMPING = 15
  Constant for accessing angular_limit/damping. The amount of damping of the rotation when the limit is surpassed.

- PARAM_ANGULAR_MOTION_SOFTNESS = 16
  Constant for accessing angular_motion/softness. A factor applied to the all rotation in the limits.

- PARAM_ANGULAR_MOTION_RESTITUTION = 17
  Constant for accessing angular_motion/restitution. The amount of restitution of the rotation in the limits.

- PARAM_ANGULAR_MOTION_DAMPING = 18
  Constant for accessing angular_motion/damping. The amount of damping of the rotation in the limits.

- PARAM_ANGULAR_ORTHOGONAL_SOFTNESS = 19
  Constant for accessing angular_ortho/softness. A factor applied to the all rotation across axes orthogonal to the slider.

- PARAM_ANGULAR_ORTHOGONAL_RESTITUTION = 20
  Constant for accessing angular_ortho/restitution. The amount of restitution of the rotation across axes orthogonal to the slider.

- PARAM_ANGULAR_ORTHOGONAL_DAMPING = 21
  Constant for accessing angular_ortho/damping. The amount of damping of the rotation across axes orthogonal to the slider.

- PARAM_MAX = 22
  Represents the size of the Param enum.
