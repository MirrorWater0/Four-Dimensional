# RDVertexAttribute

## Meta

- Name: RDVertexAttribute
- Source: RDVertexAttribute.xml
- Inherits: RefCounted
- Inheritance Chain: RDVertexAttribute -> RefCounted -> Object

## Brief Description

Vertex attribute (used by RenderingDevice).

## Description

This object is used by RenderingDevice.

## Quick Reference

```
[properties]
binding: int = 4294967295
format: int (RenderingDevice.DataFormat) = 232
frequency: int (RenderingDevice.VertexFrequency) = 0
location: int = 0
offset: int = 0
stride: int = 0
```

## Properties

- binding: int = 4294967295 [set set_binding; get get_binding]
  The index of the buffer in the vertex buffer array to bind this vertex attribute. When set to -1, it defaults to the index of the attribute. **Note:** You cannot mix binding explicitly assigned attributes with implicitly assigned ones (i.e. -1). Either all attributes must have their binding set to -1, or all must have explicit bindings.

- format: int (RenderingDevice.DataFormat) = 232 [set set_format; get get_format]
  The way that this attribute's data is interpreted when sent to a shader.

- frequency: int (RenderingDevice.VertexFrequency) = 0 [set set_frequency; get get_frequency]
  The rate at which this attribute is pulled from its vertex buffer.

- location: int = 0 [set set_location; get get_location]
  The location in the shader that this attribute is bound to.

- offset: int = 0 [set set_offset; get get_offset]
  The number of bytes between the start of the vertex buffer and the first instance of this attribute.

- stride: int = 0 [set set_stride; get get_stride]
  The number of bytes between the starts of consecutive instances of this attribute.
