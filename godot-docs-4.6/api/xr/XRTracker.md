# XRTracker

## Meta

- Name: XRTracker
- Source: XRTracker.xml
- Inherits: RefCounted
- Inheritance Chain: XRTracker -> RefCounted -> Object

## Brief Description

A tracked object.

## Description

This object is the base of all XR trackers.

## Quick Reference

```
[properties]
description: String = ""
name: StringName = &"Unknown"
type: int (XRServer.TrackerType) = 128
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Properties

- description: String = "" [set set_tracker_desc; get get_tracker_desc]
  The description of this tracker.

- name: StringName = &"Unknown" [set set_tracker_name; get get_tracker_name]
  The unique name of this tracker. The trackers that are available differ between various XR runtimes and can often be configured by the user. Godot maintains a number of reserved names that it expects the XRInterface to implement if applicable: - "head" identifies the XRPositionalTracker of the player's head - "left_hand" identifies the XRControllerTracker in the player's left hand - "right_hand" identifies the XRControllerTracker in the player's right hand - "/user/hand_tracker/left" identifies the XRHandTracker for the player's left hand - "/user/hand_tracker/right" identifies the XRHandTracker for the player's right hand - "/user/body_tracker" identifies the XRBodyTracker for the player's body - "/user/face_tracker" identifies the XRFaceTracker for the player's face

- type: int (XRServer.TrackerType) = 128 [set set_tracker_type; get get_tracker_type]
  The type of tracker.
