# XRHandModifier3D

## Meta

- Name: XRHandModifier3D
- Source: XRHandModifier3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: XRHandModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A node for driving hand meshes from XRHandTracker data.

## Description

This node uses hand tracking data from an XRHandTracker to pose the skeleton of a hand mesh. Positioning of hands is performed by creating an XRNode3D ancestor of the hand mesh driven by the same XRHandTracker. The hand tracking position-data is scaled by Skeleton3D.motion_scale when applied to the skeleton, which can be used to adjust the tracked hand to match the scale of the hand model.

## Quick Reference

```
[properties]
bone_update: int (XRHandModifier3D.BoneUpdate) = 0
hand_tracker: StringName = &"/user/hand_tracker/left"
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Properties

- bone_update: int (XRHandModifier3D.BoneUpdate) = 0 [set set_bone_update; get get_bone_update]
  Specifies the type of updates to perform on the bones.

- hand_tracker: StringName = &"/user/hand_tracker/left" [set set_hand_tracker; get get_hand_tracker]
  The name of the XRHandTracker registered with XRServer to obtain the hand tracking data from.

## Constants

### Enum BoneUpdate

- BONE_UPDATE_FULL = 0
  The skeleton's bones are fully updated (both position and rotation) to match the tracked bones.

- BONE_UPDATE_ROTATION_ONLY = 1
  The skeleton's bones are only rotated to align with the tracked bones, preserving bone length.

- BONE_UPDATE_MAX = 2
  Represents the size of the BoneUpdate enum.
