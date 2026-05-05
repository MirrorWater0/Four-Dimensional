# BoneAttachment3D

## Meta

- Name: BoneAttachment3D
- Source: BoneAttachment3D.xml
- Inherits: Node3D
- Inheritance Chain: BoneAttachment3D -> Node3D -> Node -> Object

## Brief Description

А node that dynamically copies or overrides the 3D transform of a bone in its parent Skeleton3D.

## Description

This node selects a bone in a Skeleton3D and attaches to it. This means that the BoneAttachment3D node will either dynamically copy or override the 3D transform of the selected bone.

## Quick Reference

```
[methods]
get_skeleton() -> Skeleton3D
on_skeleton_update() -> void

[properties]
bone_idx: int = -1
bone_name: String = ""
external_skeleton: NodePath
override_pose: bool = false
physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2
use_external_skeleton: bool = false
```

## Methods

- get_skeleton() -> Skeleton3D
  Returns the parent or external Skeleton3D node if it exists, otherwise returns null.

- on_skeleton_update() -> void
  A function that is called automatically when the Skeleton3D is updated. This function is where the BoneAttachment3D node updates its position so it is correctly bound when it is *not* set to override the bone pose.

## Properties

- bone_idx: int = -1 [set set_bone_idx; get get_bone_idx]
  The index of the attached bone.

- bone_name: String = "" [set set_bone_name; get get_bone_name]
  The name of the attached bone.

- external_skeleton: NodePath [set set_external_skeleton; get get_external_skeleton]
  The NodePath to the external Skeleton3D node.

- override_pose: bool = false [set set_override_pose; get get_override_pose]
  Whether the BoneAttachment3D node will override the bone pose of the bone it is attached to. When set to true, the BoneAttachment3D node can change the pose of the bone. When set to false, the BoneAttachment3D will always be set to the bone's transform. **Note:** This override performs interruptively in the skeleton update process using signals due to the old design. It may cause unintended behavior when used at the same time with SkeletonModifier3D.

- physics_interpolation_mode: int (Node.PhysicsInterpolationMode) = 2 [set set_physics_interpolation_mode; get get_physics_interpolation_mode; override Node]

- use_external_skeleton: bool = false [set set_use_external_skeleton; get get_use_external_skeleton]
  Whether the BoneAttachment3D node will use an external Skeleton3D node rather than attempting to use its parent node as the Skeleton3D. When set to true, the BoneAttachment3D node will use the external Skeleton3D node set in external_skeleton.
