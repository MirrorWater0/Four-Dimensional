# BoneMap

## Meta

- Name: BoneMap
- Source: BoneMap.xml
- Inherits: Resource
- Inheritance Chain: BoneMap -> Resource -> RefCounted -> Object

## Brief Description

Describes a mapping of bone names for retargeting Skeleton3D into common names defined by a SkeletonProfile.

## Description

This class contains a dictionary that uses a list of bone names in SkeletonProfile as key names. By assigning the actual Skeleton3D bone name as the key value, it maps the Skeleton3D to the SkeletonProfile.

## Quick Reference

```
[methods]
find_profile_bone_name(skeleton_bone_name: StringName) -> StringName [const]
get_skeleton_bone_name(profile_bone_name: StringName) -> StringName [const]
set_skeleton_bone_name(profile_bone_name: StringName, skeleton_bone_name: StringName) -> void

[properties]
profile: SkeletonProfile
```

## Tutorials

- [Retargeting 3D Skeletons]($DOCS_URL/tutorials/assets_pipeline/retargeting_3d_skeletons.html)

## Methods

- find_profile_bone_name(skeleton_bone_name: StringName) -> StringName [const]
  Returns a profile bone name having skeleton_bone_name. If not found, an empty StringName will be returned. In the retargeting process, the returned bone name is the bone name of the target skeleton.

- get_skeleton_bone_name(profile_bone_name: StringName) -> StringName [const]
  Returns a skeleton bone name is mapped to profile_bone_name. In the retargeting process, the returned bone name is the bone name of the source skeleton.

- set_skeleton_bone_name(profile_bone_name: StringName, skeleton_bone_name: StringName) -> void
  Maps a skeleton bone name to profile_bone_name. In the retargeting process, the setting bone name is the bone name of the source skeleton.

## Properties

- profile: SkeletonProfile [set set_profile; get get_profile]
  A SkeletonProfile of the mapping target. Key names in the BoneMap are synchronized with it.

## Signals

- bone_map_updated()
  This signal is emitted when change the key value in the BoneMap. This is used to validate mapping and to update BoneMap editor.

- profile_updated()
  This signal is emitted when change the value in profile or change the reference of profile. This is used to update key names in the BoneMap and to redraw the BoneMap editor.
