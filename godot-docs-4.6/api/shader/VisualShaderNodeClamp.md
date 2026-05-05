# VisualShaderNodeClamp

## Meta

- Name: VisualShaderNodeClamp
- Source: VisualShaderNodeClamp.xml
- Inherits: VisualShaderNode
- Inheritance Chain: VisualShaderNodeClamp -> VisualShaderNode -> Resource -> RefCounted -> Object

## Brief Description

Clamps a value within the visual shader graph.

## Description

Constrains a value to lie between min and max values.

## Quick Reference

```
[properties]
op_type: int (VisualShaderNodeClamp.OpType) = 0
```

## Properties

- op_type: int (VisualShaderNodeClamp.OpType) = 0 [set set_op_type; get get_op_type]
  A type of operands and returned value.

## Constants

### Enum OpType

- OP_TYPE_FLOAT = 0
  A floating-point scalar.

- OP_TYPE_INT = 1
  An integer scalar.

- OP_TYPE_UINT = 2
  An unsigned integer scalar.

- OP_TYPE_VECTOR_2D = 3
  A 2D vector type.

- OP_TYPE_VECTOR_3D = 4
  A 3D vector type.

- OP_TYPE_VECTOR_4D = 5
  A 4D vector type.

- OP_TYPE_MAX = 6
  Represents the size of the OpType enum.
