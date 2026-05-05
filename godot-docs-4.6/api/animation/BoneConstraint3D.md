# BoneConstraint3D

## Meta

- Name: BoneConstraint3D
- Source: BoneConstraint3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: BoneConstraint3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A node that may modify Skeleton3D's bone with associating the two bones.

## Description

Base class of SkeletonModifier3D that modifies the bone set in set_apply_bone() based on the transform of the bone retrieved by get_reference_bone().

## Quick Reference

```
[methods]
clear_setting() -> void
get_amount(index: int) -> float [const]
get_apply_bone(index: int) -> int [const]
get_apply_bone_name(index: int) -> String [const]
get_reference_bone(index: int) -> int [const]
get_reference_bone_name(index: int) -> String [const]
get_reference_node(index: int) -> NodePath [const]
get_reference_type(index: int) -> int (BoneConstraint3D.ReferenceType) [const]
get_setting_count() -> int [const]
set_amount(index: int, amount: float) -> void
set_apply_bone(index: int, bone: int) -> void
set_apply_bone_name(index: int, bone_name: String) -> void
set_reference_bone(index: int, bone: int) -> void
set_reference_bone_name(index: int, bone_name: String) -> void
set_reference_node(index: int, node: NodePath) -> void
set_reference_type(index: int, type: int (BoneConstraint3D.ReferenceType)) -> void
set_setting_count(count: int) -> void
```

## Methods

- clear_setting() -> void
  Clear all settings.

- get_amount(index: int) -> float [const]
  Returns the apply amount of the setting at index.

- get_apply_bone(index: int) -> int [const]
  Returns the apply bone of the setting at index. This bone will be modified.

- get_apply_bone_name(index: int) -> String [const]
  Returns the apply bone name of the setting at index. This bone will be modified.

- get_reference_bone(index: int) -> int [const]
  Returns the reference bone of the setting at index. This bone will be only referenced and not modified by this modifier.

- get_reference_bone_name(index: int) -> String [const]
  Returns the reference bone name of the setting at index. This bone will be only referenced and not modified by this modifier.

- get_reference_node(index: int) -> NodePath [const]
  Returns the reference node path of the setting at index. This node will be only referenced and not modified by this modifier.

- get_reference_type(index: int) -> int (BoneConstraint3D.ReferenceType) [const]
  Returns the reference target type of the setting at index. See also ReferenceType.

- get_setting_count() -> int [const]
  Returns the number of settings in the modifier.

- set_amount(index: int, amount: float) -> void
  Sets the apply amount of the setting at index to amount.

- set_apply_bone(index: int, bone: int) -> void
  Sets the apply bone of the setting at index to bone. This bone will be modified.

- set_apply_bone_name(index: int, bone_name: String) -> void
  Sets the apply bone of the setting at index to bone_name. This bone will be modified.

- set_reference_bone(index: int, bone: int) -> void
  Sets the reference bone of the setting at index to bone. This bone will be only referenced and not modified by this modifier.

- set_reference_bone_name(index: int, bone_name: String) -> void
  Sets the reference bone of the setting at index to bone_name. This bone will be only referenced and not modified by this modifier.

- set_reference_node(index: int, node: NodePath) -> void
  Sets the reference node path of the setting at index to node. This node will be only referenced and not modified by this modifier.

- set_reference_type(index: int, type: int (BoneConstraint3D.ReferenceType)) -> void
  Sets the reference target type of the setting at index to type. See also ReferenceType.

- set_setting_count(count: int) -> void
  Sets the number of settings in the modifier.

## Constants

### Enum ReferenceType

- REFERENCE_TYPE_BONE = 0
  The reference target is a bone. In this case, the reference target spaces is local space.

- REFERENCE_TYPE_NODE = 1
  The reference target is a Node3D. In this case, the reference target spaces is model space. In other words, the reference target's coordinates are treated as if it were placed directly under Skeleton3D which parent of the BoneConstraint3D.
