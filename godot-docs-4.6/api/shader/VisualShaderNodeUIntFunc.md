# VisualShaderNodeUIntFunc

## Meta

- Name: VisualShaderNodeUIntFunc
- Source: VisualShaderNodeUIntFunc.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeUIntFunc -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

An unsigned scalar integer function to be used within the visual shader graph.

## Description

Accept an unsigned integer scalar (x) to the input port and transform it according to function.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeUIntFunc.Function) = 0
```

## Properties

- function: int (VisualShaderNodeUIntFunc.Function) = 0 [set set_function; get get_function]
  A function to be applied to the scalar.

## Constants

### Enum Function

- FUNC_NEGATE = 0
  Negates the x using -(x).

- FUNC_BITWISE_NOT = 1
  Returns the result of bitwise NOT operation on the integer. Translates to ~a in the Godot Shader Language.

- FUNC_MAX = 2
  Represents the size of the Function enum.
