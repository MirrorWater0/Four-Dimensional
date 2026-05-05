# RDFramebufferPass

## Meta

- Name: RDFramebufferPass
- Source: RDFramebufferPass.xml
- Inherits: RefCounted
- Inheritance Chain: RDFramebufferPass -> RefCounted -> Object

## Brief Description

Framebuffer pass attachment description (used by RenderingDevice).

## Description

This class contains the list of attachment descriptions for a framebuffer pass. Each points with an index to a previously supplied list of texture attachments. Multipass framebuffers can optimize some configurations in mobile. On desktop, they provide little to no advantage. This object is used by RenderingDevice.

## Quick Reference

```
[properties]
color_attachments: PackedInt32Array = PackedInt32Array()
depth_attachment: int = -1
input_attachments: PackedInt32Array = PackedInt32Array()
preserve_attachments: PackedInt32Array = PackedInt32Array()
resolve_attachments: PackedInt32Array = PackedInt32Array()
```

## Properties

- color_attachments: PackedInt32Array = PackedInt32Array() [set set_color_attachments; get get_color_attachments]
  Color attachments in order starting from 0. If this attachment is not used by the shader, pass ATTACHMENT_UNUSED to skip.

- depth_attachment: int = -1 [set set_depth_attachment; get get_depth_attachment]
  Depth attachment. ATTACHMENT_UNUSED should be used if no depth buffer is required for this pass.

- input_attachments: PackedInt32Array = PackedInt32Array() [set set_input_attachments; get get_input_attachments]
  Used for multipass framebuffers (more than one render pass). Converts an attachment to an input. Make sure to also supply it properly in the RDUniform for the uniform set.

- preserve_attachments: PackedInt32Array = PackedInt32Array() [set set_preserve_attachments; get get_preserve_attachments]
  Attachments to preserve in this pass (otherwise they are erased).

- resolve_attachments: PackedInt32Array = PackedInt32Array() [set set_resolve_attachments; get get_resolve_attachments]
  If the color attachments are multisampled, non-multisampled resolve attachments can be provided.

## Constants

- ATTACHMENT_UNUSED = -1
  Attachment is unused.
