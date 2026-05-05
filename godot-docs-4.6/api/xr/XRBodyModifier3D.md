# XRBodyModifier3D

## Meta

- Name: XRBodyModifier3D
- Source: XRBodyModifier3D.xml
- Inherits: SkeletonModifier3D
- Inheritance Chain: XRBodyModifier3D -> SkeletonModifier3D -> Node3D -> Node -> Object

## Brief Description

A node for driving body meshes from XRBodyTracker data.

## Description

This node uses body tracking data from an XRBodyTracker to pose the skeleton of a body mesh. Positioning of the body is performed by creating an XRNode3D ancestor of the body mesh driven by the same XRBodyTracker. The body tracking position-data is scaled by Skeleton3D.motion_scale when applied to the skeleton, which can be used to adjust the tracked body to match the scale of the body model.

## Quick Reference

```
[properties]
body_tracker: StringName = &"/user/body_tracker"
body_update: int (XRBodyModifier3D.BodyUpdate) = 7
bone_update: int (XRBodyModifier3D.BoneUpdate) = 0
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Properties

- body_tracker: StringName = &"/user/body_tracker" [set set_body_tracker; get get_body_tracker]
  The name of the XRBodyTracker registered with XRServer to obtain the body tracking data from.

- body_update: int (XRBodyModifier3D.BodyUpdate) = 7 [set set_body_update; get get_body_update]
  Specifies the body parts to update.

- bone_update: int (XRBodyModifier3D.BoneUpdate) = 0 [set set_bone_update; get get_bone_update]
  Specifies the type of updates to perform on the bones.

## Constants

### Enum BodyUpdate

- BODY_UPDATE_UPPER_BODY = 1 [bitfield]
  The skeleton's upper body joints are updated.

- BODY_UPDATE_LOWER_BODY = 2 [bitfield]
  The skeleton's lower body joints are updated.

- BODY_UPDATE_HANDS = 4 [bitfield]
  The skeleton's hand joints are updated.

### Enum BoneUpdate

- BONE_UPDATE_FULL = 0
  The skeleton's bones are fully updated (both position and rotation) to match the tracked bones.

- BONE_UPDATE_ROTATION_ONLY = 1
  The skeleton's bones are only rotated to align with the tracked bones, preserving bone length.

- BONE_UPDATE_MAX = 2
  Represents the size of the BoneUpdate enum.
