# XRFaceModifier3D

## Meta

- Name: XRFaceModifier3D
- Source: XRFaceModifier3D.xml
- Inherits: Node3D
- Inheritance Chain: XRFaceModifier3D -> Node3D -> Node -> Object

## Brief Description

A node for driving standard face meshes from XRFaceTracker weights.

## Description

This node applies weights from an XRFaceTracker to a mesh with supporting face blend shapes. The [Unified Expressions](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes) blend shapes are supported, as well as ARKit and SRanipal blend shapes. The node attempts to identify blend shapes based on name matching. Blend shapes should match the names listed in the [Unified Expressions Compatibility](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/compatibility/overview) chart.

## Quick Reference

```
[properties]
face_tracker: StringName = &"/user/face_tracker"
target: NodePath = NodePath("")
```

## Tutorials

- [XR documentation index]($DOCS_URL/tutorials/xr/index.html)

## Properties

- face_tracker: StringName = &"/user/face_tracker" [set set_face_tracker; get get_face_tracker]
  The XRFaceTracker path.

- target: NodePath = NodePath("") [set set_target; get get_target]
  The NodePath of the face MeshInstance3D.
