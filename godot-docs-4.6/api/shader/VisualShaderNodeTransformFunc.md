# VisualShaderNodeTransformFunc

## Meta

- Name: VisualShaderNodeTransformFunc
- Source: VisualShaderNodeTransformFunc.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeTransformFunc -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Computes a Transform3D function within the visual shader graph.

## Description

Computes an inverse or transpose function on the provided Transform3D.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeTransformFunc.Function) = 0
```

## Properties

- function: int (VisualShaderNodeTransformFunc.Function) = 0 [set set_function; get get_function]
  The function to be computed.

## Constants

### Enum Function

- FUNC_INVERSE = 0
  Perform the inverse operation on the Transform3D matrix.

- FUNC_TRANSPOSE = 1
  Perform the transpose operation on the Transform3D matrix.

- FUNC_MAX = 2
  Represents the size of the Function enum.
