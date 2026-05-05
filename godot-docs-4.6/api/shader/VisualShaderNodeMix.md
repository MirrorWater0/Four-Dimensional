# VisualShaderNodeMix

## Meta

- Name: VisualShaderNodeMix
- Source: VisualShaderNodeMix.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeMix -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Linearly interpolates between two values within the visual shader graph.

## Description

Translates to mix(a, b, weight) in the shader language.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeMix.OpType) = 0
```

## Properties

- op_type: int (VisualShaderNodeMix.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

## Constants

### Enum OpType

- OP_TYPE_SCALAR = 0
  A floating-point scalar.

- OP_TYPE_VECTOR_2D = 1
  A 2D vector type.

- OP_TYPE_VECTOR_2D_SCALAR = 2
  The a and b ports use a 2D vector type. The weight port uses a scalar type.

- OP_TYPE_VECTOR_3D = 3
  A 3D vector type.

- OP_TYPE_VECTOR_3D_SCALAR = 4
  The a and b ports use a 3D vector type. The weight port uses a scalar type.

- OP_TYPE_VECTOR_4D = 5
  A 4D vector type.

- OP_TYPE_VECTOR_4D_SCALAR = 6
  The a and b ports use a 4D vector type. The weight port uses a scalar type.

- OP_TYPE_MAX = 7
  Represents the size of the OpType enum.
