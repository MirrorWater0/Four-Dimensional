# VisualShaderNodeUVFunc

## Meta

- Name: VisualShaderNodeUVFunc
- Source: VisualShaderNodeUVFunc.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeUVFunc -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Contains functions to modify texture coordinates (uv) to be used within the visual shader graph.

## Description

UV functions are similar to Vector2 functions, but the input port of this node uses the shader's UV value by default.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeUVFunc.Function) = 0
```

## Properties

- function: int (VisualShaderNodeUVFunc.Function) = 0 [set set_function; get get_function]
  A function to be applied to the texture coordinates.

## Constants

### Enum Function

- FUNC_PANNING = 0
  Translates uv by using scale and offset values using the following formula: uv = uv + offset * scale. uv port is connected to UV built-in by default.

- FUNC_SCALING = 1
  Scales uv by using scale and pivot values using the following formula: uv = (uv - pivot) * scale + pivot. uv port is connected to UV built-in by default.

- FUNC_MAX = 2
  Represents the size of the Function enum.
