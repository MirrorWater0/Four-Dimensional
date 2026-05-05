# IKModifier3D

## Meta

- Name: IKModifier3D
- Source: IKModifier3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: IKModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A node for inverse kinematics which may modify more than one bone.

## Description

Base class of SkeletonModifier3Ds that has some joint lists and applies inverse kinematics. This class has some structs, enums, and helper methods which are useful to solve inverse kinematics.

## Quick Reference

```
[methods]
clear_settings() -> void
get_setting_count() -> int [const]
reset() -> void
set_setting_count(count: int) -> void

[properties]
mutable_bone_axes: bool = true
```

## Tutorials

- [Inverse Kinematics Returns to Godot 4.6 - IKModifier3D](https://godotengine.org/article/inverse-kinematics-returns-to-godot-4-6/#ikmodifier3d-and-7-child-classes)

## Methods

- clear_settings() -> void
  Clears all settings.

- get_setting_count() -> int [const]
  Returns the number of settings.

- reset() -> void
  Resets a state with respect to the current bone pose.

- set_setting_count(count: int) -> void
  Sets the number of settings.

## Properties

- mutable_bone_axes: bool = true [set set_mutable_bone_axes; get are_bone_axes_mutable]
  If true, the solver retrieves the bone axis from the bone pose every frame. If false, the solver retrieves the bone axis from the bone rest and caches it, which increases performance slightly, but position changes in the bone pose made before processing this IKModifier3D are ignored.
