# VisualShaderNodeResizableBase

## Meta

- Name: VisualShaderNodeResizableBase
- Source: VisualShaderNodeResizableBase.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeResizableBase -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Base class for resizable nodes in a visual shader graph.

## Description

Resizable nodes have a handle that allows the user to adjust their size as needed.

## Quick Reference

```
[properties]
size: Vector2 = Vector2(0, 0)
```

## Properties

- size: Vector2 = Vector2(0, 0) [set set_size; get get_size]
  The size of the node in the visual shader graph.
