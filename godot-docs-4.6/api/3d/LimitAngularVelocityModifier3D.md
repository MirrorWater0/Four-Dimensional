# LimitAngularVelocityModifier3D

## Meta

- Name: LimitAngularVelocityModifier3D
- Source: LimitAngularVelocityModifier3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: LimitAngularVelocityModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

Limit bone rotation angular velocity.

## Description

This modifier limits bone rotation angular velocity by comparing poses between previous and current frame. You can add bone chains by specifying their root and end bones, then add the bones between them to a list. Modifier processes either that list or the bones excluding those in the list depending on the option exclude.

## Quick Reference

```
[methods]
clear_chains() -> void
get_end_bone(index: int) -> int [const]
get_end_bone_name(index: int) -> String [const]
get_root_bone(index: int) -> int [const]
get_root_bone_name(index: int) -> String [const]
reset() -> void
set_end_bone(index: int, bone: int) -> void
set_end_bone_name(index: int, bone_name: String) -> void
set_root_bone(index: int, bone: int) -> void
set_root_bone_name(index: int, bone_name: String) -> void

[properties]
chain_count: int = 0
exclude: bool = false
joint_count: int = 0
max_angular_velocity: float = 6.2831855
```

## Methods

- clear_chains() -> void
  Clear all chains.

- get_end_bone(index: int) -> int [const]
  Returns the end bone index of the bone chain.

- get_end_bone_name(index: int) -> String [const]
  Returns the end bone name of the bone chain.

- get_root_bone(index: int) -> int [const]
  Returns the root bone index of the bone chain.

- get_root_bone_name(index: int) -> String [const]
  Returns the root bone name of the bone chain.

- reset() -> void
  Sets the reference pose for angle comparison to the current pose with the influence of constraints removed. This function is automatically triggered when joints change or upon activation.

- set_end_bone(index: int, bone: int) -> void
  Sets the end bone index of the bone chain.

- set_end_bone_name(index: int, bone_name: String) -> void
  Sets the end bone name of the bone chain. **Note:** End bone must be the root bone or a child of the root bone.

- set_root_bone(index: int, bone: int) -> void
  Sets the root bone index of the bone chain.

- set_root_bone_name(index: int, bone_name: String) -> void
  Sets the root bone name of the bone chain.

## Properties

- chain_count: int = 0 [set set_chain_count; get get_chain_count]
  The number of chains.

- exclude: bool = false [set set_exclude; get is_exclude]
  If true, the modifier processes bones not included in the bone list. If false, the bones processed by the modifier are equal to the bone list.

- joint_count: int = 0 [get _get_joint_count]
  The number of joints in the list which created by chains dynamically.

- max_angular_velocity: float = 6.2831855 [set set_max_angular_velocity; get get_max_angular_velocity]
  The maximum angular velocity per second.
