# VisualShaderNodeSample3D

## Meta

- Name: VisualShaderNodeSample3D
- Source: VisualShaderNodeSample3D.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeSample3D -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A base node for nodes which samples 3D textures in the visual shader graph.

## Description

A virtual class, use the descendants instead.

## Quick Reference

```
[properties]
source: int (VisualShaderNodeSample3D.Source) = 0
```

## Properties

- source: int (VisualShaderNodeSample3D.Source) = 0 [set set_source; get get_source]
  An input source type.

## Constants

### Enum Source

- SOURCE_TEXTURE = 0
  Creates internal uniform and provides a way to assign it within node.

- SOURCE_PORT = 1
  Use the uniform texture from sampler port.

- SOURCE_MAX = 2
  Represents the size of the Source enum.
