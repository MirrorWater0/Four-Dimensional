# RDPipelineColorBlendState

## Meta

- Name: RDPipelineColorBlendState
- Source: RDPipelineColorBlendState.xml
- Inherits: RefCounted
- Inheritance Chain: RDPipelineColorBlendState -> RefCounted -> Object

## Brief Description

Pipeline color blend state (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[properties]
attachments: RDPipelineColorBlendStateAttachment[] = []
blend_constant: Color = Color(0, 0, 0, 1)
enable_logic_op: bool = false
logic_op: int (RenderingDevice.LogicOperation) = 0
```

## Properties

- attachments: RDPipelineColorBlendStateAttachment[] = [] [set set_attachments; get get_attachments]
  The attachments that are blended together.

- blend_constant: Color = Color(0, 0, 0, 1) [set set_blend_constant; get get_blend_constant]
  The constant color to blend with. See also RenderingDevice.draw_list_set_blend_constants().

- enable_logic_op: bool = false [set set_enable_logic_op; get get_enable_logic_op]
  If true, performs the logic operation defined in logic_op.

- logic_op: int (RenderingDevice.LogicOperation) = 0 [set set_logic_op; get get_logic_op]
  The logic operation to perform for blending. Only effective if enable_logic_op is true.
