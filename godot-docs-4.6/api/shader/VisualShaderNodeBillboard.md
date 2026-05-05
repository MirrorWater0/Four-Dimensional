# VisualShaderNodeBillboard

## Meta

- Name: VisualShaderNodeBillboard
- Source: VisualShaderNodeBillboard.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeBillboard -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A node that controls how the object faces the camera to be used within the visual shader graph.

## Description

The output port of this node needs to be connected to Model View Matrix port of VisualShaderNodeOutput.

## Quick Reference

```
[properties]
billboard_type: int (VisualShaderNodeBillboard.BillboardType) = 1
keep_scale: bool = false
```

## Properties

- billboard_type: int (VisualShaderNodeBillboard.BillboardType) = 1 [set set_billboard_type; get get_billboard_type]
  Controls how the object faces the camera.

- keep_scale: bool = false [set set_keep_scale_enabled; get is_keep_scale_enabled]
  If true, the shader will keep the scale set for the mesh. Otherwise, the scale is lost when billboarding.

## Constants

### Enum BillboardType

- BILLBOARD_TYPE_DISABLED = 0
  Billboarding is disabled and the node does nothing.

- BILLBOARD_TYPE_ENABLED = 1
  A standard billboarding algorithm is enabled.

- BILLBOARD_TYPE_FIXED_Y = 2
  A billboarding algorithm to rotate around Y-axis is enabled.

- BILLBOARD_TYPE_PARTICLES = 3
  A billboarding algorithm designed to use on particles is enabled.

- BILLBOARD_TYPE_MAX = 4
  Represents the size of the BillboardType enum.
