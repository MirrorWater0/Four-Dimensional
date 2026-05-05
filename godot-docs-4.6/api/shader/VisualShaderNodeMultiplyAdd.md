# VisualShaderNodeMultiplyAdd

## Meta

- Name: VisualShaderNodeMultiplyAdd
- Source: VisualShaderNodeMultiplyAdd.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeMultiplyAdd -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Performs a fused multiply-add operation within the visual shader graph.

## Description

Uses three operands to compute (a * b + c) expression.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeMultiplyAdd.OpType) = 0
```

## Properties

- op_type: int (VisualShaderNodeMultiplyAdd.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

## Constants

### Enum OpType

- OP_TYPE_SCALAR = 0
  A floating-point scalar type.

- OP_TYPE_VECTOR_2D = 1
  A 2D vector type.

- OP_TYPE_VECTOR_3D = 2
  A 3D vector type.

- OP_TYPE_VECTOR_4D = 3
  A 4D vector type.

- OP_TYPE_MAX = 4
  Represents the size of the OpType enum.
