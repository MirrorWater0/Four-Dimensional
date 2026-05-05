# PathFollow3D

## Meta

- Name: PathFollow3D
- Source: PathFollow3D.xml
- Inherits: Node3D
- Inheritance Chain: PathFollow3D -> Node3D -> Node -> Object

## Brief Description

Point sampler for a Path3D.

## Description

This node takes its parent Path3D, and returns the coordinates of a point within it, given a distance from the first vertex. It is useful for making other nodes follow a path, without coding the movement pattern. For that, the nodes must be children of this node. The descendant nodes will then move accordingly when setting the progress in this node.

## Quick Reference

```
[methods]
correct_posture(transform: Transform3D, rotation_mode: int (PathFollow3D.RotationMode)) -> Transform3D [static]

[properties]
cubic_interp: bool = true
h_offset: float = 0.0
loop: bool = true
progress: float = 0.0
progress_ratio: float = 0.0
rotation_mode: int (PathFollow3D.RotationMode) = 3
tilt_enabled: bool = true
use_model_front: bool = false
v_offset: float = 0.0
```

## Methods

- correct_posture(transform: Transform3D, rotation_mode: int (PathFollow3D.RotationMode)) -> Transform3D [static]
  Correct the transform. rotation_mode implicitly specifies how posture (forward, up and sideway direction) is calculated.

## Properties

- cubic_interp: bool = true [set set_cubic_interpolation; get get_cubic_interpolation]
  If true, the position between two cached points is interpolated cubically, and linearly otherwise. The points along the Curve3D of the Path3D are precomputed before use, for faster calculations. The point at the requested offset is then calculated interpolating between two adjacent cached points. This may present a problem if the curve makes sharp turns, as the cached points may not follow the curve closely enough. There are two answers to this problem: either increase the number of cached points and increase memory consumption, or make a cubic interpolation between two points at the cost of (slightly) slower calculations.

- h_offset: float = 0.0 [set set_h_offset; get get_h_offset]
  The node's offset along the curve.

- loop: bool = true [set set_loop; get has_loop]
  If true, any offset outside the path's length will wrap around, instead of stopping at the ends. Use it for cyclic paths.

- progress: float = 0.0 [set set_progress; get get_progress]
  The distance from the first vertex, measured in 3D units along the path. Changing this value sets this node's position to a point within the path.

- progress_ratio: float = 0.0 [set set_progress_ratio; get get_progress_ratio]
  The distance from the first vertex, considering 0.0 as the first vertex and 1.0 as the last. This is just another way of expressing the progress within the path, as the progress supplied is multiplied internally by the path's length. It can be set or get only if the PathFollow3D is the child of a Path3D which is part of the scene tree, and that this Path3D has a Curve3D with a non-zero length. Otherwise, trying to set this field will print an error, and getting this field will return 0.0.

- rotation_mode: int (PathFollow3D.RotationMode) = 3 [set set_rotation_mode; get get_rotation_mode]
  Allows or forbids rotation on one or more axes, depending on the RotationMode constants being used.

- tilt_enabled: bool = true [set set_tilt_enabled; get is_tilt_enabled]
  If true, the tilt property of Curve3D takes effect.

- use_model_front: bool = false [set set_use_model_front; get is_using_model_front]
  If true, the node moves on the travel path with orienting the +Z axis as forward. See also Vector3.FORWARD and Vector3.MODEL_FRONT.

- v_offset: float = 0.0 [set set_v_offset; get get_v_offset]
  The node's offset perpendicular to the curve.

## Constants

### Enum RotationMode

- ROTATION_NONE = 0
  Forbids the PathFollow3D to rotate.

- ROTATION_Y = 1
  Allows the PathFollow3D to rotate in the Y axis only.

- ROTATION_XY = 2
  Allows the PathFollow3D to rotate in both the X, and Y axes.

- ROTATION_XYZ = 3
  Allows the PathFollow3D to rotate in any axis.

- ROTATION_ORIENTED = 4
  Uses the up vector information in a Curve3D to enforce orientation. This rotation mode requires the Path3D's Curve3D.up_vector_enabled property to be set to true.
