# VisualShaderNodeIntFunc

## Meta

- Name: VisualShaderNodeIntFunc
- Source: VisualShaderNodeIntFunc.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeIntFunc -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A scalar integer function to be used within the visual shader graph.

## Description

Accept an integer scalar (x) to the input port and transform it according to function.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeIntFunc.Function) = 2
```

## Properties

- function: int (VisualShaderNodeIntFunc.Function) = 2 [set set_function; get get_function]
  A function to be applied to the scalar.

## Constants

### Enum Function

- FUNC_ABS = 0
  Returns the absolute value of the parameter. Translates to abs(x) in the Godot Shader Language.

- FUNC_NEGATE = 1
  Negates the x using -(x).

- FUNC_SIGN = 2
  Extracts the sign of the parameter. Translates to sign(x) in the Godot Shader Language.

- FUNC_BITWISE_NOT = 3
  Returns the result of bitwise NOT operation on the integer. Translates to ~a in the Godot Shader Language.

- FUNC_MAX = 4
  Represents the size of the Function enum.
