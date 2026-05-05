# SkeletonModification2DTwoBoneIK

## Meta

- Name: SkeletonModification2DTwoBoneIK
- Source: SkeletonModification2DTwoBoneIK.xml
- Inherits: SkeletonModification2D
- Inheritance Chain: SkeletonModification2DTwoBoneIK -> SkeletonModification2D -> Resource -> RefCounted -> Object

## Brief Description

A modification that rotates two bones using the law of cosines to reach the target.

## Description

This SkeletonModification2D uses an algorithm typically called TwoBoneIK. This algorithm works by leveraging the law of cosines and the lengths of the bones to figure out what rotation the bones currently have, and what rotation they need to make a complete triangle, where the first bone, the second bone, and the target form the three vertices of the triangle. Because the algorithm works by making a triangle, it can only operate on two bones. TwoBoneIK is great for arms, legs, and really any joints that can be represented by just two bones that bend to reach a target. This solver is more lightweight than SkeletonModification2DFABRIK, but gives similar, natural looking results.

## Quick Reference

```
[methods]
get_joint_one_bone2d_node() -> NodePath [const]
get_joint_one_bone_idx() -> int [const]
get_joint_two_bone2d_node() -> NodePath [const]
get_joint_two_bone_idx() -> int [const]
set_joint_one_bone2d_node(bone2d_node: NodePath) -> void
set_joint_one_bone_idx(bone_idx: int) -> void
set_joint_two_bone2d_node(bone2d_node: NodePath) -> void
set_joint_two_bone_idx(bone_idx: int) -> void

[properties]
flip_bend_direction: bool = false
target_maximum_distance: float = 0.0
target_minimum_distance: float = 0.0
target_nodepath: NodePath = NodePath("")
```

## Methods

- get_joint_one_bone2d_node() -> NodePath [const]
  Returns the Bone2D node that is being used as the first bone in the TwoBoneIK modification.

- get_joint_one_bone_idx() -> int [const]
  Returns the index of the Bone2D node that is being used as the first bone in the TwoBoneIK modification.

- get_joint_two_bone2d_node() -> NodePath [const]
  Returns the Bone2D node that is being used as the second bone in the TwoBoneIK modification.

- get_joint_two_bone_idx() -> int [const]
  Returns the index of the Bone2D node that is being used as the second bone in the TwoBoneIK modification.

- set_joint_one_bone2d_node(bone2d_node: NodePath) -> void
  Sets the Bone2D node that is being used as the first bone in the TwoBoneIK modification.

- set_joint_one_bone_idx(bone_idx: int) -> void
  Sets the index of the Bone2D node that is being used as the first bone in the TwoBoneIK modification.

- set_joint_two_bone2d_node(bone2d_node: NodePath) -> void
  Sets the Bone2D node that is being used as the second bone in the TwoBoneIK modification.

- set_joint_two_bone_idx(bone_idx: int) -> void
  Sets the index of the Bone2D node that is being used as the second bone in the TwoBoneIK modification.

## Properties

- flip_bend_direction: bool = false [set set_flip_bend_direction; get get_flip_bend_direction]
  If true, the bones in the modification will bend outward as opposed to inwards when contracting. If false, the bones will bend inwards when contracting.

- target_maximum_distance: float = 0.0 [set set_target_maximum_distance; get get_target_maximum_distance]
  The maximum distance the target can be at. If the target is farther than this distance, the modification will solve as if it's at this maximum distance. When set to 0, the modification will solve without distance constraints.

- target_minimum_distance: float = 0.0 [set set_target_minimum_distance; get get_target_minimum_distance]
  The minimum distance the target can be at. If the target is closer than this distance, the modification will solve as if it's at this minimum distance. When set to 0, the modification will solve without distance constraints.

- target_nodepath: NodePath = NodePath("") [set set_target_node; get get_target_node]
  The NodePath to the node that is the target for the TwoBoneIK modification. This node is what the modification will use when bending the Bone2D nodes.
