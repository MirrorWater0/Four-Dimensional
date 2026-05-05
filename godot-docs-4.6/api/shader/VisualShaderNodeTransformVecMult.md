# VisualShaderNodeTransformVecMult

## Meta

- Name: VisualShaderNodeTransformVecMult
- Source: VisualShaderNodeTransformVecMult.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeTransformVecMult -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Multiplies a Transform3D and a Vector3 within the visual shader graph.

## Description

A multiplication operation on a transform (4×4 matrix) and a vector, with support for different multiplication operators.

## Quick Reference

```
[properties]
operator: int (VisualShaderNodeTransformVecMult.Operator) = 0
```

## Properties

- operator: int (VisualShaderNodeTransformVecMult.Operator) = 0 [set set_operator; get get_operator]
  The multiplication type to be performed.

## Constants

### Enum Operator

- OP_AxB = 0
  Multiplies transform a by the vector b.

- OP_BxA = 1
  Multiplies vector b by the transform a.

- OP_3x3_AxB = 2
  Multiplies transform a by the vector b, skipping the last row and column of the transform.

- OP_3x3_BxA = 3
  Multiplies vector b by the transform a, skipping the last row and column of the transform.

- OP_MAX = 4
  Represents the size of the Operator enum.
