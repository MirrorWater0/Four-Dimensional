# RetargetModifier3D

## Meta

- Name: RetargetModifier3D
- Source: RetargetModifier3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: RetargetModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A modifier to transfer parent skeleton poses (or global poses) to child skeletons in model space with different rests.

## Description

Retrieves the pose (or global pose) relative to the parent Skeleton's rest in model space and transfers it to the child Skeleton. This modifier rewrites the pose of the child skeleton directly in the parent skeleton's update process. This means that it overwrites the mapped bone pose set in the normal process on the target skeleton. If you want to set the target skeleton bone pose after retargeting, you will need to add a SkeletonModifier3D child to the target skeleton and thereby modify the pose. **Note:** When the use_global_pose is enabled, even if it is an unmapped bone, it can cause visual problems because the global pose is applied ignoring the parent bone's pose **if it has mapped bone children**. See also use_global_pose.

## Quick Reference

```
[methods]
is_position_enabled() -> bool [const]
is_rotation_enabled() -> bool [const]
is_scale_enabled() -> bool [const]
set_position_enabled(enabled: bool) -> void
set_rotation_enabled(enabled: bool) -> void
set_scale_enabled(enabled: bool) -> void

[properties]
enable: int (RetargetModifier3D.TransformFlag) = 7
profile: SkeletonProfile
use_global_pose: bool = false
```

## Methods

- is_position_enabled() -> bool [const]
  Returns true if enable has TRANSFORM_FLAG_POSITION.

- is_rotation_enabled() -> bool [const]
  Returns true if enable has TRANSFORM_FLAG_ROTATION.

- is_scale_enabled() -> bool [const]
  Returns true if enable has TRANSFORM_FLAG_SCALE.

- set_position_enabled(enabled: bool) -> void
  Sets TRANSFORM_FLAG_POSITION into enable.

- set_rotation_enabled(enabled: bool) -> void
  Sets TRANSFORM_FLAG_ROTATION into enable.

- set_scale_enabled(enabled: bool) -> void
  Sets TRANSFORM_FLAG_SCALE into enable.

## Properties

- enable: int (RetargetModifier3D.TransformFlag) = 7 [set set_enable_flags; get get_enable_flags]
  Flags to control the process of the transform elements individually when use_global_pose is disabled.

- profile: SkeletonProfile [set set_profile; get get_profile]
  SkeletonProfile for retargeting bones with names matching the bone list.

- use_global_pose: bool = false [set set_use_global_pose; get is_using_global_pose]
  If false, in case the target skeleton has fewer bones than the source skeleton, the source bone parent's transform will be ignored. Instead, it is possible to retarget between models with different body shapes, and position, rotation, and scale can be retargeted separately. If true, retargeting is performed taking into account global pose. In case the target skeleton has fewer bones than the source skeleton, the source bone parent's transform is taken into account. However, bone length between skeletons must match exactly, if not, the bones will be forced to expand or shrink. This is useful for using dummy bone with length 0 to match postures when retargeting between models with different number of bones.

## Constants

### Enum TransformFlag

- TRANSFORM_FLAG_POSITION = 1 [bitfield]
  If set, allows to retarget the position.

- TRANSFORM_FLAG_ROTATION = 2 [bitfield]
  If set, allows to retarget the rotation.

- TRANSFORM_FLAG_SCALE = 4 [bitfield]
  If set, allows to retarget the scale.

- TRANSFORM_FLAG_ALL = 7 [bitfield]
  If set, allows to retarget the position/rotation/scale.
