# VisualShaderNodeVarying

## Meta

- Name: VisualShaderNodeVarying
- Source: VisualShaderNodeVarying.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeVarying -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A visual shader node that represents a "varying" shader value.

## Description

Varying values are shader variables that can be passed between shader functions, e.g. from Vertex shader to Fragment shader.

## Quick Reference

```
[properties]
varying_name: String = "[None]"
varying_type: int (VisualShader.VaryingType) = 0
```

## Properties

- varying_name: String = "[None]" [set set_varying_name; get get_varying_name]
  Name of the variable. Must be unique.

- varying_type: int (VisualShader.VaryingType) = 0 [set set_varying_type; get get_varying_type]
  Type of the variable. Determines where the variable can be accessed.
