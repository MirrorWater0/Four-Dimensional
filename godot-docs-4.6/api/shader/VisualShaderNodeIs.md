# VisualShaderNodeIs

## Meta

- Name: VisualShaderNodeIs
- Source: VisualShaderNodeIs.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeIs -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A boolean comparison operator to be used within the visual shader graph.

## Description

Returns the boolean result of the comparison between INF or NaN and a scalar parameter.

## Quick Reference

```
[properties]
function: int (VisualShaderNodeIs.Function) = 0
```

## Properties

- function: int (VisualShaderNodeIs.Function) = 0 [set set_function; get get_function]
  The comparison function.

## Constants

### Enum Function

- FUNC_IS_INF = 0
  Comparison with INF (Infinity).

- FUNC_IS_NAN = 1
  Comparison with NaN (Not a Number; indicates invalid numeric results, such as division by zero).

- FUNC_MAX = 2
  Represents the size of the Function enum.
