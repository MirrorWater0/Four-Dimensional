# VisualShaderNodeVectorOp

## Meta

- Name: VisualShaderNodeVectorOp
- Source: VisualShaderNodeVectorOp.xml
- Inherits: VisualShaderNodeVectorBase
- Inheritance Chain: VisualShaderNodeVectorOp -> VisualShaderNodeVectorBase -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

A vector operator to be used within the visual shader graph.

## Description

A visual shader node for use of vector operators. Operates on vector a and vector b.

## Quick Reference

```
[properties]
operator: int (VisualShaderNodeVectorOp.Operator) = 0
```

## Properties

- operator: int (VisualShaderNodeVectorOp.Operator) = 0 [set set_operator; get get_operator]
  The operator to be used.

## Constants

### Enum Operator

- OP_ADD = 0
  Adds two vectors.

- OP_SUB = 1
  Subtracts a vector from a vector.

- OP_MUL = 2
  Multiplies two vectors.

- OP_DIV = 3
  Divides vector by vector.

- OP_MOD = 4
  Returns the remainder of the two vectors.

- OP_POW = 5
  Returns the value of the first parameter raised to the power of the second, for each component of the vectors.

- OP_MAX = 6
  Returns the greater of two values, for each component of the vectors.

- OP_MIN = 7
  Returns the lesser of two values, for each component of the vectors.

- OP_CROSS = 8
  Calculates the cross product of two vectors.

- OP_ATAN2 = 9
  Returns the arc-tangent of the parameters.

- OP_REFLECT = 10
  Returns the vector that points in the direction of reflection. a is incident vector and b is the normal vector.

- OP_STEP = 11
  Vector step operator. Returns 0.0 if a is smaller than b and 1.0 otherwise.

- OP_ENUM_SIZE = 12
  Represents the size of the Operator enum.
