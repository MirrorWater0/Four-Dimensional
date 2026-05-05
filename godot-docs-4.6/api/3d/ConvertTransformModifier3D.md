# ConvertTransformModifier3D

## Meta

- Name: ConvertTransformModifier3D
- Source: ConvertTransformModifier3D.xml
- Inherits: BoneConstraint3D
- Inheritance Chain: ConvertTransformModifier3D -> BoneConstraint3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A SkeletonModifier3D that apply transform to the bone which converted from reference.

## Description

Apply the copied transform of the bone set by BoneConstraint3D.set_reference_bone() to the bone set by BoneConstraint3D.set_apply_bone() about the specific axis with remapping it with some options. There are 4 ways to apply the transform, depending on the combination of set_relative() and set_additive(). **Relative + Additive:** - Extract reference pose relative to the rest and add it to the apply bone's pose. **Relative + Not Additive:** - Extract reference pose relative to the rest and add it to the apply bone's rest. **Not Relative + Additive:** - Extract reference pose absolutely and add it to the apply bone's pose. **Not Relative + Not Additive:** - Extract reference pose absolutely and the apply bone's pose is replaced with it. **Note:** Relative option is available only in the case BoneConstraint3D.get_reference_type() is BoneConstraint3D.REFERENCE_TYPE_BONE. See also BoneConstraint3D.ReferenceType. **Note:** If there is a rotation greater than 180 degrees with constrained axes, flipping may occur.

## Quick Reference

```
[methods]
get_apply_axis(index: int) -> int (Vector3.Axis) [const]
get_apply_range_max(index: int) -> float [const]
get_apply_range_min(index: int) -> float [const]
get_apply_transform_mode(index: int) -> int (ConvertTransformModifier3D.TransformMode) [const]
get_reference_axis(index: int) -> int (Vector3.Axis) [const]
get_reference_range_max(index: int) -> float [const]
get_reference_range_min(index: int) -> float [const]
get_reference_transform_mode(index: int) -> int (ConvertTransformModifier3D.TransformMode) [const]
is_additive(index: int) -> bool [const]
is_relative(index: int) -> bool [const]
set_additive(index: int, enabled: bool) -> void
set_apply_axis(index: int, axis: int (Vector3.Axis)) -> void
set_apply_range_max(index: int, range_max: float) -> void
set_apply_range_min(index: int, range_min: float) -> void
set_apply_transform_mode(index: int, transform_mode: int (ConvertTransformModifier3D.TransformMode)) -> void
set_reference_axis(index: int, axis: int (Vector3.Axis)) -> void
set_reference_range_max(index: int, range_max: float) -> void
set_reference_range_min(index: int, range_min: float) -> void
set_reference_transform_mode(index: int, transform_mode: int (ConvertTransformModifier3D.TransformMode)) -> void
set_relative(index: int, enabled: bool) -> void

[properties]
setting_count: int = 0
```

## Methods

- get_apply_axis(index: int) -> int (Vector3.Axis) [const]
  Returns the axis of the remapping destination transform.

- get_apply_range_max(index: int) -> float [const]
  Returns the maximum value of the remapping destination range.

- get_apply_range_min(index: int) -> float [const]
  Returns the minimum value of the remapping destination range.

- get_apply_transform_mode(index: int) -> int (ConvertTransformModifier3D.TransformMode) [const]
  Returns the operation of the remapping destination transform.

- get_reference_axis(index: int) -> int (Vector3.Axis) [const]
  Returns the axis of the remapping source transform.

- get_reference_range_max(index: int) -> float [const]
  Returns the maximum value of the remapping source range.

- get_reference_range_min(index: int) -> float [const]
  Returns the minimum value of the remapping source range.

- get_reference_transform_mode(index: int) -> int (ConvertTransformModifier3D.TransformMode) [const]
  Returns the operation of the remapping source transform.

- is_additive(index: int) -> bool [const]
  Returns true if the additive option is enabled in the setting at index.

- is_relative(index: int) -> bool [const]
  Returns true if the relative option is enabled in the setting at index.

- set_additive(index: int, enabled: bool) -> void
  Sets additive option in the setting at index to enabled. This mainly affects the process of applying transform to the BoneConstraint3D.set_apply_bone(). If sets enabled to true, the processed transform is added to the pose of the current apply bone. If sets enabled to false, the pose of the current apply bone is replaced with the processed transform. However, if set set_relative() to true, the transform is relative to rest.

- set_apply_axis(index: int, axis: int (Vector3.Axis)) -> void
  Sets the axis of the remapping destination transform.

- set_apply_range_max(index: int, range_max: float) -> void
  Sets the maximum value of the remapping destination range.

- set_apply_range_min(index: int, range_min: float) -> void
  Sets the minimum value of the remapping destination range.

- set_apply_transform_mode(index: int, transform_mode: int (ConvertTransformModifier3D.TransformMode)) -> void
  Sets the operation of the remapping destination transform.

- set_reference_axis(index: int, axis: int (Vector3.Axis)) -> void
  Sets the axis of the remapping source transform.

- set_reference_range_max(index: int, range_max: float) -> void
  Sets the maximum value of the remapping source range.

- set_reference_range_min(index: int, range_min: float) -> void
  Sets the minimum value of the remapping source range.

- set_reference_transform_mode(index: int, transform_mode: int (ConvertTransformModifier3D.TransformMode)) -> void
  Sets the operation of the remapping source transform.

- set_relative(index: int, enabled: bool) -> void
  Sets relative option in the setting at index to enabled. If sets enabled to true, the extracted and applying transform is relative to the rest. If sets enabled to false, the extracted transform is absolute.

## Properties

- setting_count: int = 0 [set set_setting_count; get get_setting_count]
  The number of settings in the modifier.

## Constants

### Enum TransformMode

- TRANSFORM_MODE_POSITION = 0
  Convert with position. Transfer the difference.

- TRANSFORM_MODE_ROTATION = 1
  Convert with rotation. The angle is the roll for the specified axis.

- TRANSFORM_MODE_SCALE = 2
  Convert with scale. Transfers the ratio, not the difference.
