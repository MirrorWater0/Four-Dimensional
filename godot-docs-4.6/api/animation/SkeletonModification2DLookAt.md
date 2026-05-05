# SkeletonModification2DLookAt

## Meta

- Name: SkeletonModification2DLookAt
- Source: SkeletonModification2DLookAt.xml
- Inherits: SkeletonModification2D
- Inheritance Chain: SkeletonModification2DLookAt -> SkeletonModification2D -> Resource -> RefCounted -> Object

## Brief Description

A modification that rotates a Bone2D node to look at a target.

## Description

This SkeletonModification2D rotates a bone to look a target. This is extremely helpful for moving character's head to look at the player, rotating a turret to look at a target, or any other case where you want to make a bone rotate towards something quickly and easily.

## Quick Reference

```
[methods]
get_additional_rotation() -> float [const]
get_constraint_angle_invert() -> bool [const]
get_constraint_angle_max() -> float [const]
get_constraint_angle_min() -> float [const]
get_enable_constraint() -> bool [const]
set_additional_rotation(rotation: float) -> void
set_constraint_angle_invert(invert: bool) -> void
set_constraint_angle_max(angle_max: float) -> void
set_constraint_angle_min(angle_min: float) -> void
set_enable_constraint(enable_constraint: bool) -> void

[properties]
bone2d_node: NodePath = NodePath("")
bone_index: int = -1
target_nodepath: NodePath = NodePath("")
```

## Methods

- get_additional_rotation() -> float [const]
  Returns the amount of additional rotation that is applied after the LookAt modification executes.

- get_constraint_angle_invert() -> bool [const]
  Returns whether the constraints to this modification are inverted or not.

- get_constraint_angle_max() -> float [const]
  Returns the constraint's maximum allowed angle.

- get_constraint_angle_min() -> float [const]
  Returns the constraint's minimum allowed angle.

- get_enable_constraint() -> bool [const]
  Returns true if the LookAt modification is using constraints.

- set_additional_rotation(rotation: float) -> void
  Sets the amount of additional rotation that is to be applied after executing the modification. This allows for offsetting the results by the inputted rotation amount.

- set_constraint_angle_invert(invert: bool) -> void
  When true, the modification will use an inverted joint constraint. An inverted joint constraint only constraints the Bone2D to the angles *outside of* the inputted minimum and maximum angles. For this reason, it is referred to as an inverted joint constraint, as it constraints the joint to the outside of the inputted values.

- set_constraint_angle_max(angle_max: float) -> void
  Sets the constraint's maximum allowed angle.

- set_constraint_angle_min(angle_min: float) -> void
  Sets the constraint's minimum allowed angle.

- set_enable_constraint(enable_constraint: bool) -> void
  Sets whether this modification will use constraints or not. When true, constraints will be applied when solving the LookAt modification.

## Properties

- bone2d_node: NodePath = NodePath("") [set set_bone2d_node; get get_bone2d_node]
  The Bone2D node that the modification will operate on.

- bone_index: int = -1 [set set_bone_index; get get_bone_index]
  The index of the Bone2D node that the modification will operate on.

- target_nodepath: NodePath = NodePath("") [set set_target_node; get get_target_node]
  The NodePath to the node that is the target for the LookAt modification. This node is what the modification will rotate the Bone2D to.
