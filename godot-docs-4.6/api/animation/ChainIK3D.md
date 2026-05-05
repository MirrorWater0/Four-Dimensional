# ChainIK3D

## Meta

- Name: ChainIK3D
- Source: ChainIK3D.xml
- Inherits: IKModifier3D
- Inheritance Chain: ChainIK3D -> IKModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A SkeletonModifier3D to apply inverse kinematics to bone chains containing an arbitrary number of bones.

## Description

Base class of SkeletonModifier3D that automatically generates a joint list from the bones between the root bone and the end bone.

## Quick Reference

```
[methods]
get_end_bone(index: int) -> int [const]
get_end_bone_direction(index: int) -> int (SkeletonModifier3D.BoneDirection) [const]
get_end_bone_length(index: int) -> float [const]
get_end_bone_name(index: int) -> String [const]
get_joint_bone(index: int, joint: int) -> int [const]
get_joint_bone_name(index: int, joint: int) -> String [const]
get_joint_count(index: int) -> int [const]
get_root_bone(index: int) -> int [const]
get_root_bone_name(index: int) -> String [const]
is_end_bone_extended(index: int) -> bool [const]
set_end_bone(index: int, bone: int) -> void
set_end_bone_direction(index: int, bone_direction: int (SkeletonModifier3D.BoneDirection)) -> void
set_end_bone_length(index: int, length: float) -> void
set_end_bone_name(index: int, bone_name: String) -> void
set_extend_end_bone(index: int, enabled: bool) -> void
set_root_bone(index: int, bone: int) -> void
set_root_bone_name(index: int, bone_name: String) -> void
```

## Methods

- get_end_bone(index: int) -> int [const]
  Returns the end bone index of the bone chain.

- get_end_bone_direction(index: int) -> int (SkeletonModifier3D.BoneDirection) [const]
  Returns the tail direction of the end bone of the bone chain when is_end_bone_extended() is true.

- get_end_bone_length(index: int) -> float [const]
  Returns the end bone tail length of the bone chain when is_end_bone_extended() is true.

- get_end_bone_name(index: int) -> String [const]
  Returns the end bone name of the bone chain.

- get_joint_bone(index: int, joint: int) -> int [const]
  Returns the bone index at joint in the bone chain's joint list.

- get_joint_bone_name(index: int, joint: int) -> String [const]
  Returns the bone name at joint in the bone chain's joint list.

- get_joint_count(index: int) -> int [const]
  Returns the joint count of the bone chain's joint list.

- get_root_bone(index: int) -> int [const]
  Returns the root bone index of the bone chain.

- get_root_bone_name(index: int) -> String [const]
  Returns the root bone name of the bone chain.

- is_end_bone_extended(index: int) -> bool [const]
  Returns true if the end bone is extended to have a tail.

- set_end_bone(index: int, bone: int) -> void
  Sets the end bone index of the bone chain.

- set_end_bone_direction(index: int, bone_direction: int (SkeletonModifier3D.BoneDirection)) -> void
  Sets the end bone tail direction of the bone chain when is_end_bone_extended() is true.

- set_end_bone_length(index: int, length: float) -> void
  Sets the end bone tail length of the bone chain when is_end_bone_extended() is true.

- set_end_bone_name(index: int, bone_name: String) -> void
  Sets the end bone name of the bone chain. **Note:** The end bone must be the root bone or a child of the root bone. If they are the same, the tail must be extended by set_extend_end_bone() to modify the bone.

- set_extend_end_bone(index: int, enabled: bool) -> void
  If enabled is true, the end bone is extended to have a tail. The extended tail config is allocated to the last element in the joint list. In other words, if you set enabled to false, the config of the last element in the joint list has no effect in the simulated result.

- set_root_bone(index: int, bone: int) -> void
  Sets the root bone index of the bone chain.

- set_root_bone_name(index: int, bone_name: String) -> void
  Sets the root bone name of the bone chain.
