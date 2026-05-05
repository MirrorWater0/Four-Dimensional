# VisualShaderNodeCompare

## Meta

- Name: VisualShaderNodeCompare
- Source: VisualShaderNodeCompare.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeCompare -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A comparison function for common types within the visual shader graph.

## Description

Compares a and b of type by function. Returns a boolean scalar. Translates to if instruction in shader code.

## Quick Reference

```
[properties]
condition: int (VisualShaderNodeCompare.Condition) = 0
function: int (VisualShaderNodeCompare.Function) = 0
type: int (VisualShaderNodeCompare.ComparisonType) = 0
```

## Properties

- condition: int (VisualShaderNodeCompare.Condition) = 0 [set set_condition; get get_condition]
  Extra condition which is applied if type is set to CTYPE_VECTOR_3D.

- function: int (VisualShaderNodeCompare.Function) = 0 [set set_function; get get_function]
  A comparison function.

- type: int (VisualShaderNodeCompare.ComparisonType) = 0 [set set_comparison_type; get get_comparison_type]
  The type to be used in the comparison.

## Constants

### Enum ComparisonType

- CTYPE_SCALAR = 0
  A floating-point scalar.

- CTYPE_SCALAR_INT = 1
  An integer scalar.

- CTYPE_SCALAR_UINT = 2
  An unsigned integer scalar.

- CTYPE_VECTOR_2D = 3
  A 2D vector type.

- CTYPE_VECTOR_3D = 4
  A 3D vector type.

- CTYPE_VECTOR_4D = 5
  A 4D vector type.

- CTYPE_BOOLEAN = 6
  A boolean type.

- CTYPE_TRANSFORM = 7
  A transform (mat4) type.

- CTYPE_MAX = 8
  Represents the size of the ComparisonType enum.

### Enum Function

- FUNC_EQUAL = 0
  Comparison for equality (a == b).

- FUNC_NOT_EQUAL = 1
  Comparison for inequality (a != b).

- FUNC_GREATER_THAN = 2
  Comparison for greater than (a > b). Cannot be used if type set to CTYPE_BOOLEAN or CTYPE_TRANSFORM.

- FUNC_GREATER_THAN_EQUAL = 3
  Comparison for greater than or equal (a >= b). Cannot be used if type set to CTYPE_BOOLEAN or CTYPE_TRANSFORM.

- FUNC_LESS_THAN = 4
  Comparison for less than (a < b). Cannot be used if type set to CTYPE_BOOLEAN or CTYPE_TRANSFORM.

- FUNC_LESS_THAN_EQUAL = 5
  Comparison for less than or equal (a <= b). Cannot be used if type set to CTYPE_BOOLEAN or CTYPE_TRANSFORM.

- FUNC_MAX = 6
  Represents the size of the Function enum.

### Enum Condition

- COND_ALL = 0
  The result will be true if all components in the vector satisfy the comparison condition.

- COND_ANY = 1
  The result will be true if any component in the vector satisfies the comparison condition.

- COND_MAX = 2
  Represents the size of the Condition enum.
