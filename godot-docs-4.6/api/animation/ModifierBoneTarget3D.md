# ModifierBoneTarget3D

## Meta

- Name: ModifierBoneTarget3D
- Source: ModifierBoneTarget3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: ModifierBoneTarget3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

А node that dynamically copies the 3D transform of a bone in its parent Skeleton3D.

## Description

This node selects a bone in a Skeleton3D and attaches to it. This means that the ModifierBoneTarget3D node will dynamically copy the 3D transform of the selected bone. The functionality is similar to BoneAttachment3D, but this node adopts the SkeletonModifier3D cycle and is intended to be used as another SkeletonModifier3D's target.

## Quick Reference

```
[properties]
bone: int = -1
bone_name: String = ""
```

## Properties

- bone: int = -1 [set set_bone; get get_bone]
  The index of the attached bone.

- bone_name: String = "" [set set_bone_name; get get_bone_name]
  The name of the attached bone.
